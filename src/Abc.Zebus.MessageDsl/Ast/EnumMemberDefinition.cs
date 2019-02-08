namespace Abc.Zebus.MessageDsl.Ast
{
    public class EnumMemberDefinition : AstNode, INamedNode
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public AttributeSet Attributes { get; } = new AttributeSet();
        internal int? ProtoValue { get; set; }
    }
}
