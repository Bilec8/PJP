using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System.Collections.Generic;
using System.Text;

namespace PLC_Project
{
    public class CodeGeneratorVisitor : PLCBaseVisitor<string>
    {
        private readonly ParseTreeProperty<Type> _types;
        private readonly SymbolTable _symbols;
        private int _nextLabel = 0;

        public CodeGeneratorVisitor(ParseTreeProperty<Type> types, SymbolTable symbols)
        {
            _types   = types;
            _symbols = symbols;
        }

        private int NewLabel() => _nextLabel++;

        private static string Tag(Type t) => t switch
        {
            Type.Int    => "I",
            Type.Float  => "F",
            Type.Bool   => "B",
            Type.String => "S",
            _           => "I"
        };

        private static string DefaultValue(Type t) => t switch
        {
            Type.Int    => "0",
            Type.Float  => "0.0",
            Type.Bool   => "false",
            Type.String => "\"\"",
            _           => "0"
        };

        // ---- program ----

        public override string VisitProgram([NotNull] PLCParser.ProgramContext context)
        {
            var sb = new StringBuilder();
            foreach (var stmt in context.statement())
                sb.Append(Visit(stmt));
            return sb.ToString();
        }

        // ---- statements ----

        public override string VisitDeclaration([NotNull] PLCParser.DeclarationContext context)
        {
            var type = _types.Get(context.primitiveType());
            var tag  = Tag(type);
            var def  = DefaultValue(type);
            var sb   = new StringBuilder();
            foreach (var id in context.IDENTIFIER())
            {
                sb.AppendLine($"push {tag} {def}");
                sb.AppendLine($"save {id.Symbol.Text}");
            }
            return sb.ToString();
        }

        public override string VisitExprStatement([NotNull] PLCParser.ExprStatementContext context)
            => Visit(context.expr()) + "pop\n";

        public override string VisitEmptyStatement([NotNull] PLCParser.EmptyStatementContext context)
            => "";

        public override string VisitBlock([NotNull] PLCParser.BlockContext context)
        {
            var sb = new StringBuilder();
            foreach (var stmt in context.statement())
                sb.Append(Visit(stmt));
            return sb.ToString();
        }

        public override string VisitReadStatement([NotNull] PLCParser.ReadStatementContext context)
        {
            var sb = new StringBuilder();
            foreach (var id in context.IDENTIFIER())
            {
                var t = _symbols[id.Symbol];
                sb.AppendLine($"read {Tag(t)}");
                sb.AppendLine($"save {id.Symbol.Text}");
            }
            return sb.ToString();
        }

        public override string VisitWriteStatement([NotNull] PLCParser.WriteStatementContext context)
        {
            var sb = new StringBuilder();
            var exprs = context.expr();
            foreach (var e in exprs)
                sb.Append(Visit(e));
            sb.AppendLine($"print {exprs.Length}");
            return sb.ToString();
        }

        public override string VisitIfStatement([NotNull] PLCParser.IfStatementContext context)
        {
            int elseLabel = NewLabel();
            int endLabel  = NewLabel();
            var stmts = context.statement();

            var sb = new StringBuilder();
            sb.Append(Visit(context.expr()));
            sb.AppendLine($"fjmp {elseLabel}");
            sb.Append(Visit(stmts[0]));
            sb.AppendLine($"jmp {endLabel}");
            sb.AppendLine($"label {elseLabel}");
            if (stmts.Length > 1)
                sb.Append(Visit(stmts[1]));
            sb.AppendLine($"label {endLabel}");
            return sb.ToString();
        }

        public override string VisitWhileStatement([NotNull] PLCParser.WhileStatementContext context)
        {
            int startLabel = NewLabel();
            int endLabel   = NewLabel();

            var sb = new StringBuilder();
            sb.AppendLine($"label {startLabel}");
            sb.Append(Visit(context.expr()));
            sb.AppendLine($"fjmp {endLabel}");
            sb.Append(Visit(context.statement()));
            sb.AppendLine($"jmp {startLabel}");
            sb.AppendLine($"label {endLabel}");
            return sb.ToString();
        }

        // ---- literals ----

        public override string VisitInt([NotNull] PLCParser.IntContext context)
            => $"push I {context.INT().Symbol.Text}\n";

        public override string VisitFloat([NotNull] PLCParser.FloatContext context)
            => $"push F {context.FLOAT().Symbol.Text}\n";

        public override string VisitBool([NotNull] PLCParser.BoolContext context)
            => $"push B {context.BOOL().Symbol.Text}\n";

        public override string VisitString([NotNull] PLCParser.StringContext context)
            => $"push S {context.STRING().Symbol.Text}\n";

        public override string VisitId([NotNull] PLCParser.IdContext context)
            => $"load {context.IDENTIFIER().Symbol.Text}\n";

        public override string VisitParens([NotNull] PLCParser.ParensContext context)
            => Visit(context.expr());

        // ---- unary ----

        public override string VisitNot([NotNull] PLCParser.NotContext context)
            => Visit(context.expr()) + "not\n";

        public override string VisitUnaryMinus([NotNull] PLCParser.UnaryMinusContext context)
        {
            var t = _types.Get(context.expr());
            return Visit(context.expr()) + $"uminus {Tag(t)}\n";
        }

        // ---- binary arithmetic ----

        public override string VisitMulDivMod([NotNull] PLCParser.MulDivModContext context)
        {
            var (lCode, rCode, resultType) = NumericBinaryArgs(context.expr(0), context.expr(1));

            string op = context.op.Text switch
            {
                "*" => $"mul {Tag(resultType)}",
                "/" => $"div {Tag(resultType)}",
                _   => "mod"
            };
            return lCode + rCode + op + "\n";
        }

        public override string VisitAddSubConcat([NotNull] PLCParser.AddSubConcatContext context)
        {
            if (context.op.Text == ".")
                return Visit(context.expr(0)) + Visit(context.expr(1)) + "concat\n";

            var (lCode, rCode, resultType) = NumericBinaryArgs(context.expr(0), context.expr(1));
            string op = context.op.Text == "+"
                ? $"add {Tag(resultType)}"
                : $"sub {Tag(resultType)}";
            return lCode + rCode + op + "\n";
        }

        // ---- comparisons ----

        public override string VisitRelational([NotNull] PLCParser.RelationalContext context)
        {
            var (lCode, rCode, resultType) = NumericBinaryArgs(context.expr(0), context.expr(1));
            string op = context.op.Text == "<"
                ? $"lt {Tag(resultType)}"
                : $"gt {Tag(resultType)}";
            return lCode + rCode + op + "\n";
        }

        public override string VisitEquality([NotNull] PLCParser.EqualityContext context)
        {
            var lType = _types.Get(context.expr(0));
            var rType = _types.Get(context.expr(1));
            var lCode = Visit(context.expr(0));
            var rCode = Visit(context.expr(1));

            Type cmpType;
            if (lType == Type.Int && rType == Type.Float)      { lCode += "itof\n"; cmpType = Type.Float; }
            else if (lType == Type.Float && rType == Type.Int) { rCode += "itof\n"; cmpType = Type.Float; }
            else cmpType = lType;

            var code = lCode + rCode + $"eq {Tag(cmpType)}\n";
            if (context.op.Text == "!=") code += "not\n";
            return code;
        }

        // ---- logical ----

        public override string VisitOr([NotNull] PLCParser.OrContext context)
            => Visit(context.expr(0)) + Visit(context.expr(1)) + "or\n";

        public override string VisitAnd([NotNull] PLCParser.AndContext context)
            => Visit(context.expr(0)) + Visit(context.expr(1)) + "and\n";

        // ---- assignment ----

        public override string VisitAssignment([NotNull] PLCParser.AssignmentContext context)
        {
            var varType  = _symbols[context.IDENTIFIER().Symbol];
            var exprType = _types.Get(context.expr());

            var sb = new StringBuilder();
            sb.Append(Visit(context.expr()));
            if (varType == Type.Float && exprType == Type.Int)
                sb.AppendLine("itof");
            sb.AppendLine($"save {context.IDENTIFIER().Symbol.Text}");
            sb.AppendLine($"load {context.IDENTIFIER().Symbol.Text}");
            return sb.ToString();
        }

        // ---- helper ----

        private (string left, string right, Type resultType) NumericBinaryArgs(
            PLCParser.ExprContext leftCtx, PLCParser.ExprContext rightCtx)
        {
            var lType = _types.Get(leftCtx);
            var rType = _types.Get(rightCtx);
            var lCode = Visit(leftCtx);
            var rCode = Visit(rightCtx);

            if (lType == Type.Int && rType == Type.Float)      lCode += "itof\n";
            else if (lType == Type.Float && rType == Type.Int) rCode += "itof\n";

            var result = (lType == Type.Float || rType == Type.Float) ? Type.Float : lType;
            return (lCode, rCode, result);
        }
    }
}
