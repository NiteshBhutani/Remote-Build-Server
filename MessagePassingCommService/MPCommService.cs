/////////////////////////////////////////////////////////////////////
// MPCommService.cs - service for MessagePassingComm               //
// ver 3.1                                                         //
// Jim Fawcett, CSE681-OnLine, Summer 2017                         //
/////////////////////////////////////////////////////////////////////
/*
 * Started this project with C# Console Project wizard
 * - Added references to:
 *   - System.ServiceModel
 *   - System.Runtime.Serialization
 *   - System.Threading;
 *   - System.IO;
 *   
 * Package Operations:
 * -------------------
 * This package defines three classes:
 * - Sender which implements the public methods:
 *   -------------------------------------------
 *   - Sender           : constructs sender using address string and port
 *   - connect          : opens channel and attempts to connect to an endpoint, 
 *                        trying multiple times to send a connect message
 *   - close            : closes channel
 *   - postMessage      : posts to an internal thread-safe blocking queue, which
 *                        a sendThread then dequeues msg, inspects for destination,
 *                        and calls connect(address, port)
 *   - postFile         : attempts to upload a file in blocks
 *   - close            : closes current connection
 *   - getLastError     : returns exception messages on method failure
 *
 * - Receiver which implements the public methods:
 *   ---------------------------------------------
 *   - Receiver         : constructs Receiver instance
 *   - start            : creates instance of ServiceHost which services incoming messages
 *                        using address string and port of listener
 *   - postMessage      : Sender proxies call this message to enqueue for processing
 *   - getMessage       : called by Receiver application to retrieve incoming messages
 *   - close            : closes ServiceHost
 *   - openFileForWrite : opens a file for storing incoming file blocks
 *   - writeFileBlock   : writes an incoming file block to storage
 *   - closeFile        : closes newly uploaded file
 *   - size             : returns number of messages waiting in receive queue
 *   
 * - Comm which implements, using Sender and Receiver instances, the public methods:
 *   -------------------------------------------------------------------------------
 *   - Comm             : create Comm instance with address and port
 *   - postMessage      : send CommMessage instance to a Receiver instance
 *   - getMessage       : retrieves a CommMessage from a Sender instance
 *   - postFile         : called by a Sender instance to transfer a file
 *   - close()          : stops sender and receiver threads
 *   - restart          : attempts to restart with port - that must be different from
 *                        any port previously used while the embedding process states alive
 *   - closeConnection  : closes current connection, can reopen that or another connection
 *   - size             : returns number of messages in receive queue
 *    
 * The Package also implements the class TestPCommService with public methods:
 * ---------------------------------------------------------------------------
 * - testSndrRcvr()     : test instances of Sender and Receiver
 * - testComm()         : test Comm instance
 * - compareMsgs        : compare two CommMessage instances for near equality
 * - compareFileBytes   : compare two files byte by byte
 *
 * Required Files:
 * ---------------
 * IMPCommService.cs, MPCommService.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 3.1 : 01 Dec 2017
 * - added some text to these maintenance comments
 * ver 3.0 : 26 Oct 2017
 * - Receiver receive thread processing changed to discard connect messages
 * - added close, size, and restart functions
 * - changed Sender.connect to return false instead of break on fail to connect
 * ver 2.1 : 20 Oct 2017
 * - minor changes to these comments
 * ver 2.0 : 19 Oct 2017
 * - renamed namespace and several components
 * - eliminated IPluggable.cs
 * ver 1.0 : 14 Jun 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Threading;
using System.IO;
using BuildServer;

namespace MessagePassingComm
{
  ///////////////////////////////////////////////////////////////////
  // Receiver class - receives CommMessages and Files from Senders

  public class Receiver : IMessagePassingComm
  {
    public static SWTools.BlockingQueue<CommMessage> commRcvQ { get; set; } = null;
    public static string rootLocation { get; set; } = null;
    ServiceHost commHost = null;
    FileStream fs = null;
    string lastError = "";

    /*----< constructor >------------------------------------------*/

    public Receiver()
    {
      if (commRcvQ == null)
                commRcvQ = new SWTools.BlockingQueue<CommMessage>();
      
    }
    /*----< create ServiceHost listening on specified endpoint >---*/
    /*
     * baseAddress is of the form: http://IPaddress or http://networkName
     */
    public void start(string baseAddress, int port, string location)
    {
      if (rootLocation == null)
                rootLocation = location;
      string address = baseAddress + ":" + port.ToString() + "/IMessagePassingComm";
      TestUtilities.putLine(string.Format("starting Receiver for {0} on thread {1}", baseAddress + ":" + port.ToString(),Thread.CurrentThread.ManagedThreadId));
      createCommHost(address);
    }
    /*----< create ServiceHost listening on specified endpoint >---*/
    /*
     * address is of the form: http://IPaddress:8080/IMessagePassingComm
     */
    public void createCommHost(string address)
    {
      WSHttpBinding binding = new WSHttpBinding();
      Uri baseAddress = new Uri(address);
      commHost = new ServiceHost(typeof(Receiver), baseAddress);
      commHost.AddServiceEndpoint(typeof(IMessagePassingComm), binding, baseAddress);
      commHost.Open();
    }
    /*----< enqueue a message for transmission to a Receiver >-----*/

    public void postMessage(CommMessage msg)
    {
      msg.threadId = Thread.CurrentThread.ManagedThreadId;
      //TestUtilities.putLine(string.Format("sender enqueuing message on thread {0}", Thread.CurrentThread.ManagedThreadId));
      commRcvQ.enQ(msg);
    }
    /*----< retrieve a message sent by a Sender instance >---------*/

    public CommMessage getMessage()
    {
      CommMessage msg = commRcvQ.deQ();
      if (msg.type == CommMessage.MessageType.closeReceiver)
      {
        close();
      }
      return msg;
    }
    /*----< close ServiceHost >------------------------------------*/

    public void close()
    {
      Console.Write("\n  closing receiver - please wait");
      commHost.Close();
      Console.Write("\n  commHost.Close() returned");
    }
    /*---< called by Sender's proxy to open file on Receiver >-----*/

    public bool openFileForWrite(string name)
    {
      try
      {
        string writePath = Path.Combine(rootLocation, name);
        Directory.CreateDirectory(Path.GetDirectoryName(writePath));
        fs = File.OpenWrite(writePath);
        return true;
      }
      catch(Exception ex)
      {
        lastError = ex.Message;
        Console.WriteLine("\n Error Message from openFileForWrite {0} Location {1}: " , ex.Message, Path.Combine(rootLocation, name));
        return false;
      }
    }
    /*----< write a block received from Sender instance >----------*/

    public bool writeFileBlock(byte[] block)
    {
      try
      {
        fs.Write(block, 0, block.Length);
        return true;
      }
      catch (Exception ex)
      {
        lastError = ex.Message;
        return false;
      }
    }
    /*----< close Receiver's uploaded file >-----------------------*/

    public void closeFile()
    {
      fs.Close();
    }
  }
  ///////////////////////////////////////////////////////////////////
  // Sender class - sends messages and files to Receiver

  public class Sender
  {
    private IMessagePassingComm channel;
    private ChannelFactory<IMessagePassingComm> factory = null;
    private SWTools.BlockingQueue<CommMessage> sndQ = null;
    private int port = 0;
    private string fromAddress = "";
    private string toAddress = "";
    Thread sndThread = null;
    int tryCount = 0, maxCount = 10;
    string lastError = "";
    string lastUrl = "";
    string rootLocation = "";
    int blockSize = 1024;
    /*----< constructor >------------------------------------------*/

    public Sender(string baseAddress, int listenPort, string location)
    {
      port = listenPort;
      fromAddress = baseAddress + listenPort.ToString() + "/IMessagePassingComm";
      sndQ = new SWTools.BlockingQueue<CommMessage>();
      TestUtilities.putLine(string.Format("starting Sender for {0} on thread {1}", baseAddress + ":" + port.ToString(),Thread.CurrentThread.ManagedThreadId));
      sndThread = new Thread(threadProc);
      sndThread.Start();
      rootLocation = location;
    }
    /*----< creates proxy with interface of remote instance >------*/

    public void createSendChannel(string address)
    {
      EndpointAddress baseAddress = new EndpointAddress(address);
      WSHttpBinding binding = new WSHttpBinding();
      factory = new ChannelFactory<IMessagePassingComm>(binding, address);
      channel = factory.CreateChannel();
    }
    /*----< attempts to connect to Receiver instance >-------------*/

    public bool connect(string baseAddress, int port)
    {
      toAddress = baseAddress + ":" + port.ToString() + "/IMessagePassingComm";
      return connect(toAddress);
    }
    /*----< attempts to connect to Receiver instance >-------------*/
    /*
     * - attempts a finite number of times to connect to a Receiver
     * - first attempt to send will throw exception of no listener
     *   at the specified endpoint
     * - to test that we attempt to send a connect message
     */
    public bool connect(string toAddress)
    {
      int timeToSleep = 500;  
      createSendChannel(toAddress);
      CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect);
      while (true)
      {
        try
        {
          channel.postMessage(connectMsg);
          tryCount = 0;
          return true;
        }
        catch (Exception ex)
        {
          if (++tryCount < maxCount)
          {
            TestUtilities.putLine("failed to connect - waiting to try again");
            Thread.Sleep(timeToSleep);
          }
          else
          {
            TestUtilities.putLine("failed to connect - quitting");
            lastError = ex.Message;
            return false;
          }
        }
      }
    }
    /*----< closes Sender's proxy >--------------------------------*/

    public void close()
    {
      if(factory != null)
        factory.Close();
    }
    /*----< processing for send thread >--------------------------*/
    /*
     * - send thread dequeues send message and posts to channel proxy
     * - thread inspects message and routes to appropriate specified endpoint
     */
    void threadProc()
    {
      while(true)
      {
        //TestUtilities.putLine(string.Format("sender enqueuing message on thread {0}", Thread.CurrentThread.ManagedThreadId));

        CommMessage msg = sndQ.deQ();
        if (msg.type == CommMessage.MessageType.closeSender)
        {
          TestUtilities.putLine("Sender send thread quitting");
          break;
        }
        if (msg.to == lastUrl)
        {
          channel.postMessage(msg);
        }
        else
        {
          close();
          if (!connect(msg.to))
            return;
          lastUrl = msg.to;
          channel.postMessage(msg);
        }
      }
    }
    /*----< main thread enqueues message for sending >-------------*/

    public void postMessage(CommMessage msg)
    {
      sndQ.enQ(msg);
    }
    /*----< uploads file to Receiver instance >--------------------*/

    public bool postFile(string fileName)
    {
      FileStream fs = null;
      long bytesRemaining;

      try
      {
        string path = Path.Combine(rootLocation, fileName);
        fs = File.OpenRead(path);
        bytesRemaining = fs.Length;
        channel.openFileForWrite(fileName);
        while (true)
        {
          long bytesToRead = Math.Min(blockSize, bytesRemaining);
          byte[] blk = new byte[bytesToRead];
          long numBytesRead = fs.Read(blk, 0, (int)bytesToRead);
          bytesRemaining -= numBytesRead;

          channel.writeFileBlock(blk);
          if (bytesRemaining <= 0)
            break;
        }
        channel.closeFile();
        fs.Close();
      }
      catch (Exception ex)
      {
        lastError = ex.Message;
        return false;
      }
      return true;
    }
  }
  ///////////////////////////////////////////////////////////////////
  // Comm class combines Receiver and Sender

  public class Comm
  {
    private Receiver rcvr = null;
    private Sender sndr = null;
    private string address = null;
    private int portNum = 0;
    private string rootLocation = null;
    /*----< constructor >------------------------------------------*/
    /*
     * - starts listener listening on specified endpoint
     */
    public Comm(string baseAddress, int port, string location)
    {
      address = baseAddress;
      portNum = port;
      rootLocation = location;
      rcvr = new Receiver();
      rcvr.start(baseAddress, port, location);
      sndr = new Sender(baseAddress, port, location);
    }

    public void close()
    {
      Console.Write("\n  Comm closing");
      rcvr.close();
      sndr.close();
    }
    /*----< post message to remote Comm >--------------------------*/

    public void postMessage(CommMessage msg)
    {
      sndr.postMessage(msg);
    }
    /*----< retrieve message from remote Comm >--------------------*/

    public CommMessage getMessage()
    {
      return rcvr.getMessage();
    }
    /*----< called by remote Comm to upload file >-----------------*/

    public bool postFile(string filename)
    {
      return sndr.postFile(filename);
    }
  }
  ///////////////////////////////////////////////////////////////////
  // TestPCommService class - tests Receiver, Sender, and Comm

  class TestPCommService
  {
    /*----< compare CommMessages property by property >------------*/
    /*
     * - skips threadId property
     */
    public static bool compareMsgs(CommMessage msg1, CommMessage msg2)
    {
      bool t1 = (msg1.type == msg2.type);
      bool t2 = (msg1.to == msg2.to);
      bool t3 = (msg1.from == msg2.from);
      bool t4 = (msg1.author == msg2.author);
      bool t5 = (msg1.command == msg2.command);
      //bool t6 = (msg1.threadId == msg2.threadId);
      bool t7 = (msg1.errorMsg == msg2.errorMsg);
      if (msg1.arguments.Count != msg2.arguments.Count)
        return false;
      for(int i=0; i<msg1.arguments.Count; ++i)
      {
        if (msg1.arguments[i] != msg2.arguments[i])
          return false;
      }
      return t1 && t2 && t3 && t4 && t5 && /*t6 &&*/ t7;
    }
    /*----< compare binary file's bytes >--------------------------*/

    /*----< test Comm instance >-----------------------------------*/

    public static bool testComm()
    {
      TestUtilities.title("testing Comm");
      bool test = true;

      Comm comm = new Comm("http://localhost", 8081, "../../clientFolder");
      CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);

      csndMsg.command = "show";
      csndMsg.author = "Jim Fawcett";
      csndMsg.to = "http://localhost:8081/IMessagePassingComm";
      csndMsg.from = "http://localhost:8081/IMessagePassingComm";

      comm.postMessage(csndMsg);
      CommMessage crcvMsg = comm.getMessage();
      //if (localEnvironment.verbose)
      crcvMsg.show();

      crcvMsg = comm.getMessage();
      //if (localEnvironment.verbose)
      crcvMsg.show();
      if (!compareMsgs(csndMsg, crcvMsg))
        test = false;
      TestUtilities.checkResult(test, "csndMsg equals crcvMsg");
      TestUtilities.putLine();

      TestUtilities.title("testing file transfer");

      
      TestUtilities.title("test receiver close");
      csndMsg.type = CommMessage.MessageType.closeReceiver;
      comm.postMessage(csndMsg);
      crcvMsg = comm.getMessage();
      //if (localEnvironment.verbose)
      crcvMsg.show();
      //if (!compareMsgs(csndMsg, crcvMsg))
      test = false;
      TestUtilities.checkResult(test, "closeReceiver");
      TestUtilities.putLine();

      csndMsg.type = CommMessage.MessageType.closeSender;
      comm.postMessage(csndMsg);
      //if(localEnvironment.verbose)
      csndMsg.show();
      // comm.getMessage() would fail because server has shut down
      // no rcvMsg so no compare

      TestUtilities.putLine("last message received\n");

      return test ;
    }
    /*----< do the tests >-----------------------------------------*/

    static void Main(string[] args)
    {
      //localEnvironment.verbose = true;
      TestUtilities.title("testing Message-Passing Communication", '=');

      /*----< uncomment to see Sender & Receiver testing >---------*/
      //TestUtilities.checkResult(testSndrRcvr(), "Sender & Receiver");
      //TestUtilities.putLine();

      TestUtilities.checkResult(testComm(), "Comm");
      TestUtilities.putLine();

      TestUtilities.putLine("Press key to quit\n");
      //if(localEnvironment.verbose)
      Console.ReadKey();
    }
  }
}
