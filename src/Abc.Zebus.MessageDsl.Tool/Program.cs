using System.CommandLine;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;

namespace Abc.Zebus.MessageDsl.Tool;

public class Program
{
    public static void Main(string[] args)
    {
        var mainCommand = new RootCommand();
        var path = new Argument<string?>(".msg file", "The .msg file to process.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var defaultNamespace = new Option<string?>(name: "--namespace", description: "The default namespace to use for the generated files", getDefaultValue: () => null);
        var outputType = new Option<Format>(name: "--format", description: "The output format to generate (csharp or proto)", getDefaultValue: () => Format.Proto);

        mainCommand.AddArgument(path);
        mainCommand.AddOption(defaultNamespace);
        mainCommand.AddOption(outputType);

        mainCommand.SetHandler(
            (path, defaultNamespace, outputType) =>
            {
                var txt = path != null ? File.ReadAllText(path) : Console.In.ReadToEnd();
                var parsed = ParsedContracts.Parse(txt, defaultNamespace);
                foreach (var message in parsed.Messages)
                {
                    message.Options.Proto = true;
                }

                switch (outputType)
                {
                    case Format.CSharp:
                    {
                        var cs = CSharpGenerator.Generate(parsed);
                        Console.Write(cs);
                        break;
                    }
                    case Format.Proto:
                    {
                        var proto = ProtoGenerator.Generate(parsed);
                        Console.Write(proto);
                        break;
                    }
                }
            },
            path,
            defaultNamespace,
            outputType
        );

        mainCommand.Invoke(args);
    }
}

internal enum Format
{
    Proto,
    CSharp
}
