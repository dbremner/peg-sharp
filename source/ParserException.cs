using System;
using System.Collections.Generic;
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
	
	private string DoGetContext(int index, string input)
	{
		int begin = index;
		while (begin > 0 && input[begin] != '\n' && input[begin] != '\r')
			--begin;
		
		int end = index;
		while (end + 1 < input.Length && input[end + 1] != '\n' && input[end + 1] != '\r')
			++end;
		
		int len = Math.Min(end - begin, 60);
		return input.Substring(begin, len);
	}
	#endregion
}
