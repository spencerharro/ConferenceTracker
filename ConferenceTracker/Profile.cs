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
    }
}