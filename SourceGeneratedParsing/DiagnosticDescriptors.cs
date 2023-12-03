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
    public static readonly DiagnosticDescriptor EmptyTokenType = new("SourceGeneratedParsing003",
        "Token type contains no tokens",
        "Expected at least one token to be defined in the token type enum", "Parser", DiagnosticSeverity.Error, true);
    public static readonly DiagnosticDescriptor MissingTokenAttribute = new("SourceGeneratedParsing004",
        "Missing [Token] attribute",
        "All enum members in a lexer token type should have a [Token] attribute", "Parser", DiagnosticSeverity.Warning, true);
    public static readonly DiagnosticDescriptor InvalidTokenRegex = new("SourceGeneratedParsing005",
        "Invalid regex syntax for token",
        "Error in regex syntax - {0}", "Parser", DiagnosticSeverity.Warning, true);
}