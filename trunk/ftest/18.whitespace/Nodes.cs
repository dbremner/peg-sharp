using System;
using System.Collections.Generic;
using System.Text;

internal sealed class BlockNode : Node
{
	public BlockNode(IEnumerable<Node> statements)
	{
		m_statements = statements;
	}
	
	public override string ToText(int indent = 0)
	{
		var builder = new StringBuilder();
		
		foreach (Node statement in m_statements)
		{
			builder.Append(statement.ToText(indent));
		}
		
		return builder.ToString();
	}
	
	private IEnumerable<Node> m_statements;
}

internal sealed class FunctionNode : Node
{
	public FunctionNode(string name, Node block)
	{
		m_name = name;
		m_block = block;
	}
	
	public override string ToText(int indent = 0)
	{
		var builder = new StringBuilder();
		
		builder.Append(' ', 3*indent);
		builder.AppendFormat("def {0}:", m_name);
		builder.AppendLine();
		
		builder.Append(m_block.ToText(indent + 1));
		
		return builder.ToString();
	}
	
	private string m_name;
	private Node m_block;
}

internal sealed class IfNode : Node
{
	public IfNode(string predicate, Node block)
	{
		m_predicate = predicate;
		m_block = block;
	}
	
	public override string ToText(int indent = 0)
	{
		var builder = new StringBuilder();
		
		builder.Append(' ', 3*indent);
		builder.AppendFormat("if {0}:", m_predicate);
		builder.AppendLine();
		
		builder.Append(m_block.ToText(indent + 1));
		
		return builder.ToString();
	}
	
	private string m_predicate;
	private Node m_block;
}

internal sealed class PassNode : Node
{
	public PassNode()
	{
	}
	
	public override string ToText(int indent = 0)
	{
		var builder = new StringBuilder();
		
		builder.Append(' ', 3*indent);
		builder.AppendLine("pass");
		
		return builder.ToString();
	}
}

internal sealed class ProgramNode : Node
{
	public ProgramNode(IEnumerable<Node> functions)
	{
		m_functions = functions;
	}
	
	public override string ToText(int indent = 0)
	{
		var builder = new StringBuilder();
		
		foreach (Node function in m_functions)
		{
			builder.AppendLine(function.ToText(indent));
			builder.AppendLine();
		}
		
		return builder.ToString();
	}
	
	private IEnumerable<Node> m_functions;
}
