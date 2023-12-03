using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public class Result<TValue>
{
    public Result(TValue? value, IReadOnlyList<Diagnostic> diagnostics)
    {
        HasValue = true;
        Value = value;
        Diagnostics = diagnostics;
    }
    public Result(IReadOnlyList<Diagnostic> diagnostics)
    {
        HasValue = false;
        Diagnostics = diagnostics;
    }

    public bool HasValue { get; }
    public TValue? Value { get; }
    
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public void ReportDiagnostics(GeneratorExecutionContext context)
    {
        foreach (var diagnostic in Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }
}