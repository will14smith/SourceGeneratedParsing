using Microsoft.CodeAnalysis;
using SourceGeneratedParsing.Models;

namespace SourceGeneratedParsing;

public class ParserContext
{
    public ParserContext(
        INamedTypeSymbol parserSymbol, 
        INamedTypeSymbol tokenTypeSymbol,
        IReadOnlyList<string> nonTerminalsInDefinitionOrder, 
        ILookup<string, ParserRule> nonTerminalsByName,
        IReadOnlyDictionary<string, INamedTypeSymbol> nonTerminalTypes, IReadOnlyDictionary<string, string> nonTerminalParseMethodNames)
    {
        ParserSymbol = parserSymbol;
        TokenTypeSymbol = tokenTypeSymbol;
        NonTerminalsInDefinitionOrder = nonTerminalsInDefinitionOrder;
        NonTerminalsByName = nonTerminalsByName;
        NonTerminalTypes = nonTerminalTypes;
        NonTerminalParseMethodNames = nonTerminalParseMethodNames;
    }

    public INamedTypeSymbol ParserSymbol { get; }
    public INamedTypeSymbol TokenTypeSymbol { get; }
    public IReadOnlyList<string> NonTerminalsInDefinitionOrder { get; }
    public ILookup<string, ParserRule> NonTerminalsByName { get; }
    public IReadOnlyDictionary<string, INamedTypeSymbol> NonTerminalTypes { get; }
    public IReadOnlyDictionary<string, string> NonTerminalParseMethodNames { get; }

    public static ParserContext Build(ParserDescriptor descriptor)
    {
        var nonTerminalsInDefinitionOrder = descriptor.GetNonTerminalsInDefinitionOrder();
        var nonTerminalsByName = descriptor.Rules.ToLookup(x => x.Name);
        var nonTerminalTypes = nonTerminalsByName.ToDictionary(x => x.Key, x => GetCommonReturn(x.Select(r => (INamedTypeSymbol)r.Method.ReturnType)));
        var nonTerminalParseMethodNames = nonTerminalsInDefinitionOrder.ToDictionary(x => x, NonTerminalMethodName);
        
        return new ParserContext(descriptor.ParserType, descriptor.TokenType, nonTerminalsInDefinitionOrder, nonTerminalsByName, nonTerminalTypes, nonTerminalParseMethodNames);
    }
    
    private static INamedTypeSymbol GetCommonReturn(IEnumerable<INamedTypeSymbol> types)
    {
        INamedTypeSymbol? current = null;

        foreach (var type in types)
        {
            if (current == null)
            {
                current = type;
            }
            else if(!SymbolEqualityComparer.Default.Equals(current, type))
            {
                current = GetCommonReturn(current, type);
            }
        }

        // TODO report diagnostic?
        return current ?? throw new InvalidOperationException("couldn't find base");
    }

    private static INamedTypeSymbol GetCommonReturn(INamedTypeSymbol a, INamedTypeSymbol b)
    {
        var aBases = GetBaseClasses(a);
        var bBases = GetBaseClasses(b);
        
        // is one a base class of the other?
        if (aBases.Contains(b, SymbolEqualityComparer.Default))
        {
            return b;
        }
        if (bBases.Contains(a, SymbolEqualityComparer.Default))
        {
            return a;
        }

        // TODO do they share an interface?
        // TODO do they share a base class?
        
        throw new NotImplementedException();
    }

    private static IReadOnlyList<INamedTypeSymbol> GetBaseClasses(INamedTypeSymbol? a)
    {
        var types = new List<INamedTypeSymbol>();

        while (a != null)
        {
            types.Add(a);
            a = a.BaseType;
        }

        return types;
    }
    
    private static string NonTerminalMethodName(string nonTerminal)
    {
        return char.ToUpper(nonTerminal[0]) + nonTerminal.Substring(1);
    }
}