namespace Temporal.Curriculum.Workflows.Tests;

using Xunit.Abstractions;

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