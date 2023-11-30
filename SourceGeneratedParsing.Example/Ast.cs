using Xunit;

namespace SourceGeneratedParsing.Example;

[Parser(typeof(TokenType))]
public partial class Ast
{
    [Fact]
    public void Example()
    {
        var input = "1 + (2 - -3)";
        var lexer = new Lexer(input);
        
        var parser = new Ast();
        var result = parser.ParseExpression(ref lexer);

        var expected = new Expression(
            new Number(1),
            TokenType.Plus,
            new Expression(
                new Number(2),
                TokenType.Minus,
                new Negate(new Number(3))
            )
        );
        Assert.Equal(expected, result);
    }
    
    public abstract record Node;

    // productions can match constructors of classes with the same arity as the match
    [Production("expression", "term Plus expression")]
    [Production("expression", "term Minus expression")]
    public record Expression(Node Left, TokenType OperatorToken, Node Right) : Node;
    
    // elements of the match can be skipped from being passed into the constructor
    [Production("term", "@Minus term")]
    public record Negate(Node Inner) : Node;
    
    // methods can also used as matches when more parsing logic is required
    [Production("primary", "Number")]
    public Node NumberBuilder(Token value) => new Number(int.Parse(value.Span));
    public record Number(int Value) : Node;
    
    [Production("expression", "term")]
    [Production("term", "primary")]
    [Production("primary", "@LeftParen expression @RightParen")]
    public Node Group(Node inner) => inner;
}