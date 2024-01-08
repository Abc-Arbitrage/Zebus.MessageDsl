namespace Abc.Zebus.MessageDsl.Ast;

internal interface IMemberNode : INamedNode
{
    AccessModifier AccessModifier { get; set; }
    MemberOptions Options { get; }
}
