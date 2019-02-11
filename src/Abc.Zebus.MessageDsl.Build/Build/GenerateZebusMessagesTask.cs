using System;
using System.Collections.Generic;
using System.IO;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Abc.Zebus.MessageDsl.Build
{
    public class GenerateZebusMessagesTask : Task
    {
        private bool _hasErrors;

        [Required]
        public ICollection<ITaskItem> InputFiles { get; set; }

        public override bool Execute()
        {
            foreach (var inputFile in InputFiles)
                TranslateFile(inputFile);

            return !_hasErrors;
        }

        private void TranslateFile(ITaskItem inputFile)
        {
            try
            {
                var fileContents = File.ReadAllText(inputFile.ItemSpec);
                var contracts = ParsedContracts.Parse(fileContents, inputFile.GetMetadata("CustomToolNamespace") ?? string.Empty);

                var targetPath = inputFile.GetMetadata("GeneratorTargetPath") ?? throw new InvalidOperationException("No target path specified");

                if (!contracts.IsValid)
                {
                    foreach (var error in contracts.Errors)
                        Log.LogError("Zebus.MessageDsl", null, null, inputFile.ItemSpec, error.LineNumber, error.CharacterInLine, 0, 0, error.Message);

                    return;
                }

                var output = CSharpGenerator.Generate(contracts);
                File.WriteAllText(targetPath, output);
            }
            catch (Exception ex)
            {
                Log.LogError($"Error translating file {inputFile.ItemSpec}: {ex.Message}");
                _hasErrors = true;
            }
        }
    }
}
