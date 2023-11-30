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
                    .Any(x => x.Name.ToString() is "Lexer" or "LexerAttribute")))
        {
            _tokenTypes.Add(enumDeclarationSyntax);
            return;
        }
        
        if (syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclarationSyntax &&
            classDeclarationSyntax.AttributeLists
                .Any(al => al.Attributes
                    .Any(x => x.Name.ToString() is "Parser" or "ParserAttribute")))
        {
            _parserTypes.Add(classDeclarationSyntax);
            return;
        }
    }
}