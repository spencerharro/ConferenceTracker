using System;
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
//using System;
//using System.Collections.Generic;
using System.IO;
//using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//Microsoft
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Exchange.WebServices;

namespace ConferenceTracker
{
    public partial class Training : System.Web.UI.Page
    {
        // -------------------------PAGE SETUP-------------------------------- //
        //Initialize LINQ to Entities Connection
        I2RloginEntities3 db = new I2RloginEntities3();
        //Initialize Conference Room
        Room room;

        //EWS
        ExchangeService _service;

        //Set midnight tonight time for UI Calendar reference
        static DateTime midnightTonight = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, 0, 0, 0);

        //Max string characters displayed in meeting boxes
        static int maxStringLength = 33;

        //Set ConferenceRoomID
        static int conferenceRoomID = 3;

        // ------------------------------------------------------------ //
        protected void Page_Load(object sender, EventArgs e)
        {
            //Run through startup routine if first page view
            if (!IsPostBack)
            {
                //Set page controls
                EnableNormalControls();

                //Sync Meeting Database with Exchange Calendar
                GetExchangeWebServicesAppointments();

                //Sync Meetings Displayed with Meeting Database
                SyncMeetingSuggestionsInDatabase();

                //Run Startup Routine
                RunStartupRoutine();
            }

            //Set Attendees Box description text
            AttendeesBoxDescriptionLabel.Text = "Attendees in " + room.RoomName.ToString();
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

            //Sync Meeting List with Exchange Calendar (if no events in db)
            if (db.Meetings.Where(m => m.RoomID == room.RoomID) == null || db.Meetings.Where(m => m.MeetingID == 0 && m.RoomID == room.RoomID) == null)
            {
                //Exchange Calendar
                GetExchangeWebServicesAppointments();
                //Meeting Database Table
                SyncMeetingSuggestionsInDatabase();
            }

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
                    Meeting currentMeeting = db.Meetings.Where(m => m.MeetingID == 0 && m.RoomID == room.RoomID).FirstOrDefault();
                    if (currentMeeting != null)
                    {
                        emp.Remarks = room.RoomName + ((currentMeeting.Name == "Room Available") ? "" : ": " + currentMeeting.Name.ToString());

                        //Set SB return time
                        if (emp.ReturnDate == null)
                        {
                            try
                            {
                                DateTime startTime = DateTime.Parse(currentMeeting.StartTime.ToString());
                                DateTime endTime = DateTime.Parse(currentMeeting.EndTime.ToString());
                                TimeSpan duration = endTime.Subtract(startTime);
                                DateTime nowTime = DateTime.Now;
                                emp.ReturnDate = nowTime + duration;
                            }
                            catch
                            {

                            }
                        }

                    }

                    //Check if the Status ID shows the emp out of the room, and if the emp is an an attendee
                    if (emp.StatusID != room.RoomStatusID && StatusBoardAttendee.EmployeeID != 0)
                    {
                        //Remove emp from attendees
                        RemoveAttendeeByEmployeeID(StatusBoardAttendee.EmployeeID);
                    }
                    //Check if the Status ID shows the emp in the room, and if the emp is not already an attendee
                    else if (emp.StatusID == room.RoomStatusID && StatusBoardAttendee.EmployeeID == 0 && emp.ReturnDate != null)
                    {
                        AddAttendee(CreateAttendee(emp.EmployeeID,
                            room.RoomName.ToString(),
                            Convert.ToDateTime(emp.ReturnDate),
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
            List<Employee> employees = db.Employees
                .Where(ee => (ee.Division == "Fab Shop" || ee.Division == "Office"))
                .ToList();

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
        public ListItem CreateListItemWithColor(string text, string value, string color)
        {

            //Create the list item based on input text/value
            ListItem li = new ListItem();
            li.Text = text;
            li.Value = value;
            li.Attributes.Add("style", "color:" + color);
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
            //TODO: Remove TEST
            int dropDownIndex = employeeDropDownBox.SelectedIndex;
            int dropDownValue = 0;
            try
            {
                dropDownValue = Int32.Parse(employeeDropDownBox.SelectedValue.ToString());
            }
            catch { }

            string dropDownText = employeeDropDownBox.SelectedItem.ToString();

            //Create the new attendee
            Attendee newAttendee = new Attendee();

            //Check if anything was selected
            if (employeeDropDownBox.SelectedIndex != 0 || nameTextBox.Text != "")
            {
                //Check if selected attendee is guest
                if ((dropDownIndex == 1 && nameTextBox.Text == ""))
                {
                    //Set page controls
                    EnableGuestControls();
                }
                //Check if selected attendee is guest who was an invited attendee
                else if (dropDownValue <= -2)
                {
                    // Add the Attendee based on their suggested guest name (from the invited attendees list)
                    AddAttendee(CreateAttendee(-1, room.RoomName, DateTime.Now.AddHours(1), dropDownText));
                    // Find the correct invited attendee to delete based on the ID of the attendee checking in
                    var invitedAttendeeToRemove = db.InvitedAttendees.Where(att => att.EmployeeID == dropDownValue && att.Room == room.RoomName).FirstOrDefault();
                    if (invitedAttendeeToRemove != null)
                    {
                        db.InvitedAttendees.Remove(invitedAttendeeToRemove);
                        db.SaveChanges();
                    }
                }
                //Otherwise
                else if (nameTextBox.Text != "")
                {
                    //Create the guest attendee
                    AddAttendee(CreateAttendee(-1, room.RoomName,
                        DateTime.Now.AddHours(1),   //Default meeting length 1hr
                        nameTextBox.Text.ToString()));

                    db.SaveChanges();

                    //Set page controls
                    EnableNormalControls();
                }
                else if (dropDownIndex > 1 && dropDownValue > 0)
                {
                    //Find the employee in the Employee database
                    int employeeID = Int32.Parse(employeeDropDownBox.SelectedValue);
                    var checkingInEmployee = db.Employees
                        .Where(ee => ee.EmployeeID == employeeID)
                        .FirstOrDefault();

                    //Modify Status Board entries
                    checkingInEmployee.StatusID = room.RoomStatusID;
                    checkingInEmployee.Remarks = room.RoomName.ToString()
                        + (nowMeetingNameLabel.Text == "Room Available" ? "" : ": "
                        + nowMeetingNameLabel.Text.ToString());  //Change Status Board Remarks

                    //Convert the employee to an attendee
                    AddAttendee(CreateAttendee(checkingInEmployee.EmployeeID,
                        room.RoomName,
                        DateTime.Now.AddHours(1),
                        checkingInEmployee.FirstName.ToString() + " " + checkingInEmployee.LastName.ToString()));

                    // See if the attendee was an invited employee
                    var invitedAttendeeToRemove = db.InvitedAttendees.Where(att => att.EmployeeID == dropDownValue && att.Room == room.RoomName).FirstOrDefault();
                    if (invitedAttendeeToRemove != null)
                    {
                        db.InvitedAttendees.Remove(invitedAttendeeToRemove);
                    }

                    db.SaveChanges();
                }

            }
            //Update the Attendees list on the webpage
            RunStartupRoutine();

            //LOAD Meeting List
            SyncMeetingSuggestionsInDatabase();
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
            SyncMeetingSuggestionsInDatabase();
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
            //Local Database of Meetings
            SyncMeetingSuggestionsInDatabase();

        }
        public void RemoveAttendeeByEmployeeID(int employeeID)
        {
            //Find the employee based on the employee ID
            var checkingOutEmployee = db.Employees
                .Where(ee => ee.EmployeeID == employeeID)
                .FirstOrDefault();

            checkingOutEmployee.ReturnDate = null;
            checkingOutEmployee.Remarks = null;
            checkingOutEmployee.StatusID = 1;     //Employee is in office again

            //Find the attendee by comparing employee ids
            var checkingOutAttendee = db.Attendees.Where(a => a.EmployeeID == checkingOutEmployee.EmployeeID).FirstOrDefault();

            //Remove attendee
            db.Attendees.Remove(checkingOutAttendee);
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
                    var checkingOutEmployee = db.Employees
                        .Where(ee => ee.EmployeeID == checkingOutAttendee.EmployeeID)
                        .FirstOrDefault();

                    //Set Status Board properties to basic in office Setup
                    checkingOutEmployee.ReturnDate = null;
                    checkingOutEmployee.Remarks = null;
                    checkingOutEmployee.StatusID = 1;     //Employee is in office again
                }

                //Remove attendee
                db.Attendees.Remove(checkingOutAttendee);
            }
        }
        public void GetExchangeWebServicesAppointments()
        {
            ExchangeService _service = CreateExchangeService();

            if (_service.Url != null)
            {
                // Initialize values for the start and end times, and the number of appointments to retrieve.
                DateTime startDate = DateTime.Now;
                DateTime endDate = startDate.AddYears(1);
                const int NUM_APPTS = 6;

                // Initialize the calendar folder object with only the folder ID. 
                CalendarFolder calendar = CalendarFolder.Bind(_service, WellKnownFolderName.Calendar, new PropertySet());

                // Set the start and end time and number of appointments to retrieve.
                CalendarView cView = new CalendarView(startDate, endDate, NUM_APPTS);

                // Limit the properties returned to the appointment's subject, start time, and end time.
                cView.PropertySet = new PropertySet(AppointmentSchema.Subject,
                    AppointmentSchema.Start,
                    AppointmentSchema.End);

                // Retrieve a collection of appointments by using the calendar view.
                FindItemsResults<Appointment> appointments = calendar.FindAppointments(cView);

                //Check if there are appointments
                if (appointments != null)
                {
                    // Find current meeting in database
                    Meeting currentMeeting = db.Meetings.Where(m => m.MeetingID == 0 && m.RoomID == room.RoomID).FirstOrDefault();

                    List<Appointment> inactivatedMeetings = new List<Appointment>();

                    // If meetings exist in the database
                    if (currentMeeting != null)
                    {
                        // Find appointments that are not activated
                        foreach (var a in appointments)
                        {
                            // If the selected appointment does not match the current meeting
                            if (!a.Id.Equals(currentMeeting.MeetingID))
                            {
                                inactivatedMeetings.Add(a);
                            }
                        }

                        // Suggestion Counter
                        int index = 1;

                        //first delete the meeting suggestions (ID's > 0) already in database
                        foreach (var m in db.Meetings.Where(m => m.RoomID == room.RoomID && m.MeetingID != 0))
                        {
                            db.Meetings.Remove(m);

                        }

                        // Add the inactivated meetings to the Meetings database
                        foreach (var a in inactivatedMeetings)
                        {

                            try
                            {
                                // Add meeting using appointment details
                                AddMeetingToDB(new Meeting
                                {
                                    MeetingID = index,
                                    Name = TruncateMyLongString(a.Subject, 50),
                                    StartTime = a.Start,
                                    EndTime = a.End,
                                    CalendarID = a.Id.ToString(),
                                    RoomID = room.RoomID
                                });

                                // Save database changes
                                db.SaveChanges();



                            }
                            // Catch errors adding meetings to database from EWS
                            catch (Exception ex)
                            {
                                nowMeetingNameLabel.Text = ex.ToString();
                                nowMeetingNameLabel.Font.Size = 10;
                            }
                            // Increment index
                            index += 1;
                        }

                    }
                }
                // If no appointments are returned from EWS write out error in Now Div
                if (appointments == null) { nowInfoBoxDescriptionLabel.Text = "Could not access Exchange Calendar"; }
            }
        }
        private ExchangeService CreateExchangeService()
        {
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2007_SP1);

            service.Credentials = new WebCredentials(room.RoomEmailAddress, room.RoomEmailPassword);

            try
            {
                // Previous saved connection string
                service.Url = new Uri(room.RoomEmailboxURL);
            }
            catch
            {
                try
                {
                    // Use autodiscover service to relocate url
                    service.AutodiscoverUrl(room.RoomEmailAddress, RedirectionUrlValidationCallback);
                    //Save new url TODO
                    room.RoomEmailAddress = service.Url.AbsoluteUri;
                    db.SaveChanges();

                }
                catch (Exception ex)
                {
                    // Return an error that the url could not be found (In Now Div)
                    nowMeetingNameLabel.Text = ex.ToString();
                    nowMeetingNameLabel.Font.Size = 10;
                }
            }

            return service;
        }
        public void SyncMeetingSuggestionsInDatabase()
        {
            // Find the current meeting
            Meeting currentMeeting = db.Meetings.Where(m => m.MeetingID == 0 && m.RoomID == room.RoomID).FirstOrDefault();
            bool roomAvailable = true;

            // If the database has no meeting entries
            if (currentMeeting == null)
            {
                try
                {
                    // Add a current meeting (set room to Room Available status)
                    db.Meetings.Add(new Meeting
                    {
                        CalendarID = "",
                        Name = "Room Available",
                        MeetingID = 0,
                        RoomID = room.RoomID,
                        StartTime = null,
                        EndTime = null
                    });

                    // Save current meeting to database
                    db.SaveChanges();

                    //Update Suggestions list with EWS calendar
                    GetExchangeWebServicesAppointments();


                    // Update current Meeting to not be null
                    currentMeeting = db.Meetings.Where(m => m.MeetingID == 0 && m.RoomID == room.RoomID).FirstOrDefault();


                    // Update current Meeting to not be null
                    currentMeeting = db.Meetings.Where(m => m.MeetingID == 0 && m.RoomID == room.RoomID).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    nowMeetingNameLabel.Text = ex.ToString();
                    nowMeetingNameLabel.Font.Size = 10;
                }
            }

            // If the database has meeting entries
            if (currentMeeting != null)
            {
                // See if the room is Available
                roomAvailable = currentMeeting.Name.Equals("Room Available");

                if (roomAvailable)
                {
                    // Set the Now Div to Room Available Settings
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
                    // Set the Now Div to Current Meeting Settings
                    //Indicate the current Meeting
                    SetNowDiv(TruncateMyLongString(currentMeeting.Name, maxStringLength),
                        currentMeeting.StartTime.Value.ToShortTimeString(),
                        currentMeeting.EndTime.Value.ToShortTimeString(),
                        (room.RoomName + ":"));
                    if (currentMeeting.CalendarID.ToString() == "")
                    {
                        SetNowDiv(currentMeeting.Name, "", "", room.RoomName.ToString() + ":");
                    }

                    nowInfoBox.Style.Remove("background");

                    nowInfoBox.Style.Add("background", "#606060");

                    AddNewMeetingButton.Visible = false;
                    ClearCurrentMeetingButton.Visible = true;
                }
            }

            // Meeting Suggestions - not including current meeting
            List<Meeting> meetingSuggestions = new List<Meeting>();
            meetingSuggestions = db.Meetings.Where(m => !m.MeetingID.Equals(currentMeeting.MeetingID) && m.RoomID == room.RoomID).ToList();

            // Sync up the Meeting Database with the new suggestions
            foreach (var m in db.Meetings.Where(m => m.RoomID == room.RoomID && m.MeetingID != 0))
            {
                db.Meetings.Remove(m);
            }
            //Counting index
            int index = 1;
            foreach (var m in meetingSuggestions)
            {
                // Add the new meeting with the new adjusted index
                db.Meetings.Add(new Meeting
                {
                    MeetingID = index,
                    Name = m.Name,
                    CalendarID = m.CalendarID,
                    RoomID = m.RoomID,
                    StartTime = m.StartTime,
                    EndTime = m.EndTime
                });
                index += 1;
            }
            Meeting firstSuggestion;
            Meeting secondSuggestion;

            if (meetingSuggestions.Count >= 1)
            {
                firstSuggestion = meetingSuggestions.ElementAt(0);  //first suggestion at index = 0
            }
            else { firstSuggestion = null; }
            if (meetingSuggestions.Count >= 2)
            {
                secondSuggestion = meetingSuggestions.ElementAt(1); //second suggestion at index = 1
            }
            else { secondSuggestion = null; }

            // If a first suggestion exists
            if (firstSuggestion != null)
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
            }
            // If no first suggestion exists
            else
            {
                // Error message
                SetNext1Div("Refresh Suggestions", "", "", "");
            }

            // If a second suggestion exists
            if (secondSuggestion != null)
            {
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
            }
            // If no second suggestion exists
            else
            {
                SetNext2Div("Refresh Suggestions", "", "", "");
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
            roomAvailableMeeting.MeetingID = 0;
            roomAvailableMeeting.RoomID = room.RoomID;

            db.Meetings.Add(roomAvailableMeeting);

            db.SaveChanges();

            RunStartupRoutine();

            //LOAD Meeting List
            //Local Meeting Database
            SyncMeetingSuggestionsInDatabase();

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
                newMeeting.MeetingID = 0;
                newMeeting.StartTime = DateTime.Now;
                newMeeting.EndTime = (DateTime.Now + TimeSpan.FromHours(1));
                newMeeting.CalendarID = "";
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
                //Load local meetign Database
                SyncMeetingSuggestionsInDatabase();
            }
        }
        protected void ActivateNext1MeetingButton_Click(object sender, EventArgs e)
        {
            DeleteCurrentMeeting();

            //Find the next1 meeting
            Meeting next1Meeting = db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault();
            Meeting startingMeeting;

            if (next1Meeting != null)
            {
                //Create a new starting meeting
                startingMeeting = new Meeting
                {
                    MeetingID = 0,
                    CalendarID = next1Meeting.CalendarID,
                    Name = next1Meeting.Name,
                    StartTime = next1Meeting.StartTime,
                    EndTime = next1Meeting.EndTime,
                    RoomID = room.RoomID
                };

                //Add the meeting to the db under the current meeting
                db.Meetings.Add(startingMeeting);
                db.SaveChanges();

            }
            else
            {
                //Create a new Room Available starting meeting
                startingMeeting = new Meeting
                {
                    MeetingID = 0,
                    CalendarID = "",
                    Name = "Room Available",
                    StartTime = null,
                    EndTime = null,
                    RoomID = room.RoomID
                };
                //Add the meeting to the db under the current meeting
                db.Meetings.Add(startingMeeting);
                db.SaveChanges();

                GetExchangeWebServicesAppointments();
                SyncMeetingSuggestionsInDatabase();

            }

            //Delete the meeting that just got promoted
            if (next1Meeting != null)
            {
                db.Meetings.Remove(next1Meeting);
                db.SaveChanges();

                SyncMeetingSuggestionsInDatabase();
            }

            EnableNormalControls();



            // Automatically update calendar if no suggestions available
            if (db.Meetings.Where(m => m.RoomID == room.RoomID && m.MeetingID > 0 && m.CalendarID != "").FirstOrDefault() == null)
            {
                GetExchangeWebServicesAppointments();
                SyncMeetingSuggestionsInDatabase();
            }

            RunStartupRoutine();
        }
        protected void ActivateNext2MeetingButton_Click(object sender, EventArgs e)
        {
            DeleteCurrentMeeting();

            //Find the next2 meeting
            Meeting next2Meeting = db.Meetings.Where(m => m.MeetingID == 2 && m.RoomID == room.RoomID).FirstOrDefault();
            Meeting startingMeeting;

            if (next2Meeting != null)
            {
                //Create a new starting meeting
                startingMeeting = new Meeting
                {
                    MeetingID = 0,
                    CalendarID = next2Meeting.CalendarID,
                    Name = next2Meeting.Name,
                    StartTime = next2Meeting.StartTime,
                    EndTime = next2Meeting.EndTime,
                    RoomID = room.RoomID
                };

                //Add the meeting to the db under the current meeting
                db.Meetings.Add(startingMeeting);
                db.SaveChanges();

            }
            else
            {
                //Create a new Room Available starting meeting
                startingMeeting = new Meeting
                {
                    MeetingID = 0,
                    CalendarID = "",
                    Name = "Room Available",
                    StartTime = null,
                    EndTime = null,
                    RoomID = room.RoomID
                };
                //Add the meeting to the db under the current meeting
                db.Meetings.Add(startingMeeting);
                db.SaveChanges();

                GetExchangeWebServicesAppointments();
                SyncMeetingSuggestionsInDatabase();

            }

            //Delete the meeting that just got promoted
            if (next2Meeting != null)
            {
                db.Meetings.Remove(next2Meeting);
                db.SaveChanges();

                SyncMeetingSuggestionsInDatabase();
            }

            EnableNormalControls();

            // Automatically update calendar if no suggestions available
            if (db.Meetings.Where(m => m.RoomID == room.RoomID && m.MeetingID > 0 && m.CalendarID != "").FirstOrDefault() == null)
            {
                GetExchangeWebServicesAppointments();
                SyncMeetingSuggestionsInDatabase();
            }

            RunStartupRoutine();


        }
        public void DeleteCurrentMeeting()
        {

            //Remove current meeting
            Meeting meetingToDelete = db.Meetings.Where(m => m.MeetingID == 0 && m.RoomID == room.RoomID).FirstOrDefault();
            if (meetingToDelete != null)
            {
                db.Meetings.Remove(meetingToDelete);

                db.SaveChanges();
            }

        }
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
            SyncMeetingSuggestionsInDatabase();
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

        protected void refreshButton_Click(object sender, EventArgs e)
        {
            //Sync the current meeting database with EWS
            GetExchangeWebServicesAppointments();

            //Then Sync meetings in meeting DB
            SyncMeetingSuggestionsInDatabase();
        }
    }
}