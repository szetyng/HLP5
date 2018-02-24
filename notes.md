# Notes
Parsing and executing `LDR{B}` and `STR{B}` instructions.

## Specifications 
Note: Exteremely repetitive, for own use when writing code to avoid confusion. 

### Summary
|Code                               |Offset type        |Notes     |
|-----------------------------------|:-----------------:|----------|
|`LDR RDest, [RSrc]`                |None               |Get address stored in `RSrc`, get value from that address, **load** value into `RDest` |
|`LDR RDest, [RSrc, #4]`            |Normal             |Get _effective address_ by adding offset to address in `RSrc`    |
|`LDR RDest, [Rsrc, ROffset]!`      |Pre-indexed        |Get _effective address_ by adding offset to address in `RSrc`, and also _update_ `RSrc` to this new effective address  |
|`LDR RDest, [Rsrc], ROffset!`      |Post-indexed       |_Update_ address in `RSrc` by adding offset value to it, after loading value from original `RSrc` address    |
|`STR RSrc, [RDest]`                |None               |Get address stored in `RDest`, get value from `RSrc`, **store** value into that address    |
|`STR RSrc, [RDest, ROffset]`       |Normal             |Get _effective address_ by adding offset to address in `RDest`   |
|`STR RSrc, [RDest, #4]!`           |Pre-indexed        |Get _effective address_ by adding offset to address in `RDest`, and also _update_ `RDest` to this new effective address |
|`STR RSrc, [RDest], #4`            |Post-indexed       |_Update address_ in `RDest` by adding offset value to it, after storing value to original `RDest` address    |
  

|Code                               |Offset type        |Notes     |
|-----------------------------------|:-----------------:|----------|
|`LDRB RDest, [RSrc{, ROffset}]`    |All                |Get address/effective address stored in `RSrc`, get value from that address. **Set `RDest` to zero**, then **load** value into `RDest`    |
|`STRB RSrc, [RDest{, #2}]`         |All                |Get address/effective address stored in `RDest`. Get **LS 8-bits of the value stored in `RSrc`** (modulo 256), then **store** that value into the address/effective address    |


### LDR
#### No offset
`LDR RDest, [RSrc]`  
e.g. `LDR R0, [R1]`
1. Get address stored in R1
2. Get value stored in that address -- _must be divisible by 4_
3. **Load** that value into R0

#### Normal offset
`LDR RDest, [RSrc, #4]` or `LDR RDest, [Rsrc, ROffset]`  
e.g. `LDR R0, [R1, #4]` or `LDR R0, [R1, R2]`
1. Get address stored in R1
2. Get _effective address_ by adding offset, which is either a literal or a value stored in register R2 -- _must be divisible by 4_; _this doesn't change the value in R1_
3. Get value stored in the effective address
4. **Load** that value into R0

#### Pre-indexing offset
`LDR RDest, [RSrc, #4]!` or `LDR RDest, [Rsrc, ROffset]!`  
e.g. `LDR R0, [R1, #4]!` or `LDR R0, [R1, R2]!`
1. Get address stored in R1
2. Get _effective address_ by adding offset, which is either a literal or a value stored in register R2 -- _must be divisible by 4_; _this updates the value in R1 as well_
3. Get value stored in the effective address
4. **Load** that value into R0 -- _remember that R1 has been updated, too_

#### Post-indexing offset
`LDR RDest, [RSrc], #4` or `LDR RDest, [Rsrc], ROffset!`  
e.g. `LDR R0, [R1], #4` or `LDR R0, [R1], R2`
1. Get address stored in R1
2. Get value stored in that address -- _must be divisible by 4_
3. **Load** that value into R0
4. _Update value in R1_ by adding offset, which is either a literal or a value stored in register R2

### LDRB
#### No offset
`LDRB RDest, [RSrc]`  
e.g. `LDRB R0, [R1]`
1. Get address stored in R1 -- _address does not have to be divisible by 4_
2. Get value stored in that address
3. Set R0 to zero, then **load** value into R0 -- because value from the address is only 1-byte/8-bits longs, the MS bits are to be set to zero

#### Normal offset
`LDRB RDest, [RSrc, #4]` or `LDRB RDest, [Rsrc, ROffset]`  
e.g. `LDRB R0, [R1, #4]` or `LDRB R0, [R1, R2]`
1. Get address stored in R1
2. Get _effective address_ by adding offset, which is either a literal or a value stored in register R2 -- _this doesn't change the value in R1_
3. Get value stored in the effective address
4. Set R0 to zero, then **load** value into R0

#### Pre-indexing offset
`LDRB RDest, [RSrc, #4]!` or `LDRB RDest, [Rsrc, ROffset]!`  
e.g. `LDRB R0, [R1, #4]!` or `LDRB R0, [R1, R2]!`
1. Get address stored in R1
2. Get _effective address_ by adding offset, which is either a literal or a value stored in register R2 -- _this updates the value in R1 as well_
3. Get value stored in the effective address
4. Set R0 to zero, then **load** value into R0 -- _remember that R1 has been updated, too_

#### Post-indexing offset
`LDRB RDest, [RSrc], #4` or `LDRB RDest, [Rsrc], ROffset!`  
e.g. `LDRB R0, [R1], #4` or `LDRB R0, [R1], R2`
1. Get address stored in R1
2. Get value stored in that address 
3. Set R0 to zero, then **load** value into R0
4. _Update value in R1_ by adding offset, which is either a literal or a value stored in register R2

### STR
#### No offset  
`STR RSrc, [RDest]`  
e.g. `STR R0, [R1]`
1. Get address stored in R1 --_must be divisible by 4_
2. Get value stored in R0
3. **Store** that value into the address found in step 1

#### Normal offset
`STR RSrc, [RDest, #4]` or `STR RSrc, [RDest, ROffset]`  
e.g. `STR R0, [R1, #4]` or `STR R0, [R1, R2]`
1. Get address stored in R1
2. Get _effective address_ by adding offset, which is either a literal or a value stored in register R2 -- _must be divisible by 4_; _this doesn't change the value in R1_
3. Get value stored in R0
4. **Store** that value into the effective address

#### Pre-indexing offset
`STR RSrc, [RDest, #4]!` or `STR RSrc, [RDest, ROffset]!`  
e.g. `STR R0, [R1, #4]!` or `STR R0, [R1, R2]!`
1. Get address stored in R1
2. Get _effective address_ by adding offset, which is either a literal or a value stored in register R2 -- _must be divisible by 4_; _this updates the value in R1 as well_
3. Get value stored in R0
4. **Store** that value into the effective address -- _remember that R1 has been updated, too_

#### Post-indexing offset
`STR RSrc, [RDest], #4` or `STR RSrc, [RDest], ROffset!`  
e.g. `STR R0, [R1], #4` or `STR R0, [R1], R2`
1. Get address stored in R1 -- _must be divisible by 4_
2. Get value stored in R0
3. **Store** that value into the address found in step 1
4. _Update value in R1_ by adding offset, which is either a literal or a value stored in register R2

### STRB
#### No offset
`STRB RSrc, [RDest]`  
e.g. `STRB R0, [R1]`
1. Get address stored in R1 
2. Get only the LS 8 bits of the value stored in R0, by taking modulo 256
3. **Store** that value into the address found in step 1

#### Normal offset
`STRB RSrc, [RDest, #4]` or `STRB RSrc, [RDest, ROffset]`  
e.g. `STRB R0, [R1, #4]` or `STRB R0, [R1, R2]`
1. Get address stored in R1
2. Get _effective address_ by adding offset, which is either a literal or a value stored in register R2 -- _this doesn't change the value in R1_
3. Get only the LS 8 bits of the value stored in R0, by taking modulo 256
4. **Store** that value into the effective address

#### Pre-indexing offset
`STRB RSrc, [RDest, #4]!` or `STRB RSrc, [RDest, ROffset]!`  
e.g. `STRB R0, [R1, #4]!` or `STRB R0, [R1, R2]!`
1. Get address stored in R1
2. Get _effective address_ by adding offset, which is either a literal or a value stored in register R2 -- _this updates the value in R1 as well_
3. Get only the LS 8 bits of the value stored in R0, by taking modulo 256
4. **Store** that value into the effective address -- _remember that R1 has been updated, too_

#### Post-indexing offset
`STRB RSrc, [RDest], #4` or `STRB RSrc, [RDest], ROffset!`  
e.g. `STRB R0, [R1], #4` or `STRB R0, [R1], R2`
1. Get address stored in R1 
2. Get only the LS 8 bits of the value stored in R0, by taking modulo 256
3. **Store** that value into the address found in step 1
4. _Update value in R1_ by adding offset, which is either a literal or a value stored in register R2

### Notes
1. **Load** updates values in registers -- _load from memory into register_
2. **Store** updates values in machine memory -- _store from registers into memory_
3. For LDR/STR, all _memory addresses accessed_ are to be divisible by 4, because the memory addresses are all 32-bit/4-bytes each. However, in the case of post-indexed offsets, it is fine to update the values in the registers to an address indivisible by 4. No such restriction in LDRB/STRB.


