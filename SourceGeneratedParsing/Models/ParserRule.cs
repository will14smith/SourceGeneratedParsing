using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public record ParserRule(string Name, ParserElement Element, ParseMethod Method);

public abstract record ParseMethod
{
    public record Method(IMethodSymbol Symbol) : ParseMethod
    {
        public override ITypeSymbol ReturnType => Symbol.ReturnType;
        public override ImmutableArray<IParameterSymbol> Parameters(int arity) => Symbol.Parameters;

        public override string Construct(string receiver, IReadOnlyList<string> arguments)
        {
            return $"{receiver}.{Symbol.Name}({string.Join(", ", arguments)})";
        }
    }

    public record Class(INamedTypeSymbol Symbol) : ParseMethod
    {
        public override ITypeSymbol ReturnType => Symbol;
        public override ImmutableArray<IParameterSymbol> Parameters(int arity)
        {
            if (arity == 1)
            {
                // handle record copy constructor
                return Symbol.Constructors.Single(x => x.Parameters.Length == 1 && !SymbolEqualityComparer.Default.Equals(x.Parameters[0].Type, Symbol)).Parameters;
            }
            
            return Symbol.Constructors.Single(x => x.Parameters.Length == arity).Parameters;
        }

        public override string Construct(string receiver, IReadOnlyList<string> arguments)
        {
            return $"new {Symbol.FullName()}({string.Join(", ", arguments)})";
        }
    }

    public abstract ITypeSymbol ReturnType { get; }
    public abstract ImmutableArray<IParameterSymbol> Parameters(int arity);
    public abstract string Construct(string receiver, IReadOnlyList<string> arguments);
}