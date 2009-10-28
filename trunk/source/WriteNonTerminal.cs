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

internal sealed partial class Writer
{
	private void DoWriteNonTerminal(string methodName, Rule rule, int i, int maxIndex)
	{
		if (m_grammar.Settings["exclude-methods"].Contains(methodName + ' '))
			return;
			
		string debugName = rule.Name;
		if (maxIndex > 1)
			debugName += i + 1;
			
		string prolog = string.Format("{0} := {1}", rule.Name, rule.Expression);
		DoWriteLine("// " + prolog);
		DoWriteLine("private State " + methodName + "(State _state, List<Result> _outResults)");
		DoWriteLine("{");
		if (m_debug.Length > 0 && (m_debug[0] == "*" || Array.IndexOf(m_debug, rule.Name) >= 0))
		{
			if (m_grammar.Settings["debug-file"].Length > 0)
			{
				DoWriteLine("	if (m_file == m_debugFile)");
				DoWriteLine("	{");
				DoWriteLine("		DoDebugWrite(\"" + debugName + "\");");
				DoWriteLine("		++m_debugLevel;");
				DoWriteLine("	}");
			}
			else
			{
				DoWriteLine("	DoDebugWrite(\"" + debugName + "\");");
				DoWriteLine("	++m_debugLevel;");
			}
			DoWriteLine("	");
		}
		DoWriteLine("	State _start = _state;");
		DoWriteLine("	List<Result> results = new List<Result>();");
		DoWriteLine("	");
		DoWriteNonTerminalRule(rule);
		DoWriteLine("	");
		DoWriteLine("	if (_state.Parsed)");
		DoWriteLine("	{");
		if (m_grammar.Settings["value"] == "XmlNode")
		{
			DoWriteLine("		XmlElement _node = DoCreateElementNode(\"{0}\", _start.Index, _state.Index - _start.Index, DoGetLine(_start.Index), DoGetCol(_start.Index), (from r in results where r.Value != null select r.Value).ToArray());", rule.Name);
			if (maxIndex > 1)
				DoWriteLine("		_node.SetAttribute(\"alternative\", \"{0}\");", i + 1);
			DoWriteLine("		{0} value = _node;", m_grammar.Settings["value"]);
		}
		else if (m_grammar.Settings["value"] != "void")
			DoWriteLine("		{0} value = results.Count > 0 ? results[0].Value : default({0});", m_grammar.Settings["value"]);
		if (rule.PassAction != null)
		{
			if (DoReferencesLocal(rule.PassAction, "fatal"))
				DoWriteLine("		string fatal = null;");
			if (DoReferencesLocal(rule.PassAction, "text"))
				DoWriteLine("		string text = m_input.Substring(_start.Index, _state.Index - _start.Index);");
			
			string trailer = string.Empty;
			if (rule.PassAction[rule.PassAction.Length - 1] != ';' && rule.PassAction[rule.PassAction.Length - 1] != '}')
				trailer = ";";
			DoWriteLine("		" + rule.PassAction + trailer);
			
			if (DoReferencesLocal(rule.PassAction, "fatal"))
			{
				DoWriteLine("		if (!string.IsNullOrEmpty(fatal))");
				DoWriteLine("			DoThrow(_start.Index, fatal);");
			}
			
			if (DoReferencesLocal(rule.PassAction, "text"))
			{
				DoWriteLine("		if (text != null)");
				if (m_grammar.Settings["value"] != "void")
					DoWriteLine("			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));");
				else
					DoWriteLine("			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input));");
			}
			else
			{
				if (m_grammar.Settings["value"] != "void")
					DoWriteLine("		_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));");
				else
					DoWriteLine("		_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input));");
			}
		}
		else
		{
			if (m_grammar.Settings["value"] != "void")
				DoWriteLine("		_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));");
			else
				DoWriteLine("		_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input));");
		}
		DoWriteLine("	}");
		if (rule.FailAction != null && DoReferencesLocal(rule.FailAction, "expected"))
		{
			DoWriteLine("	else");
			DoWriteLine("	{");
			DoWriteLine("		string expected = null;");
			
			string trailer = string.Empty;
			if (rule.FailAction[rule.FailAction.Length - 1] != ';' && rule.FailAction[rule.FailAction.Length - 1] != '}')
				trailer = ";";
			DoWriteLine("		" + rule.FailAction + trailer);
			
			DoWriteLine("		if (expected != null)");
			DoWriteLine("			_state = new State(_start.Index, false, ErrorSet.Combine(_start.Errors, new ErrorSet(_state.Errors.Index, expected)));");
			DoWriteLine("	}");
		}
		if (m_debug.Length > 0 && (m_debug[0] == "*" || Array.IndexOf(m_debug, rule.Name) >= 0))
		{
			DoWriteLine("	");
			if (m_grammar.Settings["debug-file"].Length > 0)
			{
				DoWriteLine("	if (m_file == m_debugFile)");
				DoWriteLine("	{");
				DoWriteLine("		if (_state.Parsed)");
				DoWriteLine("			DoDebugWrite(\"" + debugName + " parsed: {0}\", DoTruncateString(m_input.Substring(_start.Index, _state.Index - _start.Index)));");
				DoWriteLine("		else");
				DoWriteLine("			DoDebugWrite(\"" + debugName + " failed: {0} at line {1} col {2}\", _state.Errors, DoGetLine(_state.Errors.Index), DoGetCol(_state.Errors.Index));");
				DoWriteLine("		--m_debugLevel;");
				DoWriteLine("	}");
			}
			else
			{
				DoWriteLine("	if (_state.Parsed)");
				DoWriteLine("		DoDebugWrite(\"" + debugName + " parsed: {0}\", DoTruncateString(m_input.Substring(_start.Index, _state.Index - _start.Index)));");
				DoWriteLine("	else");
				DoWriteLine("		DoDebugWrite(\"" + debugName + " failed: {0} at line {1} col {2}\", _state.Errors, DoGetLine(_state.Errors.Index), DoGetCol(_state.Errors.Index));");
				DoWriteLine("	--m_debugLevel;");
			}
		}
		DoWriteLine("	");
		DoWriteLine("	return _state;");
		DoWriteLine("}");
	}
	
	#region Private Methods
	private void DoWriteNonTerminalRule(Rule rule)
	{
		var line = new System.Text.StringBuilder();
		
		line.Append("\t");
		rule.Expression.Write(line, 0);
		line.Append(";");
		
		DoWriteLine(line.ToString());
	}
	
	// This isn't especially efficient but it shouldn't matter except perhaps for
	// enormous grammars.
	private bool DoReferencesLocal(string text, string local)
	{
		if (text.Contains(local + " "))
			return true;
			
		else if (text.Contains(local + "\t"))
			return true;
			
		else if (text.Contains(local + "="))
			return true;
			
		else if (text.Contains(local + "."))
			return true;
			
		else if (text.Contains(local + ";"))
			return true;
			
		else if (text.EndsWith(local))
			return true;
			
		else if (text.Contains(local + ","))		// these last two are for wacky actions that do things like `DoSet(out text)`
			return true;
			
		else if (text.Contains(local + ")"))
			return true;
			
		return false;
	}
	#endregion
}
