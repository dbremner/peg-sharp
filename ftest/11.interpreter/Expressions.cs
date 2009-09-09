using System;
using System.Collections.Generic;

internal sealed class AssignmentExpression : Expression
{
	public AssignmentExpression(string name, Expression value)
	{
		m_name = name;
		m_value = value;
	}
	
	public override int Evaluate(Dictionary<string, int> context)
	{
		int value = m_value.Evaluate(context);
		
		context[m_name] = value;
		
		return value;
	}
	
	public override string ToText(int depth)
	{
		return OnFormat(depth, "{0} = {1}", m_name, m_value);
	}
	
	private string m_name;	
	private Expression m_value;
}

internal sealed class BinaryExpression : Expression
{
	public BinaryExpression(Expression lhs, Expression rhs, string op)
	{
		m_lhs = lhs;
		m_rhs = rhs;
		m_op = op;
	}
	
	public override int Evaluate(Dictionary<string, int> context)
	{
		int value;
		
		switch (m_op)
		{
			case "+":
				value = m_lhs.Evaluate(context) + m_rhs.Evaluate(context);
				break;
			
			case "-":
				value = m_lhs.Evaluate(context) - m_rhs.Evaluate(context);
				break;
			
			case "*":
				value = m_lhs.Evaluate(context) * m_rhs.Evaluate(context);
				break;
			
			case "/":
				value = m_lhs.Evaluate(context) / m_rhs.Evaluate(context);
				break;
			
			case "<=":
				value = m_lhs.Evaluate(context) <= m_rhs.Evaluate(context) ? 1 : 0;
				break;
			
			case ">=":
				value = m_lhs.Evaluate(context) >= m_rhs.Evaluate(context) ? 1 : 0;
				break;
			
			case "==":
				value = m_lhs.Evaluate(context) == m_rhs.Evaluate(context) ? 1 : 0;
				break;
			
			case "!=":
				value = m_lhs.Evaluate(context) != m_rhs.Evaluate(context) ? 1 : 0;
				break;
			
			case "<":
				value = m_lhs.Evaluate(context) < m_rhs.Evaluate(context) ? 1 : 0;
				break;
			
			case ">":
				value = m_lhs.Evaluate(context) > m_rhs.Evaluate(context) ? 1 : 0;
				break;
			
			default:
				throw new Exception("Bad operator: " + m_op);
		}
		
		return value;
	}
	
	public override string ToText(int depth)
	{
		return OnFormat(depth, "({0} {1} {2})",m_lhs, m_op, m_rhs);
	}
	
	private Expression m_lhs;
	private Expression m_rhs;
	private string m_op;
}

internal sealed class IfExpression : Expression
{
	public IfExpression(Expression predicate, Expression case1, Expression case2)
	{
		m_predicate = predicate;
		m_case1 = case1;
		m_case2 = case2;
	}
	
	public override int Evaluate(Dictionary<string, int> context)
	{
		int value;
		
		if (m_predicate.Evaluate(context) != 0)
			value = m_case1.Evaluate(context);
		else
			value = m_case2.Evaluate(context);
		
		return value;
	}
	
	public override string ToText(int depth)
	{
		var builder = new System.Text.StringBuilder();
		
		builder.Append(OnFormat(depth, "if "));
		builder.Append(m_predicate.ToText(0));
		builder.Append(OnFormat(depth, " then{0}", Environment.NewLine));
		builder.Append(m_case1.ToText(depth + 1));
		builder.Append(OnFormat(depth, "else{0}", Environment.NewLine));
		builder.Append(m_case2.ToText(depth + 1));
		
		return builder.ToString();
	}
	
	private Expression m_predicate;
	private Expression m_case1;
	private Expression m_case2;
}

internal sealed class LiteralExpression : Expression
{
	public LiteralExpression(int value)
	{
		m_value = value;
	}
	
	public override int Evaluate(Dictionary<string, int> context)
	{
		return m_value;
	}
	
	public override string ToText(int depth)
	{
		return OnFormat(depth, "{0}", m_value);
	}
	
	private int m_value;
}

internal sealed class SequenceExpression : Expression
{
	public SequenceExpression(IEnumerable<Expression> expressions)
	{
		m_expressions = expressions;
	}
	
	public override int Evaluate(Dictionary<string, int> context)
	{
		int value = 0;
		
		foreach (Expression e in m_expressions)
		{
			value = e.Evaluate(context);
		}
		
		return value;
	}
	
	public override string ToText(int depth)
	{
		var builder = new System.Text.StringBuilder();
		
		foreach (Expression e in m_expressions)
		{
			builder.Append(e.ToText(depth));
			builder.Append(Environment.NewLine);
		}
		
		return builder.ToString();
	}
	
	private IEnumerable<Expression> m_expressions;
}

internal sealed class VariableExpression : Expression
{
	public VariableExpression(string name)
	{
		m_name = name;
	}
	
	public override int Evaluate(Dictionary<string, int> context)
	{
		int value;
		if (!context.TryGetValue(m_name, out value))
			throw new Exception(m_name + " doesn't have a value.");
		
		return value;
	}
	
	public override string ToText(int depth)
	{
		return OnFormat(depth, "{0}", m_name);
	}
	
	private string m_name;
}

internal sealed class WhileExpression : Expression
{
	public WhileExpression(Expression predicate, Expression body)
	{
		m_predicate = predicate;
		m_body = body;
	}
	
	public override int Evaluate(Dictionary<string, int> context)
	{
		int value = 0;
		
		while (m_predicate.Evaluate(context) != 0)
		{
			value = m_body.Evaluate(context);
		}
		
		return value;
	}
	
	public override string ToText(int depth)
	{
		var builder = new System.Text.StringBuilder();
		
		builder.Append(OnFormat(depth, "while "));
		builder.Append(m_predicate.ToText(0));
		builder.Append(OnFormat(depth, " do{0}", Environment.NewLine));
		builder.Append(m_body.ToText(depth + 1));
		
		return builder.ToString();
	}
	
	private Expression m_predicate;
	private Expression m_body;
}

