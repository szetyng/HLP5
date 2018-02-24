# Documentation
This zip file contains code that accepts as input an assembler line with instructions for LDR, STR, LDRB and STRB, then parses and executes it in a similar manner that [VisUAL] does. Any differences from VisUAL will be documented here.

## Specifications
<!-- |Syntax                                                         |Description
|---------------------------------------------------------------|--
|`LDR{B}{cond} RDest, [Rsrc]`, no offset                        |
|`LDR{B}{cond} RDest, [RSrc, OFFSET]`, offset addressing        |
|`LDR{B}{cond} RDest, [RSrc, OFFSET]!`, pre-indexed addressing  |
|`LDR{B}{cond} RDest, [RSrc], OFFSET`, post-indexed addressing  | -->

|Opcode     |Instructions implemented                                                                                                                                                                                                                   |Description
|-----------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------|
|LDR        |`LDR{cond} RDest, [Rsrc]`, no offset<br>`LDR{cond} RDest, [RSrc, OFFSET]`, offset addressing<br>`LDR{cond} RDest, [RSrc, OFFSET]!`, pre-indexed addressing<br>`LDR{cond} RDest, [RSrc], OFFSET`, post-indexed addressing       |Loads the **word** stored in the memory address in `RSrc` into register `RDest`. In offset addressing, the word is loaded from the address in `RSrc` added with the offset value. In pre-indexed addressing, the value stored in `RSrc` is first added with the offset before normal `LDR` operation commences. In post-indexed addressing, the value stored in `RSrc` is added with the offset after normal `LDR` operation has commenced.<br>`OFFSET` can be stated as a positive literal (in decimal) or stored in a register.<br>All effective addresses must be a multiple of four.   |
|LDRB       |`LDRB{cond} RDest, [Rsrc]`, no offset<br>`LDRB{cond} RDest, [RSrc, OFFSET]`, offset addressing<br>`LDRB{cond} RDest, [RSrc, OFFSET]!`, pre-indexed addressing<br>`LDRB{cond} RDest, [RSrc], OFFSET`, post-indexed addressing       |Loads the **byte** stored in the memory address in `RSrc` into register `RDest`, with the most significant 24 bits set to zero.<br>All offset properties are the same as implemented in `LDR`.<br>No restrictions to effective addresses.                  |
