MOV R0, #0
ADDS R0, R0, R0
MOVS R0, #1
MOV R0, #0xb
MOV R1, #0x58
MOV R2, #0xfd
ADD R2, R2, #0xff00
ADD R2, R2, #0xff0000
ADD R2, R2, #0xff000000
MOV R3, #0x4
MOV R4, #0x17
MOV R5, #0xae
ADD R5, R5, #0xff00
ADD R5, R5, #0xff0000
ADD R5, R5, #0xff000000
MOV R6, #0xc6
ADD R6, R6, #0xff00
ADD R6, R6, #0xff0000
ADD R6, R6, #0xff000000
MOV R7, #0x15
MOV R8, #0xfa
ADD R8, R8, #0xff00
ADD R8, R8, #0xff0000
ADD R8, R8, #0xff000000
MOV R9, #0x1d
MOV R10, #0x3f
MOV R11, #0x36
MOV R12, #0x3a
MOV R13, #0xd4
ADD R13, R13, #0xff00
ADD R13, R13, #0xff0000
ADD R13, R13, #0xff000000
MOV R14, #0xf7
ADD R14, R14, #0xff00
ADD R14, R14, #0xff0000
ADD R14, R14, #0xff000000


RORS R0,R2,R1
MOV R13, #0x1000
LDMIA R13, {R0-R12}
MOV R0, #0
              ADDMI R0, R0, #8
              ADDEQ R0, R0, #4
              ADDCS R0, R0, #2
              ADDVS R0, R0, #1
