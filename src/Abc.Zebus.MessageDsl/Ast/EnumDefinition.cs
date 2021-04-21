using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Abc.Zebus.MessageDsl.Ast
{
    public class EnumDefinition : AstNode, IMemberNode
    {
        private MemberOptions? _options;

        public string Name { get; set; } = default!;
        public TypeName UnderlyingType { get; set; } = "int";
        public AccessModifier AccessModifier { get; set; }
        public AttributeSet Attributes { get; } = new();
        public IList<EnumMemberDefinition> Members { get; } = new List<EnumMemberDefinition>();

        public MemberOptions Options
        {
            get => _options ??= new MemberOptions();
            set => _options = value;
        }

        public override string ToString() => Name;

        internal bool IsValidUnderlyingType()
        {
            switch (UnderlyingType.NetType)
            {
                case "byte":
                case "sbyte":
                case "short":
                case "ushort":
                case "int":
                case "uint":
                case "long":
                case "ulong":
                    return true;

                default:
                    return false;
            }
        }

        [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
        internal object? GetValidUnderlyingValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            value = value!.Trim();
            var numberStyles = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? NumberStyles.HexNumber : NumberStyles.Integer;

            if (numberStyles == NumberStyles.HexNumber)
                value = value.Substring(2);

            if (value.Length == 0)
                return null;

            return UnderlyingType.NetType switch
            {
                "byte"   => byte.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : null,
                "sbyte"  => sbyte.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : null,
                "short"  => short.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : null,
                "ushort" => ushort.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : null,
                "int"    => int.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : null,
                "uint"   => uint.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : null,
                "long"   => long.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : null,
                "ulong"  => ulong.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : null,
                _        => null
            };
        }
    }
}
