
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace PLC_Lab9
{
    public class Program
    {

        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var fileName = "input.txt";
            Console.WriteLine("Parsing: " + fileName);
            var inputFile = new StreamReader(fileName);
            AntlrInputStream input = new AntlrInputStream(inputFile);
            PLC_Lab9_exprLexer lexer = new PLC_Lab9_exprLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            PLC_Lab9_exprParser parser = new PLC_Lab9_exprParser(tokens);

            parser.AddErrorListener(new VerboseListener());

            IParseTree tree = parser.program();


            if (parser.NumberOfSyntaxErrors == 0)
            {
               
                var typeChecking = new TypeCheckingListener();
                ParseTreeWalker walker = new ParseTreeWalker();
                walker.Walk(typeChecking, tree);

                if (Errors.NumberOfErrors != 0)
                {
                    Errors.PrintAndClearErrors();
                }else
                {
                    /*
                     //Visitor
                    CodeGeneratorVisitor generator = new CodeGeneratorVisitor(typeChecking.Types, typeChecking.SymbolTable);
                    var code = generator.Visit(tree);
                    */
                    
                    //listener
                    CodeGeneratorListener generator = new CodeGeneratorListener(typeChecking.Types, typeChecking.SymbolTable);
                    walker.Walk(generator, tree);
                    var code = string.Join("\n", generator.Code);
                    
                    Console.WriteLine(code);
                }
            }
        }
    }
}