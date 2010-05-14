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
		var parser = new Test1();
		DoGood1(parser);
		DoBad1(parser);
	}
	
	#region Private Methods
	// We don't have any semantic actions so all we can check is parsed or not.
	private static void DoGood1(Test1 parser)
	{
		parser.Parse("x");
		parser.Parse("\\");
		parser.Parse("\"C\"");
		parser.Parse("'D'");
	}
	
	private static void DoBad1(Test1 parser)
	{
		try
		{
			parser.Parse("y");
			Console.Error.WriteLine("Expected a parse failure.");
		}
		catch (ParserException e)
		{
			if (e.Message != "Expected x or \\ or \"C\" or 'D' at line 1 col 2.")
				Console.Error.WriteLine("Expected 'Expected x or \\ or \"C\" or 'D' at line 1 col 2.', but got '{0}'", e.Message);
		}
	}
	#endregion
}
