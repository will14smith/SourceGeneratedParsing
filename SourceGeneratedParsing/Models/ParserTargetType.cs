using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public abstract record ParserTargetType
{
    public record Void : ParserTargetType;

    public record Token : ParserTargetType;
    public record TokenType : ParserTargetType;
    public record String : ParserTargetType;
    
    public record SymbolType(INamedTypeSymbol Symbol) : ParserTargetType;
    
    public record Sequence(ParserTargetType First, ParserTargetType Second) : ParserTargetType;
    public record List(ParserTargetType Element) : ParserTargetType;
}