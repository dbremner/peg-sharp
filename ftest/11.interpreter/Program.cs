// Copyright (C) 2009 Jesse Jones
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
using System.Collections.Generic;

internal static class Program
{
	public static void Main(string[] args)
	{
		var parser = new Test11();
		DoGood1(parser);
		DoBad1(parser);
	}
	
	#region Private Methods
	private static void DoCheck(Test11 parser, string expr, int expected)
	{
		var context = new Dictionary<string, int>();
		Expression e = parser.Parse(expr);
//		Console.WriteLine(e);
		int actual = e.Evaluate(context);
		if (expected != actual)
			throw new Exception(string.Format("Expected {0} for \"{1}\" but got {2}", expected, expr, actual));
	}
	
	private static void DoGood1(Test11 parser)
	{
		DoCheck(parser, "5", 5);
		DoCheck(parser, "5 + 2", 7);
		DoCheck(parser, "5 + 2*6", 17);
		DoCheck(parser, "(5 + 2)*6", 42);
		DoCheck(parser, @"
x = 10
y = 3
2 + x - y
", 9);
		DoCheck(parser, @"
x = 10
if x > 2 tHeN 
	y = 2
else
	y = 100
end
2 + x - y
", 10);
		DoCheck(parser, @"
x = 4
result = 1
while x > 1 do
	result = result*x
	x = x - 1
end
result
", 4*3*2*1);
		DoCheck(parser, @"
doX = 4
result = 1
while doX > 1 do
	result = result*doX
	doX = doX - 1
end
result
", 4*3*2*1);
	}
	
	private static void DoBad1(Test11 parser)
	{
		try
		{
			parser.Parse("1235678912356789123567891235678912356789");
			throw new Exception("Expected a ParserException");
		}
		catch (ParserException e)
		{
			if (e.Message != "Value is oor at line 1 col 1.")
				throw new Exception(string.Format("Expected 'Value is oor at line 1 col 1.' but got '{0}'.", e.Message));
		}
	}
	#endregion
}
