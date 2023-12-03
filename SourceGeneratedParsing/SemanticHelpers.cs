using System.Text;
using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing;

public static class SemanticHelpers
{
    public static string? Source(this INamedTypeSymbol? symbol)
    {
        if (symbol == null)
        {
            return null;
        }
        
        if (!symbol.IsGenericType)
        {
            return symbol.FullName();
        }

        var sb = new StringBuilder();
        sb.Append(symbol.FullName());
        sb.Append('<');

        var first = true;
        foreach (var symbolTypeParameter in symbol.TypeArguments)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            sb.Append(((INamedTypeSymbol)symbolTypeParameter).Source());
            
            first = false;
        }
        
        sb.Append('>');

        return sb.ToString();
    }

    
    public static string? FullName(this INamedTypeSymbol? symbol)
    {
        if (symbol == null)
        {
            return null;
        }

        if (symbol.ContainingType != null)
        {
            return $"{FullName(symbol.ContainingType)}.{symbol.Name}";
        }
        
        var ns = FullNamespace(symbol);
        return string.IsNullOrEmpty(ns) ? symbol.Name : $"{ns}.{symbol.Name}";
    }
    
    public static string? FullNamespace(this ISymbol? symbol)
    {
        if (symbol == null)
        {
            return null;
        }
        
        var ns = symbol as INamespaceSymbol ?? symbol.ContainingNamespace;
        if (ns.IsGlobalNamespace)
        {
            return null;
        }

        if (ns.ContainingNamespace == null)
        {
            return ns.Name;
        }
        
        var parentNs = FullNamespace(ns.ContainingNamespace);
        return string.IsNullOrEmpty(parentNs) ? ns.Name : $"{parentNs}.{ns.Name}";
    }
}