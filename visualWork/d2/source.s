MOV R0, #0x1f
ADD R0, R0, #0xcb00
ADD R0, R0, #0x510000
ADD R0, R0, #0xd7000000
MOV R2, #0x1000
STR R0, [R2]
MOV R0, #0x82
ADD R0, R0, #0x5400
ADD R0, R0, #0x160000
ADD R0, R0, #0xab000000
MOV R2, #0x4
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x9b
ADD R0, R0, #0xde00
ADD R0, R0, #0x580000
ADD R0, R0, #0x4000000
MOV R2, #0x8
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0xab
ADD R0, R0, #0xee00
ADD R0, R0, #0x1c0000
ADD R0, R0, #0x54000000
MOV R2, #0xc
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x80
ADD R0, R0, #0x4000
ADD R0, R0, #0x100000
ADD R0, R0, #0x9000000
MOV R2, #0x10
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0xdc
ADD R0, R0, #0x8a00
ADD R0, R0, #0x430000
ADD R0, R0, #0x44000000
MOV R2, #0x14
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0xf0
ADD R0, R0, #0x3000
ADD R0, R0, #0x400000
ADD R0, R0, #0x44000000
MOV R2, #0x18
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x21
ADD R0, R0, #0xea00
ADD R0, R0, #0xff000000
MOV R2, #0x1c
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x400000
ADD R0, R0, #0x44000000
MOV R2, #0x20
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0xf0
ADD R0, R0, #0x800
ADD R0, R0, #0x4c0000
ADD R0, R0, #0x5000000
MOV R2, #0x24
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x48
ADD R0, R0, #0xef00
ADD R0, R0, #0xcd0000
ADD R0, R0, #0xab000000
MOV R2, #0x28
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0xab
ADD R0, R0, #0xec00
ADD R0, R0, #0x1c0000
ADD R0, R0, #0x89000000
MOV R2, #0x2c
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0xed
ADD R0, R0, #0x2000
ADD R0, R0, #0x820000
ADD R0, R0, #0x77000000
MOV R2, #0x30
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0
ADDS R0, R0, R0
MOVS R0, #1
MOV R0, #0x1
MOV R1, #0x4
MOV R2, #0x8
MOV R3, #0x3
ADD R3, R3, #0x1000
MOV R4, #0x80
ADD R4, R4, #0x4000
ADD R4, R4, #0x100000
ADD R4, R4, #0x9000000
MOV R5, #0x1000
MOV R6, #0x82
ADD R6, R6, #0x5400
ADD R6, R6, #0x160000
ADD R6, R6, #0xab000000
MOV R7, #0x10
ADD R7, R7, #0x1000
MOV R8, #0x9b
ADD R8, R8, #0xde00
ADD R8, R8, #0x580000
ADD R8, R8, #0x4000000
MOV R9, #0x20
ADD R9, R9, #0x1000
MOV R10, #0xab
ADD R10, R10, #0xee00
ADD R10, R10, #0x1c0000
ADD R10, R10, #0x54000000
MOV R11, #0x24
ADD R11, R11, #0x1000
MOV R12, #0x1f
ADD R12, R12, #0xcb00
ADD R12, R12, #0x510000
ADD R12, R12, #0xd7000000
MOV R13, #0x2c
ADD R13, R13, #0x1000
MOV R14, #0xdc
ADD R14, R14, #0x8a00
ADD R14, R14, #0x430000
ADD R14, R14, #0x44000000


STRB R8, [R9], #0xA9
MOV R13, #0x1000
LDMIA R13, {R0-R12}
