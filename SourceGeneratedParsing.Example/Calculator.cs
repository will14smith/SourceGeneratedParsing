using Xunit;

namespace SourceGeneratedParsing.Example;

[Parser(typeof(TokenType))]
public partial class Calculator
{
    [Fact]
    public void Example()
    {
        var input = "1 + (2 - -3)";
        var lexer = new Lexer(input);
        
        var parser = new Calculator();
        var result = parser.ParseExpression(ref lexer);
        
        Assert.Equal(6, result);
    }

    // productions are matched top-down, so "expression" is the top level production
    // in the match expressions:
    // - lower case identifiers are non-terminals, so refer to other productions
    // - upper case identifiers are terminals, and refer to the enum values from the lexer
    [Production("expression", "term Plus expression")]
    [Production("expression", "term Minus expression")]
    public int ExpressionAddition(int left, TokenType op, int right) =>
        op switch
        {
            TokenType.Plus => left + right,
            TokenType.Minus => left - right,
            _ => throw new InvalidOperationException()
        };
    // multiple definitions are defined as an "order-choice", so all are attempted to be matched but the definition order is used to pick the first match 
    // e.g. if this came before the previous definitions then it would never match the Plus/Minus variants
    [Production("expression", "term")]
    public int ExpressionTerm(int value) => value;
    
    [Production("term", "primary")]
    public int TermPrimary(int value) => value;
    // identifiers can be skipped when calling methods by prepending them with an "@"
    // they will still be matched and any sub-functions called
    [Production("term", "@Minus term")]
    public int TermNegate(int value) => - value;
    
    // with Tokens - either the whole token can be passed as here, or just the lexer type enum as in the first production 
    [Production("primary", "Number")]
    public int Primary(Token value) => int.Parse(value.Span);
    [Production("primary", "@LeftParen expression @RightParen")]
    public int Group(int value) => value;
}