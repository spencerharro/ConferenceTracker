﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ConferenceTracker
{
    public partial class Error : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            if (ex != null)
            {
                errorLabel.Text = "Error: " + ex.InnerException.Message;
            }
            else
            {
                errorLabel.Text = "Error unknown...";
            }

        }
    }
}