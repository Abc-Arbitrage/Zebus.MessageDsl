using System;
using System.IO;
using System.Linq;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Abc.Zebus.MessageDsl.Build
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class GenerateZebusMessagesTask : Task
    {
        private const string _logSubcategory = "Zebus.MessageDsl";

        [Required]
        public ITaskItem[] InputFiles { get; set; }

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
            var contracts = ParsedContracts.Parse(fileContents, inputFile.GetMetadata("CustomToolNamespace") ?? string.Empty);

            if (!contracts.IsValid)
            {
                foreach (var error in contracts.Errors)
                    LogError(inputFile, error.Message, error.LineNumber, error.CharacterInLine);

                return;
            }

            var targetPath = inputFile.GetMetadata("GeneratorTargetPath") ?? throw new InvalidOperationException("No target path specified");
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? throw new InvalidOperationException("Invalid target directory"));

            var output = CSharpGenerator.Generate(contracts);
            File.WriteAllText(targetPath, output);

            LogDebug($"{inputFile.ItemSpec}: Translated {contracts.Messages.Count} message{(contracts.Messages.Count > 1 ? "s" : "")}");

            if (ProtoGenerator.HasProtoOutput(contracts))
            {
                var protoFileName = Path.ChangeExtension(targetPath, "proto");
                var protoText = ProtoGenerator.Generate(contracts);
                File.WriteAllText(protoFileName, protoText);

                LogDebug($"{inputFile.ItemSpec}: Generated proto file");
            }
        }

        private void LogDebug(string message)
            => Log.LogMessage(_logSubcategory, null, null, null, 0, 0, 0, 0, MessageImportance.Low, message, null);

        private void LogError(ITaskItem inputFile, string message, int lineNumber = 0, int columnNumber = 0)
            => Log.LogError(_logSubcategory, null, null, inputFile?.ItemSpec, lineNumber, columnNumber, 0, 0, message, null);
    }
}
