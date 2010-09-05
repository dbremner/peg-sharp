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
using System.Collections.Generic;
using System.Linq;

// In-lines and otherwise simplifies rules. This is largely based on Bryan Ford's
// MIT master's thesis: Packrat Parsing: a Practical Linear-Time Algorithm with Backtracking.
internal sealed class Optimizer
{
	public Optimizer(List<Rule> rules)
	{
		m_rules = rules;
	}
	
	public void Optimize()
	{
		if (Program.Verbosity >= 3)
			DoDump("before optimization:");
		
		bool optimized = false;
		while (true)
		{
			int oldSize = DoGetSize();
			DoOptimize("InlineTiny", this.DoInlineTiny);
			int newSize = DoGetSize();
			
			if (newSize == oldSize)
				break;
			else
				optimized = true;
		}
		
		if (Program.Verbosity >= 3 && optimized)
			DoDump("after optimization:");
	}
	
	#region Private Methods
	private void DoOptimize(string name, Action action)
	{
		int oldSize = DoGetSize();
		action();
		int newSize = DoGetSize();
		Contract.Assert(newSize <= oldSize);
		
		if (Program.Verbosity >= 2 && newSize < oldSize)
			Console.WriteLine("{0} reduced by {1}", name, oldSize - newSize);
		else if (Program.Verbosity >= 3 && newSize == oldSize)
			Console.WriteLine("{0} did nothing", name);
	}
	
	// Inline rules that have no more than two operators (and no semantic actions).
	private void DoInlineTiny()
	{
	}
	
	private void DoDump(string header = null)
	{
		if (header != null)
			Console.WriteLine("{0} (size = {1}", header, DoGetSize());
			
		foreach (Rule rule in m_rules)
		{
			Console.WriteLine("   {0}", rule);
		}
	}
	
	private int DoGetSize()
	{
		return m_rules.Sum(r => r.Expression.GetSize());
	}
	#endregion
	
	#region Fields
	private List<Rule> m_rules = new List<Rule>();
	#endregion
}
