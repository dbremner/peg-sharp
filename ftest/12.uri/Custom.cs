using System;
using System.Collections.Generic;

internal sealed partial class Test12
{
	public int Parse(string uri)
	{
		int count = DoParseFile(uri, null, "uri");
		
		if (count != uri.Length)
			throw new ParserException(1, 1, 0, "blah", uri, string.Format("Only matched '{0}' from '{1}'.", uri.Substring(0, count), uri));
		
		return 1;
	}
}
