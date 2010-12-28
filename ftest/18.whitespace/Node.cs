using System;

internal abstract class Node
{
	public abstract string ToText(int indent = 0);
	
	public override string ToString()
	{
		return ToText();
	}
}
