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
		var parser = new Test16();
		DoGood1(parser);
	}
	
	#region Private Methods
	private static double DoEvaluate(Node node)
	{
		double result;
		
		switch (node.Name)
		{
			case "Expr":
				result = DoEvaluate(node.Children[0]);
				break;
			
			case "Sum":
				result = DoEvaluate(node.Children[0]);
				for (int i = 1; i < node.Children.Length; i += 2)
				{
					if (node.Children[i].Text == "+")
						result += DoEvaluate(node.Children[i + 1]);
					else
						result -= DoEvaluate(node.Children[i + 1]);
				}
				break;
			
			case "Product":
				result = DoEvaluate(node.Children[0]);
				for (int i = 1; i < node.Children.Length; i += 2)
				{
					if (node.Children[i].Text == "*")
						result *= DoEvaluate(node.Children[i + 1]);
					else
						result /= DoEvaluate(node.Children[i + 1]);
				}
				break;
			
			case "Value":
				if (node.Children[0].Text == "(")
					result = DoEvaluate(node.Children[1]);
				else
					result = double.Parse(node.Text.Trim());
				break;
			
			default:
				throw new Exception("Unexpected node: " + node);
		}
		
		return result;
	}
	
	private static void DoCheck(Test16 parser, string expr, double expected)
	{
		Node node = parser.Parse(expr);
		double actual = DoEvaluate(node);
		if (Math.Abs(expected - actual) > 0.01)
			throw new Exception(string.Format("Expected {0} for \"{1}\" but got {2}", expected, expr, actual));
	}
	
	private static void DoGood1(Test16 parser)
	{
		DoCheck(parser, "5", 5.0);
		DoCheck(parser, "5", 5.0);
		DoCheck(parser, "100", 100.0);
		DoCheck(parser, "2 + 3", 5.0);
		DoCheck(parser, "2 + 3 * 5", 17.0);
		DoCheck(parser, "(2 + 3) * 5", 25.0);
		DoCheck(parser, "2 + 3 -   1", 4.0);
	}
	#endregion
}
