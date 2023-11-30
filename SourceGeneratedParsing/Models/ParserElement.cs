namespace SourceGeneratedParsing.Models;

public abstract record ParserElement
{
    // atom
    public record Terminal(string TokenName) : ParserElement
    {
        public override string ToString() => TokenName;
    }

    public record NonTerminal(string NonTerminalName) : ParserElement
    {
        public override string ToString() => NonTerminalName;
    }
    public record Empty : ParserElement;
    
    // combinators
    public record Sequence(ParserElement First, ParserElement Second) : ParserElement
    {
        public override string ToString() => $"{First} {Second}";
    }
    public record Choice(ParserElement First, ParserElement Second) : ParserElement;
    public record ZeroOrMore(ParserElement Rule) : ParserElement;
    public record OneOrMore(ParserElement Rule) : ParserElement;
    public record Optional(ParserElement Rule) : ParserElement;

    public record AndPredicate(ParserElement Rule) : ParserElement;
    public record NotPredicate(ParserElement Rule) : ParserElement;
    
    // custom - behaviour is same as Rule but result is not used
    public record Discard(ParserElement Rule) : ParserElement
    {
        public override string ToString() => $"@{Rule}";
    }
}