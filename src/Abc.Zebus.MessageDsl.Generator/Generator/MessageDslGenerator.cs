using System;
using System.Collections.Generic;
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
            var generatedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in context.AdditionalFiles)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var fileOptions = context.AnalyzerConfigOptions.GetOptions(file);

                if (!fileOptions.TryGetValue("build_metadata.AdditionalFiles.ZebusMessageDsl", out var isMessageDslFile) || !string.Equals(isMessageDslFile, "true", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!fileOptions.TryGetValue("build_metadata.AdditionalFiles.CustomToolNamespace", out var fileNamespace))
                    fileNamespace = string.Empty;

                TranslateFile(context, file, fileNamespace, generatedFileNames);
            }
        }

        private static void TranslateFile(GeneratorExecutionContext context, AdditionalText file, string fileNamespace, HashSet<string> generatedFileNames)
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
                var contracts = ParsedContracts.Parse(fileContents, fileNamespace);

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

                context.AddSource(GetHintName(file, generatedFileNames), output);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.ReportDiagnostic(Diagnostic.Create(MessageDslDiagnostics.UnexpectedError, Location.None, ex.ToString()));
            }
        }

        private static string GetHintName(AdditionalText file, HashSet<string> generatedFileNames)
        {
            var baseName = Path.GetFileName(file.Path);

            var fileName = $"{baseName}.cs";
            if (generatedFileNames.Add(fileName))
                return fileName;

            for (var index = 1;; ++index)
            {
                fileName = $"{baseName}.{index:D3}.cs";
                if (generatedFileNames.Add(fileName))
                    return fileName;
            }
        }
    }
}
