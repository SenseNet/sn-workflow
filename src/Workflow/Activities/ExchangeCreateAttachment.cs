using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SenseNet.ContentRepository.Mail;
using SenseNet.ContentRepository.Storage;
using Microsoft.Exchange.WebServices.Data;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.Portal.Handlers;

namespace SenseNet.Workflow.Activities
{
    public class ExchangeCreateAttachment : AsyncCodeActivity<WfContent>
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

            var fileAttachment = attachment as FileAttachment;
            var name = fileAttachment.Name;
            
            // check existing file
            var node = Node.LoadNode(RepositoryPath.Combine(parentPath, name));
            
            var contentTypeName = UploadHelper.GetContentType(name, parentPath) ?? "File";
            File file;
            if (node == null)
            {
                // file does not exist, create new one
                file = CreateAttachment.CreateFileContent(contentTypeName, parent, name);
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
                        file = CreateAttachment.CreateFileContent(contentTypeName, parent, name);
                        if (file == null)
                            return null;
                    }

                    file.Name = name;
                }
                else
                {
                    // do not overwrite it
                    file = CreateAttachment.CreateFileContent(contentTypeName, parent, name);
                    if (file == null)
                        return null;

                    file.AllowIncrementalNaming = true;
                }
            }

            file.DisableObserver(typeof(WorkflowNotificationObserver));

            try
            {
                // also saves the content
                ExchangeHelper.SetAttachment(file, fileAttachment);
            }
            catch (Exception e)
            {
                SnLog.WriteException(e);
            }
            return new WfContent(file);
        }

        protected override void Cancel(AsyncCodeActivityContext context)
        {
            SnTrace.Workflow.Write("ExchangeCreateAttachment.Cancel");
            base.Cancel(context);
        }
    }
}
