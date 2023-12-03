using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public abstract record ParserElementType
{
    public record Void(ParserElementType Element) : ParserElementType;
    public record Token : ParserElementType;
    public record SymbolType(INamedTypeSymbol Symbol) : ParserElementType;
    
    public record Sequence(ParserElementType First, ParserElementType Second) : ParserElementType;
    public record List(ParserElementType Element) : ParserElementType;
}