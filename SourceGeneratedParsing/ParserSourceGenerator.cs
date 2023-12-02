using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
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

    private readonly List<string> _log = new();
    
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
            
            var lexer = BuildLexerDescriptor(tokenType);
            context.AddSource($"{fileNamePrefix}.Lexer.cs", Lexer.Build(parserType, lexer));
            
            var parser = BuildParserDescriptor(parserType, tokenType);
            context.AddSource($"{fileNamePrefix}.Parser.cs", Parser.Build(parser));
        }
        
        context.AddSource("Debug.cs", SourceText.From(string.Join("\n", _log.Select(x => "// " + x)), Encoding.UTF8));
    }
    private static LexerDescriptor BuildLexerDescriptor(INamedTypeSymbol tokenType)
    {
        var rules = new List<LexerRule>();
        
        foreach (var member in tokenType.GetMembers().OfType<IFieldSymbol>())
        {
            var tokenAttributes = member.GetAttributes().Where(x => x.AttributeClass.FullName() == "SourceGeneratedParsing.TokenAttribute");
            
            foreach (var attribute in tokenAttributes)
            {
                var regex = (string)attribute.ConstructorArguments[0].Value!;
                var ignore = attribute.ConstructorArguments.Length > 1 && (bool)attribute.ConstructorArguments[1].Value!;
                
                rules.Add(new LexerRule(regex, member.Name, ignore));
            }
        }

        return new LexerDescriptor(tokenType, rules);
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