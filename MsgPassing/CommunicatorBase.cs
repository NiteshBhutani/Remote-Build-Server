//////////////////////////////////////////////////////////////////////
// CommunicationBase.cs - Base class for Mother Builder that provide//
//              queue and thread for server to deq ready and request//
//              messages from child process, repo and client       //
// Version 1.0														//
//  Language:     C#, VS 2017                                       //
// Application: Remote Build Server - CSE 681 Project 4	            //
// Platform:    Asus R558U, Win 10 Student Edition, Visual studio 17//
// Author:	Nitesh Bhutani, CSE681, nibhutan@syr.edu				//
//////////////////////////////////////////////////////////////////////

/*
Package Operations:
==================
1)  CommunicationBase.cs - Base class for Mother Builder that provide queue and thread for server 
                           to deq ready and request messages from child process, repo and client 
                           
                           It is inherted from Icommunicator interface which provides method to post message.
Public Interface :
================
public Thread thread - // Base thread for CommunicationBase class
public CommunicatorBase() - // Function declared to avoid default constructor defination by compiler
public void postMessage(Message msg) -// Function to post message into each thread of repository, builder or client.
public Thread start() -// Function to start thread of the server in fedration.
public virtual void processMessage(Message msg)-// Virtual Function to process message communicated to each of the server that inherits it. Each server class implements this method.
public void wait() - // Function to wait for all thread to finish

Build Process:
==============
- Dependency - Enviornment.cs, BlockingQueue.cs

Build commands (either one)
- devenv BuildServer.sln /rebuild debug
- devenv MsgPassing.csproj /rebuild debug

Maintainence History :
====================
- Version 1.0 : 5th October 2017
- Version 1.1 : 29th October 2017 - Updated Comments
First Release
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SWTools;

namespace BuildServer
{
    public abstract class CommunicatorBase : ICommunicator
    {
        //---------< Base thread for CommunicationBase class > -----------------
        public Thread thread { get; set; }
        //----------< Function declared to avoid default constructor defination by compiler >----------------
        public CommunicatorBase() {   }
        //----------< Function to post message into each thread of repository, builder or client. >----------------
        public void postMessage(Message msg)
    {
      rcvQ.enQ(msg);
    }
        //-----------< Function to start thread of each of the server in fedration. >---------------
        public Thread start()
    {
      thrd = new Thread(
        () => {
          while (true)
          {
            Message msg = rcvQ.deQ();
            Console.Write("\n  {0}", msg.ToString());
            processMessage(msg);
            if (msg.body == "quit")
            {
              Console.Write("\n  {0} thread quitting", msg.to);
              break;
            }
            if(msg.from == "TestHarness" && msg.type == "TestStatus" && msg.body.Contains("True"))
            {
                    testExececuted = true;
            }
          }
        }
      );
      thrd.IsBackground = true;
      thrd.Start();
      Environment.threadList.Add(thrd);
      thread = thrd;
      return thrd;
    }
        //-----------<Virtual Function to process message communicated to the server that inherits it. Each server class implements this method if it inheits this. >---------------
        public virtual void processMessage(Message msg) { }
        // ------------< Function to wait for all thread to finish >-----------------------
        public void wait()
        {
            thread.Join();  // only waits for own thread
        }

        static protected Environment environ; 
        protected BlockingQueue<Message> rcvQ = null; // Queue for the server who inherits it
        protected Thread thrd = null;
        static public bool testExececuted { get; set; } = false;
    }

#if (TEST_MSGPASSING)
    class Program
    {   
        static void Main(string[] args)
        {
            //These function will be tested in its inhereted class like  Builder   
        }
    }
    #endif


}
