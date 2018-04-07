///////////////////////////////////////////////////////////////////////
// Environment.cs - Shared Code for Build Server Project            //
//                        Contains Enviorment, Message class        //
//                        And struct containing every server info   //
// Version 1.1														//
//  Language:     C#, VS 2017                                       //
// Application: Remote Build Server - CSE 681 Project 4	            //
// Platform:    Asus R558U, Win 10 Student Edition, Visual studio 17//
// Author:	Nitesh Bhutani, CSE681, nibhutan@syr.edu				//
//////////////////////////////////////////////////////////////////////

/*
Package Operations:
==================
1) server Structs - struct for Mother builder, client, repo and test harness which stores there address, port, endpoint and root location
2) ICommunicator interface- Interface inherited by CommunicatiorBase class to inplement postMessage method used as mother builder second recieve queue.
3) Message Class - Defines the properties or structure of messages which are passed between the federation servers.                           
4) Environment class - that contains all the thread started by communicator base for sync
Public Interface :
================

Environment Class - 
    public ICommunicator client - // client property
    public ICommunicator testHarness - // TestHarness property
    public static List<Thread> threadList //list of thread for each of the servers thread
    public static void wait() -  //Function to wait for each of the server(client, repository, Builder and TestHarness thread to finish.
 
Message Class -     

public static Message makeMsg(string type, string to, string from, string body) - // Static Function to create message by assigning values to type, to, from and Body
public override string ToString() - //Function to override ToString() function - which is caleed when message class object is converted to string

Build Process:
==============
- Dependency Enviornment.cs

Build commands (either one)
- devenv BuildServer.sln /rebuild debug
- devenv Enviornment.csproj /rebuild debug

Maintainence History :
====================
- Version 1.0 : 5th October 2017
- Version 1.1 : 26th October 2017 - added builder, repo client and Test Harness env

First Release
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BuildServer
{
  public interface ICommunicator
  {
    void postMessage(Message msg); //CommunicatorBase class implements it.
   
   }

    // Defining Mother Builder constant variables
    public struct MotherBuilderEnvironment
    {
        public static string root { get; set; } = "..\\..\\..\\Builder_Folder\\";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8090/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8090;
        public static bool verbose { get; set; } = false;
    }

    // Defining Repo constant variables
    public struct RepoEnvironment
    {
        public static string root { get; set; } = "..\\..\\..\\Repository_Folder\\";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8080/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8080;
        public static bool verbose { get; set; } = false;
    }

    // Defining client constant variables
    public struct ClientEnvironment
    {
        public static string root { get; set; } = "..\\..\\..\\Client_Folder\\";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8060/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8060;
        public static bool verbose { get; set; } = false;
    }

    // Defining Test Harness constant variables
    public struct TestHarnessEnvironment
    {
        public static string root { get; set; } = "..\\..\\..\\TestHarness_Folder\\";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8070/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8070;
        public static bool verbose { get; set; } = false;
    }

    // Defining Env used by communicator base to bind alll the classes that inherits it
    public struct Environment
  {
        public static List<Thread> threadList { get; set; } = new List<Thread>(); //list of thread for each of the servers thread

        //Function to wait for each of the server(client, repository, Builder and TestHarness thread to finish.
        public static void wait() 
        {
          foreach (Thread t in Environment.threadList)
          {
            t.Join();
          }
        }
  }

    // Defining variable environment used by Filemgr
    public struct Env
    {
        public static string root { get; set; }
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; }
        public static string address { get; set; }
        public static int port { get; set; }
        public static bool verbose { get; set; }
    }

    //message class used by mother builder
    public class Message
  {
        public string type { get; set; } = "BuildRequest";

        public string to { get; set; } = "";
        public string from { get; set; } = "";
        public string body { get; set; } = "";

        // Static Function to create message by assigning values to type, to, from and Body
        public static Message makeMsg(string type, string to, string from, string body)
        {
          Message msg = new Message();
          msg.type = type;
          msg.to = to;
          msg.from = from;
          msg.body = body;
          return msg;
        }
        //Function to override ToString() function - which is caleed when message class object is converted to string
        public override string ToString()
        {
          string outStr = "Message - \n " +
            string.Format("type: {0} \n ", type) +
            string.Format("from: {0} \n ", from) +
            string.Format("to: {0} \n ", to) +
            string.Format("body: \n {0} \n ", body);
          return outStr;
        }
  }

#if (TEST_ENVIRONMENT)
    class Program
    {   
        static void Main(string[] args)
        {
            Message msg = new Message();
            Message.makeMsg("Testing type","client","Harness","This is demo message");
            Console.WriteLine(msg.ToString());
        }
    }
#endif
}
