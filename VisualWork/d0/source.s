MOV R0, #0xa
MOV R2, #0x1000
STR R0, [R2]
MOV R0, #0x14
MOV R2, #0x4
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x1e
MOV R2, #0x8
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x28
MOV R2, #0xc
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x32
MOV R2, #0x10
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x3c
MOV R2, #0x14
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x46
MOV R2, #0x18
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x50
MOV R2, #0x1c
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x400000
ADD R0, R0, #0x44000000
MOV R2, #0x20
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x64
MOV R2, #0x24
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x6e
MOV R2, #0x28
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x78
MOV R2, #0x2c
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0x8c
MOV R2, #0x30
ADD R2, R2, #0x1000
STR R0, [R2]
MOV R0, #0
ADDS R0, R0, R0
MOVS R0, #1
MOV R0, #0x4
MOV R1, #0x4
ADD R1, R1, #0x1000
MOV R2, #0x0
MOV R3, #0xc
ADD R3, R3, #0x1000
MOV R4, #0x28
MOV R5, #0x1000
MOV R6, #0x3c
MOV R7, #0x30
ADD R7, R7, #0x1000
MOV R8, #0x50
MOV R9, #0x24
ADD R9, R9, #0x1000
MOV R10, #0x64
MOV R11, #0x1c
ADD R11, R11, #0x1000
MOV R12, #0x78
MOV R13, #0x20
ADD R13, R13, #0x1000
MOV R14, #0x400000
ADD R14, R14, #0x44000000


STRB R14, [R1, #0xA]
MOV R13, #0x1000
LDMIA R13, {R0-R12}
