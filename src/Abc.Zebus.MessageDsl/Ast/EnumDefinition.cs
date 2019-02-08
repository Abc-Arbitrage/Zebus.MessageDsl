using System;
using System.Collections.Generic;
using System.Globalization;

namespace Abc.Zebus.MessageDsl.Ast
{
    public class EnumDefinition : AstNode, INamedNode
    {
        private MemberOptions _options;

        public string Name { get; set; }
        public TypeName UnderlyingType { get; set; } = "int";
        public AttributeSet Attributes { get; } = new AttributeSet();
        public IList<EnumMemberDefinition> Members { get; } = new List<EnumMemberDefinition>();

        public MemberOptions Options
        {
            get => _options ?? (_options = new MemberOptions());
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

        internal object GetValidUnderlyingValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            value = value.Trim();
            var numberStyles = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? NumberStyles.HexNumber : NumberStyles.Integer;

            if (numberStyles == NumberStyles.HexNumber)
                value = value.Substring(2);

            if (value.Length == 0)
                return null;

            switch (UnderlyingType.NetType)
            {
                case "byte":
                {
                    return byte.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? (object)result : null;
                }

                case "sbyte":
                {
                    return sbyte.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? (object)result : null;
                }

                case "short":
                {
                    return short.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? (object)result : null;
                }

                case "ushort":
                {
                    return ushort.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? (object)result : null;
                }

                case "int":
                {
                    return int.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? (object)result : null;
                }

                case "uint":
                {
                    return uint.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? (object)result : null;
                }

                case "long":
                {
                    return long.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? (object)result : null;
                }

                case "ulong":
                {
                    return ulong.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var result) ? (object)result : null;
                }

                default:
                    return null;
            }
        }
    }
}
