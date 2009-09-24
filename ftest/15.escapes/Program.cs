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
		var parser = new Test15();
		DoGood1(parser);
		DoBad1(parser);
		DoBad2(parser);
	}
	
	#region Private Methods
	private static void DoGood1(Test15 parser)
	{
		parser.Parse("ax");
		parser.Parse("a\n");
		
		parser.Parse("bA");
		
		parser.Parse("c");
		parser.Parse("B");
		
		parser.Parse("d");
		parser.Parse("-");
		
		parser.Parse("z");
		parser.Parse("CF");
	}
	
	private static void DoBad1(Test15 parser)
	{
		try
		{
			parser.Parse("y", "DoBad1");
			Console.Error.WriteLine("Expected a parse failure.");
		}
		catch (ParserException e)
		{
			if (e.Message != "Expected a or bA or [cB] or [d\\cPd] or z or CF at line 1 col 1 in DoBad1")
				Console.Error.WriteLine("Expected 'Expected a or bA or [cB] or [d\\cPd] or z or CF at line 1 col 1 in DoBad1', but got '{0}'", e.Message);
		}
	}
	
	private static void DoBad2(Test15 parser)
	{
		try
		{
			parser.Parse("a");
			Console.Error.WriteLine("Expected a parse failure.");
		}
		catch (ParserException e)
		{
			if (e.Message != "Expected . at line 1 col 2.")
				Console.Error.WriteLine("Expected 'Expected . at line 1 col 2.', but got '{0}'", e.Message);
		}
	}
	#endregion
}
