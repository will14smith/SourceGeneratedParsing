namespace SourceGeneratedParsing.ParserSource;

public class ParserSourceContext
{
    private readonly Dictionary<string, int> _counters = new();

    public string AllocateVariable(string tag)
    {
        if (_counters.TryGetValue(tag, out var counter))
        {
            counter++;
        }
        else
        {
            counter = 1;
        }

        _counters[tag] = counter;
        return $"{tag}{counter}";
    }
}