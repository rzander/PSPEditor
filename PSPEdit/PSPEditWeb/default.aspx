<%@ Page Title="PSPEdit" Language="C#" MasterPageFile="~/Default.Master" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="PSPEditWeb._default1" MaintainScrollPositionOnPostback="true" ValidateRequest="False" %>

<%@ Register Src="~/admxViewer.ascx" TagPrefix="uc1" TagName="admxViewer" %>


<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <%--            <div class="col-md-10 col-md-offset-1 container">--%>
            <div class="col-md-12 container">
                <div class="row">
                    <div class='col-md-4'>
                        <div id="dvScroll" style="overflow: auto; height: 85vh; margin-left: 10px;" onscroll="setScrollPosition(this.scrollTop);">
                            <asp:TreeView ID="tvADMXFiles" runat="server" ShowLines="True" ExpandDepth="1" OnTreeNodeExpanded="tvADMXFiles_TreeNodeExpanded" OnSelectedNodeChanged="tvADMXFiles_SelectedNodeChanged" NodeWrap="True" LineImagesFolder="~/TreeLineImages" OnTreeNodeCollapsed="tvADMXFiles_TreeNodeCollapsed" ></asp:TreeView>
                        </div>

                    </div>
                    <div class='col-md-8'>
                        <asp:Panel ID="Panel2" runat="server" ScrollBars="Auto">
                            <uc1:admxViewer runat="server" ID="admxViewer" />
                        </asp:Panel>
                    </div>
                </div>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
