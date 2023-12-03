using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Tests.Utilities;

public class HostResult
{
    public Compilation OutputCompilation { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public GeneratorRunResult GeneratorResult { get; }

    public HostResult(Compilation outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorRunResult generatorResult)
    {
        OutputCompilation = outputCompilation;
        Diagnostics = diagnostics;
        GeneratorResult = generatorResult;
    }

    public GeneratedSourceResult GetGeneratedSource(string hintName) => GeneratorResult.GeneratedSources.Single(x => x.HintName == hintName);
}