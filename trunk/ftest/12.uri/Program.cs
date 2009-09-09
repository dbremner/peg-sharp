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
		var parser = new FTest12.Test12();
		DoGood1(parser);
		DoBad1(parser);
	}
	
	#region Private Methods
	private static void DoGood(FTest12.Test12 parser, string uri)
	{
		int count = parser.Parse(uri);
		if (count != uri.Length)
			throw new Exception(string.Format("Only matched '{0}' from '{1}'.", uri.Substring(0, count), uri));
	}
	
	private static void DoBad(FTest12.Test12 parser, string uri)
	{
		try
		{
			int count = parser.Parse(uri);
			if (count == uri.Length)
				throw new Exception(string.Format("Matched all of '{0}'.", uri));
		}
		catch (FTest12.ParserException)
		{
		}
	}
	
	private static void DoGood1(FTest12.Test12 parser)
	{
		DoGood(parser, "A://");		// simplest legal uri with a host name
		DoGood(parser, "http://server");
		DoGood(parser, "http://server/foo/bar");
		DoGood(parser, "http://www.mysite.com/home/file.txt");
		DoGood(parser, "http://server:4000/foo/bar");
		DoGood(parser, "http://me@server:4000/foo/bar");
		DoGood(parser, "http://www.mysite.com/home/my%20file.txt");
		DoGood(parser, "http://server:4000/foo/b'ar");
	}
	
	private static void DoBad1(FTest12.Test12 parser)
	{
		DoBad(parser, "x");
		DoBad(parser, "A:/");
		DoBad(parser, "http://server|foo");
		DoBad(parser, "http://server/foo bar");
		DoBad(parser, "://www.mysite.com/home/file.txt");
		DoBad(parser, "//server:4000/foo/bar");
		DoBad(parser, "www.mysite.com/home/file.txt");
	}
	#endregion
}
