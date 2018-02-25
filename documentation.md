# Documentation
This zip file contains code that accepts as input an assembler line with instructions for LDR, STR, LDRB and STRB, then parses and executes it in a similar manner that [VisUAL] does. Any differences from VisUAL will be documented here.

## Specifications

|Opcode     |Name                                                                                 |Instructions implemented        |
|-----------|-------------------------------------------------------------------------------------|--------------------------------|
|LDR        |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`LDR RDest, [RSrc]`<br>`LDR RDest, [RSrc, OFFSET]`<br>`LDR RDest, [RSrc, OFFSET]!`<br>`LDR RDest, [RSrc], OFFSET`            |
|STR        |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`STR RSrc, [RDest]`<br>`STR RSrc, [RDest, OFFSET]`<br>`STR RSrc, [RDest, OFFSET]!`<br>`STR RSrc, [RDest], OFFSET`            |
|LDRB       |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`LDRB RDest, [RSrc]`<br>`LDRB RDest, [RSrc, OFFSET]`<br>`LDRB RDest, [RSrc, OFFSET]!`<br>`LDRB RDest, [RSrc], OFFSET`    |
|STRB       |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`STRB RSrc, [RDest]`<br>`STRB RSrc, [RDest, OFFSET]`<br>`STRB RSrc, [RDest, OFFSET]!`<br>`STRB RSrc, [RDest], OFFSET`    |
  
_Table 1: List of instructions implemented_
  

|Opcode     |Effective address  |Description            |
|-----------|-------------------|-----------------------|
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
  
`OFFSET` can be stated as a literal or stored in a register. 

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
`executeMemInstr` is called from `CommonTop` and takes a `Memory.Instr` record and a `DataPath` type as inputs. It then calls the nested `executeLS` function, whose purpose is to identify the correct opcode (out of LDR, STR, LDRB and STRB) and processes the effective address to be accessed. An error is thrown here if the instruction is trying to access memory locations used for storing code.   

`executeMemInstr` either returns `DataPath` as acted accordingly upon by the assembler line instruction, or an error with the appropriate error message if the instruction is invalid. The flow of data for the different opcodes are detailed in the following sections.   

#### LDR
```
executeMemInstr
|> executeLS
|> executeLSWord
|> executeLOAD
```
`executeLS`: Checks that the effective address is divisible by four  
`executeLSWord`: Obtains the payload - which is the word stored in `RSrc`/`RContents` - from memory  
`executeLOAD`: Updates the register map field of `DataPath` to represent the payload being loaded to a register from memory, and pre- or post-indexing of `RSrc` as required  

#### STR
```
executeMemInstr
|> executeLS
|> executeLSWord
|> executeSTORE
```
`executeLS`: Checks that the effective address is divisible by four  
`executeLSWord`: Obtains the payload - which is the word stored in `RSrc`/`RContents` - from the register map  
`executeSTORE`: Updates the memory map field of `DataPath` to represent the payload being stored to memory from a register. Also updates the register map if required by pre- or post-indexing  

#### LDRB
```
executeMemInstr
|> executeLS
|> executeLDRB
|> executeLOAD
```
`executeLS`: Obtains the base address and offset required for the instruction's byte-addressing (vs word-addressing in regular LDR). Required due to specifying WAddr only as multiples of four  
`executeLDRB`: Sets the register `RDest` to zero as a way to preemptively set its most significant 24 bits to zero. Obtains the payload - which is the word stored in the base address - from memory. Processes the word payload to get the effective payload - the 8 bits stored in the byte-aligned effective address    
`executeLOAD`: Updates the register map field of `DataPath` to represent the payload being loaded to a register from memory, and pre- or post-indexing of `RSrc` as required


#### STRB
```
executeMemInstr
|> executeLS
|> executeSTRB
|> executeSTORE
```
`executeLS`: Obtains the base address and offset required for the instruction's byte-addressing (vs word-addressing in regular STR). Required due to specifying WAddr only as multiples of four   
`executeSTRB`: Obtains the payload - which is the word stored in `RSrc` - from the register map. Processes the word payload to get the effective payload - the least significant 8 bits in said word located in the relevant byte position, with the rest of the word in the base address unchanged   
`executeSTORE`: Updates the memory map field of `DataPath` to represent the payload being stored to memory from a register. Also updates the register map if required by pre- or post-indexing   


## Testing
Used `Expecto` and `Exepcto.FsCheck` packages in testing. All tests are labelled with the `[<Tests>]` attribute so that they can be called with `runTestsInAssembly`.

### Parsing
<!-- Explanation of tests for parsing. All are unit tests, what and how are they tested? Walkthrough of the flow.   -->
Parsing is tested by calling the `onlyParseLine` function, which returns an output of type `Result<Instr, string>`. The output of `onlyParseLine` allows us to easily check if the assembly line has been parsed correctly into its `Memory.Instr` type. It also helps in checking if the module correctly rejects poorly formatted inputs.

The parsing implementation has been tested with a thorough list of unit tests in `parseUnitTest`, including their error messages when appropriate.

### Execution
<!-- Explanation of tests for execution. Explanation of using VisUAL test framework. All are unit tests, what and how are they tested? Walkthrough of the flow. Mention testing for errors/incorrect assembler lines.  -->

<!-- Btw, **should implement more tests**. Make sure that all the instructions have been tested. Similarly for parsing or no? If not implementing random tests, mention why (?) - left for last. -->

The robustness of the module's ability to execute or reject assembly lines is tested with a list of unit tests in `execUnitTest`. The execution is being tested against VisUAL, which is run using the [framework] provided by Dr Tom Clarke and called in `VisualMemUnitTest`. Several changes have been made to this framework, detailed below. The `VisOutput` from running VisUAL is processed and packaged into appropriate `DataPath` fields so that they can be tested with the output from the `Memory` module using `Expecto`.

In Load/Store memory instructions, it is important for the ARM simulator to not mess with memory addresses that are not being specifically allocated for data usage. VisUAL also does not allow access to memory locations that are not word-aligned (not applicable to LDRB/STRB), a practice followed by this ARM simulator . Thus, `execErrorUnitTest` is used to check for such cases. Btw does it check for poor formats?

## Differences from VisUAL
|VisUAL     |`Memory.fs`    |Reasons        |
|-----------|---------------|---------------|
|Inputs are case-insensitive    |Only accepts inputs in all uppercase   |The whole programme can easily be changed to accept assembler lines in a case-insensitive manner by forcing all inputs to uppercase. It is more efficient to implement this functionality in the top-level during the group phase of the project than to do the same conversion in each invididual module. |
|`OFFSET` can be a register, a numerical expression or a shifted register   |`OFFSET` can be a register or a literal    |Numerical expressions and shifted registers will be implemented in the group phase by integrating with the arithmetic instructions module and shift instructions module respectively.  |
|Allows access to memory locations from address ... onwards | Allows access to memory locations corresponding to `DataLoc` tag in `MemLoc` D.U.     |When building the map in top-level, can specify which memory locations are allowed to be accessed more clearly.    |


1. The `Memory` module only accepts assembler line inputs that are in all uppercase, while VisUAL is case-insensitive to inputs.   
The whole programme can easily be changed to accept assembler lines in a case-insensitive manner by forcing all inputs to uppercase. It is more efficient to implement this functionality in the top-level during the group phase of the project than to do the same conversion in each invididual module.

2. VisUAL allows `OFFSET` to be a register, a numerical expression or a shifted register. The `Memory` module allows `OFFSET` to be a register or a literal.  
Numerical expressions and shifted registers will be implemented in the group phase by integrating with the arithmetic instructions module and shift instructions module respectively.

3. VisUAL allows access to memory locations from address ... onwards. `Memory` module allows access whenever the `MachineMemory` map in `DataPath` corresponds to `DataLoc`, not `Code`.  
When building the map in top-level, can specify which memory locations are allowed to be accessed more clearly.


## Changes in top-level code / VisUAL test framework
Getting rid of flags  
In testing, can only test for 13 memory locations.  


## Usage
There are two interfaces to the `Memory` module: 
- `parse` takes a `LineData` input and will return `Result<Parse<Memory.Instr>,string> option`.   
- `executeMemInstr` takes inputs of types `Memory.Instr` and `DataPath<Memory.Instr>`.

The `CommonTop` module has several functions that allow us to input an assembly line and receive an output of the CPUdata that has been acted upon accordingly. I have included a function called `execute` to do so, which parses the line if it belongs to the `Memory` module (other instruction types have not yet been implemented) and processes the output so that it can be used in `executeMemInstr`.

Ensure that the CPUdata being used is of type `DataPath<Memory.Instr>`. Better top-level execution integration will be needed when other modules are added in the group phase, but is currently not needed.








  
