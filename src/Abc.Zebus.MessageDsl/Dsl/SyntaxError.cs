using Antlr4.Runtime;

namespace Abc.Zebus.MessageDsl.Dsl;

public class SyntaxError
{
    public int LineNumber { get; }
    public int CharacterInLine { get; }
    public string? Token { get; }

    public string Message { get; }

    public SyntaxError(string message, IToken? startToken = null)
    {
        Message = message;

        if (startToken != null)
        {
            LineNumber = startToken.Line;
            CharacterInLine = startToken.Column + 1;
            Token = startToken.Text;
        }
    }

    public override string ToString()
    {
        return LineNumber > 0
            ? CharacterInLine > 0
                ? $"[{LineNumber}:{CharacterInLine}] {Message}"
                : $"[{LineNumber}] {Message}"
            : Message;
    }
}
