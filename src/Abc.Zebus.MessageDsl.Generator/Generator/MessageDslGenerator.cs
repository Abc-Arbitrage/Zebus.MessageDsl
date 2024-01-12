using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Abc.Zebus.MessageDsl.Ast;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Abc.Zebus.MessageDsl.Generator;

[Generator]
public class MessageDslGenerator : IIncrementalGenerator
{
    private static readonly Regex _sanitizePathRegex = new(@"[:\\/]+", RegexOptions.Compiled);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var input = context.AdditionalTextsProvider
                           .Where(static file => file.Path.EndsWith(".msg", StringComparison.OrdinalIgnoreCase))
                           .Combine(context.AnalyzerConfigOptionsProvider)
                           .Select(static (input, _) =>
                           {
                               var (additionalText, config) = input;
                               var defaultNamespace = config.GetOptions(additionalText).TryGetValue("build_metadata.AdditionalFiles.ZebusMessageDslNamespace", out var ns) ? ns : null;
                               var relativePath = config.GetOptions(additionalText).TryGetValue("build_metadata.AdditionalFiles.ZebusMessageDslRelativePath", out var dir) ? dir : null;
                               return new SourceGenerationInput(additionalText, defaultNamespace, relativePath);
                           });

        context.RegisterSourceOutput(input, GenerateOutput);
    }

    private static SourceGenerationResult GenerateCode(SourceGenerationInput input, CancellationToken cancellationToken)
    {
        var result = new SourceGenerationResult(input);

        if (input.InputFile.GetText(cancellationToken)?.ToString() is not { } inputText)
        {
            result.AddDiagnostic(Diagnostic.Create(MessageDslDiagnostics.CouldNotReadFileContents, Location.Create(input.InputFile.Path, default, default)));
            return result;
        }

        try
        {
            var contracts = ParsedContracts.Parse(inputText, input.DefaultNamespace?.Trim());

            foreach (var error in contracts.Errors)
            {
                var location = Location.Create(input.InputFile.Path, default, error.ToLinePositionSpan());
                result.AddDiagnostic(Diagnostic.Create(MessageDslDiagnostics.MessageDslError, location, error.Message));
            }

            var output = CSharpGenerator.Generate(contracts);
            result.GeneratedSource = output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result.AddDiagnostic(Diagnostic.Create(MessageDslDiagnostics.UnexpectedError, Location.Create(input.InputFile.Path, default, default), ex.ToString()));
        }

        return result;
    }

    private static void GenerateOutput(SourceProductionContext context, SourceGenerationInput input)
    {
        var result = GenerateCode(input, context.CancellationToken);

        foreach (var diagnostic in result.Diagnostics)
            context.ReportDiagnostic(diagnostic);

        if (result.GeneratedSource is null)
            return;

        var hintName = _sanitizePathRegex.Replace(result.RelativePath ?? result.InputFile.Path, "_") + ".g.cs";
        context.AddSource(hintName, result.GeneratedSource);
    }

    private record SourceGenerationInput(
        AdditionalText InputFile,
        string? DefaultNamespace,
        string? RelativePath
    );

    private class SourceGenerationResult(SourceGenerationInput input)
    {
        private readonly List<Diagnostic> _diagnostics = new();

        public AdditionalText InputFile { get; } = input.InputFile;
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
        public string? GeneratedSource { get; set; }
        public string? RelativePath { get; } = input.RelativePath;

        public void AddDiagnostic(Diagnostic diagnostic)
            => _diagnostics.Add(diagnostic);
    }
}
