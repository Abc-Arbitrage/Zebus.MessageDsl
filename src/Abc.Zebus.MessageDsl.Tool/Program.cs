using System;
using System.CommandLine;
using System.IO;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;

namespace Abc.Zebus.MessageDsl.Tool;

public static class Program
{
    public static int Main(string[] args)
        => Run(args, Console.In, Console.Out, Console.Error);

    public static int Run(string[] args, TextReader input, TextWriter output, TextWriter errorOutput)
    {
        var mainCommand = new RootCommand();

        var path = new Argument<string?>(".msg file")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The .msg file to process."
        };

        var defaultNamespace = new Option<string?>("--namespace")
        {
            Description = "The default namespace to use for the generated files."
        };

        var outputType = new Option<Format>("--format")
        {
            DefaultValueFactory = _ => Format.Proto,
            Description = "The output format to generate (csharp or proto)."
        };

        mainCommand.Add(path);
        mainCommand.Add(defaultNamespace);
        mainCommand.Add(outputType);

        mainCommand.SetAction(parseResult =>
        {
            var parsedPath = parseResult.GetValue(path);
            var parsedNamespace = parseResult.GetValue(defaultNamespace);
            var parsedFormat = parseResult.GetValue(outputType);

            string? txt = null;

            try
            {
                if (parsedPath != null)
                    txt = File.ReadAllText(parsedPath);
            }
            catch (FileNotFoundException)
            {
                errorOutput.WriteLine($"File {parsedPath} does not exist.");
                return 1;
            }
            catch (Exception ex)
            {
                errorOutput.WriteLine($"Error reading file {parsedPath}: {ex.Message}");
                return 1;
            }

            txt ??= input.ReadToEnd();
            var parsed = ParsedContracts.Parse(txt, parsedNamespace);

            foreach (var error in parsed.Errors)
                errorOutput.WriteLine(error);

            if (parsed.Errors.Count != 0)
                return 1;

            foreach (var message in parsed.Messages)
                message.Options.Proto = true;

            switch (parsedFormat)
            {
                case Format.CSharp:
                {
                    var cs = CSharpGenerator.Generate(parsed);
                    output.Write(cs);
                    return 0;
                }

                case Format.Proto:
                {
                    var proto = ProtoGenerator.Generate(parsed);
                    output.Write(proto);
                    return 0;
                }

                default:
                    throw new InvalidOperationException();
            }
        });

        var parseResult = mainCommand.Parse(args);

        if (parseResult.Errors.Count != 0)
        {
            foreach (var parseError in parseResult.Errors)
                errorOutput.WriteLine(parseError.Message);

            return 1;
        }

        return parseResult.Invoke();
    }
}

internal enum Format
{
    Proto,
    CSharp
}
