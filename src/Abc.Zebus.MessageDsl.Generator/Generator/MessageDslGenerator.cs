using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Abc.Zebus.MessageDsl.Ast;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Abc.Zebus.MessageDsl.Generator
{
    [Generator]
    public class MessageDslGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var additionalTextsWithNamespaces = context.AdditionalTextsProvider
                                                       .Where(x => x.Path.EndsWith(".msg"))
                                                       .Combine(context.AnalyzerConfigOptionsProvider)
                                                       .Select((x, _) =>
                                                       {
                                                           var (additionalText, config) = x;
                                                           var optionName = "build_metadata.AdditionalFiles.ZebusMessageDslNamespace";
                                                           var fileNamespace = config.GetOptions(additionalText).TryGetValue(optionName, out var ns) ? ns : null;
                                                           return (additionalText, fileNamespace);
                                                       })
                                                       .Where(x => x.fileNamespace != null)
                                                       .Select(GenerateCode)
                                                       .Collect();

            context.RegisterSourceOutput(additionalTextsWithNamespaces, GenerateFiles);
        }

        private static SourceGenerationResult GenerateCode((AdditionalText additionalText, string? fileNamespace) additionalTextWithNamespace, CancellationToken cancellationToken)
        {
            var file = additionalTextWithNamespace.additionalText;
            var fileNamespace = additionalTextWithNamespace.fileNamespace;

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

                return SourceGenerationResult.Success(file, output);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return SourceGenerationResult.Error(Diagnostic.Create(MessageDslDiagnostics.UnexpectedError, Location.None, ex.ToString()));
            }
        }

        private static void GenerateFiles(SourceProductionContext context, ImmutableArray<SourceGenerationResult> results)
        {
            var hintNames = new HashSet<string>();

            foreach (var result in results)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }

                if (result.AdditionalText == null || result.GeneratedSource == null)
                    continue;

                var hintName = GenerateHintName(result.AdditionalText);

                context.AddSource(hintName, result.GeneratedSource);
            }

            string GenerateHintName(AdditionalText file)
            {
                var baseName = Path.GetFileName(file.Path);

                var fileName = $"{baseName}.cs";
                if (hintNames.Add(fileName))
                    return fileName;

                for (var index = 1;; ++index)
                {
                    fileName = $"{baseName}.{index:D3}.cs";
                    if (hintNames.Add(fileName))
                        return fileName;
                }
            }
        }

        public class SourceGenerationResult
        {
            public IList<Diagnostic> Diagnostics { get; }
            public string? GeneratedSource { get; set; }
            public AdditionalText? AdditionalText { get; }

            public SourceGenerationResult(IList<Diagnostic> diagnostics, string? generatedSource, AdditionalText? additionalText)
            {
                Diagnostics = diagnostics;
                GeneratedSource = generatedSource;
                AdditionalText = additionalText;
            }

            public static SourceGenerationResult Error(params Diagnostic[] diagnostics) => new(diagnostics, null, null);
            public static SourceGenerationResult Success(AdditionalText? additionalText, string generatedSource) => new(Array.Empty<Diagnostic>(), generatedSource, additionalText);
        }
    }
}
