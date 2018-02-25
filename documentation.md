# Documentation
This zip file contains code that accepts as input an assembler line with instructions for LDR, STR, LDRB and STRB, then parses and executes it in a similar manner that [VisUAL] does. Any differences from VisUAL will be documented here.

## Specifications

|Opcode     |Name                                                                                 |Instructions implemented        |
|-----------|-------------------------------------------------------------------------------------|--------------------------------|
|LDR        |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`LDR{cond} RDest, [RSrc]`<br>`LDR{cond} RDest, [RSrc, OFFSET]`<br>`LDR{cond} RDest, [RSrc, OFFSET]!`<br>`LDR{cond} RDest, [RSrc], OFFSET`            |
|STR        |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`STR{cond} RSrc, [RDest]`<br>`STR{cond} RSrc, [RDest, OFFSET]`<br>`STR{cond} RSrc, [RDest, OFFSET]!`<br>`STR{cond} RSrc, [RDest], OFFSET`            |
|LDRB       |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`LDRB{cond} RDest, [RSrc]`<br>`LDRB{cond} RDest, [RSrc, OFFSET]`<br>`LDRB{cond} RDest, [RSrc, OFFSET]!`<br>`LDRB{cond} RDest, [RSrc], OFFSET`    |
|STRB       |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`STRB{cond} RSrc, [RDest]`<br>`STRB{cond} RSrc, [RDest, OFFSET]`<br>`STRB{cond} RSrc, [RDest, OFFSET]!`<br>`STRB{cond} RSrc, [RDest], OFFSET`    |
  
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
  
`OFFSET` can be stated as a positive literal (in decimal) or stored in a register. 

## Implementation
### Parsing
The function `parse` is used in an active pattern matching by the `CommonTop` module, and is only called for instructions belonging to the MEM instruction class. `parse` accepts as input a `LineData` record, which is a semi-parsed version of the assembler line. The `root` and `suffix` of the opcode are identified and passed together with `LineData` to the `makeLS` function. This function returns either `Parse<Instr>` or an error if the assembler line was formatted incorrectly.

In the `Memory` module, an `Instr` record type has been introduced to represent the parsed assembler line's relevant information. It contains the following fields:
- `InstrN`: to represent either LDR or STR
- `Type`: to represent the suffix B, if it exists
- `RContents`: to represent the data-storing register; `RDest` in LDR{B}, `RSrc` in STR{B}
- `RAdd`: to represent the address-storing register; `RSrc` in LDR{B}, `RDest` in STR{B}
- `Offset`: to represent the type of offset and its value, if it exists, as the offset can be a literal or stored in a register.

`makeLS` first converts the `root` and the `suffix` to their respective `Instr.InstrN` and `Instr.Type`. It then splits the string of operands and converts them into a list. `Instr.RContents` is obtained from the first operand using a function `getRName`.

If there are two operands, the instruction does not have offset addressing. `Instr.RAdd` is easily obtained using `getRName` from the second operand in the list.

If there are three operands, the instruction involves offset addressing. `getrnVal` is used to obtain `Instr.RAdd`. A comprehensive pattern matching helps identify the type of offset required, and the offset value is processed using `getOffsetVal`. These are stored in `Instr.Offset`.

Any error encountered is propagated through and caught at the final pattern matching; `makeLS` returns said error to inform the top-level module that the assembler line has been incorrectly formatted. If there has been no errors, `makeLS` constructs and returns a proper `Instr` record to the top-level module.

### Execution
`executeMemInstr` is called from `CommonTop` and takes a `Memory.Instr` record and a `DataPath` type as inputs. It then calls the nested `executeLS` function, whose purpose is to identify the correct opcode (out of LDR, STR, LDRB and STRB) and processes the effective address to be accessed. An error is thrown here if the instruction is trying to access memory locations used for storing code. The flow of data for the different opcodes are detailed in the following sections.

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
`executeLDRB`: Sets the register `RDest` to zero as a way to preemptively set its most significant 24 bits to zero. Obtains the payload - which is the word stored in the base address - from memory  



#### STRB
```
executeMemInstr
|> executeLS
|> executeSTRB
|> executeSTORE
```





## Testing
### Parsing
Explanation of tests for parsing. All are unit tests, what and how are they tested? Walkthrough of the flow.

### Execution
Explanation of tests for execution. Explanation of using VisUAL test framework. All are unit tests, what and how are they tested? Walkthrough of the flow. Mention testing for errors/incorrect assembler lines. 

Btw, **should implement more tests**. Make sure that all the instructions have been tested. Similarly for parsing or no? If not implementing random tests, mention why (?) - left for last.

## Differences from VisUAL
In testing, can only test for 13 memory locations.  
Curently case-sensitive, only accepts assembler lines in all uppercase. Plan to implement in group stage, toUpper all assembler lines. More efficient than doing it at a module level.  
VisUAL allows `OFFSET` to be a register, literals or shifted register. **I allow register and decimals, will fix soon.**  
VisUAL allows **negative literals**, do mine?  
DataLoc -> specify what is meant here  
  

## Changes in top-level code / VisUAL test framework
Executing from CommonTop  
Getting rid of flags  

## Usage
How to call from CommonTop. Integration with the rest of the group.  
  
