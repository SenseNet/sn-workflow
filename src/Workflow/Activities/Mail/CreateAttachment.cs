using System;
using System.IO;
using System.Net.Mail;
using System.Activities;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using sn = SenseNet.ContentRepository;
using System.Diagnostics;
using SenseNet.Diagnostics;
using SenseNet.Portal.Handlers;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Workflow.Activities
{
    public class CreateAttachment : AsyncCodeActivity<WfContent>
    {
        public InArgument<string> ParentPath { get; set; }
        public InArgument<Attachment> Attachment { get; set; }
        public InArgument<bool> OverwriteExistingContent { get; set; }


        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var parentPath = ParentPath.Get(context);
            var attachment = Attachment.Get(context);
            var overwrite = OverwriteExistingContent.Get(context);

            var SaveAttachmentDelegate = new Func<string, Attachment, bool, WfContent>(SaveAttachment);
            context.UserState = SaveAttachmentDelegate;
            return SaveAttachmentDelegate.BeginInvoke(parentPath, attachment, overwrite, callback, state);
        }

        protected override WfContent EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var SaveAttachmentDelegate = (Func<string, Attachment, bool, WfContent>)context.UserState;
            return SaveAttachmentDelegate.EndInvoke(result);
        }

        private WfContent SaveAttachment(string parentPath, Attachment attachment, bool overwrite)
        {
            var parent = Node.LoadNode(parentPath);
            if (parent == null)
                throw new ApplicationException("Cannot create content because parent does not exist. Path: " + parentPath);

            var name = attachment.Name;
            if (string.IsNullOrEmpty(name))
                name = Guid.NewGuid().ToString();
            
            // check existing file
            var node = Node.LoadNode(RepositoryPath.Combine(parentPath, name));
            
            var contentTypeName = UploadHelper.GetContentType(name, parentPath) ?? "File";
            File file;
            if (node == null)
            {
                // file does not exist, create new one
                file = CreateFileContent(contentTypeName, parent, name);
                if (file == null)
                    return null;
            }
            else
            {
                // file exists
                if (overwrite)
                {
                    // overwrite it, so we open it
                    file = node as File;

                    // node exists and it is not a file -> delete it and create a new one
                    if (file == null)
                    {
                        node.ForceDelete();

                        file = CreateFileContent(contentTypeName, parent, name);
                        if (file == null)
                            return null;
                    }

                    file.Name = name;
                }
                else
                {
                    // do not overwrite it
                    file = CreateFileContent(contentTypeName, parent, name);
                    if (file == null)
                        return null;

                    file.AllowIncrementalNaming = true;
                }
            }

            file.DisableObserver(typeof(WorkflowNotificationObserver));

            try
            {
                attachment.ContentStream.Seek(0, SeekOrigin.Begin);
                
                var binaryData = new BinaryData() { FileName = attachment.Name };
                binaryData.SetStream(attachment.ContentStream);

                file.Binary = binaryData;
                file.Save();
            }
            catch (Exception e)
            {
                SnLog.WriteException(e);
            }
            return new WfContent(file);
        }

        protected override void Cancel(AsyncCodeActivityContext context)
        {
            SnTrace.Workflow.Write("CreateAttachment.Cancel");
            base.Cancel(context);
        }

        public static File CreateFileContent(string contentTypeName, Node parent, string name)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var fileContent = Content.CreateNew(contentTypeName, parent, name);
            if (!(fileContent.ContentHandler is File file))
            {
                SnLog.WriteWarning(SR.GetString(SR.Mail.Error_UnknownAttachmentType_3, contentTypeName, name, parent.Path), EventId.NotDefined);
                return null;
            }

            return file;
        }
    }
}
