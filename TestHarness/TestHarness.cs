///////////////////////////////////////////////////////////////////////
// TestHarness.cs - Mock- TestHarness to display fucntionality       //
//                  for Build Server Project                        //
// Version 1.0														//
//  Language:     C#, VS 2017                                       //
// Application: Remote Build Server - CSE 681 Project 4	            //
// Platform:    Asus R558U, Win 10 Student Edition, Visual studio 17//
// Author:	Nitesh Bhutani, CSE681, nibhutan@syr.edu				//
//////////////////////////////////////////////////////////////////////

/*
Package Operations:
==================
1) TestHarness.cs - It provides the Mock-Test Harness with enough functionality to represent functionality of Build Server. 
             It recives command from Builder to load and execute each library and send back test logs to repo and report pass/fail status or any exception to console.
             It can recieved command from client and builder only.

Public Interface :
================

public TestHarness() - Constructor to construct comm channel and start child recieve thread
public void processMessage(Message msg) - Function to process messages from other parts of Fedration Server (client, ChildBuilder and Repo)
public bool LoadAndTest(string Path)  - Function to load library and execute each library    
public void loadXml() - load Build Request from XML file and store it into XDocument

Build Process:
==============
- Dependency- Enviornment.cs, IMPCommService.cs, MPCommService.cs

Build commands (either one)
- devenv BuildServer.sln /rebuild debug
- devenv TestHarness.csproj /rebuild debug

Maintainence History :
====================
- Version 1.1 : 4th December - 2017 - Added Comm Channel and change the message passing using comm, Removed inheritance from CommBase
- Version 1.0 : 6th October 2017
First Release
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWTools;
using System.Xml.Linq;
using System.IO;
using MessagePassingComm;
using System.Threading;
using System.Reflection;

namespace BuildServer
{
    public class TestHarness
    {
        //-------------<Constructor to construct Communication channel, initialize env and start recieve Test harness thread> ---------
        public TestHarness()
        {
            comm = new Comm(TestHarnessEnvironment.address, TestHarnessEnvironment.port, TestHarnessEnvironment.root);

            if (!Directory.Exists(TestHarnessEnvironment.root))
                Directory.CreateDirectory(TestHarnessEnvironment.root);
            if (!Directory.Exists(MotherBuilderEnvironment.root))
                Directory.CreateDirectory(MotherBuilderEnvironment.root);
            if (!Directory.Exists(RepoEnvironment.root))
                Directory.CreateDirectory(RepoEnvironment.root);
            dllFiles = new  List<string>();
            testRequestXML = new XDocument();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }

        //-------------<Thread to process incoming WCF CommMessage  in reciver queue for processing > ---------
        void rcvThreadProc()
        {
            Console.Write("\n  Starting Test Harness receive thread");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                Console.Write("\n Message of Test Harness receive thread");
                msg.show();
                if (msg.command == null)
                    continue;
                if (msg.command == "quit")
                    break;
                processMessage(msg);
            }
        }

        //----------<Function to process messages from client, builder>---------------
        public void processMessage(CommMessage msg)
        {
            // will recieve message from client(for status) and childbuilder/mother builder only not from test harness
            if (msg.command == "TestRequest") {
                processTestRequest(msg.arguments, msg.from);
            }
            else
            {   Console.Write("\n TestHarness : Message from outside Fedration Server with contents :- \n");
                Console.Write(msg.ToString());
                Console.Write("\n\n");
            }
        }

        //----------<function to process test request recieved by builder>---------------
        private void processTestRequest(List<string> testRequests, string childBuilderEndPoint)
        {
            foreach(var testReq in testRequests)
            {
                while (true)
                {
                    if (System.IO.File.Exists(Path.Combine(TestHarnessEnvironment.root, testReq)))
                        break;
                }
                getDllFilesNames(Path.Combine(TestHarnessEnvironment.root, testReq));
                getDllFiles(childBuilderEndPoint);//transfer dll files from builder
                Thread.Sleep(4000);
                while (true)
                {
                    if (checkTestFiles())
                        break;
                }
                //for each project attempt to load and test dll in their own folder
                foreach (var dll in dllFiles)
                {   logBuilder = new StringWriter();
                    bool result = LoadAndTest(Path.Combine(TestHarnessEnvironment.root, dll));
                    Console.WriteLine("\n\n  Log:\n{0}", log);
                    logBuilder.Close();
                    File.WriteAllText(Path.Combine(TestHarnessEnvironment.root, dll.Split('\\')[0],"testLog.txt"),log);
                    Console.WriteLine("\n Sending Test logs to Repo at location {0} ...... ", Path.Combine(RepoEnvironment.root, dll.Split('\\')[0], "testLog.txt"));
                    sendTestLogstoRepo(Path.Combine(dll.Split('\\')[0], "testLog.txt")); //pass log file to repo
                    sendTestStatusToClient(dll.Split('\\')[0], result); // Build Status to client
                }
                
            }
        }

        //-------------< Helper Function to send Test logs for each project to repository > ---------
        private void sendTestLogstoRepo(String filespec)
        {
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect);
            connectMsg.from = TestHarnessEnvironment.endPoint;
            connectMsg.to = RepoEnvironment.endPoint;
            comm.postMessage(connectMsg);
            Thread.Sleep(2000);
            comm.postFile(filespec);

        }

        //-------------< Helper Function to send Test status for each project to client > ---------
        private void sendTestStatusToClient(string projectName, bool status)
        {
            string msgString;
            if (status)
                msgString = "Test Status for project " + projectName + " is SUCCESS. ";
            else
                msgString = "Test Status for project " + projectName + " is FAIL. ";
            CommMessage msg = new CommMessage(CommMessage.MessageType.reply);
            msg.from = TestHarnessEnvironment.endPoint;
            msg.to = ClientEnvironment.endPoint;
            msg.author = "Nitesh Bhutani";
            msg.command = "StatusUpdate";
            msg.arguments.Add(msgString);
            comm.postMessage(msg);

        }

        // ----------<Function to get test files from builder>----------
        private void getDllFiles(string toEndPoint)
        {
            CommMessage newMsg = new CommMessage(CommMessage.MessageType.request);
            newMsg.from = TestHarnessEnvironment.endPoint;
            newMsg.to = toEndPoint;
            newMsg.author = "Nitesh Bhutani";
            newMsg.command = "GetTestFiles";

            foreach (var item in dllFiles)
            {

                    newMsg.arguments.Add(item);
                
            }
            comm.postMessage(newMsg);
            Console.WriteLine("\n Test Harness getting Dll getting files from Builder. Message :-");
            newMsg.show();
        }

        //----------<Function to get list of dll files names>---------------
        private void getDllFilesNames(string fileSpec)
        {
            dllFiles.Clear();
            fileSpec = System.IO.Path.GetFullPath(fileSpec);
            if (!loadXml(fileSpec))
            {
                Console.WriteLine("\n Test Harness Logs : Not able to load XML into XDocument");
                return;
            }
            IEnumerable<XElement> projectSubTree = testRequestXML.Descendants("Project");
            foreach (XElement project in projectSubTree)
            {
                //source files
                string projectName = project.Descendants("ProjectName").First().Value;
                //config file .csproj
                string libFile = project.Descendants("Library").First().Value;
                dllFiles.Add(Path.Combine(projectName, libFile));
            }

        }

        //-------------< Helper Function to check if Test Dll present in Test request are present in TH storage> ---------
        private bool checkTestFiles()
        {
            foreach (var file in dllFiles)
            {
                string filePath = System.IO.Path.Combine(TestHarnessEnvironment.root, file);
                if (!System.IO.File.Exists(System.IO.Path.GetFullPath(filePath)))
                    return false;
            }
            return true;
        }

        //----------<Function to load library and execute each library>---------------
        public bool LoadAndTest(string Path)
        {   bool result=false;
            TextWriter _old = Console.Out;
            logBuilder.Flush();
            Console.Write("\n  Testing library {0}", Path);
            Console.Write("\n ==========================================================================");
            try
            {   Console.Write("\n\n  Loading the assembly ... ");
                Assembly asm = Assembly.LoadFrom(Path);
                Console.Write(" Success \n  Checking Types");
                Type[] types = asm.GetTypes();
                foreach (Type type in types)
                {   if (type.GetInterface("CSTestDemo.ITest", true) != null)
                    {   MethodInfo testMethod = type.GetMethod("test");
                        if (testMethod != null)
                        {   Console.Write("\n    Found '{1}' in {0}", type.ToString(), testMethod.ToString());
                            Console.Write("\n  Invoking Test method '{0}'", testMethod.DeclaringType.FullName + "." + testMethod.Name);
                            Console.SetOut(logBuilder);
                            result = (bool)testMethod.Invoke(Activator.CreateInstance(type), null);
                            if (result) Console.Write("\n\n  Test Passed.");
                            else Console.Write("\n\n  Test Failed.");
                            Console.SetOut(_old);
                            return result;
                        }
                    }
                }
                if (!result)
                    Console.Write("\n\n  Could not find 'bool test()' in the assembly.\n  Make sure it implements ITest\n  Test failed");
                return result;
            }
            catch (Exception ex)
            {   Console.Write("\n\n  Error: {0}", ex.Message);
                Console.SetOut(_old);
                result = false;
                return result;
            }
        }
        
        //----------< load Build Request from XML file and store it into XDocument>---------------
        private bool loadXml(string path)
        {
            try
            {
                testRequestXML = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        private static StringWriter logBuilder;


        public string log { get { return logBuilder.ToString(); } }
        Comm comm { get; set; } = null;//comm channel for repo
        Thread rcvThread = null;

        public XDocument testRequestXML { get; set; }
        public List<string> dllFiles { get; set; }

        static void Main(string[] args)
        {
            TestUtilities.title("Test Harness Started ", '=');
            TestHarness th = new TestHarness(); //starting the TH process

        }
    }

#if (TEST_TESTHARNESS)
    class Program
    {   
        static void Main(string[] args)
        {
            TestHarness th = new TestHarness();
            th.LoadAndTest("..\\..\\..\\TestHarness_Folder\\TestDemo\\CSTestDemo.dll");
            Console.WriteLine(th.log);
            //th.sendFile("..\\..\\..\\TestHarness_Folder\\testLog.txt", "..\\..\\..\\Builder_Folder\\testLog.txt");
            CommMessage msg = new CommMessage(CommMessage.MessageType.reply);
            msg.from = TestHarnessEnvironment.root;
            msg.to = ClientEnvironment.endPoint;
            msg.command = "Test";
            msg.arguments.Add("Test Message");
            comm.postMessage(msg);
            Console.WriteLine("\n Process Message being called");
            
        }
    }
#endif
}
