using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace ConferenceTracker
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {

        }


        protected void Application_Error(object sender, EventArgs e)
        {
            //What happened?
            Exception ex = Server.GetLastError();

            var innerException = ex.InnerException;

            Server.Transfer("Error.aspx");
            //Response.Write("<h2>Something bad happened...but don't panic!</h2>");
            //Response.Write("<p>" + innerException.Message + "</p>");

            //Must do this to hide the yellow page of death....Any existing exceptions after this point will send the end user the exception page
            Server.ClearError();
        }
        
    }
}