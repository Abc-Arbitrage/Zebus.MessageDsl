using Antlr4.Runtime;

namespace Abc.Zebus.MessageDsl.Dsl;

internal class MessageContractsErrorStrategy : DefaultErrorStrategy
{
    private MarkAndIndex? _startOfCurrentDefinition;

    public override void Sync(Parser recognizer)
    {
        if (InErrorRecoveryMode(recognizer))
            return;

        if (recognizer.Context.RuleIndex == MessageContractsParser.RULE_definition)
        {
            if (_startOfCurrentDefinition is { } mark)
                recognizer.InputStream.Release(mark.Mark);

            _startOfCurrentDefinition = new MarkAndIndex(recognizer.InputStream.Mark(), recognizer.InputStream.Index);
        }

        base.Sync(recognizer);

        if (InErrorRecoveryMode(recognizer))
            GoToNextDefinition(recognizer);
    }

    public override void ReportError(Parser recognizer, RecognitionException e)
    {
        if (e is GoToNextDefinitionException)
            return;

        base.ReportError(recognizer, e);
        GoToNextDefinition(recognizer);
    }

    public override void Recover(Parser recognizer, RecognitionException e)
    {
        if (e is GoToNextDefinitionException)
        {
            // Unwind the stack to the end of the current definition rule
            if (recognizer.Context.RuleIndex is not (MessageContractsParser.RULE_definition or MessageContractsParser.RULE_compileUnit))
                throw e;

            // Rewind the input to the start of the definition, then skip a single token
            if (_startOfCurrentDefinition is { } mark)
            {
                _startOfCurrentDefinition = null;
                recognizer.InputStream.Seek(mark.Index);
                recognizer.Consume();
                recognizer.InputStream.Release(mark.Mark);
            }

            while (true)
            {
                // Consume tokens until we reach a possible separator between definitions
                if (recognizer.InputStream.La(1) is MessageContractsParser.SEP or MessageContractsParser.Eof
                    || ((MessageContractsParser)recognizer).IsAtImplicitSeparator())
                {
                    EndErrorCondition(recognizer);
                    return;
                }

                recognizer.Consume();
            }
        }

        base.Recover(recognizer, e);
    }

    private static void GoToNextDefinition(Parser recognizer)
    {
        if (IsInDefinitionRule(recognizer))
            throw new GoToNextDefinitionException(recognizer);
    }

    private static bool IsInDefinitionRule(Parser recognizer)
    {
        RuleContext ctx = recognizer.Context;

        while (ctx != null)
        {
            if (ctx.RuleIndex == MessageContractsParser.RULE_definition)
                return true;

            ctx = ctx.Parent;
        }

        return false;
    }

    private sealed class GoToNextDefinitionException(Parser recognizer)
        : RecognitionException(recognizer, recognizer.InputStream, recognizer.Context);

    private readonly record struct MarkAndIndex(int Mark, int Index);
}
