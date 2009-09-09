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
using System.Text;

[Flags]
[Serializable]
internal enum Used
{
	Literal = 0x0001,
	Sequence = 0x0002,
	Choice = 0x0004,
	Repetition = 0x0008,
	Range = 0x0010,
	Assert = 0x0020,
	NAssert = 0x0040,
}

// Base class for expressions which appear in rules.
internal abstract class Expression
{
	protected Expression()
	{
	}
	
	public abstract Used FindUsed();
	
	public abstract void Write(StringBuilder line, int depth);
	
	public abstract IEnumerable<Expression> Select(Predicate<Expression> predicate);
	
	public abstract string[] GetLeftRules();
	
	public virtual string ToSubString()
	{
		return string.Format("({0})", this);
	}
}
