<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="OrderWebSite._Default" %>
<%@ Import Namespace="System.Data" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        Enter quantity:
        <asp:TextBox ID="txtQuatity" runat="server" Text="10"/>
        <asp:Button ID="btnSubmit" runat="server" OnClick="btnSubmit_Click" Text="Submit Order"  />
    </div>
    <div>
        Entering a value above 100 will require manager approval (extends the delay 10 seconds) <br />
        Refresh the page after submitting orders (would of course use Ajax for this in production :) )
    </div>
        <a href="Default.aspx">Click here to refresh orderlist</a>
    <div>
    </div>
    <div>
        <asp:Repeater ID="OrderList" runat="server">
            <HeaderTemplate>
            <table border="1" width="100%">
            <tr>
                <th>
                    Id
                </th>
                <th>
                    Quantity
                </th>
                <th>
                    Status
                </th>
            </tr>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td>
                        <%# DataBinder.Eval(Container.DataItem, "Id") %>
                    </td>
                    <td>
                        <%# DataBinder.Eval(Container.DataItem, "Quantity") %>
                    </td>
                    <td>
                        <%# DataBinder.Eval(Container.DataItem, "Status") %>
                    </td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>
    </form>
</body>
</html>
