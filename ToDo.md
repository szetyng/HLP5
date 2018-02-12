# Todo
- Write specification for parsing and simulation code
- Write proper error codes (mentioned some in comments)

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
