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
            foreach (var file in context.AdditionalFiles)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
