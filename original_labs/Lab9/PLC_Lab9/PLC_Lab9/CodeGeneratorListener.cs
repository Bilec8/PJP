using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Lab9
{
    public class CodeGeneratorListener : PLC_Lab9_exprBaseListener
    {
        private ParseTreeProperty<Type> Types;
        private SymbolTable SymbolTable;

        public List<string> Code { get; } = new List<string>();

        public CodeGeneratorListener(ParseTreeProperty<Type> types, SymbolTable symbolTable)
        {
            Types = types;
            SymbolTable = symbolTable;
        }

        public override void ExitDeclaration([NotNull] PLC_Lab9_exprParser.DeclarationContext context)
        {
            string constant = context.primitiveType().GetText() == "int" ? "I" : "F";
            foreach(var name in context.IDENTIFIER())
            {
                Code.Add($"PUSH {constant} 0");
                Code.Add($"SAVE {constant} {name.Symbol.Text}");
            }
        }

        public override void ExitPrintExpr([NotNull] PLC_Lab9_exprParser.PrintExprContext context)
        {
            var type = Types.Get(context.expr());
            Code.Add($"PRINT {(type == Type.Int ? "I" : "F")}");
        }

        public override void ExitFloat([NotNull] PLC_Lab9_exprParser.FloatContext context)
        {
            Code.Add($"PUSH F {context.FLOAT().Symbol.Text}");
        }
        public override void ExitInt([NotNull] PLC_Lab9_exprParser.IntContext context)
        {
            Code.Add($"PUSH I {context.INT().Symbol.Text}");
        }
        public override void ExitId([NotNull] PLC_Lab9_exprParser.IdContext context)
        {
            Code.Add($"LOAD {context.IDENTIFIER().Symbol.Text}");
        }
        
        public override void ExitMulDivMod([NotNull] PLC_Lab9_exprParser.MulDivModContext context)
        {
            if (context.op.Type == PLC_Lab9_exprParser.MUL)
            {
                Code.Add("MUL");
            }
            else if (context.op.Type == PLC_Lab9_exprParser.DIV)
            {
                Code.Add("DIV");
            }
            else
            {
                Code.Add("MOD");
            }

        }

        public override void ExitAddSub([NotNull] PLC_Lab9_exprParser.AddSubContext context)
        {
            if (context.op.Type == PLC_Lab9_exprParser.ADD)
            {
                Code.Add("ADD");
            }
            else
            {
                Code.Add("SUB");
            }
        }

        public override void ExitAssignment([NotNull] PLC_Lab9_exprParser.AssignmentContext context)
        {
            var type = SymbolTable[context.IDENTIFIER().Symbol];
            Code.Add($"SAVE {(type == Type.Int ? "I" : "F")} {context.IDENTIFIER().Symbol.Text}");
            Code.Add($"LOAD {context.IDENTIFIER().Symbol.Text}");
        }
    }
}
