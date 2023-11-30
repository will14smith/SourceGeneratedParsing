using Xunit;

namespace SourceGeneratedParsing.Example;

[Lexer]
public enum TokenType
{
    [Token("[0-9]+")]
    Number,
    
    [Token("\\(")]
    LeftParen,
    [Token("\\)")]
    RightParen,
    
    [Token("\\+")]
    Plus,
    [Token("\\-")]
    Minus,
    
    [Token("[ \\t]+", ignore: true)]
    WhiteSpace,
}

public class TokenTypeUsage
{
    [Fact]
    public void Lexer()
    {
        var input = "1 + (2 - -3)";
        
        // Lexers don't exist "naturally", only as a sub-component of a parser 
        // Calculator and Ast use the same TokenType so generate the same Lexer implementation
        var lexer = new Calculator.Lexer(input);

        // lexer returns true and the next valid Token 
        AssertNext(ref lexer, TokenType.Number, "1");
        AssertNext(ref lexer, TokenType.Plus);
        AssertNext(ref lexer, TokenType.LeftParen);
        AssertNext(ref lexer, TokenType.Number, "2");
        AssertNext(ref lexer, TokenType.Minus);
        AssertNext(ref lexer, TokenType.Minus);
        AssertNext(ref lexer, TokenType.Number, "3");
        AssertNext(ref lexer, TokenType.RightParen);
        // until the end of the input when it returns false
        AssertEof(ref lexer);
    }
    
    private static void AssertNext(ref Calculator.Lexer lexer, TokenType type, string? span = null)
    {
        Assert.True(lexer.Next(out var token));
        Assert.Equal(type, token.Type);
        if (span != default)
        {
            Assert.Equal(span, new string(token.Span));
        }
    }
    private static void AssertEof(ref Calculator.Lexer lexer) => Assert.False(lexer.Next(out _));
}