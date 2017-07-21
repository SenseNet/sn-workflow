using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal.UI;
using SenseNet.Portal.UI.Controls;
using SenseNet.Portal.UI.PortletFramework;
using SenseNet.Workflow;
using Content = SenseNet.ContentRepository.Content;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Portal.Portlets
{
    public class StartWorkflowPortlet : ContentAddNewPortlet
    {
        private const string StartWorkflowPortletClass = "StartWorkflowPortlet";

        // ========================================================================================= Properties

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(StartWorkflowPortletClass, "Prop_WorkflowType_DisplayName")]
        [LocalizedWebDescription(StartWorkflowPortletClass, "Prop_WorkflowType_Description")]
        [WebCategory(EditorCategory.Workflow, EditorCategory.Workflow_Order)]
        [WebOrder(10)]
        [Editor(typeof(DropDownPartField), typeof(IEditorPartField))]
        [DropDownPartOptions("+InTree:/Root/System/Schema/ContentTypes/GenericContent/Workflow -Name:Workflow")]
        public string WorkflowType { get; set; }

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(StartWorkflowPortletClass, "Prop_ConfirmContentPath_DisplayName")]
        [LocalizedWebDescription(StartWorkflowPortletClass, "Prop_ConfirmContentPath_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(20)]
        [Editor(typeof(ContentPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions()]
        public string ConfirmContentPath { get; set; }

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(StartWorkflowPortletClass, "Prop_StartOnCurrentContent_DisplayName")]
        [LocalizedWebDescription(StartWorkflowPortletClass, "Prop_StartOnCurrentContent_Description")]
        [WebCategory(EditorCategory.Workflow, EditorCategory.Workflow_Order)]
        [WebOrder(30)]
        public bool StartOnCurrentContent { get; set; }

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(StartWorkflowPortletClass, "Prop_WorkflowContainerPath_DisplayName")]
        [LocalizedWebDescription(StartWorkflowPortletClass, "Prop_WorkflowContainerPath_Description")]
        [WebCategory(EditorCategory.Workflow, EditorCategory.Workflow_Order)]
        [WebOrder(40)]
        [Editor(typeof(ContentPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions()]
        public string WorkflowContainerPath { get; set; }

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(StartWorkflowPortletClass, "Prop_WorkflowTemplatePath_DisplayName")]
        [LocalizedWebDescription(StartWorkflowPortletClass, "Prop_WorkflowTemplatePath_Description")]
        [WebCategory(EditorCategory.Workflow, EditorCategory.Workflow_Order)]
        [WebOrder(50)]
        [Editor(typeof(ContentPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions(AllowedContentTypes = "Workflow", TreeRoots = "/Root/ContentTemplates;/Root")]
        public string WorkflowTemplatePath { get; set; }

        // ========================================================================================= Controls

        private IButtonControl _button;
        protected IButtonControl StartWorkflowButton
        {
            get { return _button ?? (_button = this.FindControlRecursive("StartWorkflow") as IButtonControl); }
        }

        // ========================================================================================= Constructor

        public StartWorkflowPortlet()
        {
            this.Name = "$StartWorkflowPortlet:PortletDisplayName";
            this.Description = "$StartWorkflowPortlet:PortletDescription";

            this.Category = new PortletCategory(PortletCategoryType.Workflow);

            if (this.HiddenProperties == null)
                this.HiddenProperties = new List<string>();

            this.HiddenPropertyCategories = new List<string> { EditorCategory.Cache };
        }

        // ========================================================================================= Overrides

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            if (this.StartWorkflowButton != null)
                this.StartWorkflowButton.Click += StartWorkflowButton_Click;
        }
        
        protected override ContentView GetContentView(Content newContent)
        {
            if (!string.IsNullOrEmpty(ContentViewPath))
                return base.GetContentView(newContent);

            var contentList = ContentList.GetContentListByParentWalk(GetContextNode());
            if (contentList != null)
            {
                // try to find a content view at /Root/.../MyList/WorkflowTemplates/MyWorkflow.ascx
                var wfTemplatesPath = RepositoryPath.Combine(contentList.Path, "WorkflowTemplates");
                var viewPath = RepositoryPath.Combine(wfTemplatesPath, newContent.Name + ".ascx");

                if (Node.Exists(viewPath))
                    return ContentView.Create(newContent, Page, ViewMode.InlineNew, viewPath);

                // try to find it by type name, still locally
                viewPath = RepositoryPath.Combine(wfTemplatesPath, newContent.ContentType.Name + ".ascx");

                if (Node.Exists(viewPath))
                    return ContentView.Create(newContent, Page, ViewMode.InlineNew, viewPath);

                // last attempt: global view for the workflow type
                return ContentView.Create(newContent, Page, ViewMode.InlineNew, "StartWorkflow.ascx");
            }
            else
            {
                var viewPath = $"{RepositoryStructure.ContentViewFolderName}/{newContent.ContentType.Name}/{"StartWorkflow.ascx"}";
                string resolvedPath;
                if (!SkinManagerBase.TryResolve(viewPath, out resolvedPath))
                {
                    resolvedPath = RepositoryPath.Combine(RepositoryStructure.SkinGlobalFolderPath,
                                                          SkinManagerBase.TrimSkinPrefix(viewPath));

                    if (!Node.Exists(resolvedPath))
                        resolvedPath = string.Empty;
                }

                if (!string.IsNullOrEmpty(resolvedPath))
                    return ContentView.Create(newContent, Page, ViewMode.InlineNew, resolvedPath);
            }

            return base.GetContentView(newContent);
        }

        protected override string GetRequestedContentType()
        {
            // workflow from template
            if (!string.IsNullOrEmpty(this.WorkflowTemplatePath))
                return this.WorkflowTemplatePath;

            // workflow by type
            return !string.IsNullOrEmpty(this.WorkflowType) ? this.WorkflowType : base.GetRequestedContentType();
        }

        protected override string GetSelectedContentType()
        {
            // workflow from template
            if (!string.IsNullOrEmpty(this.WorkflowTemplatePath))
                return this.WorkflowTemplatePath;

            // workflow by type
            return !string.IsNullOrEmpty(this.WorkflowType) ? this.WorkflowType : base.GetSelectedContentType();
        }

        protected override string GetParentPath()
        {
            var wfContainer = this.WorkflowContainerPath;
            if (string.IsNullOrEmpty(wfContainer))
                return base.GetParentPath();

            if (!wfContainer.StartsWith("/Root/") && this.ContextNode != null)
            {
                wfContainer = RepositoryPath.Combine(this.ContextNode.Path, wfContainer);
            }

            return wfContainer;
        }

        protected virtual void ValidateWorkflow()
        {
            if (this.ContentView?.Content == null)
                return;

            var workflow = this.ContentView.Content.ContentHandler as WorkflowHandlerBase;
            var refNode = workflow?.RelatedContent;
            if (refNode == null)
                return;

            var contentList = ContentList.GetContentListByParentWalk(this.ContextNode);
            if (contentList == null)
                return;

            if (!workflow.Path.StartsWith(RepositoryPath.Combine(contentList.Path, "Workflows") + "/"))
                throw new InvalidOperationException(string.Format("Workflow must be under the list ({0})",
                    workflow.Path));

            if (!refNode.Path.StartsWith(contentList.Path + "/"))
                throw new InvalidOperationException(string.Format("Related content must be in the list ({0})",
                    refNode.Path));
        }

        protected override bool AllowCreationForEmptyAllowedContentTypes(string parentPath)
        {
            // startworkflow portlet uses custom type, so it does not rely upon allowed content types list
            // skip this check
            return true;
        }

        // ========================================================================================= Event handlers

        protected void StartWorkflowButton_Click(object sender, EventArgs e)
        {
            if (this.ContentView == null)
                return;

            this.ContentView.NeedToValidate = true;
            this.ContentView.UpdateContent();

            var content = this.ContentView.Content;

            if (this.ContentView.IsUserInputValid && content.IsValid)
            {
                try
                {
                    if (this.StartOnCurrentContent)
                    {
                        var workflow = content.ContentHandler as WorkflowHandlerBase;
                        if (workflow != null)
                        {
                            content.Fields["RelatedContent"].SetData(this.ContextNode);
                        }
                    }

                    ValidateWorkflow();

                    WorkflowHandlerBase wfContent;

                    // need to create workflow in elevated mode
                    using (new SystemAccount())
                    {
                        content.Fields["OwnerSiteUrl"].SetData(PortalContext.Current.RequestedUri.GetLeftPart(UriPartial.Authority));
                        content.Save();

                        //TODO: review this ... this is a temporary solution
                        wfContent = Node.Load<WorkflowHandlerBase>(content.Id);
                    }

                    // start workflow
                    InstanceManager.Start(wfContent);
                }
                catch (Exception ex)
                {
                    // cleanup: delete the instance if it was saved before the error
                    if (content.Id != 0)
                    {
                        using (new SystemAccount())
                        {
                            content.ForceDelete();
                        }
                    }

                    SnLog.WriteException(ex);
                    this.ContentView.ContentException = ex;

                    return;
                }

                if (!string.IsNullOrEmpty(ConfirmContentPath))
                {
                    // if confirm page or content is given, redirect there
                    var confirmContent = Content.Load(ConfirmContentPath);
                    var confirmBrowseAction = Helpers.Actions.BrowseUrl(confirmContent);

                    // If the user does not have enough permissions for the Browse action 
                    // in this subtree, but the target content is a page, than use its path.
                    if (string.IsNullOrEmpty(confirmBrowseAction) && confirmContent != null && confirmContent.ContentType.IsInstaceOfOrDerivedFrom("Page"))
                        confirmBrowseAction = PortalContext.GetSiteRelativePath(confirmContent.Path);

                    if (!string.IsNullOrEmpty(confirmBrowseAction))
                    {
                        HttpContext.Current.Response.Redirect(confirmBrowseAction, false);
                        return;
                    }
                }

                CallDone(false);
            }
        }
    }
}