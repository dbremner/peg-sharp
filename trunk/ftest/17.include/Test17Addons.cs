using System;
using System.Collections.Generic;

// Custom parse methods.
internal sealed partial class Test17
{
	private double EvaluateBinary(List<Result> results)
	{
		double result = results[0].Value;
		
		for (int i = 1; i < results.Count; i += 2)
		{
			switch (results[i].Text)
			{
				case "+":
					result += results[i + 1].Value;
					break;
					
				case "-":
					result -= results[i + 1].Value;
					break;
					
				case "*":
					result *= results[i + 1].Value;
					break;
					
				case "/":
					result /= results[i + 1].Value;
					break;
					
				default:
					throw new Exception("unexpected operator: " + results[i].Text);
			}
		}
		
		return result;
	}
}
