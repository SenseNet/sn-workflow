<%@  Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" EnableViewState="false" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>

<sn:ErrorView ID="ErrorView1" runat="server" />

<sn:GenericFieldControl ID="GenericFields2" runat="server" FieldsOrder="Description" />

<% var ass = this.Content.ContentHandler.GetReference<User>("AssignedTo");
   if (ass != null)
   { %>
   <div class="sn-inputunit ui-helper-clearfix" id="Div1">
    <div class="sn-iu-label">
        <span class="sn-iu-title"><%=this.Content.Fields["AssignedTo"].DisplayName%></span>
    </div>
    <div class="sn-iu-control">
        
     <a href='<%= Actions.ActionUrl(SenseNet.ContentRepository.Content.Create(ass), "Profile") %>' title='<%=ass.FullName %>'><%=ass.FullName%></a>
    </div>
</div>
   <% } %>

   <% var rejResult = this.Content["Result"] as List<string>;
      if (rejResult != null && rejResult.FirstOrDefault() == "no")
   { %>
   <div class="sn-inputunit ui-helper-clearfix" id="Div2">
    <div class="sn-iu-label">
        <span class="sn-iu-title"><%=GetGlobalResourceObject("Workflow", "RejectReason")%></span>
    </div>

    <div class="sn-iu-control">
       <sn:LongText ID="RejectReason" runat="server" FieldName="RejectReason"/>
    </div>
</div>

<% } %>

<div class="sn-panel sn-buttons">
<% var status = this.Content.ContentHandler.GetProperty<string>("Result");
   if (status == null)
   {
       %>
    <asp:Button CssClass="sn-submit" Text="<%$ Resources:Workflow,Approve %>" ID="ApproveRegistration" runat="server" OnClick="Click" CommandName="Approve" />
    <sn:RejectButton ID="RejectButton" runat="server"/>
    <% 
   }
   else
   { %>
   <%=GetGlobalResourceObject("Workflow", "TaskAlredyCompleted")%><strong> <sn:DropDown runat="server" ID="ResultField" FieldName="Result" RenderMode="Browse" /></strong>
    <% } %>
    <sn:BackButton CssClass="sn-submit" Text="<%$ Resources:Workflow,Cancel %>" ID="BackButton1" runat="server" />
</div>

<script runat="server">

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        this.RejectButton.OnReject += OnReject;
    }
    
    protected virtual void Click(object sender, EventArgs e)
    {
        string actionName = "";
        IButtonControl button = sender as IButtonControl;
        if (button != null)
            actionName = button.CommandName;

        if (!string.IsNullOrEmpty(actionName))
        {
            switch (actionName)
            {
                case "Approve": 
                    this.Content["Result"] = "yes";
                    this.Content["Status"] = "completed";
                    this.Content.Save(); 
                    this.RedirectToParent(); 
                    return;
            }
        }

        base.Click(sender, e);
    }

    protected void OnReject(object sender, VersioningActionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Comments))
            return;

        this.Content["Result"] = "no";
        this.Content["Status"] = "completed";
        this.Content["RejectReason"] = e.Comments;
        this.Content.Save();
        this.RedirectToParent();
    }

</script>
