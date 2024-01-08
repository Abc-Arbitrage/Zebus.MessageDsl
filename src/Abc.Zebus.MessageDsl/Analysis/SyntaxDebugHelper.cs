using System.CodeDom.Compiler;
using System.IO;
using Abc.Zebus.MessageDsl.Ast;
using Antlr4.Runtime.Tree;

namespace Abc.Zebus.MessageDsl.Analysis;

internal static class SyntaxDebugHelper
{
    public static string DumpParseTree(ParsedContracts contracts)
    {
        var writer = new IndentedTextWriter(new StringWriter());
        DumpParseTree(contracts.ParseTree, writer);
        return writer.InnerWriter.ToString() ?? string.Empty;
    }

    private static void DumpParseTree(IParseTree tree, IndentedTextWriter writer)
    {
        writer.WriteLine("{0}: {1}", tree.GetType().Name, tree.GetText());
        writer.Indent++;
        for (var i = 0; i < tree.ChildCount; i++)
            DumpParseTree(tree.GetChild(i), writer);
        writer.Indent--;
    }
}
