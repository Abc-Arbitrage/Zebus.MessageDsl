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
                string? txt = null;
                try
                {
                    if (path != null)
                        txt = File.ReadAllText(path);
                }
                catch (FileNotFoundException)
                {
                    Console.Error.WriteLine($"File {path} does not exists.");
                    Environment.Exit(1);
                }
                catch (DirectoryNotFoundException)
                {
                    Console.Error.WriteLine($"{path} is a directory.");
                    Environment.Exit(1);
                }
                txt ??= Console.In.ReadToEnd();
                var parsed = ParsedContracts.Parse(txt, defaultNamespace);
                foreach (var error in parsed.Errors)
                {
                    Console.Error.WriteLine(error);
                }

                if (parsed.Errors.Count != 0)
                    Environment.Exit(2);

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
                        return;
                    }
                    case Format.Proto:
                    {
                        var proto = ProtoGenerator.Generate(parsed);
                        Console.Write(proto);
                        return;
                    }
                    default:
                        throw new InvalidOperationException();
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
