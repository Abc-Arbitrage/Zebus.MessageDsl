using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Abc.Zebus.MessageDsl.Support
{
    internal static class Extensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue result;
            return dictionary.TryGetValue(key, out result) ? result : default(TValue);
        }

        public static string GetFullText(this ParserRuleContext context)
        {
            if (context.Start == null || context.Stop == null || context.Start.StartIndex < 0 || context.Stop.StopIndex < 0)
                return context.GetText();

            return context.Start.InputStream.GetText(Interval.Of(context.Start.StartIndex, context.Stop.StopIndex));
        }

        public static string GetFullTextUntil(this IToken startToken, IToken endToken)
        {
            if (startToken == null || endToken == null || startToken.StartIndex < 0 || endToken.StopIndex < 0 || startToken.StartIndex > endToken.StartIndex || startToken.InputStream != endToken.InputStream)
                return string.Empty;

            return startToken.InputStream.GetText(Interval.Of(startToken.StartIndex, endToken.StopIndex));
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> sequence) => new HashSet<T>(sequence);

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> toAdd)
        {
            foreach (var item in toAdd)
                collection.Add(item);
        }
    }
}
