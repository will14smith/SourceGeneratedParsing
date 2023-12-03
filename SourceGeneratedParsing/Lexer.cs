using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGeneratedParsing.Models;

namespace SourceGeneratedParsing;

public class Lexer
{
    public static SourceText Build(INamedTypeSymbol parserTypeSymbol, LexerDescriptor lexer)
    {
        var writer = new CodeWriter();

        writer.AppendLine("using System;");
        writer.AppendLine("using System.Text.RegularExpressions;");
        writer.AppendLine();
        
        using (writer.AppendBlock($"namespace {parserTypeSymbol.FullNamespace()}"))
        {
            using (writer.AppendBlock($"public partial class {parserTypeSymbol.Name}"))
            {
                using (writer.AppendBlock($"public ref struct Lexer"))
                {
                    // regexes

                    for (var index = 0; index < lexer.Rules.Count; index++)
                    {
                        var lexerRule = lexer.Rules[index];
                        writer.AppendLine($"private static readonly Regex T{index} = new Regex({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral("^" + lexerRule.Regex, true)});");
                    }
                    writer.AppendLine();

                    // state
                    writer.AppendLine("private ReadOnlySpan<char> _input;");
                    writer.AppendLine("private int _offset;");
                    writer.AppendLine();

                    // constructor
                    using (writer.AppendBlock("public Lexer(ReadOnlySpan<char> input)"))
                    {
                        writer.AppendLine("_input = input;");
                        writer.AppendLine("_offset = 0;");
                    }
                    writer.AppendLine();
                   
                    // helpers
                    writer.AppendLine("public int Position => _offset;"); 
                    writer.AppendLine("public void Skip(int count) => _offset += count;");
                    writer.AppendLine();
                    
                    // lex
                    using (writer.AppendBlock("public bool Next(out Token token)"))
                    {
                        using (writer.AppendBlock("while (true)"))
                        {
                            writer.AppendLine("restart:");
                            using (writer.AppendBlock("if (_offset >= _input.Length)"))
                            {
                                writer.AppendLine("token = default;");
                                writer.AppendLine("return false;");
                            }
                            writer.AppendLine();
                            
                            writer.AppendLine("var input = _input[_offset..];");
                            writer.AppendLine();
                            
                            for (var index = 0; index < lexer.Rules.Count; index++)
                            {
                                var rule = lexer.Rules[index];
                                
                                using(writer.AppendBlock($"foreach (var match in T{index}.EnumerateMatches(input))"))
                                {
                                    if (!rule.Ignore)
                                    {
                                        writer.AppendLine($"token = new Token({lexer.TokenType.FullName()}.{rule.TokenName}, _input.Slice(match.Index + _offset, match.Length), _offset);");
                                        writer.AppendLine("_offset += match.Length;");
                                        writer.AppendLine("return true;");
                                    }
                                    else
                                    {
                                        writer.AppendLine("_offset += match.Length;");
                                        writer.AppendLine("goto restart;");
                                    }
                                }    
                                writer.AppendLine();
                            }
                            
                            writer.AppendLine("throw new InvalidOperationException($\"did not match any tokens at {_offset}: {new string(_input[_offset..])}\");");
                        }
                    }
                }
            }
        }
        
        return SourceText.From(writer.ToString(), Encoding.UTF8);
    }
}