using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository;

namespace SenseNet.Workflow.Activities
{
    public class WaitForContainerChanged : NativeActivity<WfContent>
    {
        public InArgument<string> ContentPath { get; set; }

        public InArgument<bool> WaitForChildCreated { get; set; }
        public InArgument<bool> WaitForChildEdited { get; set; }
        public InArgument<bool> WaitForChildDeleted { get; set; }

        public OutArgument<string> NotificationType { get; set; }

        private Variable<int> notificationId = new Variable<int>("notificationId", 0);

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            metadata.AddImplementationVariable(notificationId);
        }

        protected override void Execute(NativeActivityContext context)
        {
            var bookMarkName = Guid.NewGuid().ToString();
            var content = new WfContent { Path = ContentPath.Get(context) };

            notificationId.Set(context, context.GetExtension<ContentWorkflowExtension>().RegisterWait(content, bookMarkName));

            context.CreateBookmark(bookMarkName, Continue, BookmarkOptions.MultipleResume);
        }

        private void Continue(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            var eventArgs = obj as WorkflowNotificationEventArgs;
            if (eventArgs == null)
                return;

            // Check two things: 
            //      1. if the wf author wanted to monitor this event type (create, etc.)
            //      2. if the current action has that event type

            var childCreated = GetOptionalBoolArgument(context, WaitForChildCreated, true) &&
                string.CompareOrdinal(eventArgs.NotificationType, WorkflowNotificationObserver.NotificationType.ChildCreated) == 0;
            var childEdited = GetOptionalBoolArgument(context, WaitForChildEdited, true) &&
                string.CompareOrdinal(eventArgs.NotificationType, WorkflowNotificationObserver.NotificationType.ChildEdited) == 0;
            var childDeleted = GetOptionalBoolArgument(context, WaitForChildDeleted, true) &&
                string.CompareOrdinal(eventArgs.NotificationType, WorkflowNotificationObserver.NotificationType.ChildDeleted) == 0;

            // do not do anything, continue waiting
            if (!childCreated && !childEdited && !childDeleted) 
                return;

            // remove the notification from the SN db
            InstanceManager.ReleaseWait(notificationId.Get(context));

            // this lets the workflow move on to the next activity
            context.RemoveBookmark(bookmark);

            // set result values: the child in question and the operation name
            Result.Set(context, new WfContent(eventArgs.Info as string));
            NotificationType.Set(context, eventArgs.NotificationType);
        }

        private static bool GetOptionalBoolArgument(ActivityContext context, Argument arg, bool defaultValue)
        {
            return arg.Expression == null ? defaultValue : arg.Get<bool>(context);
        }
    }
}
