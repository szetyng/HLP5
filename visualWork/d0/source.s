MOV R0, #0
ADDS R0, R0, R0
MOVS R0, #1
MOV R0, #0x1
MOV R1, #0x47
MOV R2, #0x58
MOV R3, #0xc
MOV R4, #0x2d
MOV R5, #0xf7
ADD R5, R5, #0xff00
ADD R5, R5, #0xff0000
ADD R5, R5, #0xff000000
MOV R6, #0x21
MOV R7, #0x46
MOV R8, #0x5d
MOV R9, #0xd4
ADD R9, R9, #0xff00
ADD R9, R9, #0xff0000
ADD R9, R9, #0xff000000
MOV R10, #0xb8
ADD R10, R10, #0xff00
ADD R10, R10, #0xff0000
ADD R10, R10, #0xff000000
MOV R11, #0xf
MOV R12, #0xb7
ADD R12, R12, #0xff00
ADD R12, R12, #0xff0000
ADD R12, R12, #0xff000000
MOV R13, #0x9
MOV R14, #0xc6
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
