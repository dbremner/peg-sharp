using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
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
		Col = col;
		Context = DoGetContext(offset, input);
	}
	
	public string Context {get; private set;}
	
	public int Col {get; private set;}
	
	#region Private Methods
	[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
	private ParserException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}
	
	private string DoGetContext(int offset, string input)
	{
	    Contract.Requires(input != null);
	    int begin = offset;
		while (begin > 0 && input[begin - 1] != '\n' && input[begin - 1] != '\r')
			--begin;
		
		int end = Math.Max(offset, begin);
		while (end < input.Length && input[end] != '\n' && input[end] != '\r')
			++end;
		
		int len = end - begin;
		if (len < 57)
			return input.Substring(begin, len);
		else
			return input.Substring(begin, 57).TrimEnd() + "...";
	}
	#endregion
}
