using System;
using System.Collections.Generic;

internal sealed class AssignmentExpression : Expression
{
	public AssignmentExpression(string name, Expression value)
	{
		Name = name;
		Value = value;
	}
	
	public string Name {get; private set;}
	
	public Expression Value {get; private set;}
	
	public override Expression Evaluate(Dictionary<string, Expression> context)
	{
		Expression result = Value.Evaluate(context);
		context[Name] = result;
		return result;
	}
	
	public override string ToString()
	{
		return Name + " = " + Value.ToString();
	}
}

internal sealed class BinaryExpression : Expression
{
	public BinaryExpression(Expression lhs, Expression rhs, string op)
	{
		Operator = op;
		Left = lhs;
		Right = rhs;
	}
	
	public string Operator {get; private set;}
	
	public Expression Left {get; private set;}
	
	public Expression Right {get; private set;}
	
	public override Expression Evaluate(Dictionary<string, Expression> context)
	{
		Expression lhs = Left.Evaluate(context);
		Expression rhs = Right.Evaluate(context);
		
		Expression result;
		if (lhs is FloatExpression && rhs is FloatExpression)
			result = DoEval((FloatExpression) lhs, (FloatExpression) rhs);
		
		else if (lhs is IntegerExpression && rhs is IntegerExpression)
			result = DoEval((IntegerExpression) lhs, (IntegerExpression) rhs);
		
		else if (lhs is FloatExpression && rhs is IntegerExpression)
			result = DoEval((FloatExpression) lhs, new FloatExpression(rhs.ToString()));
		
		else if (lhs is IntegerExpression && rhs is FloatExpression)
			result = DoEval(new FloatExpression(lhs.ToString()), (FloatExpression) rhs);
			
		else
			result = new BinaryExpression(lhs, rhs, Operator);
		
		return result;
	}
	
	public override string ToString()
	{
		return string.Format("({0} {1} {2})", Operator, Left, Right);
	}
	
	private Expression DoEval(FloatExpression lhs, FloatExpression rhs)
	{
		double result;
		
		switch (Operator)
		{
			case "+":
				result = lhs.Value + rhs.Value;
				break;
			
			case "-":
				result = lhs.Value - rhs.Value;
				break;
			
			case "*":
				result = lhs.Value * rhs.Value;
				break;
			
			case "/":
				result = lhs.Value / rhs.Value;
				break;
			
			default:
				throw new InvalidOperationException("Bad binary operator: " + Operator);
		}
		
		return new FloatExpression(result);
	}
	
	private Expression DoEval(IntegerExpression lhs, IntegerExpression rhs)
	{
		long result;
		
		switch (Operator)
		{
			case "+":
				result = lhs.Value + rhs.Value;
				break;
			
			case "-":
				result = lhs.Value - rhs.Value;
				break;
			
			case "*":
				result = lhs.Value * rhs.Value;
				break;
			
			case "/":
				result = lhs.Value / rhs.Value;
				break;
			
			default:
				throw new InvalidOperationException("Bad binary operator: " + Operator);
		}
		
		return new IntegerExpression(result);
	}
}

internal sealed class FloatExpression : Expression
{
	public FloatExpression(string value)
	{
		Value = double.Parse(value);
	}
	
	public FloatExpression(double value)
	{
		Value = value;
	}
	
	public double Value {get; private set;}
	
	public override Expression Evaluate(Dictionary<string, Expression> context)
	{
		return this;
	}
	
	public override string ToString()
	{
		return Value.ToString();
	}
}

internal sealed class IntegerExpression : Expression
{
	public IntegerExpression(string value)
	{
		Value = long.Parse(value);
	}
	
	public IntegerExpression(long value)
	{
		Value = value;
	}
	
	public long Value {get; private set;}
	
	public override Expression Evaluate(Dictionary<string, Expression> context)
	{
		return this;
	}
	
	public override string ToString()
	{
		if (Program.OutBase == 10)
			return Value.ToString();
		
		else if (Program.OutBase == 16)
			return string.Format("0x{0:X}", Value);
		
		else if (Program.OutBase == 2)
			return "0b" + DoGetBinaryString(unchecked((ulong) Value));
		
		throw new InvalidOperationException("$base should be 2, 10, or 16.");
	}
	
	private string DoGetBinaryString(ulong value)
	{
		var builder = new System.Text.StringBuilder();
		
		ulong bit = 1UL << 63;
		while (bit != 0)
		{
			if ((value & bit) != 0)
				builder.Append('1');
			else if (builder.Length > 0)
				builder.Append('0');
				
			bit >>= 1;
		}
		
		return builder.ToString();
	}
}

internal sealed class VariableExpression : Expression
{
	public VariableExpression(string name)
	{
		Name = name;
	}
	
	public string Name {get; private set;}
	
	public override Expression Evaluate(Dictionary<string, Expression> context)
	{
		Expression result;
		
		// Note that we need to evaluate whatever we get because the epressions
		// may contain variables (and we can't save the result because the context
		// may change later).
		if (Name == "$last")
			result = Program.LastValue.Evaluate(context);
			
		else if (context.TryGetValue(Name, out result))
			result = result.Evaluate(context);
			
		else
			result = this;
		
		return result;
	}
	
	public override string ToString()
	{
		return Name;
	}
}
