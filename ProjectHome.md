This library generates a C# parser for a language encoded into a Parsing Expression Grammar (PEG) along with optional semantic actions. The parser is implemented as a back-tracking  recursive descent parser with memoization (i.e. a packrat parser) so it runs in linear time. The library has the follow advantages and disadvantages:

  * +The language to be parsed is written using a PEG which provides an especially nice syntax for encoding languages (see the example below).
  * +Parses are unambiguous: if a string parses then it has exactly one parse tree.
  * +There is no need for a separate tokenization step.
  * +The library makes it very easy to add semantic actions.
  * +The library has good support for customizing error messages.
  * +The library offers a lot of support for customization.
  * -The library does not support left recursive rules.
  * -The amount of memory used is proportional to the input size.
  * -Not a lot of time has been spent optimizing the parser.

The generator requires .NET 3.0, but the generated code can run in .NET 2.0 (if you use the exclude-methods setting to disable the partial methods).

Here's an example peg file:

```
# Grammar for arithmetic expressions which may contain variables.
# The Do methods are provided by the user via partial classes.
# The Expression types are also provided by the user.
start = Start       # the rule which is used to begin parsing
value = Expression  # the type returned by the semantic actions

# Start
Start := S (Assignment / Expression);
Assignment := Identifier '=' S Expression `value = new AssignmentExpression(results[0].Text.Trim(), results[2].Value)`

# Expressions  
Expression := Sum;
Identifier := [a-zA-Z$] [a-zA-Z0-9]* S `value = new VariableExpression(text.Trim())` `expected = "variable"`
Sum := Product (('+' / '-') S Product)*  `value = DoCreateBinary(results)`
Product := Value (('*' / '/') S Value)*  `value = DoCreateBinary(results)`
Value := [0-9]+ '.' [0-9]+ (('e' / 'E') [0-9]+)? S `value = new FloatExpression(text.Trim())` `expected = "number"`
Value := [0-9]+ ('e' / 'E') [0-9]+ S `value = new FloatExpression(text.Trim())` `expected = "number"`
Value := [0-9]+ S `value = new IntegerExpression(text.Trim())` `expected = "number"`
Value := Identifier;
Value := '(' Expression ')' S `value = results[1].Value` `expected = "parenthesized expression"`

# Scaffolding
S := Space* `text = null`  # We use a separate space rule because x* always succeeds.
Space := [ \t\r\n] `;` `expected = "whitespace"`
```