using Microsoft.CodeAnalysis;
using SourceGeneratedParsing.Tests.Utilities;

namespace SourceGeneratedParsing.Tests;

public class InitializationTests
{
    [Fact]
    public void AlwaysGeneratesAttributes()
    {
        var source = @"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
}
";

        var result = Host.Run(source); 

        Assert.True(result.Diagnostics.IsEmpty);
        
        var attributesSource = result.GetGeneratedSource("Attributes.cs");
        var classes = attributesSource.GetClasses();
        
        Assert.Contains(classes, c => c.Identifier.ValueText == "LexerAttribute");
        Assert.Contains(classes, c => c.Identifier.ValueText == "TokenAttribute");
        Assert.Contains(classes, c => c.Identifier.ValueText == "ParserAttribute");
        Assert.Contains(classes, c => c.Identifier.ValueText == "ProductionAttribute");
    }
}