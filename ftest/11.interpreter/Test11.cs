// Machine generated by peg-sharp 0.4.561.0 from Test11.peg.
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
	
	public ParserException(int line, int col, int offset, string file, string input, string message) : base(string.Format("{0} at line {1} col {2}{3}", message, line, col, file != null ? (" in " + file) : "."))
	{
	}
	
	[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
	private ParserException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}
}

// The start of a comment
// and the end.
internal sealed partial class Test11
{
	public Test11()
	{
		m_nonterminals.Add("Program", new ParseMethod[]{this.DoParseProgramRule});
		m_nonterminals.Add("Assignment", new ParseMethod[]{this.DoParseAssignmentRule});
		m_nonterminals.Add("If", new ParseMethod[]{this.DoParseIfRule});
		m_nonterminals.Add("Statement", new ParseMethod[]{this.DoParseStatementRule});
		m_nonterminals.Add("Statements", new ParseMethod[]{this.DoParseStatementsRule});
		m_nonterminals.Add("While", new ParseMethod[]{this.DoParseWhileRule});
		m_nonterminals.Add("Compare", new ParseMethod[]{this.DoParseCompareRule});
		m_nonterminals.Add("Expression", new ParseMethod[]{this.DoParseExpressionRule});
		m_nonterminals.Add("Identifier", new ParseMethod[]{this.DoParseIdentifierRule});
		m_nonterminals.Add("Sum", new ParseMethod[]{this.DoParseSumRule});
		m_nonterminals.Add("Product", new ParseMethod[]{this.DoParseProductRule});
		m_nonterminals.Add("Value", new ParseMethod[]{this.DoParseValue1Rule, this.DoParseValue2Rule, this.DoParseValue3Rule});
		m_nonterminals.Add("Keywords", new ParseMethod[]{this.DoParseKeywordsRule});
		m_nonterminals.Add("S", new ParseMethod[]{this.DoParseSRule});
		m_nonterminals.Add("Space", new ParseMethod[]{this.DoParseSpaceRule});
		OnCtorEpilog();
	}
	
	public Expression Parse(string input)
	{
		return DoParseFile(input, null, "Program");
	}
	
	// File is used for error reporting.
	public Expression Parse(string input, string file)
	{
		return DoParseFile(input, file, "Program");
	}
	
	#region Non-Terminal Parse Methods
	// Program := S Statements
	private State DoParseProgramRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Statements");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Assignment := Identifier '=' S Expression
	private State DoParseAssignmentRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoParse(s, r, "Identifier");},
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, "=");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Expression");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			value = new AssignmentExpression(results[0].Text.Trim(), results[2].Value);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// If := 'if' S Expression 'then' S Statements 'else' S Statements 'end' S
	private State DoParseIfRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, "if");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Expression");},
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, "then");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Statements");},
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, "else");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Statements");},
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, "end");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			value = new IfExpression(results[1].Value, results[3].Value, results[5].Value);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Statement := If / While / Assignment / Expression
	private State DoParseStatementRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoChoice(_state, results,
			delegate (State s, List<Result> r) {return DoParse(s, r, "If");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "While");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Assignment");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Expression");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Statements := Statement+
	private State DoParseStatementsRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoRepetition(_state, results, 1, 2147483647,
			delegate (State s, List<Result> r) {return DoParse(s, r, "Statement");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			value = new SequenceExpression(from r in results where r.Value != null select r.Value);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// While := 'while' S Expression 'do' S Statements 'end' S
	private State DoParseWhileRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, "while");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Expression");},
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, "do");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Statements");},
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, "end");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			value = new WhileExpression(results[1].Value, results[3].Value);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Compare := Sum (('<=' / '>=' / '==' / '!=' / '<' / '>') S Sum)?
	private State DoParseCompareRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoParse(s, r, "Sum");},
			delegate (State s, List<Result> r) {return DoRepetition(s, r, 0, 1,
				delegate (State s2, List<Result> r2) {return DoSequence(s2, r2,
					delegate (State s3, List<Result> r3) {return DoChoice(s3, r3,
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, "<=");},
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, ">=");},
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, "==");},
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, "!=");},
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, "<");},
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, ">");});},
					delegate (State s3, List<Result> r3) {return DoParse(s3, r3, "S");},
					delegate (State s3, List<Result> r3) {return DoParse(s3, r3, "Sum");});});});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			if (results.Count > 1) value = DoCreateBinary(results);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Expression := Compare
	private State DoParseExpressionRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoParse(_state, results, "Compare");
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Identifier := !Keywords [a-zA-Z] [a-zA-Z0-9]* S
	private State DoParseIdentifierRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoNAssert(s, r,
				delegate (State s2, List<Result> r2) {return DoParse(s2, r2, "Keywords");});},
			delegate (State s, List<Result> r) {return DoParseRange(s, r, false, string.Empty, "azAZ", null, "[a-zA-Z]");},
			delegate (State s, List<Result> r) {return DoRepetition(s, r, 0, 2147483647,
				delegate (State s2, List<Result> r2) {return DoParseRange(s2, r2, false, string.Empty, "azAZ09", null, "[a-zA-Z0-9]");});},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			string text = m_input.Substring(_start.Index, _state.Index - _start.Index);
			value = new VariableExpression(text.Trim());
			if (text != null)
				_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		else
		{
			string expected = null;
			expected = "variable";
			if (expected != null)
				_state = new State(_start.Index, false, ErrorSet.Combine(_start.Errors, new ErrorSet(_state.Errors.Index, expected)));
		}
		
		return _state;
	}
	
	// Sum := Product (('+' / '-') S Product)*
	private State DoParseSumRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoParse(s, r, "Product");},
			delegate (State s, List<Result> r) {return DoRepetition(s, r, 0, 2147483647,
				delegate (State s2, List<Result> r2) {return DoSequence(s2, r2,
					delegate (State s3, List<Result> r3) {return DoChoice(s3, r3,
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, "+");},
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, "-");});},
					delegate (State s3, List<Result> r3) {return DoParse(s3, r3, "S");},
					delegate (State s3, List<Result> r3) {return DoParse(s3, r3, "Product");});});});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			value = DoCreateBinary(results);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Product := Value (('*' / '/') S Value)*
	private State DoParseProductRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoParse(s, r, "Value");},
			delegate (State s, List<Result> r) {return DoRepetition(s, r, 0, 2147483647,
				delegate (State s2, List<Result> r2) {return DoSequence(s2, r2,
					delegate (State s3, List<Result> r3) {return DoChoice(s3, r3,
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, "*");},
						delegate (State s4, List<Result> r4) {return DoParseLiteral(s4, r4, "/");});},
					delegate (State s3, List<Result> r3) {return DoParse(s3, r3, "S");},
					delegate (State s3, List<Result> r3) {return DoParse(s3, r3, "Value");});});});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			value = DoCreateBinary(results);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Value := [0-9]+ S
	private State DoParseValue1Rule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoRepetition(s, r, 1, 2147483647,
				delegate (State s2, List<Result> r2) {return DoParseRange(s2, r2, false, string.Empty, "09", null, "[0-9]");});},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			string fatal = null;
			string text = m_input.Substring(_start.Index, _state.Index - _start.Index);
				int n;	if (!int.TryParse(text.Trim(), out n))		fatal = "Value is oor";	value = new LiteralExpression(n);
			if (!string.IsNullOrEmpty(fatal))
				DoThrow(_start.Index, fatal);
			if (text != null)
				_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		else
		{
			string expected = null;
			expected = "number";
			if (expected != null)
				_state = new State(_start.Index, false, ErrorSet.Combine(_start.Errors, new ErrorSet(_state.Errors.Index, expected)));
		}
		
		return _state;
	}
	
	// Value := Identifier
	private State DoParseValue2Rule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoParse(_state, results, "Identifier");
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Value := '(' Expression ')' S
	private State DoParseValue3Rule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, "(");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "Expression");},
			delegate (State s, List<Result> r) {return DoParseLiteral(s, r, ")");},
			delegate (State s, List<Result> r) {return DoParse(s, r, "S");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			value = results[1].Value;
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		else
		{
			string expected = null;
			expected = "parenthesized expression";
			if (expected != null)
				_state = new State(_start.Index, false, ErrorSet.Combine(_start.Errors, new ErrorSet(_state.Errors.Index, expected)));
		}
		
		return _state;
	}
	
	// Keywords := ('do' / 'else' / 'end' / 'if' / 'then' / 'while') ![a-zA-Z0-9]
	private State DoParseKeywordsRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoSequence(_state, results,
			delegate (State s, List<Result> r) {return DoChoice(s, r,
				delegate (State s2, List<Result> r2) {return DoParseLiteral(s2, r2, "do");},
				delegate (State s2, List<Result> r2) {return DoParseLiteral(s2, r2, "else");},
				delegate (State s2, List<Result> r2) {return DoParseLiteral(s2, r2, "end");},
				delegate (State s2, List<Result> r2) {return DoParseLiteral(s2, r2, "if");},
				delegate (State s2, List<Result> r2) {return DoParseLiteral(s2, r2, "then");},
				delegate (State s2, List<Result> r2) {return DoParseLiteral(s2, r2, "while");});},
			delegate (State s, List<Result> r) {return DoNAssert(s, r,
				delegate (State s2, List<Result> r2) {return DoParseRange(s2, r2, false, string.Empty, "azAZ09", null, "[a-zA-Z0-9]");});});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// S := Space*
	private State DoParseSRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoRepetition(_state, results, 0, 2147483647,
			delegate (State s, List<Result> r) {return DoParse(s, r, "Space");});
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			string text = m_input.Substring(_start.Index, _state.Index - _start.Index);
			text = null;
			if (text != null)
				_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		
		return _state;
	}
	
	// Space := [ \t\r\n]
	private State DoParseSpaceRule(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		
		_state = DoParseRange(_state, results, false, " \t\r\n", string.Empty, null, "[ \t\r\n]");
		
		if (_state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : default(Expression);
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));
		}
		else
		{
			string expected = null;
			expected = "whitespace";
			if (expected != null)
				_state = new State(_start.Index, false, ErrorSet.Combine(_start.Errors, new ErrorSet(_state.Errors.Index, expected)));
		}
		
		return _state;
	}
	#endregion
	
	#region Private Helper Methods
	partial void OnCtorEpilog();
	partial void OnParseProlog();
	partial void OnParseEpilog(State state);
	
	private Expression DoParseFile(string input, string file, string rule)
	{
		m_file = file;
		m_input = m_file;				// we need to ensure that m_file is used or we will (in some cases) get a compiler warning
		m_input = input + "\x0";	// add a sentinel so we can avoid range checks
		m_cache.Clear();
		
		State state = new State(0, true);
		List<Result> results = new List<Result>();
		
		OnParseProlog();
		state = DoParse(state, results, rule);
		
		int i = state.Index;
		if (!state.Parsed)
			DoThrow(state.Errors.Index, state.Errors.ToString());
		else if (i < input.Length)
			if (state.Errors.Expected.Length > 0)
				DoThrow(state.Errors.Index, state.Errors.ToString());
			else
				DoThrow(state.Errors.Index, "Not all input was consumed starting from '" + input.Substring(i, Math.Min(16, input.Length - i)) + "'");
		OnParseEpilog(state);
		
		return results[0].Value;
	}
	
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
	
	// This is normally only used for error handling so it doesn't need to be too
	// fast. If it somehow does become a bottleneck for some parsers they can
	// replace it with the custom-methods setting.
	private int DoGetLine(int index)
	{
		int line = 1;
		
		int i = 0;
		while (i < index)
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
		
		return start - index;
	}
	
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
	
	private State DoParseLiteral(State state, List<Result> results, string literal)
	{
		int j = state.Index;
		
		for (int i = 0; i < literal.Length; ++i)
		{
			if (char.ToLower(m_input[j + i]) != literal[i])
			{
				return new State(state.Index, false, ErrorSet.Combine(state.Errors, new ErrorSet(state.Index, literal)));
			}
		}
		
		int k = j + literal.Length;
		
		results.Add(new Result(this, j, literal.Length, m_input, default(Expression)));
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
			Expression value = hasResult ? results[results.Count - 1].Value : default(Expression);
			cache = new CacheValue(state, value, hasResult);
			m_cache.Add(key, cache);
		}
		else
		{
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
	
	private State DoParseRange(State state, List<Result> results, bool inverted, string chars, string ranges, UnicodeCategory[] categories, string label)
	{
		char ch = char.ToLower(m_input[state.Index]);
		
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
			results.Add(new Result(this, state.Index, 1, m_input, default(Expression)));
			return new State(state.Index + 1, true, state.Errors);
		}
		
		return new State(state.Index, false, ErrorSet.Combine(state.Errors, new ErrorSet(state.Index, label)));
	}
	
	private State DoNAssert(State state, List<Result> results, ParseMethod method)
	{
		State temp = method(state, results);
		
		state = new State(state.Index, !temp.Parsed, state.Errors);
		
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
		public CacheValue(State state, Expression value, bool hasResult)
		{
			State = state;
			Value = value;
			HasResult = hasResult;
		}
		
		public State State;
		
		public Expression Value;
		
		public bool HasResult;
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
	
	// The result of parsing a literal or non-terminal.
	private struct Result
	{
		public Result(Test11 parser, int index, int length, string input, Expression value)
		{
			m_parser = parser;
			m_index = index;
			m_length = length;
			m_input = input;
			Value = value;
		}
		
		// The text which was parsed by the terminal or non-terminal.
		public string Text {get {return m_input.Substring(m_index, m_length);}}
		
		// The 0-based character index the (non)terminal started on.
		public int Index {get {return m_index;}}
		
		// The 1-based line number the (non)terminal started on.
		public int Line {get {return m_parser.DoGetLine(m_index);}}
		
		// The 1-based column number the (non)terminal started on.
		public int Col {get {return m_parser.DoGetCol(m_index);}}
		
		// For non-terminals this will be the result of the semantic action, 
		// otherwise it will be the default value.
		public Expression Value;
		
		private Test11 m_parser;
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
	#endregion
}
