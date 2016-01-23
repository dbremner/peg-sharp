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
using System.Diagnostics.Contracts;

// Contains the settings and other variables used to evaluate template predicates.
internal sealed class Context
{
	public void AddVariable(string name, object value)
	{
		m_variables.Add(name, value);
	}
	
	public void SetVariable(string name, object value)
	{
		m_variables[name] = value;
	}
	
	public void AddExcluded(string name)
	{
		m_excluded.Add(name);
	}
	
	public bool IsExcluded(string name)
	{
		Contract.Requires(name != null);
		
		return m_excluded.Contains(name);
	}
	
	public object Dereference(string name)
	{
		object result;
		if (m_variables.TryGetValue(name, out result))
			return result;
		else
			throw new Exception(name + " isn't a known variable");
	}
	
	#region Fields
	private readonly HashSet<string> m_excluded = new HashSet<string>();
	private readonly Dictionary<string, object> m_variables = new Dictionary<string, object>();
	#endregion
}
