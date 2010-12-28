using System;
using System.Collections.Generic;
using System.Xml;

internal sealed partial class Test16
{
	public double Parse(string input)
	{
		XmlNode node = DoParseFile(input, null, "Expr");
		
		return DoEvaluate(node.ChildNodes[0]);
	}
	
	private static double DoEvaluate(XmlNode node)
	{
		double result;
//		Console.WriteLine("{0} with {1} children and value {2}", node.Name, node.ChildNodes.Count, node.Value);
		
		switch (node.Name)
		{
			case "Expr":
				result = DoEvaluate(node.ChildNodes[0]);
				break;
			
			case "Sum":
				result = DoEvaluate(node.ChildNodes[0]);
				for (int i = 1; i < node.ChildNodes.Count; i += 2)
				{
					if (node.ChildNodes[i].Value == "+")
						result += DoEvaluate(node.ChildNodes[i + 1]);
					else
						result -= DoEvaluate(node.ChildNodes[i + 1]);
				}
				break;
			
			case "Product":
				result = DoEvaluate(node.ChildNodes[0]);
				for (int i = 1; i < node.ChildNodes.Count; i += 2)
				{
					if (node.ChildNodes[i].Value == "*")
						result *= DoEvaluate(node.ChildNodes[i + 1]);
					else
						result /= DoEvaluate(node.ChildNodes[i + 1]);
				}
				break;
			
			case "Value":
				if (node.ChildNodes[0].Value == "(")
					result = DoEvaluate(node.ChildNodes[1]);
				else
					result = double.Parse(node.InnerText.Trim());
				break;
			
			default:
				throw new Exception("Unexpected node: " + node.Name);
		}
		
		return result;
	}
}
