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
        static int conferenceRoomID = 1;

        // ------------------------------------------------------------ //
        protected void Page_Load(object sender, EventArgs e)
        {
            // Find the room in the database
            room = db.Rooms.Where(room => room.RoomID == conferenceRoomID).FirstOrDefault();

            //If room not recognized in database, set it up by hardcoding
            if(room == null)
            {
                Room newRoom = new Room();
                //Set Conference Room Info
                newRoom.RoomID = conferenceRoomID;
                newRoom.RoomName = "North Conference Room";
                newRoom.RoomStatusID = 10; //North
                newRoom.RoomEmailAddress = "north@i2r.com";
                newRoom.RoomEmailPassword = "Catesuser1";
                newRoom.RoomEmailboxURL = "https://mex07a.emailsrvr.com/EWS/Exchange.asmx";

                //Save room in Database
                db.SaveChanges();

                room = db.Rooms.Where(room => room.RoomID == conferenceRoomID).FirstOrDefault();
            }
            

            //Create Exchange Service
            _service = CreateExchangeService();

            //Run through startup routine if first page view
            if (!IsPostBack)
            {
                //Set page controls
                EnableNormalControls();
                
                //Sync Meeting List with Exchange Calendar
                SyncMeetingListWithEWSCalendar();
                
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
            if (db.Meetings.Where(m => m.RoomID == room.RoomID) == null || db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID) == null)
            {
                //Exchange Calendar
                SyncMeetingListWithEWSCalendar();
                //Meeting Database Table
                SyncMeetingListWithMeetingDB();
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
                    Meeting currentMeeting = db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault();
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
                            }catch
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

            //Create a drop down list entry at the top for employees who are required/optional meeting attendees
            var currentMeetingInvites = db.InvitedAttendees.Where(att => att.Room == room.RoomName);

            if(currentMeetingInvites != null)
            {
                foreach(InvitedAttendee ia in currentMeetingInvites)
                {
                    employeeDropDownBox.Items.Add(CreateListItemWithColor(ia.Name.ToString(), ia.EmployeeID.ToString(),"green"));
                }
            }

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
            int dropDownValue =0;
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
                    if(invitedAttendeeToRemove != null)
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

            //Set page controls//
            EnableNormalControls();

            RunStartupRoutine();

            //Remove the current meeting from the Now Div
            ResetCurrentMeeting();

            //LOAD Meeting List
            //Exchange Calendar
            SyncMeetingListWithEWSCalendar();

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
        public void SyncMeetingListWithEWSCalendar()
        {
            //ExchangeService service = CreateExchangeService();

            if (_service.Url != null)
            {
                // Initialize values for the start and end times, and the number of appointments to retrieve.
                DateTime startDate = DateTime.Now;
                DateTime endDate = startDate.AddYears(1);
                const int NUM_APPTS = 4;

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
                    Meeting currentMeeting = db.Meetings.Where(m => m.Name != "Room Available" && m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault();

                    //Doesn't work
                    if (db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault() == null)
                    {
                        db.Meetings.Add(new Meeting { MeetingID = 1, CalendarID = "", Name = "Room Available", StartTime = null, EndTime = null, RoomID = room.RoomID });
                        //db.SaveChanges();
                    }

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
                        if (currentMeeting.CalendarID.ToString() == "")
                        {
                            SetNowDiv(currentMeeting.Name, "", "", room.RoomName.ToString() + ":");
                        }
                        nowInfoBox.Style.Remove("background");

                        nowInfoBox.Style.Add("background", "#606060");

                        AddNewMeetingButton.Visible = false;
                        ClearCurrentMeetingButton.Visible = true;
                    }

                    // Will store meetings that have not been promoted yet
                    List<Appointment> inactivatedMeetings = new List<Appointment>();

                    //Ensure that appointments from EWS exist
                    if (appointments != null)
                    {
                        //If room is available, suggestions should be first two entries in appointments
                        if (roomAvailable)
                        {
                            //Set the suggestions
                            inactivatedMeetings.Add(appointments.Items.Cast<Appointment>().ElementAt(0));
                            inactivatedMeetings.Add(appointments.Items.Cast<Appointment>().ElementAt(1));
                        }
                        //If room is occupied, suggestions should be appointments NOT listed in appointments
                        else
                        {
                            //Guests previously invited to meeting
                            var invitedAttendeeToDelete = db.InvitedAttendees.Where(iatd => iatd.Room == room.RoomName).Select(iatd => iatd);
                            //if the meeting id is the same, delete those attendees
                            if (invitedAttendeeToDelete != null)
                            {
                                foreach(var iatd in invitedAttendeeToDelete)
                                {
                                    db.InvitedAttendees.Remove(iatd);
                                }
                                //Save Changes
                                db.SaveChanges();
                                
                            }

                            // Loop through appointments and add inactivated appointments to inactivated appointments variable
                            foreach (Appointment a in appointments)
                            {
                                if (!a.Id.ToString().Equals(currentMeeting.CalendarID.ToString()))
                                {
                                    inactivatedMeetings.Add(a);
                                }
                                // TODO: If the appointment is set as the ACTIVE MEETING go ahead and save the attendees who are assigned to the meeting
                                else
                                {
                                    //Check if current meeting is attached to a EWS Calendar
                                    if (currentMeeting.CalendarID != null)
                                    {
                                        // Get current EWS appointment and see its detailed properties
                                        var appointment = Appointment.Bind(_service, a.Id, new PropertySet(BasePropertySet.FirstClassProperties));

                                        // Loop through the appointment attendee invites and add them to the InvitedAttendees data table
                                        foreach (var att in appointment.RequiredAttendees/*.Concat(appointment.OptionalAttendees)*/)
                                        {
                                            //See if an overlapping employee exists
                                            var invitedEmployee = db.Employees
                                                .Where(emp => emp.Email == att.Address)
                                                .FirstOrDefault();

                                            //Find invited attendees
                                            bool invitedGuest = true;

                                            // Add the invited employees to the invited attendees list
                                            if (invitedEmployee != null)
                                            {
                                                // This is not a guest
                                                invitedGuest = false;

                                                // Add the employee to the invited attendees list
                                                db.InvitedAttendees.Add(new InvitedAttendee()
                                                {
                                                    EmployeeID = invitedEmployee.EmployeeID,
                                                    Name = invitedEmployee.FirstName + " " + invitedEmployee.LastName,
                                                    Room = room.RoomName,
                                                    MeetingID = appointment.Id.ToString()
                                                });
                                                //TODO: See if needed
                                                db.SaveChanges();

                                            }


                                            // Add the invited guest to the invited attendees list
                                            // And double check that the guest is not the conference room itself
                                            if (invitedGuest && att.Address != room.RoomEmailAddress)
                                            {
                                                // Give the guest a unique negative ID
                                                Random randomInt = new Random();
                                                int index = 0;

                                                foreach (char c in att.Name)
                                                {
                                                    index += -(int)c % 32;
                                                    
                                                }
                                                index += -1 * randomInt.Next(1, 2000);
                                                db.InvitedAttendees.Add(new InvitedAttendee()
                                                {
                                                    EmployeeID = index,
                                                    Name = att.Name,
                                                    Room = room.RoomName,
                                                    MeetingID = appointment.Id.ToString()
                                                });
                                                // Save Invited Attendee table changes
                                                db.SaveChanges();
                                            }
                                            
                                        }
                                        
                                    }
                                    
                                }
                            }
                        
                    


                            //If all else fails, just set the next two appointments to the first two in the appointments list.
                            if (inactivatedMeetings == null)
                            {
                                inactivatedMeetings.Add(appointments.Items.Cast<Appointment>().ElementAt(0));
                                inactivatedMeetings.Add(appointments.Items.Cast<Appointment>().ElementAt(1));
                            }


                        }
                        // Set first/second suggestions based on entries in inactivated meetings variable
                        firstSuggestion = inactivatedMeetings.ElementAt(0);
                        secondSuggestion = inactivatedMeetings.ElementAt(1);
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
                                CalendarID = firstSuggestion.Id.ToString(),
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
                                CalendarID = secondSuggestion.Id.ToString(),
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
                if (currentMeeting.CalendarID.ToString() == "")
                {
                    SetNowDiv(currentMeeting.Name, "", "", room.RoomName.ToString() + ":");
                }

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
                    CalendarID = "",
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
                    CalendarID = "",
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
                    CalendarID = next1Meeting.CalendarID,
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
                    CalendarID = "",
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
            
            //LOAD Meeting List
            //Exchange Calendar
            SyncMeetingListWithEWSCalendar();

            RunStartupRoutine();
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
                    CalendarID = next2Meeting.CalendarID,
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
                    CalendarID = "",
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
//LOAD Meeting List
            //Exchange Calendar
            SyncMeetingListWithEWSCalendar();
            RunStartupRoutine();

            
        }
        public void DeleteCurrentMeeting()
        {
            //Remove invited attendees
            var invitedAttendees = db.InvitedAttendees.Where(att => att.Room == room.RoomName);
            //Remove current meeting
            Meeting meetingToDelete = db.Meetings.Where(m => m.MeetingID == 1 && m.RoomID == room.RoomID).FirstOrDefault();
            if (meetingToDelete != null)
            {
                db.Meetings.Remove(meetingToDelete);
                if (invitedAttendees != null)
                {
                    foreach (var ia in invitedAttendees)
                    {
                        db.InvitedAttendees.Remove(ia);
                    }
                }

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