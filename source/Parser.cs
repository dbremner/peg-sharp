//#define DUMP
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
using System.Diagnostics;
using System.Linq;

// Parses a peg file and generates a Grammar object.
internal sealed class Parser
{
	public Grammar Parse(string input)
	{
		Contract.Requires(input != null, "input is null");
		
#if DUMP		
		DoDebugWrite(new string('-', 32));
		DoDebugWrite("input: {0}", DoTruncateString(input));
#endif
		
		m_grammar = new Grammar();
		
		m_input = input + "\x0";		// add a sentinel so we can avoid range checks
		State state = new State(0, true);
		var results = new List<Result>();
		
		state = DoParsePegFileRule(state, results);
		int i = DoSkipWhiteSpace(state.Index);
		
		if (i < input.Length || input.Length == 0)
			DoThrow(state.Errors.Index, state.Errors.ToString());
		
		return m_grammar;
	}
	
	#region Non-terminal Methods
	private State DoParseActionRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("Action := '`' [^`]+ '`'", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			(s, p) => DoParseLiteral(s, p, "`"),
			(s, p) => DoRepetition(s, p, (s2, p2) => DoParseInvertedRange(s2, p2, "`")),
			(s, p) => DoParseLiteral(s, p, "`"));
		
		if (state.Parsed)
		{
			Expression value = null;
			string action = m_input.Substring(start.Index, state.Index - start.Index).Trim();
			outResults.Add(new Result(action.Substring(1, action.Length - 2).Trim(), value));
		}
		
#if DUMP		
		DoDebugEpilog("Action", start, state);
#endif
		
		return state;
	}
	
	private State DoParseAssertRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("AssertExpression := ('&' / '!')? RepeatExpression", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			(s, p) => DoOptional(s, p,
				(s2, p2) => DoChoice(s2, p2,
					(s3, p3) => DoParseLiteral(s3, p3, "&"),
					(s3, p3) => DoParseLiteral(s3, p3, "!"))),
			this.DoParsePostfixRule);
		
		if (state.Parsed)
		{
			Expression value = null;
			if (results.Count == 1)
				value = results[0].Value;
			else if (results.Count > 1)
				if (results[0].Text == "&")
					value = new AssertExpression(results[1].Value);
				else
					value = new NAssertExpression(results[1].Value);
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("AssertExpression", start, state);
#endif
		
		return state;
	}
	
	private State DoParseCommentRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("Comment := '#' [^\n\r]*", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results, (s, p) => DoParseLiteral(s, p, "#"), this.DoParseToEol);
		
		if (state.Parsed)
		{
			Expression value = results.Count > 0 ? results[0].Value : null;
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("Comment", start, state);
#endif
		
		return state;
	}
	
	private State DoParseExpressionRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("Expression := SequenceExpression ('/' S SequenceExpression)*", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			this.DoParseSequenceRule,
			(s, p) => DoRepetition(s, p, (s2, p2) => DoSequence(s2, p2,
				(s3, p3) => DoParseLiteral(s3, p3, "/"), this.DoParseSequenceRule)));
		
		if (state.Parsed)
		{
			Expression value = null;
			if (results.Count == 1)
				value = results[0].Value;
			else if (results.Count > 1)
				value = new ChoiceExpression((from r in results where r.Value != null select r.Value).ToArray());
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("Expression", start, state);
#endif
		
		return state;
	}
	
	private State DoParseIdentifierRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("Identifier := [a-zA-Z] [a-zA-Z0-9_-]*", state);
		State start = state;
#endif
		
		int i = DoSkipWhiteSpace(state.Index);
		int len = 0;
		char ch = m_input[i];
		if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'))
		{
			while ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '_' || ch == '-')
			{
				++len;
				ch = m_input[i + len];
			}
		}
		var err = new ErrorSet(i + len, len > 0 ? "identifier suffix" : "identifier");
		state = new State(i + len, len > 0, ErrorSet.Combine(state.Errors, err));
		
		if (state.Parsed)
		{
			Expression value = null;
			outResults.Add(new Result(m_input.Substring(i, len), value));
		}
		
#if DUMP		
		DoDebugEpilog("Identifier", start, state);
#endif
		
		return state;
	}
	
	private State DoParseIntegerRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("Integer := [0-9]+", state);
		State start = state;
#endif
		
		int i = DoSkipWhiteSpace(state.Index);
		int len = 0;
		char ch = m_input[i];
		while (ch >= '0' && ch <= '9')
		{
			++len;
			ch = m_input[i + len];
		}
		var err = new ErrorSet(i + len, "integer");
		state = new State(i + len, len > 0, ErrorSet.Combine(state.Errors, err));
		
		if (state.Parsed)
		{
			Expression value = null;
			outResults.Add(new Result(m_input.Substring(i, len), value));
		}
		
#if DUMP		
		DoDebugEpilog("Integer", start, state);
#endif
		
		return state;
	}
	
	private State DoParseLiteralRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("Literal := '\'' ( [^'] / '\\\'')+ '\''", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			(s, p) => DoParseLiteral(s, p, "'"),
			(s, p) => DoRepetition(s, p,
				(s2, p2) => DoChoice(s2, p2,
					(s3, p3) => DoParseLiteral(s3, p3, "\\'"),
					(s3, p3) => DoParseInvertedRange(s3, p3, "'"))),
			(s, p) => DoParseLiteral(s, p, "'"));
		
		if (state.Parsed)
		{
			string text = m_input.Substring(start.Index, state.Index - start.Index).Trim();
			text = text.Substring(1, text.Length - 2);
			
			Expression value = new LiteralExpression(text);
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("Literal", start, state);
#endif		
		return state;
	}
	
	private State DoParsePegFileRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("PegFile := (Setting / Comment)+ (Rule / Comment)+", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			(s, p) => DoPositiveRepetition(s, p, (s2, p2) => DoChoice(s2, p2, this.DoParseSettingRule, this.DoParseCommentRule)),
			(s, p) => DoPositiveRepetition(s, p, (s2, p2) => DoChoice(s2, p2, this.DoParseRuleRule, this.DoParseCommentRule)));
		
		if (state.Parsed)
		{
			if (!m_grammar.Settings.ContainsKey("start"))
				throw new ParserException("Missing required setting 'start'.");
			if (!m_grammar.Settings.ContainsKey("value"))
				throw new ParserException("Missing required setting 'value'.");
			if (!m_grammar.Rules.Exists(r => r.Name == m_grammar.Settings["start"]))
				throw new ParserException(string.Format("Missing the start rule '{0}'.", m_grammar.Settings["start"]));
			
			Expression value = results[0].Value;
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("PegFile", start, state);
#endif
		
		return state;
	}
	
	private State DoParsePostfixRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("PostfixExpression := PostfixExpression1 / PostfixExpression2 / PostfixExpression3 / PostfixExpression4", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoChoice(state, results,
			this.DoParsePostfixRule1,
			this.DoParsePostfixRule2,
			this.DoParsePostfixRule3,
			this.DoParsePostfixRule4);
		
		if (state.Parsed)
		{
			Expression value = null;
			value = results[0].Value;
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("PostfixExpression", start, state);
#endif
		
		return state;
	}
	
	private State DoParsePostfixRule1(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("PostfixExpression1 := PrimitiveExpression '{' S Integer ',' S Integer '}' S", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			this.DoParsePrimitiveRule,
			(s, p) => DoParseLiteral(s, p, "{"),
			this.DoParseIntegerRule,
			(s, p) => DoParseLiteral(s, p, ","),
			this.DoParseIntegerRule,
			(s, p) => DoParseLiteral(s, p, "}"));
		
		if (state.Parsed)
		{
			Expression value = null;
			int min = int.Parse(results[2].Text);
			int max = int.Parse(results[4].Text);
			value = new RepetitionExpression(results[0].Value, min, max);
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("PostfixExpression1", start, state);
#endif
		
		return state;
	}
	
	private State DoParsePostfixRule2(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("PostfixExpression2 := PrimitiveExpression '{' S Integer ',' S '}' S", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			this.DoParsePrimitiveRule,
			(s, p) => DoParseLiteral(s, p, "{"),
			this.DoParseIntegerRule,
			(s, p) => DoParseLiteral(s, p, ","),
			(s, p) => DoParseLiteral(s, p, "}"));
		
		if (state.Parsed)
		{
			Expression value = null;
			int min = int.Parse(results[2].Text);
			value = new RepetitionExpression(results[0].Value, min, int.MaxValue);
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("PostfixExpression2", start, state);
#endif
		
		return state;
	}
	
	private State DoParsePostfixRule3(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("PostfixExpression3 := PrimitiveExpression ('*' / '+' / '?') S", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			this.DoParsePrimitiveRule,
			(s, p) => DoChoice(s, p,
				(s2, p2) => DoParseLiteral(s2, p2, "*"),
				(s2, p2) => DoParseLiteral(s2, p2, "+"),
				(s2, p2) => DoParseLiteral(s2, p2, "?")));
		
		if (state.Parsed)
		{
			Expression value = null;
			if (results[1].Text == "*")
				value = new RepetitionExpression(results[0].Value, 0, int.MaxValue);
			else if (results[1].Text == "+")
				value = new RepetitionExpression(results[0].Value, 1, int.MaxValue);
			else
				value = new RepetitionExpression(results[0].Value, 0, 1);
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("PostfixExpression3", start, state);
#endif
		
		return state;
	}
	
	private State DoParsePostfixRule4(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("PostfixExpression4 := PrimitiveExpression", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoParsePrimitiveRule(state, results);
		
		if (state.Parsed)
		{
			Expression value = null;
			value = results[0].Value;
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("PostfixExpression4", start, state);
#endif
		
		return state;
	}
	
	private State DoParsePrimitiveRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("PrimitiveExpression := '.' / Literal / Range / Identifier / SubExpression", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoChoice(state, results,
			(s, p) => DoParseLiteral(s, p, "."),
			this.DoParseLiteralRule,
			this.DoParseRangeRule,
			this.DoParseIdentifierRule,
			this.DoParseSubExpressionRule);
		
		if (state.Parsed)
		{
			Expression value = null;
			if (results[0].Text == ".")
			{
				value = new RangeExpression("\x0001-\xFFFF");
			}
			else
			{
				value = results[0].Value;
				if (value == null)
					value = new RuleExpression(results[0].Text);
			}
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("PrimitiveExpression", start, state);
#endif
		
		return state;
	}
	
	private State DoParseRangeRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("Range := '[' ( [^\\]] / '\\]') ']'", state);
#endif
		
		State start = state;
		
		int i = DoSkipWhiteSpace(state.Index);
		int j = i;
		
		string expected = null;
		if (m_input[j] == '[')
		{
			++j;
			while (m_input[j] != ']' && m_input[j] != '\x0')
			{
				if (m_input[j] == '\\' && j + 1 < m_input.Length)
					j += 2;
				else
					j += 1;
			}
			
			if (m_input[j] == ']')
			{
				++j;
//				if (j - i - 2 == 0)
//					expected = "[^\\]]";
			}
			else
				expected = "]";
		}
		else
			expected = "[";
		
		if (expected == null)
		{
			state = new State(j, true, state.Errors);
			
			string text = m_input.Substring(i + 1, j - i - 2).Replace("\\]", "]");
			if (string.IsNullOrEmpty(text))
				DoThrow(start.Index, "ranges cannot be empty.");
			if (text == "^")
				DoThrow(start.Index, "inverted ranges cannot be empty.");
			
			Expression value = null;
			try
			{
				value = new RangeExpression(text);
			}
			catch (Exception e)
			{
				DoThrow(start.Index, e.Message);
			}
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		else
		{
			state = new State(i, false, ErrorSet.Combine(state.Errors, new ErrorSet(j, expected)));
		}
		
#if DUMP		
		DoDebugEpilog("Range", start, state);
#endif	
		
		return state;
	}
	
	private State DoParseRuleRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("Rule := Identifier ':='  Expression (';' / Action S Action?)", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			this.DoParseIdentifierRule,
			(s, p) => DoParseLiteral(s, p, ":="),
			this.DoParseExpressionRule,
			(s, p) => DoChoice(s, p,
				(s2, p2) => DoParseLiteral(s2, p2, ";"),
				(s2, p2) => DoSequence(s2, p2,
					this.DoParseActionRule,
					(s3, p3) => DoOptional(s3, p3, this.DoParseActionRule))));
		
		if (state.Parsed)
		{
			string pass = results[3].Text;
			if (pass == ";")
				pass = null;
			
			string fail = null;
			if (results.Count >= 5)
			{
				fail = results[4].Text;
				if (fail == ";")
					fail = null;
			}
			
			m_grammar.Rules.Add(new Rule(results[0].Text, results[2].Value, pass, fail, DoGetLine(start.Index)));
			
			Expression value = null;
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("Rule", start, state);
#endif
		
		return state;
	}
	
	private State DoParseSequenceRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("SequenceExpression := AssertExpression (Space+ AssertExpression)*", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			this.DoParseAssertRule,
			(s, p) => DoRepetition(s, p, this.DoParseAssertRule));
		
		if (state.Parsed)
		{
			Expression value = null;
			if (results.Count == 1)
				value = results[0].Value;
			else if (results.Count > 1)
				value = new SequenceExpression((from r in results where r.Value != null select r.Value).ToArray());
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("SequenceExpression", start, state);
#endif
		
		return state;
	}
	
	private State DoParseSettingRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("Setting := Identifier '='  [^\n\r]+", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			this.DoParseIdentifierRule,
			(s, p) => DoParseLiteral(s, p, "="),
			this.DoParseToEol);
		
		if (state.Parsed)
		{
			string name = results[0].Text;
			if (name != "comment" && name != "debug" && name != "exclude-exception" && name != "exclude-methods" && name != "ignore-case" && name != "namespace" && name != "start" && name != "unconsumed" && name != "using" && name != "value" && name != "visibility")
				DoThrow(start.Index, "Setting '{0}' is not a valid name.", name);
			
			string temp = results[2].Text.Trim();
			if (name == "comment")
			{
				if (!temp.StartsWith("//"))
					temp = "// " + temp;
					
				if (m_grammar.Settings["comment"].Length == 0)
					m_grammar.Settings["comment"] = temp;
				else
					m_grammar.Settings["comment"] += '\n' + temp;
			}
			else
			{
				if (m_grammar.Settings.ContainsKey(name))
					DoThrow(start.Index, "Setting '{0}' is already defined.", name);
				
				if (name == "unconsumed")
					if (temp != "error" && temp != "expose" && temp != "ignore")
						DoThrow(start.Index, "Unconsumed value must be 'error', 'expose', or 'ignore'.");
				
				if (name == "exclude-methods")
					temp += ' ';		// ensure a trailing space so we can search for 'name '
				
				m_grammar.Settings.Add(name, temp);
			}
			
			Expression value = null;
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("Setting", start, state);
#endif
		
		return state;
	}
	
	private State DoParseSubExpressionRule(State state, List<Result> outResults)
	{
#if DUMP		
		DoDebugProlog("SubExpression := '(' Expression ')'", state);
#endif
		
		State start = state;
		var results = new List<Result>();
		
		state = DoSequence(state, results,
			(s, p) => DoParseLiteral(s, p, "("),
			this.DoParseExpressionRule,
			(s, p) => DoParseLiteral(s, p, ")"));
		
		if (state.Parsed)
		{
			Expression value = null;
			value = results[1].Value;
			
			outResults.Add(new Result(m_input.Substring(start.Index, state.Index - start.Index), value));
		}
		
#if DUMP		
		DoDebugEpilog("SubExpression", start, state);
#endif
		
		return state;
	}
	#endregion
	
	#region Private Methods
#if DUMP		
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
			
			else if (ch > '~')
				builder.AppendFormat("\\x{0:X4}", (int) ch);
			
			else
				builder.Append(ch);
		}
		
		return builder.ToString();
	}
	
	private string DoTruncateString(string str)
	{
		if (str.Length > 48)
			return str.Substring(0, 48) + "...";
		else if (str.Length > 0 && str[str.Length - 1] == '\x0')
			return str.Substring(0, str.Length - 1);
		else
			return str;
	}
	
	[Conditional("DUMP")]
	private void DoDebugWrite(string format, params object[] args)
	{
		Console.Write(new string(' ', 4*m_debugLevel));
		Console.WriteLine(DoEscapeAll(string.Format(format, args)));
	}
	
	[Conditional("DUMP")]
	private void DoDebugProlog(string rule, State start)
	{
		DoDebugWrite(rule);
		++m_debugLevel;
	}
	
	[Conditional("DUMP")]
	private void DoDebugEpilog(string name, State start, State stop)
	{
		if (stop.Parsed)
			DoDebugWrite("{0} on line {1} consumed {2}", name, DoGetLine(start.Index), DoTruncateString(m_input.Substring(start.Index, stop.Index - start.Index)));
		else if (start.Index >= m_input.Length - 1)
			DoDebugWrite("{0} on line {1} failed on EOT", name, DoGetLine(start.Index));
		else
			DoDebugWrite("{0} on line {1} failed on {2}", name, DoGetLine(start.Index), DoTruncateString(m_input.Substring(start.Index)));
		--m_debugLevel;
	}
#endif
	
	// This is normally only used for error handling so it doesn't need to be too
	// fast. If it somehow does become a bottleneck for some parsers they can
	// replace it using the custom-methods setting.
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
	
	private void DoThrow(int index, string format, params object[] args)
	{
		int line = DoGetLine(index);
		
		// We need this retarded if or string.Format will throw an error if it
		// gets a format string like "Expected { or something".
		if (args != null && args.Length > 0)
			throw new ParserException(line, string.Format(format, args));
		else
			throw new ParserException(line, format);
	}
	
	private int DoSkipWhiteSpace(int index)
	{
		int len = 0;
		
		while (char.IsWhiteSpace(m_input[index + len]))
		{
			++len;
		}
		
		return index + len;
	}
	
	private State DoParseLiteral(State state, List<Result> results, string literal)
	{
		int j = DoSkipWhiteSpace(state.Index);
		
		for (int i = 0; i < literal.Length; ++i)
		{
			if (m_input[j + i] != literal[i])
				return new State(state.Index, false, new ErrorSet(state.Index, literal));
		}
		
		int k = DoSkipWhiteSpace(j + literal.Length);
		
		results.Add(new Result(literal, null));
		state = new State(k, true, state.Errors);
		
		return state;
	}
	
	private State DoParseInvertedRange(State state, List<Result> results, string chars)
	{
		if (m_input[state.Index] != '\x0' && chars.IndexOf(m_input[state.Index]) < 0)
		{
			results.Add(new Result(m_input.Substring(state.Index, 1), null));
			state = new State(state.Index + 1, true, state.Errors);
		}
		else
		{
			state = new State(state.Index, false, ErrorSet.Combine(state.Errors, new ErrorSet(state.Index, string.Format("[^{0}]", chars))));
		}
		
		return state;
	}
	
	private State DoParseToEol(State state, List<Result> results)
	{
		int i = state.Index;
		
		int len = 0;
		while (m_input[i + len] != '\r' && m_input[i + len] != '\n' && m_input[i + len] != '#' && m_input[i + len] != '\x0')
			++len;
			
		if (len > 0)
		{
			results.Add(new Result(m_input.Substring(i, len), null));
			state = new State(i + len, true, state.Errors);
		}
		else
		{
			state = new State(i, false, ErrorSet.Combine(state.Errors, new ErrorSet(i, "[^\n\r]+")));
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
	
	private State DoOptional(State state, List<Result> results, ParseMethod method)
	{
		State temp = method(state, results);
		if (temp.Parsed)
			state = temp;
		else
			state = new State(state.Index, true, ErrorSet.Combine(state.Errors, temp.Errors));
		
		return state;
	}
	
	private State DoRepetition(State state, List<Result> results, ParseMethod method)
	{
		while (true)
		{
			State temp = method(state, results);
			if (temp.Parsed)
			{
				state = temp;
			}
			else
			{
				state = new State(state.Index, true, ErrorSet.Combine(state.Errors, temp.Errors));
				break;
			}
		}
		
		return state;
	}
	
	private State DoPositiveRepetition(State state, List<Result> results, ParseMethod method)
	{
		int startResult = results.Count;
		
		state = method(state, results);
		if (state.Parsed)
		{
			while (true)
			{
				State temp = method(state, results);
				if (temp.Parsed)
				{
					state = temp;
				}
				else
				{
					state = new State(state.Index, true, ErrorSet.Combine(state.Errors, temp.Errors));
					break;
				}
			}
		}
		
		if (!state.Parsed)
			results.RemoveRange(startResult, results.Count - startResult);
		
		return state;
	}
	#endregion
	
	#region Private Types
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
		
		// These are arbitrary error messages.
//		public string[] Failures {get; private set;}		// TODO: support this
		
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
			return string.Format("Expected {0}.", string.Join(" or ", Expected));
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
		public Result(string text, Expression value)
		{
			Text = text;
			Value = value;
		}
		
		// The text which was parsed by the terminal or non-terminal.
		public string Text {get; private set;}
		
		// For non-terminals this will be the result of the semantic action, 
		// otherwise it will be the default value.
		public Expression Value {get; private set;}
	}
	#endregion
	
	#region Fields
	private string m_input;
	private Grammar m_grammar;
#if DUMP
	private int m_debugLevel;
#endif
	#endregion
}
