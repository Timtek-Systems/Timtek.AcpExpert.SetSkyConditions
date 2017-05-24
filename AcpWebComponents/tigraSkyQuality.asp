<%@ LANGUAGE="javascript" %>
<script type="text/javascript" runat="server">

// Writes the supplied sky quality to a named pipe.
// The named pipe is assumed to have been created by the Sky Condition Server.
// Returns: a message string describing the outcome of the operation
function writeSkyQuality(level) 
    {
    var fso = new ActiveXObject("Scripting.FileSystemObject");
    // Establish a Named Pipe client connection
    // The assumption is that a Sky Quality Server has created the pipe and is listening at the other end.
    try 
        {
        var pipe = object.CreateTextFile("\\\\.\\pipe\\tigraSkyQuality", false);
        pipe.WriteLine(level);
        pipe.Close();
        pipe = null;
        return "Success - Sky condition set to level " + level;
        }
    catch (error) 
        {
        return "FAILED: The Sky Condition server was not running"
        }
    }

//
// Main script
//
Response.ContentType = "text/plain";
if (Request.ServerVariables("REQUEST_METHOD").toLowerCase() != "post") 
{
    // Handle the GET request, render a form select element with a Submit button.
    //if (!Session(keyName)) { Session(keyName) = ""; }                  // Make sure this exists for aux forms
    var rootPath = "/";                                                      // URL path to user's logs root        
    var tgtPath = rootPath;
    var physPath = Server.MapPath(tgtPath);                            // Physical path to target logs folder
    Response.Write("Select the required sky quality setting and click Submit\n");
    Response.Write("<html><form name='setSkyQualityForm'>");
    Response.Write("<select name='skyQuality'>");
    Response.Write("<option value='0'>Poor</option>")
    Response.Write("<option value='1'>Fair</option>")
    Response.Write("<option value='2'>Good</option>")
    Response.Write("<option value='3'>Excellent</option>")
    Response.Write("</select></form></html>");
    Response.Write("<<PostForm 'setSkyQualityForm' '/ac/tigraSkyQuality.asp' 'Submit' 'Sets the Sky Quality in ACP Scheduler'>>");
}
else
{ 
    // Handle the POST request.
    var postedSkyQuality = Request.Form("skyQuality");
    if (postedSkyQuality < "0" || postedSkyQuality > "3")
        {
            // Do something about invalid input
        }
    else
    {
        var result = writeSkyQuality(postedSkyQuality);
        // ToDo: pass the result back to the tiddler.
        // Probably: set a session variable with the result message and redirect to self/GET
    }
}
</script>
