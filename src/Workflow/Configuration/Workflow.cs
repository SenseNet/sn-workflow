// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Workflow : SnConfig
    {
        private const string SectionName = "sensenet/workflow";

        public static double TimerInterval { get; internal set; } = GetDouble(SectionName, "TimerInterval", 10.0);
        public static string NativeWorkflowNamespace { get; internal set; } = GetString(SectionName, "NativeWorkflowNamespace");
    }
}
