<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="ConferenceTracker.Error" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h1>Don't Panic!</h1>
        <h2>There was an error.</h2>
        <h3><asp:Label ID="errorLabel" runat="server"></asp:Label></h3>
        <h2>Here are some corrective options:</h2>
        <h4>1. Try Refreshing the Page.</h4>
        <h4>2. Try Rebooting the Internet in the iPad Settings.</h4>
        <h4>3. Contact IT - a database entry might be incorrect.</h4>
    </div>
    </form>
</body>
</html>
