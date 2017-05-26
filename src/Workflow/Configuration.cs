using System;

namespace SenseNet.Workflow
{
    [Obsolete("After V6.5 PATCH 9: Use the Configuration.Workflow class instead.")]
    public class Configuration
    {
        [Obsolete("After V6.5 PATCH 9: Use Configuration.Workflow.TimerInterval instead.")]
        public static double TimerInterval => SenseNet.Configuration.Workflow.TimerInterval;
    }
}
