using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGeneratedParsing.Models;

namespace SourceGeneratedParsing;

public class Parser
{
    public static SourceText Build(ParserDescriptor parser)
    {
        var nonTerminals = GetNonTerminals(parser);
        var indexedRules = parser.Rules.ToLookup(x => x.Name);
        var ruleReturns = indexedRules.ToDictionary(x => x.Key, x => GetCommonReturn(x.Select(r => r.Method.ReturnType))); 
        
        var writer = new CodeWriter();

        using (writer.AppendBlock($"namespace {parser.ParserType.FullNamespace()}"))
        {
            using (writer.AppendBlock($"public partial class {parser.ParserType.Name}"))
            {
                using (writer.AppendBlock("public enum ParserResult"))
                {
                    writer.AppendLine("Success,");
                    writer.AppendLine("Failed,");
                }
                writer.AppendLine();
                
                using (writer.AppendBlock($"private class ParserImplementation"))
                {
                    // fields
                    writer.AppendLine($"private readonly {parser.ParserType.FullName()} _logic;");
                    writer.AppendLine();
                    
                    // memo
                    foreach (var nonTerminal in nonTerminals)
                    {
                        writer.AppendLine($"private readonly Dictionary<int, (int Size, ParserResult Result, {ruleReturns[nonTerminal]} Value)> _{nonTerminal}Memo = new();");
                    }
                    writer.AppendLine();
                    
                    // constructor
                    using (writer.AppendBlock($"public ParserImplementation({parser.ParserType.FullName()} logic)"))
                    {
                        writer.AppendLine("_logic = logic;");
                    }

                    foreach (var nonTerminal in nonTerminals)
                    {
                        writer.AppendLine();

                        var returnType = ruleReturns[nonTerminal];
                        
                        using (writer.AppendBlock($"public ParserResult {NonTerminalMethodName(nonTerminal)}(ref Lexer lexer, out {returnType} value)"))
                        {
                            using (writer.AppendBlock($"if (_{nonTerminal}Memo.TryGetValue(lexer.Position, out var memo))"))
                            {
                                writer.AppendLine("lexer.Skip(memo.Size);");
                                writer.AppendLine("value = memo.Value;");
                                writer.AppendLine("return memo.Result;");
                            }
                            writer.AppendLine();

                            writer.AppendLine("var lexerOriginal = lexer;");
                            writer.AppendLine();
                            
                            var counter = 1;
                            foreach (var rule in indexedRules[nonTerminal])
                            {
                                using (writer.AppendBlock($"// alternative {counter++}: {rule.Element}"))
                                {
                                    WriteParserRule(writer, parser.TokenType, rule);
                                    
                                    writer.AppendLine();
                                    writer.AppendLine("lexer = lexerOriginal;");
                                }
                                writer.AppendLine();
                            }
                            
                            writer.AppendLine("value = default;");
                            writer.AppendLine($"_{nonTerminal}Memo[lexer.Position] = (0, ParserResult.Failed, value);");
                            writer.AppendLine("return ParserResult.Failed;");
                        }
                    }
                }

                foreach (var nonTerminal in nonTerminals)
                {
                    writer.AppendLine();
                    
                    var returnType = ruleReturns[nonTerminal];
                    var methodName = NonTerminalMethodName(nonTerminal);
                    
                    using (writer.AppendBlock($"public {returnType} Parse{methodName}(ref Lexer lexer)"))
                    {
                        writer.AppendLine("var impl = new ParserImplementation(this);");
                        writer.AppendLine();

                        writer.AppendLine($"var result = impl.{methodName}(ref lexer, out var value);");
                        using (writer.AppendBlock("if(result == ParserResult.Success)"))
                        {
                            writer.AppendLine("return value;");
                        }
                        writer.AppendLine();
                        
                        writer.AppendLine("throw new System.InvalidOperationException(\"Failed to parse\");");
                    }
                }
            }
        }

        return SourceText.From(writer.ToString(), Encoding.UTF8);
    }

    private static ITypeSymbol GetCommonReturn(IEnumerable<ITypeSymbol> types)
    {
        ITypeSymbol? current = null;

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

        // TODO report diagnostic
        return current ?? throw new InvalidOperationException("couldn't find base");
    }

    private static ITypeSymbol GetCommonReturn(ITypeSymbol a, ITypeSymbol b)
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

    private static IReadOnlyList<ITypeSymbol> GetBaseClasses(ITypeSymbol? a)
    {
        var types = new List<ITypeSymbol>();

        while (a != null)
        {
            types.Add(a);
            a = a.BaseType;
        }

        return types;
    }

    private static void WriteParserRule(CodeWriter writer, INamedTypeSymbol tokenType, ParserRule rule)
    {
        var counters = new Dictionary<string, int>();
        var stack = new Stack<IDisposable>();
        var argumentStack = new Stack<(string Variable, bool IsToken)>();
        
        WriteParseElement(rule.Element);

        var arguments = new List<string>();
        var arity = argumentStack.Count;

        var index = 0;
        foreach (var (variable, isToken) in argumentStack.Reverse())
        {
            if (isToken)
            {
                var parameterType = rule.Method.Parameters(arity)[index].Type;
                arguments.Add(SymbolEqualityComparer.Default.Equals(parameterType, tokenType)
                    ? $"{variable}.Type"
                    : variable);
            }
            else
            {
                arguments.Add(variable);
            }

            index++;
        }
        
        writer.AppendLine($"value = {rule.Method.Construct("_logic", arguments)};");

        writer.AppendLine("_expressionMemo[lexerOriginal.Position] = (lexer.Position - lexerOriginal.Position, ParserResult.Success, value);");
        writer.AppendLine("return ParserResult.Success;");
        
        while (stack.Count > 0)
        {
            stack.Pop().Dispose();
        }

        return;
        
        int Increment(string tag)
        {
            if (counters.TryGetValue(tag, out var counter))
            {
                counters[tag] = counter + 1;
                return counter + 1;
            }
            
            counters[tag] = 1;
            return 1;
        }
        
        void WriteParseElement(ParserElement element)
        {
            switch (element)
            {
                case ParserElement.Terminal terminal:
                    var terminalNumber = Increment("Lexer");
                    var terminalVariable = $"token{terminalNumber}";
                    
                    stack.Push(writer.AppendBlock($"if (lexer.Next(out var {terminalVariable}) && {terminalVariable}.Type == {tokenType.FullName()}.{terminal})"));
                    argumentStack.Push((terminalVariable, true));
                    break;
                
                case ParserElement.NonTerminal nonTerminal:
                    var nonTerminalNumber = Increment(nonTerminal.NonTerminalName);
                    var nonTerminalVariablePrefix = $"{nonTerminal.NonTerminalName}{nonTerminalNumber}";
                    var nonTerminalVariable = $"{nonTerminalVariablePrefix}Value";
                    
                    writer.AppendLine($"var {nonTerminalVariablePrefix}Result = {NonTerminalMethodName(nonTerminal.NonTerminalName)}(ref lexer, out var {nonTerminalVariable});");
                    stack.Push(writer.AppendBlock($"if ({nonTerminalVariablePrefix}Result == ParserResult.Success)"));
                    argumentStack.Push((nonTerminalVariable, false));
                    break;
                
                case ParserElement.Sequence sequence:
                    WriteParseElement(sequence.First);
                    WriteParseElement(sequence.Second);
                    break;
                
                case ParserElement.Discard discard:
                    WriteParseElement(discard.Rule);
                    argumentStack.Pop();
                    break;

                default:
                    writer.AppendLine($"throw new System.NotImplementedException(\"unsupported element {element.GetType().Name}\");");
                    break;
            }
        }
    }

    private static string NonTerminalMethodName(string nonTerminal) => char.ToUpper(nonTerminal[0]) + nonTerminal.Substring(1);

    private static IReadOnlyList<string> GetNonTerminals(ParserDescriptor parser)
    {
        var nonTerminals = new List<string>();
        
        var temp = new HashSet<string>();
        foreach (var rule in parser.Rules)
        {
            if (temp.Add(rule.Name))
            {
                nonTerminals.Add(rule.Name);
            }
        }

        return nonTerminals;
    }

    public static ParserElement ParseElement(string match)
    {
        // nonterminal: [a-z] [A-Za-z0-9]+
        // terminal: [A-Z] [A-Za-z0-9]+
        // atom: terminal | nonterminal
        // discard: [@]? atom
        // sequence: discard WS sequence
        
        var tokens = match.Split(' ');
        ParserElement? current = null;
        
        foreach (var token in tokens)
        {
            ParserElement tokenElement;
            
            if (token[0] == '@')
            {
                var actualToken = token.Substring(1);
                
                tokenElement = char.IsUpper(actualToken[0]) 
                    ? new ParserElement.Discard(new ParserElement.Terminal(actualToken)) 
                    : new ParserElement.Discard(new ParserElement.NonTerminal(actualToken));
            }
            else
            {
                tokenElement = char.IsUpper(token[0])
                    ? new ParserElement.Terminal(token)
                    : new ParserElement.NonTerminal(token);
            }

            current = current == null ? tokenElement : new ParserElement.Sequence(current, tokenElement);
        }

        return current ?? throw new InvalidOperationException("failed to parse");
    }
}