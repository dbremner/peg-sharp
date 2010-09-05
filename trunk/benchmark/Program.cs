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
	public static int Main(string[] args)
	{
		int result = 0;
		
		if (args.Length == 1)
		{
			string contents = System.IO.File.ReadAllText(args[0]);
			result = DoParse(contents);
		}
		else
		{
			Console.Error.WriteLine("Expected the name of a file to parse.");
			result = 1;
		}
		
		return result;
	}
	
	#region Private Methods
	private static int DoParse(string contents)
	{
		// All that we care about is the speed of the parser so we don't
		// actually evaluate the file.
		int result = 0;
		try
		{
			var parser = new Benchmark();
			parser.Parse(contents);
		}
		catch (ParserException e)
		{
			Console.Error.WriteLine(e.Message);
			result = 2;
		}
		
		return result;
	}
	#endregion
}
