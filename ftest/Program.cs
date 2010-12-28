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
			DoRun(Path.GetFileName(srcDir), exe, data[0]);
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
			DoGenerateCsProgram(program, parserName);
			
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
	
	private static void DoGenerateCsProgram(string program, string parserName)
	{
		using (var stream = new StreamWriter(program, false, System.Text.Encoding.UTF8))
		{
			stream.WriteLine("using System;");
			stream.WriteLine("");
			stream.WriteLine("internal static class Program");
			stream.WriteLine("{");
			stream.WriteLine("	public static void Main(string[] args)");
			stream.WriteLine("	{");
			stream.WriteLine("		try");
			stream.WriteLine("		{");
			stream.WriteLine("			string text = args[0];");
			stream.WriteLine("			text = text.Replace(\"&bslash;\", \"\\\\\");");
			stream.WriteLine("			text = text.Replace(\"&amp;\", \"'\");");
			stream.WriteLine("			text = text.Replace(\"&nl;\", \"\\n\");");
			stream.WriteLine("			var parser = new {0}();", parserName);
			stream.WriteLine("			var result = parser.Parse(text);");
			stream.WriteLine("			Console.WriteLine(result);");
			stream.WriteLine("		}");
			stream.WriteLine("		catch (ParserException e)");
			stream.WriteLine("		{");
			stream.WriteLine("			Console.Error.WriteLine(e.Message);");
			stream.WriteLine("		}");
			stream.WriteLine("		catch (Exception e)");
			stream.WriteLine("		{");
			stream.WriteLine("			Console.Error.WriteLine(e.Message);");
			stream.WriteLine("		}");
			stream.WriteLine("	}");
			stream.WriteLine("}");
		}
	}
	
	private static void DoRun(string testName, string exe, string dataFile)
	{
		const string sep = "#------------------------------------------------------------------------------";
		
		string contents = File.ReadAllText(dataFile);
		string[] tests = contents.Split(new string[]{sep}, StringSplitOptions.RemoveEmptyEntries);
		foreach (string test in tests)
		{
			++ms_numTests;
			DoRunTest(testName, exe, FieldParser.Parse(test));
		}
	}
	
	private static void DoRunTest(string testName, string exe, Field[] fields)
	{
		System.Diagnostics.Debug.Assert(fields.Length == 2);
		System.Diagnostics.Debug.Assert(fields[0].Name == "text");
		
		string stdout, stderr;
		string input = DoSanitize(fields[0].Value);
		DoRunParser(exe, input, out stdout, out stderr);
		
		stdout = DoSanitize(stdout);
		stderr = DoSanitize(stderr);
		
		var failures = new List<string>();
		if (fields[1].Name == "stdout")
		{
			if (stderr.Length > 0)
				failures.Add(string.Format("got stderr: {0}.", stderr));
			
			string expected = DoSanitize(fields[1].Value);
			if (stdout != expected)
				failures.Add(string.Format("expected {0} but got {1}.", expected, stdout));
		}
		else if (fields[1].Name == "stderr")
		{
			if (stderr.Length == 0)
				failures.Add(string.Format("no stderr but expected: {0}.", fields[1].Value));
			else if (!stderr.Contains(fields[1].Value))
				failures.Add(string.Format("expected stderr containing {0} but got {1}.", fields[1].Value, stderr));
		}
		else
			System.Diagnostics.Debug.Assert(false);
		
		if (failures.Count > 0)
		{
			Console.Error.WriteLine("{0} failed with '{1}'", testName, fields[0].Value);
			foreach (string f in failures)
			{
				Console.Error.WriteLine("   {0}", f);
			}
			++ms_numFailed;
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
		}
		
		return string.Join("\n", lines);
	}
	
	private static void DoRunParser(string exe, string text, out string stdout, out string stderr)
	{
		text = "'" + text + "'";
		
		if (Verbosity >= 2)
			Console.WriteLine("{0} {1}", exe, text);
		
		using (Process process = new Process())
		{
			process.StartInfo.FileName = exe;
			process.StartInfo.Arguments = text;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			
			process.Start();
			
			bool exited = process.WaitForExit(5000);
			if (!exited)
				throw new SystemException("Timed out.");
			
			stdout = process.StandardOutput.ReadToEnd().Trim();
			stderr = process.StandardError.ReadToEnd().Trim();
			
			if (process.ExitCode != 0)
				throw new SystemException("Non-zero exit code.");
		}
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
