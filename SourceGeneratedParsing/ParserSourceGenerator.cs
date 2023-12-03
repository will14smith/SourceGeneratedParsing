using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratedParsing.Models;

namespace SourceGeneratedParsing;

[Generator]
public class ParserSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(i => i.AddSource("Attributes.cs", Source.Attributes()));
        context.RegisterForSyntaxNotifications(() => new ParserSyntaxReceiver());
    }
    
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not ParserSyntaxReceiver parserSyntaxReceiver)
        {
            return;
        }
        
        foreach (var parserClassSyntax in parserSyntaxReceiver.ParserTypes)
        {
            var parserClassModel = context.Compilation.GetSemanticModel(parserClassSyntax.SyntaxTree);
            var parserType = (INamedTypeSymbol)parserClassModel.GetDeclaredSymbol(parserClassSyntax)!;

            var parserAttribute = parserType.GetAttributes().FirstOrDefault(x => x.AttributeClass.FullName() == "SourceGeneratedParsing.ParserAttribute");
            if (parserAttribute == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MissingParserAttribute, parserClassSyntax.Identifier.GetLocation()));
                continue;
            }
            var tokenType = (INamedTypeSymbol?)parserAttribute.ConstructorArguments[0].Value!;
            if (tokenType.EnumUnderlyingType == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ParserTokenTypeShouldBeEnum, parserAttribute.ApplicationSyntaxReference.SyntaxTree.GetLocation(parserAttribute.ApplicationSyntaxReference.Span)));
                continue;
            }

            var fileNamePrefix = parserType.FullName();
            context.AddSource($"{fileNamePrefix}.Token.cs", Source.TokenType(parserType, tokenType));
            
            var lexerResult = BuildLexerDescriptor(tokenType);
            lexerResult.ReportDiagnostics(context);
            if (!lexerResult.HasValue) { return; }
            context.AddSource($"{fileNamePrefix}.Lexer.cs", Lexer.Build(parserType, lexerResult.Value!));
            
            var parser = BuildParserDescriptor(parserType, tokenType);
            context.AddSource($"{fileNamePrefix}.Parser.cs", Parser.Build(parser));
        }
    }
    private static Result<LexerDescriptor> BuildLexerDescriptor(INamedTypeSymbol tokenType)
    {
        var rules = new List<LexerRule>();
        var diagnostics = new List<Diagnostic>();
        
        foreach (var member in tokenType.GetMembers().OfType<IFieldSymbol>())
        {
            var tokenAttributes = member.GetAttributes().Where(x => x.AttributeClass.FullName() == "SourceGeneratedParsing.TokenAttribute");

            var found = false;
            
            foreach (var attribute in tokenAttributes)
            {
                var regex = (string)attribute.ConstructorArguments[0].Value!;
                var ignore = attribute.ConstructorArguments.Length > 1 && (bool)attribute.ConstructorArguments[1].Value!;

                if (!IsValidRegex(regex, out var error))
                {
                    var attributeNode = (AttributeSyntax) attribute.ApplicationSyntaxReference!.GetSyntax();
                    var argumentNode = attributeNode.ArgumentList!.Arguments[0];
                    
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.InvalidTokenRegex, argumentNode.GetLocation(), error));
                }
                
                rules.Add(new LexerRule(regex, member.Name, ignore));
                found = true;
            }

            if (!found)
            {            
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.MissingTokenAttribute, member.Locations[0]));
            }
        }

        if (rules.Count == 0)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.EmptyTokenType, tokenType.Locations[0]));
        }

        return new Result<LexerDescriptor>(new LexerDescriptor(tokenType, rules), diagnostics);
        
        bool IsValidRegex(string regex, out string error)
        {
            try
            {
                _ = new Regex(regex);
                error = default;
                return true;
            }
            catch(Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }

    private ParserDescriptor BuildParserDescriptor(INamedTypeSymbol parserType, INamedTypeSymbol tokenType)
    {
        var rules = new List<ParserRule>();
        
        
        foreach (var member in parserType.GetMembers())
        {
            if (member is not(IMethodSymbol or INamedTypeSymbol))
            {
                continue;
            }
            
            var productionAttributes = member.GetAttributes().Where(x => x.AttributeClass.FullName() == "SourceGeneratedParsing.ProductionAttribute");

            foreach (var attribute in productionAttributes)
            {
                var name = (string)attribute.ConstructorArguments[0].Value!;
                var match = (string)attribute.ConstructorArguments[1].Value!;

                var element = ParserElementParser.Parse(match);

                switch (member)
                {
                    case IMethodSymbol method:
                        rules.Add(new ParserRule(name, element, new ParseMethod.Method(method)));
                        break;
                    case INamedTypeSymbol type:
                        rules.Add(new ParserRule(name, element, new ParseMethod.Class(type)));
                        break;
                }
            }
        }

        return new ParserDescriptor(parserType, tokenType, rules);
    } 
}