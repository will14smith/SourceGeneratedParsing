using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SourceGeneratedParsing;

public class Source
{
    public static SourceText Attributes() =>
        SourceText.From(@"
using System;

namespace SourceGeneratedParsing
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class LexerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class TokenAttribute : Attribute
    {
        public TokenAttribute(string regex, bool ignore = false) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ParserAttribute : Attribute
    {
        public ParserAttribute(Type tokenType) { }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class ProductionAttribute : Attribute
    {
        public ProductionAttribute(string name, string match) { }
    }
}
", Encoding.UTF8);

    public static SourceText TokenType(INamedTypeSymbol parserTypeSymbol, INamedTypeSymbol tokenTypeSymbol)
    {
        var tokenTypeName = tokenTypeSymbol.FullName();
        
        return SourceText.From($@"
using System;

namespace {parserTypeSymbol.FullNamespace()}
{{
    public partial class {parserTypeSymbol.Name}
    {{
        public ref struct Token
        {{
            public Token({tokenTypeName} type, ReadOnlySpan<char> span, int offset)
            {{
                Type = type;
                Span = span;
                Offset = offset;
            }}

            public {tokenTypeName} Type {{ get; }}
            public ReadOnlySpan<char> Span {{ get; }}
            public int Offset {{ get; }}
        }}
    }}
}}
", Encoding.UTF8);
    }
}