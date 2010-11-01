using System;
using System.Collections.Generic;

// Custom parse methods.
internal sealed partial class Test18
{
	partial void OnParseProlog()
	{
		m_lineStarts = null;
	}
	
	#region Private Methods
//	private static readonly string RightArrow = "\x2192";
//	private static readonly string DownArrow = "\x2193";
//	private static readonly string DownHookedArrow = "\x21A9";
	
	// TODO: 
	// need to generate this
	private void DoDebug(int oldOffset, int newOffset, string text)
	{
#if NOT_YET
		const int Width = 30;
		
		// Write the input centered on the new offset.
		int offset = Math.Max(newOffset - Width, 0);
		int count1 = Math.Min(m_input.Length - offset, Width);
		string before = m_input.Substring(offset, count1);
		if (offset > 0)
			before = "..." + before;
		
		int count2 = Math.Min(m_input.Length - newOffset, Width);
		string after = m_input.Substring(newOffset, count2);
		if (newOffset + count2 < m_input.Length)
			after += "...";
		
		before = before.Replace("\t", RightArrow);
		before = before.Replace("\n", DownArrow);
		before = before.Replace("\r", DownHookedArrow);
		
		after = after.Replace("\t", RightArrow);
		after = after.Replace("\n", DownArrow);
		after = after.Replace("\r", DownHookedArrow);
		
		if (after.EndsWith("\x00"))
			after = after.Substring(0, after.Length - 1);
		
		// If we matched then write an arrow pointing to the new offset.
		int padding = before.StartsWith("...") ? 3 : 0;
		if (newOffset > oldOffset)
		{
			Console.WriteLine("{0}{1}", before, after);
			
			Console.Write(new string(' ', padding + Math.Max(oldOffset - offset, 0)));
			Console.Write(new string('_', Math.Min(newOffset - oldOffset, before.Length - padding)));
			Console.Write("^ ");
			Console.WriteLine(text);
			Console.WriteLine();
		}
		else
		{
			// If we failed then write an arrow pointing to the old offset.
//			Console.WriteLine("{0}{1}", before, after);
//			
//			Console.Write(new string(' ', padding + newOffset - offset));
//			Console.Write("^ ");
//			Console.Write(new string('_', oldOffset - newOffset));
//			Console.WriteLine(text);
//			Console.WriteLine();
		}
#endif
	}
	
	// Used at the start of a code block to figure out what the new indentation is.
	private bool DoAdjustIndent(int offset)
	{
		bool increased = false;
		
		int line = DoGetLine(offset);
		
		int currentIndent = DoGetIndent(line);
		int prevIndent = DoGetIndent(line - 1);
		if (currentIndent > prevIndent)
		{
			m_indent = currentIndent;
			m_context = m_indent;
			increased = true;
		}
		
		return increased;
	}
	
	private void DoRestoreIndent(int oldIndent)
	{
		m_indent = oldIndent;
		m_context = m_indent;
	}
	
	// Used for each statement in a block to determine if the statement is part of the block.
	private bool DoIndentMatches(int offset)
	{
		int line = DoGetLine(offset);
		int indent = DoGetIndent(line);
		bool matches = indent == m_indent;
		
		return matches;
	}
	
	private int DoGetIndent(int line)
	{
		int indent = 0;
		
		if (line > 0)
		{
			int start = m_lineStarts[line - 1];
			
			while (start + indent < m_input.Length && m_input[start + indent] == ' ')
				++indent;
		}
		
		return indent;
	}
	#endregion
	
	#region Fields
	private int m_indent = 0;
	#endregion
}
