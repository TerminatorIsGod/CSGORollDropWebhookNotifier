using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;


namespace WebBrowser
{
    public partial class Form1 : Form
    {
        public static Form1 _instance;

        string taskName = "csgoRollNotifier";

        public bool disableAutoQuit = false;

        bool alreadyShowingScreen = false;

        bool quittingApplication = false;

        bool waitingForTime = false;
        string timeLeftStr = "";

        private Timer timer;
        private int secondsElapsed;

        public float percentThreshold = 0.98f;
        public string webhookURL = "";
        public int reloadPageInterval = 600;

        bool waitingForJavaScriptToComplete = false;

        string filePathFolder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CSGORollNotifier");

        //private bool firstTimeLaunched = false;

        Dictionary<string, WebBrowserCommand> webBrowserCommands = new Dictionary<string, WebBrowserCommand>();

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_NOACTIVATE (0x08000000) ensures the window does not activate when created
                cp.ExStyle |= 0x08000000;
                return cp;
            }
        }

        

        public Form1()
        {
            InitializeComponent();

            _instance = this;

            this.FormClosing += Form1_FormClosing;

            initalizeAsync();

            taskName = ConfigurationManager.AppSettings["taskName"];

            //Register Commands
            insertWebBrowserCommand("autoQuit", new cancelQuitCommand());
            insertWebBrowserCommand("goTo", new goToCommand());

            AutoCompleteStringCollection autoCompleteCollection = new AutoCompleteStringCollection();
            autoCompleteCollection.AddRange(webBrowserCommands.Keys.ToArray());
            textBox1.AutoCompleteCustomSource = autoCompleteCollection;

            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string configFilePath = Path.Combine(exeDirectory, "config.egario");

            if (File.Exists(configFilePath))
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(configFilePath);

                if (lines.Length >= 2)
                {
                    // Store the first and second lines in separate variables
                    string intervalLine = lines[0];
                    string urlLine = lines[1];
                    string reloline = lines[2];

                    // Extract the values after the colons (:) if needed
                    string intervalValue = intervalLine.Split(':')[1].Trim();
                    string urlValue = urlLine.Substring(urlLine.IndexOf(":") + 1).Trim();
                    string reloadInter = reloline.Split(':')[1].Trim();

                    // Convert the interval value to a float or double if needed
                    float interval = float.Parse(intervalValue);

                    percentThreshold = interval;
                    webhookURL = urlValue;
                    reloadPageInterval = int.Parse(reloadInter);

                    // Output the values for demonstration purposes
                    printToConsole($"Threshold: {interval}");
                    printToConsole($"Webhook: {urlValue}");
                    printToConsole($"Reload Interval: {reloadPageInterval}");
                }
                else
                {
                    printToConsole("The config file does not have enough lines.");
                }
            }
            else
            {
                printToConsole("The config file does not exist.");
            }
        }

        private async void initalizeAsync()
        {
            await webView21.EnsureCoreWebView2Async(null);

            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;

            timer.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!quittingApplication)
            {
                // Ask the user if they really want to close the form
                DialogResult result = MessageBox.Show("Are you sure you want to quit?", "Quit?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                // If the user clicks No, cancel the closing event
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                } else
                {
                    timer.Stop();
                }
            }
        }

        void insertWebBrowserCommand(string command, WebBrowserCommand commandObject)
        {
            webBrowserCommands.Add(command.ToLower(), commandObject);
        }

        WebBrowserCommand getWebBrowserCommand(string command)
        {
            WebBrowserCommand commandObject = null;
            webBrowserCommands.TryGetValue(command.ToLower(), out commandObject);
            return commandObject;
        }

        public void printToConsole(String text)
        {
            richTextBox1.AppendText("\r\n" + text);
            richTextBox1.ScrollToCaret();
        }

        public void setURLTextbox(String url)
        {
            richTextBox2.Text = "URL: " + url;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            secondsElapsed++;
            if(secondsElapsed % 5 == 0)
            {
                printToConsole($"Seconds elapsed: {secondsElapsed}");
            }
            if(secondsElapsed >= 120)
            {
                webView21.Reload();
                secondsElapsed = 0;
            }
        }

        private async void webView21_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            setURLTextbox(webView21.Source.ToString());
            printToConsole(webView21.Source.ToString());

            executeJavaScript();
        }

        private async void executeJavaScript()
        {
            string script = $"var triggeredThirtyMinDropMessage = false;\r\nvar triggeredSendMessageTeamCT = false;\r\nvar triggeredSendMessageTeamT = false;\r\nvar triggeredSendMessageTeamChicken = false;\r\nvar triggeredSendMessageTeamUNKNOWN = false;\r\nvar triggeredSendMessageGolden = false;\r\n\r\nvar webhookURL = \"{webhookURL}\";\r\nvar percentThreshold = {percentThreshold};" + "\r\n\r\nasync function delay(time) {\r\n    return new Promise(resolve => setTimeout(resolve, time));\r\n}\r\n\r\nfunction sendMessage(mess) {\r\n    var request = new XMLHttpRequest();\r\n    request.open(\"POST\", webhookURL);\r\n\r\n    request.setRequestHeader('Content-type', 'application/json');\r\n\r\n    var params = {\r\n      username: \"CSGORoll Drop Notifier\",\r\n      avatar_url: \"\",\r\n      content: mess\r\n    }\r\n\r\n    request.send(JSON.stringify(params));\r\n}\r\n\r\n\r\nfunction checkIfRegularThiryMinDropReady(){\r\n    // Get the current time\r\n    var currentTime = new Date();\r\n\r\n    // Get the current minutes\r\n    var minutes = currentTime.getMinutes();\r\n\r\n    // Check if the current time is an interval of 30 minutes (0 or 30 minutes)\r\n    if (minutes === 59 || minutes === 29) {\r\n        console.log(\"The time is an interval of 30 minutes.\");\r\n        if(!triggeredThirtyMinDropMessage){\r\n            console.log(\"Sending notification!\");\r\n            sendMessage(\"Regular 30 minute drop is about to begin!\");\r\n            triggeredThirtyMinDropMessage = true;\r\n        } else {\r\n            console.log(\"Already sent notification\");\r\n        }\r\n    } else {\r\n        console.log(\"The time is not an interval of 30 minutes.\");\r\n    }\r\n}\r\n\r\nfunction checkToSeeIfTeamReady(){\r\n    var percentageElements = document.querySelectorAll('span.text-warning.fs-12.fw-700.lh-12');\r\n\r\n    if(!percentageElements){\r\n        return;\r\n    }\r\n\r\n    // Extract the text content from each span element and convert it to a number\r\n    var percentages = Array.from(percentageElements).map(span => parseFloat(span.textContent.trim().replace('%', '')));\r\n    \r\n    console.log(percentages);\r\n\r\n    for(var i = 0; i < percentages.length; i++){\r\n        //1 = CT\r\n        //2 = T\r\n        //3 = Chicken\r\n\r\n        if(percentages[i] > percentThreshold * 100){\r\n            console.log(\"Team has reached over \" + (percentThreshold * 100) + \"%\");\r\n            if(i == 0){\r\n\r\n                if(!triggeredSendMessageTeamCT){\r\n                    console.log(\"Sending notification!\");\r\n                    sendMessage(\"Counter-terrorist team 2x booster is about to begin! - Above \" + (percentThreshold * 100) + \"% - (\" + percentages[i] + \"%)\");\r\n                    triggeredSendMessageTeamCT = true;\r\n                } else {\r\n                    console.log(\"Already sent notification\");\r\n                }\r\n\r\n            } else if (i == 1){\r\n\r\n                if(!triggeredSendMessageTeamT){\r\n                    console.log(\"Sending notification!\");\r\n                    sendMessage(\"Terrorist team 2x booster is about to begin! - Above \" + (percentThreshold * 100) + \"% - (\" + percentages[i] + \"%)\");\r\n                    triggeredSendMessageTeamT = true;\r\n                } else {\r\n                    console.log(\"Already sent notification\");\r\n                }\r\n\r\n            } else if (i == 2){\r\n\r\n                if(!triggeredSendMessageTeamChicken){\r\n                    console.log(\"Sending notification!\");\r\n                    sendMessage(\"Chicken team 2x booster is about to begin! - Above \" + (percentThreshold * 100) + \"% - (\" + percentages[i] + \"%)\");\r\n                    triggeredSendMessageTeamChicken = true;\r\n                } else {\r\n                    console.log(\"Already sent notification\");\r\n                }\r\n\r\n            } else {\r\n\r\n                if(!triggeredSendMessageTeamUNKNOWN){\r\n                    console.log(\"Sending notification!\");\r\n                    sendMessage(\"UNKNOWN team 2x booster is about to begin! - Above \" + (percentThreshold * 100) + \"% - (\" + percentages[i] + \"%)\");\r\n                    triggeredSendMessageTeamUNKNOWN = true;\r\n                } else {\r\n                    console.log(\"Already sent notification\");\r\n                }\r\n\r\n            }\r\n            \r\n        }\r\n    }\r\n}\r\n\r\nfunction checkToSeeIfGoldenReady(){\r\n    var divElement = document.querySelector('div[data-test=\"trigger-progressPercentage\"]');\r\n\r\n    if(!divElement){\r\n        return;\r\n    }\r\n\r\n    // Get the text content inside the div and remove any extra whitespace\r\n    var textContent = divElement.textContent.trim();\r\n    \r\n    // Remove the percentage sign and convert the string to a number\r\n    var percentageValue = parseFloat(textContent.replace('%', ''));\r\n    \r\n    // Convert the percentage value to a decimal\r\n    var decimalValue = percentageValue / 100;\r\n\r\n    if(decimalValue >= 0.99){\r\n        if(!triggeredSendMessageGolden){\r\n            sendMessage(\"Golden drop is about to begin! - Above \" + (percentThreshold * 100) + \"% - (\" + percentageValue + \"%)\");\r\n            triggeredSendMessageGolden = true;\r\n        }\r\n        \r\n    }\r\n    \r\n    console.log(decimalValue);\r\n}\r\n\r\nasync function mainLoop(){\r\n    while(true){\r\n        checkIfRegularThiryMinDropReady();\r\n        checkToSeeIfGoldenReady();\r\n        checkToSeeIfTeamReady();\r\n\r\n        await delay(3000);\r\n    }\r\n    \r\n}\r\n\r\nmainLoop();";
            await webView21.ExecuteScriptAsync(script);

            printToConsole("Running code...");

            secondsElapsed = 0;
            while (true)
            {
                await DelayAsync(100);
                if (secondsElapsed > reloadPageInterval)
                {
                    secondsElapsed = 0;
                    webView21.Reload();
                    break;
                }
            }
        }

        private void quitWebBrowser()
        {
            if (!disableAutoQuit)
            {
                quittingApplication = true;
                Application.Exit();
            }
        }

        static async System.Threading.Tasks.Task DelayAsyncSec(double seconds)
        {
            await DelayAsync((int)(seconds * 1000));
        }

        static async System.Threading.Tasks.Task DelayAsync(int milliseconds)
        {
            await System.Threading.Tasks.Task.Delay(milliseconds);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (char)Keys.Return)
            {
                if (textBox1.Text.Length > 0)
                {
                    List<string> commandParts = textBox1.Text.Split(' ').ToList();
                    string command = commandParts.First();
                    commandParts.Remove(command);
                    string[] args = commandParts.ToArray();

                    textBox1.Text = "";

                    getWebBrowserCommand(command).executeCommand(command, args);
                }
            }
        }

        interface WebBrowserCommand
        {
            void executeCommand(string command, string[] args);
        }

        class cancelQuitCommand : WebBrowserCommand
        {
            public void executeCommand(string command, string[] args)
            {
                if(args.Length > 0)
                {
                    switch (args[0].ToLower())
                    {
                        case "true":
                            Form1._instance.disableAutoQuit = true;
                            return;
                        case "false":
                            Form1._instance.disableAutoQuit = false;
                            return;
                        default:
                            Form1._instance.disableAutoQuit = !Form1._instance.disableAutoQuit;
                            return;
                    }
                    //Form1._instance.disableAutoQuit = args[0];
                }

                Form1._instance.disableAutoQuit = !Form1._instance.disableAutoQuit;

                Form1._instance.printToConsole("Cancel Auto Quit: " + Form1._instance.disableAutoQuit);
            }
        }

        class goToCommand : WebBrowserCommand
        {
            public void executeCommand(string command, string[] args)
            {
                if(args.Length < 1 )
                {
                    //Didn't provide a URL
                    Form1._instance.printToConsole("Invalid command arguments! Usage: goto <url>");
                } else
                {
                    Uri result = null;
                    if(Uri.TryCreate(args[0], UriKind.Absolute, out result))
                    {
                        Form1._instance.webView21.Source = result;
                    }
                }
            }
        }

        private void webView21_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}