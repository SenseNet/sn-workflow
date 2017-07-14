<%@ Control Language="C#" AutoEventWireup="true" Inherits="System.Web.UI.UserControl" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage.Security" %>
<%@ Import Namespace="SenseNet.Search" %>

<sn:ScriptRequest ID="ScriptRequest1" runat="server" Path="/Root/Global/scripts/jquery/plugins/password_strength_plugin.js" />

<div id="InlineViewContent" runat="server" class="sn-content sn-content-inlineview">
    <div>
        <span><%= HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "WelcomeUser")%></span> <br />
        <span><%= HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "UserToChange")%></span>
    </div>
    <br/>

    <div class="sn-inputunit ui-helper-clearfix" runat="server" id="InputUnitPanel">
        <div class="sn-iu-label">
            <span class="sn-iu-title"><%= HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "NewPasswordTitle")%></span> <br />
            <span class="sn-iu-desc"><%= HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "NewPasswordDescription")%></span>
        </div>
        <div class="sn-iu-control">
            <asp:TextBox CssClass="sn-ctrl sn-ctrl-text sn-ctrl-password" ID="InnerPassword1" runat="server" TextMode="Password" /><br />
            <asp:TextBox CssClass="sn-ctrl sn-ctrl-text sn-ctrl-password2" ID="InnerPassword2" runat="server" TextMode="Password" />
            <asp:PlaceHolder ID="ErrorPanel" Visible="false" runat="server">
                <div>
                    <asp:Label runat="server" ID="ErrorViewLabel" Style="color:red"></asp:Label>
                </div>
            </asp:PlaceHolder>
        </div>
    </div>
    
</div>

<div class="sn-panel sn-buttons">
  <asp:Button class="sn-submit" ID="SetPassword" runat="server" Text="Set new password" OnClick="OnSetPassword" />
</div>

<script runat="server">

    private void OnSetPassword(object sender, EventArgs e)
    {
        var p1 = InnerPassword1.Text;
        var p2 = InnerPassword2.Text;

        if (string.IsNullOrEmpty(p1) || string.IsNullOrEmpty(p2) || p1.CompareTo(p2) != 0)
        {
            SetError(HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "PasswordIsNotValid") as string);
            return;
        }

        using (new SystemAccount())
        {
            var user = GetUser();

            if (user != null)
            {
                user.Password = p1;
                user.PasswordHash = SenseNet.ContentRepository.Fields.PasswordField.EncodePassword(p1, user);
                user.Save();

                // clear the forgotten item so that the workflow moves on
                var token = this.Request["token"] as string;
                var forgottenItem = GetForgottenItemByToken(token);
                forgottenItem["Description"] = string.Empty;
                forgottenItem.Save();

                HttpContext.Current.Response.Redirect("/");
            }
            else
            {
                SetError(HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "EmailIsNotValid") as string);
            }
        }
    }

    private User GetUser()
    {
        using (new SystemAccount())
        {
            var email = string.Empty;
            var token = this.Request["token"] as string;
            User user = null;

            if (!string.IsNullOrEmpty(token))
            {
                var forgottenItem = GetForgottenItemByToken(token);
                if (forgottenItem != null)
                {
                    // check if the item is still valid
                    var validTill = (DateTime)forgottenItem["ValidTill"];
                    if (validTill < DateTime.UtcNow)
                        return null;

                    email = forgottenItem["Description"] as string;
                }
            }

            if (!string.IsNullOrEmpty(email))
                return SenseNet.ContentRepository.Content.All.OfType<User>().FirstOrDefault(u => u.Email == email);
        }

        return null;
    }

    private SenseNet.ContentRepository.Content GetForgottenItemByToken(string token)
    {
        var contentPath = SenseNet.Portal.Virtualization.PortalContext.Current.Page.ParentPath + "/ForgottenItems/" + token;
        return SenseNet.ContentRepository.Content.LoadByIdOrPath(contentPath);
    }

    private void SetError(string text)
    {
        this.ErrorPanel.Visible = true;
        this.ErrorViewLabel.Text = text;
    }

</script>

<sn:InlineScript ID="InlineScript" runat="server">
<script type="text/javascript">
    $.fn.shortPass = 'Too short';
    $.fn.badPass = 'Weak';
    $.fn.goodPass = 'Good';
    $.fn.strongPass = 'Strong';
    $.fn.samePassword = 'Username and Password identical.';

    $(function () {
        $("input.sn-ctrl-password, input.sn-ctrl-password2").passStrength({ userid: ".sn-ctrl-username" });
    });
</script>
</sn:InlineScript>

