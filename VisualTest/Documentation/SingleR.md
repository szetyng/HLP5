# Documentation
This zip file contains code that accepts as input an assembler line with instructions for LDR, STR, LDRB and STRB, then parses and executes it in a similar manner that [VisUAL](https://salmanarif.bitbucket.io/visual/index.html) does. The parsing and execution of the instructions are done in the `SingleR` module, and the processing between these two functions are done in the `CommonTop` module. Any differences from VisUAL will be documented here. Further explanation on the usage of the code is available [below](#usage), and information on how to run the tests is available in the [test plan](#test-plan). How my code contributes to the group deliverable can be seen from its [usage](#usage) and from the [specifications](#specifications).

## Table of contents
- [Specifications](#specifications)
- [Implementation](#implementation)
    - [Parsing](#parsing)
    - [Execution](#execution)
- [Test plan](#test-plan)
    - [Parsing](#parsing)
    - [Execution](#execution)
- [Differences from VisUAL](#differences-from-visual)
- [Usage](#usage)

## Specifications

|Opcode     |Name                                                                                 |Instructions implemented        |
|:---------:|------------------------------------------------------------------------------------:|--------------------------------|
|LDR        |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`LDR RDest, [RSrc]`<br>`LDR RDest, [RSrc, OFFSET]`<br>`LDR RDest, [RSrc, OFFSET]!`<br>`LDR RDest, [RSrc], OFFSET`            |
|STR        |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`STR RSrc, [RDest]`<br>`STR RSrc, [RDest, OFFSET]`<br>`STR RSrc, [RDest, OFFSET]!`<br>`STR RSrc, [RDest], OFFSET`            |
|LDRB       |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`LDRB RDest, [RSrc]`<br>`LDRB RDest, [RSrc, OFFSET]`<br>`LDRB RDest, [RSrc, OFFSET]!`<br>`LDRB RDest, [RSrc], OFFSET`    |
|STRB       |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`STRB RSrc, [RDest]`<br>`STRB RSrc, [RDest, OFFSET]`<br>`STRB RSrc, [RDest, OFFSET]!`<br>`STRB RSrc, [RDest], OFFSET`    |
  
_Table 1: List of instructions implemented_  
  
`OFFSET` can be stated as a literal or stored in a register.
    

|Opcode     |Effective address  |Description            |
|:---------:|-------------------|-----------------------|
|LDR        |All effective addresses must be a multiple of four     |Loads the **word** stored in the memory address in `RSrc` into register `RDest`.  |
|STR        |All effective addresses must be a multiple of four     |Stores the **word** from register `RSrc` into the memory address in `RDest`            |
|LDRB       |No restrictions to effective addresses                 |Loads the **byte** stored in the memory address in `RSrc` into register `RDest`, with the most significant 24 bits of the destination register set to zero.    |
|STRB       |No restrictions to effective addresses                 |Stores the **least significant 8 bits** of the value in register `RSrc` into the memory address in `RDest`      |
  
_Table 2: Description of the instructions_
  

|Type of offset             |Description                                                                                            |
|---------------------------|-------------------------------------------------------------------------------------------------------|
|Offset addressing          |The effective address is obtained by adding the `OFFSET` to the address stored in `RAdd`               |
|Pre-indexed addressing     |The address stored in `RAdd` is first added with the `OFFSET` before normal LS operation commences     |
|Post-indexed addressing    |The address stored in `RAdd` is added with the `OFFSET` after normal LS operation has commenced        |
  
_Table 3: Description of the different types of offsets_  
Note: `RAdd` corresponds to `RSrc` in LDR{B} and `RDest` in STR{B}
 
## Implementation
### Parsing
The function `parse` is used in an active pattern matching by the `CommonTop` module, and is called for instructions belonging to the MEM instruction class. The `root` and `suffix` of the opcode are identified and passed together with `LineData` to the `makeLS` function. This function returns either `Parse<Instr>` or an error if the assembler line was formatted incorrectly. An `Instr` record type has been introduced to represent the parsed assembler line's relevant information.  

A properly formatted `LineData` is fully parsed through a series of comprehensive pattern matchings. Any error encountered is propagated through and caught at the final pattern matching; `makeLS` returns said error to inform the top-level module that the assembler line has been incorrectly formatted. If there has been no errors, `makeLS` constructs and returns a proper `Instr` record to the top-level module.

### Execution
`executeMemInstr` is called from the top-level module.  

It then calls the nested `executeLS` function, whose purpose is to identify the correct opcode (out of LDR, STR, LDRB and STRB) and processes the effective address to be accessed. An error is thrown here if the instruction is trying to access memory locations used for storing code. For `LDR`/`STR`, it also checks that the effective address is divisible by four (i.e. it is word-aligned). For `LDRB`/`STRB`, it obtains the base address and offset required for the instruction's byte-addressing -  necessary due to specifying WAddr only as multiples of four. Any changes to the type of WAddr alignment during the group stage needs to be reflected here.     

`executeMemInstr` either returns `DataPath` as acted accordingly upon by the assembler line instruction, or an error with the appropriate error message if the instruction is invalid. Further details of the inner nested functions are available in the code documentation. 

## Test plan
Used `Expecto` and `Expecto.FsCheck` packages in testing. All tests are labelled with the `[<Tests>]` attribute so that they can be called with `runTestsInAssembly`. Run tests by running `Test.fs`. 

All the modules in the VisUAL framework provided have been placed in `VProgam.fs` and have been given the namespace `VisualTest`. Any changes in the file directory for `VisualApp`, `VisualTest` and `VisualWork` are to be reflected accordingly in `defaultParas` located in the `VTest` module. 


|Instructions   |Feature specification tested|
|---------------|----------------------------|
|All            |All instructions' basic functionalities
|All            |Accepts `OFFSET` in the form of negative and positive literals
|All            |Accepts `OFFSET` in the form of decimal, binary and hexedecimal literals
|All            |Accepts `OFFSET` in the form of registers
|All            |Allows post-indexed addressing to update the register value to anything, even memory locations that are off-limits for execution
|All            |Rejects instructions that are trying to access areas other than those tagged with `DataLoc`
|All            |Parses zero offset to be equivalent to no offset
|`LDR`/`STR`    |Allows memory access when the address in `RAdd` is not word-aligned, but the effective address after offset is
|`LDR`/`STR`    |Rejects instructions that are trying to access memory locations that are not word-aligned  
|`LDRB`/`STRB`  |Allows memory addresses that are not word-aligned for `LDRB`/`STRB`
|`LDRB`/`STRB`  |Able to access memory addresses that are not word-aligned in addition to offsets for `LDRB`/`STRB`



### Parsing
Parsing is tested by calling the `onlyParseLine` function, which returns an output of type `Result<Instr, string>`. The output of `onlyParseLine` allows us to easily check if the assembly line has been parsed correctly into its `SingleR.Instr` type. It also helps in checking if the module correctly rejects poorly formatted inputs.

The parsing implementation has been tested with a thorough list of unit tests in `parseUnitTest`, including their error messages when appropriate. 

### Execution
The robustness of the module's ability to execute or reject assembly lines is tested with a list of unit tests in `execUnitTest`. The execution is being tested against VisUAL, which is run using the [framework](https://intranet.ee.ic.ac.uk/t.clarke/hlp/images/VisualTesting.zip) provided by Dr Tom Clarke and called in `VisualMemUnitTest`. The `VisOutput` from running VisUAL is processed and packaged into appropriate `DataPath` fields so that they can be tested with the output from the `SingleR` module using `Expecto`. Each test checks both the register map and the memory map.

In Load/Store memory instructions, it is important for the ARM simulator to not mess with memory addresses that are not being specifically allocated for data usage. VisUAL also does not allow access to memory locations that are not word-aligned (not applicable to LDRB/STRB), a practice followed by this ARM simulator. Thus, `execErrorUnitTest` is used to check for such cases as well as any other instruction lines that may lead to execution errors.  

## Differences from VisUAL
|VisUAL     |`SingleR.fs`    |Reasons        |
|-----------|---------------|---------------|
|Inputs are case-insensitive    |Only accepts inputs in all uppercase   |The whole programme can be easily changed to accept assembler lines in a case-insensitive manner by forcing all inputs to uppercase. It is more efficient to implement this functionality in the top-level during the group phase of the project than to do the same conversion in each invididual module.<br>UPDATE: Inputs are now case-insensitive in group phase. Parse tests in `SingleRTests` are still case-insensitive due to the built in parser for the test module. |
|`OFFSET` can be a register, a numerical expression or a shifted register   |`OFFSET` can be a register or a literal    |Numerical expressions and shifted registers will be implemented in the group phase by integrating with the arithmetic instructions module and shift instructions module respectively.  |
|Allows access to memory locations from address `0x1000` onwards. Addresses before that are reserved for instructions. | Allows access to memory locations corresponding to `DataLoc` tag in `MemLoc` D.U.     |When building the memory map in the top-level, we can clearly specify which memory locations are allowed to be accessed as `DataLoc`, and which are reserved as `Code`. It is up to the group's decision whether or not to follow VisUAL's memory map, or to expand/reduce the area reserved for instructions.    |
|LS instructions with a register offset are not allowed use of R13 or R15 as the source base address, because they are the source pointer (SP) and program counter (PC) respectively. This was discovered during testing.  |LS instructions with a register offset are allowed use of R13 or R15 as the source base address  |Changing SP or PC when finding offset address is dangerous because the programme would lose its position in execution. This restriction will be implemented in the group phase by integrating with the branch instructions.    |


## Usage
There are two interfaces to the `SingleR` module: 
- `parse` takes a `LineData` input and will return `Result<Parse<SingleR.Instr>,string> option`.   
- `executeMemInstr` takes inputs of types `Single.Instr` and `DataPath<'INS>`.

The `CommonTop` module has several functions that allow us to input an assembly line and receive an output of the CPUdata that has been acted upon accordingly. I have written a function called `execute` to do so, which parses the line if it belongs to the `SingleR` module (other instruction types have not yet been implemented) and processes the output so that it can be used in `executeMemInstr`. Any kind of interface function that converts the output of `parse` into a suitable input for `executeMemInstr` is allowed.

In summary, `CommonTop` calls `parse`, whose output is processed by `CommonTop` and passed back to `executeMemInstr`. Tests are run using the `execute` function that I wrote in `CommonTop`, so it is necessary to include this module while testing. `DP` module is a useless dummy that is included in the zip file because of the references from `CommonTop`.

Ensure that the CPUdata being used is of type `DataPath<'INS>`. Better top-level execution integration will be needed when other modules are added in the group phase, but is currently not necessary. If you intend to run `executeMemInstr` without using inputs generated by `parse`, make sure that the registers are valid because the register-checking is done in the parse function.









  
