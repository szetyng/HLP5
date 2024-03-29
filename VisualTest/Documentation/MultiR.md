# MultiR Module
## Specification
<center>

Name | OpCode | Syntax 
| :---:| :---: | :---: |
|Load Multiple Registers| LDM | LDM{dir} Rn{!}, [reglist]
|Store Multiple Registers| STM | STM{dir} Rn{!}, [reglist]
</center>

where:

- ```{...}``` denote optional fields
- ```dir``` specifies stack direction or equivalently address mode. It can be either ```IA```,```DB```,```FD``` or ```EA```. If unspecified, defaults to `IA`.
- ```Rn``` specifies register on which the memory addresses are based.
- ```!``` is an optional writeback suffix. If specified, the final address is written back to ```Rn```.
- ```reglist``` is a list of one or more registers to be loaded or stored, enclosed in ```{...}```. 

This specification follows the [ARM documentation](http://infocenter.arm.com/help/index.jsp?topic=/com.arm.doc.dui0552a/BABCAEDD.html) closely. 

Note that the `dir` values of `IB`, `DA`, `ED`, `FA` have yet to be implemented. 

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

## Interfaces and Usage
The relevance of the MultiR Module to group effort can be seen through the type signatures of two interfaces to the ```MultiR``` module:
- ```parse: LineData -> Result<Parse<MultiR.Instr>,string> option```
- ```execute: MultiR.Instr -> DataPath<'INS> -> Result<DataPath<'INS>,string>``` 

