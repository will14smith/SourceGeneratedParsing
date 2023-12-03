using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceGeneratedParsing.Tests.Utilities;

public static class Host
{
    public static HostResult Run(string source)
    {
        var inputCompilation = CreateCompilation(source);
        
        var generator = new ParserSourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        Assert.Empty(outputCompilation.GetDeclarationDiagnostics());
        
        var runResult = driver.GetRunResult();
        var generatorResult = runResult.Results[0];
        
        return new HostResult(outputCompilation, diagnostics, generatorResult);
    }
    
    private static Compilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source, null, "Program.cs") },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Regex).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}