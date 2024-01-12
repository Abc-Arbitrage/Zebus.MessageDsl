using System;
using System.IO;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace Abc.Zebus.MessageDsl.Build;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GenerateZebusMessagesTask : Task
{
    private const string _logSubcategory = "Zebus.MessageDsl";

    [Required]
    public ITaskItem[] InputFiles { get; set; } = default!;

    public override bool Execute()
    {
        foreach (var inputFile in InputFiles)
        {
            try
            {
                TranslateFile(inputFile);
            }
            catch (Exception ex)
            {
                LogError(inputFile, $"Error translating file: {ex}");
            }
        }

        return !Log.HasLoggedErrors;
    }

    private void TranslateFile(ITaskItem inputFile)
    {
        var fileContents = File.ReadAllText(inputFile.ItemSpec);
        var defaultNamespace = inputFile.GetMetadata("CustomToolNamespace")?.Trim();
        var contracts = ParsedContracts.Parse(fileContents, defaultNamespace);

        if (!contracts.IsValid)
        {
            foreach (var error in contracts.Errors)
                LogError(inputFile, error.Message, error.LineNumber, error.CharacterInLine);

            return;
        }

        GenerateCSharpOutput(inputFile, contracts);
        GenerateProtoOutput(inputFile, contracts);
    }

    private void GenerateCSharpOutput(ITaskItem inputFile, ParsedContracts contracts)
    {
        var targetPath = GetValidTargetFilePath(inputFile);

        var output = CSharpGenerator.Generate(contracts);
        File.WriteAllText(targetPath, output);

        LogDebug($"{inputFile.ItemSpec}: Translated {contracts.Messages.Count} message{(contracts.Messages.Count > 1 ? "s" : "")}");
    }

    private void GenerateProtoOutput(ITaskItem inputFile, ParsedContracts contracts)
    {
        if (!ProtoGenerator.HasProtoOutput(contracts))
            return;

        var targetPath = Path.ChangeExtension(GetValidTargetFilePath(inputFile), "proto") ?? throw new InvalidOperationException("Invalid target path");

        var output = ProtoGenerator.Generate(contracts);
        File.WriteAllText(targetPath, output);

        LogDebug($"{inputFile.ItemSpec}: Generated proto file");
    }

    private static string GetValidTargetFilePath(ITaskItem inputFile)
    {
        var targetPath = inputFile.GetMetadata("GeneratorTargetPath") ?? throw new InvalidOperationException("No target path specified");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? throw new InvalidOperationException("Invalid target directory"));
        return targetPath;
    }

    private void LogDebug(string message)
        => Log.LogMessage(_logSubcategory, null, null, null, 0, 0, 0, 0, MessageImportance.Low, message, null);

    private void LogError(ITaskItem? inputFile, string message, int lineNumber = 0, int columnNumber = 0)
        => Log.LogError(_logSubcategory, null, null, inputFile?.ItemSpec, lineNumber, columnNumber, 0, 0, message, null);
}
