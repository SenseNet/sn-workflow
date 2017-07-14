<%@  Language="C#" AutoEventWireup="true" Inherits="System.Web.UI.UserControl" %>

<asp:Literal ID="Literal1" runat="server" Text="<%$ Resources:Content,RegistrationStartedMessage%>" /> 

<div class="sn-panel sn-buttons">
    <asp:Button CssClass="sn-submit" Text="<%$ Resources:Content,Ok %>" ID="Confirm" runat="server" OnClientClick="location.href='/';return false;" />
</div>
