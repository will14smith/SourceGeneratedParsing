using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGeneratedParsing;

public class ParserSyntaxReceiver : ISyntaxReceiver
{
    private readonly List<EnumDeclarationSyntax> _tokenTypes = new();
    private readonly List<ClassDeclarationSyntax> _parserTypes = new();

    public IReadOnlyCollection<EnumDeclarationSyntax> TokenTypes => _tokenTypes;
    public IReadOnlyCollection<ClassDeclarationSyntax> ParserTypes => _parserTypes;

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is EnumDeclarationSyntax { AttributeLists.Count: > 0 } enumDeclarationSyntax &&
            enumDeclarationSyntax.AttributeLists
                .Any(al => al.Attributes
                    .Any(x => IsAttributeNamed(x.Name, "Lexer"))))
        {
            _tokenTypes.Add(enumDeclarationSyntax);
            return;
        }
        
        if (syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclarationSyntax &&
            classDeclarationSyntax.AttributeLists
                .Any(al => al.Attributes
                    .Any(x => IsAttributeNamed(x.Name, "Parser"))))
        {
            _parserTypes.Add(classDeclarationSyntax);
            return;
        }
    }

    private static bool IsAttributeNamed(NameSyntax name, string match)
    {
        if (name is QualifiedNameSyntax qualified)
        {
            if (qualified.Left is not IdentifierNameSyntax { Identifier.ValueText: "SourceGeneratedParsing" })
            {
                return false;
            }

            name = qualified.Right;
        }

        if (name is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText == match || identifier.Identifier.ValueText == $"{match}Attribute";
        }

        return false;
    }
}