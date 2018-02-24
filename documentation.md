# Documentation
This zip file contains code that accepts as input an assembler line with instructions for LDR, STR, LDRB and STRB, then parses and executes it in a similar manner that [VisUAL] does. Any differences from VisUAL will be documented here.

## Specifications

|Opcode     |Name                                                                                 |Instructions implemented        |
|-----------|-------------------------------------------------------------------------------------|--------------------------------|
|LDR        |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`LDR{cond} RDest, [RSrc]`<br>`LDR{cond} RDest, [RSrc, OFFSET]`<br>`LDR{cond} RDest, [RSrc, OFFSET]!`<br>`LDR{cond} RDest, [RSrc], OFFSET`            |
|STR        |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`STR{cond} RSrc, [RDest]`<br>`STR{cond} RSrc, [RDest, OFFSET]`<br>`STR{cond} RSrc, [RDest, OFFSET]!`<br>`STR{cond} RSrc, [RDest], OFFSET`            |
|LDRB       |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`LDRB{cond} RDest, [RSrc]`<br>`LDRB{cond} RDest, [RSrc, OFFSET]`<br>`LDRB{cond} RDest, [RSrc, OFFSET]!`<br>`LDRB{cond} RDest, [RSrc], OFFSET`    |
|STRB       |No offset<br>Offset addressing<br>Pre-indexed addressing<br>Post-indexed addressing  |`STRB{cond} RSrc, [RDest]`<br>`STRB{cond} RSrc, [RDest, OFFSET]`<br>`STRB{cond} RSrc, [RDest, OFFSET]!`<br>`STRB{cond} RSrc, [RDest], OFFSET`    |

### LDR
#### Instructions implemented
`LDR{cond} RDest, [RSrc]`, no offset  
`LDR{cond} RDest, [RSrc, OFFSET]`, offset addressing  
`LDR{cond} RDest, [RSrc, OFFSET]!`, pre-indexed addressing  
`LDR{cond} RDest, [RSrc], OFFSET`, post-indexed addressing   

#### Description  
Loads the **word** stored in the memory address in `RSrc` into register `RDest`.  
  
In offset addressing, the word is loaded from the address in `RSrc` added with the offset value. In pre-indexed addressing, the value stored in `RSrc` is first added with the offset before normal `LDR` operation commences. In post-indexed addressing, the value stored in `RSrc` is added with the offset after normal `LDR` operation has commenced.  
  
`OFFSET` can be stated as a positive literal (in decimal) or stored in a register.  
  
All effective addresses must be a multiple of four.

### LDRB
#### Instructions implemented    
`LDRB{cond} RDest, [RSrc]`, no offset  
`LDRB{cond} RDest, [RSrc, OFFSET]`, offset addressing  
`LDRB{cond} RDest, [RSrc, OFFSET]!`, pre-indexed addressing  
`LDRB{cond} RDest, [RSrc], OFFSET`, post-indexed addressing 

#### Description
Loads the **byte** stored in the memory address in `RSrc` into register `RDest`, with the most significant 24 bits set to zero.  

All offset properties are the same as implemented in LDR.  

No restrictions to effective addresses.  

### STR
#### Instructions implemented
`STR{cond} RSrc, [RDest]`, no offset  
`STR{cond} RSrc, [RDest, OFFSET]`, offset addressing  
`STR{cond} RSrc, [RDest, OFFSET]!`, pre-indexed addressing  
`STR{cond} RSrc, [RDest], OFFSET`, post-indexed addressing 

#### Description
Stores the **word** from register `RSrc` into the memory address in `RDest`

All offset properties are the same as implemented in LDR.

All effective addresses must be a multiple of four.

### STRB
#### Instructions implemented
`STRB{cond} RSrc, [RDest]`, no offset  
`STRB{cond} RSrc, [RDest, OFFSET]`, offset addressing  
`STRB{cond} RSrc, [RDest, OFFSET]!`, pre-indexed addressing  
`STRB{cond} RSrc, [RDest], OFFSET`, post-indexed addressing 

#### Description
Stores the least significant 8 bits of the value in `RSrc` into the memory address in `RDest`  

All offset properties are the same as implemented in LDR.  

No restrictions to effective addresses.  