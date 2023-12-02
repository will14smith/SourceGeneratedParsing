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
        var ruleReturns = indexedRules.ToDictionary(x => x.Key, x => GetCommonReturn(x.Select(r => (INamedTypeSymbol)r.Method.ReturnType))); 
        
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
                                    WriteParserRule(writer, parser.TokenType, rule, ruleReturns);
                                    
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

        // TODO report diagnostic
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

    private static void WriteParserRule(CodeWriter writer, INamedTypeSymbol tokenType, ParserRule rule, IReadOnlyDictionary<string, INamedTypeSymbol> nonTerminalTypes)
    {
        var counters = new Dictionary<string, int>();
        var stack = new Stack<IDisposable>();
        var argumentStack = new Stack<(string Variable, ArgumentType Type)>();
        
        WriteParseElement(rule.Element);

        var arguments = new List<string>();
        var arity = argumentStack.Count;

        var index = 0;
        foreach (var (variable, argumentType) in argumentStack.Reverse())
        {
            var parameterType = rule.Method.Parameters(arity)[index].Type;

            if (argumentType is ArgumentType.Token)
            {
                arguments.Add(SymbolEqualityComparer.Default.Equals(parameterType, tokenType)
                    ? $"{variable}.Type"
                    : variable);
            }
            else
            {
                // if param is list and arg is (T, List<T>) - do the prepend dance
                if (parameterType.AllInterfaces.Any(x => x.FullName() == "System.Collections.IEnumerable") && argumentType is ArgumentType.Group { Inner.Count: 2 } groupArgumentType)
                {
                    if (groupArgumentType.Inner[1] is ArgumentType.List listType && listType.Inner == groupArgumentType.Inner[0])
                    {
                        var prependedVariable = $"PrependedArgument{Increment("PrependedArgument")}";
                        writer.AppendLine($"var {prependedVariable} = new List<{groupArgumentType.Inner[0].Source()}> {{ {variable}.Item1 }};");
                        writer.AppendLine($"{prependedVariable}.AddRange({variable}.Item2);");
                        
                        arguments.Add(prependedVariable);
                    }
                    else
                    {
                        arguments.Add(variable);
                    }
                }
                else
                {
                    arguments.Add(variable);
                }
            }

            index++;
        }
        
        writer.AppendLine($"value = {rule.Method.Construct("_logic", arguments)};");

        writer.AppendLine($"_{rule.Name}Memo[lexerOriginal.Position] = (lexer.Position - lexerOriginal.Position, ParserResult.Success, value);");
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
                    if (terminal.String)
                    {
                        argumentStack.Push(($"new string({terminalVariable}.Span)", new ArgumentType.Token(true)));
                    }
                    else
                    {
                        argumentStack.Push((terminalVariable, new ArgumentType.Token(false)));
                    }
                    break;
                
                case ParserElement.NonTerminal nonTerminal:
                    var nonTerminalNumber = Increment(nonTerminal.NonTerminalName);
                    var nonTerminalVariablePrefix = $"{nonTerminal.NonTerminalName}{nonTerminalNumber}";
                    var nonTerminalVariable = $"{nonTerminalVariablePrefix}Value";
                    
                    writer.AppendLine($"var {nonTerminalVariablePrefix}Result = {NonTerminalMethodName(nonTerminal.NonTerminalName)}(ref lexer, out var {nonTerminalVariable});");
                    stack.Push(writer.AppendBlock($"if ({nonTerminalVariablePrefix}Result == ParserResult.Success)"));
                    argumentStack.Push((nonTerminalVariable, new ArgumentType.Type(nonTerminalTypes[nonTerminal.NonTerminalName])));
                    break;
                
                case ParserElement.Sequence sequence:
                    WriteParseElement(sequence.First);
                    WriteParseElement(sequence.Second);
                    break;
                
                case ParserElement.Discard discard:
                    WriteParseElement(discard.Rule);
                    argumentStack.Pop();
                    break;

                case ParserElement.ZeroOrMore zeroOrMore:
                    var zeroOrMoreNumber = Increment("ZeroOrMore");
                    var zeroOrMoreVariablePrefix = $"ZeroOrMore{zeroOrMoreNumber}";
                    var zeroOrMoreVariable = $"{zeroOrMoreVariablePrefix}Value";

                    var zeroOrMoreType = TypeOf(zeroOrMore.Rule, nonTerminalTypes); 
                    var zeroOrMoreTypeSource = zeroOrMoreType.Source(); 
                    using (writer.AppendBlock($"ParserResult {zeroOrMoreVariablePrefix}(ref Lexer lexer, out List<{zeroOrMoreTypeSource}> value)"))
                    {
                        var previousArgumentCount1 = argumentStack.Count;
                        var previousStackCount1 = stack.Count;
 
                        writer.AppendLine($"value = new List<{zeroOrMoreTypeSource}>();");

                        using (writer.AppendBlock("while(true)"))
                        {
                            writer.AppendLine("var originalLexer = lexer;");
                            
                            WriteParseElement(zeroOrMore.Rule);

                            var zeroOrMoreArgumentCount = argumentStack.Count - previousArgumentCount1;
                            var zeroOrMoreArguments = new List<(string Variable, ArgumentType Type)>();
                            for (int i = 0; i < zeroOrMoreArgumentCount; i++)
                            {
                                zeroOrMoreArguments.Insert(0, argumentStack.Pop());
                            }

                            writer.AppendLine($"value.Add(({string.Join(", ", zeroOrMoreArguments.Select(x => x.Variable))}));");
                            writer.AppendLine("continue;");

                            var zeroOrMoreStackCount = stack.Count - previousStackCount1;
                            for (int i = 0; i < zeroOrMoreStackCount; i++)
                            {
                                stack.Pop().Dispose();
                            }
                            
                            writer.AppendLine("lexer = originalLexer;");
                            writer.AppendLine("break;");
                        }
                        
                        writer.AppendLine("return ParserResult.Success;");
                    }
                    
                    writer.AppendLine($"var {zeroOrMoreVariablePrefix}Result = {zeroOrMoreVariablePrefix}(ref lexer, out var {zeroOrMoreVariable});");
                    stack.Push(writer.AppendBlock($"if ({zeroOrMoreVariablePrefix}Result == ParserResult.Success)"));
                    argumentStack.Push((zeroOrMoreVariable, new ArgumentType.List(zeroOrMoreType)));
                    
                    break;
                
                case ParserElement.Group group:
                    var previousArgumentCount = argumentStack.Count;
                    WriteParseElement(group.Rule);
                    
                    var groupNumber = Increment("Group");
                    var groupVariablePrefix = $"Group{groupNumber}";
                    var groupVariable = $"{groupVariablePrefix}Value";

                    var groupArgumentCount = argumentStack.Count - previousArgumentCount;
                    var groupArguments = new List<string>();
                    var groupArgumentTypes = new List<ArgumentType>();
                    for (var i = 0; i < groupArgumentCount; i++)
                    {
                        var (variable, type) = argumentStack.Pop();
                        groupArguments.Insert(0, variable);
                        groupArgumentTypes.Insert(0, type);
                    }
                    
                    writer.AppendLine($"var {groupVariable} = ({string.Join(", ", groupArguments)});");
                    argumentStack.Push((groupVariable, new ArgumentType.Group(groupArgumentTypes)));
                    
                    break;
                
                default:
                    writer.AppendLine($"throw new System.NotImplementedException(\"unsupported element {element.GetType().Name}\");");
                    break;
            }
        }
    }

    private static ArgumentType TypeOf(ParserElement rule, IReadOnlyDictionary<string, INamedTypeSymbol> nonTerminalTypes)
    {
        switch (rule)
        {
            case ParserElement.Terminal terminal: return new ArgumentType.Token(terminal.String);
            case ParserElement.NonTerminal nonTerminal: return new ArgumentType.Type(nonTerminalTypes[nonTerminal.NonTerminalName]);

            case ParserElement.Sequence sequence:
            {
                var firstType = TypeOf(sequence.First, nonTerminalTypes);
                var secondType = TypeOf(sequence.Second, nonTerminalTypes);

                if (firstType is ArgumentType.Void) return secondType;
                if (secondType is ArgumentType.Void) return firstType;

                return new ArgumentType.Group(new [] { firstType, secondType });
            }
            case ParserElement.Discard: return new ArgumentType.Void();
            case ParserElement.Group group: return TypeOf(group.Rule, nonTerminalTypes);
            
            default: throw new ArgumentOutOfRangeException(nameof(rule));
        }
    }

    private abstract record ArgumentType
    {
        public record Void : ArgumentType
        {
            public override string Source()
            {
                throw new NotImplementedException();
            }
        }

        public record Token(bool String) : ArgumentType
        {
            public override string Source()
            {
                return String ? "string" : "Token";
            }
        }

        public record Unknown : ArgumentType
        {
            public override string Source()
            {
                throw new NotImplementedException();
            }
        }

        public record Type(INamedTypeSymbol Symbol) : ArgumentType
        {
            public override string Source() => Symbol.FullName()!;
        }

        public record List(ArgumentType Inner) : ArgumentType
        {
            public override string Source()
            {
                throw new NotImplementedException();
            }
        }

        public record Group(IReadOnlyList<ArgumentType> Inner) : ArgumentType
        {
            public override string Source()
            {
                throw new NotImplementedException();
            }
        }

        public abstract string Source();
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
}