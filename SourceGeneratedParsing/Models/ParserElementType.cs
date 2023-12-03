using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public abstract record ParserElementType
{
    public record Void : ParserElementType;

    public record Token : ParserElementType;
    public record TokenType : ParserElementType;
    public record String : ParserElementType;
    
    public record SymbolType(INamedTypeSymbol Symbol) : ParserElementType;
    
    public record Sequence(ParserElementType First, ParserElementType Second) : ParserElementType;
    public record List(ParserElementType Element) : ParserElementType;

}