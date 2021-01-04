using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;
using Antlr4.Runtime;

namespace Abc.Zebus.MessageDsl.Dsl
{
    partial class MessageContractsParser
    {
        private bool IsAtStartOfPragma()
        {
            var prevToken = _input.Lt(-1);
            var sharpToken = _input.Lt(1);
            var pragmaToken = _input.Lt(2);

            if (sharpToken == null || pragmaToken == null)
                return false;

            if (sharpToken.Text != "#" || pragmaToken.Text != "pragma")
                return false;

            if (prevToken != null && prevToken.Line == sharpToken.Line)
                return false;

            if (sharpToken.Line != pragmaToken.Line)
                return false;

            return true;
        }

        private bool IsAtImplicitSeparator()
        {
            var prevToken = _input.Lt(-1);
            var nextToken = _input.Lt(1);

            return prevToken == null
                   || nextToken == null
                   || nextToken.Type == TokenConstants.Eof
                   || prevToken.Type == MessageContractsLexer.SEP
                   || nextToken.Type == MessageContractsLexer.SEP
                   || prevToken.Line != nextToken.Line;
        }

        private bool IsAtEndOfLine()
        {
            var prevToken = _input.Lt(-1);
            var nextToken = _input.Lt(1);

            return prevToken == null
                   || nextToken == null
                   || nextToken.Type == TokenConstants.Eof
                   || prevToken.Line != nextToken.Line;
        }

        private static bool IsValidIdEscape(IToken? escapeToken, IToken? nameToken)
        {
            if (escapeToken == null)
                return true;

            return nameToken?.StartIndex == escapeToken.StopIndex + 1;
        }

        private static bool IsValidIdEscape(IToken? escapeToken, ParserRuleContext? nameContext)
            => IsValidIdEscape(escapeToken, nameContext?.Start);

        private bool AreTwoNextTokensConsecutive()
        {
            var firstToken = _input.Lt(1);
            var secondToken = _input.Lt(2);

            return secondToken.StartIndex == firstToken.StopIndex + 1;
        }

        partial class IdContext
        {
            public string GetValidatedId(ParsedContracts contracts)
            {
                var id = nameId?.Text ?? nameCtxKw?.GetText() ?? nameKw?.GetText();

                if (string.IsNullOrEmpty(id))
                    return string.Empty;

                if (CSharpSyntax.IsCSharpKeyword(id!) && escape == null)
                    contracts.AddError(this, "'{0}' is a C# keyword and has to be escaped with '@'", id);

                return id;
            }
        }
    }
}
