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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

// Writes the parser file.
internal sealed class TemplateEngine
{
	public void AddVariable(string name, object value)
	{
		m_context.AddVariable(name, value);
	}
	
	public void AddReplacement(string name, string value)
	{
		m_replacements.Add(name, value);
	}
	
	public void SetVariable(string name, object value)
	{
		m_context.SetVariable(name, value);
	}
	
	public void SetReplacement(string name, string value)
	{
		m_replacements[name] = value;
	}
	
	public void AddExcluded(string name)
	{
		m_context.AddExcluded(name);	
	}
	
	public string Process(string templateName)
	{
		string[] input = DoLoadTemplate(templateName);
		DoConditionalMethodInclusion(input);
		
		DoConditionalLineInclusion();
		DoCodeInjection();
		DoTextReplacement();
		
		var builder = new System.Text.StringBuilder();
		for (int i = 0; i < m_output.Count; ++i)
		{
			if (i == 0 || !m_output[i].IsBlank() || !m_output[i - 1].IsBlank())	// don't write two blank lines in a row
				builder.AppendLine(m_output[i]);
		}
		
		m_output.Clear();
		
		return builder.ToString();
	}
	
	#region Private Methods
	private string[] DoLoadTemplate(string inName)
	{
		string text;

		// When building with studio resource names are prepended with a path
		// like peg-sharp\source\templates and afaict there is no way to use
		// just the file name. So, we have to do a search for the file we want.
		Assembly assembly = Assembly.GetExecutingAssembly();
		string name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(inName));
		
		using (Stream stream = assembly.GetManifestResourceStream(name))	// templates must be utf-8 with unix line endings
		{
			byte[] bytes = new byte[stream.Length];
			stream.Read(bytes, 0, (int) stream.Length);
			
			text = System.Text.Encoding.UTF8.GetString(bytes);
		}
		
		string[] lines = text.Split(new char[]{'\n'}, StringSplitOptions.None);
		return lines;
	}
	
	//< name predicate?	begin method marker
	//> name					end method marker
	private void DoConditionalMethodInclusion(string[] input)
	{
		int i = 0;
		
		// For every line in the input,
		while (i < input.Length)
		{
			Match m1 = m_beginMethodRe.Match(input[i]);
			if (m1.Success)
			{
				// If we matched a begin method marker then figure out if the method should be excluded.
				string name = m1.Groups[1].ToString();
				bool excluded = m_context.IsExcluded(name) || !DoEvaluatePredicate(m1.Groups[2]);
				
				// Process the following lines until we hit the end method marker.
				++i;
				while (i < input.Length)
				{
					// Error to nest begin method markers.
					Match m2 = m_beginMethodRe.Match(input[i]);
					if (m2.Success)
					{
						Console.Error.WriteLine("Expected //> {0} but found //< {1}", name, m2.Groups[1]);
						Environment.Exit(2);
					}
					
					// If we found an end method marker then,
					Match m3 = m_endMethodRe.Match(input[i]);
					if (m3.Success)
					{
						// if it matches what we expect then we are done.
						if (name == m3.Groups[1].ToString())
						{
							++i;
							break;
						}
						else
						{
							// otherwise error out.
							Console.Error.WriteLine("Expected //> {0} but found //> {1}", name, m3.Groups[1]);
							Environment.Exit(2);
						}
					}
					
					// If the method body is not being excluded then add it to the output.
					if (!excluded)
						m_output.Add(input[i]);
					++i;
				}
			}
			else
				// If we did not match a begin method marker then add the line to the output.
				m_output.Add(input[i++]);
		}
	}
	
	// predicate
	private void DoConditionalLineInclusion()
	{
		for (int i = m_output.Count - 1; i >= 0; --i)	// backwards because we remove lines as we iterate
		{
			string line = m_output[i];
			
			Match match = m_lineRe.Match(line);
			if (match.Success)
			{
				bool excluded = !DoEvaluatePredicate(match.Groups[1]);
				
				if (excluded)
				{
					m_output.RemoveAt(i);
				}
				else
				{
					line = line.Substring(0, match.Groups[0].Index).TrimEnd();
					m_output[i] = line;
				}
			}
		}
	}
	
	/* text predicate? */
	private void DoCodeInjection()
	{
		for (int i = 0; i < m_output.Count; ++i)
		{
			string line = m_output[i];
			
			Match match = m_injectionRe.Match(line);
			if (match.Success)
			{
				bool excluded = !DoEvaluatePredicate(match.Groups[2]);
//				Console.WriteLine("matched '{0}' and '{1}' => {2}", match.Groups[1], match.Groups[2], excluded);
				
				line = line.Remove(match.Index, match.Length);
				if (!excluded)
				{
					line = line.Insert(match.Index, match.Groups[1].ToString().Trim());
					m_output[i] = line;
				}
			}
		}
	}
	
	// blah VALUE blah
	private void DoTextReplacement()
	{
		for (int i = 0; i < m_output.Count; ++i)
		{
			string line = m_output[i];
			
			foreach (var entry in m_replacements)
			{
				line = line.Replace(entry.Key, entry.Value);
			}
			
			m_output[i] = line;
		}
	}
	
	private bool DoEvaluatePredicate(Group g)
	{
		bool result = true;
		
		if (g.Success)
		{
			try
			{
				Predicate predicate = m_predicate.Parse(g.ToString());
				result = predicate.EvaluateBool(m_context);
//				Console.WriteLine("{0} => {1}", g, result);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Failed to parse predicate: {0}", g);
				Console.Error.WriteLine(e.Message);
				Environment.Exit(1);
			}
		}
		
		return result;
	}
	
	private static Regex DoMakeRe(string format, params string[] args)
	{
		return new Regex(string.Format(format, args), RegexOptions.IgnorePatternWhitespace);
	}
	#endregion
	
	#region Fields
	private Dictionary<string, string> m_replacements = new Dictionary<string, string>();
	private Context m_context = new Context();
	private PredicateParser m_predicate = new PredicateParser();
	private List<string> m_output = new List<string>();
	
	private const string Name = @"[a-zA-Z_][a-zA-Z0-9_-]*";
	private const string Predicate = @"\{\{([^}]+)\}\}";
	
	private Regex m_beginMethodRe = DoMakeRe(@"^\s* //< \s* ({0}) \s* (?:{1})?", Name, Predicate);
	private Regex m_endMethodRe = DoMakeRe(@"^\s* //> \s* ({0})", Name, Predicate);
	private Regex m_lineRe = DoMakeRe(@"// \s* {0} \s* $", Predicate);
	private Regex m_injectionRe = DoMakeRe(@"/\* \s* (.+?) (?:{0})? \s* \*/", Predicate);
	#endregion
}
