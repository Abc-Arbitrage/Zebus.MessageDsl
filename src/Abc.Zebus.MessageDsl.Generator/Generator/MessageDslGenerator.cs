using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        var additionalTextsWithNamespaces = context.AdditionalTextsProvider
                                                   .Where(x => x.Path.EndsWith(".msg"))
                                                   .Combine(context.AnalyzerConfigOptionsProvider)
                                                   .Select((input, _) =>
                                                   {
                                                       var (additionalText, config) = input;
                                                       var fileNamespace = config.GetOptions(additionalText).TryGetValue("build_metadata.AdditionalFiles.ZebusMessageDslNamespace", out var ns) ? ns : null;
                                                       var relativePath = config.GetOptions(additionalText).TryGetValue("build_metadata.AdditionalFiles.ZebusMessageDslRelativePath", out var dir) ? dir : null;
                                                       return new SourceGenerationInput(additionalText, fileNamespace, relativePath);
                                                   })
                                                   .Where(x => x.FileNamespace != null)
                                                   .Select(GenerateCode)
                                                   .Collect();

        context.RegisterSourceOutput(additionalTextsWithNamespaces, GenerateFiles);
    }

    private static SourceGenerationResult GenerateCode(SourceGenerationInput input, CancellationToken cancellationToken)
    {
        var result = new SourceGenerationResult(input);

        var fileContents = input.AdditionalText.GetText(cancellationToken)?.ToString();

        if (fileContents is null)
        {
            result.AddDiagnostic(Diagnostic.Create(MessageDslDiagnostics.CouldNotReadFileContents, Location.Create(input.AdditionalText.Path, default, default)));
            return result;
        }

        try
        {
            var contracts = ParsedContracts.Parse(fileContents, input.FileNamespace?.Trim());

            foreach (var error in contracts.Errors)
            {
                var location = Location.Create(input.AdditionalText.Path, default, error.ToLinePositionSpan());
                result.AddDiagnostic(Diagnostic.Create(MessageDslDiagnostics.MessageDslError, location, error.Message));
            }

            var output = CSharpGenerator.Generate(contracts);
            result.GeneratedSource = output;

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result.AddDiagnostic(Diagnostic.Create(MessageDslDiagnostics.UnexpectedError, Location.None, ex.ToString()));
            return result;
        }
    }

    private static void GenerateFiles(SourceProductionContext context, ImmutableArray<SourceGenerationResult> results)
    {
        foreach (var result in results)
        {
            foreach (var diagnostic in result.Diagnostics)
                context.ReportDiagnostic(diagnostic);

            if (result.GeneratedSource == null)
                continue;

            var hintName = _sanitizePathRegex.Replace(result.RelativePath ?? result.AdditionalText.Path, "_") + ".g.cs";
            context.AddSource(hintName, result.GeneratedSource);
        }
    }

    private record SourceGenerationInput(AdditionalText AdditionalText, string? FileNamespace, string? RelativePath);

    private class SourceGenerationResult(SourceGenerationInput input)
    {
        private readonly List<Diagnostic> _diagnostics = new();

        public AdditionalText AdditionalText { get; } = input.AdditionalText;
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
        public string? GeneratedSource { get; set; }
        public string? RelativePath { get; } = input.RelativePath;

        public void AddDiagnostic(Diagnostic diagnostic)
            => _diagnostics.Add(diagnostic);
    }
}
