using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public record TypedParserElement(ParserElement ParserElement, ParserElementType ElementType, ParserTargetType TargetType)
{
    public static TypedParserElement FromRule(ParserContext context, ParserRule rule)
    {
        var elementType = TypeOfElement(context, rule.Element);
        
        var parameters = rule.Method.Parameters();
        var targetType = parameters.Length == 1 
            ? FromParameter(context, parameters[0]) 
            : parameters.Select(symbol => FromParameter(context, symbol)).Reverse().Aggregate((a, b) => new ParserTargetType.Sequence(b, a));
        
        return new TypedParserElement(rule.Element, elementType, targetType);
    }

    public static ParserElementType TypeOfElement(ParserContext context, ParserElement element)
    {
        switch (element)
        {
            case ParserElement.Terminal: return new ParserElementType.Token();
            case ParserElement.NonTerminal nonTerminal: return new ParserElementType.SymbolType(context.NonTerminalTypes[nonTerminal.NonTerminalName]);
            
            case ParserElement.Sequence sequence:
                var first = TypeOfElement(context, sequence.First);
                var second = TypeOfElement(context, sequence.Second);

                return new ParserElementType.Sequence(first, second);
            case ParserElement.ZeroOrMore zeroOrMore: return new ParserElementType.List(TypeOfElement(context, zeroOrMore.Rule));
            case ParserElement.OneOrMore oneOrMore: return new ParserElementType.List(TypeOfElement(context, oneOrMore.Rule));
            case ParserElement.Optional optional: return TypeOfElement(context, optional.Rule);
            case ParserElement.Group group: return TypeOfElement(context, group.Rule);
            
            case ParserElement.Discard discard: return new ParserElementType.Void(TypeOfElement(context, discard.Rule));
            
            default: throw new ArgumentOutOfRangeException(nameof(element));
        }
    }

    private static ParserTargetType FromParameter(ParserContext context, IParameterSymbol parameter) => FromType(context, (INamedTypeSymbol) parameter.Type);

    private static ParserTargetType FromType(ParserContext context, INamedTypeSymbol type)
    {
        if (type.FullName() == "System.String") return new ParserTargetType.String();
        if (SymbolEqualityComparer.Default.Equals(type, context.TokenTypeSymbol)) return new ParserTargetType.TokenType();
        if (type.Name == "Token")
        {
            // we'll claim this is ours.
            if(type is IErrorTypeSymbol) return new ParserTargetType.Token();
            if (type.ContainingType != null && SymbolEqualityComparer.Default.Equals(type.ContainingType, context.ParserSymbol)) return new ParserTargetType.Token();
        }

        IReadOnlyCollection<INamedTypeSymbol> allInterfaces = type.TypeKind == TypeKind.Interface ? type.AllInterfaces.Prepend(type).ToArray() : type.AllInterfaces;
        
        // prevent dictionaries being treated as lists
        var dictionary = allInterfaces.FirstOrDefault(x => x.FullName() == "System.Collections.Generic.IReadOnlyDictionary");
        if (dictionary != default)
        {
            return new ParserTargetType.SymbolType(dictionary);
        }
        
        var enumerableInterface = allInterfaces.FirstOrDefault(x => x.FullName() == "System.Collections.Generic.IEnumerable");
        if (enumerableInterface != default)
        {
            var enumerableTypeArgument = (INamedTypeSymbol)enumerableInterface.TypeArguments[0];
            var elementType = FromType(context, enumerableTypeArgument);
            return new ParserTargetType.List(elementType);
        }
        
        return new ParserTargetType.SymbolType(type);
    }
}