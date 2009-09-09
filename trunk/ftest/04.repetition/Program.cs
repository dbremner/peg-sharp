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
		var parser = new Test4();
		DoGood1(parser);
		DoBad1(parser);
		DoBad2(parser);
	}
	
	#region Private Methods
	// We don't have any semantic actions so all we can check is parsed or not.
	private static void DoGood1(Test4 parser)
	{
		parser.Parse("");
		parser.Parse("x");
		parser.Parse("xxxx");
	}
	
	private static void DoBad1(Test4 parser)
	{
		try
		{
			parser.Parse("z");
			Console.Error.WriteLine("Expected a parse failure.");
		}
		catch (ParserException e)
		{
			if (e.Message != "Not all input was consumed starting from 'z' at line 1 col 1.")
				Console.Error.WriteLine("Expected 'Not all input was consumed starting from 'z' at line 1 col 1.', but got '{0}'", e.Message);
		}
	}
	
	private static void DoBad2(Test4 parser)
	{
		try
		{
			parser.Parse("xxxy");
			Console.Error.WriteLine("Expected a parse failure.");
		}
		catch (ParserException e)
		{
			if (e.Message != "Not all input was consumed starting from 'y' at line 1 col 4.")
				Console.Error.WriteLine("Expected 'Not all input was consumed starting from 'y' at line 1 col 4.', but got '{0}'", e.Message);
		}
	}
	#endregion
}
