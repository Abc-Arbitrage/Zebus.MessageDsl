using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using Abc.Zebus.MessageDsl.Support;

namespace Abc.Zebus.MessageDsl.Generator
{
    public abstract class GeneratorBase : IDisposable
    {
        protected static string GeneratorName { get; } = typeof(GeneratorBase).Assembly.GetName().Name!;
        protected static Version GeneratorVersion { get; }= typeof(GeneratorBase).Assembly.GetName().Version!;

        private readonly StringBuilder _stringBuilder;
        protected IndentedTextWriter Writer { get; }

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

        protected ListHelper List(string separator = ", ")
            => new(Writer, separator);

        protected string GeneratedOutput() => _stringBuilder.ToString();

        protected static string ParameterCase(string s) => char.ToLowerInvariant(s[0]) + s.Substring(1);
        protected static string MemberCase(string s) => char.ToUpperInvariant(s[0]) + s.Substring(1);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose()
        {
            Writer.Dispose();
        }

        protected struct ListHelper
        {
            private readonly IndentedTextWriter _writer;
            private readonly string _separator;
            private bool _firstItem;

            public ListHelper(IndentedTextWriter writer, string separator)
            {
                _writer = writer;
                _separator = separator;
                _firstItem = true;
            }

            public void NextItem()
            {
                if (_firstItem)
                    _firstItem = false;
                else
                    _writer.Write(_separator);
            }

            public void Reset()
                => _firstItem = true;
        }
    }
}
