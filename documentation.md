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
|STRB       |No restrictions to effective addresses                 |Stores the least significant 8 bits of the value in `RSrc` into the memory address in `RDest`      |
  
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


### Execution

## Testing
### Parsing
### Execution

## Differences from VisUAL
In testing, can only test for 13 memory locations.
Case-sensitive, only accepts assembler lines in all uppercase. Plan 

## Changes in top-level code / VisUAL test framework
Executing from CommonTop
Getting rid of flags

## Usage
How to call from CommonTop. Integration with the rest of the group.
  
