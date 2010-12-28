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

using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;

public sealed class SystemException : Exception
{
	public SystemException(string message) : base(message)
	{
	}
}

internal static class Program
{
	public static int Main(string[] args)
	{
		int result = 0;
		
		try
		{
			Stopwatch timer = Stopwatch.StartNew();
			
			string dir = DoProcessCommandLine(args);
			DoProcess(dir);
			
			if (Program.Verbosity > 0)
			{
				if (ms_numFailed > 0)
					Console.WriteLine("Ran {0} tests with {1} failures", ms_numTests, ms_numFailed);
				else
					Console.WriteLine("Ran {0} tests with no failures", ms_numTests);
				
				if (timer.ElapsedMilliseconds > 60*1000)
					Console.WriteLine("finished in {0:0.000} mins", timer.ElapsedMilliseconds/(60*1000.0));
				else
					Console.WriteLine("finished in {0:0.000} secs", timer.ElapsedMilliseconds/1000.0);
			}
		}
		catch (SystemException e)
		{
			if (ms_verbosity == 0)
				Console.Error.WriteLine(e.Message);
			else
				throw;
			result = 2;
		}
		catch (FormatException e)
		{
			if (ms_verbosity == 0)
				Console.Error.WriteLine(e.Message);
			else
				throw;
			result = 3;
		}
		
		return result;
	}
	
	public static int Verbosity
	{
		get {return ms_verbosity;}
	}
	
	#region Private Methods
	private static void DoProcess(string dir)
	{
		string[] pegs = Directory.GetFiles(dir, "Test*.peg", SearchOption.TopDirectoryOnly);
		if (pegs.Length > 0)
		{
			DoProcessDir(dir);
		}
		else
		{
			foreach (string srcDir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
			{
				DoProcessDir(srcDir);
			}
		}
	}
	
	private static void DoProcessDir(string srcDir)
	{
		string[] pegs = Directory.GetFiles(srcDir, "Test*.peg", SearchOption.TopDirectoryOnly);
		string[] data = Directory.GetFiles(srcDir, "*.field", SearchOption.TopDirectoryOnly);
		if (pegs.Length == 1 && data.Length == 1)
		{
			if (Verbosity >= 1)
				Console.WriteLine("processing {0}", srcDir);
			
			string exe = DoBuild(srcDir, Path.GetFullPath(pegs[0]), data[0]);
			DoRun(Path.GetFileName(srcDir), exe);
		}
		else
		{
			if (pegs.Length > 1)
				Console.Error.WriteLine("Found multiple peg files in {0}", srcDir);
			if (data.Length > 1)
				Console.Error.WriteLine("Found multiple field files in {0}", srcDir);
		}
	}
		
	// We build the test assemblies ourself instead of using make so that
	// we'll work better on systems without make (i.e. windows).
	private static string DoBuild(string srcDir, string pegFile, string dataFile)
	{
		// create bin directories as needed
		string myExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
		string myDir = Path.GetDirectoryName(myExe);
		
		string genDir = Path.Combine(myDir, "ftest");
		if (!Directory.Exists(genDir))
			Directory.CreateDirectory(genDir);
		
		string genTestDir = Path.Combine(genDir, Path.GetFileName(srcDir));
		if (!Directory.Exists(genTestDir))
			Directory.CreateDirectory(genTestDir);
			
		// see if we really need to build
		string pegSharp = Path.Combine(myDir, "peg-sharp.exe");
		
		var srcFiles = new List<string>();
		srcFiles.AddRange(from f in Directory.GetFiles(srcDir, "*.cs") let n = Path.GetFileName(f) where n != "Program.cs" && n != "Parser.cs" && !n.StartsWith("Test") select f);	// TODO: once the obsolete files are removed we can drop the filter
		
		var prerequisites = new List<string>();
		prerequisites.Add(pegSharp);
		prerequisites.Add(myExe);
		prerequisites.Add(pegFile);
		prerequisites.Add(dataFile);
		prerequisites.AddRange(srcFiles);
		
		string parser = Path.Combine(genTestDir, "Parser.cs");
		string exe = Path.Combine(genTestDir, "test.exe");
		if (DoHasNewPrerequisite(parser, prerequisites))
		{
			// build the parser
			DoRunTool(pegSharp, "--out={0} {1}", parser, pegFile);
			
			// generate a Program.cs or Program.fs file
			string program = Path.Combine(genTestDir, "Program.cs");
			string parserName = Path.GetFileNameWithoutExtension(pegFile);
			DoGenerateCsProgram(program, parserName, dataFile);
			
			// build the assembly
			srcFiles.Add(program);
			srcFiles.Add(parser);
			DoRunTool("gmcs", "-out:{0} -checked+ -debug+ -warn:4 -warnaserror+ -d:DEBUG -d:TRACE -d:CONTRACTS_FULL -target:exe {1}", exe, string.Join(" ", srcFiles.ToArray()));
		}
		
		return exe;
	}
	
	private static bool DoHasNewPrerequisite(string target, IEnumerable<string> prerequisites)
	{
		if (!File.Exists(target))
			return true;
			
		DateTime targetTime = File.GetLastWriteTimeUtc(target);
		foreach (string pre in prerequisites)
		{
			DateTime preTime = File.GetLastWriteTimeUtc(pre);
			if (preTime > targetTime)
				return true;
		}
		
		return false;
	}
	
	private static void DoGenerateCsProgram(string program, string parserName, string dataFile)
	{
		var good = new Dictionary<string, string>();
		var bad = new Dictionary<string, string>();
		DoGetTests(dataFile, good, bad);
		
		string contents = @"using System;
using System.Collections.Generic;

internal static class Program
{
	public static int Main(string[] args)
	{
		int numFailures = 0;
		
		try
		{
			foreach (var good in new Dictionary<string, string>{GOOD})
			{
				if (!DoCheckGood(good.Key, good.Value))
					++numFailures;
			}
			foreach (var bad in new Dictionary<string, string>{BAD})
			{
				if (!DoCheckBad(bad.Key, bad.Value))
					++numFailures;
			}
		}
		catch (Exception e)
		{
			Console.Error.WriteLine(e.Message);
		}
		
		return numFailures;
	}
	
	private static bool DoCheckGood(string input, string expected)
	{
		var result = ms_parser.Parse(input);
		string actual = result.ToString().Trim();
		if (actual != expected)
			Console.Error.WriteLine(""input: {0}, expected: {1}, actual: {2}"", input, expected, actual);
		
		return actual == expected;
	}
	
	private static bool DoCheckBad(string input, string expected)
	{
		bool passed = false;
		
		try
		{
			var result = ms_parser.Parse(input);
			string actual = result.ToString().Trim();
			Console.Error.WriteLine(""input: {0}, expected: {1} error, actual: {2}"", input, expected, actual);
		}
		catch (Exception e)
		{
			if (e.Message.Contains(expected))
				passed = true;
			else
				Console.Error.WriteLine(""input: {0}, expected: {1}, actual: {2}"", input, expected, e.Message);
		}
		
		return passed;
	}
	
	private static PARSER ms_parser = new PARSER();
}";
		contents = contents.Replace("PARSER", parserName);
		contents = contents.Replace("GOOD", DoDictToStr(good));
		contents = contents.Replace("BAD", DoDictToStr(bad));
		
		using (var stream = new StreamWriter(program, false, System.Text.Encoding.UTF8))
		{
			stream.Write(contents);
		}
	}
	
	private static string DoDictToStr(Dictionary<string, string> dict)
	{
		var entries = new List<string>();
		
		foreach (var entry in dict)
		{
			string text = "{\"";
			text += entry.Key;
			text += "\", \"";
			text += entry.Value;
			text += "\"}";
			entries.Add(text);
		}
		
		return string.Join(", ", entries.ToArray());
	}
	
	private static void DoGetTests(string dataFile, Dictionary<string, string> good, Dictionary<string, string> bad)
	{
		const string sep = "#------------------------------------------------------------------------------";
		
		string contents = File.ReadAllText(dataFile);
		string[] tests = contents.Split(new string[]{sep}, StringSplitOptions.RemoveEmptyEntries);
		foreach (string test in tests)
		{
			Field[] fields = FieldParser.Parse(test);
			
			if (fields[1].Name == "stdout")
			{
				good.Add(DoSanitize(fields[0].Value), DoSanitize(fields[1].Value));
			}
			else if (fields[1].Name == "stderr")
			{
				bad.Add(DoSanitize(fields[0].Value), DoSanitize(fields[1].Value));
			}
			else
				System.Diagnostics.Debug.Assert(false);
		}
	}
	
	// We need something like this for test 18 and, I guess, it's helpful elsewhere.
	private static string DoSanitize(string text)
	{
		string[] lines = text.Trim().Split('\n');
		
		for (int i = 0; i < lines.Length; ++i)
		{
			if (lines[i].Length > 0 && lines[i][0] == '\t')
				lines[i] = lines[i].Substring(1);
				
			lines[i] = lines[i].Replace("\\", "\\\\");
			lines[i] = lines[i].Replace("\"", "\\\"");
			lines[i] = lines[i].Replace("\t", "\\t");		// makes it a bit easier to see whats going on
		}
		
		return string.Join("\\n", lines);
	}
	
	private static void DoRun(string testName, string exe)
	{
		string stdout, stderr;
		int result = DoRunParser(exe, out stdout, out stderr);
		
		var failures = new List<string>();
		if (result != 0)
		{
			failures.Add(string.Format("{0} test cases failed", result));
		}
		
		if (stderr.Length > 0)
			failures.Add(string.Format("got stderr: {0}.", stderr));
		
		if (failures.Count > 0)
		{
			Console.Error.WriteLine("{0} failed", testName);
			foreach (string f in failures)
			{
				Console.Error.WriteLine("   {0}", f);
			}
			++ms_numFailed;
		}
		++ms_numTests;
	}
	
	private static int DoRunParser(string exe, out string stdout, out string stderr)
	{
		if (Verbosity >= 2)
			Console.WriteLine(exe);
		
		int result = -1;
		using (Process process = new Process())
		{
			process.StartInfo.FileName = exe;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			
			process.Start();
			
			bool exited = process.WaitForExit(5000);
			if (!exited)
				throw new SystemException("Timed out.");
			
			stdout = process.StandardOutput.ReadToEnd().Trim();
			stderr = process.StandardError.ReadToEnd().Trim();
			result = process.ExitCode;
		}
		
		return result;
	}
	
	private static void DoRunTool(string tool, string format, params object[] args)
	{
		string arguments = string.Format(format, args);
		if (Verbosity >= 2)
			Console.WriteLine("{0} {1}", tool, arguments);
		
		using (Process process = new Process())
		{
			process.StartInfo.FileName = tool;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardError = true;
			
			process.Start();
			
			bool exited = process.WaitForExit(5000);
			if (!exited)
				throw new SystemException("Timed out.");
			
			string err = process.StandardError.ReadToEnd();
			if (err.Length > 0)
				Console.Error.WriteLine(err);
			
			if (process.ExitCode != 0)
				throw new SystemException("Non-zero exit code.");
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
			Console.Error.WriteLine("One test directory must be specified.");
			Environment.Exit(1);
		}
		
		if (!Directory.Exists(fixedArgs[0]))
		{
			Console.Error.WriteLine("Can't find '{0}'.", fixedArgs[0]);
			Environment.Exit(1);
		}
		
		return fixedArgs[0];
	}
	
	private static void DoShowHelp(string value)
	{
		Console.WriteLine("mono ftester.exe [options] dir");
		ms_options.WriteOptionDescriptions(Console.Out);
		
		Environment.Exit(0);
	}
	
	private static void DoShowVersion(string value)
	{
		Version version = Assembly.GetExecutingAssembly().GetName().Version;
		Console.WriteLine("ftester {0}", version);
		
		Environment.Exit(0);
	}
	#endregion
	
	#region Fields
	private static int ms_verbosity;
	private static int ms_numTests;
	private static int ms_numFailed;
	
	private static OptionSet ms_options = new OptionSet()
	{
		{"h|?|help", "prints this message and exits", Program.DoShowHelp},
		{"v|verbose", "enables extra output, may be used more than once", v => ++ms_verbosity},
		{"version", "prints the version number and exits", Program.DoShowVersion},
	};
	#endregion
}
