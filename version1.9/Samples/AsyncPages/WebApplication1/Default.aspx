<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="Default.aspx.cs" Inherits="WebApplication1._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        Enter a number below and click "Go".<br />
        If the number is even, the result will be "Fail", otherwise it will be "None".
        <br /><br />
        
        <asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Go" />
        <asp:Label ID="Label1" runat="server"></asp:Label></div>
    </form>
</body>
</html>
