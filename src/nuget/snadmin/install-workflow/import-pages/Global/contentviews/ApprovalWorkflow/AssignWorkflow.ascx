<%@ Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Import Namespace="SNCR=SenseNet.ContentRepository" %>

<div class="sn-content sn-content-inlineview">

    <sn:GenericFieldControl runat="server" ID="GenericFieldcontrol1" FieldsOrder="DisplayName" />        

    <div class="sn-inputunit ui-helper-clearfix sn-required">
        <div class="sn-iu-label">
            <span class="sn-iu-title"><%=GetGlobalResourceObject("Workflow", "StartOptions")%></span>
            <span class="sn-iu-required-mark">*</span><br />
            <span class="sn-iu-desc"><%=GetGlobalResourceObject("Workflow", "Specify")%></span>
        </div>
        <div class="sn-iu-control">       
            <sn:Boolean ID="AllowManualStart" runat="server" FieldName="AllowManualStart" FrameMode="NoFrame" /><label for='<%= this.AllowManualStart.FindControlRecursive("InnerCheckBox").ClientID  %>'><%=GetGlobalResourceObject("Workflow", "ManualStart")%></label><br />
            <% var cl = SNCR.ContentList.GetContentListByParentWalk(this.Content.ContentHandler);
               if (cl != null && cl.InheritableApprovingMode == SNCR.Versioning.ApprovingType.True && cl.InheritableVersioningMode == SNCR.Versioning.InheritableVersioningType.MajorAndMinor)
               { %>
            <sn:Boolean ID="AutostartOnPublished1" runat="server" FieldName="AutostartOnPublished" FrameMode="NoFrame" /><label for='<%= this.AutostartOnPublished1.FindControlRecursive("InnerCheckBox").ClientID  %>'><%=GetGlobalResourceObject("Workflow", "MajorVersion")%></label><br />
            <% } %>
            <sn:Boolean ID="AutostartOnCreated" runat="server" FieldName="AutostartOnCreated" FrameMode="NoFrame" /><label for='<%= this.AutostartOnCreated.FindControlRecursive("InnerCheckBox").ClientID  %>'><%=GetGlobalResourceObject("Workflow", "NewContent")%></label><br />
            <sn:Boolean ID="AutostartOnChanged" runat="server" FieldName="AutostartOnChanged" FrameMode="NoFrame" /><label for='<%= this.AutostartOnChanged.FindControlRecursive("InnerCheckBox").ClientID  %>'><%=GetGlobalResourceObject("Workflow", "ContentChange")%></label>
            <asp:PlaceHolder Visible="false" ID="StartOptionErrorContainer" runat="server">
                <asp:Label CssClass="sn-iu-error" ID="StartOptionErrorLabel" runat="server" />
            </asp:PlaceHolder>
        </div>
    </div>

    <sn:GenericFieldControl runat="server" ID="GenericFieldcontrol2" FieldsOrder="Description FirstLevelApprover FirstLevelTimeFrame SecondLevelApprover SecondLevelTimeFrame WaitForAll" />        

</div>
<asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>

<div class="sn-panel sn-buttons">
  <asp:Button class="sn-submit" ID="AssignWorkflow" runat="server" Text="<%$ Resources:Workflow,AssignToList %>" />
  <sn:BackButton class="sn-submit" ID="Cancel" runat="server" Text="<%$ Resources:Workflow,Cancel %>" />
</div>