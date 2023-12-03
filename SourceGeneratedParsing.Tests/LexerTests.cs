using SourceGeneratedParsing.Tests.Utilities;

namespace SourceGeneratedParsing.Tests;

public class LexerTests
{
    [Fact]
    public void GeneratesLexer()
    {
        var source = @"
using SourceGeneratedParsing; 

namespace MyCode
{
    [Lexer]
    public enum TokenType
    {
        [Token(""[0-9]+"")]
        Number,
        [Token(""[a-z]+"")]
        Word,
        [Token(""[ ]+"", true)]
        Whitespace,
    }

    [Parser(typeof(TokenType))]
    public partial class Parser { }
}
";

        var result = Host.Run(source); 

        Assert.True(result.Diagnostics.IsEmpty);
        
        var attributesSource = result.GetGeneratedSource("MyCode.Parser.Lexer.cs");
        var classes = attributesSource.GetStructs();

        Assert.Contains(classes, c => c.Identifier.ValueText == "Lexer");
    }
    
    [Fact]
    public void UntaggedEnumValue_GeneratesWarning()
    {
        var source = @"
using SourceGeneratedParsing; 

namespace MyCode
{
    [Lexer]
    public enum TokenType
    {
        Number,
        [Token(""[a-z]+"")]
        Word,
        [Token(""[ ]+"", true)]
        Whitespace,
    }

    [Parser(typeof(TokenType))]
    public partial class Parser { }
}
";

        var result = Host.Run(source); 

        Assert.Contains(result.Diagnostics, d => d.ToString() == "Program.cs(9,9): warning SourceGeneratedParsing004: All enum members in a lexer token type should have a [Token] attribute");
    }    
    
    [Fact]
    public void EmptyEnum_GeneratesError()
    {
        var source = @"
using SourceGeneratedParsing; 

namespace MyCode
{
    [Lexer]
    public enum TokenType { }

    [Parser(typeof(TokenType))]
    public partial class Parser { }
}
";

        var result = Host.Run(source); 

        Assert.Contains(result.Diagnostics, d => d.ToString() == "Program.cs(7,17): error SourceGeneratedParsing003: Expected at least one token to be defined in the token type enum");
    }
    
    [Fact]
    public void InvalidRegex_GeneratesError()
    {
        var source = @"
using SourceGeneratedParsing; 

namespace MyCode
{
    [Lexer]
    public enum TokenType
    {
        [Token(""[a-z+"")]
        Word,
    }

    [Parser(typeof(TokenType))]
    public partial class Parser { }
}
";

        var result = Host.Run(source); 

        Assert.Contains(result.Diagnostics, d => d.ToString() == "Program.cs(9,16): warning SourceGeneratedParsing005: Error in regex syntax - Invalid pattern '[a-z+' at offset 5. Unterminated [] set.");
    }
}