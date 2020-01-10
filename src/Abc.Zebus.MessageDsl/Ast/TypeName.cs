using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Abc.Zebus.MessageDsl.Generator;
using Abc.Zebus.MessageDsl.Support;

namespace Abc.Zebus.MessageDsl.Ast
{
    public sealed class TypeName : IEquatable<TypeName>
    {
        private static readonly Regex _reSystemTypeName = new Regex(@"\b(?:global::|(?<!::))System\.(?<unqualifiedName>\w+)(?!\.)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _reUnqualifiedName = new Regex(@"\b(?<!\.)\w+(?!\.)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _reWhitespace = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _reComma = new Regex(@"(?<=[\w?\]>]),", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _reIdentifierPart = new Regex(@"[^,\[\]<>\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Dictionary<string, string> _aliasTypeMap = new Dictionary<string, string>
        {
            { "bool", "Boolean" },
            { "byte", "Byte" },
            { "sbyte", "SByte" },
            { "char", "Char" },
            { "decimal", "Decimal" },
            { "double", "Double" },
            { "float", "Single" },
            { "int", "Int32" },
            { "uint", "UInt32" },
            { "long", "Int64" },
            { "ulong", "UInt64" },
            { "object", "Object" },
            { "short", "Int16" },
            { "ushort", "UInt16" },
            { "string", "String" },
        };

        private static readonly Dictionary<string, string> _clrTypeToAlias;

        private static readonly HashSet<string> _knownBclTypes = new HashSet<string>
        {
            "TimeSpan",
            "DateTime",
            "Guid",
            "Decimal"
        };

        private static readonly Dictionary<string, string> _protoTypeNameMap = new Dictionary<string, string>
        {
            { "Double", "double" },
            { "Single", "float" },
            { "Int32", "int32" },
            { "Int64", "int64" },
            { "UInt32", "uint32" },
            { "UInt64", "uint64" },
            { "Boolean", "bool" },
            { "String", "string" },
        };

        private static readonly HashSet<string> _packableProtoBufTypes = new HashSet<string>
        {
            "double", "float", "int32", "int64", "uint32", "uint64", "sint32", "sint64", "fixed32", "fixed64", "sfixed32", "sfixed64", "bool"
        };

        private static readonly HashSet<string> _csharpNonTypeKeywords = CSharpSyntax.EnumerateCSharpKeywords().Except(_aliasTypeMap.Keys).ToHashSet();

        static TypeName()
        {
            _clrTypeToAlias = new Dictionary<string, string>();
            foreach (var pair in _aliasTypeMap)
                _clrTypeToAlias.Add(pair.Value, pair.Key);

            _clrTypeToAlias.Add(typeof(List<>).Namespace + ".List", "List");
        }

        private string? _protoBufType;

        public string NetType { get; }

        public string ProtoBufType => _protoBufType ??= GetProtoBufType();

        public bool IsArray => NetType.EndsWith("[]");
        public bool IsList => NetType.StartsWith("List<") && NetType.EndsWith(">");
        public bool IsDictionary => NetType.StartsWith("Dictionary<") && NetType.EndsWith(">");
        public bool IsHashSet => NetType.StartsWith("HashSet<") && NetType.EndsWith(">");
        public bool IsRepeated => IsArray || IsList || IsHashSet;

        public bool IsNullable => NetType.EndsWith("?");

        public bool IsPackable => IsRepeated && _packableProtoBufTypes.Contains(ProtoBufType);

        public TypeName(string? netType)
        {
            NetType = NormalizeName(netType ?? string.Empty);
        }

        public TypeName? GetRepeatedItemType()
        {
            if (IsArray)
                return NetType.Substring(0, NetType.Length - 2);

            if (IsList)
                return NetType.Substring("List<".Length, NetType.Length - "List<>".Length);

            if (IsHashSet)
                return NetType.Substring("HashSet<".Length, NetType.Length - "HashSet<>".Length);

            return null;
        }

        public TypeName GetNonNullableType()
        {
            if (IsNullable)
                return NetType.Substring(0, NetType.Length - 1);

            return this;
        }

        public static implicit operator TypeName(string? netType) => new TypeName(netType);

        public override bool Equals(object? obj) => Equals(obj as TypeName);

        public bool Equals(TypeName? other) => other != null && other.NetType == NetType;

        public override int GetHashCode() => NetType.GetHashCode();

        public override string ToString() => NetType;

        private static string NormalizeName(string name)
        {
            name = _reWhitespace.Replace(name, string.Empty);
            name = _reComma.Replace(name, ", ");

            name = _reIdentifierPart.Replace(name, match => NormalizeNamePart(match.Value));

            return name;
        }

        private static string NormalizeNamePart(string name)
        {
            name = name.TrimStart('@');

            name = _reSystemTypeName.Replace(name, match =>
            {
                var unqualifiedName = match.Groups["unqualifiedName"].Value;
                return _clrTypeToAlias.GetValueOrDefault(unqualifiedName) ?? unqualifiedName;
            });

            name = _clrTypeToAlias.GetValueOrDefault(name) ?? name;

            if (_csharpNonTypeKeywords.Contains(name))
                return "@" + name;

            return name;
        }

        private string GetClrSystemTypeName()
        {
            var name = _reSystemTypeName.Replace(NetType, "${unqualifiedName}");
            return _reUnqualifiedName.Replace(name, match => _aliasTypeMap.GetValueOrDefault(match.Value) ?? match.Value);
        }

        private string GetProtoBufType()
        {
            var type = GetNonNullableType();

            if (type.IsRepeated)
                type = type.GetRepeatedItemType()!;

            var clrName = type.GetClrSystemTypeName();

            if (clrName.StartsWith("@"))
                clrName = clrName.Substring(1);

            if (_knownBclTypes.Contains(clrName))
                return "bcl." + clrName;

            var name = _protoTypeNameMap.GetValueOrDefault(clrName) ?? type.NetType;
            name = name.Replace("::", ".");

            return name;
        }
    }
}
