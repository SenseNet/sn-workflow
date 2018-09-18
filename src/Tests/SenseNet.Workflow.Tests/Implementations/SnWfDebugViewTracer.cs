using System;
using System.Diagnostics;
using SenseNet.Diagnostics;

namespace SenseNet.Workflow.Tests.Implementations
{
    internal class SnWfDebugViewTracer : ISnTracer
    {
        public void Write(string line)
        {
            var p0 = line.IndexOf("\tA:", StringComparison.Ordinal);
            var p1 = line.IndexOf("\tT:", StringComparison.Ordinal);
            var result = $"{line.Substring(0, p0)}\tA:[removed]{line.Substring(p1)}";
            Trace.WriteLine(result, "SnTrace");
        }

        public void Flush() {/* do nothing */}
    }
}
