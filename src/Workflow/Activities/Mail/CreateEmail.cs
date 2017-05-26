using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Workflow.Activities
{
    public class CreateEmail : NativeActivity
    {
        public InArgument<string> ParentPath { get; set; }
        public InArgument<MailMessage> Message { get; set; }
        public InArgument<bool> OverwriteExistingContent { get; set; }
        public InArgument<string> ContentDisplayName { get; set; }
        public InArgument<string> ContentName { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            var message = Message.Get(context);
            var parentPath = ParentPath.Get(context);
            var overwrite = OverwriteExistingContent.Get(context);
            var displayName = ContentDisplayName.Get(context);
            var name = ContentName.Get(context);
            if (string.IsNullOrEmpty(name))
                name = ContentNamingProvider.GetNameFromDisplayName(displayName) + ".eml";

            var parent = Node.LoadNode(parentPath);
            if (parent == null)
                throw new ApplicationException("Cannot create content because parent does not exist. Path: " + parentPath);

            // check existing file
            var node = Node.LoadNode(RepositoryPath.Combine(parentPath, name));
            File file;
            if (node == null)
            {
                // file does not exist, create new one
                file = new File(parent);
                if (!string.IsNullOrEmpty(displayName))
                    file.DisplayName = displayName;
                file.Name = name;
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
                        try
                        {
                            node.ForceDelete();
                        }
                        catch
                        {
                            SnLog.WriteError("Mail processor workflow: content could not be deleted during saving the email. Path: " + node.Path);
                            return;
                        }

                        file = new File(parent);
                    }
                    file.DisplayName = displayName;
                    file.Name = name;
                }
                else
                {
                    // do not overwrite it
                    file = new File(parent);
                    if (!string.IsNullOrEmpty(displayName))
                        file.DisplayName = displayName;
                    file.Name = name;
                    file.AllowIncrementalNaming = true;
                }
            }

            try
            {
                var binaryData = new BinaryData() { FileName = name };
                binaryData.SetStream(RepositoryTools.GetStreamFromString(message.Body));
                
                file.Binary = binaryData;
                file.Save();
                
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }
        }
    }
}
