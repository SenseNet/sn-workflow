<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage.Security" %>

<div id="InlineViewContent" runat="server" class="sn-content sn-content-inlineview">
    <sn:ErrorView ID="ErrorView1" runat="server" />
    <sn:ShortText runat="server" ID="Email" FieldName="EmailForPassword" RenderMode="Edit" />
</div>
<div class="sn-panel sn-buttons">
  <asp:Button class="sn-submit" ID="StartWorkflow" runat="server" Text="Send mail" />
</div>

<script runat="server">

    protected override void OnContentUpdated()
    {
        base.OnContentUpdated();

        //do not check if there is already an error
        if (!this.IsUserInputValid || !this.Content.IsValid)
            return;

        var email = (string)this.Content["EmailForPassword"];
        using (new SystemAccount())
        {
            if (string.IsNullOrEmpty(email) || !SenseNet.ContentRepository.Content.All.OfType<User>().Any(u => u.Email == email))
            {
                this.IsUserInputValid = false;
                this.ContentException = new InvalidOperationException(HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "EmailIsNotValid") as string);
                return;
            }
        }
    }

</script>