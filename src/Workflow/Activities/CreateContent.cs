using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Workflow.Activities.Design;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;

namespace SenseNet.Workflow.Activities
{
    [Designer(typeof(CreateContentDesigner))]
    public class CreateContent : NativeActivity<WfContent>
    {
        public InArgument<string> ParentPath { get; set; }
        public InArgument<string> ContentTypeName { get; set; }
        public InArgument<string> Name { get; set; }
        public InArgument<string> ContentDisplayName { get; set; }
        public InArgument<Dictionary<string,object>> FieldValues { get; set; }

        protected virtual string GetContentTypeName(NativeActivityContext context)
        {
            return ContentTypeName.Get(context);
        }
        protected virtual void SetContentFields(Content content, NativeActivityContext context)
        {
        }

        protected override void Execute(NativeActivityContext context)
        {
            var parent = Node.LoadNode(ParentPath.Get(context));
            if (parent == null)
                throw new ApplicationException("Cannot create content because parent does not exist. Path: " + ParentPath.Get(context));

            var name = Name.Get(context);
            var displayName = ContentDisplayName.Get(context);
            if (string.IsNullOrEmpty(name))
                name = ContentNamingProvider.GetNameFromDisplayName(displayName);

            var content = CreateContentInternal(GetContentTypeName(context), name, ParentPath.Get(context));
            if (!string.IsNullOrEmpty(displayName))
                content.DisplayName = displayName;

            var fieldValues = FieldValues.Get(context);
            if (fieldValues != null)
            {
                foreach (var key in fieldValues.Keys)
                {
                    content[key] = fieldValues[key];
                }
            }

            SetContentFields(content, context);

            content.ContentHandler.DisableObserver(typeof(WorkflowNotificationObserver));

            try
            {
                content.Save();
            }
            catch (Exception e)
            {
                throw new ApplicationException(String.Concat("Cannot create content. See inner exception. Expected path: "
                    , ParentPath.Get<string>(context), "/", Name.Get(context)), e);
            }

            Result.Set(context, new WfContent(content.ContentHandler));
        }

        protected static Content CreateContentInternal(string contentTypeName, string contentName, string parentPath)
        {
            if (string.IsNullOrEmpty(contentTypeName))
                throw new ArgumentNullException(nameof(contentTypeName));
            if (string.IsNullOrEmpty(parentPath))
                throw new ArgumentNullException(nameof(parentPath));

            var parentNode = Node.LoadNode(parentPath);
            if (parentNode == null)
                throw new ApplicationException($"Cannot create new content: invalid parent ({parentPath})");

            if (string.IsNullOrEmpty(contentName))
                contentName = contentTypeName;

            var contentType = ContentType.GetByName(contentTypeName);
            if (contentType == null)
            {
                // full template path is given as content type name
                return ContentTemplate.CreateTemplated(parentNode.Path, contentTypeName);
            }

            return Content.CreateNew(contentTypeName, parentNode, contentName);
        }

        protected override void Cancel(NativeActivityContext context)
        {
            SnTrace.Workflow.Write("CreateContent.Cancel: {0}", Name.Get(context));
            base.Cancel(context);
        }
    }
}
