// Machine generated by peg-sharp 0.3.384.0 from Test14.peg.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;

[Serializable]
internal sealed class ParserException : Exception
{
	public ParserException()
	{
	}
	
	public ParserException(string message) : base(message)
	{
	}
	
	public ParserException(int line, int col, string file, string message) : base(string.Format("{0} at line {1} col {2}{3}", message, line, col, file != null ? (" in " + file) : "."))
	{
	}
	
	public ParserException(int line, int col, string file, string format, params object[] args) : this(line, col, file, string.Format(format, args))
	{
	}
	
	public ParserException(int line, int col, string file, string message, Exception inner) : base(string.Format("{0} at line {1} col {2}{3}", message, line, col, file != null ? (" in " + file) : "."), inner)
	{
	}
	
	[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
	private ParserException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}
}

// Thread safe if Parser instances are not shared across threads.
internal sealed partial class Test14
{
	public Test14()
	{
		m_nonterminals.Add("Expr", new ParseMethod[]{this.DoParseExprRule});
		OnCtorEpilog();
	}
	
	public double Parse(string input)
	{
		return DoParseFile(input, null);
	}
	
	// File is used for error reporting.
	public double Parse(string input, string file)
	{
		return DoParseFile(input, file);
	}
	
	#region Non-Terminal Parse Methods
	// Expr := 'x'+
	private State DoParseExprRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		var results = new List<Result>();
		
		_state = DoRepetition(_state, results, 1, 2147483647,
			(s, r) => DoParseLiteral(s, r, "x"));
		
		if (_state.Parsed)
		{
			double value = results.Count > 0 ? results[0].Value : default(double);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	#endregion
	
	#region Private Helper Methods
	partial void OnCtorEpilog();
	partial void OnParseProlog();
	partial void OnParseEpilog(State state);
	
	private double DoParseFile(string input, string file)
	{
		DoDebugWrite(new string('-', 32));
		if (!string.IsNullOrEmpty(file))
			DoDebugWrite(file);
		
		m_file = file;
		m_input = m_file;				// we need to ensure that m_file is used or we will (in some cases) get a compiler warning
		m_input = input + "\x0";	// add a sentinel so we can avoid range checks
		m_cache.Clear();
		
		State state = new State(0, true);
		var results = new List<Result>();
		
		OnParseProlog();
		state = DoParse(state, results, "Expr");
		
		OnParseEpilog(state);
		
		return results[0].Value;
	}
	
	public string DoEscapeAll(string s)
	{
		var builder = new System.Text.StringBuilder(s.Length);
		
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
	
	private void DoDebugWrite(string format, params object[] args)
	{
		Console.Write(new string(' ', 4*m_debugLevel));
		Console.WriteLine(DoEscapeAll(string.Format(format, args)));
	}
	
	private string DoTruncateString(string str)
	{
		if (str.Length > 48)
			return str.Substring(0, 24) + "..." + str.Substring(str.Length - 24);
		else if (str.Length > 0 && str[str.Length - 1] == '\x0')
			return str.Substring(0, str.Length - 1);
		else
			return str;
	}
	
	// This is normally only used for error handling so it doesn't need to be too
	// fast. If it somehow does become a bottleneck for some parsers they can
	// replace it with the custom-methods setting.
	private int DoGetLine(int index)
	{
		int line = 1;
		
		int i = 0;
		while (i <= index)
		{
			char ch = m_input[i++];
			
			if (ch == '\r' && m_input[i] == '\n')
			{
				++i;
				++line;
			}
			else if (ch == '\r')
			{
				++line;
			}
			else if (ch == '\n')
			{
				++line;
			}
		}
		
		return line;
	}
	
	private int DoGetCol(int index)
	{
		int start = index;
		
		while (index > 0 && m_input[index - 1] != '\n' && m_input[index - 1] != '\r')
		{
			--index;
		}
		
		return start - index + 1;
	}
	
	private State DoParseLiteral(State state, List<Result> results, string literal)
	{
		int j = state.Index;
		
		for (int i = 0; i < literal.Length; ++i)
		{
			if (m_input[j + i] != literal[i])
			{
				return new State(state.Index, false, ErrorSet.Combine(state.Errors, new ErrorSet(state.Index, literal)));
			}
		}
		
		int k = j + literal.Length;
		
		results.Add(new Result(this, j, literal.Length, m_input, default(double)));
		state = new State(k, true, state.Errors);
		
		return state;
	}
	
	private State DoParse(State state, List<Result> results, string nonterminal)
	{
		State start = state;
		
		CacheValue cache;
		CacheKey key = new CacheKey(nonterminal, start.Index);
		if (!m_cache.TryGetValue(key, out cache))
		{
			ParseMethod[] methods = m_nonterminals[nonterminal];
			
			int oldCount = results.Count;
			state = DoChoice(state, results, methods);
			
			bool hasResult = state.Parsed && results.Count > oldCount;
			double value = hasResult ? results[results.Count - 1].Value : default(double);
			cache = new CacheValue(state, value, hasResult);
			m_cache.Add(key, cache);
		}
		else
		{
			if (m_debug[0] == "*" || Array.IndexOf(m_debug, nonterminal) >= 0)
			{
				DoDebugWrite("cached {0}", nonterminal);
				++m_debugLevel;
				if (cache.State.Parsed)
					DoDebugWrite("{0} parsed: {1}", nonterminal, DoTruncateString(m_input.Substring(start.Index, cache.State.Index - start.Index)));
				else
					DoDebugWrite("{0} failed: {1} at line {2} col {3}", nonterminal, cache.State.Errors, DoGetLine(cache.State.Errors.Index), DoGetCol(cache.State.Errors.Index));
				--m_debugLevel;
			}
			
			if (cache.HasResult)
				results.Add(new Result(this, start.Index, cache.State.Index - start.Index, m_input, cache.Value));
		}
		
		return cache.State;
	}
	
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
	#endregion
	
	#region Private Types
	private struct CacheKey : IEquatable<CacheKey>
	{
		public CacheKey(string rule, int index)
		{
			m_rule = rule;
			m_index = index;
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
			}
			
			return hash;
		}
		
		private string m_rule;
		private int m_index;
	}
	
	private struct CacheValue
	{
		public CacheValue(State state, double value, bool hasResult)
		{
			State = state;
			Value = value;
			HasResult = hasResult;
		}
		
		public State State {get; private set;}
		
		public double Value {get; private set;}
		
		public bool HasResult {get; private set;}
	}
	
	private delegate State ParseMethod(State state, List<Result> results);
	
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
		public int Index {get; private set;}
		
		// This will be the name of something which was expected, but not found.
		public string[] Expected {get; private set;}
		
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
				var errors = new List<string>(lhs.Expected.Length + rhs.Expected.Length);
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
		public int Index {get; private set;}
		
		// True if the expression associated with the state successfully parsed.
		public bool Parsed {get; private set;}
		
		// If Parsed is false then this will explain why. If Parsed is true it will
		// say why the parse stopped.
		public ErrorSet Errors {get; private set;}
	}
	
	// The result of parsing a literal or non-terminal.
	private struct Result
	{
		public Result(Test14 parser, int index, int length, string input, double value)
		{
			m_parser = parser;
			m_index = index;
			m_length = length;
			m_input = input;
			Value = value;
		}
		
		// The text which was parsed by the terminal or non-terminal.
		public string Text {get {return m_input.Substring(m_index, m_length);}}
		
		// The 1-based line number the (non)terminal started on.
		public int Line {get {return m_parser.DoGetLine(m_index);}}
		
		// The 1-based column number the (non)terminal started on.
		public int Col {get {return m_parser.DoGetCol(m_index);}}
		
		// For non-terminals this will be the result of the semantic action, 
		// otherwise it will be the default value.
		public double Value {get; private set;}
		
		private Test14 m_parser;
		private int m_index;
		private int m_length;
		private string m_input;
	}
	
	#endregion
	
	#region Fields
	private string m_input;
	private string m_file;
	private Dictionary<string, ParseMethod[]> m_nonterminals = new Dictionary<string, ParseMethod[]>();
	private Dictionary<CacheKey, CacheValue> m_cache = new Dictionary<CacheKey, CacheValue>();
	private int m_debugLevel;
	private string[] m_debug = new string[]{"false"};
	#endregion
}
