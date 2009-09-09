using System;
using System.Collections.Generic;

internal sealed partial class Test11
{
	// Assumes that the operators are left associative.
	private Expression DoCreateBinary(List<Result> results)
	{
		Expression result = results[0].Value;
		
		for (int i = 1; i < results.Count; i += 2)
		{
			result = new BinaryExpression(result, results[i + 1].Value, results[i].Text);
		}
		
		return result;
	}
}
