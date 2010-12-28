/* TIME-STAMP */

using System;
using System.Collections.Generic;
using System.Globalization;
//using System.Linq;								// TODO: this is handy enough that we'd like to include it by default, but we want the generated parsers to work with .NET 2.0 for now
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml;									// {{value == 'XmlNode'}}
/* USING-DECLARATIONS */

/* OPEN-NAMESPACE */
[Serializable]										// {{not exclude-exception}}
/* VISIBILITY */ sealed partial class ParserException : Exception					// {{not exclude-exception}}
{														// {{not exclude-exception}}
	//< ExceptionCtor
	public ParserException(int line, int col, int offset, string file, string input, string message) : base(string.Format("{0} at line {1} col {2}{3}", message, line, col, file != null ? (" in " + file) : "."))	// {{not exclude-exception}}
	{													// {{not exclude-exception}}
		File = file;									// {{not exclude-exception}}
		Line = line;									// {{not exclude-exception}}
		Col = col;										// {{not exclude-exception}}
	}													// {{not exclude-exception}}
	//> ExceptionCtor
	
	//< File
	public string File {get; private set;}		// {{not exclude-exception}}
	//> File
	//< Line
	public int Line {get; private set;}			// {{not exclude-exception}}
	//> Line
	//< Col
	public int Col {get; private set;}			// {{not exclude-exception}}
	//> Col
}														// {{not exclude-exception}}

/* PARSER-COMMENT */
/* VISIBILITY */ sealed partial class PARSER
{
	//< ParserCtor
	public PARSER()
	{
ADD-NON-TERMINALS
											//  {{not excluded('OnCtorEpilog')}}
		OnCtorEpilog();					//  {{not excluded('OnCtorEpilog')}}
	}
	//> ParserCtor
	
	//< Parse
	/* PARSE-ACCESSIBILITY */ RESULT Parse(string input)
	{
		return DoParseFile(input, null, "START-RULE");
	}
	//> Parse
	
	//< Parse
	// File is used for error reporting.
	/* PARSE-ACCESSIBILITY */ RESULT Parse(string input, string file)
	{
		return DoParseFile(input, file, "START-RULE");
	}
	//> Parse
	
	//< Unconsumed {{unconsumed == 'expose'}}
	// Will be string.Empty if everything was consumed.
	public string Unconsumed
	{
		get {return m_input.Substring(m_consumed, m_input.Length - m_consumed - 1);}
	}
	//> Unconsumed
	
	#region Non-Terminal Parse Methods
/* NON-TERMINALS */
	#endregion
	
	#region Private Helper Methods
	//< OnCtorEpilog
	partial void OnCtorEpilog();
	//> OnCtorEpilog
	//< OnParseProlog
	partial void OnParseProlog();
	//> OnParseProlog
	//< OnParseEpilog
	partial void OnParseEpilog(State state);
	//> OnParseEpilog
	
	//< DoAssert {{used-assert}}
	private State DoAssert(State state, List<Result> results, ParseMethod method)
	{
		State temp = method(state, results);
		
		state = new State(state.Index, temp.Parsed, state.Errors);
		
		return state;
	}
	//> DoAssert
	
	//< DoBuildLineStarts
	private void DoBuildLineStarts()
	{
		m_lineStarts = new List<int>();
		
		m_lineStarts.Add(0);		// line 1 starts at index 0 (even if we have no text)
		
		int i = 0;
		while (i < m_input.Length)
		{
			char ch = m_input[i++];
			
			if (ch == '\r' && m_input[i] == '\n')
			{
				m_lineStarts.Add(++i);
			}
			else if (ch == '\r')
			{
				m_lineStarts.Add(i);
			}
			else if (ch == '\n')
			{
				m_lineStarts.Add(i);
			}
		}
	}
	//> DoBuildLineStarts
	
	//< DoChoice {{used-choice}}
	private State DoChoice(State state, List<Result> results, params ParseMethod[] methods)
	{
		State start = state;
		int startResult = results.Count;
		
		foreach (ParseMethod method in methods)
		{
			State temp = method(state, results);
			if (temp.Parsed)
			{
				state = temp;
				break;
			}
			else
			{
				state = new State(start.Index, false, ErrorSet.Combine(state.Errors, temp.Errors));
				results.RemoveRange(startResult, results.Count - startResult);
			}
		}
		
		return state;
	}
	//> DoChoice
	
	//< DoCreateElementNode {{value == 'XmlNode'}}
	private XmlElement DoCreateElementNode(string name, int offset, int length, int line, int col, XmlNode[] children)
	{
		XmlElement node = m_doc.CreateElement(name);
		
		node.SetAttribute("offset", offset.ToString());
		node.SetAttribute("length", length.ToString());
		node.SetAttribute("line", line.ToString());
		node.SetAttribute("col", col.ToString());
		
		foreach (XmlNode child in children)
			node.AppendChild(child);
		
		return node;
	}
	//> DoCreateElementNode
	
	//< DoCreateTextNode {{value == 'XmlNode'}}
	private XmlText DoCreateTextNode(string data, int line, int col)
	{
		XmlText node = m_doc.CreateTextNode(data);
		
		return node;
	}
	//> DoCreateTextNode
	
	private static readonly string RightArrow = "\x2192";			// {{debugging}}
	private static readonly string DownArrow = "\x2193";			// {{debugging}}
	private static readonly string DownHookedArrow = "\x21A9";	// {{debugging}}
	private const int DebugWidth = 30;									// {{debugging}}
	
	//< DoDebugMatch {{debug == 'failures' or debug == 'both'}}
	private void DoDebugFailure(int offset, string label)
	{
		// Write the input centered on the offset.
		int end = Math.Min(offset + DebugWidth, m_input.Length - 1);		// last char is 0x00
		int begin = Math.Max(end - 2*DebugWidth, 0);
		int length = end - begin;
		
		string text = m_input.Substring(begin, length);
		if (begin > 0)
			text = "..." + text;
		if (end < m_input.Length - 1)
			text += "...";
			
		text = text.Replace("\t", RightArrow);
		text = text.Replace("\n", DownArrow);
		text = text.Replace("\r", DownHookedArrow);
		
		// Write an arrow pointing to the old offset.
		Console.WriteLine(text);
		
		int padding = begin > 0 ? 3 : 0;
		Console.Write(new string(' ', padding + Math.Max(offset - begin, 0)));
		Console.Write("^ ");
		Console.Write(label);
		DoDebugLine(offset, offset);
		Console.Out.Flush();
	}
	//> DoDebugMatch
	
	//< DoDebugLine {{debugging}}
	private void DoDebugLine(int offset1, int offset2)
	{
		int line1 = DoGetLine(offset1);
		int line2 = DoGetLine(offset2);
		if (line1 > line2)	
			Console.WriteLine(", lines {0}-{1}", line2, line1);
		else if (line1 < line2)
			Console.WriteLine(", lines {0}-{1}", line1, line2);
		else
			Console.WriteLine(", line {0}", line1);
		Console.WriteLine();
	}
	//> DoDebugLine
	
	//< DoDebugMatch {{debug == 'matches' or debug == 'both'}}
	private void DoDebugMatch(int oldOffset, int newOffset, string label)
	{
		// Write the input centered on the new offset.
		int end = Math.Min(newOffset + DebugWidth, m_input.Length - 1);	// last char is 0x00
		int begin = Math.Max(end - 2*DebugWidth, 0);
		int length = end - begin;
		
		string text = m_input.Substring(begin, length);
		if (begin > 0)
			text = "..." + text;
		if (end < m_input.Length - 1)
			text += "...";
		
		text = text.Replace("\t", RightArrow);
		text = text.Replace("\n", DownArrow);
		text = text.Replace("\r", DownHookedArrow);
		
		// Write an arrow pointing to the new offset.
		int padding = begin > 0 ? 3 : 0;
		Console.WriteLine(text);
		
		Console.Write(new string(' ', padding + Math.Max(oldOffset - begin, 0)));
		Console.Write(new string('_', newOffset - Math.Max(oldOffset, begin)));
		Console.Write("^ ");
		Console.Write(label);
		DoDebugLine(oldOffset, newOffset);
		Console.Out.Flush();
	}
	//> DoDebugMatch
	
	//< DoEscapeAll
	public string DoEscapeAll(string s)
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder(s.Length);
		
		foreach (char ch in s)
		{
			if (ch == '\n')
				builder.Append("\\n");
			
			else if (ch == '\r')
				builder.Append("\\r");
			
			else if (ch == '\t')
				builder.Append("\\t");
			
			else if (ch < ' ')
				builder.AppendFormat("\\x{0:X2}", (int) ch);
			
			else
				builder.Append(ch);
		}
		
		return builder.ToString();
	}
	//> DoEscapeAll
	
	//< DoGetCol
	private int DoGetCol(int index)
	{
		int start = index;
		
		while (index > 0 && m_input[index - 1] != '\n' && m_input[index - 1] != '\r')
		{
			--index;
		}
		
		return start - index;
	}
	//> DoGetCol
	
	//< DoGetLine
	// This is most often used just for error handling where it is a bit overkill.
	// However it's also sometimes used in rule prologs where efficiency is more
	// important (and doing a bit of extra work in the error case is not very harmful).
	private int DoGetLine(int index)
	{
		if (m_lineStarts == null)
			DoBuildLineStarts();
			
		int line = m_lineStarts.BinarySearch(index);
		if (line >= 0)
			return line + 1;
			
		return ~line;
	}
	//> DoGetLine
	
	//< DoNAssert {{used-nassert}}
	private State DoNAssert(State state, List<Result> results, ParseMethod method)
	{
		State temp = method(state, results);
		
		state = new State(state.Index, !temp.Parsed, state.Errors);
		
		return state;
	}
	//> DoNAssert
	
	//< DoParse
	private State DoParse(State state, List<Result> results, string nonterminal)
	{
		State start = state;
		
		CacheValue cache;
		CacheKey key = new CacheKey(nonterminal, start.Index, m_context);
		if (!m_cache.TryGetValue(key, out cache))
		{
			ParseMethod[] methods = null;
			if (!m_nonterminals.TryGetValue(nonterminal, out methods))
				throw new Exception("Couldn't find a " + nonterminal + " parse method");
			
			int oldCount = results.Count;											// {{value != 'void'}}
			state = DoChoice(state, results, methods);
			
			bool hasResult = state.Parsed && results.Count > oldCount;		// {{value != 'void'}}
			VALUE value = hasResult ? results[results.Count - 1].Value : default(VALUE);	// {{value != 'void'}}
			cache = new CacheValue(state, value, hasResult);					// {{value != 'void'}}
			cache = new CacheValue(state, state.Parsed);						// {{value == 'void'}}
			m_cache.Add(key, cache);
		}
		else
		{
			Console.WriteLine(nonterminal);				// {{debug != 'none'}}
			if (cache.HasResult)
				results.Add(new Result(this, start.Index, cache.State.Index - start.Index, m_input, cache.Value));	// {{value != 'void'}}
				results.Add(new Result(this, start.Index, cache.State.Index - start.Index, m_input));	// {{value == 'void'}}

			string name = string.Format("cached '{0}' ", nonterminal);									// {{debug != 'none'}}
			if (m_file == m_debugFile)																		// {{debugging and has-debug-file}}
			{																										// {{debugging and has-debug-file}}
				if (cache.State.Parsed)																			// {{(debug == 'matches' or debug == 'both') and has-debug-file}}
					DoDebugMatch(start.Index, cache.State.Index, name + "parsed");					// {{(debug == 'matches' or debug == 'both') and has-debug-file}}
				if (!cache.State.Parsed)																			// {{(debug == 'failures' or debug == 'both') and has-debug-file}}
					DoDebugFailure(start.Index, name + DoEscapeAll(cache.State.Errors.ToString()));	// {{(debug == 'failures' or debug == 'both') and has-debug-file}}
			}																										// {{debugging and has-debug-file}}
			if (cache.State.Parsed)																				// {{(debug == 'matches' or debug == 'both') and not has-debug-file}}
				DoDebugMatch(start.Index, cache.State.Index, name + "parsed");						// {{(debug == 'matches' or debug == 'both') and not has-debug-file}}
			if (!cache.State.Parsed)																				// {{(debug == 'failures' or debug == 'both') and not has-debug-file}}
				DoDebugFailure(start.Index, name + DoEscapeAll(cache.State.Errors.ToString()));	// {{(debug == 'failures' or debug == 'both') and not has-debug-file}}
		}
		
		return cache.State;
	}
	//> DoParse
	
	//< DoParseFile
	private RESULT DoParseFile(string input, string file, string rule)
	{
		if (m_file == m_debugFile)					// {{debugging and has-debug-file}}
		{													// {{debugging and has-debug-file}}
			Console.WriteLine(new string('-', 32));	// {{debugging and has-debug-file}}
			if (!string.IsNullOrEmpty(file))			// {{debugging and has-debug-file}}
				Console.WriteLine(file);					// {{debugging and has-debug-file}}
		}													// {{debugging and has-debug-file}}
		Console.WriteLine(new string('-', 32));		// {{debugging and not has-debug-file}}
		if (!string.IsNullOrEmpty(file))				// {{debugging and not has-debug-file}}
			Console.WriteLine(file);						// {{debugging and not has-debug-file}}
															// {{debugging}}
		m_doc = new XmlDocument();				// {{value == 'XmlNode'}}
		m_file = file;
		m_input = m_file;				// we need to ensure that m_file is used or we will (in some cases) get a compiler warning
		m_input = input + "\x0";		// add a sentinel so we can avoid range checks
		m_cache.Clear();
		m_lineStarts = null;
		m_consumed = 0;								// {{unconsumed == 'expose'}}
		
		State state = new State(0, true);
		List<Result> results = new List<Result>();
		OnParseProlog();									// {{not excluded('OnParseProlog')}}
		state = DoParse(state, results, rule);
			
		m_consumed = state.Index;					// {{unconsumed == 'expose'}}
		int i = state.Index;								// {{unconsumed == 'error'}}
		if (!state.Parsed)								// {{unconsumed == 'error'}}
			DoThrow(state.Errors.Index, state.Errors.ToString());			// {{unconsumed == 'error'}}
		else if (i < input.Length)						// {{unconsumed == 'error'}}
			if (state.Errors.Expected.Length > 0)		// {{unconsumed == 'error'}}
				DoThrow(state.Errors.Index, state.Errors.ToString());			// {{unconsumed == 'error'}}
			else											// {{unconsumed == 'error'}}
				DoThrow(state.Errors.Index, "Not all input was consumed starting from '" + input.Substring(i, Math.Min(16, input.Length - i)) + "'");			// {{unconsumed=='error'}}
		
		m_doc.AppendChild(results[0].Value);		// {{value == 'XmlNode'}}
		OnParseEpilog(state);							// {{not excluded('OnParseEpilog')}}
		
		return state.Index;								// {{value == 'void'}}
		return m_doc;									// {{value == 'XmlNode'}}
		return results[0].Value;						// {{value != 'void' and value != 'XmlNode'}}
	}
	//> DoParseFile
	
	//< DoParseLiteral {{used-literal}}
	private State DoParseLiteral(State state, List<Result> results, string literal)
	{
		State result;
		
		if (string.Compare(m_input, state.Index, literal, 0, literal.Length, true) == 0)	// {{ignore-case}}
		if (string.Compare(m_input, state.Index, literal, 0, literal.Length) == 0)			// {{not ignore-case}}
		{
			results.Add(new Result(this, state.Index, literal.Length, m_input, DoCreateTextNode(literal, DoGetLine(state.Index), DoGetCol(state.Index))));	// {{value == 'XmlNode'}}
			results.Add(new Result(this, state.Index, literal.Length, m_input));						// {{value == 'void'}}
			results.Add(new Result(this, state.Index, literal.Length, m_input, default(VALUE)));	// {{value != 'void' and value != 'XmlNode'}}
			result = new State(state.Index + literal.Length, true, state.Errors);
		}
		else
		{
			result = new State(state.Index, false, ErrorSet.Combine(state.Errors, new ErrorSet(state.Index, literal)));
		}
		
		return result;
	}
	//> DoParseLiteral
	
	//< DoParseRange {{used-range}}
	private State DoParseRange(State state, List<Result> results, bool inverted, string chars, string ranges, UnicodeCategory[] categories, string label)
	{
		char ch = char.ToLower(m_input[state.Index]);		// {{ignore-case}}
		char ch = m_input[state.Index];							// {{not ignore-case}}
		
		bool matched = chars.IndexOf(ch) >= 0;
		for (int i = 0; i < ranges.Length && !matched; i += 2)
		{
			matched = ranges[i] <= ch && ch <= ranges[i + 1];
		}
		for (int i = 0; categories != null && i < categories.Length && !matched; ++i)
		{
			matched = char.GetUnicodeCategory(ch) == categories[i];
		}
		
		if (inverted)
			matched = !matched && ch != '\x0';
		
		if (matched)
		{
			results.Add(new Result(this, state.Index, 1, m_input, DoCreateTextNode(m_input.Substring(state.Index, 1), DoGetLine(state.Index), DoGetCol(state.Index)))); // {{value == 'XmlNode'}}
			results.Add(new Result(this, state.Index, 1, m_input));							// {{value == 'void'}}
			results.Add(new Result(this, state.Index, 1, m_input, default(VALUE)));		// {{value != 'void' and value != 'XmlNode'}}
			return new State(state.Index + 1, true, state.Errors);
		}
		
		return new State(state.Index, false, ErrorSet.Combine(state.Errors, new ErrorSet(state.Index, label)));
	}
	//> DoParseRange
	
	//< DoRepetition {{used-repetition}}
	private State DoRepetition(State state, List<Result> results, int min, int max, ParseMethod method)
	{
		State start = state;
		
		int count = 0;
		while (count <= max)
		{
			State temp = method(state, results);
			if (temp.Parsed && temp.Index > state.Index)
			{
				state = temp;
				++count;
			}
			else
			{
				state = new State(state.Index, true, ErrorSet.Combine(state.Errors, temp.Errors));
				break;
			}
		}
		
		if (count < min || count > max)
			state = new State(start.Index, false, ErrorSet.Combine(start.Errors, state.Errors));
		
		return state;
	}
	//> DoRepetition
	
	//< DoSequence {{used-sequence}}
	private State DoSequence(State state, List<Result> results, params ParseMethod[] methods)
	{
		State start = state;
		int startResult = results.Count;
		
		foreach (ParseMethod method in methods)
		{
			State temp = method(state, results);
			if (temp.Parsed)
			{
				state = temp;
			}
			else
			{
				state = new State(start.Index, false, ErrorSet.Combine(start.Errors, temp.Errors));
				results.RemoveRange(startResult, results.Count - startResult);
				break;
			}
		}
		
		return state;
	}
	//> DoSequence
	
	//< DoThrow {{unconsumed == 'error'}}
	private void DoThrow(int index, string format, params object[] args)
	{
		int line = DoGetLine(index);
		int col = DoGetCol(index) + 1;	// editors seem to usually use 1-based cols so that's what we will report
		
		// We need this retarded if or string.Format will throw an error if it
		// gets a format string like "Expected { or something".
		if (args != null && args.Length > 0)
			throw new ParserException(line, col, index, m_file, m_input, DoEscapeAll(string.Format(format, args)));
		else
			throw new ParserException(line, col, index, m_file, m_input, DoEscapeAll(format));
	}
	//> DoThrow
	#endregion
	
	#region Private Types
	//< CacheKey
	private struct CacheKey : IEquatable<CacheKey>
	{
		public CacheKey(string rule, int index, object context)
		{
			m_rule = rule;
			m_index = index;
			m_context = context;
		}
		
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			
			if (GetType() != obj.GetType())
				return false;
			
			CacheKey rhs = (CacheKey) obj;
			return this == rhs;
		}
		
		public bool Equals(CacheKey rhs)
		{
			return this == rhs;
		}
		
		public static bool operator==(CacheKey lhs, CacheKey rhs)
		{
			if (lhs.m_rule != rhs.m_rule)
				return false;
			
			if (lhs.m_index != rhs.m_index)
				return false;
			
			if (lhs.m_context != rhs.m_context)
				return false;
			
			return true;
		}
		
		public static bool operator!=(CacheKey lhs, CacheKey rhs)
		{
			return !(lhs == rhs);
		}
		
		public override int GetHashCode()
		{
			int hash = 0;
			
			unchecked
			{
				hash += m_rule.GetHashCode();
				hash += m_index.GetHashCode();
				hash += m_context.GetHashCode();
			}
			
			return hash;
		}
		
		private string m_rule;
		private int m_index;
		private object m_context;
	}
	//> CacheKey
	
	//< CacheValue
	private struct CacheValue
	{
		public CacheValue(State state/*, VALUE value {{value != 'void'}}*/, bool hasResult)	
		{
			State = state;
			Value = value;				// {{value != 'void'}}
			HasResult = hasResult;
		}
		
		public State State;
		public VALUE Value;			// {{value != 'void'}}
		public bool HasResult;
	}
	//> CacheValue
	
	private delegate State ParseMethod(State state, List<Result> results);
	
	//< ErrorSet
	// These are either an error that caused parsing to fail or the reason a
	// successful parse stopped.
	private struct ErrorSet
	{
		public ErrorSet(int index, string expected)
		{
			Index = index;
			Expected = new string[]{expected};
		}
		
		public ErrorSet(int index, string[] expected)
		{
			Index = index;
			Expected = expected;
		}
		
		// The location associated with the errors. For a failed parse this will be the
		// same as State.Index. For a successful parse it will be State.Index or later.
		public int Index;
		
		// This will be the name of something which was expected, but not found.
		public string[] Expected;
		
		public static ErrorSet Combine(ErrorSet lhs, ErrorSet rhs)
		{
			if (lhs.Index > rhs.Index)
			{
				return lhs;
			}
			else if (lhs.Index < rhs.Index)
			{
				return rhs;
			}
			else
			{
				List<string> errors = new List<string>(lhs.Expected.Length + rhs.Expected.Length);
				errors.AddRange(lhs.Expected);
				foreach (string err in rhs.Expected)
				{
					if (errors.IndexOf(err) < 0)
						errors.Add(err);
				}
				return new ErrorSet(lhs.Index, errors.ToArray());
			}
		}
		
		public override string ToString()
		{
			if (Expected.Length > 0)
				return string.Format("Expected {0}", string.Join(" or ", Expected));
			else
				return "<none>";
		}
	}
	//> ErrorSet
	
	//< State
	// The state of the parser.
	private struct State
	{
		public State(int index, bool parsed)
		{
			Index = index;
			Parsed = parsed;
			Errors = new ErrorSet(index, new string[0]);
		}
		
		public State(int index, bool parsed, ErrorSet errors)
		{
			Index = index;
			Parsed = parsed;
			Errors = errors;
		}
		
		// Index of the first unconsumed character.
		public int Index;
		
		// True if the expression associated with the state successfully parsed.
		public bool Parsed;
		
		// If Parsed is false then this will explain why. If Parsed is true it will
		// say why the parse stopped.
		public ErrorSet Errors;
	}
	//> State
	
	//< Result
	// The result of parsing a literal or non-terminal.
	private struct Result
	{
		public Result(PARSER parser, int index, int length, string input/*, VALUE value {{value != 'void'}}*/)
		{
			m_parser = parser;
			m_index = index;
			m_length = length;
			m_input = input;
			Value = value;	// {{value != 'void'}}
		}
		
		// The text which was parsed by the terminal or non-terminal.
		public string Text {get {return m_input.Substring(m_index, m_length);}}
		
		// The 0-based character index the (non)terminal started on.
		public int Index {get {return m_index;}}
		
		// The 1-based line number the (non)terminal started on.
		public int Line {get {return m_parser.DoGetLine(m_index);}}
		
		// The 1-based column number the (non)terminal started on.
		public int Col {get {return m_parser.DoGetCol(m_index);}}
			
		// For non-terminals this will be the result of the semantic action, 	// {{value != 'void'}}
		// otherwise it will be the default value.									// {{value != 'void'}}
		public VALUE Value;														// {{value != 'void'}}
		
		private PARSER m_parser;
		private int m_index;
		private int m_length;
		private string m_input;
	}
	//> Result
	#endregion
	
	#region Fields
	private string m_input;
	private string m_file;
	private object m_context = 0;
	private Dictionary<string, ParseMethod[]> m_nonterminals = new Dictionary<string, ParseMethod[]>();
	private Dictionary<CacheKey, CacheValue> m_cache = new Dictionary<CacheKey, CacheValue>();
	private int m_consumed;								// {{unconsumed == 'expose'}}
	private string m_debugFile /*= DEBUG-FILE*/;		// {{debugging and has-debug-file}}
	private XmlDocument m_doc;							// {{value == 'XmlNode'}}
	private List<int> m_lineStarts;		// offsets at which each line starts
	#endregion
}
/* CLOSE-NAMESPACE */
