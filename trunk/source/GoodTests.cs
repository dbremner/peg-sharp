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

#if TEST
using NUnit.Framework;
using System;

[TestFixture]
public sealed class GoodTests
{
	[Test]
	public void Basic()
	{
		string input = @"
start = Expr
value = double

Expr := 'x';";
		
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		
		Assert.NotNull(grammar);
		grammar.Validate();
		
		Assert.AreEqual("Expr", grammar.Settings["start"]);
		Assert.AreEqual("double", grammar.Settings["value"]);
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		Assert.AreEqual(typeof(LiteralExpression), grammar.Rules[0].Expression.GetType());
		
		var le = (LiteralExpression) grammar.Rules[0].Expression;
		Assert.AreEqual("x", le.Literal);
	}
	
	[Test]
	public void Comments()
	{
		string input = @"
# your comment here
start = Expr		# another comment
value = double

Expr := 'x';
";
		
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual("Expr", grammar.Settings["start"]);
		Assert.AreEqual("double", grammar.Settings["value"]);
	}
	
	[Test]
	public void Sequence()
	{
		string input = @"
start = Expr
value = double

Expr := 'x'    'y';
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		Assert.AreEqual(typeof(SequenceExpression), grammar.Rules[0].Expression.GetType());
		
		var e = (SequenceExpression) grammar.Rules[0].Expression;
		Assert.AreEqual(2, e.Expressions.Length);
		Assert.AreEqual("'x' 'y'", e.ToString());
	}
	
	[Test]
	public void Choice()
	{
		string input = @"
start = Expr
value = double

Expr := 'x' / 'y';
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		Assert.AreEqual(typeof(ChoiceExpression), grammar.Rules[0].Expression.GetType());
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("'x' / 'y'", e.ToString());
	}
	
	[Test]
	public void Repetition()
	{
		string input = @"
start = Expr
value = double

Expr := 'x'*;
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		Assert.AreEqual(typeof(RepetitionExpression), grammar.Rules[0].Expression.GetType());
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("'x'*", e.ToString());
	}
	
	[Test]
	public void SubExpression()
	{
		string input = @"
start = Expr
value = double

Expr := ('x'    'y')*;
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		Assert.AreEqual(typeof(RepetitionExpression), grammar.Rules[0].Expression.GetType());
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("('x' 'y')*", e.ToString());
	}
	
	[Test]
	public void MultipleRules()
	{
		string input = @"
start = Expr
value = double

Expr := Sum;
Sum := Product (('+' / '-') Product)*;
Product := Value (('*' / '/') Value)*;
Value := 'x' / 'y' / 'z';
Value := '(' Expr ')';
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(5, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		Assert.AreEqual("Sum", grammar.Rules[1].Name);
		Assert.AreEqual("Product", grammar.Rules[2].Name);
		Assert.AreEqual("Value", grammar.Rules[3].Name);
		Assert.AreEqual("Value", grammar.Rules[4].Name);
		Assert.AreEqual(typeof(RuleExpression), grammar.Rules[0].Expression.GetType());
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("Sum", e.ToString());
	}
	
	[Test]
	public void Range1()
	{
		string input = @"
start = Expr
value = double

Expr := [abc-];";
		
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual(typeof(RangeExpression), grammar.Rules[0].Expression.GetType());
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("[abc-]", e.ToString());
	}
	
	[Test]
	public void Range2()
	{
		string input = @"
start = Expr
value = double

Expr := [-a-x];";
		
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual(typeof(RangeExpression), grammar.Rules[0].Expression.GetType());
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("[-a-x]", e.ToString());
	}
	
	[Test]
	public void Actions1()
	{
		string input = @"
start = Expr
value = double

Expr := [a-x] `foo bar`";
		
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual(typeof(RangeExpression), grammar.Rules[0].Expression.GetType());
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("[a-x]", e.ToString());
		Assert.AreEqual("foo bar", grammar.Rules[0].PassAction);
	}
	
	[Test]
	public void Actions2()
	{
		string input = @"
start = Expr
value = double

Expr := [a-x] `foo bar` `fie fum`";
		
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual(typeof(RangeExpression), grammar.Rules[0].Expression.GetType());
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("[a-x]", e.ToString());
		Assert.AreEqual("foo bar", grammar.Rules[0].PassAction);
		Assert.AreEqual("fie fum", grammar.Rules[0].FailAction);
	}
	
	[Test]
	public void PositiveRepetition()
	{
		string input = @"
start = Expr
value = double

Expr := 'x'+;
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("'x'+", e.ToString());
	}
	
	[Test]
	public void Asserts()
	{
		string input = @"
start = Expr
value = double

Expr := !'foo' 'x'+ &'bar';
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("!'foo' 'x'+ &'bar'", e.ToString());
	}
	
	[Test]
	public void Optional()
	{
		string input = @"
start = Expr
value = double

Expr := 'x'? 'y';
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("'x'? 'y'", e.ToString());
	}
	
	[Test]
	public void Unconsumed()
	{
		string input = @"
start = Expr
value = double
unconsumed = expose

Expr := 'x'? 'y';
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("'x'? 'y'", e.ToString());
	}
	
	[Test]
	public void GeneralRepeat1()
	{
		string input = @"
start = Expr
value = double
unconsumed = expose

Expr := 'x'{0, 100};
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("'x'{0, 100}", e.ToString());
	}
	
	[Test]
	public void GeneralRepeat2()
	{
		string input = @"
start = Expr
value = double
unconsumed = expose

Expr := 'x'{32,};
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual("'x'{32,}", e.ToString());
	}
	
	[Test]
	public void Any()
	{
		string input = @"
start = Expr
value = double
unconsumed = expose

Expr := ..;
";
		var parser = new Parser();
		Grammar grammar = parser.Parse(input);
		grammar.Validate();
		
		Assert.AreEqual(1, grammar.Rules.Count);
		Assert.AreEqual("Expr", grammar.Rules[0].Name);
		
		var e = grammar.Rules[0].Expression;
		Assert.AreEqual(". .", e.ToString());
	}
}
#endif	// TEST