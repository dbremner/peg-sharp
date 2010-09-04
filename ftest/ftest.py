#!/usr/bin/python
# Builds and runs the functional test exes (we don't use make files to do this so that we 
# can run these tests on Windows without requiring make).
import glob
import os
import platform
import subprocess
import sys

Verbose = False

def runProcess(command, name):
	if Verbose:
		sys.stdout.write('   %s\n' % command)
	process = subprocess.Popen(command, shell = True, stdout = subprocess.PIPE, stderr = subprocess.PIPE)
	(outData, errData) = process.communicate()
	if process.returncode != 0 or len(errData) > 0:
		sys.stdout.flush()
		sys.stderr.flush()
		sys.stdout.write(outData)
		sys.stderr.write(errData)
		raise ValueError('%s failed with error %s' % (name, process.returncode))

def compileParser(command):
	if platform.system() != 'Windows':
		command = "mono --debug " + command
	
	runProcess(command, 'build parser')

def compileExe(exeName, sources):
	if platform.system() == 'Windows':
		flags = '-checked+ -debug+ -warn:4 -warnaserror+ -d:DEBUG -d:TRACE -d:CONTRACTS_FULL'
		command = "csc /out:%s %s /target:exe %s" % (exeName, flags, sources)
	else:
		command = "gmcs -out:%s %s -target:exe %s" % (exeName, os.getenv('CSC_FLAGS', ''), sources)
	
	runProcess(command, 'build exe')

def buildParser(dir):
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
	compileParser("%s --out=%s %s" % (exe, parser, pegs[0]))

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
		command = "mono --debug " + command
	
	runProcess(command, 'run app')
	
def runTest(dir):
	sys.stdout.write(dir + "...")
	try:
		if Verbose:
			sys.stdout.write("\n")
		buildParser(dir)
		app = buildExe(dir)
		runExe(app)
		sys.stdout.write("passed\n")
		return True
	except ValueError, e:
		sys.stdout.write("FAILED %s\n" % str(e))
		return False

failures = 0
for root, dirs, files in os.walk("."):
	for dir in dirs:
		if dir[0].isdigit():
			if not runTest(dir):
				failures += 1

exit(failures)