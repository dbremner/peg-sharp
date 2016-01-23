// Copyright (C) 2010 Jesse Jones
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
using System.Diagnostics.Contracts;
//using System.Globalization;
using System.Linq;
//using System.Text;

// Concrete classes for predicate expressions within templates.
internal sealed class AndPredicate : Predicate
{
	public AndPredicate(IEnumerable<Predicate> terms)
	{
		Contract.Requires(terms != null, "terms is null");
		
		Terms = terms.ToArray();
	}
	
	public Predicate[] Terms {get; }
	
	public override string ToString()
	{
		var builder = new System.Text.StringBuilder();
		
		for (int i = 0; i < Terms.Length; ++i)
		{
			builder.Append(Terms[i]);
			
			if (i + 1 < Terms.Length)
				builder.Append(" and ");
		}
		
		return builder.ToString();
	}
	
	protected override object OnEvaluate(Context context)
	{
		bool result = true;
		
		for (int i = 0; i < Terms.Length && result; ++i)
		{
			result = Terms[i].EvaluateBool(context);
		}
		
		return result;
	}
}

internal sealed class BoolPredicate : Predicate
{
	public BoolPredicate(bool value)
	{
		Value = value;
	}
	
	public bool Value {get; }
	
	public override string ToString()
	{
		return Value ? "true" : "false";
	}
	
	protected override object OnEvaluate(Context context)
	{
		return Value;
	}
}

internal sealed class EqualsPredicate : Predicate
{
	public EqualsPredicate(Predicate lhs, Predicate rhs)
	{
		Contract.Requires(lhs != null, "lhs is null");
		Contract.Requires(rhs != null, "rhs is null");
		
		Lhs = lhs;
		Rhs = rhs;
	}

    [ContractInvariantMethod]
    private void ObjectInvariant()
    {
        Contract.Invariant(Lhs != null);
        Contract.Invariant(Rhs != null);
    }
	
	public Predicate Lhs {get; }
	public Predicate Rhs {get; }
	
	public override string ToString()
	{
		return string.Format("{0} == {1}", Lhs, Rhs);
	}
	
	protected override object OnEvaluate(Context context)
	{
		object lhs = Lhs.Evaluate(context);
		object rhs = Rhs.Evaluate(context);
		return lhs.Equals(rhs);
	}
}

internal sealed class ExcludedPredicate : Predicate
{
	public ExcludedPredicate(Predicate name)
	{
		Contract.Requires(name != null, "name is null");
		
		Name = name;
	}
	
	public Predicate Name {get; }
	
	public override string ToString()
	{
		return string.Format("excluded({0})", Name);
	}
	
	protected override object OnEvaluate(Context context)
	{
		string name = Name.EvaluateString(context);
		return context.IsExcluded(name);
	}
}

internal sealed class NotEqualsPredicate : Predicate
{
	public NotEqualsPredicate(Predicate lhs, Predicate rhs)
	{
		Contract.Requires(lhs != null, "lhs is null");
		Contract.Requires(rhs != null, "rhs is null");
		
		Lhs = lhs;
		Rhs = rhs;
	}
	
	public Predicate Lhs {get; }
	public Predicate Rhs {get; }
	
	public override string ToString()
	{
		return string.Format("{0} != {1}", Lhs, Rhs);
	}
	
	protected override object OnEvaluate(Context context)
	{
		object lhs = Lhs.Evaluate(context);
		object rhs = Rhs.Evaluate(context);
		return !lhs.Equals(rhs);
	}
}

internal sealed class NotPredicate : Predicate
{
	public NotPredicate(Predicate expression)
	{
		Contract.Requires(expression != null, "expression is null");
		
		Expression = expression;
	}
	
	public Predicate Expression {get; }
	
	public override string ToString()
	{
		return string.Format("not {0}", Expression);
	}
	
	protected override object OnEvaluate(Context context)
	{
		return !Expression.EvaluateBool(context);
	}
}

internal sealed class OrPredicate : Predicate
{
	public OrPredicate(IEnumerable<Predicate> terms)
	{
		Contract.Requires(terms != null, "terms is null");
		
		Terms = terms.ToArray();
	}
	
	public Predicate[] Terms {get; }
	
	public override string ToString()
	{
		var builder = new System.Text.StringBuilder();
		
		for (int i = 0; i < Terms.Length; ++i)
		{
			builder.Append(Terms[i]);
			
			if (i + 1 < Terms.Length)
				builder.Append(" or ");
		}
		
		return builder.ToString();
	}
	
	protected override object OnEvaluate(Context context)
	{
		bool result = false;
		
		for (int i = 0; i < Terms.Length && !result; ++i)
		{
			result = Terms[i].EvaluateBool(context);
		}
		
		return result;
	}
}

internal sealed class StringPredicate : Predicate
{
	public StringPredicate(string text)
	{
		Contract.Requires(text != null, "text is null");
		
		Text = text.Substring(1, text.Length - 2);	// lose the quotes
	}
	
	public string Text {get; }
	
	public override string ToString()
	{
		return string.Format("'{0}'", Text);
	}
	
	protected override object OnEvaluate(Context context)
	{
		return Text;
	}
}

internal sealed class VariablePredicate : Predicate
{
	public VariablePredicate(string name)
	{
		Contract.Requires(name != null, "name is null");
		
		Name = name;
	}
	
	public string Name {get; }
	
	public override string ToString()
	{
		return Name;
	}
	
	protected override object OnEvaluate(Context context)
	{
		return context.Dereference(Name);
	}
}
