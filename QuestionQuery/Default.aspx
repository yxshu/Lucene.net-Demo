<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="QuestionQuery._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="s_form_wrapper">
        <div id="lg">
            <img alt="" src="ICON/logo.png" style="width: 460px; height: 127px" />
        </div>

        <span class="bg s_btn_wr">&nbsp;<asp:TextBox ID="TextBox1" runat="server" Width="414px"></asp:TextBox>
            <asp:Button ID="Button1" runat="server" Text="搜索" OnClick="Button1_Click" />
        </span>
    </div>

</asp:Content>
