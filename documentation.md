# Documentation
This zip file contains code that accepts as input an assembler line with instructions for LDR, STR, LDRB and STRB, then parses and executes it in a similar manner that [VisUAL] does. Any differences from VisUAL will be documented here.

- [Documentation](#documentation)
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
|LDRB       |No restrictions to effective addresses                 |Loads the **byte** stored in the memory address in `RSrc` into register `RDest`, with the most significant 24 bits set to zero.    |
|STRB       |No restrictions to effective addresses                 |Stores the least significant 8 bits of the value in register `RSrc` into the memory address in `RDest`      |
  
_Table 2: Description of the instructions_
  

|Type of offset             |Description                                                                                            |
|---------------------------|-------------------------------------------------------------------------------------------------------|
|Offset addressing          |The effective address is obtained by adding the `OFFSET` to the address stored in `RAdd`               |
|Pre-indexed addressing     |The address stored in `RAdd` is first added with the `OFFSET` before normal LS operation commences     |
|Post-indexed addressing    |The address stored in `RAdd` is added with the `OFFSET` after normal LS operation has commenced        |
  
_Table 3: Description of the different types of offsets_
 
## Implementation
### Parsing
The function `parse` is used in an active pattern matching by the `CommonTop` module, and is called for instructions belonging to the MEM instruction class. `parse` accepts as input a `LineData` record, which is a semi-parsed version of the assembler line. The `root` and `suffix` of the opcode are identified and passed together with `LineData` to the `makeLS` function. This function returns either `Parse<Instr>` or an error if the assembler line was formatted incorrectly.

In the `Memory` module, an `Instr` record type has been introduced to represent the parsed assembler line's relevant information. It contains the following fields:
- `InstrN`: to represent either LDR or STR
- `Type`: to represent the suffix B, if it exists
- `RContents`: to represent the data-storing register; `RDest` in LDR{B}, `RSrc` in STR{B}
- `RAdd`: to represent the address-storing register; `RSrc` in LDR{B}, `RDest` in STR{B}
- `Offset`: to represent the type of offset and its value, if it exists, as the offset can be a literal or stored in a register  

`makeLS` first converts the `root` and the `suffix` to their respective `Instr.InstrN` and `Instr.Type`. It then splits the string of operands and converts them into a list. `Instr.RContents` is obtained from the first operand using a function `getRName`.

If there are two operands, the instruction does not have offset addressing. `Instr.RAdd` is easily obtained using `getRName` from the second operand in the list.

If there are three operands, the instruction involves offset addressing. `getrnVal` is used to obtain `Instr.RAdd`. A comprehensive pattern matching helps identify the type of offset required, and the offset value is processed using `getOffsetVal`. These are stored in `Instr.Offset`.

Any error encountered is propagated through and caught at the final pattern matching; `makeLS` returns said error to inform the top-level module that the assembler line has been incorrectly formatted. If there has been no errors, `makeLS` constructs and returns a proper `Instr` record to the top-level module.

### Execution
`executeMemInstr` is called from the top-level module.  

It then calls the nested `executeLS` function, whose purpose is to identify the correct opcode (out of LDR, STR, LDRB and STRB) and processes the effective address to be accessed. An error is thrown here if the instruction is trying to access memory locations used for storing code. For `LDR`/`STR`, it also checks that the effective address is divisible by four (i.e. it is word-aligned). For `LDRB`/`STRB`, it obtains the base address and offset required for the instruction's byte-addressing -  necessary due to specifying WAddr only as multiples of four. Any changes to the type of WAddr alignment during the group stage needs to be reflected here.     

`executeMemInstr` either returns `DataPath` as acted accordingly upon by the assembler line instruction, or an error with the appropriate error message if the instruction is invalid. Further details of the inner nested functions are available in the code documentation. 

<!-- #### LDR
```
executeMemInstr |> executeLS |> executeLSWord |> executeLOAD
```
#### STR
```
executeMemInstr |> executeLS |> executeLSWord |> executeSTORE
```

#### LDRB
```
executeMemInstr |> executeLS |> executeLDRB |> executeLOAD
```

#### STRB
```
executeMemInstr |> executeLS |> executeSTRB |> executeSTORE
``` -->

## Test plan
Used `Expecto` and `Expecto.FsCheck` packages in testing. All tests are labelled with the `[<Tests>]` attribute so that they can be called with `runTestsInAssembly`. 

All the modules in the VisUAL framework provided have been placed in `VProgam.fs` and have been given the namespace `VisualTest`. Any changes in the file directory for `VisualApp`, `VisualTest` and `VisualWork` are to be reflected accordingly in `defaultParas` located in the `VTest` module. 

Several changes have been made to the framework when used to test `Memory.fs`. The function `RunVisualWithFlagsOut` has been modified so that it can read 13 memory locations in postlude by using register `R0` which was initially used to store the flags. As a result, it has lost the functionality to check for flags, which is not needed by this module but can be easily reverted if need be. Furthermore, `GETWRAPPER` has been prepended with `STOREALLMEM` to allow us to write test data into memory.

|Instructions   |Feature specification tested|
|---------------|----------------------------|
|All            |All instructions' basic functionalities
|All            |Accepts `OFFSET` in the form of negative and positive literals
|All            |Accepts `OFFSET` in the form of decimal, binary and hexedecimal literals
|All            |Accepts `OFFSET` in the form of registers
|All            |Allows post-indexed addressing to update the register value to anything, even memory locations that are off-limits for execution
|All            |Rejects instructions that are trying to access areas other than those tagged with `DataLoc`
|`LDR`/`STR`    |Allows memory access when the address in `RAdd` is not word-aligned, but the effective address after offset is
|`LDR`/`STR`    |Rejects instructions that are trying to access memory locations that are not word-aligned
|`LDR`/`STR`    |Rejects instructions where the address in `RAdd` is word-aligned, but the effective address after offset is not  
|`LDRB`/`STRB`  |Allows memory addresses that are not word-aligned for `LDRB`/`STRB`
|`LDRB`/`STRB`  |Able to access memory addresses that are not word-aligned in addition to offsets for `LDRB`/`STRB`



### Parsing
<!-- Explanation of tests for parsing. All are unit tests, what and how are they tested? Walkthrough of the flow.   -->
Parsing is tested by calling the `onlyParseLine` function, which returns an output of type `Result<Instr, string>`. The output of `onlyParseLine` allows us to easily check if the assembly line has been parsed correctly into its `Memory.Instr` type. It also helps in checking if the module correctly rejects poorly formatted inputs.

The parsing implementation has been tested with a thorough list of unit tests in `parseUnitTest`, including their error messages when appropriate. 

### Execution
<!-- Explanation of tests for execution. Explanation of using VisUAL test framework. All are unit tests, what and how are they tested? Walkthrough of the flow. Mention testing for errors/incorrect assembler lines.  -->

<!-- Btw, **should implement more tests**. Make sure that all the instructions have been tested. Similarly for parsing or no? If not implementing random tests, mention why (?) - left for last. -->

The robustness of the module's ability to execute or reject assembly lines is tested with a list of unit tests in `execUnitTest`. The execution is being tested against VisUAL, which is run using the [framework] provided by Dr Tom Clarke and called in `VisualMemUnitTest`. The `VisOutput` from running VisUAL is processed and packaged into appropriate `DataPath` fields so that they can be tested with the output from the `Memory` module using `Expecto`.

In Load/Store memory instructions, it is important for the ARM simulator to not mess with memory addresses that are not being specifically allocated for data usage. VisUAL also does not allow access to memory locations that are not word-aligned (not applicable to LDRB/STRB), a practice followed by this ARM simulator . Thus, `execErrorUnitTest` is used to check for such cases.  

## Differences from VisUAL
|VisUAL     |`Memory.fs`    |Reasons        |
|-----------|---------------|---------------|
|Inputs are case-insensitive    |Only accepts inputs in all uppercase   |The whole programme can easily be changed to accept assembler lines in a case-insensitive manner by forcing all inputs to uppercase. It is more efficient to implement this functionality in the top-level during the group phase of the project than to do the same conversion in each invididual module. |
|`OFFSET` can be a register, a numerical expression or a shifted register   |`OFFSET` can be a register or a literal    |Numerical expressions and shifted registers will be implemented in the group phase by integrating with the arithmetic instructions module and shift instructions module respectively.  |
|Allows access to memory locations from address ... onwards | Allows access to memory locations corresponding to `DataLoc` tag in `MemLoc` D.U.     |When building the memory map in the top-level, can clearly specify which memory locations are allowed to be accessed as `DataLoc`. It is up to the group's decision whether or not to follow VisUAL's memory map.    |


## Usage
There are two interfaces to the `Memory` module: 
- `parse` takes a `LineData` input and will return `Result<Parse<Memory.Instr>,string> option`.   
- `executeMemInstr` takes inputs of types `Memory.Instr` and `DataPath<Memory.Instr>`.

The `CommonTop` module has several functions that allow us to input an assembly line and receive an output of the CPUdata that has been acted upon accordingly. I have included a function called `execute` to do so, which parses the line if it belongs to the `Memory` module (other instruction types have not yet been implemented) and processes the output so that it can be used in `executeMemInstr`. In summary, `CommonTop` calls `parse`, whose output is processed by `CommonTop` and passed back to `executeMemInstr`.

Ensure that the CPUdata being used is of type `DataPath<Memory.Instr>`. Better top-level execution integration will be needed when other modules are added in the group phase, but is currently not necessary.









  
