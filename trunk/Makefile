# ------------------
# Public variables
CSC ?= gmcs
MONO ?= mono
PYTHON ?= python
GENDARME ?= /usr/local/bin/gendarme
INSTALL_DIR ?= /usr/local/bin

# script that looks something like: mono --debug /some/path/nunit-console.exe $@
NUNIT ?= nunit-console3

ifdef RELEASE
	# Note that -debug+ just generates an mdb file.
	CSC_FLAGS ?= -checked+ -debug+ -optimize+ -warn:4 -d:TRACE -d:CONTRACTS_PRECONDITIONS
else
	CSC_FLAGS ?= -checked+ -debug+ -warn:4 -warnaserror+ -d:DEBUG -d:TRACE -d:CONTRACTS_FULL
endif

# ------------------
# Internal variables
dummy1 := $(shell mkdir bin 2> /dev/null)
dummy2 := $(shell if [[ "$(CSC_FLAGS)" != `cat bin/csc_flags 2> /dev/null` ]]; then echo "$(CSC_FLAGS)" > bin/csc_flags; fi)

base_version := 0.1.xxx.0										# major.minor.build.revision
version := $(shell ./get_version.sh $(base_version) build_num)	# this will increment the build number stored in build_num
version := $(strip $(version))
export version

# ------------------
# Primary targets
all: app example

app: bin/peg-sharp.exe

example: bin/example.exe

check: utest ftest

utest: bin/unit-tests.dll
	cd bin && "$(NUNIT)" -nologo unit-tests.dll

.PHONY: ftest
ftest: bin/peg-sharp.exe
	cd ftest && "$(PYTHON)" ftest.py

update-libraries:
	cp `pkg-config --variable=Sources mono-options` source

# ------------------
# Binary targets 
sources := source/*.cs

bin/peg-sharp.exe: bin/csc_flags $(sources)
	@./gen_version.sh $(version) source/AssemblyVersion.cs
	$(CSC) -out:$@ $(CSC_FLAGS) -target:exe $(sources)

example/Parser.cs: bin/peg-sharp.exe example/Parser.peg
	$(MONO) --debug bin/peg-sharp.exe --out=example/Parser.cs example/Parser.peg

bin/example.exe: example/Parser.cs example/*.cs
	$(CSC) -out:bin/example.exe $(CSC_FLAGS) -target:exe example/*.cs

bin/unit-tests.dll: bin/csc_flags $(sources)
	$(CSC) -out:$@ $(CSC_FLAGS) -d:TEST -r:bin/nunit.framework.dll -target:library $(sources)

# ------------------
# Misc targets
gendarme_flags := --severity all --confidence all --ignore gendarme.ignore --quiet
gendarme: bin/peg-sharp.exe
	@-"$(GENDARME)" $(gendarme_flags) bin/peg-sharp.exe

clean:
	-rm -rf bin/*.exe
	-rm -rf bin/*.exe.mdb
	-rm -rf bin/unit-tests.dll
	-rm -rf bin/unit-tests.dll.mdb
	-rm -rf bin/ftest

install: app
	install "bin/peg-sharp.exe" "$(INSTALL_DIR)"
ifndef RELEASE
	install "bin/peg-sharp.exe.mdb" "$(INSTALL_DIR)"
endif
	echo "#!/bin/sh" > $(INSTALL_DIR)/peg-sharp
	echo "exec $(MONO) \x24MONO_OPTIONS --debug $(INSTALL_DIR)/peg-sharp.exe \x24@" >> $(INSTALL_DIR)/peg-sharp
	chmod +x $(INSTALL_DIR)/peg-sharp

uninstall:
	-rm "$(INSTALL_DIR)/peg-sharp"
	-rm "$(INSTALL_DIR)/peg-sharp.exe"
	-rm "$(INSTALL_DIR)/peg-sharp.exe.mdb"

help:
	@echo "peg-sharp version $(version)"
	@echo " "
	@echo "The primary targets are:"
	@echo "app              - builds the application"
	@echo "check             - runs the unit and functional tests"
	@echo "update-libraries - uses pkg-config to copy libraries peg-sharp depends upon"
	@echo "clean            - removes generated files from the bin directory"
	@echo "install          - copies the exe to $(INSTALL_DIR)"
	@echo "uninstall        - removes the exe from $(INSTALL_DIR)"
