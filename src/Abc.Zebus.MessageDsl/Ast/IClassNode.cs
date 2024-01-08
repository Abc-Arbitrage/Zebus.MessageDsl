namespace Abc.Zebus.MessageDsl.Ast;

internal interface IClassNode : IMemberNode
{
    InheritanceModifier InheritanceModifier { get; set; }
}
