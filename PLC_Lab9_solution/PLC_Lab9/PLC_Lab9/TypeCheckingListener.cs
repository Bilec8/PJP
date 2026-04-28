using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Lab9
{
    public class TypeCheckingListener : PLC_Lab9_exprBaseListener
    {
        public SymbolTable SymbolTable { get; } = new SymbolTable();
        public ParseTreeProperty<Type> Types { get; } = new ParseTreeProperty<Type>();

        public override void ExitDeclaration([NotNull] PLC_Lab9_exprParser.DeclarationContext context)
        {
            var type = Types.Get(context.primitiveType());
            foreach(var identifier in context.IDENTIFIER())
            {
                SymbolTable.Add(identifier.Symbol, type);
            }
        }

        public override void ExitPrimitiveType([NotNull] PLC_Lab9_exprParser.PrimitiveTypeContext context)
        {
            if (context.type.Text.Equals("int")) Types.Put(context,Type.Int);
            else Types.Put(context, Type.Float);
        }

        public override void ExitFloat([NotNull] PLC_Lab9_exprParser.FloatContext context)
        {
            Types.Put(context, Type.Float);
        }
        public override void ExitInt([NotNull] PLC_Lab9_exprParser.IntContext context)
        {
            Types.Put(context, Type.Int);
        }
        public override void ExitId([NotNull] PLC_Lab9_exprParser.IdContext context)
        {
            Types.Put(context, SymbolTable[context.IDENTIFIER().Symbol]);
        }
        public override void ExitParens([NotNull] PLC_Lab9_exprParser.ParensContext context)
        {
            Types.Put(context, Types.Get(context.expr()));
        }

        public override void ExitMulDivMod([NotNull] PLC_Lab9_exprParser.MulDivModContext context)
        {
            var left = Types.Get(context.expr()[0]);
            var right = Types.Get(context.expr()[1]);
            if (left == Type.Error || right == Type.Error)
            {
                Types.Put( context, Type.Error);
                return;
            }

            if (context.op.Type == PLC_Lab9_exprParser.MOD)
            {
                if (left == Type.Float || right == Type.Float)
                {
                    Errors.ReportError(context.MOD().Symbol, $"Operator {context.MOD().GetText()} can be used only with integers.");
                    Types.Put(context, Type.Error);
                    return;
                }
                else
                {
                    Types.Put(context,Type.Int);
                }
            }

            if (left == Type.Float || right == Type.Float)
            {
                Types.Put(context, Type.Float);
            }
            else
            {
                Types.Put(context, Type.Int);
            }
        }

        public override void ExitAddSub([NotNull] PLC_Lab9_exprParser.AddSubContext context)
        {
            var left = Types.Get(context.expr()[0]);
            var right = Types.Get(context.expr()[1]);
            if (left == Type.Error || right == Type.Error)
            {
                Types.Put(context, Type.Error);
                return;
            }
            if (left == Type.Float || right == Type.Float)
            {
                Types.Put(context, Type.Float);
            }
            else
            {
                Types.Put(context, Type.Int);
            }
        }

        public override void ExitAssignment([NotNull] PLC_Lab9_exprParser.AssignmentContext context)
        {
            var right = Types.Get(context.expr());
            var variable = SymbolTable[context.IDENTIFIER().Symbol];
            if (variable == Type.Error || right == Type.Error)
            {
                Types.Put(context, Type.Error);
            }else if (variable == Type.Int && right == Type.Float)
            {
                Errors.ReportError(context.IDENTIFIER().Symbol, $"Variable '{context.IDENTIFIER().GetText()}' type is int, but the assigned value is float.");
                Types.Put(context, Type.Error);
            }
            else Types.Put(context, variable);
        }
    }
}
