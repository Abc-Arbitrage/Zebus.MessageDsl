using System;
using System.Collections.Generic;
using Abc.Zebus.MessageDsl.Analysis;
using Abc.Zebus.MessageDsl.Dsl;
using Antlr4.Runtime;
using JetBrains.Annotations;

namespace Abc.Zebus.MessageDsl.Ast;

public class ParsedContracts
{
    public IList<MessageDefinition> Messages { get; } = new List<MessageDefinition>();
    public IList<EnumDefinition> Enums { get; } = new List<EnumDefinition>();
    public ContractOptions Options { get; } = new();
    public ICollection<SyntaxError> Errors { get; }
    public string? Namespace { get; set; }
    public bool ExplicitNamespace { get; internal set; }
    public ICollection<string> ImportedNamespaces { get; } = new HashSet<string>();

    public CommonTokenStream TokenStream { get; }
    public MessageContractsParser.CompileUnitContext ParseTree { get; }

    public bool IsValid => Errors.Count == 0;

    internal ParsedContracts()
    {
        // For unit tests

        TokenStream = default!;
        ParseTree = default!;
        Errors = new List<SyntaxError>();
    }

    private ParsedContracts(CommonTokenStream tokenStream, MessageContractsParser.CompileUnitContext parseTree, ICollection<SyntaxError> errors)
    {
        TokenStream = tokenStream;
        ParseTree = parseTree;
        Errors = errors;
    }

    public static ParsedContracts CreateParseTree(string definitionText)
    {
        var errorListener = new CollectingErrorListener();

        var input = new AntlrInputStream(definitionText);

        var lexer = new MessageContractsLexer(input);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errorListener);

        var tokenStream = new CommonTokenStream(lexer);

        var parser = new MessageContractsParser(tokenStream)
        {
            ErrorHandler = new MessageContractsErrorStrategy()
        };

        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);

        var parseTree = parser.compileUnit();

        return new ParsedContracts(tokenStream, parseTree, errorListener.Errors);
    }

    public static ParsedContracts Parse(string definitionText, string? defaultNamespace)
    {
        var result = CreateParseTree(definitionText);

        if (!result.ExplicitNamespace)
            result.Namespace = defaultNamespace;

        new AstCreationVisitor(result).VisitCompileUnit(result.ParseTree);
        result.Process();

        return result;
    }

    internal void Process()
    {
        var processor = new AstProcessor(this);

        processor.PreProcess();
        new AttributeInterpreter(this).InterpretAttributes();
        new ContractsEnhancer(this).Process();
        processor.PostProcess();

        new AstValidator(this).Validate();
        processor.Cleanup();
    }

    public void AddError(string message)
        => Errors.Add(new SyntaxError(message));

    public void AddError(IToken? token, string message)
        => Errors.Add(new SyntaxError(message, token));

    public void AddError(ParserRuleContext? context, string message)
        => AddError(context?.Start, message);

    [StringFormatMethod("format"), Obsolete("Use a string interpolation")]
    public void AddError(ParserRuleContext? context, string format, params object?[] args)
        => AddError(context, string.Format(format, args));

    [StringFormatMethod("format"), Obsolete("Use a string interpolation")]
    public void AddError(IToken? token, string format, params object?[] args)
        => AddError(token, string.Format(format, args));

    [StringFormatMethod("format"), Obsolete("Use a string interpolation")]
    public void AddError(string format, params object?[] args)
        => AddError(string.Format(format, args));
}
