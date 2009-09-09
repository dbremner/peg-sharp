#!/usr/bin/python
import os
import subprocess
import sys

def run(path, command):
	process = subprocess.Popen(command, cwd = path, shell = True, stdout = subprocess.PIPE, stderr = subprocess.PIPE)
	(outData, errData) = process.communicate()
	if process.returncode != 0 or len(errData) > 0:
		sys.stdout.flush()
		sys.stderr.flush()
		sys.stdout.write(outData)
		sys.stderr.write(errData)
		return False
	else:
		return True

# TODO: print stats
failures = 0
for root, dirs, files in os.walk("."):
	for dir in dirs:
		if dir[0].isdigit():
			sys.stdout.write(dir + "...")
			if run(dir, "make run"):
				sys.stdout.write("passed\n")
			else:
				failures += 1

exit(failures)