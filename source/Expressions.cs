// Copyright (C) 2009 Jesse Jones
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

// Concrete classes for expressions which appear in rules.
internal sealed class AssertExpression : Expression
{
	public AssertExpression(Expression expression)
	{
		Contract.Requires(expression != null, "expression is null");
		
		Expression = expression;
	}
	
	public Expression Expression {get; private set;}
	
	public override Used FindUsed()
	{
		Used used = Used.Assert;
		
		used |= Expression.FindUsed();
		
		return used;
	}
	
	public override IEnumerable<Expression> Select(Predicate<Expression> predicate)
	{
		if (predicate(this))
			yield return this;
		
		foreach (Expression f in Expression.Select(predicate))
		{
			yield return f;
		}
	}
	
	public override string[] GetLeftRules()
	{
		return Expression.GetLeftRules();
	}
	
	public override void Write(StringBuilder line, int depth)
	{
		if (depth == 0)
		{
			line.Append("state = DoAssert(state, results,");
			Expression.Write(line, depth + 1);
			line.Append(")");
		}
		else
		{
			string s = depth == 1 ? "s" : ("s" + depth);
			string r = depth == 1 ? "r" : ("r" + depth);
			string t = new string('\t', 2 + depth);
			line.AppendFormat("{0}{1}({2}, {3}) => DoAssert({2}, {3},", Environment.NewLine, t, s, r);
			Expression.Write(line, depth + 1);
			line.Append(")");
		}
	}
	
	public override string ToSubString()
	{
		return ToString();
	}
	
	public override string ToString()
	{
		var result = new System.Text.StringBuilder();
		
		result.Append('&');
		result.Append(Expression.ToSubString());
		
		return result.ToString();
	}
}

internal sealed class ChoiceExpression : Expression
{
	public ChoiceExpression(Expression[] expressions)
	{
		Contract.Requires(expressions != null, "expressions is null");
		Contract.Requires(expressions.Length >= 2, "expressions length is less than two");
		Contract.Requires(Contract.ForAll(expressions, e => e != null));
		
		Expressions = expressions;
	}
	
	public Expression[] Expressions {get; private set;}
	
	public override Used FindUsed()
	{
		Used used = Used.Choice;
		
		foreach (Expression e in Expressions)
		{
			used |= e.FindUsed();
		}
		
		return used;
	}
	
	public override IEnumerable<Expression> Select(Predicate<Expression> predicate)
	{
		if (predicate(this))
			yield return this;
		
		foreach (Expression e in Expressions)
		{
			foreach (Expression f in e.Select(predicate))
			{
				yield return f;
			}
		}
	}
	
	public override string[] GetLeftRules()
	{
		return (from e in Expressions from l in e.GetLeftRules() select l).ToArray();
	}
	
	public override void Write(StringBuilder line, int depth)
	{
		string args = DoGetArgs(depth + 1);
		if (depth == 0)
		{
			line.AppendFormat("state = DoChoice(state, results{0})", args);
		}
		else
		{
			string s = depth == 1 ? "s" : ("s" + depth);
			string r = depth == 1 ? "r" : ("r" + depth);
			string t = new string('\t', 2 + depth);
			line.AppendFormat("{0}{1}({2}, {3}) => DoChoice({2}, {3}{4})", Environment.NewLine, t, s, r, args);
		}
	}
	
	public override string ToString()
	{
		var result = new System.Text.StringBuilder();
		
		for (int i = 0; i < Expressions.Length; ++i)
		{
			result.Append(Expressions[i].ToSubString());
			
			if (i + 1 < Expressions.Length)
				result.Append(" / ");
		}
		
		return result.ToString();
	}
	
	private string DoGetArgs(int depth)
	{
		var line = new StringBuilder();
		
		foreach (Expression e in Expressions)
		{
			line.Append(",");
			e.Write(line, depth);
		}
		
		return line.ToString();
	}
}

internal sealed class LiteralExpression : Expression
{
	public LiteralExpression(string literal)
	{
		Contract.Requires(!string.IsNullOrEmpty(literal), "literal is null or empty");
		
		Literal = literal;
	}
	
	public string Literal {get; private set;}
	
	public override Used FindUsed()
	{
		return Used.Literal;
	}
	
	public override void Write(StringBuilder line, int depth)
	{
		if (depth == 0)
		{
			line.AppendFormat("state = DoParseLiteral(state, results, \"{0}\")", Literal);
		}
		else
		{
			string s = depth == 1 ? "s" : ("s" + depth);
			string r = depth == 1 ? "r" : ("r" + depth);
			string t = new string('\t', 2 + depth);
			line.AppendFormat("{0}{1}({2}, {3}) => DoParseLiteral({2}, {3}, \"{4}\")", Environment.NewLine, t, s, r, Literal);
		}
	}
	
	public override IEnumerable<Expression> Select(Predicate<Expression> predicate)
	{
		if (predicate(this))
			yield return this;
	}
	
	public override string[] GetLeftRules()
	{
		return new string[0];
	}
	
	public override string ToSubString()
	{
		return ToString();
	}
	
	public override string ToString()
	{
		return "'" + Literal.EscapeAll() + "'";
	}
}

internal sealed class NAssertExpression : Expression
{
	public NAssertExpression(Expression expression)
	{
		Contract.Requires(expression != null, "expression is null");
		
		Expression = expression;
	}
	
	public Expression Expression {get; private set;}
	
	public override Used FindUsed()
	{
		Used used = Used.NAssert;
		
		used |= Expression.FindUsed();
		
		return used;
	}
	
	public override IEnumerable<Expression> Select(Predicate<Expression> predicate)
	{
		if (predicate(this))
			yield return this;
		
		foreach (Expression f in Expression.Select(predicate))
		{
			yield return f;
		}
	}
	
	public override string[] GetLeftRules()
	{
		return Expression.GetLeftRules();
	}
	
	public override void Write(StringBuilder line, int depth)
	{
		if (depth == 0)
		{
			line.Append("state = DoNAssert(state, results,");
			Expression.Write(line, depth + 1);
			line.Append(")");
		}
		else
		{
			string s = depth == 1 ? "s" : ("s" + depth);
			string r = depth == 1 ? "r" : ("r" + depth);
			string t = new string('\t', 2 + depth);
			line.AppendFormat("{0}{1}({2}, {3}) => DoNAssert({2}, {3},", Environment.NewLine, t, s, r);
			Expression.Write(line, depth + 1);
			line.Append(")");
		}
	}
	
	public override string ToSubString()
	{
		return ToString();
	}
	
	public override string ToString()
	{
		var result = new System.Text.StringBuilder();
		
		result.Append('!');
		result.Append(Expression.ToSubString());
		
		return result.ToString();
	}
}

internal sealed class RangeExpression : Expression
{
	public RangeExpression(string text)
	{
		Contract.Requires(!string.IsNullOrEmpty(text), "text is null or empty");
		
		var chars = new System.Text.StringBuilder();
		var ranges = new System.Text.StringBuilder();
		var categories = new List<string>();
		CategoryLabel = string.Empty;
		
		Inverted = text[0] == '^';
		
		int i = Inverted? 1 : 0;
		while (i < text.Length)
		{
			if (i + 3 < text.Length && text[i] == '\\' && text[i + 1] == 'c')
			{
				categories.Add(DoGetCategory(text.Substring(i + 2, 2)));
				CategoryLabel += "\\\\c" + text.Substring(i+ 2, 2);
				i += 4;
			}
			else if (i + 2 < text.Length && text[i + 1] == '-')	// note that - has no effect at the start or the end of the text
			{
				ranges.Append(text.Substring(i, 1) + text[i + 2]);
				i += 3;
			}
			else
			{
				chars.Append(text[i++]);
			}
		}
		
		Chars = chars.ToString();
		Ranges = ranges.ToString();
		if (categories.Count > 0)
			Categories = "new UnicodeCategory[]{" + string.Join(", ", categories.ToArray()) + "}";
		else
			Categories = "null";
	}
	
	public string Chars {get; private set;}
	
	public string Ranges {get; private set;}
	
	public string Categories {get; private set;}
	
	public string CategoryLabel {get; private set;}
	
	public bool Inverted {get; private set;}
	
	public override Used FindUsed()
	{
		return Used.Range;
	}
	
	public override string[] GetLeftRules()
	{
		return new string[0];
	}
	
	public override void Write(StringBuilder line, int depth)
	{
		string chars = Chars.Length > 0 ? "\"" + Chars.EscapeAll() + "\"" : "string.Empty";
		string ranges = Ranges.Length > 0 ? "\"" + Ranges.EscapeAll() + "\"" : "string.Empty";
		
		if (depth == 0)
		{
			line.AppendFormat("state = DoParseRange(state, results, {0}, {1}, {2}, {3}, \"{4}\")",
				Inverted ? "true" : "false", chars, ranges, Categories, this);
		}
		else
		{
			string s = depth == 1 ? "s" : ("s" + depth);
			string r = depth == 1 ? "r" : ("r" + depth);
			string t = new string('\t', 2 + depth);
			
			line.AppendFormat("{0}{1}({2}, {3}) => DoParseRange({2}, {3}, {4}, {5}, {6}, {7}, \"{8}\")",
				Environment.NewLine, t, s, r, Inverted ? "true" : "false", chars, ranges, Categories, this);
		}
	}
	
	public override IEnumerable<Expression> Select(Predicate<Expression> predicate)
	{
		if (predicate(this))
			yield return this;
	}
	
	public override string ToSubString()
	{
		return ToString();
	}
	
	public override string ToString()
	{
		var builder = new StringBuilder();
		
		if (Chars.Length == 0 && Ranges == "\x0001\xFFFF")
		{
			builder.Append('.');
		}
		else
		{
			builder.Append('[');
			if (Inverted)
				builder.Append('^');
			if (Chars.Length > 0)
				builder.Append(DoEscape(Chars));
			builder.Append(CategoryLabel);
			for (int i = 0; i < Ranges.Length; i += 2)
			{
				builder.Append(DoEscape(Ranges.Substring(i, 1)));
				builder.Append('-');
				builder.Append(DoEscape(Ranges.Substring(i + 1, 1)));
			}
			builder.Append(']');
		}
		
		return builder.ToString();
	}
	
	private string DoEscape(string s)
	{
		return s.EscapeAll().Replace("]", "\\\\]");
	}
	
	private string DoGetCategory(string s)
	{
		string result;
		
		switch (s)
		{
			case "Lu":
				result = "UnicodeCategory.UppercaseLetter";
				break;
				
			case "Ll":
				result = "UnicodeCategory.LowercaseLetter";
				break;
				
			case "Lt":
				result = "UnicodeCategory.TitlecaseLetter";
				break;
				
			case "Lm":
				result = "UnicodeCategory.ModifierLetter";
				break;
				
			case "Lo":
				result = "UnicodeCategory.OtherLetter";
				break;
				
			case "Mn":
				result = "UnicodeCategory.NonSpacingMark";
				break;
				
			case "Mc":
				result = "UnicodeCategory.SpacingCombiningMark";
				break;
				
			case "Me":
				result = "UnicodeCategory.EnclosingMark";
				break;
				
			case "Nd":
				result = "UnicodeCategory.DecimalDigitNumber";
				break;
				
			case "Nl":
				result = "UnicodeCategory.LetterNumber";
				break;
				
			case "No":
				result = "UnicodeCategory.OtherNumber";
				break;
				
			case "Zs":
				result = "UnicodeCategory.SpaceSeparator";
				break;
				
			case "Zl":
				result = "UnicodeCategory.LineSeparator";
				break;
				
			case "Zp":
				result = "UnicodeCategory.ParagraphSeparator";
				break;
				
			case "Cc":
				result = "UnicodeCategory.Control";
				break;
				
			case "Cf":
				result = "UnicodeCategory.Format";
				break;
				
			case "Cs":
				result = "UnicodeCategory.Surrogate";
				break;
				
			case "Co":
				result = "UnicodeCategory.PrivateUse";
				break;
				
			case "Pc":
				result = "UnicodeCategory.ConnectorPunctuation";
				break;
				
			case "Pd":
				result = "UnicodeCategory.DashPunctuation";
				break;
				
			case "Ps":
				result = "UnicodeCategory.OpenPunctuation";
				break;
				
			case "Pe":
				result = "UnicodeCategory.ClosePunctuation";
				break;
				
			case "Pi":
				result = "UnicodeCategory.InitialQuotePunctuation";
				break;
				
			case "Pf":
				result = "UnicodeCategory.FinalQuotePunctuation";
				break;
				
			case "Po":
				result = "UnicodeCategory.OtherPunctuation";
				break;
				
			case "Sm":
				result = "UnicodeCategory.MathSymbol";
				break;
				
			case "Sc":
				result = "UnicodeCategory.CurrencySymbol";
				break;
				
			case "Sk":
				result = "UnicodeCategory.ModifierSymbol";
				break;
				
			case "So":
				result = "UnicodeCategory.OtherSymbol";
				break;
				
			case "Cn":
				result = "UnicodeCategory.OtherNotAssigned";
				break;
				
			default:
				throw new ArgumentException(s + " is not a valid Unicode character category.");
		}
		
		return result;
	}
}

internal sealed class RepetitionExpression : Expression
{
	public RepetitionExpression(Expression expression, int min, int max)
	{
		Contract.Requires(expression != null, "expression is null");
		Contract.Requires(min >= 0, "min is negative");
		Contract.Requires(min <= max, "mismatched min and max");
		
		Expression = expression;
		Min = min;
		Max = max;
	}
	
	public Expression Expression {get; private set;}
	
	public int Min {get; private set;}
	
	public int Max {get; private set;}
	
	public override Used FindUsed()
	{
		Used used = Used.Repetition;
		
		used |= Expression.FindUsed();
		
		return used;
	}
	
	public override IEnumerable<Expression> Select(Predicate<Expression> predicate)
	{
		if (predicate(this))
			yield return this;
		
		foreach (Expression f in Expression.Select(predicate))
		{
			yield return f;
		}
	}
	
	public override string[] GetLeftRules()
	{
		return Expression.GetLeftRules();
	}
	
	public override void Write(StringBuilder line, int depth)
	{
		if (depth == 0)
		{
			line.AppendFormat("state = DoRepetition(state, results, {0}, {1},", Min, Max);
			Expression.Write(line, depth + 1);
			line.Append(")");
		}
		else
		{
			string s = depth == 1 ? "s" : ("s" + depth);
			string r = depth == 1 ? "r" : ("r" + depth);
			string t = new string('\t', 2 + depth);
			line.AppendFormat("{0}{1}({2}, {3}) => DoRepetition({2}, {3}, {4}, {5},", Environment.NewLine, t, s, r, Min, Max);
			Expression.Write(line, depth + 1);
			line.Append(")");
		}
	}
	
	public override string ToSubString()
	{
		return ToString();
	}
	
	public override string ToString()
	{
		var result = new System.Text.StringBuilder();
		
		result.Append(Expression.ToSubString());
		
		if (Min == 0 && Max == 1)
		{
			result.Append('?');
		}
		else if (Min == 0 && Max == int.MaxValue)
		{
			result.Append('*');
		}
		else if (Min == 1 && Max == int.MaxValue)
		{
			result.Append('+');
		}
		else if (Max == int.MaxValue)
		{
			result.Append('{');
			result.Append(Min.ToString());
			result.Append(',');
			result.Append('}');
		}
		else
		{
			result.Append('{');
			result.Append(Min.ToString());
			result.Append(',');
			result.Append(' ');
			result.Append(Max.ToString());
			result.Append('}');
		}
		
		return result.ToString();
	}
}

internal sealed class RuleExpression : Expression
{
	public RuleExpression(string name)
	{
		Contract.Requires(!string.IsNullOrEmpty(name), "name is null or empty");
		
		Name = name;
	}
	
	public string Name {get; private set;}
	
	public override Used FindUsed()
	{
		return 0;
	}
	
	public override string[] GetLeftRules()
	{
		return new string[]{Name};
	}
	
	public override void Write(StringBuilder line, int depth)
	{
		if (depth == 0)
		{
			line.Append("state = DoParse(state, results, \"");
			line.Append(Name);
			line.Append("\")");
		}
		else
		{
			string s = depth == 1 ? "s" : ("s" + depth);
			string r = depth == 1 ? "r" : ("r" + depth);
			string t = new string('\t', 2 + depth);
			line.AppendFormat("{0}{1}({2}, {3}) => DoParse({2}, {3}, \"", Environment.NewLine, t, s, r);
			line.Append(Name);
			line.Append("\")");
		}
	}
	
	public override IEnumerable<Expression> Select(Predicate<Expression> predicate)
	{
		if (predicate(this))
			yield return this;
	}
	
	public override string ToSubString()
	{
		return ToString();
	}
	
	public override string ToString()
	{
		return Name;
	}
}

internal sealed class SequenceExpression : Expression
{
	public SequenceExpression(Expression[] expressions)
	{
		Contract.Requires(expressions != null, "expressions is null");
		Contract.Requires(expressions.Length > 0, "expressions is empty");
		Contract.Requires(Contract.ForAll(expressions, e => e != null));
		
		Expressions = expressions;
	}
	
	public Expression[] Expressions {get; private set;}
	
	public override IEnumerable<Expression> Select(Predicate<Expression> predicate)
	{
		if (predicate(this))
			yield return this;
		
		foreach (Expression e in Expressions)
		{
			foreach (Expression f in e.Select(predicate))
			{
				yield return f;
			}
		}
	}
	
	public override Used FindUsed()
	{
		Used used = Used.Sequence;
		
		foreach (Expression e in Expressions)
		{
			used |= e.FindUsed();
		}
		
		return used;
	}
	
	public override string[] GetLeftRules()
	{
		return Expressions[0].GetLeftRules();
	}
	
	public override void Write(StringBuilder line, int depth)
	{
		string args = DoGetArgs(depth + 1);
		if (depth == 0)
		{
			line.AppendFormat("state = DoSequence(state, results{0})", args);
		}
		else
		{
			string s = depth == 1 ? "s" : ("s" + depth);
			string r = depth == 1 ? "r" : ("r" + depth);
			string t = new string('\t', 2 + depth);
			line.AppendFormat("{0}{1}({2}, {3}) => DoSequence({2}, {3}{4})", Environment.NewLine, t, s, r, args);
		}
	}
	
	public override string ToString()
	{
		var result = new System.Text.StringBuilder();
		
		for (int i = 0; i < Expressions.Length; ++i)
		{
			result.Append(Expressions[i].ToSubString());
			
			if (i + 1 < Expressions.Length)
				result.Append(' ');
		}
		
		return result.ToString();
	}
	
	private string DoGetArgs(int depth)
	{
		var line = new StringBuilder();
		
		foreach (Expression e in Expressions)
		{
			line.Append(",");
			e.Write(line, depth);
		}
		
		return line.ToString();
	}
}
