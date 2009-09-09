# ------------------
# Public variables
CSC ?= gmcs
MONO ?= mono

CSC_FLAGS ?= -checked+ -debug+ -warn:4 -warnaserror+ -d:DEBUG -d:TRACE -d:CONTRACTS_FULL

# ------------------
# Internal variables
parser := Test10
app := ../../bin/ftest/test10.exe

# ------------------
# Primary targets
all: $(app)

run: $(app)
	$(MONO) --debug $(app)

$(parser).cs: $(parser).peg ../../bin/peg-sharp.exe
	$(MONO) --debug ../../bin/peg-sharp.exe --out=$(parser).cs $(parser).peg

$(app): $(parser).cs *.cs
	$(CSC) -out:$(app) $(CSC_FLAGS) -target:exe *.cs
