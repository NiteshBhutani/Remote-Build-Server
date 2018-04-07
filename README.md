# Remote-Build-Server

Build Server – an automated tool to build test libraries. This project implements buld infrastructure for C# and have communication channel for communicating with Repository and Test Harness built using WCF. As the, test requests and code are submitted by the Repository to the Build Server. The Build Server will build the libraries needed for each test request, and will submit the request and libraries to the Test Harness, where they are executed for Test Automation.

Activity Diagram :-

![buildserver](https://user-images.githubusercontent.com/16679348/38453723-562e9c24-3a28-11e8-8dbb-2ef70a1bc6ee.PNG)

