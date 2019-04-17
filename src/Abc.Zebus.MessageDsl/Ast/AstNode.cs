using Abc.Zebus.MessageDsl.Analysis;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Abc.Zebus.MessageDsl.Ast
{
    public class AstNode
    {
        internal ParserRuleContext ParseContext { get; set; }

        public TextInterval GetSourceTextInterval()
        {
            var startIndex = ParseContext?.Start?.StartIndex ?? 0;
            var endIndex = ParseContext?.Stop?.StopIndex + 1 ?? 0;

            return new TextInterval(startIndex, endIndex);
        }

        public string GetSourceText()
        {
            var interval = GetSourceTextInterval();
            if (interval.IsEmpty)
                return string.Empty;

            return ParseContext?.Start?.InputStream?.GetText(Interval.Of(interval.Start, interval.End - 1)) ?? string.Empty;
        }
    }
}
