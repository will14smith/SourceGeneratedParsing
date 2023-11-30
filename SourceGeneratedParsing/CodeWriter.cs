using System.Text;

namespace SourceGeneratedParsing;

public class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    public override string ToString() => _sb.ToString();

    public IDisposable AppendBlock(string header)
    {
        AppendLine(header);
        AppendLine("{");
        _indent++;
        
        return new BlockDisposable(this);
    }

    public void AppendLine() => _sb.AppendLine();
    public void AppendLine(string text) => _sb.Append(new string('\t', _indent)).AppendLine(text);

    private class BlockDisposable : IDisposable
    {
        private readonly CodeWriter _writer;
        public BlockDisposable(CodeWriter writer) => _writer = writer;

        public void Dispose()
        {
            _writer._indent--;
            _writer.AppendLine("}");
        }
    }
    
}