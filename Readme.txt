<<<<<<<<<<<Project #4 - Remote Build Server Prototypes>>>>>>>>>>>>>>



Note - All the files ( compile.bat, run.bat, ) are present in Project Root/Home Location.

Each Server/Package has it own storage path which is kept Environment.cs.
Storage Path for Client = {Project Root Directory}/Client_folder
Storage Path for Repository = {Project Root Directory}/Repository_Folder
Storage Path for Build = {Project Root Directory}/Build_folder
Storage Path for TestHarness = {Project Root Directory}/TestHarness_folder
------------------------------------------------------------------------------------------------------

"Repository_folder" folder is the folder in which all the source code files is kept.
"Build_Folder" .dll files after build and build logs . 
"TestHarness_folder" contains .dll for test harness to load and execute and contains test logs.

---------------------------------------------------------------------------------------------------------

Build logs- {Build_Folder}/{Project_Name}/builLogs.txt or they are also sent back to {Repository_folder}/{Project_Name}/builLogs.txt
Test logs- {TestHarness_folder}/{Project_Name}/testLogs.txt or they are also sent back to {Repository_folder}/{Project_Name}/testLogs.txt

----------------------------------------------------------------------------------------------------------------------------------

As Demo, Build Request(BuildRequest1.xml) is sent which contain project/TestDriver. 
Please see Client-GUI for detailed messages :-
1) TestDemo - Will build successfully and pass all test.
2) TestDemo2 - Will Fail the build so will not be executed by Test harness
3) TestDemo3- Will pass the build but will fail the Test in Test Harness

------------------------------------------------------------------------------------------------------------------------------------

Status of Build and Test can be seen in Status bar of Client GUI 
Please select only one XML/Build Request File in Repo Navigation while sending build request to mother builder by clicking "Send Build Request"
------------------------------------------------------------------------------------------------------

To Compile the program :- 

1) Go to Project Home folder(where "BuildServer.sln" is present)
2) Run "devenv "BuildServer.sln" /rebuild debug" -> This will create Executive.exe "Executive/bin/Debug" Folder 

To Run the program :-

1) Go to Project Home folder(where CodeAnalysis.sln is present) and 
2) and run 
------------------------
cd "Executive/bin/Debug"
start Executive.exe 
cd ../../../
cd "Builder/bin/Debug"
start Builder.exe
cd ../../../
cd "TestHarness/bin/Debug"
start TestHarness.exe
cd ../../../
cd "Repo/bin/Debug"
start Repo.exe
cd ../../../
cd "ClientGUI/bin/Debug"
start ClientGUI.exe
---------------------------

We need to start 5 projects Builder, ClientGUI, Executive, Repo and Test Harness



