using System;
using System.Collections.Generic;
using System.IO;
using Abc.Zebus.MessageDsl.Ast;
using Microsoft.CodeAnalysis;

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

#if RIDER_FALLBACK
                // Rider does not provide build metadata to the generator, so this handles the simplest case.
                // Revert when https://youtrack.jetbrains.com/issue/RIDER-55242 is fixed.

                if (!file.Path.EndsWith(".msg", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileNamespace = context.Compilation.AssemblyName ?? string.Empty;

                if (!string.IsNullOrEmpty(fileNamespace))
                {
                    var dirs = Path.GetDirectoryName(file.Path)?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (dirs != null)
                    {
                        for (var asmIdx = dirs.Length - 1; asmIdx >= 0; --asmIdx)
                        {
                            if (string.Equals(dirs[asmIdx], context.Compilation.AssemblyName, StringComparison.OrdinalIgnoreCase))
                            {
                                for (var dirIdx = asmIdx + 1; dirIdx < dirs.Length; ++dirIdx)
                                    fileNamespace += "." + dirs[dirIdx];

                                break;
                            }
                        }
                    }
                }
#else
                var fileOptions = context.AnalyzerConfigOptions.GetOptions(file);
                if (!fileOptions.TryGetValue("build_metadata.AdditionalFiles.ZebusMessageDslNamespace", out var fileNamespace))
                    continue;
#endif

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
                        var location = Location.Create(file.Path, default, error.ToLinePositionSpan());
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
