///////////////////////////////////////////////////////////////////////
// Builder.cs - Builder to manage build by passing build request    //
//                to child builder of Build Server Project          //
// Version 1.1														//
//  Language:     C#, VS 2017                                       //
// Application: Remote Build Server - CSE 681 Project 4	            //
// Platform:    Asus R558U, Win 10 Student Edition, Visual studio 17//
// Author:	Nitesh Bhutani, CSE681, nibhutan@syr.edu				//
//////////////////////////////////////////////////////////////////////

/*
Package Operations:
==================
1) Builder.cs - Based on build requests and code sent from the Repository,
                the Build Server builds test libraries for submission to the Test Harness.
                On completion, if successful, the build server submits test libraries and 
                test requests to the Test Harness, and sends build logs to the Repository.
   
Public Interface :
================

public Builder() - Constructor to construct comm channel and start Builder recieve thread
public override void processMessage(Message msg) - Function to process messages from client and repo

Build Process:
==============
- Dependency-  Enviornment.cs, MsgPassing.cs, IMPCommService.cs, MPCommService.cs

Build commands (either one)
- devenv BuildServer.sln /rebuild debug
- devenv Builder.csproj /rebuild debug

Maintainence History :
====================
- Version 1.0 : 6th October 2017
- Version 1.1 : 31th October 2017 - added comm and modified process message

First Release
*/

using System;
using System.Collections.Generic;
using System.Linq;
using SWTools;
using System.Xml.Linq;
using System.IO;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities;
using MessagePassingComm;
using System.Threading;
using System.Diagnostics;

namespace BuildServer
{
    public class Builder : CommunicatorBase
    {
        // ----------<Constructor to construct comm channel and start Builder recieve thread>----------
        public Builder()
        {
            comm = new Comm(MotherBuilderEnvironment.address, MotherBuilderEnvironment.port, MotherBuilderEnvironment.root);
            if (!Directory.Exists(RepoEnvironment.root))
                Directory.CreateDirectory(RepoEnvironment.root);
            if (!Directory.Exists(MotherBuilderEnvironment.root))
                Directory.CreateDirectory(MotherBuilderEnvironment.root);
            if (!Directory.Exists(TestHarnessEnvironment.root))
                Directory.CreateDirectory(TestHarnessEnvironment.root);
            rcvQ = new BlockingQueue<Message>();
            if (readyMessages == null)
                readyMessages = new BlockingQueue<CommMessage>();
            if (childBuilderEndpoint == null)
                childBuilderEndpoint = new List<string>();
            if (processList == null)
                processList = new List<Process>();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
            start();//thread to deq ready messages from child process and request messages from other server
        }

        //----------------<Function to create single process called by "createProcessPool" function >----------------
        static bool createProcess(string address, string port)
        {
            Process proc = new Process();
            proc.StartInfo.Verb = "runas";
            string fileName = "..\\..\\..\\ChildBuilder\\bin\\debug\\ChildBuilder.exe";
            string absFileSpec = Path.GetFullPath(fileName);
            Console.Write("\n  attempting to start {0}", absFileSpec);
            string commandline = address + " " + port;
            try
            {
                proc = Process.Start(fileName, commandline);
                childBuilderEndpoint.Add(address + ":" + port.ToString() + "/IMessagePassingComm");
                processList.Add(proc);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }

            return true;
        }
        //----------------<Recieve thread for builder>----------------
        void rcvThreadProc()
        {
            Console.Write("\n  starting Mother Builder receive thread");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                Console.Write("\n Message of Mother Builder receive thread");
                msg.show();
                if (msg.command == null)
                    continue;
                if (msg.command == "quit")
                    break;
                if (msg.command == "Ready")
                {
                    proccessChildBuilderReadyCommand(msg);
                    continue;
                }
                if (msg.command == "QuitChildBuilder")
                {
                    processQuitCommand();
                    continue;
                }
                if (msg.command == "BuildRequest")
                {   string msgBody = null;
                    foreach (var arg in msg.arguments)
                    {
                        msgBody += arg + "\n";
                    }
                    msgBody = msgBody.Remove(msgBody.Length - 1);
                    Message newMsg = Message.makeMsg(msg.command, "Builder", "Repository", msgBody);
                    rcvQ.enQ(newMsg);
                    continue;
                }
                if (msg.command == "CreatePoolProcess")
                {
                    createProcessPool(msg.arguments[0]);
                }
            }
        }

        //----------------<Function to create Process pool>----------------
        private void createProcessPool(string noOfProcess)
        {
            processQuitCommand();
            int no = Int32.Parse(noOfProcess);
            int port = 0;
            for(int i=0; i < no; i++)
            {
                createProcess("http://localhost", (ChildProcessStartingPortNo + port).ToString());
                port += 10;
            }
        }

        //----------------<Function to quit child processes>----------------
        private void processQuitCommand()
        {

            foreach (var proc in processList)
            {
                if (!proc.HasExited)
                    proc.Kill();
               
            }
            processList.Clear();
        }

        // ----------<Function to process Ready Messages from child Builder to Mother Builder>----------
        private void proccessChildBuilderReadyCommand(CommMessage msg)
        {
            readyMessages.enQ(msg);
        }

        // ----------<Function to process messages from client and repo>----------
        public override void processMessage(Message msg)
        {
            if (msg.from == "Repository")
            {
                if (msg.type == "BuildRequest")
                {
                    processBuildRequest(msg);
                }
            }
            else if (msg.from == "Client")
            {/*any client commands directly to Builder*/  }
            else
            {
                Console.Write("\n Builder : Message from outside Fedration Server with contents :- \n");
                Console.Write(msg.ToString());
                Console.Write("\n\n");
            }
        }

        // ----------<Function to send Build Request to child builder based on ready messages from them>----------
        private void processBuildRequest(Message msg)
        {
            CommMessage readyMsg = readyMessages.deQ();
            CommMessage sendMsg = new CommMessage(CommMessage.MessageType.request);
            sendMsg.from = MotherBuilderEnvironment.endPoint;
            sendMsg.to = readyMsg.from;
            sendMsg.author = "Nitesh Bhutani";
            sendMsg.command = msg.type;
            string[] args = msg.body.Split('\n');
            foreach(var a in args)
            {
                sendMsg.arguments.Add(a);
            }
            comm.postMessage(sendMsg);
        }


        Comm comm { get; set; } = null;//comm channel for repo

        Thread rcvThread = null;
        protected static SWTools.BlockingQueue<CommMessage> readyMessages;
        protected static List<string> childBuilderEndpoint = null;
        protected static List<Process> processList = null;
        public static int ChildProcessStartingPortNo { get; set; } = 9000;

        static void Main(string[] args)
        {
            TestUtilities.title("Mother Builder Started ", '=');
            Builder cl = new Builder();

        }
    }

 #if (TEST_BUILDER)
    class Program
    {   
        static void Main(string[] args)
        {
            TestUtilities.title("Mother Builder Started ",'=');
            Builder cl = new Builder();
            Message msg = Message.makeMsg("TestMessage","Repository","TestHarness","Test Message");
            cl.processMessage(msg);
            Console.WriteLine("Process Message being called");
            
        }
    }
#endif
}
