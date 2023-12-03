using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGeneratedParsing.Tests.Utilities;

public static class GeneratedSourceResultExtensions
{
    public static IReadOnlyList<ClassDeclarationSyntax> GetClasses(this GeneratedSourceResult result) =>
        result.SyntaxTree
            .GetRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .ToList();
    public static IReadOnlyList<StructDeclarationSyntax> GetStructs(this GeneratedSourceResult result) =>
        result.SyntaxTree
            .GetRoot().DescendantNodes()
            .OfType<StructDeclarationSyntax>()
            .ToList();
}