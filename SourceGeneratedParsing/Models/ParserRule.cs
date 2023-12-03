using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SourceGeneratedParsing.Models;

public record ParserRule(string Name, ParserElement Element, ParseMethod Method);

public abstract record ParseMethod
{
    public record Method(IMethodSymbol Symbol) : ParseMethod
    {
        public override ITypeSymbol ReturnType => Symbol.ReturnType;
        public override ImmutableArray<IParameterSymbol> Parameters() => Symbol.Parameters;

        public override string Construct(string receiver, IReadOnlyList<string> arguments)
        {
            return $"{receiver}.{Symbol.Name}({string.Join(", ", arguments)})";
        }
    }

    public record Class(INamedTypeSymbol Symbol) : ParseMethod
    {
        public override ITypeSymbol ReturnType => Symbol;
        public override ImmutableArray<IParameterSymbol> Parameters()
        {
            foreach (var constructor in Symbol.Constructors)
            {
                switch (constructor.Parameters.Length)
                {
                    // handle default constructor
                    case 0: continue;
                    
                    // handle record copy constructor
                    case 1 when SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, Symbol): continue;
                    
                    default: return constructor.Parameters;
                }
            }

            throw new InvalidOperationException("missing constructor");
        }

        public override string Construct(string receiver, IReadOnlyList<string> arguments)
        {
            return $"new {Symbol.FullName()}({string.Join(", ", arguments)})";
        }
    }

    public abstract ITypeSymbol ReturnType { get; }
    public abstract ImmutableArray<IParameterSymbol> Parameters();
    public abstract string Construct(string receiver, IReadOnlyList<string> arguments);
}