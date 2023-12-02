using Xunit;

namespace SourceGeneratedParsing.Example;

[Parser(typeof(TokenType))]
public partial class Csv
{
    [Lexer]
    public enum TokenType
    {
        [Token("[a-zA-Z0-9]+")]
        Field,
        [Token(",")]
        Comma,
        [Token("\n")]
        NewLine,
    }

    [Fact]
    public void Example()
    {
        var input = "a,b,c\n1,2,3\n4,5,6";
        var lexer = new Lexer(input);
        
        var parser = new Csv();
        var result = parser.ParseFile(ref lexer);
        
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(new[] { "a", "b", "c" }, result.Headers);
        Assert.Equal(new[] { 1, 2, 3 }, result.Lines[0].Fields);
        Assert.Equal(new[] { 4, 5, 6 }, result.Lines[1].Fields);
    }

    [Production("file", "($Field (@Comma $Field)*) @NewLine (line (@NewLine line)*)")]
    public record File(IReadOnlyList<string> Headers, IReadOnlyList<Line> Lines);
    [Production("line", "(number (@Comma number)*)")]
    public record Line(IReadOnlyList<int> Fields);

    [Production("number", "Field")]
    public int Number(Token token) => int.Parse(token.Span);
}