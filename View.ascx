﻿<%@ Control Language="C#" AutoEventWireup="true" Inherits="ToSic.SexyContent.View" Codebehind="View.ascx.cs" %>
<asp:Panel runat="server" ID="pnlTemplateChooser" Visible="false" CssClass="dnnFormMessage dnnFormInfo sc-choosetemplate">
    <div>
        <asp:DropDownList runat="server" ID="ddlContentType" AppendDataBoundItems="true" DataTextField="Name" DataValueField="AttributeSetId" OnSelectedIndexChanged="ddlContentType_SelectedIndexChanged" AutoPostBack="true" CssClass="sc-contenttype-selector">
            <asp:ListItem Value="0" ResourceKey="ddlContentTypeDefaultItem"></asp:ListItem>
        </asp:DropDownList>
    </div>
    <div>
        <asp:DropDownList runat="server" ID="ddlTemplate" DataTextField="Name" DataValueField="TemplateID" CssClass="sc-template-selector">
        </asp:DropDownList>
    </div>
    <% if (Template != null) { %>
        <a class="sc-choosetemplate-close" href="javascript:$2sxc(<%= ModuleId %>).manage._setTemplateChooserState(false);">Close</a>
    <% } %>
</asp:Panel>

<asp:Panel runat="server" Visible="False" class="dnnFormMessage dnnFormInfo" ID="pnlGetStarted"></asp:Panel>

<asp:Panel runat="server" ID="pnlZoneConfigurationMissing" Visible="false" CssClass="dnnFormMessage dnnFormInfo">
    <asp:Label runat="server" ID="lblMissingZoneConfiguration" ResourceKey="ZoneConfigurationMissing"></asp:Label>
    <asp:HyperLink runat="server" ID="hlkConfigureZone" 
        CssClass="dnnSecondaryAction" ResourceKey="hlkConfigureZone"></asp:HyperLink>
</asp:Panel>

<asp:Panel runat="server" ID="pnlError" CssClass="dnnFormMessage dnnFormWarning" Visible="false"></asp:Panel>
<asp:Panel runat="server" ID="pnlMessage" CssClass="dnnFormMessage dnnFormInfo" Visible="false"></asp:Panel>
<asp:PlaceHolder runat="server" ID="phOutput"></asp:PlaceHolder>