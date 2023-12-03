namespace SourceGeneratedParsing.ParserSource;

public class ParserSourceOutput : IDisposable
{
    public IDisposable Postfix { get; }
    public IReadOnlyList<string> Outputs { get; }

    public ParserSourceOutput(IDisposable postfix, IReadOnlyList<string> outputs)
    {
        Postfix = postfix;
        Outputs = outputs;
    }

    public void Dispose() => Postfix.Dispose();
}