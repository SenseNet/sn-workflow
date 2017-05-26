using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository;
using SenseNet.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Search.Internal;

namespace SenseNet.Workflow
{
    public class WorkflowNotificationObserver : NodeObserver
    {
        public static class NotificationType
        {
            public static string ContentChanged = "ContentChanged";
            public static string ChildCreated = "ChildCreated";
            public static string ChildEdited = "ChildEdited";
            public static string ChildDeleted = "ChildDeleted";
        }

        protected override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            base.OnNodeCreated(sender, e);

            // notify workflows about parent change
            InstanceManager.NotifyMultipleContentChanged(CollectParentIds(e.SourceNode), NotificationType.ChildCreated, e.SourceNode.Path);

            StartWorkflowAutomatically(e.SourceNode, TriggerEvent.Created, null);
        }
        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            base.OnNodeModified(sender, e);

            // notify workflows about content change
            InstanceManager.NotifyContentChanged(new WorkflowNotificationEventArgs(e.SourceNode.Id, NotificationType.ContentChanged, null));

            // notify workflows about parent change
            InstanceManager.NotifyMultipleContentChanged(CollectParentIds(e.SourceNode), NotificationType.ChildEdited, e.SourceNode.Path);
            
            AbortRelatedWorkflows(e.SourceNode, WorkflowApplicationAbortReason.RelatedContentChanged);
            StartWorkflowAutomatically(e.SourceNode, TriggerEvent.Changed, e.ChangedData);
        }
        protected override void OnNodeCopied(object sender, NodeOperationEventArgs e)
        {
            base.OnNodeCopied(sender, e);
            StartWorkflowAutomatically(e.TargetNode, TriggerEvent.Created, null);
        }
        protected override void OnNodeDeletedPhysically(object sender, NodeEventArgs e)
        {
            base.OnNodeDeletedPhysically(sender, e);

            // notify workflows about parent change
            InstanceManager.NotifyMultipleContentChanged(CollectParentIds(e.SourceNode), NotificationType.ChildDeleted, e.SourceNode.Path);
        }

        private void AbortRelatedWorkflows(Node currentNode, WorkflowApplicationAbortReason reason)
        {
            IEnumerable<Node> nodes = null;
            if (RepositoryInstance.ContentQueryIsAllowed)
            {
                nodes = SenseNet.Search.ContentQuery.Query(SafeQueries.WorkflowsByRelatedContent, null, currentNode.Id).Nodes;
            }
            else
            {
                var nodeType = ActiveSchema.NodeTypes["Workflow"];
                nodes = SenseNet.ContentRepository.Storage.Search.NodeQuery.QueryNodesByReferenceAndType("RelatedContent", currentNode.Id, nodeType, false).Nodes;
            }

            foreach (WorkflowHandlerBase workflow in nodes)
                if (workflow.WorkflowStatus == WorkflowStatusEnum.Running && workflow.AbortOnRelatedContentChange)
                    InstanceManager.Abort(workflow, reason);
        }

        private void StartWorkflowAutomatically(Node currentNode, TriggerEvent triggerEvent, IEnumerable<ChangedData> changedData)
        {
            if (currentNode.ContentListId == 0)
                return;
            var gc = currentNode as GenericContent;
            triggerEvent = (gc.Approvable && gc.Version.Status == VersionStatus.Pending && IsNewVersion(changedData)) ? TriggerEvent.Published : triggerEvent;

            var templates = GetWorkflowTemplates(currentNode, triggerEvent);
            foreach (WorkflowHandlerBase wfTemplateNode in templates)
                if (wfTemplateNode.CanStartAutomatically(currentNode, triggerEvent))
                    StartWorkflow(wfTemplateNode, currentNode);
        }
        private static Node[] GetWorkflowTemplates(Node currentNode, TriggerEvent triggerEvent)
        {
            var listPath = NodeHead.Get(currentNode.ContentListId).Path;
            var templatesPath = RepositoryPath.Combine(listPath, "WorkflowTemplates");
            Node[] templates;
            if (RepositoryInstance.ContentQueryIsAllowed)
            {
                string query = null;
                switch (triggerEvent)
                {
                    case TriggerEvent.Created: query = SafeQueries.WorkflowsAutostartWhenCreated; break;
                    case TriggerEvent.Changed: query = SafeQueries.WorkflowsAutostartWhenChanged; break;
                    case TriggerEvent.Published: query = SafeQueries.WorkflowsAutostartWhenPublished; break;
                    default:
                        throw new SnNotSupportedException("Unkown TriggerEvent: " + triggerEvent);
                }
                var result = ContentQuery.Query(query, null, templatesPath);
                templates = result.Nodes.ToArray();
            }
            else
            {
                QueryPropertyData[] propData;
                switch (triggerEvent)
                {
                    case TriggerEvent.Created:
                        // fieldClause = "+AutostartOnCreated:yes";
                        propData = new[] { new QueryPropertyData { PropertyName = "AutostartOnCreated", QueryOperator = Operator.Equal, Value = 1 } };
                        break;
                    case TriggerEvent.Changed:
                        // fieldClause = "+AutostartOnChanged:yes";
                        propData = new[] { new QueryPropertyData { PropertyName = "AutostartOnChanged", QueryOperator = Operator.Equal, Value = 1 } };
                        break;
                    case TriggerEvent.Published:
                        // fieldClause = "+(AutostartOnPublished:yes AutostartOnChanged:yes)";
                        propData = new[] { new QueryPropertyData { PropertyName = "AutostartOnPublished", QueryOperator = Operator.Equal, Value = 1 } };
                        break;
                    default:
                        throw new SnNotSupportedException("Unkown TriggerEvent: " + triggerEvent);
                }
                var queryPropertyData = new List<QueryPropertyData>(propData);
                var nodeType = ActiveSchema.NodeTypes["Workflow"];
                templates = NodeQuery.QueryNodesByTypeAndPathAndProperty(nodeType, false, templatesPath, false, queryPropertyData).Nodes.ToArray();
                if (templates.Length == 0 && triggerEvent == TriggerEvent.Published)
                {
                    propData = new[] { new QueryPropertyData { PropertyName = "AutostartOnChanged", QueryOperator = Operator.Equal, Value = 1 } };
                    templates = NodeQuery.QueryNodesByTypeAndPathAndProperty(nodeType, false, templatesPath, false, queryPropertyData).Nodes.ToArray();
                }
            }
            return templates;
        }

        private bool IsNewVersion(IEnumerable<ChangedData> changedData)
        {
            if (changedData != null)
                foreach (var c in changedData)
                    if (c.Name == "Version")
                        return true;
            return false;
        }
        private void StartWorkflow(WorkflowHandlerBase wfTemplate, Node currentNode)
        {
            var list = (ContentList)currentNode.LoadContentList();
            var targetFolder = list.GetWorkflowContainer(); // Node.LoadNode(targetFolderPath);

            var wfInstance = (WorkflowHandlerBase)ContentTemplate.CreateTemplated(targetFolder, wfTemplate, wfTemplate.Name).ContentHandler;

            wfInstance.RelatedContent = currentNode;
            if (!ValidateWorkflow(wfInstance, currentNode))
            {
                wfInstance.Save();
                return;
            }

            wfInstance["OwnerSiteUrl"] = PortalContext.Current.RequestedUri.GetLeftPart(UriPartial.Authority);
            using (new SystemAccount())
            {
                // We need to save the wf instance before the engine starts it to have everything persisted to the repo. 
                // Please do not remove this line.
                wfInstance.Save();
            }

            InstanceManager.Start(wfInstance);
        }
        protected virtual bool ValidateWorkflow(WorkflowHandlerBase wfContent, Node currentNode)
        {
            return true;
        }

        protected static int[] CollectParentIds(Node node)
        {
            var pids = new List<int>();
            var parentHead = (node != null && node.ParentId > 0) ? NodeHead.Get(node.ParentId) : null;

            while (parentHead != null)
            {
                pids.Add(parentHead.Id);
                parentHead = parentHead.ParentId > 0 ? NodeHead.Get(parentHead.ParentId) : null;
            }

            return pids.ToArray();
        }
    }
}
