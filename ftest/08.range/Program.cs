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
		var parser = new Test8();
		DoGood1(parser);
		DoBad1(parser);
	}
	
	#region Private Methods
	// We don't have any semantic actions so all we can check is parsed or not.
	private static void DoGood1(Test8 parser)
	{
		parser.Parse("a");
		parser.Parse("b");
		parser.Parse("c");

		parser.Parse("e");
		parser.Parse("f");
		parser.Parse("g");
		parser.Parse("h");

		parser.Parse("m");
		parser.Parse("n");
		parser.Parse("p");
		parser.Parse("q");
		parser.Parse("u");
		parser.Parse("w");
		parser.Parse("z");
		
		parser.Parse("A");
		parser.Parse("D");
		parser.Parse("*");
	}
	
	private static void DoBad(Test8 parser, string input)
	{
		try
		{
			parser.Parse(input);
			Console.Error.WriteLine("Expected a parse failure for: " + input);
		}
		catch (ParserException e)
		{
			if (!e.Message.StartsWith("Expected [abc] or [e-h] or [mnzp-ru-w] or [^\\n\\r\\]a-z]"))
				Console.Error.WriteLine("Expected 'Expected [abc] or [e-h] or [mnzp-ru-w] or [^\\n\\r\\]a-z].', but got '{0}'", e.Message);
		}
	}
	
	private static void DoBad1(Test8 parser)
	{
		DoBad(parser, "d");
		DoBad(parser, "i");
		DoBad(parser, "j");
		DoBad(parser, "o");
		DoBad(parser, "y");
		DoBad(parser, "\n");
		DoBad(parser, "]");
	}
	#endregion
}
