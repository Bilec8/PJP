using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace PLC_Project
{
    public class TypeCheckingListener : PLCBaseListener
    {
        public SymbolTable SymbolTable { get; } = new SymbolTable();
        public ParseTreeProperty<Type> Types { get; } = new ParseTreeProperty<Type>();

        // ---- declarations ----

        public override void ExitPrimitiveType([NotNull] PLCParser.PrimitiveTypeContext context)
        {
            Types.Put(context, context.GetText() switch
            {
                "int"    => Type.Int,
                "float"  => Type.Float,
                "bool"   => Type.Bool,
                "string" => Type.String,
                "String" => Type.String,
                _        => Type.Error
            });
        }

        public override void ExitDeclaration([NotNull] PLCParser.DeclarationContext context)
        {
            var type = Types.Get(context.primitiveType());
            foreach (var id in context.IDENTIFIER())
                SymbolTable.Add(id.Symbol, type);
        }

        // ---- literals ----

        public override void ExitInt([NotNull] PLCParser.IntContext context)
            => Types.Put(context, Type.Int);

        public override void ExitFloat([NotNull] PLCParser.FloatContext context)
            => Types.Put(context, Type.Float);

        public override void ExitBool([NotNull] PLCParser.BoolContext context)
            => Types.Put(context, Type.Bool);

        public override void ExitString([NotNull] PLCParser.StringContext context)
            => Types.Put(context, Type.String);

        public override void ExitId([NotNull] PLCParser.IdContext context)
            => Types.Put(context, SymbolTable[context.IDENTIFIER().Symbol]);

        public override void ExitParens([NotNull] PLCParser.ParensContext context)
            => Types.Put(context, Types.Get(context.expr()));

        // ---- unary ----

        public override void ExitNot([NotNull] PLCParser.NotContext context)
        {
            var t = Types.Get(context.expr());
            if (t != Type.Bool && t != Type.Error)
                Errors.ReportError(context.expr().Start, "Operator '!' requires bool operand.");
            Types.Put(context, t == Type.Error ? Type.Error : Type.Bool);
        }

        public override void ExitUnaryMinus([NotNull] PLCParser.UnaryMinusContext context)
        {
            var t = Types.Get(context.expr());
            if (t != Type.Int && t != Type.Float && t != Type.Error)
                Errors.ReportError(context.expr().Start, "Unary '-' requires int or float operand.");
            Types.Put(context, t == Type.Error ? Type.Error : t);
        }

        // ---- binary arithmetic ----

        public override void ExitMulDivMod([NotNull] PLCParser.MulDivModContext context)
        {
            var left  = Types.Get(context.expr(0));
            var right = Types.Get(context.expr(1));
            if (left == Type.Error || right == Type.Error) { Types.Put(context, Type.Error); return; }

            if (context.op.Text == "%")
            {
                if (left != Type.Int || right != Type.Int)
                {
                    Errors.ReportError(context.op, "Operator '%' requires integer operands.");
                    Types.Put(context, Type.Error);
                    return;
                }
                Types.Put(context, Type.Int);
                return;
            }

            if (!IsNumeric(left) || !IsNumeric(right))
            {
                Errors.ReportError(context.op, $"Operator '{context.op.Text}' requires int or float operands.");
                Types.Put(context, Type.Error);
                return;
            }
            Types.Put(context, (left == Type.Float || right == Type.Float) ? Type.Float : Type.Int);
        }

        public override void ExitAddSubConcat([NotNull] PLCParser.AddSubConcatContext context)
        {
            var left  = Types.Get(context.expr(0));
            var right = Types.Get(context.expr(1));
            if (left == Type.Error || right == Type.Error) { Types.Put(context, Type.Error); return; }

            if (context.op.Text == ".")
            {
                if (left != Type.String || right != Type.String)
                {
                    Errors.ReportError(context.op, "Operator '.' requires string operands.");
                    Types.Put(context, Type.Error);
                    return;
                }
                Types.Put(context, Type.String);
                return;
            }

            if (!IsNumeric(left) || !IsNumeric(right))
            {
                Errors.ReportError(context.op, $"Operator '{context.op.Text}' requires int or float operands.");
                Types.Put(context, Type.Error);
                return;
            }
            Types.Put(context, (left == Type.Float || right == Type.Float) ? Type.Float : Type.Int);
        }

        // ---- comparisons ----

        public override void ExitRelational([NotNull] PLCParser.RelationalContext context)
        {
            var left  = Types.Get(context.expr(0));
            var right = Types.Get(context.expr(1));
            if (left == Type.Error || right == Type.Error) { Types.Put(context, Type.Error); return; }

            if (!IsNumeric(left) || !IsNumeric(right))
            {
                Errors.ReportError(context.op, $"Operator '{context.op.Text}' requires int or float operands.");
                Types.Put(context, Type.Error);
                return;
            }
            Types.Put(context, Type.Bool);
        }

        public override void ExitEquality([NotNull] PLCParser.EqualityContext context)
        {
            var left  = Types.Get(context.expr(0));
            var right = Types.Get(context.expr(1));
            if (left == Type.Error || right == Type.Error) { Types.Put(context, Type.Error); return; }

            bool valid = (left == right)
                      || (IsNumeric(left) && IsNumeric(right));

            if (!valid)
            {
                Errors.ReportError(context.op, $"Operator '{context.op.Text}' cannot compare {left} and {right}.");
                Types.Put(context, Type.Error);
                return;
            }
            Types.Put(context, Type.Bool);
        }

        // ---- logical ----

        public override void ExitOr([NotNull] PLCParser.OrContext context)
            => CheckLogical(context, Types.Get(context.expr(0)), Types.Get(context.expr(1)), "||");

        public override void ExitAnd([NotNull] PLCParser.AndContext context)
            => CheckLogical(context, Types.Get(context.expr(0)), Types.Get(context.expr(1)), "&&");

        // ---- assignment ----

        public override void ExitAssignment([NotNull] PLCParser.AssignmentContext context)
        {
            var right   = Types.Get(context.expr());
            var varType = SymbolTable[context.IDENTIFIER().Symbol];
            if (varType == Type.Error || right == Type.Error) { Types.Put(context, Type.Error); return; }

            bool ok = varType == right || (varType == Type.Float && right == Type.Int);
            if (!ok)
            {
                Errors.ReportError(context.IDENTIFIER().Symbol,
                    $"Cannot assign {right} to variable '{context.IDENTIFIER().GetText()}' of type {varType}.");
                Types.Put(context, Type.Error);
                return;
            }
            Types.Put(context, varType);
        }

        // ---- statements ----

        public override void ExitReadStatement([NotNull] PLCParser.ReadStatementContext context)
        {
            foreach (var id in context.IDENTIFIER())
                _ = SymbolTable[id.Symbol]; // triggers "not declared" error if missing
        }

        public override void ExitIfStatement([NotNull] PLCParser.IfStatementContext context)
        {
            var t = Types.Get(context.expr());
            if (t != Type.Bool && t != Type.Error)
                Errors.ReportError(context.expr().Start, "Condition of 'if' must be bool.");
        }

        public override void ExitWhileStatement([NotNull] PLCParser.WhileStatementContext context)
        {
            var t = Types.Get(context.expr());
            if (t != Type.Bool && t != Type.Error)
                Errors.ReportError(context.expr().Start, "Condition of 'while' must be bool.");
        }

        // ---- helpers ----

        private static bool IsNumeric(Type t) => t == Type.Int || t == Type.Float;

        private void CheckLogical(Antlr4.Runtime.ParserRuleContext context, Type left, Type right, string op)
        {
            if (left == Type.Error || right == Type.Error) { Types.Put(context, Type.Error); return; }
            if (left != Type.Bool || right != Type.Bool)
            {
                Errors.ReportError(context.Start, $"Operator '{op}' requires bool operands.");
                Types.Put(context, Type.Error);
                return;
            }
            Types.Put(context, Type.Bool);
        }
    }
}
