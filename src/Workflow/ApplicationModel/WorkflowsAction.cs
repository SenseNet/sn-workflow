namespace SenseNet.ApplicationModel
{
    public class WorkflowsAction : UrlAction
    {
        public override void Initialize(ContentRepository.Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            if (!context.IsContentListItem)
                this.Visible = false;
        }
    }
}
