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
        var (file, fileNamespace, relativePath) = input;

        var fileContents = file.GetText(cancellationToken)?.ToString();

        if (fileContents is null)
            return SourceGenerationResult.Error(Diagnostic.Create(MessageDslDiagnostics.CouldNotReadFileContents, Location.Create(file.Path, default, default)));

        try
        {
            var contracts = ParsedContracts.Parse(fileContents, fileNamespace!.Trim());

            if (!contracts.IsValid)
            {
                var diagnostics = new List<Diagnostic>();
                foreach (var error in contracts.Errors)
                {
                    var location = Location.Create(file.Path, default, error.ToLinePositionSpan());
                    diagnostics.Add(Diagnostic.Create(MessageDslDiagnostics.MessageDslError, location, error.Message));
                }

                return SourceGenerationResult.Error(diagnostics.ToArray());
            }

            var output = CSharpGenerator.Generate(contracts);

            return SourceGenerationResult.Success(file, output, relativePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SourceGenerationResult.Error(Diagnostic.Create(MessageDslDiagnostics.UnexpectedError, Location.None, ex.ToString()));
        }
    }

    private static void GenerateFiles(SourceProductionContext context, ImmutableArray<SourceGenerationResult> results)
    {
        foreach (var result in results)
        {
            foreach (var diagnostic in result.Diagnostics)
                context.ReportDiagnostic(diagnostic);

            if (result.AdditionalText == null || result.GeneratedSource == null)
                continue;

            var hintName = _sanitizePathRegex.Replace(result.RelativePath ?? result.AdditionalText.Path, "_") + ".g.cs";

            context.AddSource(hintName, result.GeneratedSource);
        }
    }

    private record SourceGenerationInput(AdditionalText AdditionalText, string? FileNamespace, string? RelativePath);

    public class SourceGenerationResult
    {
        public IList<Diagnostic> Diagnostics { get; }
        public string? GeneratedSource { get; set; }
        public AdditionalText? AdditionalText { get; }
        public string? RelativePath { get; }

        private SourceGenerationResult(IList<Diagnostic> diagnostics, string? generatedSource, AdditionalText? additionalText, string? relativePath)
        {
            Diagnostics = diagnostics;
            GeneratedSource = generatedSource;
            AdditionalText = additionalText;
            RelativePath = relativePath;
        }

        public static SourceGenerationResult Error(params Diagnostic[] diagnostics)
            => new(diagnostics, null, null, null);

        public static SourceGenerationResult Success(AdditionalText? additionalText, string generatedSource, string? relativePath)
            => new(Array.Empty<Diagnostic>(), generatedSource, additionalText, relativePath);
    }
}
