using Xunit;

namespace SourceGeneratedParsing.Example;

[Parser(typeof(TokenType))]
public partial class Y23D2
{
    [Lexer]
    public enum TokenType
    {
        [Token("[a-zA-Z]+")]
        Word,
        [Token("[0-9]+")]
        Number,
        [Token(":")]
        Colon,
        [Token(",")]
        Comma,
        [Token(";")]
        Semicolon,
        [Token("\n")]
        NewLine,
        [Token("[ ]+", true)]
        WhiteSpace,
    }
    
    [Fact]
    public void Example()
    {
        var input = "Game 1: 3 blue, 4 red; 1 red, 2 green, 6 blue; 2 green\nGame 2: 1 blue, 2 green; 3 green, 4 blue, 1 red; 1 green, 1 blue\nGame 3: 8 green, 6 blue, 20 red; 5 blue, 4 red, 13 green; 5 green, 1 red\nGame 4: 1 green, 3 red, 6 blue; 3 green, 6 red; 3 green, 15 blue, 14 red\nGame 5: 6 red, 1 blue, 3 green; 2 blue, 1 red, 2 green";
        var lexer = new Lexer(input);
        
        var parser = new Y23D2();
        var result = parser.ParseGames(ref lexer);
    }
    
    [Production("games", "(game (@NewLine game)*)")]
    public IReadOnlyCollection<(int Id, IReadOnlyList<IReadOnlyDictionary<Colour, int>> Rounds)> Games(IReadOnlyCollection<(int Id, IReadOnlyList<IReadOnlyDictionary<Colour, int>> Rounds)> games) => games;
    
    [Production("game", "@Word number @Colon (round (@Semicolon round)*)")]
    public (int Id, IReadOnlyList<IReadOnlyDictionary<Colour, int>> Rounds) Game(int id, IReadOnlyList<IReadOnlyDictionary<Colour, int>> rounds) => (id, rounds);
    [Production("round", "(colourCount (@Comma colourCount)*)")]
    public IReadOnlyDictionary<Colour, int> Round(IReadOnlyList<(Colour Colour, int Count)> counts) => counts.ToDictionary(x => x.Colour, x => x.Count);
    [Production("colourCount", "number colour")]
    public (Colour Colour, int Count) ColourCount(int count, Colour colour) => (colour, count);
    [Production("number", "Number")]
    public int Number(Token token) => int.Parse(token.Span);
    [Production("colour", "Word")]
    public Colour ColourParser(string input) => Enum.Parse<Colour>(input, true);
    
    public enum Colour
    {
        Red,
        Green,
        Blue,
    }
}