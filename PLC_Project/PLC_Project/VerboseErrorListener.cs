using Antlr4.Runtime;
using System.IO;

namespace PLC_Project
{
    public class VerboseErrorListener : BaseErrorListener
    {
        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Errors.ReportError(line, charPositionInLine, $"Syntax error: {msg}");
        }
    }
}
