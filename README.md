# High Level Programming 2018 Group 5
#### Authors
Low Zuo Kai Nicholas, Sze Tyng Lee, Jee Yong Park

#### Purpose
This project supports of a subset of ARM UAL instructions written as individual modules in the `Modules` folder. Testing and documentation for specific modules can be found in the `Testing` and `Documentation` folders respectively.

## Implemented modules:
### Data Processing
- Shift
### Memory
- SingleR
- MultiR

The listed modules have been implemented and tested to simulate the Data processing and Memory instructions. This project uses the [VisUAL](https://salmanarif.bitbucket.io/visual/index.html) program and a `VisualInterface` framework for testing. 

## Top-Level 
### Creation and Integration of future modules
Due to the simple dependencies between modules, it is easy to create and integrate new modules. Any new module `NewModule` needs to have two interfaces:
- `parse: LineData -> Result<Parse<NewModule.Instr>,string> option`
- `execute: NewModule.Instr -> DataPath<'INS> -> Result<DataPath<'INS>,string>`

The new module will require a type record `Instr` to be defined and included in `CommonTop` as D.U. Also, its `parse` and `execute` functions are to be included using a match statement in functions `IMatch` and `IExecute`. Its opcodes need to be included at the beginning of the top level module. The opcodes are used in the multipass parser when checking for labels in the lines.

### Multipass Functionality
The individual modules have been parsing and executing instruction lines one at a time; they do not accept multiple lines of instructions as an input. In order to allow that, the functions `fullExecute` and `multiParseLine` have been added in the `CommonTop` module. The original `parseLine`, which accepts a single string of assembler line and outputs a single parsed output, has been modified to become an interface to `multiParseLine`. This is to avoid renaming all the instances where `parseLine` was called in the individual modules and tests.

The first of the integration functions, `multiParseLine`, does a two pass parsing of the assembler program, which is represented as multiple lines of instructions. It accepts an array of strings as an input, wherein each line of the assembly program is stored as a string in the array. It is called with `SymbolTable` and `WAddr`, which represent the initial symbol table and the memory address of the assembler. Our demo codes are initialising the assembler to start at memory address 0 with an empty symbol table. 
 
The inner subfunction, `firstPass`, is sent one line of the program at at time, with a `LineData` type threaded through to keep track of the symbol table and the memory address of the instructions. For each line, it updates the symbol table with the label, if it exists. The function creates a `LineData` for the current line, and updates the threaded `LineData` to increase the memory address and reflect any addition to the symbol table. After all the lines have been processed by `firstPass`, the final result is a list of `LineData` for each line of the assembler program, and a `finalLineData` which stores the complete symbol table. All the symbol tables in the list of `LineData` are updated to this version of the table.

Another inner subfunction, `secondPass`, receives one `LineData` at a time and sends it to its relevant module's `parse` function. The result is a list of parsed outputs for each line of the program.

`fullExecute` is the interface which runs `IExecute` with the parsed lines consecutively on the `DataPath` given. The output is the resultant `DataPath` after executing all the instructions in the program. 

## Testing
Individual module testing uses a common modules `VisualInterface`, `CommonTest` and the Expecto framework, which makes it easy to understand, refactor and develop tests for existing and new modules. Top level testing aims to ensure that valid programs involving instructions from multiple modules execute correctly. This is done in the test module `Integration.fs`. Due to the functional nature of the code, individual instructions have well-defined input and outputs, thus integrating multiple modules and simulating multi-line programs can be naturally extended from the work done in individual modules.  

The project currently does not contain instructions which involve forward references, so the robustness of the multipass assembler can be checked by calling `multiParseLine`. The labelling and addressing of each line should be shown clearly in the result of this function. `multiParseLine` currently returns both the result and the symbol table, for further inspection. The symbol table can be removed after integration with modules that contain forward references.

## Usage
An example script to use the modules written is `test.fsx`. The desired ARM program is written in the file `input.txt`. The script file `test.fsx` runs the instructions on some initialized values of registers and memory, and the results are stored in the file `output.txt`. The default initial state of the registers and memory can be changed in the script file `test.fsx`. 

The assembler is case-insensitive to inputs. To execute an assembler program, call `CommonTop.fullExecute` with the initial `DataPath` and an array of strings to represent multiple lines of instructions. If you only want to parse the program, call `CommonTop.multiParseLine` with the array of strings, initial symbol table and initial memory address.


## Improvements to be made for existing modules
### MultiR 
- Only half of the LDM/STM operations have been implemented
- Use more complex match instead of if then else
- More use of D.U.s as types instead of strings

### SingleR
- The instructions currently only accepts register and literal offsets, VisUAL accepts numerical expressions and shifted registers, too

### Top level
- The current multipass assembler gives each line a 4 bytes address, because that is what is required by all the instructions included here. Further implementation of other instructions, such as `FIll` or `DCD`, would require changes to the `firstPass` function in `multiParseLine`
- The current exeuction of multi-line programs returns the last error encountered in the program, and does not distinguish between parse errors or execution errors. Improvements can be made in the form of an error combinator, which identifies the exact line of the program where the error occurs, and whether or not it is a parse or execute error.
