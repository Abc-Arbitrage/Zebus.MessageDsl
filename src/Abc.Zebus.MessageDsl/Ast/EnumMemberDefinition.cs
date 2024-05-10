namespace Abc.Zebus.MessageDsl.Ast;

public class EnumMemberDefinition : AstNode, INamedNode
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public AttributeSet Attributes { get; } = new();

    internal string? InferredValueAsCSharpString { get; set; }
    internal object? InferredValueAsNumber { get; set; }
    internal bool IsDiscarded { get; set; }

    public override string ToString()
        => Name;
}
