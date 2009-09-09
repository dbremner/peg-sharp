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

internal static class Program
{
	public static void Main(string[] args)
	{
		var parser = new Test13();
		DoGood1(parser);
	}
	
	#region Private Methods
	private static void DoCheck(Test13 parser, string input, string expected)
	{
		parser.Parse(input);
		if (parser.Unconsumed != expected)
			Console.Error.WriteLine("Expected unconsumed '{0}' but got '{1}'.", expected, parser.Unconsumed);
	}
	
	private static void DoGood1(Test13 parser)
	{
		DoCheck(parser, "x", "");
		DoCheck(parser, "xxxx", "");
		DoCheck(parser, "xxyy", "yy");
	}
	#endregion
}
