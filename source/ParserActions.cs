using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

internal sealed partial class Parser
{
	public Grammar Grammar
	{
		get {return m_grammar;}
	}
	
	partial void OnParseEpilog(State state)
	{
		DoProcessIncludes();
		
		if (!Included)
		{
			if (!m_grammar.Settings.ContainsKey("start"))
				throw new ParserException("Missing required setting 'start'.");
			if (!m_grammar.Settings.ContainsKey("value"))
				throw new ParserException("Missing required setting 'value'.");
			if (!m_grammar.Rules.Exists(r => r.Name == m_grammar.Settings["start"]))
				throw new ParserException(string.Format("Missing the start rule '{0}'.", m_grammar.Settings["start"]));
		}
		else
		{
			if (m_grammar.Settings.Count > 1)
				throw new ParserException("Included files should not have settings.");
			else
				Contract.Assert(m_grammar.Settings.ContainsKey("comment"));		// added by the Grammar ctor
		}
	}
	
	public bool Included {get; set;}
	
	#region Private Methods
	private void DoProcessIncludes()
	{
		foreach (string i in m_includes)
		{
			Grammar grammar = DoProcessInclude(i);
			m_grammar.Rules.AddRange(grammar.Rules);
		}
	}
	
	private Grammar DoProcessInclude(string i)
	{
		string oldWd = System.IO.Directory.GetCurrentDirectory();
		string newWd = System.IO.Path.GetDirectoryName(m_file);
		if (newWd.Length > 0)
			System.IO.Directory.SetCurrentDirectory(newWd);
		
		string contents = System.IO.File.ReadAllText(i);
		var parser = new Parser();
		parser.Included = true;
		parser.DoParseFile(contents, i, "IncludedFile");
		
		System.IO.Directory.SetCurrentDirectory(oldWd);
		
		return parser.Grammar;
	}
	
	private string DoAddSetting(List<Result> results)
	{
		return DoAddSetting(results[0].Text.Trim(), results[2].Text.Trim());
	}
	
	private string DoAddSetting(string name, string value)
	{
		if (name != "comment" && name != "parse-accessibility" && name != "debug" && name != "debug-file" && name != "exclude-exception" && name != "exclude-methods" && name != "ignore-case" && name != "namespace" && name != "start" && name != "unconsumed" && name != "used" && name != "using" && name != "value" && name != "visibility")
			return string.Format("Setting '{0}' is not a valid name", name);
		
		if (name == "comment")
		{
			if (!value.StartsWith("//"))
				value = "// " + value;
				
			if (m_grammar.Settings["comment"].Length == 0)
				m_grammar.Settings["comment"] = value;
			else
				m_grammar.Settings["comment"] += '\n' + value;
		}
		else
		{
			if (m_grammar.Settings.ContainsKey(name))
				return string.Format("Setting '{0}' is already defined", name);
			
			if (name == "unconsumed")
				if (value != "error" && value != "expose" && value != "ignore")
					return "Unconsumed value must be 'error', 'expose', or 'ignore'";
			
			if (name == "exclude-methods")
				value += ' ';		// ensure a trailing space so we can search for 'name '
			
			m_grammar.Settings.Add(name, value);
		}
		
		return null;
	}
	
	private void DoAddRule(List<Result> results)
	{
		string pass = null;
		if (results.Count >= 4)
		{
			pass = results[3].Text.Trim();
			if (pass == ";" || pass == "`;`")
				pass = null;
			else
				pass = pass.Substring(1, pass.Length - 2);
		}
		
		string fail = null;
		if (results.Count >= 5)
		{
			fail = results[4].Text.Trim();
			if (fail == ";" || fail == "`;`")
				fail = null;
			else
				fail = fail.Substring(1, fail.Length - 2);
		}
		
		m_grammar.Rules.Add(new Rule(results[0].Text.Trim(), results[2].Value, pass, fail, results[0].Line));
	}
	
	private Expression DoSequence(List<Result> results)
	{
		Expression value = null;
		
		if (results.Count == 1)
			value = results[0].Value;
		else if (results.Count > 1)
			value = new SequenceExpression((from r in results where r.Value != null select r.Value).ToArray());
			
		return value;
	}
	
	private Expression DoChoice(List<Result> results)
	{
		Expression value = null;
		
		if (results.Count == 1)
			value = results[0].Value;
		else if (results.Count > 1)
			value = new ChoiceExpression((from r in results where r.Value != null select r.Value).ToArray());
			
		return value;
	}
	
	private string DoRange(string text, ref Expression value)
	{
		string literal = text.Trim();
		literal = literal.Substring(1, literal.Length - 2);
		
		if (literal == "^")
			return "Inverted range cannot be empty";
		
		try
		{
			value = new RangeExpression(literal);
		}
		catch (Exception e)
		{
			return e.Message;
		}
		
		return null;
	}
	
	private Expression DoRepetition(Expression e, int min, int max, int index)
	{
		if (min < 0)
			DoThrow(index, "Min is less than zero");
		if (max <= 0)
			DoThrow(index, "Max is not positive");
		if (min > max)
			DoThrow(index, "Min is greater than max");
		
		return new RepetitionExpression(e, min, max);
	}
	#endregion
	
	#region Fields
	private Grammar m_grammar = new Grammar();
	private List<string> m_includes = new List<string>();
	#endregion
}
