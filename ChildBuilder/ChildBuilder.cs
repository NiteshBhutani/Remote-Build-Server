///////////////////////////////////////////////////////////////////////
// ChildBuilder.cs - Child Builder to build project files                //
//                  for Build Server Project                        //
// Version 1.0														//
//  Language:     C#, VS 2017                                       //
// Application: Core Build Server - CSE 681 Project 3	            //
// Platform:    Asus R558U, Win 10 Student Edition, Visual studio 17//
// Author:	Nitesh Bhutani, CSE681, nibhutan@syr.edu				//
//////////////////////////////////////////////////////////////////////
/*
Package Operations:
==================
1) ChildBuilder.cs - It recieve build request from Mother builder, it builds it, send build logs to repo, dll to test
                    harness and send ready messages back to mother builder.

   BasicLogger Class- It is the Logger class used during build to log build events(error, warning, success/fail status) in a log file.
   
Public Interface :
================

public ChildBuilder() - Constructor to construct comm channel and start child recieve thread
void processMessage(Message msg) - Function to process messages from mother builder
public bool sendFile(string fileSpec, string destination) - Helper Function to send file to location 
public bool buildProject(string path, string project) - Function to build project given path to .csproj file
public void readFile(string filespec) - Function to readfile and display on console with any exception messages


Build Process:
==============
- Dependency- Enviornment.cs, IMPCommService.cs, MPCommService.cs

Build commands (either one)
- devenv BuildServer.sln /rebuild debug
- devenv ChildBuilder.csproj /rebuild debug

Maintainence History :
====================
- Version 1.1- 1st Dec 2017 Added functionality to integrate Test Harness with child builder- function to create test  req,
                            send test request to TH, function to send files to TH
- Version 1.0 : 31th October 2017

First Release
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using System.Threading;
using System.Xml.Linq;
using System.IO;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities;

namespace BuildServer
{

    public class ChildBuilder
    {
        // ----------<Constructor to construct comm channel and start Child Builder recieve thread>----------
        public ChildBuilder(string address, int port)
        {
            childBuilderAddress = address;
            childBuilderPort = port;
            childBuilderEndpoint = childBuilderAddress + ":" + childBuilderPort.ToString() + "/IMessagePassingComm";
            comm = new Comm(childBuilderAddress, childBuilderPort, MotherBuilderEnvironment.root);
            if (!Directory.Exists(TestHarnessEnvironment.root))
                Directory.CreateDirectory(TestHarnessEnvironment.root);
            if (!Directory.Exists(MotherBuilderEnvironment.root))
                Directory.CreateDirectory(MotherBuilderEnvironment.root);
            if (!Directory.Exists(RepoEnvironment.root))
                Directory.CreateDirectory(RepoEnvironment.root);
            buildFiles = new Dictionary<string, List<string>>();
            dllFiles = new Dictionary<string, string>();
            requestXML = new XDocument();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
            
            sendReadyMessageToMotherBuilder(); //send the ready message to mother builder
        }
        
        //----------------<Recieve thread for child builder>----------------
        void rcvThreadProc()
        {
            Console.Write("\n  starting Child Builder receive thread");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                Console.Write("\n Message of Child Builder receive thread");
                msg.show();
                if (msg.command == null || msg.command == "SendingBuildFiles")
                    continue;
                if (msg.command == "quit") // to close the child process from mother builder
                {
                    Console.Write("\n  {0} thread quitting", msg.to);
                    break;
                }
                processMessage(msg);
            }
        } 

        //-----------------<Function to send ready message to builder>--------------
        void sendReadyMessageToMotherBuilder() {
            CommMessage newMsg = new CommMessage(CommMessage.MessageType.request);
            newMsg.from = childBuilderEndpoint;
            newMsg.to = MotherBuilderEnvironment.endPoint;
            newMsg.author = "Nitesh Bhutani";
            newMsg.command = "Ready";
            newMsg.arguments.Add("");
            comm.postMessage(newMsg);
        }

        // ----------<Function to process messages from client and repo>----------
        void processMessage(CommMessage msg)
        {
            if (msg.from == MotherBuilderEnvironment.endPoint && msg.command == "BuildRequest")
            {
                processBuildRequest(msg.arguments);
                //send ready message to mother builder
                sendReadyMessageToMotherBuilder();
            }
            else if(msg.command == "GetTestFiles") transferTestFiles(msg);
        }

        //-------------<Function to send Test files to Test Harness> ---------
        private void transferTestFiles(CommMessage msg)
        {
            CommMessage newMsg = new CommMessage(CommMessage.MessageType.connect);
            newMsg.from = childBuilderEndpoint;
            newMsg.to = TestHarnessEnvironment.endPoint;
            comm.postMessage(newMsg); 
            Thread.Sleep(2000);
            foreach (var file in msg.arguments)
            {
                comm.postFile(file);
            }
        }


        // ----------<Function to processbuild request>----------
        private void processBuildRequest(List<string> requests)
        {   foreach (var req in requests) { 
                while (true)
                {   if (System.IO.File.Exists(Path.Combine(MotherBuilderEnvironment.root, req)))
                        break;
                }
                buildFiles.Clear();
                dllFiles.Clear();
                getBuildFilesNames(Path.Combine(MotherBuilderEnvironment.root, req));
                getBuildFiles();//transfer build files from repo
                while (true)
                {   if (checkBuildFiles())
                        break;
                }
                foreach (var item in buildFiles)//for each project attempt to build dll in their own folder
                {   bool status = buildProject(item.Key, item.Value[0]);
                    if (status)
                    {   string dllName = item.Value[0].Split('.')[0] + ".dll";
                        //add library to dllFiles dictionary
                        dllFiles.Add(item.Key, Path.Combine("Output", dllName));
                    }
                    Console.WriteLine("\n Sending Build logs to Repo at location {0} ...... ", Path.Combine(RepoEnvironment.root,item.Key, "buildLog.txt"));
                    sendBuildLogstoRepo(Path.Combine(item.Key, "buildLog.txt")); //pass log file to repo
                    sendBuildStatusToClient(item.Key,status); // Build Status to client
                }
                createTestRequest();//create test request
                sendTestRequestToTH();
        }  
    }

        //-------------< Helper Function to send Test Request XML and Test Request command to Test Harness > ---------
        private void sendTestRequestToTH()
        {
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect); //send this request xml file created to repo folder for storage done
            connectMsg.from = childBuilderEndpoint;
            connectMsg.to = TestHarnessEnvironment.endPoint;
            comm.postMessage(connectMsg);
            Thread.Sleep(2000);
            comm.postFile(testRequestXMLName + "_testrequest.xml");
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = childBuilderEndpoint;
            msg1.to = TestHarnessEnvironment.endPoint;
            msg1.command = "TestRequest";
            msg1.arguments.Add(testRequestXMLName + "_testrequest.xml");
            Console.WriteLine("********************** Sending Test Request to Test Harness :- ****************************");
            readFile(System.IO.Path.Combine(MotherBuilderEnvironment.root, testRequestXMLName + "_testrequest.xml"));
            comm.postMessage(msg1);
        }

        //-------------< Helper Function to readfile and display on console with any exception messages> ---------
        public void readFile(string filespec)
        {
            string line;
            try
            {
                StreamReader sr = new StreamReader(filespec);
                line = sr.ReadLine();
                while (line != null)
                {
                    Console.WriteLine(line);
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client : Exception while reading file. Exception Message : {0}", ex.Message);
            }


        }
        //-------------< Helper Function to send build status for each project to client > ---------
        private void sendBuildStatusToClient(string projectName, bool status)
        {
            string msgString;
            if (status)
                msgString = "Build Status for project " + projectName + " is SUCCESS. ";
            else
                msgString = "Build Status for project " + projectName + " is FAIL. ";
            CommMessage msg = new CommMessage(CommMessage.MessageType.reply);
            msg.from = childBuilderEndpoint;
            msg.to = ClientEnvironment.endPoint;
            msg.author = "Nitesh Bhutani";
            msg.command = "StatusUpdate";
            msg.arguments.Add(msgString);
            comm.postMessage(msg);

        }

        //-------------< Helper Function to send build logs for each project to repository > ---------
        private void sendBuildLogstoRepo(String filespec)
        {
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect);
            connectMsg.from = childBuilderEndpoint;
            connectMsg.to = RepoEnvironment.endPoint;
            comm.postMessage(connectMsg);
            Thread.Sleep(2000);
            comm.postFile(filespec);
            
        }

        //-------------< Helper Function to check if files present in build request are present in storage> ---------
        private bool checkBuildFiles()
        {
            foreach (var project in buildFiles)
            {
                foreach(var file in project.Value)
                {
                    string filePath = System.IO.Path.Combine(MotherBuilderEnvironment.root , project.Key , file);
                    if (!System.IO.File.Exists(System.IO.Path.GetFullPath(filePath)))
                        return false;
                }
                
            }
            return true;
        }
        // ----------<Function to get build files from repo>----------
        private void getBuildFiles()
        {
            CommMessage newMsg = new CommMessage(CommMessage.MessageType.request);
            newMsg.from = childBuilderEndpoint;
            newMsg.to = RepoEnvironment.endPoint;
            newMsg.author = "Nitesh Bhutani";
            newMsg.command = "GetFiles";

            foreach (var item in buildFiles)
            {  foreach (var file in item.Value)
                {
                    newMsg.arguments.Add(Path.Combine(item.Key,file));
                }
            }
            comm.postMessage(newMsg);
            Console.WriteLine("\n Child Builder getting files from Repo. Message :-");
            newMsg.show();
        }

        // ----------<Function to create XML Test Request>----------
        private void createTestRequest()
        {
            testRequestXMLName = requestXML.Descendants("Name").First().Value;
            XDocument testRequestXML = new XDocument();
            XElement testRequestElem = new XElement("TestRequest");
            testRequestXML.Add(testRequestElem);
            XElement authorElem = new XElement("Author");
            authorElem.Add(requestXML.Descendants("Author").First().Value);
            testRequestElem.Add(authorElem);
            XElement NameElem = new XElement("Name");
            NameElem.Add(requestXML.Descendants("Name").First().Value);
            testRequestElem.Add(NameElem);
            XElement buildToolElem = new XElement("BuildTool");
            buildToolElem.Add(requestXML.Descendants("BuildTool").First().Value);
            testRequestElem.Add(buildToolElem);
            foreach (var item in dllFiles)
            {
                XElement projectElement = new XElement("Project");
                XElement projectNameElement = new XElement("ProjectName");
                projectNameElement.Add(item.Key);
                projectElement.Add(projectNameElement);
                XElement libraryElement = new XElement("Library");
                libraryElement.Add(item.Value);
                projectElement.Add(libraryElement);
                testRequestElem.Add(projectElement);
            }
            testRequestXML.Save(Path.Combine(MotherBuilderEnvironment.root, testRequestXMLName + "_testrequest.xml"));
        }

        // ----------<Helper Function to send file to location >----------
        public bool sendFile(string fileSpec, string destination)
        {
            try
            {
                string fileName = Path.GetFileName(fileSpec);
                string destSpec = Path.Combine(destination, fileName);
                File.Copy(fileSpec, destSpec, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--", ex.Message);
                return false;
            }
        }

        // ----------<Function to build project given path to .csproj file>----------
        public bool buildProject(string path, string project)
        {
            string logFileLocation = Path.Combine(MotherBuilderEnvironment.root, path + "\\buildLog.txt");
            BasicLogger logger = new BasicLogger(logFileLocation);
            string projectSpec = Path.GetFullPath(Path.Combine(MotherBuilderEnvironment.root, path, project));
            Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
            BuildRequestData BuildRequest = new BuildRequestData(projectSpec, GlobalProperty, null, new string[] { "Rebuild" }, null);
            BuildParameters bp = new BuildParameters();
            bp.Loggers = new List<ILogger> { logger };

            BuildResult buildResult = BuildManager.DefaultBuildManager.Build(bp, BuildRequest);

            if (buildResult.OverallResult.ToString() == "Failure")
                return false;
            return true;
        }

        // ----------<Helper function to get the list of build files names>----------
        private void getBuildFilesNames(string fileSpec)
        {
            fileSpec = System.IO.Path.GetFullPath(fileSpec);
            if (!loadXml(fileSpec))
            {
                Console.WriteLine("\n Builder Logs : Not able to load XML into XDocument");
                return;
            }
            IEnumerable<XElement> projectSubTree = requestXML.Descendants("Project");

            foreach (XElement project in projectSubTree)
            {
                //source files
                string projectName = project.Descendants("ProjectName").First().Value;
                //config file .csproj
                string configFile = project.Descendants("Config").First().Value;
                List<string> files = new List<string>();
                files.Add(configFile);
                IEnumerable<XElement> parseElems = project.Descendants("File");
                foreach (XElement elem in parseElems)
                {
                    files.Add(elem.Value);
                }

                buildFiles.Add(projectName, files);
            }

        }

        /*----< load Build Request from XML file and store it into XDocument >-----------------------*/
        private bool loadXml(string path)
        {
            try
            {
                requestXML = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        Comm comm { get; set; } = null;//comm channel for repo
        Thread rcvThread = null;
        public string childBuilderAddress { get; set; } = null;
        public int childBuilderPort { get; set; }
        public string childBuilderEndpoint { get; set; } = null;

        public XDocument requestXML { get; set; }
        public string testRequestXMLName { get; set; }
        public Dictionary<string, List<string>> buildFiles { get; set; }
        public Dictionary<string, string> dllFiles { get; set; }

        //Test Stub
        static void Main(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.Write("\n  please enter address and port value on command line");
                return;
            }
            TestUtilities.title("Child Builder Started on port "+args[1], '=');
            ChildBuilder ch = new ChildBuilder(args[0], Int32.Parse(args[1]));
        }

    }

#if (TEST_CHILDBUILDER)
    class Program
    {   
        static void Main(string[] args)
        {
            TestUtilities.title("Mother Builder Started ",'=');
            ChildBuilder cl = new ChildBuilder("http://localhost",9090);
            cl.buildProject("..\\..\\..\\Repo_Folder\\TestDemo", "CSTestDemo.csproj");
            
        }
    }
#endif
    public class BasicLogger : Logger
    {
        private StreamWriter streamWriter;
        private int indent;
        private int warnings = 0;
        private int errors = 0;
        public string logPath { get; set; }

        //constructor - assing the path of log file
        public BasicLogger(string filePath)
        {
            logPath = filePath;
        }
        //Called at the start of the build - It initailizes different function to handle events which happen during build
        public override void Initialize(IEventSource eventSource)
        {
            try
            {
                this.streamWriter = new StreamWriter(logPath);
            }
            catch (Exception ex)
            {
                Console.Write("\n Builder(Basic Logger Initialize)--{0}--\n", ex.Message);
            }

            eventSource.ProjectStarted += new ProjectStartedEventHandler(ProjectStarted);
            eventSource.WarningRaised += new BuildWarningEventHandler(WarningRaised);
            eventSource.ErrorRaised += new BuildErrorEventHandler(ErrorRaised);
            eventSource.ProjectFinished += new ProjectFinishedEventHandler(ProjectFinished);
            eventSource.BuildStarted += new BuildStartedEventHandler(BuildStarted);
            eventSource.BuildFinished += new BuildFinishedEventHandler(BuildFinished);


        }

        // event to handle build start 
        void BuildStarted(Object sender, BuildStartedEventArgs e)
        {
            Console.WriteLine("\n /***********************Build Started ***************************/");
            WriteLine("/////////////", e);

        }
        // event to handle build finish
        void BuildFinished(Object sender, BuildFinishedEventArgs e)
        {
            Console.WriteLine("\n /***********************Build Finished ***************************/");
            Console.WriteLine(e.Message);
            Console.WriteLine(String.Format("    {0} Warning(s)", warnings));
            Console.WriteLine(String.Format("    {0} Error(s)", errors));

            WriteLine("/////////////", e);
        }

        // Called at the end of the build
        public override void Shutdown()
        {
            streamWriter.Close();
        }
        // called when error are raised
        void ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            ++errors;
            string line = String.Format(" \n \n \n ----- ERROR {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
            WriteLine(line, e);
            Console.WriteLine(line + e);
        }
        // called when warnigs are raised
        void WarningRaised(object sender, BuildWarningEventArgs e)
        {
            ++warnings;
            string line = String.Format(" ----- WARNING {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
            WriteLine(line, e);
            Console.WriteLine(line + e);
        }
        // called when individual project build is started
        void ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            WriteLine(String.Empty, e);
            indent++;
        }
        // called when individual project build is finished
        void ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            indent--;
            WriteLine(String.Empty, e);
            Console.WriteLine("\n Project {0} Build Finished with Status : {1}", e.ProjectFile, e.Succeeded);

        }
        // Utility function
        private void WriteLine(string line, BuildEventArgs e)
        {
            streamWriter.WriteLine(line + e.Message);
        }


    }

}