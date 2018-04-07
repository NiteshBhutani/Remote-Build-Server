///////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - Mock- Client GUI to display fucntionality   //
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
1) Client.cs - It provides the Mock-Client GUI with enough functionality to represent functionality of Build Server. 
                It command client to build with Build Request and Process any messages back from Builder,Repo, TestHarness

                1) Its display repo, test harness, client and builder folder in GUI
                2) Functionality to create test request
                3)function to start number of child process
                4) functionality to send build request
Public Interface :
================

public MainWindow() - Constructor to construct comm and start Client thread and demo req
public vsoid readFile(string filespec) - Function to readfile and display on console with any exception messages
public void getTopFiles() - Function to get top files of client

Build Process:
==============
-Dependency  Enviornment.cs, IMPCommService.cs, MPCommService.cs

Build commands (either one)
- devenv BuildServer.sln /rebuild debug
- devenv ClientGUI.csproj /rebuild debug

Maintainence History :
====================
- Version 1.0 : 31st October 2017
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BuildServer;
using System.Windows.Controls.Primitives;

namespace ClientGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml - Client GUI CodeBehind
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        Comm comm { get; set; } = null;
        Thread rcvThread = null;
        Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
        private IFileMgr fileMgr { get; set; } = null;  // note: Navigator just uses interface declarations

        //------------------<Constructor of MainWindow object>---------------
        public MainWindow()
        {
            TestUtilities.title("Client Started ", '=');
            InitializeComponent();
            comm = new Comm(ClientEnvironment.address, ClientEnvironment.port, ClientEnvironment.root);
            initializeEnvironment();
            fileMgr = FileMgrFactory.create(FileMgrType.Local); // uses Environment
            getTopFiles();
            if (!Directory.Exists(ClientEnvironment.root))
                Directory.CreateDirectory(ClientEnvironment.root);
            initializeMessageDispatcher();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
            RepoTop_Files();

            demoReq();


        }

        //------------------<Helper Function to demp req>---------------

        private void demoReq()
        {
            TestUtilities.title("Project 4 - Remote Build Server", '=');
            demoReq3_4_5_6();
            CreateProcess.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect);
            connectMsg.from = ClientEnvironment.endPoint;
            connectMsg.to = RepoEnvironment.endPoint;
            comm.postMessage(connectMsg);
            Thread.Sleep(2000);
            comm.postFile("BuildRequest1.xml");
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = RepoEnvironment.endPoint;
            msg1.command = "BuildRequest";
            msg1.arguments.Add("BuildRequest1.xml");
            comm.postMessage(msg1);
            demoReq7_8_9_11_12_13();
        }

        //------------------<Function to initialize client env>---------------

        void initializeEnvironment()
        {
            BuildServer.Env.root = ClientEnvironment.root;
            BuildServer.Env.address = ClientEnvironment.address;
            BuildServer.Env.port = ClientEnvironment.port;
            BuildServer.Env.endPoint = ClientEnvironment.endPoint;
        }

        //------------------<Function to initialize Command Dispatcher for client>---------------

        void initializeMessageDispatcher()
        {
            messageDispatcher["getTopFiles"] = (CommMessage msg) =>    // load remoteFiles listbox with files from root
            {   repoFiles.Items.Clear();
                foreach (string file in msg.arguments)
                {  repoFiles.Items.Add(file);     }
            };
            
           messageDispatcher["getTopDirs"] = (CommMessage msg) => // load remoteDirs listbox with dirs from root
            {   repoDirs.Items.Clear();
                foreach (string dir in msg.arguments)
                { repoDirs.Items.Add(dir);       }
            };
            messageDispatcher["moveIntoFolderFiles"] = (CommMessage msg) => // load remoteFiles listbox with files from folder
            {   repoFiles.Items.Clear();
                foreach (string file in msg.arguments)
                {   repoFiles.Items.Add(file);                }
            };
            messageDispatcher["moveIntoFolderDirs"] = (CommMessage msg) => // load remoteDirs listbox with dirs from folder
            {   repoDirs.Items.Clear();
                foreach (string dir in msg.arguments)
                {   repoDirs.Items.Add(dir);                }
            };
            messageDispatcher["BadRequestXML"] = (CommMessage msg) =>
            {   Console.WriteLine("\n Client : Error from Reppository : + {0}", msg.arguments[0]);
            };
            messageDispatcher["StatusUpdate"] = (CommMessage msg) =>
            {
                status.Text = "Status: "+ msg.arguments[0];
            };
        }

        //------------------<Client recieve thread procesessing>---------------

        void rcvThreadProc()
        {
            Console.Write("\n  starting client's receive thread");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                Console.Write("\n Message of Client receive thread");
                msg.show();
                if (msg.command == null)
                    continue;

                // pass the Dispatcher's action value to the main thread for execution

                Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
            }
        }

        //------------------<Winodw_closed event handler>---------------

        private void Window_Closed(object sender, EventArgs e)
        {
            sendQuitChildBuilderToBuilder();//send quit message to child builder
            comm.close();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        //------------------<Helper function for req 1 to 5>---------------
        private void demoReq3_4_5_6()
        {
            TestUtilities.title("\n Demonstrating Req 2, 3, 5, 6,  .....", '=');
            Console.WriteLine("\n Creating the Process pool of 2 child builder for demonstration and sending single request XML from repo. The Child builder process recieve build request from mother builder using WCF when Ready. /n Each component uses Message passing comm built using WCF");
            Console.WriteLine("\n Then it communicates with repo to get appropraite files.    ");

        }

        //------------------<Helper function for req 6 to 9>---------------
        private void demoReq7_8_9_11_12_13()
        {
            TestUtilities.title("\n Demonstrating Req  7, 8, 9, .....");

            Console.WriteLine("\n 7,8)After recieving build request child builder will try to buil the file , send build logs to repo and is build succeeds will send dll along with test request to Test Harness");
            Console.WriteLine("9)Test Harness will pull the files from builder as per test request and will load and execute the test, sending test logs to repo.");
            Console.WriteLine("\n---- (Build/Test Log File Location:- In MotherBuilder/TestHarness root and Repository root {Project_Name/buildLog.txt}");

            TestUtilities.title("\n Demonstrating Req  10, 11, 12, 13 .....");

            Console.WriteLine("\n 10) Include a Graphical User Interface, built using WPF.", '-');
            Console.WriteLine("\n Client is a GUI built using WPF");

            Console.WriteLine("11) The GUI client is a separate process, implemented with WPF and using message-passing communication. " +
                "It provide mechanisms to get file lists from the Repository, and select files for adding to a build request structure. " +
                "It provides the capability of repeating that process to add other test libraries to the build request structure.");
            Console.WriteLine();
            Console.WriteLine("12) The client send build request structures to the repository for storage and transmission to the Build Server");
            Console.WriteLine("\n The \"Client Navigation\" tab provides the mechanism for this. Please select the file in client folder and click \"Send Request to Repo\" for sending select request XML for storage in Repo. " +
                "\n The XML request created with help of GUI-client will automatically be sent to repo for storage ");
            Console.WriteLine();
            Console.WriteLine("13) The client is  able to request the repository to send a build request in its storage to the Build Server for build processing.\n (\"Repo Navigation tab provides functionality for this.\")");
            Console.WriteLine("Select the a single XML file and click \" Send Build Request\" in Repo Navigation tab");
            Console.WriteLine();
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
        
        //------------------<Function to get top files of client>---------------
        public void getTopFiles()
        {
            List<string> files = fileMgr.getFiles().ToList<string>();
            clientFiles.Items.Clear();
            foreach (string file in files)
            {
                clientFiles.Items.Add(file);
            }
            List<string> dirs = fileMgr.getDirs().ToList<string>();
            clientDirs.Items.Clear();
            foreach (string dir in dirs)
            {
                clientDirs.Items.Add(dir);
            }
        }

        //------------------<Event Handler for ClientTop_Click>---------------
        private void ClientTop_Click(object sender, RoutedEventArgs e)
        {
            fileMgr.currentPath = "";
            getTopFiles();
        }

        //----< move to parent directory and show files and subdirs >----
        private void clientUp_Click(object sender, RoutedEventArgs e)
        {
            if (fileMgr.currentPath == "")
                return;
            fileMgr.currentPath = fileMgr.pathStack.Peek();
            fileMgr.pathStack.Pop();
            getTopFiles();
        }
        
        //----< move into subdir and show files and subdirs >------------
        private void clientDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string dirName = clientDirs.SelectedValue as string;
            fileMgr.pathStack.Push(fileMgr.currentPath);
            fileMgr.currentPath = dirName;
            getTopFiles();
        }

        //----< give repo root files >---------------------
        private void RepoTop_Files()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = RepoEnvironment.endPoint;
            msg1.author = "Nitesh Bhutani";
            msg1.command = "getTopFiles";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getTopDirs";
            comm.postMessage(msg2);
        }
        //----< move to root of repo directories >---------------------
        private void RepoTop_Click(object sender, RoutedEventArgs e)
        {
            RepoTop_Files();
        }
        //----< download file and display source in popup window >-------
        private void repoFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // coming soon

        }
        //----< move to parent directory of current repo path >--------
        private void RepoUp_Click(object sender, RoutedEventArgs e)
        {
            // coming soon
        }
        //----< move into repo subdir and display files and subdirs >--
        private void repoDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = RepoEnvironment.endPoint;
            msg1.command = "moveIntoFolderFiles";
            msg1.arguments.Add(repoDirs.SelectedValue as string);
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "moveIntoFolderDirs";
            comm.postMessage(msg2);
        }

        //-----< create test request by selecting repo files>-------
        private void createRequest_Click(object sender, RoutedEventArgs e)
        {   string projectName = repoFiles.SelectedItems[0].ToString().Split('\\')[0];
            string configFileName = null;
            List<string> files = new List<string>();
            foreach (var val in repoFiles.SelectedItems)
            {   string fileName = val.ToString().Split('\\')[1];
                var extension = fileName.Split('.')[1];
                if (extension == "csproj")
                    configFileName = fileName;
                else
                    files.Add(fileName);
            }
            XDocument testRequestXML = new XDocument();
            XElement testRequestElem = new XElement("BuildRequest");
            testRequestXML.Add(testRequestElem);
            XElement authorElem = new XElement("Author");
            authorElem.Add("Nitesh");
            testRequestElem.Add(authorElem);
            XElement NameElem = new XElement("Name");
            NameElem.Add(projectName+"_request");
            testRequestElem.Add(NameElem);
            XElement buildToolElem = new XElement("BuildTool");
            buildToolElem.Add("MSBuild");
            testRequestElem.Add(buildToolElem);
            XElement projectElement = new XElement("Project");
            XElement projectNameElement = new XElement("ProjectName");
            projectNameElement.Add(projectName);
            projectElement.Add(projectNameElement);
            XElement SourceFile = new XElement("SourceFiles");
            foreach(var file in files)
            {   XElement f = new XElement("File");
                f.Add(file);
                SourceFile.Add(f);
            }
            projectElement.Add(SourceFile);
            XElement configEle = new XElement("Config");
            configEle.Add(configFileName);
            projectElement.Add(configEle);
            testRequestElem.Add(projectElement);
            Console.WriteLine("\n XMl :- \n" + testRequestXML.ToString());
            Console.WriteLine("\n Saving it to Location {0}....",ClientEnvironment.root);
            testRequestXML.Save(System.IO.Path.Combine(ClientEnvironment.root, projectName+ "_buildrequest.xml"));
            sendGenerateRequestFileToRepo(projectName + "_buildrequest.xml");
        }

        //-----< Helper function to send generated build request to repo for storage.>-------
        private void sendGenerateRequestFileToRepo(string filespec)
        {
            Console.WriteLine("\n Sending it to Location {0} - Repository Folder for storage....", RepoEnvironment.root);
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect); //send this request xml file created to repo folder for storage done
            connectMsg.from = ClientEnvironment.endPoint;
            connectMsg.to = RepoEnvironment.endPoint;
            comm.postMessage(connectMsg);
            Thread.Sleep(2000);
            comm.postFile(filespec);


        }
        //----< send test request to repo >---------------------
        private void sendBuildRequest_Click(object sender, RoutedEventArgs e)
        {
            //update this to send build request from repo folder - done
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = RepoEnvironment.endPoint;
            msg1.command = "BuildRequest";
            msg1.arguments.Add(repoFiles.SelectedValue as string);
            Console.WriteLine("********************** Sending Build Request to client :- ****************************");
            readFile(System.IO.Path.Combine(RepoEnvironment.root, repoFiles.SelectedValue as string));
            comm.postMessage(msg1);
        }
        /*----------Function to send request from client to Repo -----------*/
        private void ClientSend_Click(object sender, RoutedEventArgs e)
        {
            sendGenerateRequestFileToRepo(clientFiles.SelectedValue as string);
        }
        /*----------Function to Kill Child Process -----------*/
            private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            sendQuitChildBuilderToBuilder();
            
        }

        //----< Function to send "QuitChildBuilder" message to repo>---------------------
        private void sendQuitChildBuilderToBuilder()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = MotherBuilderEnvironment.endPoint;
            msg1.command = "QuitChildBuilder";
            comm.postMessage(msg1);
        }

        //----< Function to send quit message to repo>---------------------
        private void sendQuitToRepo()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = RepoEnvironment.endPoint;
            msg1.command = "quit";
            comm.postMessage(msg1);
        }

        //----< Function to send quit message to builder>---------------------
        private void sendQuitToBuilder()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = MotherBuilderEnvironment.endPoint;
            msg1.command = "quit";
            comm.postMessage(msg1);
        }

        /*----------Function to Create Child Process -----------*/
        private void CreateProcess_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = MotherBuilderEnvironment.endPoint;
            msg1.command = "CreatePoolProcess";
            msg1.arguments.Add(ProcessNum.Text as string);
            comm.postMessage(msg1);

        }
    }
}
