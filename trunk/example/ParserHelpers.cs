using System;
using System.Collections.Generic;

// This is the hand-written code used by the semantic actions for the 
// machine generated parser.
internal sealed partial class Parser
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
