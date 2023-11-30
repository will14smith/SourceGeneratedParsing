using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public class LexerDescriptor
{
    public LexerDescriptor(INamedTypeSymbol tokenType, IReadOnlyList<LexerRule> rules)
    {
        TokenType = tokenType;
        Rules = rules;
    }

    public INamedTypeSymbol TokenType { get; }
    public IReadOnlyList<LexerRule> Rules { get; }
}