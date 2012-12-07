<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="Default.aspx.cs" Inherits="OrderWebSite._Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server" enctype="multipart/form-data">
    <div>
        Select File:
        <asp:FileUpLoad ID="fileUpload" runat="server" /><br />
        <asp:Button ID="btnUpload" runat="server" OnClick="btnUpload_Click" Text="Upload"  />
    </div>
    <div>
        Refresh the page after uploading the image to see the resize status (would of course use Ajax for this in production :) )
    </div>
    <div>
        <a href="Default.aspx">Click here to refresh</a>
    </div>
    <div>
       <div id="status" runat="server"/>
    </div>
    </form>
</body>
</html>
