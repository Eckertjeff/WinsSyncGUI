﻿//#define debug
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Reflection;
using Microsoft.Win32.TaskScheduler;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Requests;

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.PhantomJS;
using System.Security;
using System.Runtime.InteropServices;
//using OpenQA.Selenium.Firefox;

public class Cell
{
    public string value;
}

public class Row
{
    public List<Cell> cells = new List<Cell>();
}

public class ScheduleGETException : Exception
{
    public ScheduleGETException() : base()
    {

    }
    
    public ScheduleGETException(string message) : base(message)
    {

    }

    public ScheduleGETException(string message, Exception inner) : base(message, inner)
    {

    }
}

public class WorkDay
{
    private DayEnum day;
    private DateTime date;
    private float hours;
    private string activity;
    private string location;
    private string starttime;
    private string endtime;
    private DateTime startdatetime;
    private DateTime enddatetime;
    private string comments;

    public enum DayEnum { Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, INVALID }

    public DayEnum enumDay(string value)
    {
        if (value.Contains("Sun"))
        {
            return DayEnum.Sunday;
        }
        else if (value.Contains("Mon"))
        {
            return DayEnum.Monday;
        }
        else if (value.Contains("Tue"))
        {
            return DayEnum.Tuesday;
        }
        else if (value.Contains("Wed"))
        {
            return DayEnum.Wednesday;
        }
        else if (value.Contains("Thu"))
        {
            return DayEnum.Thursday;
        }
        else if (value.Contains("Fri"))
        {
            return DayEnum.Friday;
        }
        else if (value.Contains("Sat"))
        {
            return DayEnum.Saturday;
        }
        return DayEnum.INVALID;
    }

    public DayEnum Day
    {
        get
        {
            return day;
        }
        set
        {
            day = value;
        }
    }

    public DateTime Date
    {
        get
        {
            return date;
        }

        set
        {
            date = value;
        }
    }

    public float Hours
    {
        get
        {
            return hours;
        }

        set
        {
            hours = value;
        }
    }

    public string Activity
    {
        get
        {
            return activity;
        }

        set
        {
            activity = value;
        }
    }

    public string Location
    {
        get
        {
            return location;
        }

        set
        {
            location = value;
        }
    }

    public string StartTime
    {
        get
        {
            return starttime;
        }

        set
        {
            starttime = value;
        }
    }

    public string EndTime
    {
        get
        {
            return endtime;
        }

        set
        {
            endtime = value;
        }
    }

    public DateTime StartDateTime
    {
        get
        {
            return startdatetime;
        }

        set
        {
            startdatetime = value;
        }
    }

    public DateTime EndDateTime
    {
        get
        {
            return enddatetime;
        }

        set
        {
            enddatetime = value;
        }
    }

    public string Comments
    {
        get
        {
            return comments;
        }

        set
        {
            comments = value;
        }
    }

    public string ConvertLocation(string loc)  //todo: add more stores.
    {
        if (loc.Contains("ST030"))
        {
            return "6789 E Genesee St, Fayetteville, NY 13066";
        }
        else
        {
            return loc;  // Address of the store isn't known, so just leave it as the store number.
        }
    }
}

public class WinsSync
{
    private string[] scopes = { CalendarService.Scope.Calendar };
    private string applicationname = "WinsSync";
    private string calendarid = "primary";
    private string username;
    private string password;
    private SecureString securePwd;
    private string automate;
    private bool savedlogin;
    private UserCredential credential;
    private List<string> m_schedules;
    private CalendarService service;
    private List<string> correctTable;
    private List<Row> rows;
    private List<WorkDay> schedule;

    public string Username
    {
        get
        {
            return username;
        }

        set
        {
            username = value;
        }
    }

    public string Automate
    {
        get
        {
            return automate;
        }

        set
        {
            automate = value;
        }
    }

    public bool Savedlogin
    {
        get
        {
            return savedlogin;
        }

        set
        {
            savedlogin = value;
        }
    }

    public List<string> Schedules
    {
        get
        {
            return m_schedules;
        }

        set
        {
            m_schedules = value;
        }
    }

    public CalendarService Service
    {
        get
        {
            return service;
        }

        set
        {
            service = value;
        }
    }

    public UserCredential Credential
    {
        get
        {
            return credential;
        }

        set
        {
            credential = value;
        }
    }

    public string Applicationname
    {
        get
        {
            return applicationname;
        }

        set
        {
            applicationname = value;
        }
    }

    public string Calendarid
    {
        get
        {
            return calendarid;
        }

        set
        {
            calendarid = value;
        }
    }

    public string[] Scopes
    {
        get
        {
            return scopes;
        }

        set
        {
            scopes = value;
        }
    }

    public List<WorkDay> Schedule
    {
        get
        {
            return schedule;
        }

        set
        {
            schedule = value;
        }
    }

    public List<Row> Rows
    {
        get
        {
            return rows;
        }

        set
        {
            rows = value;
        }
    }

    public List<string> CorrectTable
    {
        get
        {
            return correctTable;
        }

        set
        {
            correctTable = value;
        }
    }

    public void setSPassword(SecureString password)
    {
        securePwd = password;
    }

    public WinsSync()
    {
        Username = string.Empty;
        password = string.Empty;
        Automate = string.Empty;
        Savedlogin = false;
        securePwd = new SecureString();
        Schedules = new List<string>();
        CorrectTable = new List<string>();
        Rows = new List<Row>();
        Schedule = new List<WorkDay>();
    }

    public void SetupGoogleCreds()
    {
        
        // Getting the full path of client_secret is required for running with Windows Task Scheduler.
        string secretpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\client_secret.json";
        using (var stream =
            new FileStream(secretpath, FileMode.Open, FileAccess.Read))
        {
            string credPath = Environment.GetFolderPath(
                Environment.SpecialFolder.Personal);
            credPath = Path.Combine(credPath, ".credentials");

            Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets, Scopes, "user", CancellationToken.None,
                new FileDataStore(credPath, true)).Result;
            Console.WriteLine("Credentials files saved to: " + credPath);
        }
        Service = new CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = Credential,
            ApplicationName = Applicationname,
        });
    }

    public void LoadLoginCreds()
    {
        string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        string logincredPath = Path.Combine(credPath, ".credentials/login.txt");
        string entropyPath = Path.Combine(credPath, ".credentials/entropy.txt");
        byte[] logincreds = null, entropy = null;
        string ptcreds = string.Empty;
        string[] creds;
        try
        {
            logincreds = File.ReadAllBytes(logincredPath);
            entropy = File.ReadAllBytes(entropyPath);
        }
        catch (Exception)
        {
            Console.WriteLine("No Saved Login Credentials Detected.");
        }
        finally
        {
            if (logincreds != null)
            {
                byte[] credbytes = ProtectedData.Unprotect(logincreds, entropy, DataProtectionScope.CurrentUser);
                ptcreds = Encoding.UTF8.GetString(credbytes);
                creds = ptcreds.Split('\n');
                Username = creds[0];
                for (int i = 0; i < creds[1].Length; i++)
                {
                    securePwd.AppendChar(creds[1][i]);
                }
                Savedlogin = true;
                try
                {
                    if (creds[2] == "Automate")
                    {
                        Automate = creds[2];
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    // creds[2] doesn't exist, so it can't possibly hold "Automate".
                }
                if (Automate != "Automate")
                {
                    RemoveAutomationTask(); // Cleanup automation task if user credentials were reset.
                    Console.Write("Would you like to use your saved login info? Y/N: ");
                    if (!Console.ReadLine().Equals("y", StringComparison.OrdinalIgnoreCase)) // Login creds but are not used.
                    {
                        GetLoginCreds();
                        SaveLoginCreds();
                    }
                }
            }
            else // Login creds don't exist
            {
                GetLoginCreds();
                SaveLoginCreds();
            }
        }
    }

    public void GetLoginCreds()
    {
        Console.Write("Please Enter your username: ");
        Username = Console.ReadLine();
        if (!(Username.Contains("@"))) // If they just entered their employee number, add the rest for them.
        {
            Username = Username + "@Wegmans.com";
        }
        Console.Write("Please Enter your password: ");
        const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
        int[] FILTERED = { 0, 27, 9, 10 /*, 32 space */ };
        char chr = (char)0;

        while ((chr = Console.ReadKey(true).KeyChar) != ENTER)
        {
            if (chr == BACKSP)
            {
                if (securePwd.Length > 0)
                {
                    Console.Write("\b \b");
                    securePwd.RemoveAt(securePwd.Length - 1);
                }
            }
            else if (chr == CTRLBACKSP)
            {
                while (securePwd.Length > 0)
                {
                    Console.Write("\b \b");
                    securePwd.RemoveAt(securePwd.Length - 1);
                }
            }
            else if (FILTERED.Count(x => chr == x) > 0) { }
            else
            {
                securePwd.AppendChar(chr);
                Console.Write("*");
            }
        }
        Console.WriteLine();

        //Password = ConvertToUnsecureString(securePwd);
    }

    public string ConvertToUnsecureString(SecureString securePassword)
    {
        // Taken from: http://blogs.msdn.com/b/fpintos/archive/2009/06/12/how-to-properly-convert-securestring-to-string.aspx
        if (securePassword == null)
            throw new ArgumentNullException("securePassword is Null");

        IntPtr unmanagedString = IntPtr.Zero;
        try
        {
            unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
            return Marshal.PtrToStringUni(unmanagedString);
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
        }
    }

    public void SaveLoginCreds(string automate = "")
    {
        string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        string logincredPath = Path.Combine(credPath, ".credentials/login.txt");
        string entropyPath = Path.Combine(credPath, ".credentials/entropy.txt");
        string logincreds = string.Empty;
        if (automate == "Automate")
        {
            password = ConvertToUnsecureString(securePwd);
            logincreds = Username + "\n" + password + "\n" + automate;
            ClearPassword();
            byte[] plaintextcreds = Encoding.UTF8.GetBytes(logincreds);
            byte[] entropy = new byte[20];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(entropy);
            }

            byte[] encryptedcreds = ProtectedData.Protect(plaintextcreds, entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(logincredPath, encryptedcreds);
            File.WriteAllBytes(entropyPath, entropy);
            savedlogin = true;
            Automate = automate;
        }
        else
        {
            Console.Write("Would you like to save your login info? Y/N: ");
            if (Console.ReadLine().Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                password = ConvertToUnsecureString(securePwd);
                logincreds = Username + "\n" + password;
                ClearPassword();
                byte[] plaintextcreds = Encoding.UTF8.GetBytes(logincreds);
                byte[] entropy = new byte[20];
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(entropy);
                }
                byte[] encryptedcreds = ProtectedData.Protect(plaintextcreds, entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(logincredPath, encryptedcreds);
                File.WriteAllBytes(entropyPath, entropy);
                savedlogin = true;
                Automate = automate;
            }
        }
    }

    public void DeleteLoginCreds()
    {
        Console.WriteLine("Corrupted login credentials detected, removing them now.");
        string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        string logincredPath = Path.Combine(credPath, ".credentials/login.txt");
        string entropyPath = Path.Combine(credPath, ".credentials/entropy.txt");
        File.Delete(logincredPath);
        File.Delete(entropyPath);
    }

    public void ScheduleGET()
    {
        var driverService = PhantomJSDriverService.CreateDefaultService();
        driverService.HideCommandPromptWindow = true; // Disables verbose phantomjs output
        IWebDriver driver = new PhantomJSDriver(driverService);
        //IWebDriver driver = new FirefoxDriver(); // Debug with firefox.

        Console.WriteLine("Logging into Office 365.");
        driver.Navigate().GoToUrl("https://wegmans.sharepoint.com/resources/Pages/LaborPro.aspx");
        if (driver.Title.ToString() == "Sign in to Office 365")
        {
            IWebElement loginentry = driver.FindElement(By.XPath("//*[@id='cred_userid_inputtext']"));
            loginentry.SendKeys(Username);
            IWebElement rememberme = driver.FindElement(By.XPath("//*[@id='cred_keep_me_signed_in_checkbox']"));
            rememberme.Click();
        }

        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
        try { wait.Until((d) => { return (d.Title.ToString().Contains("Sign In") || d.Title.ToString().Contains("My Schedule")); }); } // Sometimes it skips the second login page.
        catch (WebDriverTimeoutException)
        {
            driver.Quit();
            throw new ScheduleGETException("Did not recieve an appropriate response from the Sharepoint server. The connection most likely timed out.");
        }
        Console.WriteLine("Logging into Sharepoint.");

        if (driver.Title.ToString() == "Sign In")
        {
            try { wait.Until((d) => { return (d.FindElement(By.XPath("//*[@id='passwordInput']"))); }); }
            catch (Exception)
            {
                driver.Quit();
                throw new ScheduleGETException("Password input box did not load correctly.");
            }
            IWebElement passwordentry = driver.FindElement(By.XPath("//*[@id='passwordInput']"));
            password = ConvertToUnsecureString(securePwd);
            passwordentry.SendKeys(password);
            ClearPassword();
            passwordentry.Submit();
        }

        try { wait.Until((d) => { return (d.Title.ToString().Contains("Sign In") || d.Title.ToString().Contains("My Schedule")); }); } // Checks to see if the password was incorrect.
        catch (WebDriverTimeoutException)
        {
            driver.Quit();
            throw new ScheduleGETException("Did not recieve an appropriate response from the Sharepoint server. The connection most likely timed out.");
        }
        if (driver.Title.ToString() == "Sign In")
        {

            IWebElement error = driver.FindElement(By.XPath("//*[@id='error']"));
            string errorString = error.Text.ToString();
            if (errorString.Contains("Incorrect user ID or password"))
            {
                while (driver.Title.ToString() == "Sign In")
                {
                    IWebElement usernameentry = driver.FindElement(By.XPath("//*[@id='userNameInput']"));
                    IWebElement passwordentry = driver.FindElement(By.XPath("//*[@id='passwordInput']"));
                    usernameentry.Clear();
                    passwordentry.Clear();
                    Console.WriteLine("You seem to have entered the wrong username or password.");
                    GetLoginCreds();
                    Console.WriteLine("Trying again...");
                    usernameentry.SendKeys(Username);
                    password = ConvertToUnsecureString(securePwd);
                    passwordentry.SendKeys(password);
                    ClearPassword();
                    passwordentry.Submit();
                }
                SaveLoginCreds();
            }
            else
            {
                Console.WriteLine("An unexpected error has occured with the webpage.");
                Console.WriteLine(errorString);
                driver.Quit();
                throw new ScheduleGETException("An unexpected error has occured with the webpage.");
            }
        }

        Console.WriteLine("Waiting for LaborPro...");
        int retries = 2;
        while (true) // Retry because this error can be solved by a simple page reload.
        {
            try { wait.Until((d) => { return (d.SwitchTo().Frame(0)); }); break; } // Waits for the inline frame to load.
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("LaborPro link's inline frame was not generated properly.");
                Console.WriteLine("Reloading the page...");
                driver.Navigate().Refresh();
                retries--;
                if (retries <= 0)
                {
                    driver.Quit();
                    throw new ScheduleGETException("LaborPro link's inline frame was not generated properly.");
                }

            }
        }

        string BaseWindow = driver.CurrentWindowHandle;
        try { wait.Until((d) => { return (d.FindElement(By.XPath("/html/body/a"))); }); } // Waits until javascript generates the SSO link.
        catch (Exception)
        {
            if (driver.Title.ToString().Contains("Sign In")) // We were redirected to the sign-in page once again, so let's fill it out again...
            {
                IWebElement usernameentry = driver.FindElement(By.XPath("//*[@id='userNameInput']"));
                IWebElement passwordentry = driver.FindElement(By.XPath("//*[@id='passwordInput']"));
                usernameentry.Clear();
                passwordentry.Clear();
                usernameentry.SendKeys(Username);
                password = ConvertToUnsecureString(securePwd);
                passwordentry.SendKeys(password);
                ClearPassword();
                passwordentry.Submit();
            }
            else
            {
                driver.Quit();
                throw new ScheduleGETException("LaborPro SSO Link was not generated properly.");
            }
        }

        IWebElement accessschedule = driver.FindElement(By.XPath("/html/body/a"));
        accessschedule.Click();
        string popupHandle = string.Empty;
        ReadOnlyCollection<string> windowHandles = driver.WindowHandles;

        foreach (string handle in windowHandles)
        {
            if (handle != driver.CurrentWindowHandle)
            {
                popupHandle = handle;
                break;
            }
        }
        driver.SwitchTo().Window(popupHandle);

        Console.WriteLine("Accessing LaborPro.");
        try { wait.Until((d) => { return (d.Title.ToString().Contains("Welcome")); }); }
        catch (WebDriverTimeoutException)
        {
            throw new ScheduleGETException("Did not properly switch to LabroPro Window.");
        }
        Schedules.Add(driver.PageSource.ToString());
        for (int i = 0; i < 2; i++) // Clicks "Next" and gets the schedules for the next two weeks.
        {
            driver.FindElement(By.XPath("//*[@id='pageBody']/form/table[2]/tbody/tr[2]/td/table/tbody/tr[2]/td/table/tbody/tr/td/div/table[1]/tbody/tr/td[1]/a[3]")).Click();
            Schedules.Add(driver.PageSource.ToString());
        }
        ClearPassword(); // We don't need the password anymore, so let's not keep it laying around.
        driver.Quit();
        Console.WriteLine("Got your Schedule.");
    }

    public void FindTable()
    {
        string endTable = "</table>";
        string startTable = "<table";
        int index = 0;
        
        // Parse the DOM, find our tables
        foreach (var week in Schedules)
        {
            List<string> m_tables = new List<string>();
            while ((index = week.IndexOf(startTable, index + 1, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                int tableContentEndIndex = week.IndexOf(endTable, index, StringComparison.OrdinalIgnoreCase);
                string tableContent = week.Substring(index, tableContentEndIndex - index + endTable.Length);

#if debug
                Console.WriteLine("Found a table DOM element!");
#endif

                m_tables.Add(tableContent);
            }

            // Identify the table that's actually relevant to us
            string keyword = "Schedule Times:";
            var oneWeWantList = m_tables.Where(s => s.Contains(keyword));

            if (oneWeWantList.Count() > 1)
            {
                throw new Exception("Found more than one 'right' table.");
            }

            var correcttable = oneWeWantList.FirstOrDefault();

            if (correcttable == null)
            {
                throw new Exception("The 'right' table does not exist.");
            }

            CorrectTable.Add(correcttable);
        }
    }

    public void ParseTable()
    {
        const string rowStart = "<tr";
        const string cellStart = "<td";
        const string rowEnd = "</tr>";
        const string cellEnd = "</td>";

        int index1 = 0; //todo: trace index going out of bounds.
        foreach (var table in CorrectTable)
        {
            while ((index1 = table.IndexOf(rowStart, index1 + 1, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                int rowContentEndIndex = table.IndexOf(rowEnd, index1);
                String rowContent = table.Substring(index1, rowContentEndIndex - index1 + rowEnd.Length);
                var row = new Row();

#if debug
                Console.WriteLine("Found a row within the table...");
#endif

                int index2 = 0;
                while ((index2 = rowContent.IndexOf(cellStart, index2 + 1, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    int cellContentEndIndex = rowContent.IndexOf(cellEnd, index2);
                    String cellContent = rowContent.Substring(index2, cellContentEndIndex - index2 + cellEnd.Length);

                    // We have to parse the cell html for the value
                    // we just want the value between ...>HERE<...
                    int startOpen = cellContent.IndexOf(">");
                    int endOpen = cellContent.IndexOf("<", cellStart.Length); // start past the <td> part
                    String actualValue = cellContent
                        .Substring(startOpen + 1, endOpen - startOpen - 1)
                        .Replace("\n", string.Empty)
                        .Replace("\r", string.Empty)
                        .Replace("&nbsp;", string.Empty)
                        .Trim();

                    if (actualValue.Equals("&nbsp;"))
                    {
                        actualValue = string.Empty; // Non breaking space is space...
                    }

#if debug
                    Console.WriteLine("Found a cell within the row with value '{0}'", actualValue);
#endif
                    row.cells.Add(new Cell { value = actualValue });
                }

                Rows.Add(row);
            }
        }
    }

    public void ParseRows()
    {
        for (int i = 0; i < 3; i++)
        {
            Row dateRow = Rows[1 + (i * 8)]; // First row is empty, then adds a week each iteration.
            Row scheduleHoursRow = Rows[2 + (i * 8)];
            Row activityRow = Rows[3 + (i * 8)];
            Row locationRow = Rows[4 + (i * 8)];
            Row scheduleTimesRow = Rows[5 + (i * 8)];
            Row commentsRow = Rows[6 + (i * 8)];

            int dayIndex = 1; // 0 is column names
            while (true)
            {
                var workDay = new WorkDay();
                String dateString = dateRow.cells[dayIndex].value;

                // Get the day of the week
                workDay.Day = workDay.enumDay(dateString);
                if (workDay.Day == WorkDay.DayEnum.INVALID)
                {
                    break;
                }

                // split the string "Tue 6/23"
                // and use the second part ['Tue', '6/23']
                String datePart = dateString.Split(' ')[1];
                workDay.Date = DateTime.Parse(datePart);

                workDay.Hours = float.Parse(scheduleHoursRow.cells[dayIndex].value.Replace(":", "."));
                workDay.Activity = activityRow.cells[dayIndex].value;
                workDay.Comments = commentsRow.cells[dayIndex].value;
                workDay.Location = workDay.ConvertLocation(locationRow.cells[dayIndex].value);


                String timeSpanPart = scheduleTimesRow.cells[dayIndex].value;
                String[] times = timeSpanPart.Split('-'); // split '2:00 AM-8:00 PM' on the '-'

                workDay.StartTime = times[0];
                workDay.EndTime = times.Length == 1 ? times[0] : times[1];

                if (workDay.Hours > 0)
                {
                    workDay.StartDateTime = DateTime.Parse(workDay.Date.ToShortDateString() + " " + workDay.StartTime);
                    workDay.EndDateTime = DateTime.Parse(workDay.Date.ToShortDateString() + " " + workDay.EndTime);
                }
                Schedule.Add(workDay);
                dayIndex++;
            }
        }
    }
    public void DisplayResults()
    {
        foreach (WorkDay day in Schedule)
        {
            if (day.Hours == 0)
            {
                //Console.WriteLine("You get " + day.Day.ToString() + " off!");
                //Console.WriteLine(string.Empty);
                continue;
            }

            if (day.Hours > 8)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("******** WARNING - ANOMALY DETECTED: ********");
            }

            Console.WriteLine(day.Date.ToShortDateString() + " at " + day.Location + Environment.NewLine + " from " +
                day.StartTime + " to " + day.EndTime + Environment.NewLine + " doing " + day.Activity
                + " (" + day.Comments + ") total of " + day.Hours + " hours");
            Console.WriteLine(string.Empty);
        }
    }

    public async System.Threading.Tasks.Task UploadResults()
    {
        var request = new BatchRequest(Service);
        foreach (WorkDay day in Schedule)
        {
            // Setup request for current events.
            EventsResource.ListRequest listrequest = Service.Events.List(Calendarid);
            listrequest.TimeMin = DateTime.Today.AddDays(-6);
            listrequest.TimeMax = DateTime.Today.AddDays(15);
            listrequest.ShowDeleted = false;
            listrequest.SingleEvents = true;
            listrequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // Check to see if work events are already in place on the schedule, if they are,
            // setup a batch request to delete them.
            // Not the best implementation, but takes care of duplicates without tracking
            // eventIds to update existing events.
            String workevent = "Wegmans: ";
            Events eventslist = listrequest.Execute();
            if (eventslist.Items != null && eventslist.Items.Count > 0)
            {
                foreach (Event eventItem in eventslist.Items)
                {
                    DateTime eventcontainer = (DateTime)eventItem.Start.DateTime; // Typecast to use ToShortDateString() method for comparison.
                    if (((eventcontainer.ToShortDateString()) == (day.Date.ToShortDateString())) && (eventItem.Summary.Contains(workevent)))
                    {
                        request.Queue<Event>(Service.Events.Delete(Calendarid, eventItem.Id),
                            (content, error, i, message) =>
                            {
                                if (error != null)
                                {
                                    throw new Exception(error.ToString());
                                }
                            });
                    }
                }
            }
            // Setup a batch request to upload the work events.
            request.Queue<Event>(Service.Events.Insert(
            new Event
            {
                Summary = workevent + day.Activity,
                Description = day.Comments,
                Location = day.Location,
                Start = new EventDateTime()
                {
                    DateTime = day.StartDateTime,
                    TimeZone = "America/New_York",
                },
                End = new EventDateTime()
                {
                    DateTime = day.EndDateTime,
                    TimeZone = "America/New_York",
                },
                Reminders = new Event.RemindersData()
                {
                    UseDefault = true,
                },
            }, Calendarid),
            (content, error, i, message) =>
            {
                if (error != null)
                {
                    throw new Exception(error.ToString());
                }
            });
        }
        // Execute batch request.
        await request.ExecuteAsync();
    }

    public void AutomateRun()
    {
        using (TaskService ts = new TaskService())
        {
            TaskDefinition td = ts.NewTask();
            td.RegistrationInfo.Description = "Runs daily to update your schedule with possible changes.";
            // Sets trigger for every day at the current time. This assumes the user sets up automation at a time their computer would normally be running.
            td.Triggers.Add(new DailyTrigger { DaysInterval = 1 });
            td.Actions.Add(new ExecAction(Path.GetFullPath(Assembly.GetExecutingAssembly().Location)));
            td.Settings.Hidden = true;
            td.Settings.StartWhenAvailable = true;
            td.Settings.RunOnlyIfNetworkAvailable = true;
            ts.RootFolder.RegisterTaskDefinition(@"WSchedulerTask", td);
        }
    }

    public void RemoveAutomationTask()
    {
        using (TaskService ts = new TaskService())
        {
            try
            {
                ts.RootFolder.DeleteTask("WSchedulerTask");
            }
            catch (Exception)
            {
                // Do nothing, the task just doesn't exist, so we don't need to delete it.
            }
        }
    }

    public void ClearSchedules()
    {
        Schedules = new List<string>();
        CorrectTable = new List<string>();
        Rows = new List<Row>();
        Schedule = new List<WorkDay>();
    }

    public void ClearPassword()
    {
        password = "";
    }
}
