using System;
using System.Collections.Generic;

// For better separation of concerns Expression objects hold only
// the data associated with the expression: all operations are
// done with multimethods. 
internal abstract class Expression
{
	public abstract Expression Evaluate(Dictionary<string, Expression> context);
}
