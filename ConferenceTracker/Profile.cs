using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConferenceTracker
{
    public class Profile
    {
        public string emailAddress { get; set; }
        public string emailPassword { get; set; }
        public string emailURL { get; set; }
        public Room room { get; set; }
        public int conferenceRoomStatusID { get; set; }

        public Profile SetupRoomProfile(string roomName, I2RloginEntities db)
        {
            Profile profile = new Profile();
            Room r = new Room();

            // Choose which profile based on room
            switch (roomName)
            {
                case "North":
                    profile.room.RoomName = "North Conference Room";
                    profile.conferenceRoomStatusID = 10;
                    db.SaveChanges();

                    //Exchange setup
                    profile.emailAddress = "north@i2r.com";
                    profile.emailPassword = "Catesuser1";
                    profile.emailURL = "https://mex07a.emailsrvr.com/EWS/Exchange.asmx";

                    break;
                case "South":

                    //Setup room
                    profile.room.RoomID = 1; // 1 for North
                    profile.room.RoomName = "South Conference Room";
                    profile.conferenceRoomStatusID = 11;

                    //Setup Exchange
                    profile.emailAddress = "south@i2r.com";
                    profile.emailPassword = "Catesuser1";
                    profile.emailURL = "https://mex07a.emailsrvr.com/EWS/Exchange.asmx";

                    break;
                case "Training":

                    //Setup room
                    profile.room.RoomID = 1; // 1 for North
                    profile.room.RoomName = "Training Room";
                    profile.conferenceRoomStatusID = 12;

                    //Setup Exchange
                    profile.emailAddress = "training@i2r.com";
                    profile.emailPassword = "Catesuser1";
                    profile.emailURL = "https://mex07a.emailsrvr.com/EWS/Exchange.asmx";

                    break;

            };
            return profile;
        }
    }
}