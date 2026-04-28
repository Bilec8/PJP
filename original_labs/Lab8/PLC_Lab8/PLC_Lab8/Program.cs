
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace PLC_Lab8
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
            PLC_Lab8_exprLexer lexer = new PLC_Lab8_exprLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            PLC_Lab8_exprParser parser = new PLC_Lab8_exprParser(tokens);

            parser.AddErrorListener(new VerboseListener());

            IParseTree tree = parser.program();

            if (parser.NumberOfSyntaxErrors == 0)
            {
                //Console.WriteLine(tree.ToStringTree(parser));
                /*
                //visitor
                new TypeCheckingVisitor().Visit(tree);
                */
                //listener
                var typeChecking = new TypeCheckingListener();
                ParseTreeWalker walker = new ParseTreeWalker();
                walker.Walk(typeChecking, tree);

                if (Errors.NumberOfErrors != 0)
                {
                    Errors.PrintAndClearErrors();
                }
            }
        }
    }
}