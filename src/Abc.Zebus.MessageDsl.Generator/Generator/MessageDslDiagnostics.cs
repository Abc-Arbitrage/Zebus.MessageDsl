using Microsoft.CodeAnalysis;

#nullable enable

namespace Abc.Zebus.MessageDsl.Generator
{
    internal static class MessageDslDiagnostics
    {
        public static DiagnosticDescriptor MessageDslError { get; } = Error(1, "MessageDsl error", "{0}");
        public static DiagnosticDescriptor UnexpectedError { get; } = Error(2, "Unexpected error", "Unexpected error: {0}");
        public static DiagnosticDescriptor CouldNotReadFileContents { get; } = Error(3, "Could not read file contents", "Could not read file contents");

        private static DiagnosticDescriptor Error(int index, string title, string messageFormat)
            => new($"MessageDsl{index:D3}", title, messageFormat, "MessageDsl", DiagnosticSeverity.Error, true);
    }
}
