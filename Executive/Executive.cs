///////////////////////////////////////////////////////////////////////
// Executive.cs - Test Suite to demostrate				            //
//                     requirements                                 //
// Version 1.1														//
//  Language:     C#, VS 2017                                       //
// Application: Remote Build Server - CSE 681 Project 4	            //
// Platform:    Asus R558U, Win 10 Student Edition, Visual studio 17//
// Author:	Nitesh Bhutani, CSE681, nibhutan@syr.edu				//
//////////////////////////////////////////////////////////////////////

/*
Package Operations:
==================
1)  Executive class- Test Suite class to demonstrate requirements. 

Public Interface :
================
public Executive() - Constructor
public void Req1to6() - helper function to demo Req
public void Req7to13() - helper function to demo req
void displaySubtitle(string s) - Helper function to display subtitle 

Build Process:
==============
- Dependency - Environment.cs, TestUtilities.cs

Build commands (either one)
- devenv BuildServer.sln /rebuild debug
- devenv Executive.csproj /rebuild debug

Maintainence History :
====================
- Version 1.0 : 6th October 2017
- Version 1.2 : 29th October 2017 - modified for project 3
- Version 1.3 : 5th December 2017 - modified for project 4

First Release
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BuildServer
{
    public class Executive 
    {
        //-------------< Constructor>-------------------
        public Executive()
        {
        }

        


        // ----------- < req 1, 2, 3, 4, 5, 6 demo > -----------
        public void Req1to6()
        {
            Console.WriteLine("1) This project is developed using C#, the .Net Frameowrk, and Visual Studio 2017");
            Console.WriteLine();
            Console.WriteLine("\n2) Message-Passing communication is used by Client GUI, Repo,  Mother Builder and child builder or processes and Test Harness");
            Console.WriteLine();
            Console.WriteLine("\n3) The Communication Service support accessing build requests by Pool Processes from the mother Builder process, " +
                "sending and receiving build requests from mother builder, and sending and receiving files from repo as asked by Child Builder." +
                "\n Pull Method is used for getting files from Repo");
            Console.WriteLine();
            Console.WriteLine("\n4)Provide a Repository server that supports client browsing to find files to build, builds an XML build request string and sends that and the cited files to the Build Server" +
                "\n (\"Repo Navigation Tab\" in Client GUI provides the mechanism for this. Please select the multiple files and click \"Create Request\" button to create build request.+" +
                "\n Request will be create in Repo Folder whose location is {0} " +
                "\n Select the single XML build request and  click Send Build Request to send Build Request to mother Builder)", Path.GetFullPath(RepoEnvironment.root));
            Console.WriteLine();
            Console.WriteLine("\n5)Provide a Process Pool component that creates a specified number of processes on command. ");
            Console.WriteLine("\n The \"Builder Navigation\" tab provides the mechanism for this. Please specify the number of child process in textbox and click \"Create Process\" ");
            Console.WriteLine("\n To kill all child process click \" Kill Process \" button ");
            Console.WriteLine();
            Console.WriteLine("\n6) Pool Processes uses message-passing communication to access messages from the mother Builder process.");
            Console.WriteLine();


        }

        // ----------- < req 7, 8, 9, 10, 11, 12 and 13demo > -----------
        public void Req7to13()
        {
            Console.WriteLine("\n 7) Each Pool Process attempt to build each library, cited in a retrieved build request as recieved by Mother builder, logging warnings and errors in log file." +
                "\n (Build Log File Location:- In MotherBuilder root and Repository root {Project_Name/buildLog.txt}");
            Console.WriteLine();
            Console.WriteLine("8) If the build succeeds, it sends a test request and libraries to the Test Harness for execution, and shall send the build log to the repository.+" +
                "\n Pull mechanism is used thus builder send files as request by Test Harness when it parses Test Request XML ");
            Console.WriteLine();
            Console.WriteLine("9) The Test Harness attempt to load each test library it receives and execute it. It  submit the results of testing to the Repository in log file." +
                "+\n \n (Test Log File Location:- In TestHarness root and Repository root {Project_Name/testLog.txt}");
            Console.WriteLine();
            Console.WriteLine("10) Include a Graphical User Interface, built using WPF");
            Console.WriteLine();
            Console.WriteLine("11) The GUI client is a separate process, implemented with WPF and using message-passing communication. " +
                "It provide mechanisms to get file lists from the Repository, and select files for adding to a build request structure. " +
                "It provides the capability of repeating that process to add other test libraries to the build request structure.");
            Console.WriteLine();
            Console.WriteLine("12) The client send build request structures to the repository for storage and transmission to the Build Server");
            Console.WriteLine("\n The \"Client Navigation\" tab provides the mechanism for this. Please select the file in client folder and click \"Send Request to Repo\" for sending select request XML for storage in Repo. " +
                "\n The XML request created with help of GUI-client will automatically be sent to repo for storage ");
            Console.WriteLine();
            Console.WriteLine("13) The client is  able to request the repository to send a build request in its storage to the Build Server for build processing.\n (\"Repo Navigation tab provides functionality for this.\")");
            Console.WriteLine("Select the single XML file and click \" Send Build Request\" in Repo Navigation tab");
            Console.WriteLine();
        }

        // ------------- <Helper function to display subtitle > ---------
        void displaySubtitle(string s)
        {
            Console.WriteLine(s);
            Console.WriteLine("---------------------------------------");
            Console.WriteLine();
        }

        // ------------- < Main Entry point> ---------
#if (TEST_EXECUTIVE)
        static int Main(string[] args)
        {
            Console.WriteLine(" Project 4 - Remote Build Server ");
            Console.WriteLine("================================================");
            Console.WriteLine();
            Executive exec = new Executive(); 
            exec.displaySubtitle("Demonstrating Req 1, 2, 3, 4, 5 and 6 ");
            exec.Req1to6();
            exec.displaySubtitle("Demonstrating Req 7, 8, 9, 10, 11, 12 and 13");
            exec.Req7to13();
            Console.Write("\n\n");
            Console.ReadKey();
            return 0;
        }
#endif
    } 

}
  
