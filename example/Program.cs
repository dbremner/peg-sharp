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
		while (true)
		{
			Console.Write("> ");
			string line = Console.ReadLine();
			string lower = line.ToLower();
			
			if (line == "?" || lower == "help")
				DoHelp();
			
			else if (lower == "q" || lower == "quit")
				break;
			
			else if (line.Trim().Length > 0)
				DoEval(line);
		}
	}
	
	public static long OutBase
	{
		get
		{
			Expression result;
			if (!ms_context.TryGetValue("$base", out result))
				return 10;
			
			result = result.Evaluate(ms_context);
			if (result is IntegerExpression)
				return ((IntegerExpression) result).Value;
				
			throw new InvalidOperationException("$base should be an integer valued expression, not " + result + ".");
		}
	}
	
	public static Expression LastValue
	{
		get
		{
			if (ms_last == null)
				throw new InvalidOperationException("There is no last expression.");
			
			return ms_last;
		}
	}
	
	#region Private Methods
	private static void DoHelp()
	{
		Console.WriteLine("Type an expression to evaulate it, e.g. `4 * (2 + x)`.");
		Console.WriteLine("Type `name = expression` to set a variable.");
		Console.WriteLine("Type `$base = 16` to change the integer output base to hex.");
		Console.WriteLine("Type `$last` to get the value of the previous expression.");
		Console.WriteLine("Type `q` or `quit` to exit.");
	}
	
	private static void DoEval(string input)
	{
		try
		{
			Expression expr = ms_parser.Parse(input);
			ms_last = expr.Evaluate(ms_context);
			Console.WriteLine(ms_last);
		}
		catch (System.Reflection.TargetInvocationException t)
		{
			Console.WriteLine(t.InnerException.Message);
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
		}
	}
	#endregion
	
	#region Fields
	private static Parser ms_parser = new Parser();
	private static Dictionary<string, Expression> ms_context = new Dictionary<string, Expression>();
	private static Expression ms_last;
	#endregion
}
