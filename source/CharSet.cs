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
using System.Diagnostics.Contracts;
using System.Globalization;

internal sealed class CharSet
{
	public CharSet(RangeExpression range)
	{
		var builder = new System.Text.StringBuilder();
	    Contract.Requires(range != null);
        Contract.Requires(range.Chars != null);
		
		char ch = char.MinValue;
		while (true)							// note that we can't use a for loop or we'll get an overflow
		{
			if (range.Chars.IndexOf(ch) >= 0)
				builder.Append(ch);
				
			else if (DoRangesInclude(range.Ranges, ch))
				builder.Append(ch);
				
			else if (DoCategoriesInclude(range.CategoryLabel, ch))
				builder.Append(ch);
			
			if (ch < char.MaxValue)
				++ch;
			else
				break;
		}
		
		m_chars = builder.ToString();
	}
	
	public string Chars {get; private set;}
	
	public bool IsSuperSetOf(CharSet rhs)
	{
	    Contract.Requires(rhs != null);
	    foreach (char ch in rhs.m_chars)
		{
			if (m_chars.IndexOf(ch) < 0)
				return false;
		}
		
		return true;
	}
	
	#region Private Methods
	private bool DoRangesInclude(string ranges, char ch)
	{
	    Contract.Requires(ranges != null);
	    for (int i = 0; i < ranges.Length; i += 2)
		{
			if (ranges[i] <= ch && ch <= ranges[i + 1])
				return true;
		}
		
		return false;
	}
	
	private bool DoCategoriesInclude(string categories, char ch)
	{
	    Contract.Requires(categories != null);
	    UnicodeCategory cat = char.GetUnicodeCategory(ch);
		
		for (int i = 0; i < categories.Length; i += 5)
		{
			UnicodeCategory candidate = DoGetCategory(categories.Substring(i + 3, 2));
			if (candidate == cat)
				return true;
		}
		
		return false;
	}
	
	private UnicodeCategory DoGetCategory(string name)
	{
		UnicodeCategory result;
		
		switch (name)
		{
			case "Lu":
				result = UnicodeCategory.UppercaseLetter;
				break;
				
			case "Ll":
				result = UnicodeCategory.LowercaseLetter;
				break;
				
			case "Lt":
				result = UnicodeCategory.TitlecaseLetter;
				break;
				
			case "Lm":
				result = UnicodeCategory.ModifierLetter;
				break;
				
			case "Lo":
				result = UnicodeCategory.OtherLetter;
				break;
				
			case "Mn":
				result = UnicodeCategory.NonSpacingMark;
				break;
				
			case "Mc":
				result = UnicodeCategory.SpacingCombiningMark;
				break;
				
			case "Me":
				result = UnicodeCategory.EnclosingMark;
				break;
				
			case "Nd":
				result = UnicodeCategory.DecimalDigitNumber;
				break;
				
			case "Nl":
				result = UnicodeCategory.LetterNumber;
				break;
				
			case "No":
				result = UnicodeCategory.OtherNumber;
				break;
				
			case "Zs":
				result = UnicodeCategory.SpaceSeparator;
				break;
				
			case "Zl":
				result = UnicodeCategory.LineSeparator;
				break;
				
			case "Zp":
				result = UnicodeCategory.ParagraphSeparator;
				break;
				
			case "Cc":
				result = UnicodeCategory.Control;
				break;
				
			case "Cf":
				result = UnicodeCategory.Format;
				break;
				
			case "Cs":
				result = UnicodeCategory.Surrogate;
				break;
				
			case "Co":
				result = UnicodeCategory.PrivateUse;
				break;
				
			case "Pc":
				result = UnicodeCategory.ConnectorPunctuation;
				break;
				
			case "Pd":
				result = UnicodeCategory.DashPunctuation;
				break;
				
			case "Ps":
				result = UnicodeCategory.OpenPunctuation;
				break;
				
			case "Pe":
				result = UnicodeCategory.ClosePunctuation;
				break;
				
			case "Pi":
				result = UnicodeCategory.InitialQuotePunctuation;
				break;
				
			case "Pf":
				result = UnicodeCategory.FinalQuotePunctuation;
				break;
				
			case "Po":
				result = UnicodeCategory.OtherPunctuation;
				break;
				
			case "Sm":
				result = UnicodeCategory.MathSymbol;
				break;
				
			case "Sc":
				result = UnicodeCategory.CurrencySymbol;
				break;
				
			case "Sk":
				result = UnicodeCategory.ModifierSymbol;
				break;
				
			case "So":
				result = UnicodeCategory.OtherSymbol;
				break;
				
			case "Cn":
				result = UnicodeCategory.OtherNotAssigned;
				break;
				
			default:
				throw new ArgumentException(name + " is not a valid Unicode character class.");
		}
		
		return result;
	}
	#endregion
	
	#region Fields
	private readonly string m_chars;
	#endregion
}
