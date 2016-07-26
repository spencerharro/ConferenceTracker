using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConferenceTracker
{
    public class ProfileSetup
    {

        public Profile SetupRoomProfile(string roomName)
        {
            Profile profile = new Profile();

            // Choose which profile based on room
            switch (roomName)
            {
                case "North":

                    //Setup room
                    profile.room.RoomID = 1;
                    profile.room.RoomName = "North Conference Room";

                    //Exchange setup
                    profile.emailAddress = "north@i2r.com";
                    profile.emailPassword = "Catesuser1";
                    profile.emailURL = "https://mex07a.emailsrvr.com/EWS/Exchange.asmx";

                    break;
                case "South":

                    //Setup room
                    profile.room.RoomID = 1; // 1 for North
                    profile.room.RoomName = "South Conference Room";

                    //Setup Exchange
                    profile.emailAddress = "south@i2r.com";
                    profile.emailPassword = "Catesuser1";
                    profile.emailURL = "https://mex07a.emailsrvr.com/EWS/Exchange.asmx";

                    break;
                case "Training":

                    //Setup room
                    profile.room.RoomID = 1; // 1 for North
                    profile.room.RoomName = "Training Room";

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