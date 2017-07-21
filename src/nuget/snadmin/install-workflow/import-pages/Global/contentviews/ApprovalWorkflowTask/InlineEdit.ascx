<%@  Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" EnableViewState="false" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>

<sn:ErrorView ID="ErrorView1" runat="server" />

<% var doc = this.Content.ContentHandler.GetReference<GenericContent>("ContentToApprove");
    if (doc != null) {
        var docc = SenseNet.ContentRepository.Content.Create(doc);
        var parentc = SenseNet.ContentRepository.Content.Create(doc.Parent);
        %>

<div class="sn-inputunit ui-helper-clearfix" id="InputUnitPanel1">
    <div class="sn-iu-label">
        <asp:Label CssClass="sn-iu-title" ID="LabelForTitle" runat="server"><%=this.Content.Fields["ContentToApprove"].DisplayName %></asp:Label>
    </div>
    <div class="sn-iu-control">
        <%= IconHelper.RenderIconTag(doc.Icon)%>
        <a href='<%= Actions.BrowseUrl(docc) %>' title='<%=docc.DisplayName %>'><%=docc.DisplayName %></a> (<%=GetGlobalResourceObject("Workflow", "In")%> 
        <a href='<%= Actions.BrowseUrl(parentc) %>' title='<%=parentc.Path %>' target="_blank"><%=parentc.DisplayName%></a><%=GetGlobalResourceObject("Workflow", "InH")%>)
     
    </div>
</div>
   <% } %>

 <sn:GenericFieldControl ID="GenericFields2" runat="server" FieldsOrder="DueDate" />

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
       var gc = this.Content.ContentHandler.GetReference<GenericContent>("ContentToApprove");
       if (gc != null && SavingAction.HasApprove(gc))
       {%>
    <asp:Button CssClass="sn-submit" Text="<%$ Resources:Workflow,Approve %>" ID="Approve" runat="server" OnClick="Click" CommandName="Approve" />
    <sn:RejectButton ID="RejectButton" runat="server"/>
    <% } else { %>
            <%=GetGlobalResourceObject("Workflow", "CannotBeApproved")%>
           
           <%
       }
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
                    using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
                    {
                        this.Content["Result"] = "yes";
                        this.Content["Status"] = "completed";
                        this.Content.Save();
                    }
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

        using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
        {
            this.Content["Result"] = "no";
            this.Content["Status"] = "completed";
            this.Content["RejectReason"] = e.Comments;
            this.Content.Save();
        }
        this.RedirectToParent();
    }

</script>
