using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using System.Reflection;
using System.Diagnostics;
using SenseNet.Diagnostics;
using System.ComponentModel;

namespace SenseNet.Workflow.Activities
{
    public class CreateUser : CreateContent
    {
        protected override string GetContentTypeName(NativeActivityContext context)
        {
            var baseContentTypeName = typeof(User).Name;
            var contentTypeName = ContentTypeName.Get(context);
            if (String.IsNullOrEmpty(contentTypeName))
                return baseContentTypeName;

            var ct = ContentType.GetByName(contentTypeName);
            if (ct != null)
            {
                if (ct.IsDescendantOf(ContentType.GetByName(baseContentTypeName)))
                    return contentTypeName;
                throw new InvalidOperationException("Invalid content type: " + contentTypeName);
            }

            throw new InvalidOperationException("Unknown content type: " + contentTypeName);
        }
        protected override void SetContentFields(Content content, NativeActivityContext context)
        {
            try
            {
                var stateContent = GetStateContent(context);
                var user = (User)content.ContentHandler;
                var method = typeof(User).GetMethod("SetCreationDate", BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(user, new object[] { stateContent.CreationDate });
                var hash = (string)stateContent.GetField("PasswordHash");

                content["FullName"] = stateContent.GetField("FullName");
                content["Email"] = stateContent.GetField("Email");
                content["Password"] = new SenseNet.ContentRepository.Fields.PasswordField.PasswordData { Hash = hash };
                content["Enabled"] = true;
            }
            catch (Exception e)
            {
                SnTrace.Workflow.WriteError("SetContentFields error: {0}", e);
                SnLog.WriteException(e);
                throw;
            }
        }
        protected WfContent GetStateContent(NativeActivityContext context)
        {
            var props = context.DataContext.GetProperties();
            WfContent result = null;
            foreach (PropertyDescriptor prop in props)
            {
                var propName = prop.Name;
                if (propName == "StateContent" && prop.PropertyType == typeof(WfContent))
                {
                    result = (WfContent)prop.GetValue(context.DataContext); ;
                    break;
                }
            }
            return result;
        }
    }
}
