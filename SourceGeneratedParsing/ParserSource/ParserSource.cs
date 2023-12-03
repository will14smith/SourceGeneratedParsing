using Microsoft.CodeAnalysis;
using SourceGeneratedParsing.Models;

namespace SourceGeneratedParsing.ParserSource;

public abstract class ParserSource
{
    public abstract ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer);
    
    public class Terminal : ParserSource
    {
        public INamedTypeSymbol TokenTypeSymbol { get; }
        public string Name { get; }
        public TerminalType Type { get; }

        public Terminal(INamedTypeSymbol tokenTypeSymbol, string name, TerminalType type)
        {
            TokenTypeSymbol = tokenTypeSymbol;
            Name = name;
            Type = type;
        }

        public enum TerminalType
        {
            String,
            TokenType,
            Token,
            Void
        }

        public override ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer)
        {
            var variable = context.AllocateVariable("token");
                    
            var postfix = writer.AppendBlock($"if (lexer.Next(out var {variable}) && {variable}.Type == {TokenTypeSymbol.FullName()}.{Name})");

            return Type switch
            {
                TerminalType.Void => new ParserSourceOutput(postfix, Array.Empty<string>()),
                
                TerminalType.String => new ParserSourceOutput(postfix, new[] { $"new string({variable}.Span)" }),
                TerminalType.TokenType => new ParserSourceOutput(postfix, new[] { $"{variable}.Type" }),
                TerminalType.Token => new ParserSourceOutput(postfix, new[] { variable }),
                
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    public class NonTerminal : ParserSource
    {
        public string Name { get; }
        public string ParseMethodName { get; }

        public NonTerminal(string name, string parseMethodName)
        {
            Name = name;
            ParseMethodName = parseMethodName;
        }

        public override ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer)
        {
            var resultVariable = context.AllocateVariable($"{Name}Result");
            var valueVariable = context.AllocateVariable($"{Name}Value");
            
            writer.AppendLine($"var {resultVariable} = {ParseMethodName}(ref lexer, out var {valueVariable});");
            var postfix = writer.AppendBlock($"if ({resultVariable} == ParserResult.Success)");

            return new ParserSourceOutput(postfix, new[] { valueVariable });
        }
    }
    
    public class Sequence : ParserSource
    {
        public ParserSource First { get; }
        public ParserSource Second { get; }

        public Sequence(ParserSource first, ParserSource second)
        {
            First = first;
            Second = second;
        }

        public override ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer)
        {
            var first=  First.WriteParse(context, writer);
            var second = Second.WriteParse(context, writer);

            var postfix = new DisposableAction(() =>
            {
                second.Dispose();
                first.Dispose();
            });

            return new ParserSourceOutput(postfix, first.Outputs.Concat(second.Outputs).ToArray());
        }
    }
    
    public class SequenceConcat : ParserSource
    {
        public ParserSource First { get; }
        public ParserSource Second { get; }

        public SequenceConcat(ParserSource first, ParserSource second)
        {
            First = first;
            Second = second;
        }

        public override ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer)
        {
            var first=  First.WriteParse(context, writer);
            var second = Second.WriteParse(context, writer);

            string variable;
            switch (first.Outputs.Count, second.Outputs.Count)
            {
                case (1, 1):
                    variable = context.AllocateVariable($"_{nameof(SequenceConcat)}");
                    writer.AppendLine($"var {variable} = System.Linq.Enumerable.Concat(({first.Outputs.Single()}), ({second.Outputs.Single()})).ToList();");
                    break;

                case (1, 0): variable = first.Outputs[0]; break;
                case (0, 1): variable = second.Outputs[0]; break;

                default: throw new NotImplementedException();
            }

            var postfix = new DisposableAction(() =>
            {
                second.Dispose();
                first.Dispose();
            });

            return new ParserSourceOutput(postfix, new [] { variable });
        }
    }


    public class Discard : ParserSource
    {
        public ParserSource Inner { get; }

        public Discard(ParserSource inner)
        {
            Inner = inner;
        }

        public override ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer)
        {
            var output = Inner.WriteParse(context, writer);

            return new ParserSourceOutput(output.Postfix, Array.Empty<string>());
        }
    }

    public class SingletonList : ParserSource
    {
        public ParserSource Inner { get; }

        public SingletonList(ParserSource inner)
        {
            Inner = inner;
        }

        public override ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer)
        {
            var inner = Inner.WriteParse(context, writer);

            var listVariable = context.AllocateVariable($"_{nameof(SingletonList)}");
            writer.AppendLine($"var {listVariable} = new [] {{ {inner.Outputs.Single()} }};");

            return new ParserSourceOutput(inner.Postfix, new[] { listVariable });
        }
    }

    public class ZeroOrMore : ParserSource
    {
        public ParserSource Inner { get; }
        public ParserTargetType InnerType { get; }

        public ZeroOrMore(ParserSource inner, ParserTargetType innerType)
        {
            Inner = inner;
            InnerType = innerType;
        }

        public override ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer)
        {
            var methodName = context.AllocateVariable($"_{nameof(ZeroOrMore)}");
            var innerType = TypeToSource(InnerType);
            using (writer.AppendBlock($"ParserResult {methodName}(ref Lexer lexer, out System.Collections.Generic.List<{innerType}> value)"))
            {
                writer.AppendLine($"value = new System.Collections.Generic.List<{innerType}>();");

                using (writer.AppendBlock("while(true)"))
                {
                    writer.AppendLine("var originalLexer = lexer;");
                            
                    var inner = Inner.WriteParse(context, writer);

                    writer.AppendLine($"value.Add(({string.Join(", ", inner.Outputs)}));");
                    writer.AppendLine("continue;");

                    inner.Dispose();
                            
                    writer.AppendLine("lexer = originalLexer;");
                    writer.AppendLine("break;");
                }
                        
                writer.AppendLine("return ParserResult.Success;");
            }

            var resultVariable = context.AllocateVariable($"_{nameof(ZeroOrMore)}Result");
            var valueVariable = context.AllocateVariable($"_{nameof(ZeroOrMore)}Value");
            
            writer.AppendLine($"var {resultVariable} = {methodName}(ref lexer, out var {valueVariable});");
            var postfix = writer.AppendBlock($"if ({resultVariable} == ParserResult.Success)");

            return new ParserSourceOutput(postfix, new[] { valueVariable });
        }
    }
    
    public class OneOrMore : ParserSource
    {
        public ParserSource Inner { get; }
        public ParserTargetType InnerType { get; }

        public OneOrMore(ParserSource inner, ParserTargetType innerType)
        {
            Inner = inner;
            InnerType = innerType;
        }

        public override ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer)
        {
            var methodName = context.AllocateVariable($"_{nameof(OneOrMore)}");
            var innerType = TypeToSource(InnerType);
            using (writer.AppendBlock($"ParserResult {methodName}(ref Lexer lexer, out System.Collections.Generic.List<{innerType}> value)"))
            {
                writer.AppendLine("var originalLexer = lexer;");

                writer.AppendLine($"value = new System.Collections.Generic.List<{innerType}>();");
                
                using (writer.AppendBlock("while(true)"))
                {
                    writer.AppendLine("var originalLexerInner = lexer;");

                    var inner = Inner.WriteParse(context, writer);

                    writer.AppendLine($"value.Add(({string.Join(", ", inner.Outputs)}));");
                    writer.AppendLine("continue;");

                    inner.Dispose();
                            
                    writer.AppendLine("lexer = originalLexerInner;");
                    writer.AppendLine("break;");
                }
                writer.AppendLine();
                
                using (writer.AppendBlock("if(value.Count == 0)"))
                {
                    writer.AppendLine("lexer = originalLexer;");
                    writer.AppendLine("return ParserResult.Failed;");
                }
                writer.AppendLine();

                writer.AppendLine("return ParserResult.Success;");
            }

            var resultVariable = context.AllocateVariable($"_{nameof(OneOrMore)}Result");
            var valueVariable = context.AllocateVariable($"_{nameof(OneOrMore)}Value");
            
            writer.AppendLine($"var {resultVariable} = {methodName}(ref lexer, out var {valueVariable});");
            var postfix = writer.AppendBlock($"if ({resultVariable} == ParserResult.Success)");

            return new ParserSourceOutput(postfix, new[] { valueVariable });
        }
    }
    
    public class Optional : ParserSource
    {
        public ParserSource Inner { get; }
        public ParserTargetType InnerType { get; }

        public Optional(ParserSource inner, ParserTargetType innerType)
        {
            Inner = inner;
            InnerType = innerType;
        }

        public override ParserSourceOutput WriteParse(ParserSourceContext context, CodeWriter writer)
        {
            var innerIsVoid = InnerType is ParserTargetType.Void;
            
            var lexerVariable = context.AllocateVariable($"_{nameof(Optional)}OriginalLexer");
            var resultVariable = context.AllocateVariable($"_{nameof(Optional)}Result");
            var valueVariable = context.AllocateVariable($"_{nameof(Optional)}Value");
            
            writer.AppendLine($"var {lexerVariable} = lexer;");
            writer.AppendLine($"var {resultVariable} = ParserResult.Failed;");
            if (!innerIsVoid)
            {
                writer.AppendLine($"{TypeToSource(InnerType)} {valueVariable} = default;");
            }

            var inner = Inner.WriteParse(context, writer);
            
            writer.AppendLine($"{resultVariable} = ParserResult.Success;");
            if (!innerIsVoid)
            {
                writer.AppendLine($"{valueVariable} = ({string.Join(", ", inner.Outputs)})");
            }

            inner.Dispose();

            using (writer.AppendBlock($"if({resultVariable} == ParserResult.Failed)"))
            {
                writer.AppendLine($"lexer = {lexerVariable};");
            }

            var outputs = innerIsVoid ? Array.Empty<string>() : new [] { valueVariable };
            return new ParserSourceOutput(new DisposableAction(() => { }), outputs);
        }
    }
    
    private static string TypeToSource(ParserTargetType type)
    {
        switch (type)
        {
            case ParserTargetType.String: return "string";
            
            case ParserTargetType.SymbolType symbolType: return symbolType.Symbol.Source()!;
            
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}