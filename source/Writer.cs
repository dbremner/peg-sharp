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
using System.IO;
using System.Collections.Generic;
using System.Linq;

// Writes the parser file.
internal sealed partial class Writer : IDisposable
{
	public Writer(TextWriter writer, Grammar grammar)
	{
		m_writer = writer;
		m_grammar = grammar;
		if (m_grammar.Settings.ContainsKey("debug"))
			m_debug = m_grammar.Settings["debug"].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
		else
			m_debug = new string[0];
	}
	
	public void Write(string pegFile)
	{
		if (m_disposed)
			throw new ObjectDisposedException(GetType().Name);
		
		m_className = System.IO.Path.GetFileNameWithoutExtension(pegFile);
		
		var rules = new Dictionary<string, List<Rule>>();
		foreach (Rule rule in m_grammar.Rules)
		{
			List<Rule> temp;
			if (!rules.TryGetValue(rule.Name, out temp))
			{
				temp = new List<Rule>();
				rules.Add(rule.Name, temp);
			}
			temp.Add(rule);
		}
		
		DoWriteFileComment(pegFile);
		DoWriteUsing();
		if (m_grammar.Settings["exclude-exception"] == "false")
			DoWriteException();
		DoWriteClassHeader();
		DoWriteCtor(rules);
		DoWriteParseMethod();
		if (m_grammar.Settings["unconsumed"] == "expose")
			DoWriteUnconsumedProperty();
		DoWriteNonTerminals(rules);
		DoWriteHelpers();
		DoWriteTypes();
		DoWriteFields();
		DoWriteClassTrailer();
	}
	
	public void Dispose()
	{
		if (!m_disposed)
		{
			m_writer.Dispose();
			m_disposed = true;
		}
	}
	
	#region Private Methods
	private void DoWriteFileComment(string pegFile)
	{
		Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		DoWriteLine("// Machine generated by peg-sharp {0} from {1}.", version, pegFile);
	}
	
	private void DoWriteClassHeader()
	{
		if (m_grammar.Settings["comment"].Length == 0)
		{
			DoWriteLine("// Thread safe if Parser instances are not shared across threads.");
		}
		else
		{
			foreach (string line in m_grammar.Settings["comment"].Split('\n'))
			{
				DoWriteLine(line);
			}
		}
		
		DoWriteLine("{0} sealed partial class {1}", m_grammar.Settings["visibility"], m_className);
		DoWriteLine("{");
		
		++m_indent;
	}
	
	private void DoWriteClassTrailer()
	{
		--m_indent;
		
		DoWriteLine("}");
		
		if (m_grammar.Settings.ContainsKey("namespace"))
		{
			--m_indent;
			DoWriteLine("}");
		}
	}
	
	private string DoGetNonterminalMethodName(List<Rule> rules, int i)
	{
		var line = new System.Text.StringBuilder();
		
		line.Append("DoParse");
		line.Append(rules[i].Name.Replace('-', '_'));
		if (rules.Count > 1)
			line.Append((i + 1).ToString());
		
		line.Append("Rule");
		
		return line.ToString();
	}
	
	private void DoWriteCtor(Dictionary<string, List<Rule>> rules)
	{
		if (m_grammar.Settings["exclude-methods"].Contains(m_className + ' '))
			return;
			
		DoWriteLine("public {0}()", m_className);
		DoWriteLine("{");
		
		foreach (var entry in rules)
		{
			var line = new System.Text.StringBuilder();
			line.Append("\tm_nonterminals.Add(\"");
			line.Append(entry.Key);
			line.Append("\", new ParseMethod[]{");
			
			for (int i = 0; i < entry.Value.Count; ++i)
			{
				line.Append("this.");
				line.Append(DoGetNonterminalMethodName(entry.Value, i));
				
				if (i + 1 < entry.Value.Count)
					line.Append(", ");
			}
			
			line.Append("});");
			DoWriteLine(line.ToString());
		}
		
		DoWriteLine("\tOnCtorEpilog();");
		DoWriteLine("}");
		DoWriteLine();
	}
	
	private void DoWriteException()
	{
		DoWriteLine("[Serializable]");
		DoWriteLine("{0} sealed class ParserException : Exception", m_grammar.Settings["visibility"]);
		DoWriteLine("{");
		DoWriteLine("	public ParserException()");
		DoWriteLine("	{");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public ParserException(string message) : base(message)");
		DoWriteLine("	{");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public ParserException(int line, int col, string file, string message) : base(string.Format(\"{0} at line {1} col {2}{3}\", message, line, col, file != null ? (\" in \" + file) : \".\"))");
		DoWriteLine("	{");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public ParserException(int line, int col, string file, string format, params object[] args) : this(line, col, file, string.Format(format, args))");
		DoWriteLine("	{");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public ParserException(int line, int col, string file, string message, Exception inner) : base(string.Format(\"{0} at line {1} col {2}{3}\", message, line, col, file != null ? (\" in \" + file) : \".\"), inner)");
		DoWriteLine("	{");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]");
		DoWriteLine("	private ParserException(SerializationInfo info, StreamingContext context) : base(info, context)");
		DoWriteLine("	{");
		DoWriteLine("	}");
		DoWriteLine("}");
		DoWriteLine("");
	}
	
	private void DoWriteFields()
	{
		DoWriteLine("#region Fields");
		DoWriteLine("private string m_input;");
		DoWriteLine("private string m_file;");
		DoWriteLine("private Dictionary<string, ParseMethod[]> m_nonterminals = new Dictionary<string, ParseMethod[]>();");
		DoWriteLine("private Dictionary<CacheKey, CacheValue> m_cache = new Dictionary<CacheKey, CacheValue>();");
		if (m_grammar.Settings["unconsumed"] == "expose")
			DoWriteLine("private int m_consumed;");
		if (m_debug.Length > 0)
		{
			DoWriteLine("private int m_debugLevel;");
			string[] names = (from n in m_debug select '"' + n + '"').ToArray();
			DoWriteLine("private string[] m_debug = new string[]{" + string.Join(", ", names) + "};");
			if (m_grammar.Settings["debug-file"].Length > 0)
				DoWriteLine("private string m_debugFile = \"" + m_grammar.Settings["debug-file"] + "\";");
		}
		if (m_grammar.Settings["value"] == "XmlNode")
			DoWriteLine("private XmlDocument m_doc;");
		DoWriteLine("#endregion");
	}
	
	private void DoWriteHelpers()
	{
		DoWriteLine("#region Private Helper Methods");
		DoWriteLine("partial void OnCtorEpilog();");
		DoWriteLine("partial void OnParseProlog();");
		DoWriteLine("partial void OnParseEpilog(State state);");
		DoWriteLine();
		if (!m_grammar.Settings["exclude-methods"].Contains("DoEscapeAll "))
		{
			DoWriteLine("public string DoEscapeAll(string s)");
			DoWriteLine("{");
			DoWriteLine("	var builder = new System.Text.StringBuilder(s.Length);");
			DoWriteLine("	");
			DoWriteLine("	foreach (char ch in s)");
			DoWriteLine("	{");
			DoWriteLine("		if (ch == '\\n')");
			DoWriteLine("			builder.Append(\"\\\\n\");");
			DoWriteLine("		");
			DoWriteLine("		else if (ch == '\\r')");
			DoWriteLine("			builder.Append(\"\\\\r\");");
			DoWriteLine("		");
			DoWriteLine("		else if (ch == '\\t')");
			DoWriteLine("			builder.Append(\"\\\\t\");");
			DoWriteLine("		");
			DoWriteLine("		else if (ch < ' ')");
			DoWriteLine("			builder.AppendFormat(\"\\\\x{0:X2}\", (int) ch);");
			DoWriteLine("		");
			DoWriteLine("		else");
			DoWriteLine("			builder.Append(ch);");
			DoWriteLine("	}");
			DoWriteLine("	");
			DoWriteLine("	return builder.ToString();");
			DoWriteLine("}");
			DoWriteLine("");
		}
		if (m_debug.Length > 0)
		{
			if (!m_grammar.Settings["exclude-methods"].Contains("DoDebugWrite "))
			{
				DoWriteLine("private void DoDebugWrite(string format, params object[] args)");
				DoWriteLine("{");
				DoWriteLine("	Console.Write(new string(' ', 4*m_debugLevel));");
				DoWriteLine("	Console.WriteLine(DoEscapeAll(string.Format(format, args)));");
				DoWriteLine("}");
				DoWriteLine("");
			}
			if (!m_grammar.Settings["exclude-methods"].Contains("DoTruncateString "))
			{
				DoWriteLine("private string DoTruncateString(string str)");
				DoWriteLine("{");
				DoWriteLine("	if (str.Length > 48)");
				DoWriteLine("		return str.Substring(0, 24) + \"...\" + str.Substring(str.Length - 24);");
				DoWriteLine("	else if (str.Length > 0 && str[str.Length - 1] == '\\x0')");
				DoWriteLine("		return str.Substring(0, str.Length - 1);");
				DoWriteLine("	else");
				DoWriteLine("		return str;");
				DoWriteLine("}");
				DoWriteLine("");
			}
		}
		if (!m_grammar.Settings["exclude-methods"].Contains("DoGetLine "))
		{
			DoWriteLine("// This is normally only used for error handling so it doesn't need to be too");
			DoWriteLine("// fast. If it somehow does become a bottleneck for some parsers they can");
			DoWriteLine("// replace it with the custom-methods setting.");
			DoWriteLine("private int DoGetLine(int index)");
			DoWriteLine("{");
			DoWriteLine("	int line = 1;");
			DoWriteLine("	");
			DoWriteLine("	int i = 0;");
			DoWriteLine("	while (i <= index)");
			DoWriteLine("	{");
			DoWriteLine("		char ch = m_input[i++];");
			DoWriteLine("		");
			DoWriteLine("		if (ch == '\\r' && m_input[i] == '\\n')");
			DoWriteLine("		{");
			DoWriteLine("			++i;");
			DoWriteLine("			++line;");
			DoWriteLine("		}");
			DoWriteLine("		else if (ch == '\\r')");
			DoWriteLine("		{");
			DoWriteLine("			++line;");
			DoWriteLine("		}");
			DoWriteLine("		else if (ch == '\\n')");
			DoWriteLine("		{");
			DoWriteLine("			++line;");
			DoWriteLine("		}");
			DoWriteLine("	}");
			DoWriteLine("	");
			DoWriteLine("	return line;");
			DoWriteLine("}");
			DoWriteLine("");
		}
		if (!m_grammar.Settings["exclude-methods"].Contains("DoGetCol "))
		{
			DoWriteLine("private int DoGetCol(int index)");
			DoWriteLine("{");
			DoWriteLine("	int start = index;");
			DoWriteLine("	");
			DoWriteLine("	while (index > 0 && m_input[index - 1] != '\\n' && m_input[index - 1] != '\\r')");
			DoWriteLine("	{");
			DoWriteLine("		--index;");
			DoWriteLine("	}");
			DoWriteLine("	");
			DoWriteLine("	return start - index + 1;");
			DoWriteLine("}");
			DoWriteLine("");
		}
		if (m_grammar.Settings["unconsumed"] == "error")
		{
			if (!m_grammar.Settings["exclude-methods"].Contains("DoThrow "))
			{
				DoWriteLine("private void DoThrow(int index, string format, params object[] args)");
				DoWriteLine("{");
				DoWriteLine("	int line = DoGetLine(index);");
				DoWriteLine("	int col = DoGetCol(index);");
				DoWriteLine("");
				DoWriteLine("	// We need this retarded if or string.Format will throw an error if it");
				DoWriteLine("	// gets a format string like \"Expected { or something\".");
				DoWriteLine("	if (args != null && args.Length > 0)");
				DoWriteLine("		throw new ParserException(line, col, m_file, DoEscapeAll(string.Format(format, args)));");
				DoWriteLine("	else");
				DoWriteLine("		throw new ParserException(line, col, m_file, DoEscapeAll(format));");
				DoWriteLine("}");
				DoWriteLine("");
			}
		}
		if (m_grammar.Settings["value"] == "XmlNode")
		{
			DoWriteLine("private XmlText DoCreateTextNode(string data, int line, int col)");
			DoWriteLine("{");
			DoWriteLine("	XmlText node = m_doc.CreateTextNode(data);");
			DoWriteLine("	");
			DoWriteLine("	return node;");
			DoWriteLine("}");
			DoWriteLine("");
			DoWriteLine("private XmlElement DoCreateElementNode(string name, int offset, int length, int line, int col, XmlNode[] children)");
			DoWriteLine("{");
			DoWriteLine("	XmlElement node = m_doc.CreateElement(name);");
			DoWriteLine("	");
			DoWriteLine("	node.SetAttribute(\"offset\", offset.ToString());");
			DoWriteLine("	node.SetAttribute(\"length\", length.ToString());");
			DoWriteLine("	node.SetAttribute(\"line\", line.ToString());");
			DoWriteLine("	node.SetAttribute(\"col\", col.ToString());");
			DoWriteLine("	");
			DoWriteLine("	foreach (XmlNode child in children)");
			DoWriteLine("		node.AppendChild(child);");
			DoWriteLine("	");
			DoWriteLine("	return node;");
			DoWriteLine("}");
			DoWriteLine("");
		}
		if ((m_used & Used.Literal) != 0)
		{
			if (!m_grammar.Settings["exclude-methods"].Contains("DoParseLiteral "))
			{
				DoWriteLine("private State DoParseLiteral(State state, List<Result> results, string literal)");
				DoWriteLine("{");
				DoWriteLine("	int j = state.Index;");
				DoWriteLine("	");
				DoWriteLine("	for (int i = 0; i < literal.Length; ++i)");
				DoWriteLine("	{");
				if (m_grammar.Settings["ignore-case"] == "true")
					DoWriteLine("		if (char.ToLower(m_input[j + i]) != literal[i])");
				else
					DoWriteLine("		if (m_input[j + i] != literal[i])");
				DoWriteLine("		{");
				DoWriteLine("			return new State(state.Index, false, ErrorSet.Combine(state.Errors, new ErrorSet(state.Index, literal)));");
				DoWriteLine("		}");
				DoWriteLine("	}");
				DoWriteLine("	");
				DoWriteLine("	int k = j + literal.Length;");
				DoWriteLine("	");
				if (m_grammar.Settings["value"] == "XmlNode")
					DoWriteLine("	results.Add(new Result(this, j, literal.Length, m_input, DoCreateTextNode(literal, DoGetLine(j), DoGetCol(j))));");
				else if (m_grammar.Settings["value"] != "void")
					DoWriteLine("	results.Add(new Result(this, j, literal.Length, m_input, default({0})));", m_grammar.Settings["value"]);
				else
					DoWriteLine("	results.Add(new Result(this, j, literal.Length, m_input));");
				DoWriteLine("	state = new State(k, true, state.Errors);");
				DoWriteLine("	");
				DoWriteLine("	return state;");
				DoWriteLine("}");
				DoWriteLine("");
			}
		}
		if (!m_grammar.Settings["exclude-methods"].Contains("DoParse "))
		{
			DoWriteLine("private State DoParse(State state, List<Result> results, string nonterminal)");
			DoWriteLine("{");
			DoWriteLine("	State start = state;");
			DoWriteLine("	");
			DoWriteLine("	CacheValue cache;");
			DoWriteLine("	CacheKey key = new CacheKey(nonterminal, start.Index);");
			DoWriteLine("	if (!m_cache.TryGetValue(key, out cache))");
			DoWriteLine("	{");
			DoWriteLine("		ParseMethod[] methods = m_nonterminals[nonterminal];");
			DoWriteLine("		");
			if (m_grammar.Settings["value"] != "void")
				DoWriteLine("		int oldCount = results.Count;");
			DoWriteLine("		state = DoChoice(state, results, methods);");
			DoWriteLine("		");
			if (m_grammar.Settings["value"] != "void")
			{
				DoWriteLine("		bool hasResult = state.Parsed && results.Count > oldCount;");
				DoWriteLine("		{0} value = hasResult ? results[results.Count - 1].Value : default({0});", m_grammar.Settings["value"]);
				DoWriteLine("		cache = new CacheValue(state, value, hasResult);");
			}
			else
			{
				DoWriteLine("		cache = new CacheValue(state, state.Parsed);");
			}
			DoWriteLine("		m_cache.Add(key, cache);");
			DoWriteLine("	}");
			DoWriteLine("	else");
			DoWriteLine("	{");
			if (m_debug.Length > 0)
			{
				if (m_grammar.Settings["debug-file"].Length > 0)
					DoWriteLine("		if (m_file == m_debugFile && (m_debug[0] == \"*\" || Array.IndexOf(m_debug, nonterminal) >= 0))");
				else
					DoWriteLine("		if (m_debug[0] == \"*\" || Array.IndexOf(m_debug, nonterminal) >= 0)");
				DoWriteLine("		{");
				DoWriteLine("			DoDebugWrite(\"cached {0}\", nonterminal);");
				DoWriteLine("			++m_debugLevel;");
				DoWriteLine("			if (cache.State.Parsed)");
				DoWriteLine("				DoDebugWrite(\"{0} parsed: {1}\", nonterminal, DoTruncateString(m_input.Substring(start.Index, cache.State.Index - start.Index)));");
				DoWriteLine("			else");
				DoWriteLine("				DoDebugWrite(\"{0} failed: {1} at line {2} col {3}\", nonterminal, cache.State.Errors, DoGetLine(cache.State.Errors.Index), DoGetCol(cache.State.Errors.Index));");
				DoWriteLine("			--m_debugLevel;");
				DoWriteLine("		}");
				DoWriteLine("		");
			}
			DoWriteLine("		if (cache.HasResult)");
			if (m_grammar.Settings["value"] != "void")
				DoWriteLine("			results.Add(new Result(this, start.Index, cache.State.Index - start.Index, m_input, cache.Value));");
			else
				DoWriteLine("			results.Add(new Result(this, start.Index, cache.State.Index - start.Index, m_input));");
			DoWriteLine("	}");
			DoWriteLine("	");
			DoWriteLine("	return cache.State;");
			DoWriteLine("}");
			DoWriteLine("");
		}
		if (!m_grammar.Settings["exclude-methods"].Contains("DoChoice "))
		{
			DoWriteLine("private State DoChoice(State state, List<Result> results, params ParseMethod[] methods)");
			DoWriteLine("{");
			DoWriteLine("	State start = state;");
			DoWriteLine("	int startResult = results.Count;");
			DoWriteLine("	");
			DoWriteLine("	foreach (ParseMethod method in methods)");
			DoWriteLine("	{");
			DoWriteLine("		State temp = method(state, results);");
			DoWriteLine("		if (temp.Parsed)");
			DoWriteLine("		{");
			DoWriteLine("			state = temp;");
			DoWriteLine("			break;");
			DoWriteLine("		}");
			DoWriteLine("		else");
			DoWriteLine("		{");
			DoWriteLine("			state = new State(start.Index, false, ErrorSet.Combine(state.Errors, temp.Errors));");
			DoWriteLine("			results.RemoveRange(startResult, results.Count - startResult);");
			DoWriteLine("		}");
			DoWriteLine("	}");
			DoWriteLine("	");
			DoWriteLine("	return state;");
			DoWriteLine("}");
			DoWriteLine();
		}
		if ((m_used & Used.Sequence) != 0 && !m_grammar.Settings["exclude-methods"].Contains("DoSequence "))
		{
			DoWriteLine("private State DoSequence(State state, List<Result> results, params ParseMethod[] methods)");
			DoWriteLine("{");
			DoWriteLine("	State start = state;");
			DoWriteLine("	int startResult = results.Count;");
			DoWriteLine("	");
			DoWriteLine("	foreach (ParseMethod method in methods)");
			DoWriteLine("	{");
			DoWriteLine("		State temp = method(state, results);");
			DoWriteLine("		if (temp.Parsed)");
			DoWriteLine("		{");
			DoWriteLine("			state = temp;");
			DoWriteLine("		}");
			DoWriteLine("		else");
			DoWriteLine("		{");
			DoWriteLine("			state = new State(start.Index, false, ErrorSet.Combine(start.Errors, temp.Errors));");
			DoWriteLine("			results.RemoveRange(startResult, results.Count - startResult);");
			DoWriteLine("			break;");
			DoWriteLine("		}");
			DoWriteLine("	}");
			DoWriteLine("	");
			DoWriteLine("	return state;");
			DoWriteLine("}");
		}
		if ((m_used & Used.Repetition) != 0 && !m_grammar.Settings["exclude-methods"].Contains("DoRepetition "))
		{
			DoWriteLine();
			DoWriteLine("private State DoRepetition(State state, List<Result> results, int min, int max, ParseMethod method)");
			DoWriteLine("{");
			DoWriteLine("	State start = state;");
			DoWriteLine("	");
			DoWriteLine("	int count = 0;");
			DoWriteLine("	while (count <= max)");
			DoWriteLine("	{");
			DoWriteLine("		State temp = method(state, results);");
			DoWriteLine("		if (temp.Parsed && temp.Index > state.Index)");
			DoWriteLine("		{");
			DoWriteLine("			state = temp;");
			DoWriteLine("			++count;");
			DoWriteLine("		}");
			DoWriteLine("		else");
			DoWriteLine("		{");
			DoWriteLine("			state = new State(state.Index, true, ErrorSet.Combine(state.Errors, temp.Errors));");
			DoWriteLine("			break;");
			DoWriteLine("		}");
			DoWriteLine("	}");
			DoWriteLine("	");
			DoWriteLine("	if (count < min || count > max)");
			DoWriteLine("		state = new State(start.Index, false, ErrorSet.Combine(start.Errors, state.Errors));");
			DoWriteLine("	");
			DoWriteLine("	return state;");
			DoWriteLine("}");
		}
		if ((m_used & Used.Range) != 0 && !m_grammar.Settings["exclude-methods"].Contains("DoParseRange "))
		{
			DoWriteLine();
			DoWriteLine("private State DoParseRange(State state, List<Result> results, bool inverted, string chars, string ranges, UnicodeCategory[] categories, string label)");
			DoWriteLine("{");
			if (m_grammar.Settings["ignore-case"] == "true")
				DoWriteLine("	char ch = char.ToLower(m_input[state.Index]);");
			else
				DoWriteLine("	char ch = m_input[state.Index];");
			DoWriteLine("	");
			DoWriteLine("	bool matched = chars.IndexOf(ch) >= 0;");
			DoWriteLine("	for (int i = 0; i < ranges.Length && !matched; i += 2)");
			DoWriteLine("	{");
			DoWriteLine("		matched = ranges[i] <= ch && ch <= ranges[i + 1];");
			DoWriteLine("	}");
			DoWriteLine("	for (int i = 0; categories != null && i < categories.Length && !matched; ++i)");
			DoWriteLine("	{");
			DoWriteLine("		matched = char.GetUnicodeCategory(ch) == categories[i];");
			DoWriteLine("	}");
			DoWriteLine("	");
			DoWriteLine("	if (inverted)");
			DoWriteLine("		matched = !matched && ch != '\\x0';");
			DoWriteLine("	");
			DoWriteLine("	if (matched)");
			DoWriteLine("	{");
			if (m_grammar.Settings["value"] == "XmlNode")
				DoWriteLine("		results.Add(new Result(this, state.Index, 1, m_input, DoCreateTextNode(m_input.Substring(state.Index, 1), DoGetLine(state.Index), DoGetCol(state.Index))));");
			else if (m_grammar.Settings["value"] != "void")
				DoWriteLine("		results.Add(new Result(this, state.Index, 1, m_input, default({0})));", m_grammar.Settings["value"]);
			else
				DoWriteLine("		results.Add(new Result(this, state.Index, 1, m_input));");
			DoWriteLine("		return new State(state.Index + 1, true, state.Errors);");
			DoWriteLine("	}");
			DoWriteLine("	");
			DoWriteLine("	return new State(state.Index, false, ErrorSet.Combine(state.Errors, new ErrorSet(state.Index, label)));");
			DoWriteLine("}");
		}
		if ((m_used & Used.Assert) != 0 && !m_grammar.Settings["exclude-methods"].Contains("DoAssert "))
		{
			DoWriteLine();
			DoWriteLine("private State DoAssert(State state, List<Result> results, ParseMethod method)");
			DoWriteLine("{");
			DoWriteLine("	State temp = method(state, results);");
			DoWriteLine("	");
			DoWriteLine("	state = new State(state.Index, temp.Parsed, state.Errors);");
			DoWriteLine("	");
			DoWriteLine("	return state;");
			DoWriteLine("}");
		}
		if ((m_used & Used.NAssert) != 0 && !m_grammar.Settings["exclude-methods"].Contains("DoNAssert "))
		{
			DoWriteLine();
			DoWriteLine("private State DoNAssert(State state, List<Result> results, ParseMethod method)");
			DoWriteLine("{");
			DoWriteLine("	State temp = method(state, results);");
			DoWriteLine("	");
			DoWriteLine("	state = new State(state.Index, !temp.Parsed, state.Errors);");
			DoWriteLine("	");
			DoWriteLine("	return state;");
			DoWriteLine("}");
		}
		DoWriteLine("#endregion");
		DoWriteLine();
	}
	
	private void DoWriteNonTerminals(Dictionary<string, List<Rule>> rules)
	{
		DoWriteLine("#region Non-Terminal Parse Methods");
		int count = 0;
		foreach (var entry in rules)
		{
			for (int i = 0; i < entry.Value.Count; ++i)
			{
				string name = DoGetNonterminalMethodName(entry.Value, i);
				DoWriteNonTerminal(name, entry.Value[i], i, entry.Value.Count);
				m_used |= entry.Value[i].Expression.FindUsed();
				
				if (i + 1 < entry.Value.Count)
					DoWriteLine("");
			}
			
			if (++count < rules.Count)
				DoWriteLine("");
		}
		
		DoWriteLine("#endregion");
		DoWriteLine();
	}
	
	private void DoWriteParseMethod()
	{
		if (m_grammar.Settings["exclude-methods"].Contains("Parse "))
			return;
			
		string value = m_grammar.Settings["value"];
		DoWriteLine("public {0} Parse(string input)", value == "void" ? "int" : value);
		DoWriteLine("{");
		DoWriteLine("	return Parse(input, null);");
		DoWriteLine("}");
		DoWriteLine();

		DoWriteLine("// File is used for error reporting.");
		DoWriteLine("public {0} Parse(string input, string file)", value == "void" ? "int" : value);
		DoWriteLine("{");
		++m_indent;
		
		if (m_debug.Length > 0)
		{
			if (m_grammar.Settings["debug-file"].Length > 0)
			{
				DoWriteLine("if (m_file == m_debugFile)");
				DoWriteLine("{");
				DoWriteLine("	DoDebugWrite(new string('-', 32));");
				DoWriteLine("	if (!string.IsNullOrEmpty(file))");
				DoWriteLine("		DoDebugWrite(file);");
				DoWriteLine("}");
			}
			else
			{
				DoWriteLine("DoDebugWrite(new string('-', 32));");
				DoWriteLine("if (!string.IsNullOrEmpty(file))");
				DoWriteLine("	DoDebugWrite(file);");
			}
			DoWriteLine();
		}
		
		if (value == "XmlNode")
			DoWriteLine("m_doc = new XmlDocument();");
		DoWriteLine("m_file = file;");
		DoWriteLine("m_input = m_file;				// we need to ensure that m_file is used or we will (in some cases) get a compiler warning");
		DoWriteLine("m_input = input + \"\\x0\";	// add a sentinel so we can avoid range checks");
		DoWriteLine("m_cache.Clear();");
		if (m_grammar.Settings["unconsumed"] == "expose")
			DoWriteLine("m_consumed = 0;");
		DoWriteLine();
		DoWriteLine("State state = new State(0, true);");
		DoWriteLine("var results = new List<Result>();");
		DoWriteLine();
		DoWriteLine("OnParseProlog();");
		DoWriteLine("state = DoParse(state, results, \"{0}\");", m_grammar.Settings["start"]);
		DoWriteLine();
		
		if (m_grammar.Settings["unconsumed"] == "expose")
		{
			DoWriteLine("m_consumed = state.Index;");
		}
		else if (m_grammar.Settings["unconsumed"] == "error")
		{
			DoWriteLine("int i = state.Index;");
			DoWriteLine("if (!state.Parsed)");
			DoWriteLine("	DoThrow(state.Errors.Index, state.Errors.ToString());");
			DoWriteLine("else if (i < input.Length)");
			DoWriteLine("	if (state.Errors.Expected.Length > 0)");
			DoWriteLine("		DoThrow(state.Errors.Index, state.Errors.ToString());");
			DoWriteLine("	else");
			DoWriteLine("		DoThrow(state.Errors.Index, \"Not all input was consumed starting from '\" + input.Substring(i, Math.Min(16, input.Length - i)) + \"'\");");
		}
		
		if (value == "XmlNode")
		{
			DoWriteLine("");
			DoWriteLine("m_doc.AppendChild(results[0].Value);");
		}
		DoWriteLine("OnParseEpilog(state);");
		
		DoWriteLine();
		if (value == "void")
			DoWriteLine("return state.Index;");
		else if (value == "XmlNode")
			DoWriteLine("return m_doc;");
		else
			DoWriteLine("return results[0].Value;");
		
		--m_indent;
		DoWriteLine("}");
		DoWriteLine();
	}
	
	private void DoWriteUnconsumedProperty()
	{
		DoWriteLine("// Will be string.Empty if everything was consumed.");
		DoWriteLine("public string Unconsumed");
		DoWriteLine("{");
		DoWriteLine("	get {return m_input.Substring(m_consumed, m_input.Length - m_consumed - 1);}");
		DoWriteLine("}");
		DoWriteLine();
	}
	
	private void DoWriteTypes()
	{
		DoWriteLine("#region Private Types");
		DoWriteLine("private struct CacheKey : IEquatable<CacheKey>");
		DoWriteLine("{");
		DoWriteLine("	public CacheKey(string rule, int index)");
		DoWriteLine("	{");
		DoWriteLine("		m_rule = rule;");
		DoWriteLine("		m_index = index;");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public override bool Equals(object obj)");
		DoWriteLine("	{");
		DoWriteLine("		if (obj == null)");
		DoWriteLine("			return false;");
		DoWriteLine("		");
		DoWriteLine("		if (GetType() != obj.GetType())");
		DoWriteLine("			return false;");
		DoWriteLine("		");
		DoWriteLine("		CacheKey rhs = (CacheKey) obj;");
		DoWriteLine("		return this == rhs;");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public bool Equals(CacheKey rhs)");
		DoWriteLine("	{");
		DoWriteLine("		return this == rhs;");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public static bool operator==(CacheKey lhs, CacheKey rhs)");
		DoWriteLine("	{");
		DoWriteLine("		if (lhs.m_rule != rhs.m_rule)");
		DoWriteLine("			return false;");
		DoWriteLine("		");
		DoWriteLine("		if (lhs.m_index != rhs.m_index)");
		DoWriteLine("			return false;");
		DoWriteLine("		");
		DoWriteLine("		return true;");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public static bool operator!=(CacheKey lhs, CacheKey rhs)");
		DoWriteLine("	{");
		DoWriteLine("		return !(lhs == rhs);");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public override int GetHashCode()");
		DoWriteLine("	{");
		DoWriteLine("		int hash = 0;");
		DoWriteLine("		");
		DoWriteLine("		unchecked");
		DoWriteLine("		{");
		DoWriteLine("			hash += m_rule.GetHashCode();");
		DoWriteLine("			hash += m_index.GetHashCode();");
		DoWriteLine("		}");
		DoWriteLine("		");
		DoWriteLine("		return hash;");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	private string m_rule;");
		DoWriteLine("	private int m_index;");
		DoWriteLine("}");
		DoWriteLine("");
		DoWriteLine("private struct CacheValue");
		DoWriteLine("{");
		if (m_grammar.Settings["value"] != "void")
			DoWriteLine("	public CacheValue(State state, {0} value, bool hasResult)", m_grammar.Settings["value"]);
		else
			DoWriteLine("	public CacheValue(State state, bool hasResult)");
		DoWriteLine("	{");
		DoWriteLine("		State = state;");
		if (m_grammar.Settings["value"] != "void")
			DoWriteLine("		Value = value;");
		DoWriteLine("		HasResult = hasResult;");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public State State {get; private set;}");
		if (m_grammar.Settings["value"] != "void")
		{
			DoWriteLine("	");
			DoWriteLine("	public {0} Value {1}get; private set;{2}", m_grammar.Settings["value"], "{", "}");
		}
		DoWriteLine("	");
		DoWriteLine("	public bool HasResult {get; private set;}");
		DoWriteLine("}");
		DoWriteLine("");
		DoWriteLine("private delegate State ParseMethod(State state, List<Result> results);");
		DoWriteLine("");
		DoWriteLine("// These are either an error that caused parsing to fail or the reason a");
		DoWriteLine("// successful parse stopped.");
		DoWriteLine("private struct ErrorSet");
		DoWriteLine("{");
		DoWriteLine("	public ErrorSet(int index, string expected)");
		DoWriteLine("	{");
		DoWriteLine("		Index = index;");
		DoWriteLine("		Expected = new string[]{expected};");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public ErrorSet(int index, string[] expected)");
		DoWriteLine("	{");
		DoWriteLine("		Index = index;");
		DoWriteLine("		Expected = expected;");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	// The location associated with the errors. For a failed parse this will be the");
		DoWriteLine("	// same as State.Index. For a successful parse it will be State.Index or later.");
		DoWriteLine("	public int Index {get; private set;}");
		DoWriteLine("	");
		DoWriteLine("	// This will be the name of something which was expected, but not found.");
		DoWriteLine("	public string[] Expected {get; private set;}");
		DoWriteLine("	");
		DoWriteLine("	public static ErrorSet Combine(ErrorSet lhs, ErrorSet rhs)");
		DoWriteLine("	{");
		DoWriteLine("		if (lhs.Index > rhs.Index)");
		DoWriteLine("		{");
		DoWriteLine("			return lhs;");
		DoWriteLine("		}");
		DoWriteLine("		else if (lhs.Index < rhs.Index)");
		DoWriteLine("		{");
		DoWriteLine("			return rhs;");
		DoWriteLine("		}");
		DoWriteLine("		else");
		DoWriteLine("		{");
		DoWriteLine("			var errors = new List<string>(lhs.Expected.Length + rhs.Expected.Length);");
		DoWriteLine("			errors.AddRange(lhs.Expected);");
		DoWriteLine("			foreach (string err in rhs.Expected)");
		DoWriteLine("			{");
		DoWriteLine("				if (errors.IndexOf(err) < 0)");
		DoWriteLine("					errors.Add(err);");
		DoWriteLine("			}");
		DoWriteLine("			return new ErrorSet(lhs.Index, errors.ToArray());");
		DoWriteLine("		}");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public override string ToString()");
		DoWriteLine("	{");
		DoWriteLine("		if (Expected.Length > 0)");
		DoWriteLine("			return string.Format(\"Expected {0}\", string.Join(\" or \", Expected));");
		DoWriteLine("		else");
		DoWriteLine("			return \"<none>\";");
		DoWriteLine("	}");
		DoWriteLine("}");
		DoWriteLine("");
		DoWriteLine("// The state of the parser.");
		DoWriteLine("private struct State");
		DoWriteLine("{");
		DoWriteLine("	public State(int index, bool parsed)");
		DoWriteLine("	{");
		DoWriteLine("		Index = index;");
		DoWriteLine("		Parsed = parsed;");
		DoWriteLine("		Errors = new ErrorSet(index, new string[0]);");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	public State(int index, bool parsed, ErrorSet errors)");
		DoWriteLine("	{");
		DoWriteLine("		Index = index;");
		DoWriteLine("		Parsed = parsed;");
		DoWriteLine("		Errors = errors;");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	// Index of the first unconsumed character.");
		DoWriteLine("	public int Index {get; private set;}");
		DoWriteLine("	");
		DoWriteLine("	// True if the expression associated with the state successfully parsed.");
		DoWriteLine("	public bool Parsed {get; private set;}");
		DoWriteLine("	");
		DoWriteLine("	// If Parsed is false then this will explain why. If Parsed is true it will");
		DoWriteLine("	// say why the parse stopped.");
		DoWriteLine("	public ErrorSet Errors {get; private set;}");
		DoWriteLine("}");
		DoWriteLine("");
		DoWriteLine("// The result of parsing a literal or non-terminal.");
		DoWriteLine("private struct Result");
		DoWriteLine("{");
		if (m_grammar.Settings["value"] != "void")
			DoWriteLine("	public Result({0} parser, int index, int length, string input, {1} value)", m_className, m_grammar.Settings["value"]);
		else
			DoWriteLine("	public Result({0} parser, int index, int length, string input)", m_className);
		DoWriteLine("	{");
		DoWriteLine("		m_parser = parser;");
		DoWriteLine("		m_index = index;");
		DoWriteLine("		m_length = length;");
		DoWriteLine("		m_input = input;");
		if (m_grammar.Settings["value"] != "void")
			DoWriteLine("		Value = value;");
		DoWriteLine("	}");
		DoWriteLine("	");
		DoWriteLine("	// The text which was parsed by the terminal or non-terminal.");
		DoWriteLine("	public string Text {get {return m_input.Substring(m_index, m_length);}}");
		DoWriteLine("	");
		DoWriteLine("	// The 1-based line number the (non)terminal started on.");
		DoWriteLine("	public int Line {get {return m_parser.DoGetLine(m_index);}}");
		DoWriteLine("	");
		DoWriteLine("	// The 1-based column number the (non)terminal started on.");
		DoWriteLine("	public int Col {get {return m_parser.DoGetCol(m_index);}}");
		if (m_grammar.Settings["value"] != "void")
		{
			DoWriteLine("	");
			DoWriteLine("	// For non-terminals this will be the result of the semantic action, ");
			DoWriteLine("	// otherwise it will be the default value.");
			DoWriteLine("	public {0} Value {1}get; private set;{2}", m_grammar.Settings["value"], "{", "}");
		}
		DoWriteLine("	");
		DoWriteLine("	private {0} m_parser;", m_className);
		DoWriteLine("	private int m_index;");
		DoWriteLine("	private int m_length;");
		DoWriteLine("	private string m_input;");
		DoWriteLine("}");
		DoWriteLine("");
		DoWriteLine("#endregion");
		DoWriteLine();
	}
	
	private void DoWriteUsing()
	{
		var names = new List<string>{
			"System",
			"System.Collections.Generic",
			"System.Globalization",
			"System.Linq",
			"System.Runtime.Serialization",
			"System.Security.Permissions",
		};
		
		if (m_grammar.Settings.ContainsKey("using"))
			names.AddRange(m_grammar.Settings["using"].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries));
		
		names.Sort();
		
		foreach (string name in names)
		{
			DoWriteLine("using {0};", name);
		}
		if (m_grammar.Settings["value"] == "XmlNode")
			DoWriteLine("using System.Xml;");
		DoWriteLine();
		
		if (m_grammar.Settings.ContainsKey("namespace"))
		{
			DoWriteLine("namespace {0}", m_grammar.Settings["namespace"]);
			DoWriteLine("{");
			++m_indent;
		}
	}
	
	private void DoWriteLine()
	{
		for (int i = 0; i < m_indent; ++i)
			m_writer.Write('\t');						// TODO: add a setting for indent character(s)
		m_writer.WriteLine();
	}
	
	private void DoWriteLine(string message)
	{
		for (int i = 0; i < m_indent; ++i)
			m_writer.Write('\t');						// TODO: add a setting for indent character(s)
		m_writer.WriteLine(message);
	}
	
	private void DoWriteLine(string format, params object[] args)
	{
		DoWriteLine(string.Format(format, args));
	}
	#endregion
	
	#region Fields
	private TextWriter m_writer;
	private Grammar m_grammar;
	private int m_indent;
	private string[] m_debug;
	private Used m_used;
	private string m_className;
	private bool m_disposed;
	#endregion
}
