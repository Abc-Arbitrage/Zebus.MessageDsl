using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Abc.Zebus.MessageDsl.Generator
{
    public static class CSharpSyntax
    {
        private static readonly HashSet<string> _csharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break",
            "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default",
            "delegate", "do", "double", "else", "enum",
            "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto",
            "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace",
            "new", "null", "object", "operator", "out",
            "override", "params", "private", "protected", "public",
            "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked",
            "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while"
        };

        public static IEnumerable<string> EnumerateCSharpKeywords() => _csharpKeywords.Select(i => i);
        public static bool IsCSharpKeyword(string id) => _csharpKeywords.Contains(id);

        private static readonly Regex _tokenRe = new Regex(@"@?\s*(?<id>\w+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // https://msdn.microsoft.com/en-us/library/aa664670.aspx
        private static readonly Regex _identifierRe = new Regex(@"^@?[\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}_][\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // https://msdn.microsoft.com/en-us/library/aa664669.aspx
        private static readonly Regex _unicodeEscapeSequence = new Regex(@"\\u(?<hex>[0-9a-fA-F]{4})|\\U(?<hex>[0-9a-fA-F]{8})", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Identifier(string? id)
        {
            return _tokenRe.Replace(id ?? string.Empty, match =>
            {
                var token = match.Groups["id"].Value;
                return IsCSharpKeyword(token)
                    ? "@" + token
                    : token;
            });
        }

        public static bool IsValidNamespace(string? ns)
        {
            if (string.IsNullOrEmpty(ns))
                return false;

            return ns!.Split(new[] { '.' }, StringSplitOptions.None)
                      .All(IsValidIdentifier);
        }

        public static bool IsValidIdentifier(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            id = ProcessUnicodeEscapeSequences(id!);

            if (!_identifierRe.IsMatch(id))
                return false;

            if (IsCSharpKeyword(id))
                return false;

            return true;
        }

        private static string ProcessUnicodeEscapeSequences(string value)
            => _unicodeEscapeSequence.Replace(value, match => char.ConvertFromUtf32(Convert.ToInt32(match.Groups["hex"].Value, 16)));
    }
}
