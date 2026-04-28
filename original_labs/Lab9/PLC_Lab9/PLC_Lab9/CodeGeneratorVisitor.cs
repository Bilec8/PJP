using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Lab9
{
    public class CodeGeneratorVisitor : PLC_Lab9_exprBaseVisitor<string>
    {
        private ParseTreeProperty<Type> Types;
        private SymbolTable SymbolTable;

        public CodeGeneratorVisitor(ParseTreeProperty<Type> types, SymbolTable symbolTable)
        {
            Types = types;
            SymbolTable = symbolTable;
        }

        public override string VisitProgram([NotNull] PLC_Lab9_exprParser.ProgramContext context)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var statement in context.statement())
            {
                sb.Append(Visit(statement));
            }
            return sb.ToString();
        }

        public override string VisitDeclaration([NotNull] PLC_Lab9_exprParser.DeclarationContext context)
        {
            StringBuilder sb = new StringBuilder();
            string constant = Visit(context.primitiveType());
            foreach (var name in context.IDENTIFIER())
            {
                sb.AppendLine($"PUSH {constant} 0");
                sb.AppendLine($"SAVE {constant} {name.Symbol.Text}");
            }
            return sb.ToString();
        }

        public override string VisitPrimitiveType([NotNull] PLC_Lab9_exprParser.PrimitiveTypeContext context)
        {
            return context.type.Text == "int" ? "I" : "F";
        }

        public override string VisitPrintExpr([NotNull] PLC_Lab9_exprParser.PrintExprContext context)
        {
            var code = Visit(context.expr());
            var type = Types.Get(context.expr());
            return code + $"PRINT {(type == Type.Int? "I":"F")}\n";
        }

        public override string VisitFloat([NotNull] PLC_Lab9_exprParser.FloatContext context)
        {
            return $"PUSH F {context.FLOAT().Symbol.Text}\n";
        }
        public override string VisitInt([NotNull] PLC_Lab9_exprParser.IntContext context)
        {
            return $"PUSH I {context.INT().Symbol.Text}\n";
        }
        public override string VisitId([NotNull] PLC_Lab9_exprParser.IdContext context)
        {
            return $"LOAD {context.IDENTIFIER().Symbol.Text}\n";
        }
        public override string VisitParens([NotNull] PLC_Lab9_exprParser.ParensContext context)
        {
            return Visit(context.expr());
        }

        public override string VisitMulDivMod([NotNull] PLC_Lab9_exprParser.MulDivModContext context)
        {
            var left = Visit(context.expr()[0]);
            var right = Visit(context.expr()[1]);
            if (context.op.Type == PLC_Lab9_exprParser.MUL)
            {
                return left + right + "MUL\n";
            } else if (context.op.Type == PLC_Lab9_exprParser.DIV)
            {
                return left + right + "DIV\n";
            }else
            {
                return left + right + "MOD\n";
            }

        }

        public override string VisitAddSub([NotNull] PLC_Lab9_exprParser.AddSubContext context)
        {
            var left = Visit(context.expr()[0]);
            var right = Visit(context.expr()[1]);
            if (context.op.Type == PLC_Lab9_exprParser.ADD)
            {
                return left + right + "ADD\n";
            }else
            {
                return left + right + "SUB\n";
            }
        }

        public override string VisitAssignment([NotNull] PLC_Lab9_exprParser.AssignmentContext context)
        {
            var type = SymbolTable[context.IDENTIFIER().Symbol];
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Visit(context.expr()));
            stringBuilder.AppendLine($"SAVE {(type == Type.Int? "I" : "F")} {context.IDENTIFIER().Symbol.Text}");
            stringBuilder.AppendLine($"LOAD {context.IDENTIFIER().Symbol.Text}");
            return stringBuilder.ToString();
        }
    }
}
