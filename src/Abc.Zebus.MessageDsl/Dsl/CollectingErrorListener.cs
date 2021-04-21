using System.Collections.Generic;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;

namespace Abc.Zebus.MessageDsl.Dsl
{
    internal class CollectingErrorListener : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
    {
        public ICollection<SyntaxError> Errors { get; }

        public CollectingErrorListener()
        {
            Errors = new List<SyntaxError>();
        }

        public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            var fakeToken = new CommonToken(0, string.Empty)
            {
                Line = line,
                Column = charPositionInLine
            };

            ReportError(msg, fakeToken, e);
        }

        public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            ReportError(msg, offendingSymbol, e);
        }

        private void ReportError(string msg, IToken offendingSymbol, RecognitionException exception)
        {
            switch (exception)
            {
                case NoViableAltException noViableAlt:
                {
                    var errorToken = noViableAlt.OffendingToken ?? noViableAlt.StartToken;
                    if (errorToken != null)
                    {
                        msg = errorToken.Type == Recognizer<int, LexerATNSimulator>.Eof
                            ? "More input expected, the file is not terminated properly"
                            : $"Unexpected input at {GetTokenDisplay(errorToken)}";
                    }

                    break;
                }

                case FailedPredicateException failedPredicate:
                {
                    var tokenDisplay = GetTokenDisplay(failedPredicate.OffendingToken);
                    msg = $"Syntax error at {tokenDisplay}";

                    if (failedPredicate.Context is MessageContractsParser.EndOfLineContext)
                        msg = $"End of line expected at {tokenDisplay}";
                    break;
                }
            }

            msg = Regex.Replace(msg, @"expecting\s+\{(.+)\}", "expecting one of: $1");
            Errors.Add(new SyntaxError(msg, offendingSymbol));
        }

        private static string GetTokenDisplay(IToken? token)
        {
            if (token == null)
                return "(unknown)";

            if (token.Type == Recognizer<int, LexerATNSimulator>.Eof)
                return "(end of expression)";

            var str = token.Text ?? string.Empty;
            str = str.Replace('\n', ' ').Replace('\r', ' ');
            return $"'{str}'";
        }
    }
}
