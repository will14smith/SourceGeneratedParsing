using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public class ParserDescriptor
{
    public ParserDescriptor(INamedTypeSymbol parserType, INamedTypeSymbol tokenType, IReadOnlyList<ParserRule> rules)
    {
        ParserType = parserType;
        TokenType = tokenType;
        Rules = rules;
    }

    public INamedTypeSymbol ParserType { get; }
    public INamedTypeSymbol TokenType { get; }
    public IReadOnlyList<ParserRule> Rules { get; }
}