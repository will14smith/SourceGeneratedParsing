namespace SourceGeneratedParsing.Models;

public class LexerRule
{
    public LexerRule(string regex, string tokenName, bool ignore)
    {
        Regex = regex;
        TokenName = tokenName;
        Ignore = ignore;
    }

    public string Regex { get; }
    public string TokenName { get; }
    public bool Ignore { get; }
}