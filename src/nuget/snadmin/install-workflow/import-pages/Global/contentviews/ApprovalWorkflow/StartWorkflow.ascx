<%@ Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage" %>
<%@ Import Namespace="SenseNet.Portal" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>

<div class="sn-content sn-workflow sn-content-inlineview">

        <%
            var FirtsLevelApprover = this.Content["FirstLevelApprover"] as IEnumerable<Node>;
            var firstTfVal = TimeSpan.Parse(this.Content["FirstLevelTimeFrame"] as string ?? string.Empty);
            string FirstLevelTimeFrame = firstTfVal.TotalHours > 0 ? SNSR.GetString("$Workflow,ApproveTimeFrame", firstTfVal.TotalHours) : string.Empty;
            var SecondLevelApprover = this.Content["SecondLevelApprover"] as IEnumerable<Node>;
            var secondTfVal = TimeSpan.Parse(this.Content["SecondLevelTimeFrame"] as string ?? string.Empty);
            string SecondLevelTimeFrame = secondTfVal.TotalHours > 0 ? SNSR.GetString("$Workflow,ApproveTimeFrame", secondTfVal.TotalHours) : string.Empty;
        %>

        <h2 class="sn-content-title"><%= SenseNet.Portal.UI.IconHelper.RenderIconTag(this.Icon, null, 32) %><%=GetGlobalResourceObject("Workflow", "Start")%> <strong><%= (this.Content.ContentHandler as GenericContent).DisplayName%></strong> <%=GetGlobalResourceObject("Workflow", "On")%> <strong><%= ((Node)this.Content["RelatedContent"]).DisplayName %></strong><%=GetGlobalResourceObject("Workflow", "N")%></h2> 
        <dl class="sn-content-lead">
        <% if (FirtsLevelApprover != null && FirtsLevelApprover.Count() > 0) { %> <dt><%=GetGlobalResourceObject("Workflow", "FirstLevelApprover")%></dt><dd><strong> <%= FirtsLevelApprover.FirstOrDefault().DisplayName %> </strong></dd><% } %>
        <% if (!String.IsNullOrEmpty(FirstLevelTimeFrame) && FirstLevelTimeFrame != "0")
           { %> <dt><%=GetGlobalResourceObject("Workflow", "FirstLevelApprover")%></dt><dd><strong><%= FirstLevelTimeFrame %> </strong></dd><% } %>
        <% if (SecondLevelApprover != null && SecondLevelApprover.Count() > 0) { %>
        <dt><%=GetGlobalResourceObject("Workflow", "SecondLevelApprover")%></dt><dd>
            <% foreach (var approver in SecondLevelApprover)
               { %> 
               <strong> <%= approver.DisplayName %> </strong> &nbsp;
            <% } %></dd> 
        <% } %>
        <% if (!String.IsNullOrEmpty(SecondLevelTimeFrame) && SecondLevelTimeFrame != "0")
           { %> <dt><%=GetGlobalResourceObject("Workflow", "SecondLevelTimeFrame")%></dt><dd><strong> <%= SecondLevelTimeFrame %> </strong></dd><% } %>
        </dl>

</div>
<sn:ErrorView id="ERROR"  runat="server" />
<asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>

<div class="sn-panel sn-buttons">
    <% 
        var doc = this.Content["RelatedContent"] as Node;
        if (doc != null && doc.Version.Status == VersionStatus.Pending ) { %>
        <asp:Button class="sn-submit" ID="StartWorkflow" runat="server" Text="<%$ Resources: Workflow, Start %>" />
    <% } else { %>
        <%= IconHelper.RenderIconTag("warning", null, 32) %>
        <span><%=GetGlobalResourceObject("Workflow", "ContentMustBePending")%></span>
    <% } %>

    <sn:BackButton CssClass="sn-submit" Text="<%$ Resources: Workflow, Cancel %>" ID="BackButton1" runat="server" />
</div>