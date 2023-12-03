using SourceGeneratedParsing.Models;

namespace SourceGeneratedParsing.ParserSource;

public class ParserSourceBuilder
{
    public static ParserSource Build(ParserContext context, TypedParserElement typed)
    {
        return Build(context, typed.ParserElement, typed.Type);
    }

    private static ParserSource Build(ParserContext context, ParserElement element, ParserElementType type)
    {
        switch (element)
        {
            case ParserElement.Terminal terminal: return BuildTerminal(context, terminal, type);
            case ParserElement.NonTerminal nonTerminal: return BuildNonTerminal(context, nonTerminal, type);
            
            case ParserElement.Sequence sequence: return BuildSequence(context, sequence, type);
            case ParserElement.ZeroOrMore zeroOrMore: return BuildZeroOrMore(context, zeroOrMore, type);
            case ParserElement.Group group: return BuildGroup(context, group, type);
            case ParserElement.Discard discard: return BuildDiscard(context, discard, type);
            
            default: throw new ArgumentOutOfRangeException();
        }
    }
    
    private static ParserSource BuildTerminal(ParserContext context, ParserElement.Terminal terminal, ParserElementType type)
    {
        if (type is ParserElementType.List listType)
        {
            return new ParserSource.SingletonList(BuildTerminal(context, terminal, listType.Element));
        }

        return type switch
        {
            ParserElementType.Void => new ParserSource.Terminal(context.TokenTypeSymbol, terminal.TokenName, ParserSource.Terminal.TerminalType.Void),
            ParserElementType.String => new ParserSource.Terminal(context.TokenTypeSymbol, terminal.TokenName, ParserSource.Terminal.TerminalType.String),
            ParserElementType.TokenType => new ParserSource.Terminal(context.TokenTypeSymbol, terminal.TokenName, ParserSource.Terminal.TerminalType.TokenType),
            ParserElementType.Token => new ParserSource.Terminal(context.TokenTypeSymbol, terminal.TokenName, ParserSource.Terminal.TerminalType.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static ParserSource BuildNonTerminal(ParserContext context, ParserElement.NonTerminal nonTerminal, ParserElementType type)
    {
        if (type is ParserElementType.List listType)
        {
            return new ParserSource.SingletonList(BuildNonTerminal(context, nonTerminal, listType.Element));
        }
        
        // TODO check symbol type is assignable from nonTerminal type
        if (type is not ParserElementType.SymbolType)
        {
            throw new NotImplementedException();
        }
        
        return new ParserSource.NonTerminal(nonTerminal.NonTerminalName, context.NonTerminalParseMethodNames[nonTerminal.NonTerminalName]);
    }

    private static ParserSource BuildSequence(ParserContext context, ParserElement.Sequence sequence, ParserElementType type)
    {
        if (type is ParserElementType.Sequence sequenceType)
        {
            var a = Build(context, sequence.First, sequenceType.First);
            var b = Build(context, sequence.Second, sequenceType.Second);

            return new ParserSource.Sequence(a, b);
        }

        if (type is ParserElementType.List listType)
        {
            var a = Build(context, sequence.First, listType);
            var b = Build(context, sequence.Second, listType);

            return new ParserSource.SequenceConcat(a, b);
        }

        if (sequence.First is ParserElement.Discard)
        {
            var a = Build(context, sequence.First, new ParserElementType.Void());
            var b = Build(context, sequence.Second, type);

            return new ParserSource.Sequence(a, b);
        }

        if (sequence.Second is ParserElement.Discard)
        {
            var a = Build(context, sequence.First, type);
            var b = Build(context, sequence.Second, new ParserElementType.Void());

            return new ParserSource.Sequence(a, b);
        }

        throw new NotImplementedException();
    }
    
    private static ParserSource BuildZeroOrMore(ParserContext context, ParserElement.ZeroOrMore zeroOrMore, ParserElementType type)
    {
        if (type is not ParserElementType.List listType)
        {
            throw new NotImplementedException();
        }

        var inner = Build(context, zeroOrMore.Rule, listType.Element);

        return new ParserSource.ZeroOrMore(inner, listType.Element);
    }
    
    private static ParserSource BuildGroup(ParserContext context, ParserElement.Group group, ParserElementType type)
    {
        return Build(context, group.Rule, type);
    }
    
    private static ParserSource BuildDiscard(ParserContext context, ParserElement.Discard discard, ParserElementType type)
    {
        switch (type)
        {
            case ParserElementType.Void:
            {
                var inner = Build(context, discard.Rule, type);

                return new ParserSource.Discard(inner);
            }

            case ParserElementType.List:
            {
                var inner = Build(context, discard.Rule, new ParserElementType.Void());

                return new ParserSource.Discard(inner);
            }
            
            default:
                throw new NotImplementedException();
        }
    }
}