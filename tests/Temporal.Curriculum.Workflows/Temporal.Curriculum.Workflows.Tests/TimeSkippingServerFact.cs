namespace Temporal.Curriculum.Workflows.Tests;
using System.Runtime.InteropServices;
using Xunit;

/// <summary>
/// The time-skipping test server can only run on x86/x64 currently.
/// </remarks>
public sealed class TimeSkippingServerFactAttribute : FactAttribute
{
    public TimeSkippingServerFactAttribute()
    {
        Console.WriteLine("process {0:G}", RuntimeInformation.ProcessArchitecture);
        // if (RuntimeInformation.ProcessArchitecture != Architecture.X86 &&
        //     RuntimeInformation.ProcessArchitecture != Architecture.X64)
        // {
        //     Skip = "Time-skipping test server only works on x86/x64 platforms";
        // }
    }
}