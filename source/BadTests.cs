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
public sealed class BadTests
{
	// None of the standard nunit mehods allow us to easily check the exception message.
	private void AssertThrows<E>(Action callback, string message) where E : Exception
	{
		try
		{
			callback();
			Assert.Fail("Expected a " + typeof(E).Name + " exception.");
		}
		catch (E e)
		{
			Assert.AreEqual(message, e.Message);
		}
	}
	
	[Test]
	public void Empty()
	{
		AssertThrows<ParserException>(() => new Parser().Parse(""), "Expected whitespace or setting or comment at line 1 col 1.");
	}
	
	[Test]
	public void MissingRequiredSettings()
	{
		AssertThrows<ParserException>(() => new Parser().Parse("debug = Foo\nFoo := 'z';"), "Missing required setting 'start'.");
		AssertThrows<ParserException>(() => new Parser().Parse("start = foo\nFoo := 'z';"), "Missing required setting 'value'.");
	}
	
	[Test]
	public void DuplicateSetting()
	{
		string input = @"
start = Expr
value = double
start = Expr
";
		
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Setting 'start' is already defined at line 4 col 1.");
	}
	
	[Test]
	public void UnknownSetting()
	{
		string input = @"
start = Expr
value = double
foo = Expr
";
		
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Setting 'foo' is not a valid name at line 4 col 1.");
	}
	
	[Test]
	public void BadTrailer()
	{
		string input = @"
start = Expr
value = double
some bogus text
";
		
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Expected setting or comment or include or rule at line 4 col 6.");
	}
	
	[Test]
	public void NoExpr()
	{
		string input = @"
start = Expr
value = double

Expr := 
";
		
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Expected rule at line 6 col 1.");
	}
	
	[Test]
	public void NoStartRule()
	{
		string input = @"
start = Expr
value = double

Expr2 := 'x';
";
		
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Missing the start rule 'Expr'.");
	}
	
	[Test]
	public void MissingNonterminal()
	{
		string input = @"
start = Expr
value = double

Expr := Sum;
Sum := Product (('+' / '-') Product)*;
Product := ValueX (('*' / '/') Value)*;
Value := 'x' / 'y' / 'z';
Value := '(' ExprZ ')';
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Undefined nonterminals: ValueX ExprZ");
	}
	
	[Test]
	public void UnusedNonterminal()
	{
		string input = @"
start = Expr
value = double

Expr := Sum;
Sum := Product (('+' / '-') Product)*;
Product := Value (('*' / '/') Value)*;
Value := 'x' / 'y' / 'z';
Value := '(' Expr ')';
ValueX := 'x';
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unused nonterminals: ValueX");
	}
	
	[Test]
	public void Unconsumed1()
	{
		string input = @"
start = Expr
value = double
unconsumed = foo

Expr := 'x'? 'y';
";
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Unconsumed value must be 'error', 'expose', or 'ignore' at line 4 col 1.");
	}
	
	[Test]
	public void EmptyRepetition()
	{
		string input = @"
start = Expr
value = double

Expr := 'x'{0, 0};
Foo := 'x';
";
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Max is not positive at line 5 col 9.");
	}
	
	[Test]
	public void LeftRecursion()
	{
		string input = @"
start = Expr
value = double

Expr := Sum;
Sum := Sum '+' Product;	# left recursive
Sum := Sum '-' Product;	# left recursive
Sum := Product;
Product := Unary (('*' / '/') Unary)*;
Unary := '+' Unary;	# right recursive (which is OK)
Unary := '-' Unary; 
Unary := Value; 
Value := 'x' / 'y' / 'z' / Value 'a';	# left recursive
Value := '(' Expr ')';
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Left recursive rules: Sum Value");
	}
	
	[Test]
	public void IndirectLeftRecursion()
	{
		string input = @"
start = Expr
value = double

Expr := Sum;
Sum := Product (('+' / '-') Product)*;
Product := Value (('*' / '/') Value)*;
Value := 'x' / 'y' / 'z';
Value := '(' Expr ')';
Value := Sum 'k';		# indirect left recursion
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Left recursive rules: Sum Product Value");
	}
	
	[Test]
	public void UnreachableFailAction1()
	{
		string input = @"
start = Expr
value = double

Expr := S Sum;
Sum := Product (('+' / '-') S Product)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Product := Value (('*' / '/') S Value)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Value := [0-9]+ S `value = Double.Parse(text.Trim())` `expected = ""number""`
Value := '(' Expr ')' S `value = results[1].Value` `expected = ""parenthesized expression""`
S := Space* `text = null` `expected = ""unreachable""`
Space := [ \t\r\n] `;` `expected = ""whitespace""`
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unreachable fail actions: S");
	}
	
	[Test]
	public void UnreachableFailAction2()
	{
		string input = @"
start = Expr
value = double

Expr := S Sum;
Sum := Product (('+' / '-') S Product)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Product := Value (('*' / '/') S Value)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Value := [0-9]+ S `value = Double.Parse(text.Trim())` `expected = ""number""`
Value := '(' Expr ')' S `value = results[1].Value` `expected = ""parenthesized expression""`
S := Spacer `text = null` `expected = ""unreachable""`
Spacer := Space*;
Space := [ \t\r\n] `;` `expected = ""whitespace""`
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unreachable fail actions: S");
	}
	
	[Test]
	public void UnreachableFailAction3()
	{
		string input = @"
start = Expr
value = double

Expr := S Sum;
Sum := Product (('+' / '-') S Product)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Product := Value (('*' / '/') S Value)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Value := [0-9]+ S `value = Double.Parse(text.Trim())` `expected = ""number""`
Value := '(' Expr ')' S `value = results[1].Value` `expected = ""parenthesized expression""`
S := . `text = null` `expected = ""unreachable""`
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unreachable fail actions: S");
	}
	
	[Test]
	public void UnreachableFailAction4()
	{
		string input = @"
start = Expr
value = double

Expr := S Sum;
Sum := Product (('+' / '-') S Product)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Product := Value (('*' / '/') S Value)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Value := [0-9]+ S `value = Double.Parse(text.Trim())` `expected = ""number""`
Value := '(' Expr ')' S `value = results[1].Value` `expected = ""parenthesized expression""`
S := 'y' / 'z' / . `text = null` `expected = ""unreachable""`
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unreachable fail actions: S");
	}
	
	[Test]
	public void UnreachableAlternative1()
	{
		string input = @"
start = Expr
value = double

Expr := S Sum;
Sum := Product (('+' / '-') S Product)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Product := Value (('*' / '/') S Value)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Value := [0-9]+ S `value = Double.Parse(text.Trim())` `expected = ""number""`
Value := '(' Expr ')' S `value = results[1].Value` `expected = ""parenthesized expression""`
S := 'y' / . / 'z' `text = null` `expected = ""unreachable""`
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unreachable alternative in: S");
	}
	
	[Test]
	public void UnreachableAlternative2()
	{
		string input = @"
start = Expr
value = double

Expr := S Sum;
Sum := Product (('+' / '-') S Product)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Product := Value (('*' / '/') S Value)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Value := [0-9]+ S `value = Double.Parse(text.Trim())` `expected = ""number""`
Value := '(' Expr ')' S `value = results[1].Value` `expected = ""parenthesized expression""`
S := 'xx' / 'xxY' `text = null` `expected = ""unreachable""`
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unreachable alternative in: S");
	}
	
	[Test]
	public void UnreachableAlternative3()
	{
		string input = @"
start = Expr
value = double

Expr := S Sum;
Sum := Product (('+' / '-') S Product)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Product := Value (('*' / '/') S Value)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Value := [0-9]+ S `value = Double.Parse(text.Trim())` `expected = ""number""`
Value := '(' Expr ')' S `value = results[1].Value` `expected = ""parenthesized expression""`
S := [abcdXYZ] / [acd] `text = null` `expected = ""unreachable""`
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unreachable alternative in: S");
	}
	
	[Test]
	public void UnreachableAlternative4()
	{
		string input = @"
start = Expr
value = double

Expr := S Sum;
Sum := Product (('+' / '-') S Product)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Product := Value (('*' / '/') S Value)*  `if (results.Count > 0) value = EvaluateBinary(results)`
Value := [0-9]+ S `value = Double.Parse(text.Trim())` `expected = ""number""`
Value := '(' Expr ')' S `value = results[1].Value` `expected = ""parenthesized expression""`
S := 'a' ([X\cLl] / [a-d]) `text = null` `expected = ""unreachable""`
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unreachable alternative in: S");
	}
	
	[Test]
	public void UnreachableAlternative5()
	{
		string input = @"
start = Top
value = void

Top := Bad / 'x';

Bad := Alpha* Beta;

Alpha := 'a';

Beta := 'b'*;
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Unreachable alternative in: Top");
	}
	
	[Test]
	public void BackwardsRange()
	{
		string input = @"
start = Expr
value = double

Expr := Sum;
Sum := Product (('+' / '-') Product)*;
Product := Value (('*' / '/') Value)*;
Value := 'x' / 'y' / 'z' / [abZ-Cd];
Value := '(' Expr ')';
";
		var parser = new Parser();
		parser.Parse(input);
		AssertThrows<ParserException>(() => parser.Grammar.Validate(), "Backwards range in: Value");
	}
	
	[Test]
	public void BadCategory()
	{
		string input = @"
start = Expr
value = double

Expr := Sum;
Sum := Product (('+' / '-') Product)*;
Product := Value (('*' / '/') Value)*;
Value := 'x' / 'y' / 'z' / [ab\cXxd];
Value := '(' Expr ')';
";
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Xx is not a valid Unicode character category at line 8 col 28.");
	}
	
	[Test]
	public void EmptyRange()
	{
		string input = @"
start = Expr
value = double

Expr := Sum;
Sum := Product (('+' / '-') Product)*;
Product := Value (('*' / '/') Value)*;
Value := 'x' / 'y' / 'z' / [];
Value := '(' Expr ')';
";
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Expected rule or comment at line 8 col 29.");
	}
	
	[Test]
	public void EmptyInvertedRange()
	{
		string input = @"
start = Expr
value = double

Expr := Sum;
Sum := Product (('+' / '-') Product)*;
Product := Value (('*' / '/') Value)*;
Value := 'x' / 'y' / 'z' / [^];
Value := '(' Expr ')';
";
		var parser = new Parser();
		AssertThrows<ParserException>(() => parser.Parse(input), "Inverted range cannot be empty at line 8 col 28.");
	}
}
#endif	// TEST
