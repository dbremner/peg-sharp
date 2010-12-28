using System;
using System.Collections.Generic;

internal sealed partial class Test13
{
	public string Parse(string input)
	{
		DoParseFile(input, null, "Expr");
		
		return Unconsumed;
	}
}
