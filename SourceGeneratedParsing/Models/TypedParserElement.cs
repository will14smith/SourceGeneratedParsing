using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public record TypedParserElement(ParserElement ParserElement, ParserElementType Type)
{
    public static TypedParserElement FromRule(ParserContext context, ParserRule rule)
    {
        var parameters = rule.Method.Parameters();

        var type = parameters.Length == 1 
            ? FromParameter(context, parameters[0]) 
            : parameters.Select(symbol => FromParameter(context, symbol)).Reverse().Aggregate((a, b) => new ParserElementType.Sequence(b, a));
        
        return new TypedParserElement(rule.Element, type);
    }

    private static ParserElementType FromParameter(ParserContext context, IParameterSymbol parameter) => FromType(context, (INamedTypeSymbol) parameter.Type);

    private static ParserElementType FromType(ParserContext context, INamedTypeSymbol type)
    {
        if (type.FullName() == "System.String") return new ParserElementType.String();
        if (SymbolEqualityComparer.Default.Equals(type, context.TokenTypeSymbol)) return new ParserElementType.TokenType();
        if (type.Name == "Token")
        {
            // we'll claim this is ours.
            if(type is IErrorTypeSymbol) return new ParserElementType.Token();
            if (type.ContainingType != null && SymbolEqualityComparer.Default.Equals(type.ContainingType, context.ParserSymbol)) return new ParserElementType.Token();
        }

        var enumerableInterface = type.AllInterfaces.FirstOrDefault(x => x.FullName() == "System.Collections.Generic.IEnumerable");
        if (enumerableInterface != default)
        {
            var enumerableTypeArgument = (INamedTypeSymbol)enumerableInterface.TypeArguments[0];
            var elementType = FromType(context, enumerableTypeArgument);
            return new ParserElementType.List(elementType);
        }
        
        return new ParserElementType.SymbolType(type);
    }
}