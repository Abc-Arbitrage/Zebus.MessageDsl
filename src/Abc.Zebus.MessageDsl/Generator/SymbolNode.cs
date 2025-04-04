using System.Collections.Generic;
using System.Linq;
using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Generator;

public class SymbolNode
{
    private readonly Dictionary<string, SymbolNode> _children = [];

    public SymbolNode(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public MessageDefinition? Definition { get; private set; }

    public IReadOnlyDictionary<string, SymbolNode> Children => _children;

    public static SymbolNode Create(ParsedContracts parsed)
    {
        var rootNode = new SymbolNode("");
        foreach (var message in parsed.Messages)
        {
            var current = rootNode;
            foreach (var containingClass in message.ContainingClasses)
            {
                var name = containingClass.ProtoBufType;
                if (!current._children.TryGetValue(name, out var childNode))
                {
                    childNode = new SymbolNode(name);
                    current._children.Add(name, childNode);
                }
                current = childNode;
            }
            if(!current._children.TryGetValue(message.Name, out var messageNode))
            {
                messageNode = new SymbolNode(message.Name);
                current._children.Add(message.Name, messageNode);
            }
            messageNode.Definition = message;
        }

        return rootNode;
    }
}
