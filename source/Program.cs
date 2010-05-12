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

using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: CLSCompliant (true)]
[assembly: ComVisible (false)]

internal static class Program
{
	public static int Main(string[] args)
	{
		int result = 0;
		
		try
		{
			Stopwatch timer = Stopwatch.StartNew();
			
			string pegFile = DoProcessCommandLine(args);
			DoGenerate(pegFile);
			
			if (Program.Verbosity > 0)
				if (timer.ElapsedMilliseconds > 60*1000)
					Console.WriteLine("finished in {0:0.000} mins", timer.ElapsedMilliseconds/(60*1000.0));
				else
					Console.WriteLine("finished in {0:0.000} secs", timer.ElapsedMilliseconds/1000.0);
		}
		catch (ParserException e)
		{
			if (ms_verbosity == 0)
				Console.Error.WriteLine(e.Message);
			else
				throw;
			result = 2;
		}
		
		return result;
	}
	
	public static int Verbosity
	{
		get {return ms_verbosity;}
	}
	
	#region Private Methods
	private static void DoGenerate(string pegFile)
	{
		// Parse the file.
		if (Program.Verbosity > 0)
			Console.WriteLine("parsing '{0}'", pegFile);
		
		var parser = new Parser();
		string input = System.IO.File.ReadAllText(pegFile);
		parser.Parse(input);
		
		// Check for errors.
		parser.Grammar.Validate();
		
		// Delete the old parser.
		if (File.Exists(ms_outFile))
			File.Delete(ms_outFile);
		
		// Write the new parser.
		if (Program.Verbosity > 0)
			Console.WriteLine("writing '{0}'", ms_outFile);
		
		using (var stream = new StreamWriter(ms_outFile))
		{
			using (var writer = new Writer(stream, parser.Grammar))
			{
				writer.Write(pegFile);
				stream.Flush();
			}
		}
	}
	
	private static string DoProcessCommandLine(string[] args)
	{
		var fixedArgs = ms_options.Parse(args);
		
		var bad = from f in fixedArgs where f.StartsWith("-") select f;
		if (bad.Any())
		{
			Console.Error.WriteLine("Unknown option: {0}", string.Join(" ", bad.ToArray()));
			Environment.Exit(1);
		}
		
		if (fixedArgs.Count != 1)
		{
			Console.Error.WriteLine("One peg file must be specified.");
			Environment.Exit(1);
		}
		
		if (!File.Exists(fixedArgs[0]))
		{
			Console.Error.WriteLine("Can't find '{0}'.", fixedArgs[0]);
			Environment.Exit(1);
		}
		
		return fixedArgs[0];
	}
	
	private static void DoShowHelp(string value)
	{
		Console.WriteLine("mono peg-sharp.exe [options] file.peg");
		ms_options.WriteOptionDescriptions(Console.Out);
		Console.WriteLine("If --out is not specified a Parser.cs file will be generated in the working directory.");
		
		Environment.Exit(0);
	}
	
	private static void DoShowVersion(string value)
	{
		Version version = Assembly.GetExecutingAssembly().GetName().Version;
		Console.WriteLine("peg-sharp {0}", version);
		
		Environment.Exit(0);
	}
	#endregion
	
	#region Fields
	private static string ms_outFile = "Parser.cs";
	private static int ms_verbosity;
	
	private static OptionSet ms_options = new OptionSet()
	{
		{"h|?|help", "prints this message and exits", Program.DoShowHelp},
		{"o=|out=", "path to the parser file to be generated", v => {ms_outFile = v;}},
		{"verbose", "enables extra output, may be used more than once", v => ++ms_verbosity},
		{"version", "prints the version number and exits", Program.DoShowVersion},
	};
	#endregion
}
