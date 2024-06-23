using Xunit.Abstractions;

namespace Temporal.Curriculum.Reads.Tests;

public class ConsoleWriter : StringWriter
{
    private ITestOutputHelper _output;

    public ConsoleWriter(ITestOutputHelper output)
    {
        this._output = output;
    }

    public override void WriteLine(string? value)
    {
        _output.WriteLine(value);
    }
}