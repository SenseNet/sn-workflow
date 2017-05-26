using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.Workflow
{
    public enum WorkflowDeletionStrategy { DeleteWhenCompleted, DeleteWhenCompletedOrAborted, AlwaysKeep }

    [ContentHandler]
    public class WorkflowDefinitionHandler : File
    {
        public WorkflowDefinitionHandler(Node parent) : this(parent, null) { }
        public WorkflowDefinitionHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected WorkflowDefinitionHandler(NodeToken nt) : base(nt) { }

        public const string ABORTONRELATEDCONTENTCHANGE = "AbortOnRelatedContentChange";
        [RepositoryProperty(ABORTONRELATEDCONTENTCHANGE, RepositoryDataType.Int)]
        public bool AbortOnRelatedContentChange
        {
            get { return base.GetProperty<int>(ABORTONRELATEDCONTENTCHANGE) != 0; }
            set { base.SetProperty(ABORTONRELATEDCONTENTCHANGE, value ? 1 : 0); }
        }

        public const string DELETEINSTANCEAFTERFINISHED = "DeleteInstanceAfterFinished";
        [RepositoryProperty(DELETEINSTANCEAFTERFINISHED, RepositoryDataType.String)]
        public WorkflowDeletionStrategy DeleteInstanceAfterFinished
        {
            get
            {
                var result = WorkflowDeletionStrategy.DeleteWhenCompleted;
                var enumVal = base.GetProperty<string>(DELETEINSTANCEAFTERFINISHED);
                if (string.IsNullOrEmpty(enumVal))
                    return result;
                Enum.TryParse(enumVal, false, out result);
                return result;
            }
            set
            {
                this[DELETEINSTANCEAFTERFINISHED] = Enum.GetName(typeof(WorkflowDeletionStrategy), value);
            }
        }

        [RepositoryProperty(VERSIONINGMODE, RepositoryDataType.Int)]
        public override VersioningType VersioningMode
        {
            get
            {
                return VersioningType.MajorOnly;
            }
            set
            {
                SnLog.WriteWarning("MajorOnly versioning is compulsory for WorkflowDefinition objects, ignoring change on node " + this.Path);
                base.VersioningMode = VersioningType.MajorOnly;
            }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case VERSIONINGMODE:
                    return VersioningMode;
                case ABORTONRELATEDCONTENTCHANGE:
                    return AbortOnRelatedContentChange;
                case DELETEINSTANCEAFTERFINISHED:
                    return DeleteInstanceAfterFinished;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case VERSIONINGMODE:
                    VersioningMode = (VersioningType)value;
                    break;
                case ABORTONRELATEDCONTENTCHANGE:
                    AbortOnRelatedContentChange = (bool)value;
                    break;
                case DELETEINSTANCEAFTERFINISHED:
                    DeleteInstanceAfterFinished = (WorkflowDeletionStrategy)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}
