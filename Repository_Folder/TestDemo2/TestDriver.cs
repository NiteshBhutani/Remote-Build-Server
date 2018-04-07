/////////////////////////////////////////////////////////////////////
// TestDriver.cs - demonstration test driver                       //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 //
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSTestDemo
{
  public class TestDriver : ITest
  {
    bool testTested1() //This will pass
    {
      bool result = true;
      Tested1 td1 = new Tested1();
      string value = td1.vowels();
      Console.Write("\n  td1.vowels() returned {0}", value);
      if (value != "aeiou")
      {
        result = false;
      }
      return result;
    }
    bool testTested2()
    {
      bool result = true;
      Tested2 td2 = new Tested2();
      string value = td2.truncate("123456", 3);
      Console.Write("\n  td2.truncate(\"123456\", 3) returned {0}", value);
      if (value != "123")
        result = false;
      value = td2.expand("123", 6);
      Console.Write("\n  td2.expand(\"123\", 6) returned {0}", value);
      if (value != "123...")
        result = false;
      value = td2.expand("123", 2);
      Console.Write("\n  td2.expand(\"123\", 2) returned {0}", value);
      if (value != "12")
        result = false;
      value = td2.expand("123", -2);
      Console.Write("\n  td2.expand(\"123\", -2) returned {0}", value);
      if (value != "12")
        result = false;
      value = td2.expand("123", -6);
      Console.Write("\n  td2.expand(\"123\", -6) returned {0}", value);
      if (value != "...123")
        result = false;
      return result;
    }
    public bool test()
    {
      bool result1 = testTested1();
      bool result2 = testTested2();
      return result1 && result2;
    }
  }
}
