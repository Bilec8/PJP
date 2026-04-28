using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Lab8
{
    public class TypeCheckingVisitor : PLC_Lab8_exprBaseVisitor<Type>
    {
        SymbolTable symbolTable = new SymbolTable();

        public override Type VisitProgram([NotNull] PLC_Lab8_exprParser.ProgramContext context)
        {
            foreach(var statement in context.statement())
            {
                Visit(statement);
            }
            return Type.Error;
        }

        public override Type VisitDeclaration([NotNull] PLC_Lab8_exprParser.DeclarationContext context)
        {
            var type = Visit(context.primitiveType());
            foreach(var identifier in context.IDENTIFIER())
            {
                symbolTable.Add(identifier.Symbol, type);
            }
            return Type.Error;
        }

        public override Type VisitPrintExpr([NotNull] PLC_Lab8_exprParser.PrintExprContext context)
        {
            var value = Visit(context.expr());
            
            return Type.Error;
        }
        public override Type VisitPrimitiveType([NotNull] PLC_Lab8_exprParser.PrimitiveTypeContext context)
        {
            if (context.type.Text.Equals("int")) return Type.Int;
            else return Type.Float;
        }

        public override Type VisitFloat([NotNull] PLC_Lab8_exprParser.FloatContext context)
        {
            return Type.Float;
        }
        public override Type VisitInt([NotNull] PLC_Lab8_exprParser.IntContext context)
        {
            return Type.Int;
        }
        public override Type VisitId([NotNull] PLC_Lab8_exprParser.IdContext context)
        {
            return symbolTable[context.IDENTIFIER().Symbol];
        }
        public override Type VisitParens([NotNull] PLC_Lab8_exprParser.ParensContext context)
        {
            return Visit(context.expr());
        }

        public override Type VisitMulDivMod([NotNull] PLC_Lab8_exprParser.MulDivModContext context)
        {
            var left = Visit(context.expr()[0]);
            var right = Visit(context.expr()[1]);
            if (left == Type.Error || right == Type.Error) return Type.Error;

            if (context.op.Type == PLC_Lab8_exprParser.MOD)
            {
                if (left == Type.Float || right == Type.Float)
                {
                    Errors.ReportError(context.MOD().Symbol, $"Module can be used only with integers.");
                    return Type.Error;
                }
                else
                {
                    return Type.Int;
                }
            }

            if (left == Type.Float || right == Type.Float)
            {
                return Type.Float;
            }
            else
            {
                return Type.Int;
            }
        }

        public override Type VisitAddSub([NotNull] PLC_Lab8_exprParser.AddSubContext context)
        {
            var left = Visit(context.expr()[0]);
            var right = Visit(context.expr()[1]);
            if (left == Type.Error || right == Type.Error) return Type.Error;
            if (left == Type.Float || right == Type.Float)
            {
                return Type.Float;
            }
            else
            {
                return Type.Int;
            }
        }

        public override Type VisitAssignment([NotNull] PLC_Lab8_exprParser.AssignmentContext context)
        {
            var right = Visit(context.expr());
            var variable = symbolTable[context.IDENTIFIER().Symbol];
            if (variable == Type.Error || right == Type.Error) return Type.Error;
            if (variable == Type.Int && right == Type.Float)
            {
                Errors.ReportError(context.IDENTIFIER().Symbol, $"Variable '{context.IDENTIFIER().GetText()}' type is int, but the assigned value is float.");
                return Type.Error;
            }
            return variable;
        }
    }
}
