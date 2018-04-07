///////////////////////////////////////////////////////////////////////
// Repo.cs - Mock- Repository to display fucntionality            //
//                  for Build Server Project                        //
// Version 1.1														//
//  Language:     C#, VS 2017                                       //
// Application: Remote Build Server - CSE 681 Project 4	            //
// Platform:    Asus R558U, Win 10 Student Edition, Visual studio 17//
// Author:	Nitesh Bhutani, CSE681, nibhutan@syr.edu				//
//////////////////////////////////////////////////////////////////////

/*
Package Operations:
==================
1) Repo.cs - It provides the Mock-Repository with enough functionality to represent functionality of Build Server. 
             It recives command from client to build with Build Request and commands Builder to build by sending(WCF) build files and build request
             In Project 4 it will recives test logs and build logs from testharness and builder. 
             (Project 4)It can also recieved command from client to send test and build logs back to client.

Public Interface :
================

public Repo() - Constructor to construct comm channel, initialize env and start Repo recieve thread
public  void processMessage(Comm msg) - Function to process messages from client, builder and test harness
public void loadXml() - load Build Request from XML file and store it into XDocument

Build Process:
==============
- Dependency-  Enviornment.cs, IMPCommService.cs, MPCommService.cs

Build commands (either one)
- devenv BuildServer.sln /rebuild debug
- devenv Repo.csproj /rebuild debug

Maintainence History :
====================
- Version 1.0 : 6th October 2017
- version 1.1 : 27th Oct 2017 - added wcf comm and modified process message recpectively
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

namespace BuildServer
{
    public class Repo
    {
        //-------------<Constructor to construct Communication channel, initialize env and start recieve Repo thread> ---------
        public Repo()
        {
            comm = new Comm(RepoEnvironment.address, RepoEnvironment.port, RepoEnvironment.root);
            initializeEnvironment();
            localFileMgr = FileMgrFactory.create(FileMgrType.Local);
            if (!Directory.Exists(RepoEnvironment.root))
                Directory.CreateDirectory(RepoEnvironment.root);
            if (!Directory.Exists(MotherBuilderEnvironment.root))
                Directory.CreateDirectory(MotherBuilderEnvironment.root);
            buildRequestXML = new XDocument();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }
        //-------------<Function to Env used by FileManger to Repo Location> ---------
        void initializeEnvironment()
        {
            Env.root = RepoEnvironment.root;
            Env.address = RepoEnvironment.address;
            Env.port = RepoEnvironment.port;
            Env.endPoint = RepoEnvironment.endPoint;
        }
        //-------------<Thread to process incoming WCF CommMessage  in reciver queue for processing > ---------
        void rcvThreadProc()
        {
            Console.Write("\n  Starting Repository receive thread");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                Console.Write("\n Message of Repository receive thread");
                msg.show();
                if (msg.command == null)
                    continue;
                if (msg.command == "quit")
                    break;
                processMessage(msg);
            }
        }

        //-------------<Function to send build files to childbuilder> ---------
        private void transferFilesToBuilder(CommMessage msg)
        {
            CommMessage newMsg = new CommMessage(CommMessage.MessageType.reply);
            newMsg.from = RepoEnvironment.endPoint;
            newMsg.to = msg.from;
            newMsg.author = "Nitesh Bhutani";
            newMsg.command = "SendingBuildFiles";
            comm.postMessage(newMsg); //msg send to create channel between child builder process and repo
            Thread.Sleep(2000);
            foreach (var file in msg.arguments)
            {
                comm.postFile(file);
            }
        }

        //-------------<Function to process messages from client, builder and test harness> ---------
        public void processMessage(CommMessage msg)
        {   if (msg.from == ClientEnvironment.endPoint && msg.command == "BuildRequest")
            { processBuildRequest(msg.arguments); }
            else if (msg.command == "GetFiles") { transferFilesToBuilder(msg); }
            else if(msg.command == "getTopFiles") { processRepoTopFiles(msg); }
            else if (msg.command == "getTopDirs") { processRepoTopDirs(msg); }
            else if (msg.command == "moveIntoFolderFiles") { processRepoMoveIntoFolderFiles(msg); }
            else if (msg.command == "moveIntoFolderDirs") { processRepoMoveIntoFolderDirs(msg); }
            else
            {   Console.Write("\n Repository : Message from outside Fedration Server with contents :- \n");
                Console.Write(msg.ToString());
                Console.Write("\n\n");}
        }

        //-------------<Function to reply Repo top level dirs to client> ---------
        private void processRepoTopDirs(CommMessage msg) {
            localFileMgr.currentPath = "";
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = msg.from;
            reply.from = msg.to;
            reply.command = "getTopDirs";
            reply.arguments = localFileMgr.getDirs().ToList<string>();
            comm.postMessage(reply);
        }
        //-------------<Function to process MoveintoFolder command from client> ---------
        private void processRepoMoveIntoFolderFiles(CommMessage msg) {

            if (msg.arguments.Count() == 1)
                localFileMgr.currentPath = msg.arguments[0];
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = msg.from;
            reply.from = msg.to;
            reply.command = "moveIntoFolderFiles";
            reply.arguments = localFileMgr.getFiles().ToList<string>();
            comm.postMessage(reply);
        }

        //-------------<Function to process MoveIntoFolderDirs command from client> ---------
        private void processRepoMoveIntoFolderDirs(CommMessage msg) {
            if (msg.arguments.Count() == 1)
                localFileMgr.currentPath = msg.arguments[0];
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = msg.from;
            reply.from = msg.to;
            reply.command = "moveIntoFolderDirs";
            reply.arguments = localFileMgr.getDirs().ToList<string>();
            comm.postMessage(reply);
        }

        //-------------<Function to reply Repo top level files to client> ---------
        private void processRepoTopFiles(CommMessage msg)
        {
            localFileMgr.currentPath = "";
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = msg.from;
            reply.from = msg.to;
            reply.command = "getTopFiles";
            reply.arguments = localFileMgr.getFiles().ToList<string>();
            comm.postMessage(reply);
        }

        //-------------< Function to process build request by client> ---------
        private void processBuildRequest(List<string> buildRequests)
        {
            foreach (var request in buildRequests)
            {
                if (checkBuildFiles(request, RepoEnvironment.root))
                {
                    CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect);
                    connectMsg.from = RepoEnvironment.endPoint;
                    connectMsg.to = MotherBuilderEnvironment.endPoint;
                    comm.postMessage(connectMsg);
                    Thread.Sleep(2000);
                    comm.postFile(request);
                    CommMessage newMsg = new CommMessage(CommMessage.MessageType.request);
                    newMsg.from = RepoEnvironment.endPoint;
                    newMsg.to = MotherBuilderEnvironment.endPoint;
                    newMsg.author = "Nitesh Bhutani";
                    newMsg.command = "BuildRequest";
                    newMsg.arguments.Add(request);
                    comm.postMessage(newMsg);
                    

                }
                else
                {
                    CommMessage newMsg = new CommMessage(CommMessage.MessageType.reply);
                    newMsg.from = RepoEnvironment.endPoint;
                    newMsg.to = ClientEnvironment.endPoint;
                    newMsg.author = "Nitesh Bhutani";
                    newMsg.command = "BadRequestXML";
                    newMsg.arguments.Add("1 or more Files does not exist in Repository");
                    comm.postMessage(newMsg);
                }
            }
        }

        //-------------< Helper Function to check if files present in build request are present in storage> ---------
        private bool checkBuildFiles(string fileName, string filepath)
        {
            if (!System.IO.Directory.Exists(filepath))
            {
                Console.WriteLine("\n Repository Logs : Build Request XML directory does not exist");
                return false;
            }
            string fileSpec = System.IO.Path.Combine(filepath, fileName);
            fileSpec = System.IO.Path.GetFullPath(fileSpec);
            if (!loadXml(fileSpec))
            {
                Console.WriteLine("\n Repository Logs : Not able to load XML into XDocument");
                return false;
            }

            List<string> fileList = getFileList();

            foreach (string file in fileList)
            {
                string filePath = System.IO.Path.Combine(RepoEnvironment.root, file);
                if (!System.IO.File.Exists(System.IO.Path.GetFullPath(filePath)))
                    return false;
            }

            return true;
        }
        //-------------< Helper Function to get list of files in build request> ---------
        private List<string> getFileList()
        {
            List<string> files = new List<string>();
            IEnumerable<XElement> projectSubTree = buildRequestXML.Descendants("Project");

            foreach (XElement project in projectSubTree)
            {
                //source files
                string projectName = project.Descendants("ProjectName").First().Value;
                IEnumerable<XElement> parseElems = project.Descendants("File");
                foreach (XElement elem in parseElems)
                {
                    files.Add(System.IO.Path.Combine(projectName + "\\" + elem.Value));
                }
                //config file .csproj
                string configFile = project.Descendants("Config").First().Value;
                files.Add(System.IO.Path.Combine(projectName + "\\" + configFile));
            }
            return files;
        }
        /*----< load Build Request from XML file and store it into XDocument >-----------------------*/
        public bool loadXml(string path)
        {
            try
            {
                buildRequestXML = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        // Properties
        Comm comm { get; set; } = null;//comm channel for repo
        Thread rcvThread = null;
        IFileMgr localFileMgr { get; set; } = null;

        public XDocument buildRequestXML { get; set; }

        static void Main(string[] args)
        {
            TestUtilities.title("Repository Started ", '=');
            Repo rp = new Repo(); //starting the repo process

        }
    }
#if (TEST_REPO)
    class Program
    {   
        static void Main(string[] args)
        {
            Repo rp = new Repo();
            rp.loadXml("..\\..\\..\\Client_folder\\BuildRequest1.xml");
            Console.WriteLine(rp.buildRequestXML.ToString());
            Console.WriteLine(rp.readFileToString("..\\..\\..\\Client_folder\\BuildRequest1.xml"));
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = RepoEnvironment.endPoint;
            msg1.command = "BuildRequest";
            msg1.arguments.Add("BuilRequest1.xml");
            rp.processMessage(msg1);
            Console.WriteLine(rp.readFileToString("..\\..\\..\\Client_folder\\BuildRequest1.xml"));
            Console.WriteLine("Process Message being called");
            
        }
    }
#endif

}

