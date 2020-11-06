<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="admxViewer.ascx.cs" Inherits="PSPEditWeb.admxViewer" %>

<div class="row" style="margin-right:10px">
    <div class='col-md-12' style="height:90vh">
        <label for="user_title">Title</label>
        <asp:Label class="form-control" ID="lName" runat="server" Text=""></asp:Label>

        <label for="user_title">Description</label>
        <asp:TextBox class="form-control" ID="tbDescription" runat="server" TextMode="MultiLine" Height="30%"></asp:TextBox>

        <label for="user_title">Status</label>
        <div class="form-control" style="height: 95px">
            <asp:RadioButtonList ID="statusRadios" RepeatLayout="Flow" RepeatDirection="vertical" runat="server" OnSelectedIndexChanged="statusRadios_SelectedIndexChanged" AutoPostBack="True">
                <asp:ListItem class="radio-inline" Value="1" Text="Enabled"></asp:ListItem>
                <asp:ListItem class="radio-inline" Value="0" Text="Disabled"></asp:ListItem>
                <asp:ListItem class="radio-inline" Value="2" Text="Not configured" Selected="True"></asp:ListItem>
            </asp:RadioButtonList>
        </div>

        <asp:Panel ID="pOptions" runat="server" GroupingText="Options" Visible="False">
        </asp:Panel>

        <label for="user_title">PowerShell</label>
        <asp:TextBox class="form-control" ID="tbPOSH" runat="server" Height="30%" TextMode="MultiLine" ReadOnly="True" BackColor="#012456" ForeColor="White" Font-Size="Larger" Font-Names="Courier New "></asp:TextBox>
    </div>
</div>

