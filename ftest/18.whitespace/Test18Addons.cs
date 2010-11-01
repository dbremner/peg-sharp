using System;
using System.Collections.Generic;

// Custom parse methods.
internal sealed partial class Test18
{
	#region Private Methods
	// Used at the start of a code block to figure out what the new indentation is.
	private bool DoAdjustIndent(int offset)
	{
		bool increased = false;
		
		int line = DoGetLine(offset);
		
		int currentIndent = DoGetIndent(line);
		int prevIndent = DoGetIndent(line - 1);
		if (currentIndent > prevIndent)
		{
			// Setting m_indent will affect whether or not Statement rules parse.
			// In order for parsing to work correctly the memoization we do with 
			// m_cache must take into account m_indent so we have to set m_context
			// whenever we change m_indent.
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
