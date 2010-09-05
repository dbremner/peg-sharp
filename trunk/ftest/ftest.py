#!/usr/bin/python
# Builds and runs the functional test exes (we don't use make files to do this so that we 
# can run these tests on Windows without requiring make).
import glob, os, platform, subprocess, sys
from optparse import OptionParser

Options = None

def runProcess(command, name):
	if Options.verbose >= 1:
		sys.stdout.write('   %s\n' % command)
	process = subprocess.Popen(command, shell = True, stdout = subprocess.PIPE, stderr = subprocess.PIPE)
	(outData, errData) = process.communicate()
	if process.returncode != 0 or len(errData) > 0:
		sys.stdout.flush()
		sys.stderr.flush()
		sys.stdout.write(outData)
		sys.stderr.write(errData)
		raise ValueError('%s failed with error %s' % (name, process.returncode))
	elif len(outData) > 0:
		sys.stdout.flush()
		sys.stdout.write(outData)

def compileParser(command):
	if platform.system() != 'Windows':
		command = "mono --debug " + command
	
	runProcess(command, 'build parser')

def compileExe(exeName, sources):
	if platform.system() == 'Windows':
		flags = '-checked+ -debug+ -warn:4 -warnaserror+ -d:DEBUG -d:TRACE -d:CONTRACTS_FULL'
		command = "csc /out:%s %s /target:exe %s" % (exeName, flags, sources)
	else:
		command = "gmcs -out:%s %s -debug+ -target:exe %s" % (exeName, os.getenv('CSC_FLAGS', ''), sources)
	
	runProcess(command, 'build exe')

def buildParser(dir, options):
	pattern = os.path.join(dir, 'Test*.peg')
	pegs = glob.glob(pattern)
	assert len(pegs) == 1, 'found %s peg files in %s' % (pegs, dir)
	
	parser = pegs[0].replace('.peg', '.cs')
	if os.path.exists(parser):
		os.remove(parser)
	if platform.system() != 'Windows':
		exe = os.path.join('..', 'bin', 'peg-sharp.exe')
	else:
		exe = os.path.join('..', 'bin', 'Debug', 'peg-sharp.exe')
	compileParser("%s --out=%s%s %s" % (exe, parser, options, pegs[0]))

def buildExe(dir):
	pattern = os.path.join(dir, 'Test*.peg')
	pegs = glob.glob(pattern)
	assert len(pegs) == 1
	
	if platform.system() != 'Windows':
		app = os.path.join('..', 'bin', 'ftest', os.path.basename(pegs[0].replace('.peg', '.exe')))
	else:
		app = os.path.join('..', 'bin', 'Debug', os.path.basename(pegs[0].replace('.peg', '.exe')))
	if os.path.exists(app):
		os.remove(app)
	sources = os.path.join(dir, '*.cs')
	compileExe(app, ' '.join(glob.glob(sources)))
	return app
	
def runExe(app):
	command = app
	if platform.system() != 'Windows':
		command = "mono --debug %s" % command
	
	runProcess(command, 'run app')
	
def runTest(dir, options):
	sys.stdout.write(dir + "...")
	try:
		if Options.verbose >= 1:
			sys.stdout.write("\n")
		buildParser(dir, options)
		app = buildExe(dir)
		runExe(app)
		sys.stdout.write("passed\n")
		return True
	except ValueError, e:
		sys.stdout.write("FAILED %s\n" % str(e))
		return False

# Main
parser = OptionParser(usage = 'Usage: ftest.py [options] [test-dirs]')
parser.add_option("-v", "--verbose", action="count", default=0, help="enables extra output")

(Options, args) = parser.parse_args()

failures = 0
if args and len(args) > 0:
	for dir in args:
		if not runTest(dir, " -vvv"):
			failures += 1
else:
	for root, dirs, files in os.walk("."):
		for dir in dirs:
			if dir[0].isdigit():
				if not runTest(dir, ""):
					failures += 1

exit(failures)
