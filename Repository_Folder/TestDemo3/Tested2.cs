/////////////////////////////////////////////////////////////////////
// Tested2.cs - demonstration production code                      //
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
  public class Tested2
  {
    public string truncate(string inString, int n)
    {
      if (0 <= n && n < inString.Count())
        return inString.Substring(0, n);
      else
        return inString;
    }
    public string expand(string inString, int n)
    {
      int m = Math.Abs(n);
      if (m < inString.Count())
        return truncate(inString, m);
      string expansion = new string('.', m - inString.Count());
      if (0 < n)
      {
        return inString + expansion;
      }
      else
      {
        return expansion + inString;
      }
    }
  }
}
