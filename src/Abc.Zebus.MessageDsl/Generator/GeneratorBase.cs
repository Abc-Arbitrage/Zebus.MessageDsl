using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using Abc.Zebus.MessageDsl.Support;

namespace Abc.Zebus.MessageDsl.Generator
{
    public abstract class GeneratorBase : IDisposable
    {
        protected static readonly string GeneratorName = typeof(GeneratorBase).Assembly.GetName().Name!;
        protected static readonly Version GeneratorVersion = typeof(GeneratorBase).Assembly.GetName().Version!;

        private readonly StringBuilder _stringBuilder;
        protected readonly IndentedTextWriter Writer;

        protected GeneratorBase()
        {
            _stringBuilder = new StringBuilder();
            Writer = new IndentedTextWriter(new StringWriter(_stringBuilder));
        }

        protected void Reset()
        {
            _stringBuilder.Clear();
            Writer.Indent = 0;
        }

        protected IDisposable Indent()
        {
            ++Writer.Indent;
            return Disposable.Create(() => --Writer.Indent);
        }

        protected IDisposable Block()
        {
            Writer.WriteLine("{");
            ++Writer.Indent;
            return Disposable.Create(() =>
            {
                --Writer.Indent;
                Writer.WriteLine("}");
            });
        }

        protected string GeneratedOutput() => _stringBuilder.ToString();

        protected static string ParameterCase(string s) => char.ToLowerInvariant(s[0]) + s.Substring(1);
        protected static string MemberCase(string s) => char.ToUpperInvariant(s[0]) + s.Substring(1);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose()
        {
            Writer.Dispose();
        }
    }
}
