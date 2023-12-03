using SourceGeneratedParsing.Models;

namespace SourceGeneratedParsing.ParserSource;

public class ParserSourceBuilder
{
    public static ParserSource Build(ParserContext context, TypedParserElement typed)
    {
        return Build(context, typed.ParserElement, typed.ElementType, typed.TargetType);
    }

    private static ParserSource Build(ParserContext context, ParserElement element, ParserElementType elementType, ParserTargetType targetType)
    {
        switch (element)
        {
            case ParserElement.Terminal terminal: return BuildTerminal(context, terminal, elementType, targetType);
            case ParserElement.NonTerminal nonTerminal: return BuildNonTerminal(context, nonTerminal, elementType, targetType);
            
            case ParserElement.Sequence sequence: return BuildSequence(context, sequence, elementType, targetType);
            case ParserElement.ZeroOrMore zeroOrMore: return BuildZeroOrMore(context, zeroOrMore, elementType, targetType);
            case ParserElement.OneOrMore oneOrMore: return BuildOneOrMore(context, oneOrMore, elementType, targetType);
            case ParserElement.Optional optional: return BuildOptional(context, optional, elementType, targetType);
            case ParserElement.Group group: return BuildGroup(context, group, elementType, targetType);
            case ParserElement.Discard discard: return BuildDiscard(context, discard, elementType, targetType);
            
            default: throw new ArgumentOutOfRangeException();
        }
    }
    
    private static ParserSource BuildTerminal(ParserContext context, ParserElement.Terminal terminal, ParserElementType elementType, ParserTargetType targetType)
    {
        if (elementType is not ParserElementType.Token)
        {
            throw new InvalidOperationException($"attempting to cast Token to {elementType}");
        }
        
        return targetType switch
        {
            ParserTargetType.Void => new ParserSource.Terminal(context.TokenTypeSymbol, terminal.TokenName, ParserSource.Terminal.TerminalType.Void),
            ParserTargetType.String => new ParserSource.Terminal(context.TokenTypeSymbol, terminal.TokenName, ParserSource.Terminal.TerminalType.String),
            ParserTargetType.TokenType => new ParserSource.Terminal(context.TokenTypeSymbol, terminal.TokenName, ParserSource.Terminal.TerminalType.TokenType),
            ParserTargetType.Token => new ParserSource.Terminal(context.TokenTypeSymbol, terminal.TokenName, ParserSource.Terminal.TerminalType.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(targetType))
        };
    }

    private static ParserSource BuildNonTerminal(ParserContext context, ParserElement.NonTerminal nonTerminal, ParserElementType elementType, ParserTargetType targetType)
    {
        // TODO check symbol types are assignable from the non-terminal type

        if (elementType is not ParserElementType.SymbolType)
        {
            throw new InvalidOperationException($"attempting to cast non-terminal '{nonTerminal.NonTerminalName}' to {elementType}");
        }
        
        if (targetType is not ParserTargetType.SymbolType)
        {
            throw new NotImplementedException();
        }
        
        return new ParserSource.NonTerminal(nonTerminal.NonTerminalName, context.NonTerminalParseMethodNames[nonTerminal.NonTerminalName]);
    }

    private static ParserSource BuildSequence(ParserContext context, ParserElement.Sequence sequence, ParserElementType elementType, ParserTargetType targetType)
    {
        if (elementType is not ParserElementType.Sequence sequenceElementType)
        {
            throw new InvalidOperationException($"attempting to cast sequence to non-sequence type {elementType}");
        }
        
        if (sequenceElementType.First is ParserElementType.Void)
        {
            var a = Build(context, sequence.First, sequenceElementType.First, new ParserTargetType.Void());
            var b = Build(context, sequence.Second, sequenceElementType.Second, targetType);

            return new ParserSource.Sequence(a, b);
        }

        if (sequenceElementType.Second is ParserElementType.Void)
        {
            var a = Build(context, sequence.First, sequenceElementType.First, targetType);
            var b = Build(context, sequence.Second, sequenceElementType.Second, new ParserTargetType.Void());

            return new ParserSource.Sequence(a, b);
        }
        
        if (targetType is ParserTargetType.Sequence sequenceType)
        {
            var a = Build(context, sequence.First, sequenceElementType.First, sequenceType.First);
            var b = Build(context, sequence.Second, sequenceElementType.Second, sequenceType.Second);

            return new ParserSource.Sequence(a, b);
        }

        if (targetType is ParserTargetType.List listType)
        {
            ParserSource a, b;

            if (sequenceElementType.First is not ParserElementType.List)
            {
                a = Build(context, sequence.First, sequenceElementType.First, listType.Element);
                a = new ParserSource.SingletonList(a);
            }
            else
            {
                a = Build(context, sequence.First, sequenceElementType.First, listType);
            }
            
            if (sequenceElementType.Second is not ParserElementType.List)
            {
                b = Build(context, sequence.Second, sequenceElementType.Second, listType.Element);
                b = new ParserSource.SingletonList(a);
            }
            else
            {
                b = Build(context, sequence.Second, sequenceElementType.Second, listType);
            }

            return new ParserSource.SequenceConcat(a, b);
        }
        
        throw new NotImplementedException();
    }
    
    private static ParserSource BuildZeroOrMore(ParserContext context, ParserElement.ZeroOrMore zeroOrMore, ParserElementType elementType, ParserTargetType targetType)
    {
        if (elementType is not ParserElementType.List listElementType)
        {
            throw new InvalidOperationException($"attempting to cast zero-or-more to non-list type {elementType}");
        }
        if (targetType is not ParserTargetType.List listTargetType)
        {
            throw new NotImplementedException();
        }

        var inner = Build(context, zeroOrMore.Rule, listElementType.Element, listTargetType.Element);

        return new ParserSource.ZeroOrMore(inner, listTargetType.Element);
    }
    
    private static ParserSource BuildOneOrMore(ParserContext context, ParserElement.OneOrMore oneOrMore, ParserElementType elementType, ParserTargetType targetType)
    {
        if (elementType is not ParserElementType.List listElementType)
        {
            throw new InvalidOperationException($"attempting to cast one-or-more to non-list type {elementType}");
        }
        if (targetType is not ParserTargetType.List listTargetType)
        {
            throw new NotImplementedException();
        }

        var inner = Build(context, oneOrMore.Rule, listElementType.Element, listTargetType.Element);

        return new ParserSource.OneOrMore(inner, listTargetType.Element);
    }
    
    private static ParserSource BuildOptional(ParserContext context, ParserElement.Optional optional, ParserElementType elementType, ParserTargetType targetType)
    {
        var inner = Build(context, optional.Rule, elementType, targetType);

        return new ParserSource.Optional(inner, targetType);
    }

    
    private static ParserSource BuildGroup(ParserContext context, ParserElement.Group group, ParserElementType elementType, ParserTargetType targetType)
    {
        return Build(context, group.Rule, elementType, targetType);
    }
    
    private static ParserSource BuildDiscard(ParserContext context, ParserElement.Discard discard, ParserElementType elementType, ParserTargetType targetType)
    {
        if (elementType is not ParserElementType.Void voidElementType)
        {
            throw new InvalidOperationException($"attempting to cast discard to non-void type {elementType}");
        }
        
        switch (targetType)
        {
            case ParserTargetType.Void:
            {
                var inner = Build(context, discard.Rule, voidElementType.Element, targetType);

                return new ParserSource.Discard(inner);
            }
            
            default:
                throw new NotImplementedException();
        }
    }
}