﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
//Google
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading;

using System.Data.Entity.Validation;
using System.Diagnostics;

using System.Data.SqlClient;

//Microsoft
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Exchange.WebServices;

namespace ConferenceTracker
{
    public partial class North : System.Web.UI.Page
    {
        // -------------------------PAGE SETUP-------------------------------- //
        //Initialize LINQ to Entities Connection
        I2RloginEntities db = new I2RloginEntities();
        //Initialize Conference Room
        Room room = new Room();
        
        //Set Conference Room Status ID - for StatusBoard
        static int conferenceRoomStatusID = 10;  //10 is status for North

        //Microsoft Exchange Web Services Credentials
        static string emailAddress = "north@i2r.com";
        static string emailPassword = "Catesuser1";
        private string emailURL = "https://mex07a.emailsrvr.com/EWS/Exchange.asmx";
        
        //Google - Retired in ConferenceTracker 2.1.0
        ////Google
        //static string[] Scopes = { CalendarService.Scope.CalendarReadonly, CalendarService.Scope.Calendar };
        //static string ApplicationName = "Conference Tracker";
        //static string clientId = "850215639552-7nr5sn2dc5hrn8keilhm4t0cgcn75igu.apps.googleusercontent.com";
        //static string clientSecret = "bE4zxcyawDCMdla1UzVylJxy";
        ////ID for the calendar of interest
        //static string calID = "kupj7mf53rgeu2mbtlhhf49cps@group.calendar.google.com";
        //Location of the .json credential file for accessing Google's API
        //static string credfilepath = @"c:\Authentication\client_secret_850215639552-7nr5sn2dc5hrn8keilhm4t0cgcn75igu.apps.googleusercontent.com.json";

        static DateTime midnightTonight = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, 0, 0, 0);

        static int maxStringLength = 33;

        //test comment on north page for Git purposes
        // ------------------------------------------------------------ //
        protected void Page_Load(object sender, EventArgs e)
        {
            //Set Conference Room Info
            room.RoomID = 1;    //North
            room.RoomName = "North Conference Room";
            db.SaveChanges();

            //Run through startup routine if first page view
            if (!IsPostBack)
            {
                //Set page controls
                EnableNormalControls();

                //Run Startup Routine
                RunStartupRoutine();

                //Sync Meeting List with Google Calendar
                //SyncMeetingListWithGoogleCalendar();

                //Sync Meeting List with Exchange Calendar
                SyncMeetingListWithEWSCalendar();

                //May comment out
                //db.SaveChanges();

            }
        }
        private void RunStartupRoutine()
        {
            //CLEAR LISTBOX
            ClearAttendeesList();

            //CLEAR DROPDOWN
            ClearEmployeeDropDown();

            //CLEAR GUEST TEXTBOX
            nameTextBox.Text = "";

            //SET DROPDOWN DEFAULT TEXT();
            employeeDropDownBox.Items.Insert(0, new ListItem("Select Name", null));
            employeeDropDownBox.Items.Insert(1, new ListItem("Guest", null));

            //DISABLED: CONFERENCE TRACKER 2.0
            //REMOVE OVERDUE ATTENDEES
            //RemoveOverdueAttendees();

            //Sync Meeting List with Google Calendar (if no events in db)
            if (db.Meetings.Where(m => m.RoomID == room.RoomID) == null || db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID) == null)
            {
                //Google Calendar
                //SyncMeetingListWithGoogleCalendar();
                //Exchange Calendar
                SyncMeetingListWithEWSCalendar();
                //Meeting Database Table
                SyncMeetingListWithMeetingDB();
            }


            //NEW: CONFERENCE TRACKER 2.0
            //REMOVE ATTENDEES CHECKED OUT ON STATUS BOARD
            SyncStatusForEachEmployee();

            //LOAD ATTENDEES IN RADIOBUTTONLIST
            PopulateAttendeesList();

            //LOAD EMPLOYEES IN DROPDOWN
            PopulateEmployeesList();

        }
        public void SyncStatusForEachEmployee()
        {
            //Loop through employees
            foreach (var emp in db.Employees)
            {
                var StatusBoardAttendee = db.Attendees
                    .Where(a => a.EmployeeID == emp.EmployeeID && a.Location == room.RoomName)
                    .FirstOrDefault();

                if (StatusBoardAttendee != null)
                {
                    //Set remarks to room name and current meeting
                    Meeting currentMeeting = db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault();
                    if (currentMeeting != null)
                    {
                        emp.Remarks = room.RoomName + ((currentMeeting.Name == "Room Available") ? "" : ": " + currentMeeting.Name.ToString());
                        //Set SB return time
                        //if(emp.ReturnDate == null)
                        //{
                        //    DateTime startTime = DateTime.Parse(currentMeeting.StartTime.ToString());
                        //    DateTime endTime = DateTime.Parse(currentMeeting.EndTime.ToString());
                        //    TimeSpan duration = endTime.Subtract(startTime);
                        //    DateTime nowTime = DateTime.Now;
                        //    emp.ReturnDate = nowTime + duration;
                        //}

                    }

                    //Check if the Status ID shows the emp out of the room, and if the emp is an an attendee
                    if (emp.StatusID != conferenceRoomStatusID && StatusBoardAttendee.EmployeeID != 0)
                    {
                        //Remove emp from attendees
                        RemoveAttendeeByEmployeeID(StatusBoardAttendee.EmployeeID);
                    }
                    //Check if the Status ID shows the emp in the room, and if the emp is not already an attendee
                    else if (emp.StatusID == conferenceRoomStatusID && StatusBoardAttendee.EmployeeID == 0 && emp.ReturnDate != null)
                    {
                        AddAttendee(CreateAttendee(emp.EmployeeID,
                            room.RoomName.ToString(),
                            emp.ReturnDate.Value,
                            emp.FirstName + " " + emp.LastName));
                    }
                }
            }
            db.SaveChanges();
        }

        public Attendee CreateAttendee(int employeeID,
            string location,
            DateTime timeOut,
            string attendeeName)
        {
            //Initialize Attendee
            Attendee newAttendee = new Attendee();
            newAttendee.EmployeeID = employeeID;
            newAttendee.Location = location;
            newAttendee.TimeOut = timeOut;
            newAttendee.Name = attendeeName;

            //Return the attendee
            return newAttendee;
        }
        public void AddAttendee(Attendee a)
        {
            db.Attendees.Add(a);
            //db.SaveChanges();
        }
        public void RemoveOverdueAttendees()
        {
            //Find Attendees that are overdue
            var overdueAttendees = db.Attendees.Where(a => a.TimeOut < DateTime.Now && a.Location == room.RoomName);

            //Remove overdue attendees (if there are any)
            if (overdueAttendees != null)
            {
                foreach (var oa in overdueAttendees)
                    RemoveAttendeeByAttendeeID((oa.AttendeeID));
                db.SaveChanges();
            }


        }
        public void PopulateEmployeesList()
        {

            //Query for all attendees who are employees
            List<Attendee> employeesWhoAreAttendees = db.Attendees.Where(ea => ea.EmployeeID > 0).ToList();
            //Query for all employees in database
            List<Employee> employees = db.Employees.Where(ee => (ee.Division == "Fab Shop" || ee.Division == "Office")).ToList();

            //Find All employees who are not currently attendees in meetings
            var employeesNotCheckedIn = employees.Where(ee => employeesWhoAreAttendees.All(aa => ee.EmployeeID != aa.EmployeeID)).OrderBy(ee => ee.FirstName.ToString());

            //Create drop down list entries for each employee not checked in
            foreach (var emp in employeesNotCheckedIn)
            {
                employeeDropDownBox.Items.Add(CreateListItem(emp.FirstName.ToString() + " " + emp.LastName.ToString(), emp.EmployeeID.ToString()));
            }
        }
        public void PopulateAttendeesList()
        {
            //Query Attendees table for all attendees in room
            var attendees = db.Attendees.Where(a => a.Location == room.RoomName).Select(a => new { a.Name, a.AttendeeID });

            //Add each attendee in room to list
            foreach (var a in attendees)
            {
                //Attendee name appears in box, Attendee id is the selection value
                inMeetingRadioButtonList.Items.Add(CreateListItem(a.Name.ToString(), a.AttendeeID.ToString()));

            }

        }
        public ListItem CreateListItem(string text, string value)
        {
            //Create the list item based on input text/value
            ListItem li = new ListItem();
            li.Text = text;
            li.Value = value;
            return li;
        }
        public void ClearAttendeesList()
        {
            inMeetingRadioButtonList.Items.Clear();
        }
        public void ClearEmployeeDropDown()
        {
            employeeDropDownBox.Items.Clear();
        }
        public void checkInButton_Click(object sender, EventArgs e)
        {
            //Create the new attendee
            Attendee newAttendee = new Attendee();

            //Check if anything was selected
            if (employeeDropDownBox.SelectedIndex != 0 || nameTextBox.Text != "")
            {
                //Check if selected attendee is guest
                if (employeeDropDownBox.SelectedIndex == 1 && nameTextBox.Text == "")
                {
                    //Set page controls
                    EnableGuestControls();
                }
                //Otherwise
                else if (nameTextBox.Text != "")
                {
                    //Create the guest attendee
                    AddAttendee(CreateAttendee(-1, room.RoomName,
                        DateTime.Now.AddHours(1),   //Default meeting length 1hr
                        nameTextBox.Text.ToString()));

                    //Set page controls
                    EnableNormalControls();
                }
                else if (employeeDropDownBox.SelectedIndex > 1)
                {
                    //Find the employee in the Employee database
                    int employeeID = Int32.Parse(employeeDropDownBox.SelectedValue);
                    var checkingInEmployee = db.Employees.Where(ee => ee.EmployeeID == employeeID).FirstOrDefault();

                    //Modify Status Board entries
                    checkingInEmployee.StatusID = conferenceRoomStatusID;
                    //DateTime timeOut = DateTime.Now.Add(TimeSpan.Parse(durationDropDownBox.SelectedValue));
                    //checkingInEmployee.ReturnDate = timeOut; //Change Status Board Return Time
                    checkingInEmployee.Remarks = room.RoomName.ToString() + (nowMeetingNameLabel.Text == "Room Available" ? "" : ": " + nowMeetingNameLabel.Text.ToString());  //Change Status Board Remarks

                    //Convert the employee to an attendee
                    AddAttendee(CreateAttendee(checkingInEmployee.EmployeeID,
                        room.RoomName,
                        DateTime.Now.AddHours(1),
                        checkingInEmployee.FirstName.ToString() + " " + checkingInEmployee.LastName.ToString()));
                }

            }
            //Update the Attendees list on the webpage
            RunStartupRoutine();


            //LOAD Meeting List
            SyncMeetingListWithMeetingDB();
        }
        protected void checkOutButton_Click(object sender, EventArgs e)
        {
            if (inMeetingRadioButtonList.SelectedIndex != -1)
            {
                //Find the name of the attendee checking out
                int attendeeID = Int32.Parse(inMeetingRadioButtonList.SelectedValue);
                //Check out the attendee
                RemoveAttendeeByAttendeeID(attendeeID);
                //Save changes to database
                db.SaveChanges();
            }
            //Set page controls
            EnableNormalControls();

            RunStartupRoutine();


            //LOAD Meeting List
            SyncMeetingListWithMeetingDB();
        }
        protected void clearAllButton_Click(object sender, EventArgs e)
        {
            //Individually delete each attendee from the attendee list
            foreach (ListItem li in inMeetingRadioButtonList.Items)
            {
                RemoveAttendeeByAttendeeID(Int32.Parse(li.Value));
            }
            db.SaveChanges();

            //Set page controls
            EnableNormalControls();

            RunStartupRoutine();

            //Remove the current meeting from the Now Div
            ResetCurrentMeeting();

            //LOAD Meeting List
            //Google Calendar
            //SyncMeetingListWithGoogleCalendar();

            //Exchange Calendar
            SyncMeetingListWithEWSCalendar();

        }
        public void RemoveAttendeeByEmployeeID(int employeeID)
        {
            //Find the employee based on the employee ID
            var checkingOutEmployee = db.Employees.Where(ee => ee.EmployeeID == employeeID).FirstOrDefault();
            //Set Status Board properties to basic in office Setup
            //UpdateEmployeeOnStatusBoard(DateTime.Parse(""), null, 1);
            checkingOutEmployee.ReturnDate = null;
            checkingOutEmployee.Remarks = null;
            checkingOutEmployee.StatusID = 1;     //Employee is in office again

            //Find the attendee by comparing employee ids
            var checkingOutAttendee = db.Attendees.Where(a => a.EmployeeID == checkingOutEmployee.EmployeeID).FirstOrDefault();

            //Remove attendee
            db.Attendees.Remove(checkingOutAttendee);

            //Commit changes to database
            //db.SaveChanges();
        }
        public void UpdateEmployeeOnStatusBoard(DateTime returnDate, string remarks, int statusID)
        {
            Employee updatedEmployee = new Employee();
            updatedEmployee.ReturnDate = returnDate;
            updatedEmployee.Remarks = remarks;
            updatedEmployee.StatusID = statusID;
        }
        public void RemoveAttendeeByAttendeeID(int attendeeID)
        {
            //Find the checking out attendee by their name  in the Attendee database
            var checkingOutAttendee = db.Attendees.Where(a => a.AttendeeID == attendeeID).FirstOrDefault();

            if (checkingOutAttendee != null)
            {
                //Check if the attendee is an employee (if their employee id is not equal to the guest id = -1)
                if (checkingOutAttendee.EmployeeID != int.Parse("-1"))
                {
                    //Find the employee based on the attendee name
                    var checkingOutEmployee = db.Employees.Where(ee => ee.EmployeeID == checkingOutAttendee.EmployeeID).FirstOrDefault();

                    //Set Status Board properties to basic in office Setup
                    checkingOutEmployee.ReturnDate = null;
                    checkingOutEmployee.Remarks = null;
                    checkingOutEmployee.StatusID = 1;     //Employee is in office again
                }

                //Remove attendee
                db.Attendees.Remove(checkingOutAttendee);

                //Commit changes to database
                //db.SaveChanges();
            }
        }
        public void SyncMeetingListWithEWSCalendar()
        {
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2007_SP1);

            service.Credentials = new WebCredentials(emailAddress, emailPassword);
            

            try
            {
                // Previous saved connection string
                service.Url = new Uri(emailURL);
            }
            catch
            {
                try
                {
                    // Use autodiscover service to relocate url
                    service.AutodiscoverUrl(emailAddress, RedirectionUrlValidationCallback);
                    //Save new url TODO
                    emailURL = service.Url.AbsoluteUri;
                   
                }
                catch(Exception ex)
                {
                    // Return an error that the url could not be found (In Now Div)
                    nowMeetingNameLabel.Text = ex.ToString();
                    nowMeetingNameLabel.Font.Size = 10;
                    
                }
            }

            if(service.Url != null)
            {
                // Initialize values for the start and end times, and the number of appointments to retrieve.
                DateTime startDate = DateTime.Now;
                DateTime endDate = startDate.AddYears(1);
                const int NUM_APPTS = 4;

                // Initialize the calendar folder object with only the folder ID. 
                CalendarFolder calendar = CalendarFolder.Bind(service, WellKnownFolderName.Calendar, new PropertySet());

                // Set the start and end time and number of appointments to retrieve.
                CalendarView cView = new CalendarView(startDate, endDate, NUM_APPTS);

                // Limit the properties returned to the appointment's subject, start time, and end time.
                cView.PropertySet = new PropertySet(AppointmentSchema.Subject, AppointmentSchema.Start, AppointmentSchema.End);

                // Retrieve a collection of appointments by using the calendar view.
                FindItemsResults<Appointment> appointments = calendar.FindAppointments(cView);

                //Check if there are appointments
                if (appointments != null)
                {
                    Meeting currentMeeting = db.Meetings.Where(m => m.Name != "Room Available" && m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault();

                    //Doesn't work
                    if (db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault() == null)
                    {
                        db.Meetings.Add(new Meeting { MeetingID = 1, GoogleCalendarID = "", Name = "Room Available", StartTime = null, EndTime = null, RoomID = room.RoomID });
                        //db.SaveChanges();
                    }

                    List<Appointment> appointmentsNotActivated;

                    Appointment firstSuggestion;
                    Appointment secondSuggestion;

                    //True if available, False if not available
                    bool roomAvailable = true;

                    if (currentMeeting != null) { roomAvailable = false; }

                    if (roomAvailable)
                    {
                        //Show the room is available
                        SetNowDiv("Room Available", "", "", (room.RoomName + ":"));
                        AddNewMeetingButton.Visible = true;
                        ClearCurrentMeetingButton.Visible = false;

                        nowInfoBox.Style.Remove("background");

                        nowInfoBox.Style.Add("background", "#007600");
                        nowInfoBox.Style.Add("background", "-webkit-gradient-webkit-gradient(linear, left top, left bottom, from(#fed403), to(#c9a800))");
                        nowInfoBox.Style.Add("background", "-moz-linear-gradient(top, #007600, #006f00))");
                        nowInfoBox.Style.Add("background", "linear-gradient(to bottom, #007600, #006f00)");

                    }
                    else
                    {
                        //Indicate the current Meeting
                        SetNowDiv(currentMeeting.Name,
                            currentMeeting.StartTime.Value.ToShortTimeString(),
                            currentMeeting.EndTime.Value.ToShortTimeString(),
                            (room.RoomName + ":"));
                        if (currentMeeting.GoogleCalendarID.ToString() == "")
                        {
                            SetNowDiv(currentMeeting.Name, "", "", room.RoomName.ToString() + ":");
                        }
                        //Turn the current meeting box red since it is in meeting
                        //    background: #003b00;
                        //background:
                        //    -webkit - gradient(linear, left top, left bottom, from(#fed403), to(#c9a800));
                        //background: -moz - linear - gradient(top, #003b00, #005500);
                        //background: linear - gradient(to bottom, #003b00, #005500);
                        nowInfoBox.Style.Remove("background");

                        nowInfoBox.Style.Add("background", "#606060");

                        AddNewMeetingButton.Visible = false;
                        ClearCurrentMeetingButton.Visible = true;
                    }
                    if (appointments != null)
                    {
                        if (roomAvailable)
                        {
                            //Set the suggestions
                            firstSuggestion = appointments.Items.Cast<Appointment>().ElementAt(0);
                            secondSuggestion = appointments.Items.Cast<Appointment>().ElementAt(1);
                        }
                        else
                        {
                            //TODO:  Need to fix how appointments that are not activated are determined.
                            ////Find events besides current event
                            //appointmentsNotActivated = (appointments.Items.Where(ap => !TruncateMyLongString(ap.Id.ToString(),50).Equals(currentMeeting.GoogleCalendarID.ToString())).Select(ev => ev)).ToList();

                            ////Set the suggestions
                            //firstSuggestion = appointmentsNotActivated.Cast<Appointment>().ElementAt(0);
                            //secondSuggestion = appointmentsNotActivated.Cast<Appointment>().ElementAt(1);
                            //Temporary
                            firstSuggestion = appointments.Items.Cast<Appointment>().ElementAt(0);
                            secondSuggestion = appointments.Items.Cast<Appointment>().ElementAt(1);
                        }


                        //Suggest the first meeting, adjusting for it's time
                        if (firstSuggestion.Start < midnightTonight && firstSuggestion != null)
                        {
                            //Indicate the next meeting suggestion
                            SetNext1Div(TruncateMyLongString(firstSuggestion.Subject, maxStringLength),
                                firstSuggestion.Start.ToShortTimeString(),
                                firstSuggestion.End.ToShortTimeString(),
                                "Next:");

                        }
                        else
                        {
                            //Suggest a meeting for another day
                            SetNext1Div(TruncateMyLongString(firstSuggestion.Subject, maxStringLength),
                                firstSuggestion.Start.ToShortTimeString(),
                                firstSuggestion.End.ToShortTimeString(),
                                "-- " + firstSuggestion.Start.DayOfWeek.ToString() + " --");
                        }

                        //Suggest the second meeting, adjusting for it's time
                        if (secondSuggestion.Start < midnightTonight && firstSuggestion != null)
                        {
                            //Indicate the next meeting suggestion
                            SetNext2Div(TruncateMyLongString(secondSuggestion.Subject, maxStringLength),
                                secondSuggestion.Start.ToShortTimeString(),
                                secondSuggestion.End.ToShortTimeString(),
                                "Next:");
                        }
                        else
                        {
                            //Suggest a meeting for another day
                            SetNext2Div(TruncateMyLongString(secondSuggestion.Subject, maxStringLength),
                                secondSuggestion.Start.ToShortTimeString(),
                                secondSuggestion.End.ToShortTimeString(),
                                "-- " + secondSuggestion.Start.DayOfWeek.ToString() + " --");
                        }
                        foreach (var meeting in db.Meetings.Where(m => m.MeetingID != 1 && m.RoomID == room.RoomID))
                        {
                            RemoveMeetingFromDB(meeting);
                        }
                        //Add the first/second suggestions to the meetings table
                        //Input the meeting to the meetings table under meeting #2

                        try
                        {
                            AddMeetingToDB(new Meeting
                            {
                                MeetingID = 2,
                                Name = TruncateMyLongString(firstSuggestion.Subject, 50),
                                StartTime = firstSuggestion.Start,
                                EndTime = firstSuggestion.End,
                                GoogleCalendarID = TruncateMyLongString(firstSuggestion.Id.ToString(), 50),
                                RoomID = room.RoomID
                            });

                            // db.SaveChanges();

                            //Input the meeting to the meetings table under meeting #3
                            AddMeetingToDB(new Meeting
                            {
                                MeetingID = 3,
                                Name = TruncateMyLongString(secondSuggestion.Subject, 50),
                                StartTime = secondSuggestion.Start,
                                EndTime = secondSuggestion.End,
                                GoogleCalendarID = TruncateMyLongString(secondSuggestion.Id.ToString(), 50),
                                RoomID = room.RoomID
                            });
                            //db.SaveChanges();
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            nowMeetingNameLabel.Text = ex.ToString();
                            nowMeetingNameLabel.Font.Size = 10;
                        }

                    }


                }
                if (appointments == null) { nowInfoBoxDescriptionLabel.Text = "Could not access Exchange Calendar"; }
                
            }
        }
        //public void SyncMeetingListWithGoogleCalendar()
        //{
        //    Events googleCalendarEvents = null;
        //    //Try to access 
        //    try
        //    {
        //        googleCalendarEvents = GetEvents(OpenCalendar(), 10);
        //    }
        //    catch (Exception e)
        //    {
        //        nowMeetingNameLabel.Text = e.ToString();
        //    }
        //    if (googleCalendarEvents != null)
        //    {
        //        Meeting currentMeeting = db.Meetings.Where(m => m.Name != "Room Available" && m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault();

        //        //Doesn't work
        //        if (db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault() == null)
        //        {
        //            db.Meetings.Add(new Meeting { MeetingID = 1, GoogleCalendarID = "", Name = "Room Available", StartTime = null, EndTime = null, RoomID = room.RoomID });
        //            //db.SaveChanges();
        //        }

        //        List<Event> eventsNotActivated;

        //        Event firstSuggestion;
        //        Event secondSuggestion;

        //        //True if available, False if not available
        //        bool roomAvailable = true;

        //        if (currentMeeting != null) { roomAvailable = false; }

        //        if (roomAvailable)
        //        {
        //            //Show the room is available
        //            SetNowDiv("Room Available", "", "", (room.RoomName + ":"));
        //            AddNewMeetingButton.Visible = true;
        //            ClearCurrentMeetingButton.Visible = false;

        //            nowInfoBox.Style.Remove("background");

        //            nowInfoBox.Style.Add("background", "#007600");
        //            nowInfoBox.Style.Add("background", "-webkit-gradient-webkit-gradient(linear, left top, left bottom, from(#fed403), to(#c9a800))");
        //            nowInfoBox.Style.Add("background", "-moz-linear-gradient(top, #007600, #006f00))");
        //            nowInfoBox.Style.Add("background", "linear-gradient(to bottom, #007600, #006f00)");

        //        }
        //        else
        //        {
        //            //Indicate the current Meeting
        //            SetNowDiv(currentMeeting.Name,
        //                currentMeeting.StartTime.Value.ToShortTimeString(),
        //                currentMeeting.EndTime.Value.ToShortTimeString(),
        //                (room.RoomName + ":"));
        //            if (currentMeeting.GoogleCalendarID.ToString() == "")
        //            {
        //                SetNowDiv(currentMeeting.Name, "", "", room.RoomName.ToString() + ":");
        //            }
        //            //Turn the current meeting box red since it is in meeting
        //            //    background: #003b00;
        //            //background:
        //            //    -webkit - gradient(linear, left top, left bottom, from(#fed403), to(#c9a800));
        //            //background: -moz - linear - gradient(top, #003b00, #005500);
        //            //background: linear - gradient(to bottom, #003b00, #005500);
        //            nowInfoBox.Style.Remove("background");

        //            nowInfoBox.Style.Add("background", "#606060");

        //            AddNewMeetingButton.Visible = false;
        //            ClearCurrentMeetingButton.Visible = true;
        //        }
        //        if (googleCalendarEvents != null)
        //        {
        //            if (roomAvailable)
        //            {
        //                //Set the suggestions
        //                firstSuggestion = googleCalendarEvents.Items.Cast<Event>().ElementAt(0);
        //                secondSuggestion = googleCalendarEvents.Items.Cast<Event>().ElementAt(1);
        //            }
        //            else
        //            {
        //                //Find events besides current event
        //                eventsNotActivated = (googleCalendarEvents.Items.Where(ev => !ev.Id.Equals(currentMeeting.GoogleCalendarID)).Select(ev => ev)).ToList();

        //                //Set the suggestions
        //                firstSuggestion = eventsNotActivated.Cast<Event>().ElementAt(0);
        //                secondSuggestion = eventsNotActivated.Cast<Event>().ElementAt(1);
        //            }


        //            //Suggest the first meeting, adjusting for it's time
        //            if (firstSuggestion.Start.DateTime < midnightTonight && firstSuggestion != null)
        //            {
        //                //Indicate the next meeting suggestion
        //                SetNext1Div(TruncateMyLongString(firstSuggestion.Summary, maxStringLength),
        //                    firstSuggestion.Start.DateTime.Value.ToShortTimeString(),
        //                    firstSuggestion.End.DateTime.Value.ToShortTimeString(),
        //                    "Next:");

        //            }
        //            else
        //            {
        //                //Suggest a meeting for another day
        //                SetNext1Div(TruncateMyLongString(firstSuggestion.Summary, maxStringLength),
        //                    firstSuggestion.Start.DateTime.Value.ToShortTimeString(),
        //                    firstSuggestion.End.DateTime.Value.ToShortTimeString(),
        //                    "-- " + firstSuggestion.Start.DateTime.Value.DayOfWeek.ToString() + " --");
        //            }

        //            //Suggest the second meeting, adjusting for it's time
        //            if (secondSuggestion.Start.DateTime < midnightTonight && firstSuggestion != null)
        //            {
        //                //Indicate the next meeting suggestion
        //                SetNext2Div(TruncateMyLongString(secondSuggestion.Summary, maxStringLength),
        //                    secondSuggestion.Start.DateTime.Value.ToShortTimeString(),
        //                    secondSuggestion.End.DateTime.Value.ToShortTimeString(),
        //                    "Next:");
        //            }
        //            else
        //            {
        //                //Suggest a meeting for another day
        //                SetNext2Div(TruncateMyLongString(secondSuggestion.Summary, maxStringLength),
        //                    secondSuggestion.Start.DateTime.Value.ToShortTimeString(),
        //                    secondSuggestion.End.DateTime.Value.ToShortTimeString(),
        //                    "-- " + secondSuggestion.Start.DateTime.Value.DayOfWeek.ToString() + " --");
        //            }
        //            foreach (var meeting in db.Meetings.Where(m => m.MeetingID != 1 && m.RoomID == room.RoomID))
        //            {
        //                RemoveMeetingFromDB(meeting);
        //            }
        //            //Add the first/second suggestions to the meetings table
        //            //Input the meeting to the meetings table under meeting #2

        //            try
        //            {
        //                AddMeetingToDB(new Meeting
        //                {
        //                    MeetingID = 2,
        //                    Name = TruncateMyLongString(firstSuggestion.Summary, 50),
        //                    StartTime = firstSuggestion.Start.DateTime,
        //                    EndTime = firstSuggestion.End.DateTime,
        //                    GoogleCalendarID = TruncateMyLongString(firstSuggestion.Id, 50),
        //                    RoomID = room.RoomID
        //                });

        //                // db.SaveChanges();

        //                //Input the meeting to the meetings table under meeting #3
        //                AddMeetingToDB(new Meeting
        //                {
        //                    MeetingID = 3,
        //                    Name = TruncateMyLongString(secondSuggestion.Summary, 50),
        //                    StartTime = secondSuggestion.Start.DateTime,
        //                    EndTime = secondSuggestion.End.DateTime,
        //                    GoogleCalendarID = TruncateMyLongString(secondSuggestion.Id, 50),
        //                    RoomID = room.RoomID
        //                });
        //                //db.SaveChanges();
        //                db.SaveChanges();
        //            }
        //            catch (Exception ex)
        //            {
        //                nowMeetingNameLabel.Text = ex.ToString();
        //                nowMeetingNameLabel.Font.Size = 10;



        //                //foreach (var validationErrors in ex.EntityValidationErrors)
        //                //{
        //                //    foreach (var validationError in validationErrors.ValidationErrors)
        //                //    {
        //                //        System.Diagnostics.Trace.TraceInformation("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
        //                //    }
        //                //}
        //            }

        //        }


        //    }
        //    if (googleCalendarEvents == null) { nowInfoBoxDescriptionLabel.Text = "Could not access google"; }


        //}
        public void SyncMeetingListWithMeetingDB()
        {
            Meeting currentMeeting = db.Meetings.Where(m => m.Name != "Room Available" && m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault();

            Meeting firstSuggestion;
            Meeting secondSuggestion;

            //True if available, False if not available
            bool roomAvailable = true;

            if (currentMeeting != null)
            {
                roomAvailable = false;
            }

            if (roomAvailable)
            {
                //Show the room is available
                SetNowDiv("Room Available", "", "", (room.RoomName + ":"));
                AddNewMeetingButton.Visible = true;
                ClearCurrentMeetingButton.Visible = false;

                nowInfoBox.Style.Remove("background");

                nowInfoBox.Style.Add("background", "#007600");
                nowInfoBox.Style.Add("background", "-webkit-gradient-webkit-gradient(linear, left top, left bottom, from(#fed403), to(#c9a800))");
                nowInfoBox.Style.Add("background", "-moz-linear-gradient(top, #007600, #006f00))");
                nowInfoBox.Style.Add("background", "linear-gradient(to bottom, #007600, #006f00)");

            }
            else
            {
                //Indicate the current Meeting
                SetNowDiv(TruncateMyLongString(currentMeeting.Name, maxStringLength),
                    currentMeeting.StartTime.Value.ToShortTimeString(),
                    currentMeeting.EndTime.Value.ToShortTimeString(),
                    (room.RoomName + ":"));
                if (currentMeeting.GoogleCalendarID.ToString() == "")
                {
                    SetNowDiv(currentMeeting.Name, "", "", room.RoomName.ToString() + ":");
                }
                //Turn the current meeting box red since it is in meeting
                //    background: #003b00;
                //background:
                //    -webkit - gradient(linear, left top, left bottom, from(#fed403), to(#c9a800));
                //background: -moz - linear - gradient(top, #003b00, #005500);
                //background: linear - gradient(to bottom, #003b00, #005500);
                nowInfoBox.Style.Remove("background");

                nowInfoBox.Style.Add("background", "#606060");

                AddNewMeetingButton.Visible = false;
                ClearCurrentMeetingButton.Visible = true;
            }


            firstSuggestion = db.Meetings.Where(m => m.MeetingID == 2 && m.RoomID == room.RoomID).FirstOrDefault();
            secondSuggestion = db.Meetings.Where(m => m.MeetingID == 3 && m.RoomID == room.RoomID).FirstOrDefault();

            if (firstSuggestion != null && secondSuggestion != null)
            {
                //Suggest the first meeting, adjusting for it's time
                if (firstSuggestion.StartTime < midnightTonight && firstSuggestion != null)
                {
                    //Indicate the next meeting suggestion
                    SetNext1Div(firstSuggestion.Name,
                        firstSuggestion.StartTime.Value.ToShortTimeString(),
                        firstSuggestion.EndTime.Value.ToShortTimeString(),
                        "Next:");

                }
                else
                {
                    //Suggest a meeting for another day
                    SetNext1Div(TruncateMyLongString(firstSuggestion.Name, maxStringLength),
                        firstSuggestion.StartTime.Value.ToShortTimeString(),
                        firstSuggestion.EndTime.Value.ToShortTimeString(),
                        "-- " + firstSuggestion.StartTime.Value.DayOfWeek.ToString() + " --");
                }

                //Suggest the second meeting, adjusting for it's time
                if (secondSuggestion.StartTime < midnightTonight && firstSuggestion != null)
                {
                    //Indicate the next meeting suggestion
                    SetNext2Div(TruncateMyLongString(secondSuggestion.Name, maxStringLength),
                        secondSuggestion.StartTime.Value.ToShortTimeString(),
                        secondSuggestion.EndTime.Value.ToShortTimeString(),
                        "Next:");
                }
                else
                {
                    //Suggest a meeting for another day
                    SetNext2Div(secondSuggestion.Name,
                        secondSuggestion.StartTime.Value.ToShortTimeString(),
                        secondSuggestion.EndTime.Value.ToShortTimeString(),
                        "-- " + secondSuggestion.StartTime.Value.DayOfWeek.ToString() + " --");
                }

                foreach (var meeting in db.Meetings.Where(m => m.MeetingID != 1))
                {
                    db.Meetings.Remove(meeting);
                }
                //Add the first/second suggestions to the meetings table
                //Input the meeting to the meetings table under meeting #2
                db.Meetings.Add(new Meeting
                {
                    MeetingID = 2,
                    Name = firstSuggestion.Name,
                    StartTime = firstSuggestion.StartTime,
                    EndTime = firstSuggestion.EndTime,
                    GoogleCalendarID = "",
                    RoomID = room.RoomID
                });

                db.SaveChanges();

                //Input the meeting to the meetings table under meeting #3
                db.Meetings.Add(new Meeting
                {
                    MeetingID = 3,
                    Name = secondSuggestion.Name,
                    StartTime = secondSuggestion.StartTime,
                    EndTime = secondSuggestion.EndTime,
                    GoogleCalendarID = "",
                    RoomID = room.RoomID
                });
                // TODO Had this commented out
                db.SaveChanges();
            }
            else
            {
                //Suggestions were null
                //Therefore no suggestions are available at this time

                //Indicate there are no suggestions available
                SetNext1Div("No Suggestions Available", "", "", "");
                SetNext2Div("Calendar Unable To Connect", "", "", "");
            }
        }
        public void RemoveMeetingFromDB(Meeting meeting)
        {
            db.Meetings.Remove(meeting);
            //db.SaveChanges();
        }
        public void AddMeetingToDB(Meeting meeting)
        {
            db.Meetings.Add(meeting);
            //db.SaveChanges();
        }
        protected void ClearCurrentMeetingButton_Click(object sender, EventArgs e)
        {
            ResetCurrentMeeting();
        }

        private void ResetCurrentMeeting()
        {
            DeleteCurrentMeeting();

            //Erase info in current meeting div
            ClearNowDiv();

            //Load "Room Available" Event into the database
            Meeting roomAvailableMeeting = new Meeting();
            roomAvailableMeeting.Name = "Room Available";
            roomAvailableMeeting.MeetingID = 1;
            roomAvailableMeeting.RoomID = room.RoomID;

            db.Meetings.Add(roomAvailableMeeting);

            db.SaveChanges();

            RunStartupRoutine();

            //LOAD Meeting List
            //Google Calendar
            //SyncMeetingListWithGoogleCalendar();

            //Exchange Calendar
            SyncMeetingListWithEWSCalendar();

            AddNewMeetingButton.Visible = true;
            ClearCurrentMeetingButton.Visible = false;
        }

        public void AddNewMeetingButton_Click(object sender, EventArgs e)
        {
            DeleteCurrentMeeting();

            //Check if text has been entered
            if (meetingNameTextbox.Text == "")
            {
                EnableNewMeetingControls();
            }
            else
            {
                //Enter the new meeting into the meeting database under item 1
                Meeting newMeeting = new Meeting();
                newMeeting.Name = TruncateMyLongString(meetingNameTextbox.Text, 50);
                newMeeting.MeetingID = 1;
                newMeeting.StartTime = DateTime.Now;
                newMeeting.EndTime = (DateTime.Now + TimeSpan.FromHours(1));
                newMeeting.GoogleCalendarID = "";
                newMeeting.RoomID = room.RoomID;
                db.Meetings.Add(newMeeting);
                db.SaveChanges();

                //Set the meeting on the Now Div
                SetNowDiv(newMeeting.Name, newMeeting.StartTime.Value.ToShortTimeString(), newMeeting.EndTime.Value.ToShortTimeString(), (room.RoomName + ":"));

                EnableNormalControls();

                meetingNameTextbox.Text = "";
                meetingNameTextbox.Focus();

                RunStartupRoutine();

                //LOAD Meeting List
                //Google Calendar
                //SyncMeetingListWithGoogleCalendar();
                //Exchange Calendar
                SyncMeetingListWithEWSCalendar();
            }
        }
        protected void ActivateNext1MeetingButton_Click(object sender, EventArgs e)
        {
            DeleteCurrentMeeting();

            //Find the next1 meeting
            Meeting next1Meeting = db.Meetings.Where(m => m.MeetingID == 2 && m.RoomID == room.RoomID).FirstOrDefault();
            Meeting startingMeeting;

            if (next1Meeting != null)
            {
                //Create a new starting meeting
                startingMeeting = new Meeting
                {
                    MeetingID = 1,
                    GoogleCalendarID = next1Meeting.GoogleCalendarID,
                    Name = next1Meeting.Name,
                    StartTime = next1Meeting.StartTime,
                    EndTime = next1Meeting.EndTime,
                    RoomID = room.RoomID
                };

            }
            else
            {
                startingMeeting = new Meeting
                {
                    MeetingID = 1,
                    GoogleCalendarID = "",
                    Name = next1MeetingNameLabel.Text,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now.AddHours(1),
                    RoomID = room.RoomID
                };

            }
            //Add the meeting to the db under the current meeting
            db.Meetings.Add(startingMeeting);
            db.SaveChanges();

            EnableNormalControls();

            RunStartupRoutine();

            //LOAD Meeting List
            //Google Calendar
            //SyncMeetingListWithGoogleCalendar();
            //Exchange Calendar
            SyncMeetingListWithEWSCalendar();
        }
        protected void ActivateNext2MeetingButton_Click(object sender, EventArgs e)
        {
            DeleteCurrentMeeting();

            //Find the next2 meeting
            Meeting next2Meeting = db.Meetings.Where(m => m.MeetingID == 3 && m.RoomID == room.RoomID).FirstOrDefault();
            Meeting startingMeeting;

            if (next2Meeting != null)
            {
                //Create a new starting meeting
                startingMeeting = new Meeting
                {
                    MeetingID = 1,
                    GoogleCalendarID = next2Meeting.GoogleCalendarID,
                    Name = next2Meeting.Name,
                    StartTime = next2Meeting.StartTime,
                    EndTime = next2Meeting.EndTime,
                    RoomID = room.RoomID
                };
            }
            else
            {
                startingMeeting = new Meeting
                {
                    MeetingID = 1,
                    GoogleCalendarID = "",
                    Name = next2MeetingNameLabel.Text,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now.AddHours(1),
                    RoomID = room.RoomID
                };
            }


            //Add the meeting to the db under the current meeting
            db.Meetings.Add(startingMeeting);
            db.SaveChanges();

            EnableNormalControls();

            RunStartupRoutine();

            //LOAD Meeting List
            //Google Calendar
            //SyncMeetingListWithGoogleCalendar();
            //Exchange Calendar
            SyncMeetingListWithEWSCalendar();
        }
        public void DeleteCurrentMeeting()
        {
            //Remove current meeting
            Meeting meetingToDelete = db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault();
            if (meetingToDelete != null)
            {
                db.Meetings.Remove(meetingToDelete);
                db.SaveChanges();
            }

        }
        //public CalendarService OpenCalendar()
        //{

        //    //THIS WORKS!
        //    UserCredential credential = GoogleWebAuthorizationBroker.
        //            AuthorizeAsync(new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
        //            Scopes,
        //            "user",
        //            CancellationToken.None,
        //            new FileDataStore(Server.MapPath("~/App_Data"))).Result;

        //    // Create Google Calendar API service.
        //    var service = new CalendarService(new BaseClientService.Initializer()
        //    {
        //        HttpClientInitializer = credential,
        //        ApplicationName = ApplicationName,
        //    });

        //    return service;
        //}
        /// <summary>
        /// Queries Google Calendar using the calendar service.  Finds events desired.
        /// </summary>
        /// <param name="service"></param>
        //public Events GetEvents(CalendarService service, int numberEvents)
        //{
        //    // Define parameters of request
        //    EventsResource.ListRequest request = service.Events.List(calID);
        //    request.TimeMin = DateTime.Now;
        //    request.ShowDeleted = false;
        //    request.SingleEvents = true;
        //    request.MaxResults = numberEvents;
        //    request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        //    return request.Execute();
        //}
        public void SetNowDiv(string meetingName, string startTime, string endTime, string meetingDescription)
        {
            nowInfoBoxDescriptionLabel.Text = meetingDescription;
            nowMeetingEndTimeLabel.Text = endTime;
            nowMeetingStartTimeLabel.Text = startTime;
            nowMeetingNameLabel.Text = meetingName;
        }
        public void SetNext1Div(string meetingName, string startTime, string endTime, string meetingDescription)
        {
            next1InfoBoxDescriptionLabel.Text = meetingDescription;
            next1MeetingEndTimeLabel.Text = endTime;
            next1MeetingStartTimeLabel.Text = startTime;
            next1MeetingNameLabel.Text = meetingName;
        }
        public void SetNext2Div(string meetingName, string startTime, string endTime, string meetingDescription)
        {
            next2InfoBoxDescriptionLabel.Text = meetingDescription;
            next2MeetingEndTimeLabel.Text = endTime;
            next2MeetingStartTimeLabel.Text = startTime;
            next2MeetingNameLabel.Text = meetingName;
        }

        /// <summary>
        /// Clears the current event div element
        /// </summary>
        public void ClearNowDiv()
        {
            /*Clears the now div*/

            nowMeetingEndTimeLabel.Text = "";
            nowMeetingStartTimeLabel.Text = "";
            nowMeetingNameLabel.Text = "";
        }

        protected void GoBackButton_Click(object sender, EventArgs e)
        {
            EnableNormalControls();
            meetingNameTextbox.Text = "";
            nameTextBox.Text = "";
            RunStartupRoutine();


            //LOAD Meeting List
            SyncMeetingListWithMeetingDB();
        }

        public void EnableNewMeetingControls()
        {
            newMeetingNameLabel.Visible = true;
            employeeDropDownBox.Visible = false;
            headerElement1.Visible = false;
            nameTextBox.Visible = false;
            headerElement2.Visible = true;
            ConfirmGuestNameButton.Visible = false;
            ConfirmNewMeetingNameButton.Visible = true;
            AddNewMeetingButton.Visible = false;
            ClearCurrentMeetingButton.Visible = false;
            meetingNameTextbox.Visible = true;
            nowMeetingNameLabel.Visible = false;
            goBackButton1.Visible = true;
            goBackButton2.Visible = false;
            clearAllButton.Visible = false;
            refreshButton.Visible = false;
        }
        public void EnableGuestControls()
        {
            GuestAndMeetingNameLabel.Text = "Enter Guest Name:";
            employeeDropDownBox.Visible = false;
            headerElement1.Visible = false;
            nameTextBox.Visible = true;
            headerElement2.Visible = true;
            ConfirmGuestNameButton.Visible = true;
            ConfirmNewMeetingNameButton.Visible = false;
            employeeDropDownBox.SelectedIndex = 1;
            goBackButton2.Visible = true;
            clearAllButton.Visible = false;
            refreshButton.Visible = false;
        }
        public void EnableNormalControls()
        {
            GuestAndMeetingNameLabel.Text = "";
            employeeDropDownBox.Visible = true;
            headerElement1.Visible = true;
            nameTextBox.Visible = false;
            headerElement2.Visible = false;
            ConfirmGuestNameButton.Visible = false;
            ConfirmNewMeetingNameButton.Visible = false;
            AddNewMeetingButton.Visible = false;
            ClearCurrentMeetingButton.Visible = true;
            meetingNameTextbox.Visible = false;
            nowMeetingNameLabel.Visible = true;
            goBackButton1.Visible = false;
            newMeetingNameLabel.Visible = false;
            goBackButton2.Visible = false;
            clearAllButton.Visible = true;
            refreshButton.Visible = true;
        }
        public string TruncateMyLongString(string str, int maxLenth)
        {
            return str.Substring(0, Math.Min(str.Length, maxLenth));
        }
        private static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;

            Uri redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }
    }
}