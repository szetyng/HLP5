# Todo
- Document STR/LDR executions properly
- Fix executeAnyInstr in CommonTop 
- Try negative offsets

## Delayed todo
- Write specification for parsing and simulation codee

## Error when:  
1. Absence of `#` in front of offset values (rmb to add functionality for hex and bin)

## Memory locations:
- Can only be accessed by LDR and STR instructions
- LDR/STR: access the whole 32-bit memory (i.e. one word i.e. 4 bytes)
- LDRB/STRB: access one byte of the memory location

More details can be found in the [CT5 worksheet](https://intranet.ee.ic.ac.uk/t.clarke/arch/html16/CT5.html) and [memory lecture](https://intranet.ee.ic.ac.uk/t.clarke/arch/html16/lect16/memory.pdf), including information about symbols and labels.

**For each instruction:**
- Write initial tests for the code
- Write code that passes these tests
- Add testing against VisUAL (single and/or complex, randomised tests)
- Add randomised VisUAL tests

## Instructions to be implemented

1. LDR{B}
2. STR{B}

**Usage:**  
LDR{B}{cond} dest, \[source\]                    -> source contains an add, loads contents from that add to dest  
LDR{B}{cond} RDest, \[RSrc , OFFSET\]            -> Offset  
LDR{B}{cond} RDest, \[RSrc , OFFSET\]!           -> Pre-indexed offset  
LDR{B}{cond} RDest, \[RSrc\], OFFSET             -> Post-indexed offset  

STR{B}{cond} source, \[dest\]                    -> dest contains an add, stores content from source to that add  
STR{B}{cond} source, \[dest , OFFSET\]           -> Offset  
STR{B}{cond} source, \[dest , OFFSET\]!          -> Pre-indexed offset  
STR{B}{cond} source, \[dest\], OFFSET            -> Post-indexed offset  

`type InstrLine` has tags `RAdd` and `RContents`, indicating the registers holding the address and the registers where the contents will go to (LDR) / contents come from (STR). `RAdd` for LDR is the address of the register holding the contents, for STR is the address of the register that will store the new contents.
