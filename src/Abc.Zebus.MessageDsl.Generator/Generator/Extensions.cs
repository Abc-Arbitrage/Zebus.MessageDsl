using Abc.Zebus.MessageDsl.Dsl;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Abc.Zebus.MessageDsl.Generator;

internal static class Extensions
{
    private static readonly char[] _crlf = ['\r', '\n'];

    public static LinePositionSpan ToLinePositionSpan(this SyntaxError error)
    {
        if (error.LineNumber <= 0 || error.CharacterInLine <= 0)
            return default;

        var length = 0;

        if (error.Token is not null)
        {
            length = error.Token.IndexOfAny(_crlf);
            if (length < 0)
                length = error.Token.Length;
        }

        var startPosition = new LinePosition(error.LineNumber - 1, error.CharacterInLine - 1);
        var endPosition = new LinePosition(startPosition.Line, startPosition.Character + length);

        return new LinePositionSpan(startPosition, endPosition);
    }
}
