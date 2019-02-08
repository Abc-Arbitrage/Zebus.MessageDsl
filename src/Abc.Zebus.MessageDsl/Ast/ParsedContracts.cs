using System.Collections.Generic;
using Abc.Zebus.MessageDsl.Analysis;
using Abc.Zebus.MessageDsl.Dsl;
using Antlr4.Runtime;
using JetBrains.Annotations;

namespace Abc.Zebus.MessageDsl.Ast
{
    public class ParsedContracts
    {
        public IList<MessageDefinition> Messages { get; } = new List<MessageDefinition>();
        public IList<EnumDefinition> Enums { get; } = new List<EnumDefinition>();
        public ContractOptions Options { get; } = new ContractOptions();
        public ICollection<SyntaxError> Errors { get; private set; } = new List<SyntaxError>();
        public string Namespace { get; private set; }
        public ICollection<string> ImportedNamespaces { get; } = new HashSet<string>();

        public CommonTokenStream TokenStream { get; private set; }
        public MessageContractsParser.CompileUnitContext ParseTree { get; private set; }

        public bool IsValid => Errors.Count == 0;

        internal ParsedContracts()
        {
        }

        public static ParsedContracts CreateParseTree(string definitionText)
        {
            var result = new ParsedContracts();

            var errorListener = new CollectingErrorListener();

            var input = new AntlrInputStream(definitionText);

            var lexer = new MessageContractsLexer(input);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(errorListener);

            result.TokenStream = new CommonTokenStream(lexer);

            var parser = new MessageContractsParser(result.TokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            result.ParseTree = parser.compileUnit();
            result.Errors = errorListener.Errors;

            return result;
        }

        public static ParsedContracts Parse(string definitionText, string messagesNamespace)
        {
            var result = CreateParseTree(definitionText);
            result.Namespace = messagesNamespace;

            if (result.Errors.Count == 0)
            {
                new AstCreationVisitor(result).VisitCompileUnit(result.ParseTree);
                result.Process();
            }

            return result;
        }

        internal void Process()
        {
            var processor = new AstProcessor(this);

            processor.PreProcess();
            new AttributeInterpretor(this).InterpretAttributes();
            new ContractsEnhancer(this).Process();
            processor.PostProcess();

            new AstValidator(this).Validate();
        }

        public void AddError(string message)
            => Errors.Add(new SyntaxError(message));

        public void AddError(IToken token, string message)
            => Errors.Add(new SyntaxError(message, token));

        public void AddError(ParserRuleContext context, string message)
            => AddError(context?.Start, message);

        [StringFormatMethod("format")]
        public void AddError(ParserRuleContext context, string format, params object[] args)
            => AddError(context, string.Format(format, args));

        [StringFormatMethod("format")]
        public void AddError(IToken token, string format, params object[] args)
            => AddError(token, string.Format(format, args));

        // ReSharper disable once RedundantStringFormatCall

        [StringFormatMethod("format")]
        public void AddError(string format, params object[] args)
            => AddError(string.Format(format, args));
    }
}
