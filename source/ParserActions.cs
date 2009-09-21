using System;
using System.Collections.Generic;
using System.Linq;

internal sealed partial class Parser
{
	public Grammar Grammar
	{
		get {return m_grammar;}
	}
	
	partial void OnParseEpilog(State state)
	{
		if (!m_grammar.Settings.ContainsKey("start"))
			throw new ParserException("Missing required setting 'start'.");
		if (!m_grammar.Settings.ContainsKey("value"))
			throw new ParserException("Missing required setting 'value'.");
		if (!m_grammar.Rules.Exists(r => r.Name == m_grammar.Settings["start"]))
			throw new ParserException(string.Format("Missing the start rule '{0}'.", m_grammar.Settings["start"]));
	}
	
	private string DoAddSetting(List<Result> results)
	{
		string name = results[0].Text.Trim();
		if (name != "comment" && name != "debug" && name != "exclude-exception" && name != "exclude-methods" && name != "ignore-case" && name != "namespace" && name != "start" && name != "unconsumed" && name != "using" && name != "value" && name != "visibility")
			return string.Format("Setting '{0}' is not a valid name", name);
		
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
				return string.Format("Setting '{0}' is already defined", name);
			
			if (name == "unconsumed")
				if (temp != "error" && temp != "expose" && temp != "ignore")
					return "Unconsumed value must be 'error', 'expose', or 'ignore'";
			
			if (name == "exclude-methods")
				temp += ' ';		// ensure a trailing space so we can search for 'name '
			
			m_grammar.Settings.Add(name, temp);
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
	
	#region Fields
	private Grammar m_grammar = new Grammar();
	#endregion
}
