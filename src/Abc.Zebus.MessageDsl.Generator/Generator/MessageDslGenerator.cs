using System;
using System.IO;
using Abc.Zebus.MessageDsl.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Abc.Zebus.MessageDsl.Generator
{
    [Generator]
    public class MessageDslGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var file in context.AdditionalFiles)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var fileOptions = context.AnalyzerConfigOptions.GetOptions(file);

                if (!fileOptions.TryGetValue("build_metadata.AdditionalFiles.ZebusMessageDsl", out var isMessageDslFile) || !string.Equals(isMessageDslFile, "true", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!fileOptions.TryGetValue("build_metadata.AdditionalFiles.CustomToolNamespace", out var fileNamespace))
                    fileNamespace = null;

                TranslateFile(context, file, fileNamespace);
            }
        }

        private static void TranslateFile(GeneratorExecutionContext context, AdditionalText file, string? fileNamespace)
        {
            var fileContents = file.GetText(context.CancellationToken)?.ToString();
            if (fileContents is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(MessageDslDiagnostics.CouldNotReadFileContents, Location.Create(file.Path, default, default)));
                return;
            }

            try
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                var contracts = ParsedContracts.Parse(fileContents, fileNamespace ?? string.Empty);

                if (!contracts.IsValid)
                {
                    foreach (var error in contracts.Errors)
                    {
                        var linePosition = new LinePosition(error.LineNumber, error.CharacterInLine);
                        var location = Location.Create(file.Path, default, new LinePositionSpan(linePosition, linePosition));
                        context.ReportDiagnostic(Diagnostic.Create(MessageDslDiagnostics.MessageDslError, location, error.Message));
                    }

                    return;
                }

                context.CancellationToken.ThrowIfCancellationRequested();
                var output = CSharpGenerator.Generate(contracts);

                context.AddSource(Path.GetFileName(file.Path) + ".cs", output);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.ReportDiagnostic(Diagnostic.Create(MessageDslDiagnostics.UnexpectedError, Location.None, ex.ToString()));
            }
        }
    }
}
