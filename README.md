# SourceGeneratedParsing

I hadn't seen any libraries generating parsers using C# Source Generators so I decided to give it a go

The example project has some examples of what is supported currently

## Notes

- Currently using a very basic lexing system, just tries to match a bunch of regexes sequentially until it hits the first match
- Parser is a subset of a [PEG](https://en.wikipedia.org/wiki/Parsing_expression_grammar) [Packrat parser](https://en.wikipedia.org/wiki/Packrat_parser), currently only terminals/non-terminals/sequences are supported (and ordered choice through multiple definitions)

## Future stuff

- More PEG support, probably +/*/? operators would be nice to have (with the iterative combinator [transformation](https://en.wikipedia.org/wiki/Packrat_parser#Iterative_combinator) for performance), also supporting [left recursion](https://dl.acm.org/doi/10.1145/1328408.1328424) might be nice
- Much better error handling in the source generator, currently if you step out of what's expected it'll explode with no warning, there are a couple of examples of using diagnostics that could be expanded
- Making the lexer/tokentype concept optional, e.g. productions could reference chars/regex directly
- Look at switching to an incremental source generator 