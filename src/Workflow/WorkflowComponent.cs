using System;
using SenseNet.ContentRepository;

namespace SenseNet.Workflow
{
    public class WorkflowComponent : SnComponent
    {
        public override string ComponentId => "SenseNet.Workflow";
        public override Version SupportedVersion { get; } = new Version(7, 1, 0);
    }
}
