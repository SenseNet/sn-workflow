using SenseNet.Search;

namespace SenseNet.Workflow
{
    public class SafeQueries : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "+TypeIs:Workflow +RelatedContent:@0 .AUTOFILTERS:OFF"</summary>
        public static string WorkflowsByRelatedContent { get { return "+TypeIs:Workflow +RelatedContent:@0 .AUTOFILTERS:OFF .QUICK"; } }
        /// <summary>Returns with the following query: "+TypeIs:User +Name:@0 .COUNTONLY .TOP:1"</summary>
        public static string UserCountByName { get { return "+TypeIs:User +Name:@0 .COUNTONLY .TOP:1"; } }

        /// <summary>Returns with the following query: "+TypeIs:Workflow +InFolder:@0 +AutostartOnCreated:yes .AUTOFILTERS:OFF"</summary>
        public static string WorkflowsAutostartWhenCreated { get { return "+TypeIs:Workflow +InFolder:@0 +AutostartOnCreated:yes .AUTOFILTERS:OFF .QUICK"; } }
        /// <summary>Returns with the following query: "+TypeIs:Workflow +InFolder:@0 +AutostartOnChanged:yes .AUTOFILTERS:OFF"</summary>
        public static string WorkflowsAutostartWhenChanged { get { return "+TypeIs:Workflow +InFolder:@0 +AutostartOnChanged:yes .AUTOFILTERS:OFF .QUICK"; } }
        /// <summary>Returns with the following query: "+TypeIs:Workflow +InFolder:@0 +(AutostartOnPublished:yes AutostartOnChanged:yes) .AUTOFILTERS:OFF"</summary>
        public static string WorkflowsAutostartWhenPublished { get { return "+TypeIs:Workflow +InFolder:@0 +(AutostartOnPublished:yes AutostartOnChanged:yes) .AUTOFILTERS:OFF .QUICK"; } }

        /// <summary>Returns with the following query: "+TypeIs:Workflow +@0:@1 .AUTOFILTERS:OFF"</summary>
        public static string WorkflowStateContent { get { return "+TypeIs:Workflow +@0:@1 .AUTOFILTERS:OFF"; } }
        /// <summary>Returns with the following query: "WorkflowStatus:@0 .AUTOFILTERS:OFF"</summary>
        public static string GetPollingInstances { get { return "WorkflowStatus:@0 .AUTOFILTERS:OFF"; } }
    }
}
