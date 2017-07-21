using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using Repo = SenseNet.ContentRepository;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Workflow
{
    [Serializable]
    public class WfContent
    {
        public WfContent() { }
        public WfContent(Node n) { Path = n.Path; Id = n.Id; }
        public WfContent(string path) { Path = path; }

        public string Path { get; set; }

        [NonSerialized]
        private int __id;
        public int Id
        {
            get
            {
                if (__id == 0)
                {
                    var nodeHead = NodeHead.Get(Path);
                    if (nodeHead != null)
                        __id = nodeHead.Id;
                }
                return __id;
            }
            set
            {
                __id = value;
            }
        }

        [NonSerialized]
        private DateTime? __creationDate;
        public DateTime CreationDate
        {
            get
            {
                if (!__creationDate.HasValue)
                {
                    var nodeHead = NodeHead.Get(Path);
                    if (nodeHead != null)
                        __creationDate = nodeHead.CreationDate;
                }
                return __creationDate.Value;
            }
        }

        public WfReference Reference
        {
            get
            {
                return new WfReference(Path);
            }
        }

        public WfContentCollection References(string fieldName)
        {
            return new WfContentCollection(Path, fieldName);
        }

        public object this[string fieldName]
        {
            get
            {
                var content = Repo.Content.Load(Path);
                if (content == null)
                    throw new ContentNotFoundException(Path);

                Field field;
                if(content.Fields.TryGetValue(fieldName, out field))
                {
                    var value = content[fieldName];
                    var listofstring = value as IEnumerable<string>;
                    if (listofstring != null)
                        value = string.Join(",", listofstring);
                    return value;
                }

                var gcontent = content.ContentHandler as GenericContent;
                if (gcontent != null)
                    return gcontent.GetProperty(fieldName);

                throw new ApplicationException(String.Format("Field '{0}' not found in a {1} content: {2} ", fieldName, content.ContentType.Name, content.Path));
            }
            set
            {
                Retrier.Retry(3, 10, typeof(NodeIsOutOfDateException), () =>
                {
                    var content = Repo.Content.Load(Path);
                    if (content != null)
                    {
                        content[fieldName] = value;
                        content.ContentHandler.DisableObserver(typeof(WorkflowNotificationObserver));
                        content.Save();
                    }
                    else
                    {
                        // the content does not exist any more
                        SnLog.WriteWarning($"The content could not be loaded: {Path}");
                    }
                    //TODO: WF: Write back the timestamp (if the content is the relatedContent)
                });
            }
        }

        public string ActionUrl(string ActionName)
        {
            var node = Node.LoadNode(Path);
            var content = Repo.Content.Create(node);
            return ActionFramework.GetAction(ActionName, content, null, null).Uri;
        }

        public override string ToString()
        {
            return Path;
        }

        public object GetField(string fieldName)
        {
            return this[fieldName];
        }

        public void Delete()
        {
            Node.ForceDelete(this.Id);
        }
        public void SetPermission(IUser user, PermissionType permissionType, PermissionValue permissionValue)
        {
            var node = Node.LoadNode(Path);
            if(node != null)
                SecurityHandler.CreateAclEditor().SetPermission(node.Id, user.Id, false, permissionType, permissionValue).Apply();
        }

    }
}
