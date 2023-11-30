using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MissingParserAttribute = new("SourceGeneratedParsing001",
        "Missing parser attribute",
        "Failed to find [Parser(typeof(Token))]", "Parser", DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor ParserTokenTypeShouldBeEnum = new("SourceGeneratedParsing002",
        "Parser token type should be an enum",
        "Expected an enum didn't get it", "Parser", DiagnosticSeverity.Error, true);
    
    

}