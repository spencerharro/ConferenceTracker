<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="South.aspx.cs" Inherits="ConferenceTracker.South" %>




<!DOCTYPE html>
<html>
    



<head>
    <meta http-equiv="refresh" content="300">
    
    <style>
        /*iframe {
            height: 500px;
            width: 975px;
        }*/

        #conferenceTrackerLogo {
            text-align: center;
            width: 975px;
            height: 100px;
        }

        #header {
            background-color: #c2c2c2;
            height: 100px;
            width: 975px;
        }

        #headerElement1 {
            height: 100px;
            width: 275px;
            float: left;
            text-align: center;
        }

        #headerElement2 {
            height: 100px;
            width: 275px;
            float: left;
            text-align: center;
        }

        /*#headerElement3 {
            height: 100px;
            width: 275px;
            float: left;
            text-align: center;
        }*/

        #nameTextBox {
            height: 50px;
            width: 250px;
            font-size: 35px;
            top:3px;
            position:fixed;
            top:635px;
            left:20px;
        }

        #logo {
            /*width: 110px;
            height: 99px;
	    text-align: center;
            vertical-align: middle;*/
            display: inline-block;
            margin-top: 7px;
        }

        #durationDropDownBox {
            height: 65px;
            width: 250px;
            font-size: 27px;
        }

        #employeeDropDownBox {
            height: 65px;
            width: 425px;
            font-size: 27px;
            margin-left:5px;
        }

        #ConfirmGuestNameButton {
            text-align: center;
            background-image: url(Images/check_icon_48x48.png);
            background-position: center top 15px;
            background-repeat: no-repeat;
            height: 100px;
            width: 70px;
            background-color: transparent;
            outline: none;
            border: none;
            position:fixed;
            top:630px;
            left:300px;
        }

        .descriptionText {
            font-size: 20px;
            font-family: Tahoma,sans-serif;
            text-align: center;
            margin: 2px;
            padding: 0px;
        }

        #image {
            height: 80px;
            width: 80px;
            vertical-align: middle;
        }

        #lower {
            background-color: #e6e6e6;
            height: 500px;
            width: 975px;
        }

        .left {
            height: 500px;
            width: 487.5px;
            display: inline;
            float: left;
            vertical-align: top;
        }

        .upperLeftButton {
            display: inline-block;
            text-align: center;
            vertical-align: middle;
            padding: 22px 60px;
            border: 4px solid #000000;
            border-radius: 100px;
            /*background-image:url(Images/Refresh.png);*/
            background-repeat: no-repeat;
            background-position: center left 10px;
            background: #0065b3;
            background: -webkit-gradient(linear, left top, left bottom, from(#0065b3), to(#5e9ccc));
            background: -moz-linear-gradient(top, #0065b3, #5e9ccc);
            background: linear-gradient(to bottom, #0065b3, #5e9ccc);
            text-shadow: #591717 1px 1px 1px;
            font: normal normal bold 50px arial;
            color: #ffffff;
            text-decoration: none;
            /*background-image:url(Images\checkmark.png)*/
        }
        /*.upperLeftButton {
            display: inline-block;
            padding: 8px 20px;
            background-image:url(Images/checkmark.png);
            background-repeat:no-repeat;
            background-position: center left 25px;
            height:90px;
            width:90px;
            border-radius: 5px;
            color: #fff;
            font: normal 700 24px/1 "Calibri", sans-serif;
            text-align: center;
            text-shadow: 1px 1px 0 #000;
        }*/

        .lowerLeftButton {
            display: inline-block;
            text-align: center;
            vertical-align: middle;
            padding: 22px 60px;
            border: 4px solid #000000;
            border-radius: 100px;
            background: #e61920;
            background: -webkit-gradient(linear, left top, left bottom, from(#e61920), to(#f2797f));
            background: -moz-linear-gradient(top, #e61920, #f2797f);
            background: linear-gradient(to bottom, #e61920, #f2797f);
            text-shadow: #591717 1px 1px 1px;
            font: normal normal bold 50px arial;
            color: #ffffff;
            text-decoration: none;
        }

        .middleLeftButton {
            display: inline-block;
            text-align: center;
            vertical-align: middle;
            padding: 22px 40px;
            border: 4px solid #000000;
            border-radius: 100px;
            background: #fed403;
            background: -webkit-gradient(linear, left top, left bottom, from(#fed403), to(#c9a800));
            background: -moz-linear-gradient(top, #fed403, #c9a800);
            background: linear-gradient(to bottom, #fed403, #c9a800);
            text-shadow: #591717 1px 1px 1px;
            font: normal normal bold 50px arial;
            color: #ffffff;
            text-decoration: none;
        }

        #leftUpper {
            height: 166px;
            width: 487.5px;
            text-align: center;
            vertical-align: middle;
            line-height: 133px;
        }

        .leftMiddle {
            height: 166px;
            width: 487.5px;
            text-align: center;
            vertical-align: middle;
            line-height: 133px;
        }

        .leftLower {
            height: 166px;
            width: 487.5px;
            text-align: center;
            vertical-align: middle;
            line-height: 133px;
        }

        .attendeeZone {
            height: 500px;
            width: 975px;
            display: inline;
            float: right;
            vertical-align: top;
            background-color: #e6e6e6;
        }

        .listbox {
            height: 400px;
            width: 487.5px;
            font-size: 25px;
        }

        #bulletListContainer {
            border: 4px solid black;
            background-color: #e6e6e6;
            height: 475px;
            width: 970px;
        }

        #inMeetingRadioButtonList {
            font-family: Tahoma,sans-serif;
            font-size: 35px;
            text-align: center;
        }

        /*Radiobutton styling*/
        /*-------------------*/
        .ListControl input[type=checkbox], input[type=radio] {
            display: none;
        }

        /*.ListControl label {
            display: inline;
            float: left;
            color: #000;
            cursor: pointer;
            text-indent: 20px;
            white-space: nowrap;
            color:transparent;
        }

        .ListControl input[type=checkbox] + label {
            display: block;
            width: 1em;
            height: 1em;
            border: 0.0625em solid rgb(192,192,192);
            border-radius: 0.25em;
            background: rgb(211,168,255);
            background-image: -moz-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: -ms-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: -o-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: -webkit-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: linear-gradient(rgb(240,240,240),rgb(211,168,255));
            vertical-align: middle;
            line-height: 1em;
            font-size: 14px;
            color:transparent;
        }

        .ListControl input[type=checkbox]:checked + label::before {
            content: "\2714";
            color: #fff;
            height: 1em;
            line-height: 1.1em;
            width: 1em;
            font-weight: 900;
            margin-right: 6px;
            margin-left: -20px;
            color:transparent;
        }

        .ListControl input[type=radio] + label {
            display: block;
            width: 1em;
            height: 1em;
            border: 0.0625em solid rgb(192,192,192);
            border-radius: 1em;
            background: rgb(211,168,255);
            background-image: -moz-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: -ms-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: -o-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: -webkit-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: linear-gradient(rgb(240,240,240),rgb(211,168,255));
            vertical-align: middle;
            line-height: 1em;
            font-size: 14px;
            color:transparent;
        }

        .ListControl input[type=radio]:checked + label::before {
            content: "\2716";
            color: #fff;
            display: inline;
            width: 1em;
            height: 1em;
            margin-right: 6px;
            margin-left: -20px;
            color:transparent;
        }*/

        /*Single Checkbox: .CheckBoxLabel {
            white-space: nowrap;
        }

        .SingleCheckbox input[type=checkbox] {
            display: none;
        }

        .SingleCheckbox label {
            display: block;
            float: left;
            color: #000;
            cursor: pointer;
        }

        .SingleCheckbox input[type=checkbox] + label {
            width: 1em;
            height: 1em;
            border: 0.0625em solid rgb(192,192,192);
            border-radius: 0.25em;
            background: rgb(211,168,255);
            background-image: -moz-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: -ms-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: -o-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: -webkit-linear-gradient(rgb(240,240,240),rgb(211,168,255));
            background-image: linear-gradient(rgb(240,240,240),rgb(211,168,255));
            vertical-align: middle;
            line-height: 1em;
            text-indent: 20px;
            font-size: 14px;
        }

        .SingleCheckbox input[type=checkbox]:checked + label::before {
            content: "\2714";
            color: #fff;
            height: 1em;
            line-height: 1.1em;
            width: 1em;
            font-weight: 900;
            margin-right: 6px;
            margin-left: -20px;
        }*/

        /*-------------------*/

        #refreshButton {
            float: right;
            text-align: center;
            background-image: url(Images/refresh_icon_48x48.png);
            background-position: center;
            background-repeat: no-repeat;
            height: 100px;
            width: 70px;
            background-color: transparent;
            outline: none;
            border: none;
        }

        #clearAllButton {
            float: right;
            text-align: center;
            background-image: url(Images/deleteall_icon_48x48.png);
            background-position: center;
            background-repeat: no-repeat;
            height: 100px;
            width: 70px;
            background-color: transparent;
            outline: none;
            border: none;
        }
        /*----------------------------------------Overall this contains the calendar feature------------------------------------------------*/
        #infoBoxContainer {
            height: 500px;
            width: 975px;
            display: inline-block;
        }
        /*----Each calendar list item-----*/
        #nowInfoBox {
            position: relative;
            display: inline-block;
            height: 190px;
            width: 965px;
            border-radius: 10px;
            border: 3px solid black;
            margin: 5px;
            font-family: Optima;
            z-index:1;
            /*background: #003b00;
            background: -webkit-gradient(linear, left top, left bottom, from(#fed403), to(#c9a800));
            background: -moz-linear-gradient(top, #003b00, #005500);
            background: linear-gradient(to bottom, #003b00, #005500);*/
        }

        #next1InfoBox {
            position: relative;
            display: inline-block;
            float: left;
            height: 130px;
            width: 965px;
            border-radius: 10px;
            border: 3px solid black;
            margin: 5px;
            font-family: Optima;
            background: #e6e6e6;
        }

        #next2InfoBox {
            position: relative;
            display: inline-block;
            float: left;
            height: 130px;
            width: 965px;
            border-radius: 10px;
            border: 3px solid black;
            margin: 5px;
            font-family: Optima;
            background: #e6e6e6;
        }
        /*Each calendar event has a header*/
        #nowInfoBoxHeader {
            position:absolute;
            height: 50px;
            text-align: center;
            width: 915px;
            color: black;
        }

        #next1InfoBoxHeader {
            position:absolute;
            height: 50px;
            text-align: center;
            width: 915px;
            color: black;
        }

        #next2InfoBoxHeader {
            position:absolute;
            height: 50px;
            text-align: center;
            width: 915px;
            color: black;
        }
        /*-----BUTTONS----*/
        /*Now Meeting Box has clear/add buttons*/
        #ClearCurrentMeetingButton {
            position: absolute;
            top: 0;
            text-align: center;
            background-image: url(Images/delete_icon_48x48_1.png);
            background-position: top 5px right 10px;
            background-repeat: no-repeat;
            height: 190px;
            width: 975px;
            background-color: transparent;
            outline: none;
            border: none;
            z-index:3;
        }

        #AddNewMeetingButton {
            position: absolute;
            top: 0;
            text-align: center;
            background-image: url(Images/add_icon_48x48.png);
            background-position: top 5px right 10px;
            background-repeat: no-repeat;
            height: 190px;
            width: 975px;
            background-color: transparent;
            outline: none;
            border: none;
            z-index: 2;
        }

        #goBackButton1 {
            text-align: center;
            background-image: url(Images/delete_icon_48x48.png);
            background-position: center;
            background-repeat: no-repeat;
            height: 100px;
            width: 70px;
            background-color: transparent;
            outline: none;
            border: none;
            position:fixed;
            top:160px;
            left:695px;
        }
        #Panel2{
            float:left;
            height:80px;
            width:430px;
        }

        #goBackButton2 {
            text-align: center;
            background-image: url(Images/delete_icon_48x48.png);
            background-position: center top 15px;
            background-repeat: no-repeat;
            height: 100px;
            width: 70px;
            background-color: transparent;
            outline: none;
            border: none;
            position:fixed;
            top:630px;
            left:370px;
        }
        /*Other Controls*/
        #meetingNameTextbox {
            position: fixed;
            top:180px;
            left:350px;
            height: 45px;
            width: 250px;
            font-size: 35px;
        }

        #ConfirmNewMeetingNameButton {
            text-align: center;
            background-image: url(Images/check_icon_48x48.png);
            background-position: center;
            background-repeat: no-repeat;
            height: 100px;
            width: 70px;
            background-color: transparent;
            outline: none;
            border: none;
            position:fixed;
            top:160px;
            left:630px;
        }

        #newMeetingNameLabel {
            position:fixed;
            left:50px;
            top:175px;
            /*position: absolute;*/
            /*bottom: 20px;*/
            font-size: 45px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: white;
            word-wrap: normal;
        }


        /*Next1 and Next2 Meeting Boxes have activate button only*/
        #ActivateNext1MeetingButton {
            /*float: right;
            text-align: center;
            background-image: url(Images/enter_icon_64x64.png);
            background-position: center;
            background-repeat: no-repeat;
            height: 70px;
            width: 70px;
            background-color: transparent;
            outline: none;
            border: none;*/
            position: absolute;
            top: 0;
            float: right;
            text-align: center;
            background-image: url(Images/enter_icon_48x48_1.png);
            background-position: top right 10px;
            background-repeat: no-repeat;
            height: 130px;
            width: 975px;
            background-color: transparent;
            outline: none;
            border: none;
        }

        #ActivateNext2MeetingButton {
            /*float: right;
            text-align: center;
            background-image: url(Images/enter_icon_64x64.png);
            background-position: center;
            background-repeat: no-repeat;
            height: 70px;
            width: 70px;
            background-color: transparent;
            outline: none;
            border: none;*/
            position: absolute;
            top: 0;
            float: right;
            text-align: center;
            background-image: url(Images/enter_icon_48x48_1.png);
            background-position: top right 10px;
            background-repeat: no-repeat;
            height: 130px;
            width: 975px;
            background-color: transparent;
            outline: none;
            border: none;
        }

        /*Description Label in each box header*/
        #nowInfoBoxDescriptionLabel {
            font-size: 30px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: white;
        }

        #next1InfoBoxDescriptionLabel {
            font-size: 30px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: black;
        }

        #next2InfoBoxDescriptionLabel {
            font-size: 30px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: black;
        }

        /*Now Box Body*/
        #nowInfoBoxBody {
            position:absolute;
            z-index:auto;
            height: 50px;
            width: 975px;
            float: left;
            margin-top:35px;
        }


        #nowMeetingStartTimeLabel {
            text-align: left;
            font-size: 40px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: white;
        }

        #nowMeetingEndTimeLabel {
            margin-right: 50px;
            text-align: right;
            font-size: 40px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: white;
        }

        #nowMeetingNameLabel {
            font-size: 60px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: white;
            word-wrap: normal;
        }

        /*Next1 Box Body*/

        #next1InfoBoxBody {
            height: 50px;
            width: 975px;
            float: left;
            margin-top:35px;
            position:absolute;
            z-index:auto;
        }

        #next1MeetingStartTimeLabel {
            text-align: left;
            font-size: 30px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: black;
            margin-left: 5px;
        }

        #next1MeetingEndTimeLabel {
            text-align: right;
            float: right;
            margin-right: 20px;
            font-size: 30px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: black;
        }

        #next1MeetingNameLabel {
            font-size: 35px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: black;
            word-wrap: normal;
        }

        /*Next2 Box Body*/

        #next2InfoBoxBody {
            height: 50px;
            width: 975px;
            float: left;
            margin-top:35px;
            position:absolute;
            z-index:auto;
        }

        #next2MeetingStartTimeLabel {
            text-align: left;
            font-size: 30px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: black;
            margin-left: 5px;
        }

        #next2MeetingEndTimeLabel {
            text-align: right;
            font-size: 30px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: black;
            margin-right: 20px;
        }

        #next2MeetingNameLabel {
            font-size: 35px;
            font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;
            color: black;
            word-wrap: normal;
        }
    </style>

    <title></title>
</head>
<body>
    <form runat="server">
        <div <%--style="background-color:#DB3C0F"--%>>
            <div id="conferenceTrackerLogo">
                <img src="Images\confTrackerLogo5.png" alt="Logo" runat="server" onclick="checkInButton_Click">
            </div>
            <div id="infoBoxContainer">
                <div id="nowInfoBox" runat="server">
                    <div id="nowInfoBoxHeader">
                        <asp:Label ID="nowInfoBoxDescriptionLabel" runat="server" Text="Right Now:"></asp:Label>
                    </div>
                    <div id="nowInfoBoxBody">
                        <div style="float: left">
                            <asp:Label ID="nowMeetingStartTimeLabel" runat="server"></asp:Label>
                        </div>
                        <div style="float: right">
                            <asp:Label ID="nowMeetingEndTimeLabel" runat="server"></asp:Label>
                        </div>
                        <div style="text-align: center">
                            
                            <asp:Label ID="nowMeetingNameLabel" runat="server"></asp:Label>
                            
                            <asp:Panel ID="Panel1" runat="server" DefaultButton="ConfirmNewMeetingNameButton">
                                <asp:Label ID="newMeetingNameLabel" runat="server" Text="Meeting Name: "></asp:Label>
                                <asp:TextBox ID="meetingNameTextbox" runat="server" />
                                <asp:Button ID="ConfirmNewMeetingNameButton" runat="server" OnClick="AddNewMeetingButton_Click" />
                                <asp:Button ID="goBackButton1" runat="server" OnClick="GoBackButton_Click" />
                            </asp:Panel>
                            
                        </div>
                    </div>
                    <asp:Button ID="ClearCurrentMeetingButton" runat="server" OnClick="ClearCurrentMeetingButton_Click" />
                    <asp:Button ID="AddNewMeetingButton" runat="server" OnClick="AddNewMeetingButton_Click" />
                </div>
                
                <div id="next1InfoBox" runat="server">
                    <div id="next1InfoBoxHeader">
                        <asp:Label ID="next1InfoBoxDescriptionLabel" runat="server" Text="Up Next:"></asp:Label>

                    </div>
                    <div id="next1InfoBoxBody">
                        <div style="float: left">
                            <asp:Label ID="next1MeetingStartTimeLabel" runat="server" Text="Start:<br>9:00AM"></asp:Label>
                        </div>

                        <asp:Label ID="next1MeetingEndTimeLabel" runat="server" Text="End:<br>10:00AM"></asp:Label>

                        <div style="text-align: center">
                            <asp:Label ID="next1MeetingNameLabel" runat="server" Text="My Big Meeting"></asp:Label>
                        </div>

                    </div>
                    <asp:Button ID="ActivateNext1MeetingButton" runat="server" OnClick="ActivateNext1MeetingButton_Click" />
                </div>
                <div id="next2InfoBox" runat="server">
                    <div id="next2InfoBoxHeader">
                        <asp:Label ID="next2InfoBoxDescriptionLabel" runat="server" Text="Up Later:"></asp:Label>
                    </div>
                    <div id="next2InfoBoxBody">
                        <div style="float: left">
                            <asp:Label ID="next2MeetingStartTimeLabel" runat="server" Text="Start:<br>9:00AM"></asp:Label>
                        </div>
                        <div style="float: right">
                            <asp:Label ID="next2MeetingEndTimeLabel" runat="server" Text="End:<br>10:00AM"></asp:Label>
                        </div>
                        <div style="text-align: center">
                            <asp:Label ID="next2MeetingNameLabel" runat="server" Text="My Big Meeting"></asp:Label>
                        </div>
                    </div>
                    <asp:Button ID="ActivateNext2MeetingButton" runat="server" OnClick="ActivateNext2MeetingButton_Click" />
                </div>
            </div>

            <!--Contains Name and Meeting Duration fields, as well as logo-->
            <div id="header">
                <div id="headerElement1" runat="server">
                    <p class="descriptionText">Select Your Name:</p>
                    <asp:DropDownList ID="employeeDropDownBox" runat="server" OnSelectedIndexChanged="checkInButton_Click" AutoPostBack="true"></asp:DropDownList>
                </div>
                <div id="headerElement2" runat="server">
                    <!--Name Textbox:-->
                        <p class="descriptionText">
                            <asp:Label ID="GuestAndMeetingNameLabel" CssClass="descriptionText" runat="server"></asp:Label>
                        </p>
                    <asp:Panel ID="Panel2" runat="server" DefaultButton="ConfirmGuestNameButton">
                        
                        <asp:TextBox ID="nameTextBox" runat="server"/>
                        <asp:Button ID="ConfirmGuestNameButton" runat="server" OnClick="checkInButton_Click" />
                        <asp:Button ID="goBackButton2" runat="server" OnClick="GoBackButton_Click" />
                    </asp:Panel>
                    
                    <!--Logo Image used to be here:-->
                    <%--<img src="Images\confTrackerLogo5.png" alt="Logo" id="logo">--%>
                </div>
                <%--<div id="headerElement3" runat ="server">
                    <!--Meeeting Duration:-->
                    <p class="descriptionText">Meeting Duration:</p>
                    <asp:DropDownList ID="durationDropDownBox" runat="server">
                        <asp:ListItem Value="00:15:00">15 min</asp:ListItem>
                        <asp:ListItem Value="00:30:00">30 min</asp:ListItem>
                        <asp:ListItem Value="01:00:00" Selected="True">1 hr</asp:ListItem>
                        <asp:ListItem Value="01:30:00">1 hr 30 min</asp:ListItem>
                        <asp:ListItem Value="02:00:00">2 hr</asp:ListItem>
                        <asp:ListItem Value="03:00:00">3 hr</asp:ListItem>
                        <asp:ListItem Value="04:00:00">4 hr</asp:ListItem>
                        <asp:ListItem Value="05:00:00">5 hr</asp:ListItem>
                        <asp:ListItem Value="06:00:00">6 hr</asp:ListItem>
                        <asp:ListItem Value="07:00:00">7 hr</asp:ListItem>
                        <asp:ListItem Value="08:00:00">8 hr</asp:ListItem>
                        <asp:ListItem Value="09:00:00">9 hr</asp:ListItem>
                        <asp:ListItem Value="10:00:00">10 hr</asp:ListItem>
                    </asp:DropDownList>
                </div>--%>
               
                
                <asp:Button ID="refreshButton" runat="server" />
                <asp:Button ID="clearAllButton" runat="server" OnClick="clearAllButton_Click" />

            </div>
            <div id="lower">
                <%--<div class="left">
                    <div id="leftUpper" runat="server">
                        <div>
                            <asp:Button ID="checkInButton" class="upperLeftButton" runat="server" Text="      Check In"  OnClick="checkInButton_Click" Height="132px" Width="403px"/>
                        </div>
                    </div>
                    <div class="leftMiddle">
                        <div>
                            <asp:Button ID="checkOutButton" class="middleLeftButton" runat="server"
                                OnClick="checkOutButton_Click" Text="Check Out" />
                        </div>
                    </div>
                    <div class="leftLower">
                        <div>
                            <asp:Button ID="clearAllButton" class="lowerLeftButton" runat="server"
                                OnClick="clearAllButton
                    _Click" Text="Clear All" />
                        </div>
                    </div>
                </div>--%>
                <div class="attendeeZone">
                    <div>
                        <p class="descriptionText"><strong>Attendees In South Conference Room:</strong></p>
                        <%--This is a listbox that once was used     <asp:ListBox ID="inMeetingListBox" class="listbox" runat="server" DataSourceID="LinqDataSource1" DataTextField="name" DataValueField="id" ></asp:ListBox>--%>
                        <div id="bulletListContainer">
                            <asp:RadioButtonList ID="inMeetingRadioButtonList" runat="server" RepeatColumns="2" Height="300px" Width="965px" AutoPostBack="true" OnSelectedIndexChanged="checkOutButton_Click"></asp:RadioButtonList>
                        </div>
                        <%--This bulleted list never worked    <asp:BulletedList ID="inMeetingList" runat="server" DataSourceID="LinqDataSource1" DataTextField="name" DataValueField="id"></asp:BulletedList>--%>
                    </div>
                </div>

            </div>
            <%--<asp:Panel ID="enterMeetingNameWindow" runat="server" ScrollBars="Auto" Width="100%" Height="395px"></asp:Panel>--%>
        </div>
    </form>
</body>
</html>
