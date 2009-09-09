using System;
using System.Collections.Generic;

internal abstract class Expression
{
	public abstract int Evaluate(Dictionary<string, int> context);
	
	public abstract string ToText(int depth);
	
	public override string ToString()
	{
		return ToText(0);
	}
	
	protected string OnFormat(int depth, string format, params object[] args)
	{
		string padding = new string(' ', 4*depth);
		return padding + string.Format(format, args);
	}
}
