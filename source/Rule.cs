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

// (One) definition of a non-terminal.
internal sealed class Rule
{
	public Rule(string name, Expression expr, string pass, string fail, int line)
	{
		Name = name;
		Expression = expr;
		PassAction = pass;
		FailAction = fail;
		Line = line;
	}
	
	public string Name {get; private set;}
	
	public Expression Expression {get; private set;}
	
	// May be null.
	public string PassAction {get; private set;}
	
	// May be null.
	public string FailAction {get; private set;}
	
	// Returns null if there is no hook.
	public List<string> GetHook(Hook hook)
	{
		List<string> code = null;
		
		if (m_hooks != null)
			Unused.Value = m_hooks.TryGetValue(hook, out code);
		
		return code;
	}
	
	public int Line {get; private set;}
	
	public override string ToString()
	{
		return string.Format("{0} := {1}", Name, Expression);
	}
	
	internal void AddHook(Hook hook, List<string> code)
	{
		if (m_hooks == null)
			m_hooks = new Dictionary<Hook, List<string>>();
			
		m_hooks.Add(hook, code);
	}
	
	#region Fields
	private Dictionary<Hook, List<string>> m_hooks;
	#endregion
}
