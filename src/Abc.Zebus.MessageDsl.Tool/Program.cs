﻿using System;
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
        var path = new Argument<string?>(".msg file", "The .msg file to process.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var defaultNamespace = new Option<string?>(name: "--namespace", description: "The default namespace to use for the generated files", getDefaultValue: () => null);
        var outputType = new Option<Format>(name: "--format", description: "The output format to generate (csharp or proto)", getDefaultValue: () => Format.Proto);

        mainCommand.AddArgument(path);
        mainCommand.AddOption(defaultNamespace);
        mainCommand.AddOption(outputType);

        mainCommand.SetHandler(context =>
            {
                var parse = context.ParseResult;
                var parsedPath = parse.GetValueForArgument(path);
                var parsedNamespace = parse.GetValueForOption(defaultNamespace);
                var parsedFormat = parse.GetValueForOption(outputType);

                string? txt = null;

                try
                {
                    if (parsedPath != null)
                        txt = File.ReadAllText(parsedPath);
                }
                catch (FileNotFoundException)
                {
                    errorOutput.WriteLine($"File {parsedPath} does not exist.");
                    context.ExitCode = 1;
                    return;
                }
                catch (Exception ex)
                {
                    errorOutput.WriteLine($"Error reading file {parsedPath}: {ex.Message}");
                    context.ExitCode = 1;
                    return;
                }

                txt ??= input.ReadToEnd();
                var parsed = ParsedContracts.Parse(txt, parsedNamespace);

                foreach (var error in parsed.Errors)
                    errorOutput.WriteLine(error);

                if (parsed.Errors.Count != 0)
                {
                    context.ExitCode = 1;
                    return;
                }

                foreach (var message in parsed.Messages)
                    message.Options.Proto = true;

                switch (parsedFormat)
                {
                    case Format.CSharp:
                    {
                        var cs = CSharpGenerator.Generate(parsed);
                        output.Write(cs);
                        return;
                    }

                    case Format.Proto:
                    {
                        var proto = ProtoGenerator.Generate(parsed);
                        output.Write(proto);
                        return;
                    }

                    default:
                        throw new InvalidOperationException();
                }
            }
        );

        return mainCommand.Invoke(args);
    }
}

internal enum Format
{
    Proto,
    CSharp
}
