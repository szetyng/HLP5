# ALU Instruction Module

Jee Yong Park

This project includes a module `Arithmetic` later to be used in the `CommonTop` module, in which `Arithmetic.IMatch` and `Arithmetic.execute` functions can be respectively used to parse and execute a given instruction line with opcodes that implement arithmetic logic. The codes from this module can be used in the later stages of the project, with relatively small modifications, for modules that implement move, bitwise boolean, and compare instructions as they all share similar input formats, especially with `op2`.

## Specification

|Opcode|Description|Instructions
|---|---|---|
|ADD|Add|`ADD{S}{cond} dest, op1, op2 {,SHIFT_op, #expression}`|
|ADC|Add with carry|`ADC{S}{cond} dest, op1, op2 {,SHIFT_op, #expression}`|
|SUB|Subtract|`SUB{S}{cond} dest, op1, op2 {,SHIFT_op, #expression}`|
|SBC|Subtract with carry|`SBC{S}{cond} dest, op1, op2 {,SHIFT_op, #expression}`|
|RSB|Reverse subtract|`RSB{S}{cond} dest, op1, op2 {,SHIFT_op, #expression}`|
|RSC|Reverse subtract with carry|`RSC{S}{cond} dest, op1, op2 {,SHIFT_op, #expression}`|

_Table 1: Instructions supported by the Arithmetic module_

Several differences should be noted, compared to what is capable in VisUAL. `Arithmetic.parse` does not recognise lower case input. This is not a problem for the opcode, as it is already partially parsed into `CommonLex.LineData` type at `CommonTop` module. Registers will not be recognised if they are passed on as lower case. It may be beneficial to process instruction line with String.Upper in `CommonTop` to convert all letters to upper case before passing the input string to instruction modules.

### Difference from VisUAL

- Negative literals cannot be parsed. This can easily be fixed by adding another partial active pattern matching dedicated in deciding whether the given negative literal is allowed, which will return the given literal in `uint32` if so.
- Lower case operands cannot be parsed. This can also easily be fixed by implementing `string.Upper`.

## Implementation

First it should be noted that the type `Arithmetic.Instr` has been constructed to suit the instructions of this class.

```fsharp
type OpInstr = ADD | ADC | SUB | SBC | RSB | RSC
type Instr = {
    OpCode: OpInstr
    OpFlag: bool //true if opcode has suffix "S"
    OpCond: Condition
    Dest: RName
    Op1: Operand //FlexOp.Operand
    Op2: Op2 //FlexOp.Op2
    }
```

### Parsing

The `parse` function is largely consisted of two separate subfunctions `parseOpCode`and `parseOperand` which respectively parses the opcode information in the given `LineData` to return a dummy `Instr`, and updates the pipelined dummy `Instr` with parsed operand information.

Parsing the opcode is simple, as all recognised opcodes in the instruction class has already been mapped to `opCodes`. The map only needs to be searched with the given opcode string as the key.

The subfunction `parseOperand`, the given operands string is first broken up into separate word strings with white spaces and commas removed, and then saved into `wordList: string list`. It then parses the destination and op1 registers, and consecutively attempts at parsing op2, where whether op2 has shift or not is determined by match `wordList.Length`. Rest of the strings in `wordList` are then parsed according to the type of op2 decided by subfunction `updateOp2`.

 Note that parsing the operands by detection of the number of words in the operand string instead of using regex means if there is a syntax error regarding the location of commas will not be detected, as long as the operand words of the instruction lines are in correct order and spaced.

### Execution

The operation of `execute` function is much simpler, especially because in principle, there is no error cases to be processed within the function, since any error that originates from the input is to be detected at either top level code or `parse`.

This part is comprised of 6 subfunctions, as described below:

- `readCondition` matches the opcode's execution condition and the flag to decide whether the instruction is to be executed.
- `callOpValues` returns a pair of `uint32` values represented by op1 and op2.
- `arithmetic` takes in the two unsigned 32-bit integers and carries out the arithmetics as per opcode instruction, but returns the result in `uint64`.
- `updateDest` updates the destination register with the returned value.
- `updateFlags` updates the flags if the opcode specifies so (`ins.OpFlag = true`), using the `uint64` values returned from `arithmetic`.
- `updatePC` increments whatever is stored in PC register by `4u`.

## Test Plan

Unit tests have been carried out for different cases and functionalities of the `parse` and `execute` functions, as detailed below.

|Tested features|Details|Remarks
|---|---|---|
|Parse: `op2` without shift| Passed | Parses correctly when given instruction line with `op2` as single register or immediate expression
|Parse: invalid register | Passed | Correct `Error` returned for invalid register names in `Ddst` and `op1`
|Parse: invalid literal | Passed | Correct `Error` returned for unallowed immediate expression for `op2`
|Parse: operand syntax | Passed | Correct `Error` returned for unappropriate number of words in instruction
|Parse: `op2` with shift| Failed | The `parseOperand` function recogises only `RRX` shift. Instructions with other shifts result in `Error` that is returned when processing `RRX` instruction.
|Execute: `PC` update | Passed | `PC` register always increments by `4u`
|Execute: with carry | Passed | Instructions with carry behave as expected regardless of the flags
|Execute: flag updates | Passed | Flags are updated as expected when given `Instr.OpFlag = true` (opcode has suffix "S")
|Execute: with `op2` shift | Passed | Instructions with shifts in `op2` are executed as expected

_Table 2: List of features of parse and execute functions tested, and the results_