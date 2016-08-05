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
        <asp:Label ID="errorLabel" runat="server"></asp:Label>
        <h2>Here are some corrective options</h2>
        <h3>1. Try Refreshing the Page</h3>
        <h3>2. Try Rebooting the Internet in the iPad Settings</h3>
        <h3>3. Contact IT - a database entry might be incorrect</h3>
    </div>
    </form>
</body>
</html>
