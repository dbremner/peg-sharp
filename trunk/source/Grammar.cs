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
using System.Linq;

// In-memory representation of the grammar encoded in a peg file.
internal sealed class Grammar
{
	public Grammar()
	{
		Settings.Add("comment", string.Empty);
	}
	
	public Dictionary<string, string> Settings
	{
		get {return m_settings;}
	}
	
	public List<Rule> Rules
	{
		get {return m_rules;}
	}
	
	public void Validate()
	{
		if (!Settings.ContainsKey("unconsumed"))
			if (Settings["value"] == "void")
				Settings.Add("unconsumed", "expose");
			else
				Settings.Add("unconsumed", "error");
			
		if (!Settings.ContainsKey("ignore-case"))
			Settings.Add("ignore-case", "false");
			
		if (!Settings.ContainsKey("visibility"))
			Settings.Add("visibility", "internal");
			
		if (!Settings.ContainsKey("exclude-exception"))
			Settings.Add("exclude-exception", "false");
			
		if (!Settings.ContainsKey("exclude-methods"))
			Settings.Add("exclude-methods", string.Empty);
			
		if (!Settings.ContainsKey("debug"))
			Settings.Add("debug", string.Empty);
			
		if (!Settings.ContainsKey("debug-file"))
			Settings.Add("debug-file", string.Empty);
			
		var used =
			from r in Rules
				from e in r.Expression.Select(f => f is RuleExpression)
				let re = e as RuleExpression
					select re.Name;
		
		DoCheckForBadDebugSetting();
		DoCheckForMissingNonterminals(used);
		DoCheckForUnusedNonterminals(used);
		DoCheckForLeftRecursion();
		DoCheckForUnreachableFailAction();
		DoCheckForUnreachableAlternative();
		DoCheckForBackwardsRange();
	}
	
	#region Private Methods
	private void DoCheckForBadDebugSetting()
	{
		string[] names = Settings["debug"].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
		foreach (string name in names)
		{
			if (!m_rules.Any(r => r.Name == name))
			{
				string mesg = string.Format("Debug setting has a value which is not the name of a non-terminal: {0}.", name);
				throw new ParserException(mesg);
			}
		}
	}
	
	private void DoCheckForMissingNonterminals(IEnumerable<string> used)
	{
		var missing = from name in used where !Rules.Exists(r => r.Name == name) select name;
		missing = missing.Distinct();
		
		if (missing.Any())
		{
			string mesg = string.Format("Undefined nonterminals: {0}", string.Join(" ", missing.ToArray()));
			throw new ParserException(mesg);
		}
	}
	
	private void DoCheckForUnusedNonterminals(IEnumerable<string> used)
	{
		var names = from r in Rules select r.Name;
		string start = Settings["start"];
		var unused = from n in names where !used.Contains(n) && n != start select n;
		
		if (unused.Any())
		{
			string mesg = string.Format("Unused nonterminals: {0}", string.Join(" ", unused.ToArray()));
			throw new ParserException(mesg);
		}
	}
	
	// This checks for direct left recursion (Foo := Foo X) and indirect left recursion
	// (Foo := X Y where X or a rule called by X starts with Foo).
	private bool DoIsLeftRecursive(Rule rule, string nonterminal, List<Rule> processed)
	{
		if (processed.IndexOf(rule) >= 0)
			return false;
		processed.Add(rule);
		
		string[] left = rule.Expression.GetLeftRules();
		if (Array.IndexOf(left, nonterminal) >= 0)
			return true;
		
		foreach (string l in left)
			foreach (Rule r in m_rules.Where(q => q.Name == l))
				if (DoIsLeftRecursive(r, nonterminal, processed))
					return true;
		
		return false;
	}
	
	private void DoCheckForLeftRecursion()
	{
		var bad = new List<string>();
		
		foreach (Rule rule in m_rules)
		{
			if (bad.IndexOf(rule.Name) < 0)
			{
				if (DoIsLeftRecursive(rule, rule.Name, new List<Rule>()))
					bad.Add(rule.Name);
			}
		}
		
		if (bad.Count > 0)
		{
			string mesg = string.Format("Left recursive rules: {0}", string.Join(" ", bad.ToArray()));
			throw new ParserException(mesg);
		}
	}
	
	private bool DoAlwaysSucceeds(string ruleName, Expression expr)
	{
		bool succeeds = false;
		
		do
		{
			ChoiceExpression choice = expr as ChoiceExpression;
			if (choice != null)
			{
				// Note that we only need to check the last alternative because we'll
				// get another error if an interior one always succeeds.
				succeeds = DoAlwaysSucceeds(ruleName, choice.Expressions[choice.Expressions.Length - 1]);
				break;
			}
			
			RangeExpression range = expr as RangeExpression;
			if (range != null)
			{
				succeeds = range.ToString() == ".";
				break;
			}
			
			RepetitionExpression rep = expr as RepetitionExpression;
			if (rep != null)
			{
				succeeds = rep.Min == 0;
				break;
			}
			
			SequenceExpression seq = expr as SequenceExpression;
			if (seq != null)
			{
				succeeds = seq.Expressions.All(e => DoAlwaysSucceeds(ruleName, e));
				break;
			}
			
			RuleExpression rule2 = expr as RuleExpression;
			if (rule2 != null && rule2.Name != ruleName)
			{
				succeeds = DoAlwaysSucceeds(rule2.Name);
				break;
			}
		}
		while (false);
		
		return succeeds;
	}
	
	private bool DoAlwaysSucceeds(string ruleName)
	{
		foreach (Rule rule in m_rules)
		{
			if (rule.Name == ruleName)
			{
				if (!DoAlwaysSucceeds(rule.Name, rule.Expression))
					return false;
			}
		}
		
		return true;
	}
	
	// Check for fail action attached to an expression which always succeeds.
	private void DoCheckForUnreachableFailAction()
	{
		var bad = new List<string>();
		
		foreach (Rule rule in m_rules)
		{
			if (bad.IndexOf(rule.Name) < 0)
			{
				if (rule.FailAction != null && DoAlwaysSucceeds(rule.Name, rule.Expression))
					bad.Add(rule.Name);
			}
		}
		
		if (bad.Count > 0)
		{
			string mesg = string.Format("Unreachable fail actions: {0}", string.Join(" ", bad.ToArray()));
			throw new ParserException(mesg);
		}
	}
	
	private bool DoHasBadLiteralPrefix(ChoiceExpression choice)
	{
		bool bad = false;
		
		for (int i = 0; i < choice.Expressions.Length - 1 && !bad; ++i)
		{
			LiteralExpression literal1 = choice.Expressions[i] as LiteralExpression;
			if (literal1 != null)
			{
				for (int j= i + 1; j < choice.Expressions.Length && !bad; ++j)
				{
					LiteralExpression literal2 = choice.Expressions[j] as LiteralExpression;
					if (literal2 != null)
					{
						if (m_settings["ignore-case"] == "true")
							bad = literal2.Literal.ToLower().StartsWith(literal1.Literal.ToLower());
						else
							bad = literal2.Literal.StartsWith(literal1.Literal);
					}
				}
			}
		}
		
		return bad;
	}
	
	private bool DoHasBadRangePrefix(ChoiceExpression choice)
	{
		bool bad = false;
		
		for (int i = 0; i < choice.Expressions.Length - 1 && !bad; ++i)
		{
			var range1 = choice.Expressions[i] as RangeExpression;
			if (range1 != null)
			{
				var chars1 = new CharSet(range1);
				
				for (int j= i + 1; j < choice.Expressions.Length && !bad; ++j)
				{
					var range2 = choice.Expressions[j] as RangeExpression;
					if (range2 != null)
					{
						var chars2 = new CharSet(range2);
						bad = chars1.IsSuperSetOf(chars2);
					}
				}
			}
		}
		
		return bad;
	}
	
	private bool DoHasUnreachableAlternative(string ruleName, ChoiceExpression choice)
	{
		bool has = false;
		
		for (int i = 0; i < choice.Expressions.Length - 1 && !has; ++i)
		{
			// 'x' / . / 'z'
			if (DoAlwaysSucceeds(ruleName, choice.Expressions[i]))
				has = true;
			
			// 'xx' / 'xxY'
			else if (DoHasBadLiteralPrefix(choice))
				has = true;
			
			// [e] / [f] where e is a superset of the chars in f
			else if (DoHasBadRangePrefix(choice))
				has = true;
		}
		
		return has;
	}
	
	// Check for a choice alternative which will never be used.
	private void DoCheckForUnreachableAlternative()
	{
		var bad = new List<string>();
		
		foreach (Rule rule in m_rules)
		{
			foreach (ChoiceExpression choice in rule.Expression.Select(e => e is ChoiceExpression))
			{
				if (DoHasUnreachableAlternative(rule.Name, choice))
					bad.Add(rule.Name);
			}
		}
		
		if (bad.Count > 0)
		{
			string mesg = string.Format("Unreachable alternative in: {0}", string.Join(" ", bad.ToArray()));
			throw new ParserException(mesg);
		}
	}
	
	private bool DoHasBackwardsRange(string ranges)
	{
		for (int i = 0; i < ranges.Length; i += 2)
		{
			if (ranges[i] > ranges[i + 1])
				return true;
		}
		
		return false;
	}
	
	// [Z-A]
	private void DoCheckForBackwardsRange()
	{
		var bad = new List<string>();
		
		foreach (Rule rule in m_rules)
		{
			foreach (RangeExpression range in rule.Expression.Select(e => e is RangeExpression))
			{
				if (DoHasBackwardsRange(range.Ranges))
					bad.Add(rule.Name);
			}
		}
		
		if (bad.Count > 0)
		{
			string mesg = string.Format("Backwards range in: {0}", string.Join(" ", bad.ToArray()));
			throw new ParserException(mesg);
		}
	}
	#endregion
	
	#region Fields
	private Dictionary<string, string> m_settings = new Dictionary<string, string>();
	private List<Rule> m_rules = new List<Rule>();			// note that the order is significant
	#endregion
}
