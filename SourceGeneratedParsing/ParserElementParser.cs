using SourceGeneratedParsing.Models;

namespace SourceGeneratedParsing;

public class ParserElementParser
{
    private enum TokenType
    {
        NonTerminalIdentifier,
        TerminalIdentifier,
        LeftParen,
        RightParen,
        At,
        Star,
        Plus,
        Question,
        Pipe,
        WhiteSpace,
    }
    
    private ref struct Token
    {
        public Token(TokenType type, ReadOnlySpan<char> span)
        {
            Type = type;
            Span = span;
        }

        public TokenType Type { get; }
        public ReadOnlySpan<char> Span { get; }
    }
    
    private ref struct Lexer
    {
        private readonly ReadOnlySpan<char> _input;
        private int _offset;

        public Lexer(ReadOnlySpan<char> input)
        {
            _input = input;
            _offset = 0;
        }
        
        public bool Next(out Token token)
        {
            while (true)
            {
                restart:
                if (_offset >= _input.Length)
                {
                    token = default;
                    return false;
                }
                
                var input = _input.Slice(_offset);

                if (input[0] == '$' || char.IsUpper(input[0]))
                {
                    var len = 1;
                    while(len < input.Length && char.IsLetterOrDigit(input[len])) { len++; }
                    _offset += len;
                    
                    token = new Token(TokenType.TerminalIdentifier, input.Slice(0, len));
                    return true;
                }
                
                if (char.IsLower(input[0]))
                {
                    var len = 1;
                    while (len < input.Length && char.IsLetterOrDigit(input[len])) { len++; }
                    _offset += len;

                    token = new Token(TokenType.NonTerminalIdentifier, input.Slice(0, len));
                    return true;
                }

                if (input[0] == '(')
                {
                    _offset++;
                    token = new Token(TokenType.LeftParen, input.Slice(0, 1));
                    return true;
                }
                if (input[0] == ')')
                {
                    _offset++;
                    token = new Token(TokenType.RightParen, input.Slice(0, 1));
                    return true;
                }
                if (input[0] == '@')
                {
                    _offset++;
                    token = new Token(TokenType.At, input.Slice(0, 1));
                    return true;
                }
                if (input[0] == '*')
                {
                    _offset++;
                    token = new Token(TokenType.Star, input.Slice(0, 1));
                    return true;
                }
                if (input[0] == '+')
                {
                    _offset++;
                    token = new Token(TokenType.Plus, input.Slice(0, 1));
                    return true;
                }
                if (input[0] == '?')
                {
                    _offset++;
                    token = new Token(TokenType.Question, input.Slice(0, 1));
                    return true;
                }
                if (input[0] == '|')
                {
                    _offset++;
                    token = new Token(TokenType.Pipe, input.Slice(0, 1));
                    return true;
                }

                if (char.IsWhiteSpace(input[0]))
                {
                    var len = 1;
                    while (len < input.Length && char.IsWhiteSpace(input[len])) { len++; }
                    _offset += len;

                    goto restart;
                }

                unsafe
                {
                    fixed (char* ptr = _input)
                    {
                        var remaining = new string(ptr, _offset, _input.Length - _offset);
                        throw new InvalidOperationException($"did not match any tokens at {_offset}: {remaining}");
                    }
                }
            }
        }
    }

    // nonterminal: [a-z] [A-Za-z0-9]+
    // terminal: [A-Z] [A-Za-z0-9]+
    // atom: terminal | nonterminal
    // group: '(' element ')' | atom
    // discard: '@'? group
    // repeats: discard [*+?]?
    // sequence: repeats sequence?
    // choice: sequence ('|' choice)?
    // element: choice
    public static ParserElement Parse(string input)
    {
        var lexer = new Lexer(input.AsSpan());
        return ParseElement(ref lexer);
    }

    private static ParserElement ParseElement(ref Lexer lexer) => ParseChoice(ref lexer);

    private static ParserElement ParseChoice(ref Lexer lexer)
    {
        var left = ParseSequence(ref lexer);

        var originalLexer = lexer;
        if (!lexer.Next(out var token) || token.Type != TokenType.Pipe)
        {
            lexer = originalLexer;
            return left;
        }

        var right = ParseChoice(ref lexer);

        return new ParserElement.Choice(left, right);
    }

    private static ParserElement ParseSequence(ref Lexer lexer)
    {
        var left = ParseRepeats(ref lexer);

        var originalLexer = lexer;
        if (!lexer.Next(out var token) || token.Type is not (TokenType.At or TokenType.LeftParen or TokenType.TerminalIdentifier or TokenType.NonTerminalIdentifier))
        {
            lexer = originalLexer;
            return left;
        }

        lexer = originalLexer;
        
        var right = ParseSequence(ref lexer);

        return new ParserElement.Sequence(left, right);
    }
    
    private static ParserElement ParseRepeats(ref Lexer lexer)
    {
        var left = ParseDiscard(ref lexer);

        var originalLexer = lexer;
        if (!lexer.Next(out var token))
        {
            lexer = originalLexer;
            return left;
        }

        if (token.Type == TokenType.Star)
        {
            return new ParserElement.ZeroOrMore(left);
        }

        if (token.Type == TokenType.Plus)
        {
            return new ParserElement.OneOrMore(left);
        }

        if (token.Type == TokenType.Question)
        {
            return new ParserElement.Optional(left);
        }
        
        lexer = originalLexer;
        return left;
    }

    private static ParserElement ParseDiscard(ref Lexer lexer)
    {
        var originalLexer = lexer;
        
        if (!lexer.Next(out var token) || token.Type != TokenType.At)
        {
            lexer = originalLexer;
            return ParseGroup(ref lexer);
        }

        return new ParserElement.Discard(ParseGroup(ref lexer));
    }

    private static ParserElement ParseGroup(ref Lexer lexer)
    {
        var originalLexer = lexer;
        
        if (!lexer.Next(out var token) || token.Type != TokenType.LeftParen)
        {
            lexer = originalLexer;
            return ParseAtom(ref lexer);
        }

        var inner = ParseElement(ref lexer);
        
        if (!lexer.Next(out token))
        {
            throw new InvalidOperationException($"expected {TokenType.RightParen}, but got EOF");
        }

        if (token.Type != TokenType.RightParen)
        {
            throw new InvalidOperationException($"expected {TokenType.RightParen}, but got {token.Type}");
        }

        return new ParserElement.Group(inner);
    }

    private static ParserElement ParseAtom(ref Lexer lexer)
    {
        if (!lexer.Next(out var token))
        {
            throw new InvalidOperationException($"expected atom, but got EOF");
        }

        if (token.Type == TokenType.TerminalIdentifier)
        {
            unsafe
            {
                fixed (char* ptr = token.Span)
                {
                    if (token.Span[0] == '$')
                    {
                        var identifier = new string(ptr, 1, token.Span.Length - 1);
                        return new ParserElement.Terminal(identifier, true);
                    }
                    else
                    {
                        var identifier = new string(ptr, 0, token.Span.Length);
                        return new ParserElement.Terminal(identifier, false);
                    }
                }
            }
        }
        
        if (token.Type == TokenType.NonTerminalIdentifier)
        {
            unsafe
            {
                fixed (char* ptr = token.Span)
                {
                    var identifier = new string(ptr, 0, token.Span.Length);
                    return new ParserElement.NonTerminal(identifier);
                }
            }
        }
        
        throw new InvalidOperationException($"expected atom, but got {token.Type}");
    }
}