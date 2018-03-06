# High Level Programming 2018 Individual 
#### Author
Low Zuo Kai Nicholas
### Purpose
This project supports of a subset of ARM UAL instructions detailed in [Specification](#specification). 
Two modules, [Shift](#shift-module) and [MultiR](#multir-module) have been implemented and tested to simulate the Data processing and Memory instructions respectively. This project uses the [VisUAL](https://salmanarif.bitbucket.io/visual/index.html) program for testing, and their differences are highlighted throughout the document. How this project contributes to the group deliverable is also described in the module documetation.

## Content Table
- [Specification](#specification)
- [Shift Module](#shift-module)
- [MultiR Module](#memory-module)

    
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

# MultiR Module
## Implementation
### Parse
The ```parse``` function uses regular expression patterns to match the syntax described in specification. On top of that, it has to ensure that none of the restrictions were violated, and ```reglist``` is in the appropriate format. Errors are passed in a monadic fashion due to the many invalid cases. Refer to code and test plan for details. 

### Execute
The memory address range depends on the type of opcode and suffix used. An active pattern is used to simplify the matching as there are only two distinct cases.

| OpCode | Equivalent Suffix | Memory address range | Writeback Value 
| :---:|:---:|:---:|:---:|
| LDM | IA <br> FD <br> None |Rn to Rn + 4 * (n-1)| Rn + 4 * n
| STM |IA <br> EA <br> None | Rn to Rn + 4 * (n-1) |  Rn + 4 * n
| STM | DB <br> FD | Rn - 4 * n to Rn - 4 | Rn - 4 * n
| LDM | DB <br> EA | Rn - 4 * n to Rn - 4  | Rn - 4 * n

## Test Plan
### Parse Tests
| Test Description | Test status
| :---:|:---:|
| Basic syntax  |  Passed
| Additional ```reglist``` syntax | Passed
| Restrictions on ```reglist``` | Passed

### Execute Tests
#### Error tests
| Test Description | Test status
| :---:|:---:|
| Error if address not found in memory for LDM |  Passed
| Error if memory address is not divisible by 4 | Passed
| Error if memory address for STM is below ```0x1000u``` | Passed

#### Functionality tests
| Test Description | Test status
| :---:|:---:|
| LDM, STM Tests: Check memory and register values equivalence with VisUAL|  Passed

```Rn``` is chosen to have values ```0x1000u``` or ```0x1018u``` depending on ```dir```. ```reglist``` is chosen to be ```{R4-R9}```. The values used for ```Rn``` and ```reglist``` are chosen to allow for valid memory addresses to be accessed. Hence, 6 consecutive words starting from ```Rn``` are read from memory using the provided testing framework. To use the testing framework, the function ```STOREALLLMEM``` generates assembler instructions that can store values into memory. 

The memory for ```STM``` tests is empty initally, but has 6 values starting from ```0x1000u``` for ```LDM``` tests. Both tests are initialized using the same register values. All equivalent variants of the instructions were tested and verified to be equivalent to VisUAL.

## Differences in VisUAL
As the testing is done in a restricted fashion, not many differences were observed. This module will, however, differ if consecutive instructions are used. For example, executing two ```STMIA``` instructions using the ```MultiR``` module with disjoint ```Rn``` will result in an invalid memory map with non-continuous keys. In VisUAL, the second instruction seems to be ignored. More testing in this regard should be considered.  
## Contribution to group deliverable
### Interfaces and Usage
The type signatures of the two interfaces for ```MultiR``` module is the same as the ```Shift``` module. Similar arguments made in ```Shift``` module applies here.  
