# Specification
## Data Processing 
<center>

Name | OpCode | Syntax 
| :---:| :---: | :---: |
|Arithmetic Shift Right | ASR | ASR{S} Rd, Rm, Rs <br> ASR{S} Rd, Rm, #n |
|Logical Shift Right | LSR | LSR{S} Rd, Rm, Rs <br> LSR{S} Rd, Rm, #n | 
|Logical Shift Left| LSL | LSL{S} Rd, Rm, Rs <br> LSL{S} Rd, Rm, #n|
|Rotate Right| ROR | ROR{S} Rd, Rm, Rs <br> ROR{S} Rd, Rm, #n|
|Rotate Right and Extend| RRX | RRX{S} Rd, Rm       |

</center>
where:

- ```{...}``` denote optional fields
- ```S``` is an optional suffix. If specified, the conditon code flags are updated on result of operation.
- ```Rd``` specifies the destintion register
- ```Rm``` specifies the register holding the value to be shifted
- ```Rs``` specifies the register holding the shift length to apply to the value in ```Rm```, restricted to range 0 to 255.
- ```n``` specifies shift length, restricted to range 0 to 255.

This specification follows the [ARM documentation](http://infocenter.arm.com/help/index.jsp?topic=/com.arm.doc.dui0552a/BABJCCDH.html) closely.
## Memory

<center>

Name | OpCode | Syntax 
| :---:| :---: | :---: |
|Load Multiple Registers| LDM | LDM{dir} Rn{!}, [reglist]
|Store Multiple Registers| STM | STM{dir} Rn{!}, [reglist]
</center>
where:

- ```{...}``` denote optional fields
- ```dir``` specifies stack direction or equivalently address mode. It can be either ```IA```,```DB```,```FD``` or ```EA```
- ```Rn``` specifies register on which the memory addresses are based.
- ```!``` is an optional writeback suffix. If specified, the final address is written back to ```Rn```.
- ```reglist``` is a list of one or more registers to be loaded or stored, enclosed in ```{...}```. 

This specification follows the [ARM documentation](http://infocenter.arm.com/help/index.jsp?topic=/com.arm.doc.dui0552a/BABCAEDD.html) closely.

#### Restrictions
- ```Rn``` cannot be ```PC``` (also called ```R15```)
- ```reglist``` must not contain ```SP``` (also called ```R13```)
- In any ```STM``` instruction, ```reglist``` must not contain ```PC```
- In any ```LDM``` instruction, ```reglist``` must not contain ```PC``` if it contains ```LR``` (also called ```R14```)
- ```reglist``` must not contain ```Rn``` if writeback suffix is specified

#### Additional syntax
- ```reglist``` can contain register range (Example: {R1-R4} ={R1,R2,R3,R4})
- ```reglist``` must be comma separated if it contains more than one register or register range
- ```reglist``` cannot be empty
## Differences from VisUAL
- ```dir``` is optional.
- ```{cond}``` specifies the conditon code, to be implemented in the group stage

# Shift Module
## Implementation

### Parse 
The ```parse``` function uses regular expression patterns to match the ```Operands``` field of type ```LineData``` with the syntax described in specification. The regex allows for registers in range of r0-r14, upper and lowercase r, and restricts shift lengths ```n``` and value in ```Rs``` to range 0 to 255. The type ```SVal``` is defined to allow ```RRX``` to be dealt together with the other op codes. The type ```Instr``` is used to store the parsed information. See code for details of type definitions and regex pattern.

### Execute
The ```execute``` function takes the parsed instruction of type ```Instr``` and CPU state of type and performs the appropriate action. Of importance are the shift results and flag update value if ```S``` is specified. The execution function follows the [ARM documentation]((http://infocenter.arm.com/help/index.jsp?topic=/com.arm.doc.dui0552a/CIHFDDHB.html).
) closely. 


The shift value for shift instructions can either be a constant integer [0,255] or a register value casted to an integer type. A shift value of zero results in no shift with no flag changes. Shift values that exceed the valid ranges of [1,31] (e.g. negative integers, larger than 31) are treated similarly as an invalid case, resulting in an error. Flags are only updated if S bit is set and if shift values are non-zero. Valid shift values for register and flag changes for the instructions are shown:

| OpCode | Shift Result | Flag Update Value
| :---:|---|---|
| ASR | Arithmetic shift right n bits, n = [0,31].<br> All bits set to Rm[31] otherwise| Flag updated to Rm[n-1], n = [1,32]. <br>Flag updated to Rm[31] otherwise. 
| LSR | Logical shift right n bits, n = [0,31].<br> Result is zero otherwise | Flag updated to Rm[n-1], n = [1,32]. <br>Flag updated to Rm[31] otherwise.
| LSL |  Logical shift left n bits, n = [0,31].<br> Result is zero otherwise  |Flag updated to Rm[32-n], n = [1,32]. <br>Flag updated to false otherwise.
| ROR | Rotate right by n bits, n = n-32 for n > 32 | Flag updated to Rm[n-1], n = n-32 for n > 32 <br> Flag updated to Rm[31] if n % 32 = 0, n$\neq$ 0
| RRX | Right shift by 1, copies carry flag into Rm[31]       | Flag updated to Rm[0]

### Differences in VisUAL
Note that for ROR with n as a multiple of 32, the shift value has the result of zero, but flags are updated. This differs from the implementation in VisUAL. Also, the value in ```Rs``` is allowed to exceed the range [0-255] without throwing an error in VisUAL.



## Test Plan
The ``parse`` and ``execute`` functions were tested for functionality using the Expecto framework in program ```ShiftTests.fs```. As the ```Shift``` module will be integrated in the group stage, the function ```parseLine``` from module ```CommonTop``` was borrowed to convert an assembly string into ```LineData``` for testing. The function ```makeExecute``` allows for direct comparison of results with the given VisUAL testing framework.
### Parse Tests
| Test Description | Test status
| :---:|:---:|
| Uppercase R and lowercase r |  Passed
| Shift Value in range [0,255] | Passed
| R15 invalid | Passed
| Shift Value out of range [0,255] invalid | Passed

### Execute Tests
| Test| Description |Expected outcome | Instructions tested | Test status
| :---:|---|---|---|---|
| Rd, Rm, Rs | Test for Rd,Rm,Rs having same and different registers | Equivalent to VisUAL | All | Passed
| Zero shift length| Test on random registers and flags| No change to registers and flags | All except RRX,RRXS| Passed
| RRX test | Test on random registers and flags | Equivalent to VisUAL | RRX, RRXS | Passed
| Shift lengths of 1, 31, 32 , 33| Test on positive, negative and random register values, random flags | Equivalent to VisUAL except ROR* | All except RRX, RRXS | Passed
| Random Shift lengths | Use random registers as shift values**| Equivalent to VisUAL | All except RRX, RRXS | Passed

*For ROR with shift value 32. If carry bit is updated, VisUAL sets it to false while the ARM documentation requires it to be set to the MSB of ```Rm```. Tests for ROR are hence expected to fail! This can be verified in VisUAL:
```
MOV      r1, #-1
RORS     r1,r1,#32   
```
**Shift values from registers could be negative integers, hence shift values must be constrained to range 0 < n < 32 during implementation.

*The points marked by asterisk were discovered during testing.*
## Contribution to group deliverable
### Interfaces and Usage

The relevance of the Shift Module to group effort can be seen through the type signatures of two interfaces to the ```Shift``` module:
- ```parse: LineData -> Result<Parse<Shift.Instr>,Shift.ErrInstr> option```
- ```execute: Shift.Instr -> DataPath<Shift.Instr> -> Result<DataPath<Shift.Instr>,ErrRun>``` 

Hence, any top level module can use the ```Shift``` module to parse and execute shift instructions using the two functions and the ```Instr``` type defined. ``` ErrInstr``` and ```ErrRun``` are error types of the module for the parse and execute functions respectively. 

Care has also been taken to use functional abstraction to allow for faster refactoring and simple code, but not to the extent that it causes confusion. For example, it was decided that the subfunctions in ```execute``` such as ```regLSL```,```flagLSL``` should be kept separate for different opCodes for clarity purposes, despite possibilities for funtional abstraction.  