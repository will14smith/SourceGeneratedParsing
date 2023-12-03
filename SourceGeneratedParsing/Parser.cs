using System.Text;
using Microsoft.CodeAnalysis.Text;
using SourceGeneratedParsing.Models;
using SourceGeneratedParsing.ParserSource;

namespace SourceGeneratedParsing;

public static class Parser
{
    public static SourceText Build(ParserDescriptor parser)
    {
        var context = ParserContext.Build(parser);
        
        var writer = new CodeWriter();

        using (writer.AppendBlock($"namespace {parser.ParserType.FullNamespace()}"))
        {
            using (writer.AppendBlock($"public partial class {parser.ParserType.Name}"))
            {
                WriteParserResultEnum(writer);
                writer.AppendLine();
                
                using (writer.AppendBlock($"private class ParserImplementation"))
                {
                    WriteParserFields(context, writer);

                    foreach (var nonTerminal in context.NonTerminalsInDefinitionOrder)
                    {
                        WriteNonTerminalParseMethod(context, writer, nonTerminal);
                    }
                }

                WritePublicParseMethods(context, writer);
            }
        }

        return SourceText.From(writer.ToString(), Encoding.UTF8);
    }
    
    private static void WriteParserResultEnum(CodeWriter writer)
    {
        using (writer.AppendBlock("public enum ParserResult"))
        {
            writer.AppendLine("Success,");
            writer.AppendLine("Failed,");
        }
    }
    
    private static void WriteParserFields(ParserContext context, CodeWriter writer)
    {
        // fields
        writer.AppendLine($"private readonly {context.ParserSymbol.FullName()} _logic;");
        writer.AppendLine();

        // memo
        foreach (var nonTerminal in context.NonTerminalsInDefinitionOrder)
        {
            writer.AppendLine(
                $"private readonly Dictionary<int, (int Size, ParserResult Result, {context.NonTerminalTypes[nonTerminal]} Value)> _{nonTerminal}Memo = new();");
        }

        writer.AppendLine();

        // constructor
        using (writer.AppendBlock($"public ParserImplementation({context.ParserSymbol.FullName()} logic)"))
        {
            writer.AppendLine("_logic = logic;");
        }
    }
    
    private static void WriteNonTerminalParseMethod(ParserContext context, CodeWriter writer, string nonTerminal)
    {
        writer.AppendLine();

        var returnType = context.NonTerminalTypes[nonTerminal];

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
            foreach (var rule in context.NonTerminalsByName[nonTerminal])
            {
                using (writer.AppendBlock($"// alternative {counter++}: {rule.Element}"))
                {
                    WriteParseRuleSection(context, writer, rule);

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

    private static void WriteParseRuleSection(ParserContext context, CodeWriter writer, ParserRule rule)
    {
        var typed = TypedParserElement.FromRule(context, rule);
        var source = ParserSourceBuilder.Build(context, typed);
        var output = source.WriteParse(new ParserSourceContext(), writer);

        writer.AppendLine($"value = {rule.Method.Construct("_logic", output.Outputs)};");
        writer.AppendLine($"_{rule.Name}Memo[lexerOriginal.Position] = (lexer.Position - lexerOriginal.Position, ParserResult.Success, value);");
        writer.AppendLine("return ParserResult.Success;");

        output.Dispose();
    }

    private static void WritePublicParseMethods(ParserContext context, CodeWriter writer)
    {
        foreach (var nonTerminal in context.NonTerminalsInDefinitionOrder)
        {
            writer.AppendLine();

            var returnType = context.NonTerminalTypes[nonTerminal];
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

    private static string NonTerminalMethodName(string nonTerminal) => char.ToUpper(nonTerminal[0]) + nonTerminal.Substring(1);
}