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
using System.Linq;

// In-lines and otherwise simplifies rules. This is largely based on Bryan Ford's
// MIT master's thesis: Packrat Parsing: a Practical Linear-Time Algorithm with Backtracking.
internal sealed class Optimizer
{
	public Optimizer(Dictionary<string, string> settings, List<Rule> rules)
	{
		m_settings = settings;
		m_rules = rules;
	}
	
	public void Optimize()
	{
		if (Program.Verbosity >= 3)
			DoDump("before optimization:");
		
		for	 (int i = 0; i < 64; ++i)		// do a max of 64 optimization passes
		{
			var oldEdit = m_editCount;
			if (Program.Verbosity >= 3)
				Console.WriteLine("{0}optimization pass {1}", Environment.NewLine, i+1);
			DoOptimize("Merge", this.DoMergeRules);
			DoOptimize("InlineTiny", this.DoInlineTiny);
			DoOptimize("InlineSingleUse", this.DoInlineSingleUses);
			var newEdit = m_editCount;
			
			if (newEdit == oldEdit)
				break;
		}
		
		if (Program.Verbosity >= 3 && m_editCount > 0)
			DoDump("after optimization:");
	}
	
	#region Private Methods
	private delegate void OptimizeCallback(List<int> deathRow);
	
	private void DoOptimize(string name, Action<List<int>> action)
	{
		var deathRow = new List<int>();
			
		int oldSize = DoGetSize();
		action(deathRow);
		int newSize = DoGetSize();
		
		for (int index = deathRow.Count - 1; index >= 0; --index)
		{
			m_rules.RemoveAt(deathRow[index]);
		}
		
		if (Program.Verbosity >= 3 && newSize < oldSize)
			Console.WriteLine("{0} reduced by {1}", name, oldSize - newSize);
		else if (Program.Verbosity >= 3 && newSize > oldSize)
			Console.WriteLine("{0} grew by {1}", name, newSize - oldSize);
	}
	
	private Dictionary<string, List<int>> DoGetNoActionRules()
	{
		var rules = new Dictionary<string, List<int>>();
		
		for (int i = 0; i < m_rules.Count; ++i)
		{
			string name = m_rules[i].Name;
			if (!rules.ContainsKey(name))
			{
				var indices = new List<int>();
				
				for (int j = i; j < m_rules.Count; ++j)
				{
					if (m_rules[j].Name == name)
					{
						if (m_rules[j].PassAction == null && m_rules[j].FailAction == null)
						{
							indices.Add(j);
						}
						else
						{
							indices.Clear();
							break;
						}
					}
				}
				
				rules.Add(name, indices);
			}
		}
		
		return rules;
	}
	
	private Expression[] DoGetExpressions(List<int> indices)
	{
		var expressions = new Expression[indices.Count];
		
		for (int i = 0; i < indices.Count; ++i)
		{
			expressions[i] = m_rules[indices[i]].Expression;
		}
		
		return expressions;
	}
	
	private IEnumerable<Expression> DoFindMatching(Predicate<Expression> predicate)
	{
		foreach (Rule rule in m_rules)
		{
			foreach (Expression e in rule.Expression.Select(predicate))
			{
				yield return e;
			}
		}
	}
	
	// Merge rules with the same name and no actions into a single rule.
	private void DoMergeRules(List<int> deathRow)
	{
		Dictionary<string, List<int>> rules = DoGetNoActionRules();
		foreach (var entry in rules)
		{
			if (entry.Value.Count > 1)
			{
				if (Program.Verbosity >= 3)
					Console.WriteLine("merging {0}", entry.Key);
				var expr = new ChoiceExpression(DoGetExpressions(entry.Value));
				m_rules.Add(new Rule(entry.Key, expr, null, null, m_rules[entry.Value[0]].Line));
				deathRow.AddRange(entry.Value);
				++m_editCount;
			}
		}
	}
	
	private void DoInline(Rule rule)
	{
		for (int i = 0; i < m_rules.Count; ++i)
		{
			Rule candidate = m_rules[i];
			if (candidate.Name != rule.Name)
			{
				candidate.Optimize(e =>
				{
					var r = e as RuleExpression;
					if (r != null && r.Name == rule.Name)
						return rule.Expression;
					else
						return e;
				});
			}
		}
		
		++m_editCount;
	}
	
	// Inline rules that have no more than two operators (and no semantic actions).
	// It kind of sucks to not be able to inline rules with semantic actions, but doing
	// so would require major changes and considerable complexity.
	private void DoInlineTiny(List<int> deathRow)
	{
		Dictionary<string, List<int>> rules = DoGetNoActionRules();
		foreach (var entry in rules)
		{
			if (entry.Value.Count == 1)
			{
				Rule rule = m_rules[entry.Value[0]];
				
				if (rule.Expression.GetSize() <= 2 && rule.Name != m_settings["start"])
				{
					if (Program.Verbosity >= 3)
						Console.WriteLine("inlining tiny {0}", rule.Name);
						
					DoInline(rule);
					deathRow.AddRange(entry.Value);
				}
			}
		}
	}
	
	// Inline rules that are used in a single place (and have no semantic actions).
	private void DoInlineSingleUses(List<int> deathRow)
	{
		Dictionary<string, List<int>> rules = DoGetNoActionRules();
		foreach (var entry in rules)
		{
			if (entry.Value.Count == 1)
			{
				Rule rule = m_rules[entry.Value[0]];
				
				if (rule.Name != m_settings["start"])
				{
					if (DoFindMatching(e =>
						{
							var r = e as RuleExpression;
							return r != null && r.Name == rule.Name;
						}).Count() == 1)
					{
						if (Program.Verbosity >= 3)
							Console.WriteLine("inlining single {0}", rule.Name);
							
						DoInline(rule);
						deathRow.AddRange(entry.Value);
					}
				}
			}
		}
	}
	
	private void DoDump(string header = null)
	{
		if (header != null)
			Console.WriteLine("{0} (size = {1})", header, DoGetSize());
			
		foreach (Rule rule in m_rules)
		{
			Console.WriteLine("   {0}  [{1}]", rule, rule.Expression.GetSize());
		}
	}
	
	private int DoGetSize()
	{
		return m_rules.Sum(r => r.Expression.GetSize());
	}
	#endregion
	
	#region Fields
	private Dictionary<string, string> m_settings = new Dictionary<string, string>();
	private List<Rule> m_rules = new List<Rule>();
	private ulong m_editCount;
	#endregion
}
