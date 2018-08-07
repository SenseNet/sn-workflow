using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using System.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using System.Activities.XamlIntegration;
using System.Reflection;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Events;

namespace SenseNet.Workflow
{
    [Flags]
    public enum TriggerEvent { Published, Created, Changed }

    public enum WorkflowStatusEnum { Created = 0, Running = 1, Aborted = 2, Completed = 3 }

    [ContentHandler]
    public class WorkflowHandlerBase : GenericContent
    {
        public WorkflowHandlerBase(Node parent) : this(parent, null) { }
        public WorkflowHandlerBase(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected WorkflowHandlerBase(NodeToken nt) : base(nt) { }

        public const string WORKFLOWSTATUS = "WorkflowStatus";
        [RepositoryProperty(WORKFLOWSTATUS, RepositoryDataType.String)]
        public WorkflowStatusEnum WorkflowStatus
        {
            get
            {
                var result = WorkflowStatusEnum.Created;
                var enumVal = base.GetProperty<string>(WORKFLOWSTATUS);
                if (string.IsNullOrEmpty(enumVal))
                    return result;

                Enum.TryParse(enumVal, false, out result);

                return result;
            }
            set
            {
                this[WORKFLOWSTATUS] = Enum.GetName(typeof(WorkflowStatusEnum), value);
            }
        }

        public const string WORKFLOWDEFINITIONVERSION = "WorkflowDefinitionVersion";
        [RepositoryProperty(WORKFLOWDEFINITIONVERSION, RepositoryDataType.String)]
        public string WorkflowDefinitionVersion
        {
            get { return (string)base.GetProperty(WORKFLOWDEFINITIONVERSION); }
            internal set { base.SetProperty(WORKFLOWDEFINITIONVERSION, value); }
        }

        public const string WORKFLOWINSTANCEGUID = "WorkflowInstanceGuid";
        [RepositoryProperty(WORKFLOWINSTANCEGUID, RepositoryDataType.String)]
        public string WorkflowInstanceGuid
        {
            get { return (string)base.GetProperty(WORKFLOWINSTANCEGUID); }
            internal set { base.SetProperty(WORKFLOWINSTANCEGUID, value); }
        }

        public const string RELATEDCONTENT = "RelatedContent";
        [RepositoryProperty(RELATEDCONTENT, RepositoryDataType.Reference)]
        public Node RelatedContent
        {
            get { return base.GetReference<Node>(RELATEDCONTENT); }
            set
            {
                if (value != null)
                    this.RelatedContentTimestamp = value.NodeTimestamp;
                base.SetReference(RELATEDCONTENT, value);
            }
        }

        public const string RELATEDCONTENTTIMESTAMP = "RelatedContentTimestamp";
        [RepositoryProperty(RELATEDCONTENTTIMESTAMP, RepositoryDataType.Currency)]
        public long RelatedContentTimestamp
        {
            get
            {
                var value = base.GetProperty(RELATEDCONTENTTIMESTAMP);
                if (value == null)
                    return 0;
                return Convert.ToInt64(value);
            }
            internal set { base.SetProperty(RELATEDCONTENTTIMESTAMP, Convert.ToDecimal(value)); }
        }

        public const string SYSTEMMESSAGES = "SystemMessages";
        [RepositoryProperty(SYSTEMMESSAGES, RepositoryDataType.Text)]
        public string SystemMessages
        {
            get { return base.GetProperty<string>(SYSTEMMESSAGES); }
            internal set { base.SetProperty(SYSTEMMESSAGES, value); }
        }

        public const string ALLOWMANUALSTART = "AllowManualStart";
        [RepositoryProperty(ALLOWMANUALSTART, RepositoryDataType.Int)]
        public bool AllowManualStart
        {
            get { return base.GetProperty<int>(ALLOWMANUALSTART) != 0; }
            set { base.SetProperty(ALLOWMANUALSTART, value ? 1 : 0); }
        }

        public const string AUTOSTARTONPUBLISHED = "AutostartOnPublished";
        [RepositoryProperty(AUTOSTARTONPUBLISHED, RepositoryDataType.Int)]
        public bool AutostartOnPublished
        {
            get { return base.GetProperty<int>(AUTOSTARTONPUBLISHED) != 0; }
            set { base.SetProperty(AUTOSTARTONPUBLISHED, value ? 1 : 0); }
        }

        public const string AUTOSTARTONCREATED = "AutostartOnCreated";
        [RepositoryProperty(AUTOSTARTONCREATED, RepositoryDataType.Int)]
        public bool AutostartOnCreated
        {
            get { return base.GetProperty<int>(AUTOSTARTONCREATED) != 0; }
            set { base.SetProperty(AUTOSTARTONCREATED, value ? 1 : 0); }
        }

        public const string AUTOSTARTONCHANGED = "AutostartOnChanged";
        [RepositoryProperty(AUTOSTARTONCHANGED, RepositoryDataType.Int)]
        public bool AutostartOnChanged
        {
            get { return base.GetProperty<int>(AUTOSTARTONCHANGED) != 0; }
            set { base.SetProperty(AUTOSTARTONCHANGED, value ? 1 : 0); }
        }

        public const string CONTENTWORKFLOW = "ContentWorkflow";
        [RepositoryProperty(CONTENTWORKFLOW, RepositoryDataType.Int)]
        public bool ContentWorkflow
        {
            get { return base.GetProperty<int>(CONTENTWORKFLOW) != 0; }
            set { base.SetProperty(CONTENTWORKFLOW, value ? 1 : 0); }
        }

        public const string ABORTONRELATEDCONTENTCHANGE = "AbortOnRelatedContentChange";
        [RepositoryProperty(ABORTONRELATEDCONTENTCHANGE, RepositoryDataType.Int)]
        public bool AbortOnRelatedContentChange
        {
            get { return base.GetProperty<int>(ABORTONRELATEDCONTENTCHANGE) != 0; }
            set { base.SetProperty(ABORTONRELATEDCONTENTCHANGE, value ? 1 : 0); }
        }

        public string WorkflowTypeName
        {
            get { return NodeType.Name; }
        }

        public string WorkflowHostType
        {
            get
            {
                if (string.IsNullOrEmpty(WorkflowDefinitionVersion))
                    return null;

                return WorkflowTypeName + "-" + WorkflowDefinitionVersion;
            }
        }

        public bool WorkflowStarted
        {
            get
            {
                return !(string.IsNullOrEmpty(WorkflowInstanceGuid));
            }
        }

        public bool WorkflowRunnable
        {
            get
            {
                return !WorkflowStarted && Node.Exists(GetWorkflowDefinitionPath());
            }
        }

        public override bool IsTrashable
        {
            get
            {
                return false;
            }
        }

        public override VersioningType VersioningMode
        {
            get
            {
                return VersioningType.None;
            }
            set { }
        }

        public override ApprovingType ApprovingMode
        {
            get
            {
                return ApprovingType.False;
            }
            set { }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "VersioningMode":
                    return this.VersioningMode;
                case "ApprovingMode":
                    return this.ApprovingMode;
                case WORKFLOWSTATUS:
                    return this.WorkflowStatus;
                case WORKFLOWDEFINITIONVERSION:
                    return this.WorkflowDefinitionVersion;
                case WORKFLOWINSTANCEGUID:
                    return this.WorkflowInstanceGuid;
                case RELATEDCONTENT:
                    return RelatedContent;
                case RELATEDCONTENTTIMESTAMP:
                    return RelatedContentTimestamp;
                case SYSTEMMESSAGES:
                    return SystemMessages;
                case ALLOWMANUALSTART:
                    return AllowManualStart;
                case AUTOSTARTONPUBLISHED:
                    return AutostartOnPublished;
                case AUTOSTARTONCREATED:
                    return AutostartOnCreated;
                case AUTOSTARTONCHANGED:
                    return AutostartOnChanged;
                case CONTENTWORKFLOW:
                    return ContentWorkflow;
                case ABORTONRELATEDCONTENTCHANGE:
                    return AbortOnRelatedContentChange;
                case "WorkflowTypeName":
                    return this.WorkflowTypeName;
                case "WorkflowHostType":
                    return this.WorkflowHostType;
                case "WorkflowStarted":
                    return this.WorkflowStarted;
                case "WorkflowRunnable":
                    return this.WorkflowRunnable;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "VersioningMode":
                    this.VersioningMode = (VersioningType)value;
                    break;
                case "ApprovingMode":
                    this.ApprovingMode = (ApprovingType)value;
                    break;
                case WORKFLOWSTATUS:
                    this.WorkflowStatus = (WorkflowStatusEnum)value;
                    break;
                case WORKFLOWDEFINITIONVERSION:
                    this.WorkflowDefinitionVersion = (string)value;
                    break;
                case WORKFLOWINSTANCEGUID:
                    this.WorkflowInstanceGuid = (string)value;
                    break;
                case RELATEDCONTENT:
                    this.RelatedContent = (Node)value;
                    break;
                case RELATEDCONTENTTIMESTAMP:
                    this.RelatedContentTimestamp = (long)value;
                    break;
                case SYSTEMMESSAGES:
                    this.SystemMessages = (string)value;
                    break;
                case ALLOWMANUALSTART:
                    this.AllowManualStart = (bool)value;
                    break;
                case AUTOSTARTONPUBLISHED:
                    this.AutostartOnPublished = (bool)value;
                    break;
                case AUTOSTARTONCREATED:
                    this.AutostartOnCreated = (bool)value;
                    break;
                case AUTOSTARTONCHANGED:
                    this.AutostartOnChanged = (bool)value;
                    break;
                case CONTENTWORKFLOW:
                    this.ContentWorkflow = (bool)value;
                    break;
                case ABORTONRELATEDCONTENTCHANGE:
                    this.AbortOnRelatedContentChange = (bool)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        public string GetWorkflowDefinitionPath()
        {
            var wfDefinitionName = String.Concat(this.NodeType.Name, ".xaml");
            return System.IO.Path.Combine(Repository.WorkflowDefinitionPath, wfDefinitionName);
        }

        private WorkflowDefinitionHandler LoadWorkflowDefinition(out string version)
        {
            if (string.IsNullOrEmpty(WorkflowDefinitionVersion))
            {
                var def = Node.Load<WorkflowDefinitionHandler>(GetWorkflowDefinitionPath());
                version = def?.Version?.VersionString ?? string.Empty;
                return def;
            }

            version = WorkflowDefinitionVersion;
            return Node.Load<WorkflowDefinitionHandler>(GetWorkflowDefinitionPath(), VersionNumber.Parse(WorkflowDefinitionVersion));
        }


        internal Activity CreateWorkflowInstance()
        {
            string version;
            return CreateWorkflowInstance(out version);
        }

        internal Activity CreateWorkflowInstance(out string version)
        {
            var ns = SenseNet.Configuration.Workflow.NativeWorkflowNamespace;
            if (!string.IsNullOrEmpty(ns))
            {
                var asm = Assembly.LoadWithPartialName(ns);
                if (asm == null)
                {
                    SnTrace.Workflow.WriteError($"Assembly not found by namespace: {ns}");
                    version = string.Empty;
                    return null;
                }

                var cn = ns + "." + WorkflowTypeName;
                var act = asm.CreateInstance(cn);
                version = asm.GetName().Version.ToString();
                return act as Activity;
            }

            // elevation: this is a system function, we need to do this 
            // regardless of the current users permissions
            using (new SystemAccount())
            {
                var workflowDefinition = LoadWorkflowDefinition(out version);
                if (workflowDefinition == null)
                {
                    SnTrace.Workflow.WriteError($"CreateWorkflowInstance: Workflow definition not found: {GetWorkflowDefinitionPath()}");
                    return null;
                }

                var settings = new ActivityXamlServicesSettings { CompileExpressions = true };
                return ActivityXamlServices.Load(workflowDefinition.Binary.GetStream(), settings); 
            }
        }

        public virtual Dictionary<string, object> CreateParameters()
        {
            return new Dictionary<string, object>();
        }

        public override void Delete()
        {
            SnTrace.Workflow.Write("WorkflowHandlerBase.Delete: {0}, {1}, {2}", Id, WorkflowInstanceGuid, Path);
            if (WorkflowStatus == WorkflowStatusEnum.Running)
                InstanceManager.Abort(this, WorkflowApplicationAbortReason.StateContentDeleted);
            if (!TrashBin.IsInTrash(this))
                base.Delete();
        }
        public override void ForceDelete()
        {
            SnTrace.Workflow.Write("WorkflowHandlerBase.ForceDelete: {0}, {1}, {2}", Id, WorkflowInstanceGuid, Path);
            if (WorkflowStatus == WorkflowStatusEnum.Running)
                InstanceManager.Abort(this, WorkflowApplicationAbortReason.StateContentDeleted);
            if (Node.Exists(this.Path))
                base.ForceDelete();
        }

        public override void Save(NodeSaveSettings settings)
        {
            // copy abortonrelatedcontentchange info from definition to instance
            var workflowDefinition = Node.Load<WorkflowDefinitionHandler>(GetWorkflowDefinitionPath());
            if (workflowDefinition != null)
                this.AbortOnRelatedContentChange = workflowDefinition.AbortOnRelatedContentChange;

            // disconnect from underlying workflow instance
            var status = this.WorkflowStatus;
            if (status == WorkflowStatusEnum.Aborted || status == WorkflowStatusEnum.Completed)
                this.WorkflowInstanceGuid = Guid.Empty.ToString();

            // save
            base.Save(settings);
        }

        public virtual bool CanStartAutomatically(Node currentNode, TriggerEvent eventType)
        {
            return true;
        }

        protected override void OnModified(object sender, NodeEventArgs e)
        {
            SnTrace.Workflow.Write("WorkflowHandlerBase.OnModified: {0}, {1}, {2}", Id, WorkflowInstanceGuid, Path);
            base.OnModified(sender, e);
            if (e.ChangedData.Any(x => x.Name == WORKFLOWSTATUS))
            {
                // elevation: we need to act here anyway, regardless of the current users permissions
                using (new SystemAccount())
                {
                    string wfDefVer;
                    var wfDef = LoadWorkflowDefinition(out wfDefVer);
                    if (Node.Exists(this.Path) && !TrashBin.IsInTrash(this)
                        &&
                        (
                            (wfDef.DeleteInstanceAfterFinished == WorkflowDeletionStrategy.DeleteWhenCompleted && this.WorkflowStatus == WorkflowStatusEnum.Completed)
                            ||
                            (wfDef.DeleteInstanceAfterFinished == WorkflowDeletionStrategy.DeleteWhenCompletedOrAborted &&
                                (this.WorkflowStatus == WorkflowStatusEnum.Completed || this.WorkflowStatus == WorkflowStatusEnum.Aborted))
                        ))
                    {
                        ForceDelete();
                    } 
                }
            }
        }
    }
}
