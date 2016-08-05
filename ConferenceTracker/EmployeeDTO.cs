using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConferenceTracker
{
    public class EmployeeDTO
    {
        public int EmployeeID { get; set; }
        public int StatusID { get; set; }
        public string Remarks { get; set; }
        public DateTime ReturnDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

    }
}