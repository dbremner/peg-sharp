// Copyright (C) 2010 Jesse Jones
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;

internal static class Program
{
	public static void Main(string[] args)
	{
		var parser = new Test18();
		DoTrivial(parser);
		DoIf(parser);
		DoTwoIfs(parser);
	}
	
	#region Private Methods
	private static void DoTrivial(Test18 parser)
	{
		string input = @"def Alpha:
    pass";
		string expected = @"def Alpha:
   pass";
		
		DoCheck(parser, input, expected);
	}
	
	private static void DoIf(Test18 parser)
	{
		string input = @"def Alpha:
    if beta:
        pass";
		string expected = @"def Alpha:
   if beta:
      pass";
		
		DoCheck(parser, input, expected);
	}
	
	private static void DoTwoIfs(Test18 parser)
	{
		string input = @"def Alpha:
    if beta:
        pass
    if gamma:
        pass";
		string expected = @"def Alpha:
   if beta:
      pass
   if gamma:
      pass";
		
		DoCheck(parser, input, expected);
	}
	
	private static void DoCheck(Test18 parser, string input, string expected)
	{
		string actual = parser.Parse(input).ToText().Trim();
		expected = expected.Trim();
		
		if (actual != expected)
		{
			Console.Error.WriteLine("Expected:");
			Console.Error.WriteLine(expected);
			
			Console.Error.WriteLine("Actual:");
			Console.Error.WriteLine(actual);
			
			throw new Exception("failed");
		}
	}
	#endregion
}
