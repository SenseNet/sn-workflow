<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>

<div id="InlineViewContent" runat="server" class="sn-content sn-content-inlineview">
    <sn:ShortText runat="server" ID="FullName" FieldName="FullName" RenderMode="Edit" />
    <sn:ShortText runat="server" ID="UserName" FieldName="UserName" RenderMode="Edit">
      <EditTemplate>
        <asp:TextBox ID="InnerShortText" Class="sn-ctrl sn-ctrl-text sn-ctrl-username" runat="server"></asp:TextBox>
      </EditTemplate>
    </sn:ShortText>
    <sn:ShortText runat="server" ID="Email" FieldName="Email" RenderMode="Edit" />
    <sn:ShortText ID="InitialPassword" runat="server" FieldName="InitialPassword">
      <EditTemplate>
        <asp:TextBox ID="InnerShortText" Class="sn-ctrl sn-ctrl-text sn-ctrl-password" runat="server" TextMode="Password"></asp:TextBox>
      </EditTemplate>
   </sn:ShortText>
    <sn:DropDown runat="server" ID="RegistrationType" FieldName="RegistrationType" RenderMode="Edit" />
</div>
<div class="sn-panel sn-buttons">
  <asp:Button class="sn-submit" ID="StartWorkflow" runat="server" Text="Register" />
</div>