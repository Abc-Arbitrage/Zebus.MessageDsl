using System.Collections.Generic;

namespace Abc.Zebus.MessageDsl.Ast;

public class GenericConstraint : AstNode
{
    public string GenericParameterName { get; set; } = string.Empty;

    public bool IsClass { get; set; }
    public bool IsStruct { get; set; }
    public bool HasDefaultConstructor { get; set; }

    public ISet<TypeName> Types { get; } = new HashSet<TypeName>();

    public override string ToString()
        => GenericParameterName;
}
