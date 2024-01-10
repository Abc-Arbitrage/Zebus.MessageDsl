using Antlr4.Runtime;

namespace Abc.Zebus.MessageDsl.Dsl;

internal class MessageContractsErrorStrategy : DefaultErrorStrategy
{
    public override void Sync(Parser recognizer)
    {
        if (InErrorRecoveryMode(recognizer))
            return;

        base.Sync(recognizer);

        if (InErrorRecoveryMode(recognizer) && IsInDefinitionRule(recognizer))
            throw new GoToNextDefinitionException(recognizer);
    }

    public override void ReportError(Parser recognizer, RecognitionException e)
    {
        if (e is GoToNextDefinitionException)
            return;

        base.ReportError(recognizer, e);

        if (IsInDefinitionRule(recognizer))
            throw new GoToNextDefinitionException(recognizer);
    }

    public override void Recover(Parser recognizer, RecognitionException e)
    {
        if (e is GoToNextDefinitionException)
        {
            if (recognizer.Context.RuleIndex != MessageContractsParser.RULE_definition)
                throw e;

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

    private class GoToNextDefinitionException(Parser recognizer) : RecognitionException(recognizer, recognizer.InputStream, recognizer.Context);
}
