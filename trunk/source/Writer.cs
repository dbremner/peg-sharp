// Copyright (C) 2009-2010 Jesse Jones
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
//using System.Reflection;
//using System.Text.RegularExpressions;

// Writes the parser file.
internal sealed partial class Writer : IDisposable
{
	public Writer(TextWriter writer, Grammar grammar)
	{
		m_writer = writer;
		m_grammar = grammar;
		
		foreach (Rule rule in m_grammar.Rules)
		{
			List<Rule> temp;
			if (!m_rules.TryGetValue(rule.Name, out temp))
			{
				temp = new List<Rule>();
				m_rules.Add(rule.Name, temp);
			}
			temp.Add(rule);
		}
		
		foreach (string e in m_grammar.Settings["exclude-methods"].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries))
		{
			m_engine.AddExcluded(e);
		}
		
		foreach (var entry in m_grammar.Settings)
		{
			if ("true".Equals(entry.Value))
				m_engine.AddVariable(entry.Key, true);
			else if ("false".Equals(entry.Value))
				m_engine.AddVariable(entry.Key, false);
			else
				m_engine.AddVariable(entry.Key, entry.Value);
		}
		m_engine.AddVariable("debugging", m_grammar.Settings["debug"] != "none");
		
		DoSetUsed();
	}
	
	public void Write(string pegFile)
	{
		if (m_disposed)
			throw new ObjectDisposedException(GetType().Name);
		
		m_className = System.IO.Path.GetFileNameWithoutExtension(pegFile);
		DoSetReplacements(pegFile);
		
//		var rules = new Dictionary<string, List<Rule>>();
//		foreach (Rule rule in m_grammar.Rules)
//		{
//			List<Rule> temp;
//			if (!rules.TryGetValue(rule.Name, out temp))
//			{
//				temp = new List<Rule>();
//				rules.Add(rule.Name, temp);
//			}
//			temp.Add(rule);
//		}
		
		string parser = DoCreateParser();
		m_writer.Write(parser);
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
	
	private string DoCreateParser()
	{
		int count = 0;
		var nonterminals = new System.Text.StringBuilder();
		foreach (var entry in m_rules)
		{
			for (int i = 0; i < entry.Value.Count; ++i)
			{
				string name = DoGetNonterminalMethodName(entry.Value, i);
				nonterminals.Append(DoCreateNonTerminal(name, entry.Value[i], i, entry.Value.Count));
				
				if (i + 1 < entry.Value.Count)
					nonterminals.AppendLine();
			}
			
			if (++count < m_rules.Count)
				nonterminals.AppendLine();
		}
		
		m_engine.AddReplacement("NON-TERMINALS", nonterminals.ToString());
		
		string parser = m_engine.Process("Code.cs");
		
		return parser;
	}
	
	private string DoGetCode(string indent, string code)
	{
		string trailer = string.Empty;
		if (code[code.Length - 1] != ';' && code[code.Length - 1] != '{' && code[code.Length - 1] != '}')
			trailer = ";";
		return indent + code + trailer;
	}
	
	private string DoGetHook(Rule rule, Hook hook)
	{
		var builder = new System.Text.StringBuilder();
		
		List<string> code = rule.GetHook(hook);
		if (code != null)
		{
			for (int i = 0; i < code.Count; ++i)
			{
				string c = code[i];
				
				builder.AppendLine(DoGetCode(string.Empty, c));
			}
		}
		
		string result = builder.ToString();
		
		return result.TrimEnd();
	}
	
	private string DoCreateNonTerminal(string methodName, Rule rule, int i, int maxIndex)
	{
		string prolog = DoGetHook(rule, Hook.Prolog);
		string epilog = DoGetHook(rule, Hook.Epilog);
		string failEpilog = DoGetHook(rule, Hook.FailEpilog);
		string passEpilog = DoGetHook(rule, Hook.PassEpilog);
		
		m_engine.SetVariable("fail-action-uses-expected", rule.FailAction != null && DoReferencesLocal(rule.FailAction, "expected"));
		m_engine.SetVariable("has-debug-file", m_grammar.Settings["debug-file"].Length > 0);
		m_engine.SetVariable("has-fail-action", rule.FailAction != null);
		m_engine.SetVariable("has-fail-epilog", failEpilog.Length > 0);
		m_engine.SetVariable("has-pass-action", rule.PassAction != null);
		m_engine.SetVariable("has-pass-epilog", passEpilog.Length > 0);
		m_engine.SetVariable("has-prolog", prolog.Length > 0);
		m_engine.SetVariable("pass-action-uses-fatal", rule.PassAction != null && DoReferencesLocal(rule.PassAction, "fatal"));
		m_engine.SetVariable("pass-action-uses-text", rule.PassAction != null && DoReferencesLocal(rule.PassAction, "text"));
		m_engine.SetVariable("pass-epilog-uses-fail", passEpilog.Length > 0 && DoReferencesLocal(passEpilog, "fail"));
		m_engine.SetVariable("prolog-uses-fail", prolog.Length > 0 && DoReferencesLocal(prolog, "fail"));
		m_engine.SetVariable("rule-has-alternatives", maxIndex > 1);
		
		string comment = string.Format("{0} := {1}", rule.Name, rule.Expression);
		
		string debugName = rule.Name;
		if (maxIndex > 1)
			debugName += i + 1;
		
		var body = new System.Text.StringBuilder();
		body.Append("\t");
		rule.Expression.Write(body, 0);
		body.Append(";");
		
		m_engine.SetReplacement("DEBUG-NAME", debugName);
		m_engine.SetReplacement("FAIL-ACTION", rule.FailAction != null ? DoGetCode("", rule.FailAction) : string.Empty);
		m_engine.SetReplacement("FAIL-EPILOG-HOOK", failEpilog);
		m_engine.SetReplacement("METHOD-NAME", methodName);
		m_engine.SetReplacement("PASS-ACTION", rule.PassAction != null ? DoGetCode("", rule.PassAction) : string.Empty);
		m_engine.SetReplacement("PASS-EPILOG-HOOK", passEpilog);
		m_engine.SetReplacement("PROLOG-HOOK", prolog);
		m_engine.SetReplacement("RULE-BODY", body.ToString());
		m_engine.SetReplacement("RULE-COMMENT", comment);
		m_engine.SetReplacement("RULE-INDEX", (i + 1).ToString());
		m_engine.SetReplacement("RULE-NAME", rule.Name);
		m_engine.SetReplacement("EPILOG-HOOK", epilog);					// has to be after FAIL-EPILOG-HOOK
		m_engine.SetReplacement("START-RULE", m_grammar.Settings["start"]);
		
		string nonterminal = m_engine.Process("NonTerminal.cs");
		
		return nonterminal;
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
			
		else if (text.Contains(local + "["))
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
	
	private void DoSetUsed()
	{
		Used used = 0;
		foreach (Rule rule in m_grammar.Rules)
		{
			used |= rule.Expression.FindUsed();
		}
		
		m_engine.AddVariable("used-assert", (used & Used.Assert) == Used.Assert);
		m_engine.AddVariable("used-choice", true);			// DoParse uses this
		m_engine.AddVariable("used-nassert", (used & Used.NAssert) == Used.NAssert);
		m_engine.AddVariable("used-literal", (used & Used.Literal) == Used.Literal);
		m_engine.AddVariable("used-range", (used & Used.Range) == Used.Range);
		m_engine.AddVariable("used-repetition", (used & Used.Repetition) == Used.Repetition);
		m_engine.AddVariable("used-sequence", (used & Used.Sequence) == Used.Sequence);
	}
	
	private void DoSetReplacements(string pegFile)
	{
		Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		m_engine.AddReplacement("TIME-STAMP", string.Format("// Machine generated by peg-sharp {0} from {1}.", version, pegFile));
		
		var names = (from n in m_grammar.Settings["using"].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries) select string.Format("using {0};", n)).ToArray();
		Array.Sort(names);
		m_engine.AddReplacement("USING-DECLARATIONS", string.Join(Environment.NewLine, names));
		
		if (m_grammar.Settings.ContainsKey("namespace"))
		{
			m_engine.AddReplacement("OPEN-NAMESPACE", string.Format("namespace {0}{1}{2}", m_grammar.Settings["namespace"], Environment.NewLine, "{"));
			m_engine.AddReplacement("CLOSE-NAMESPACE", "}");
		}
		else
		{
			m_engine.AddReplacement("OPEN-NAMESPACE", Environment.NewLine);
			m_engine.AddReplacement("CLOSE-NAMESPACE", Environment.NewLine);
		}
		
		if (m_grammar.Settings["comment"].Length == 0)
		{
			m_engine.AddReplacement("PARSER-COMMENT", "// Thread safe if Parser instances are not shared across threads.");
		}
		else
		{
			m_engine.AddReplacement("PARSER-COMMENT", m_grammar.Settings["comment"]);
		}
		
		m_engine.AddReplacement("PARSE-ACCESSIBILITY", m_grammar.Settings["parse-accessibility"]);
		m_engine.AddReplacement("PARSER", m_className);
		m_engine.AddReplacement("RESULT", m_grammar.Settings["value"] == "void" ? "int" : m_grammar.Settings["value"]);
		m_engine.AddReplacement("VISIBILITY", m_grammar.Settings["visibility"]);
		m_engine.AddReplacement("VALUE", m_grammar.Settings["value"]);
		
		var add = new System.Text.StringBuilder();
		foreach (var entry in m_rules)
		{
			add.Append("\t\tm_nonterminals.Add(\"");
			add.Append(entry.Key);
			add.Append("\", new ParseMethod[]{");
			
			for (int i = 0; i < entry.Value.Count; ++i)
			{
				add.Append("this.");
				add.Append(DoGetNonterminalMethodName(entry.Value, i));
				
				if (i + 1 < entry.Value.Count)
					add.Append(", ");
			}
			
			add.Append("});");
			add.Append(Environment.NewLine);
		}
		m_engine.AddReplacement("ADD-NON-TERMINALS", add.ToString().TrimEnd());
	}
	#endregion
	
	#region Fields
	private TextWriter m_writer;
	private Grammar m_grammar;
	private Dictionary<string, List<Rule>> m_rules = new Dictionary<string, List<Rule>>();
	private string m_className;
	private bool m_disposed;
	private TemplateEngine m_engine = new TemplateEngine();
	#endregion
}
