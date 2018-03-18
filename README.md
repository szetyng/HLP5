# High Level Programming 2018 Group 5
#### Authors
Low Zuo Kai Nicholas, Sze Tyng Lee, Jee Yong Park

#### Purpose
This project supports of a subset of ARM UAL instructions with documentation for specific modules in the `\VisualWork\Documentation` folder.

## Implemented modules:
- Shift
- SingleR
- MultiR

The listed modules have been implemented and tested to simulate the Data processing and Memory instructions. This project uses the [VisUAL](https://salmanarif.bitbucket.io/visual/index.html) program and a VisualInterface framework for testing. 

## Testing
Individual module testing uses a common VisualInterface and Expecto framework, which makes it easy to understand, refactor, and develop tests for existing and new modules. 
Top level testing aims to ...

## Top Level functionality
Explain how the code integrates the existing modules, how it does multi-pass to read a program.

## Usage
An example script to use the modules written is `test.fsx`. The desired ARM program is written in the file `input.txt`. The script file `test.fsx` runs the instructions on some initialized values of registers and memory, and the results are stored in the file `output.txt`. The default initial state of the registers and memory can be changed in the script file `test.fsx`. 

## Future integration of modules
Due to the simple dependencies between modules, it is easy to create and integrate new modules. Any new module `NewModule` needs to have two interfaces:
- `parse: LineData -> Result<Parse<NewModule.Instr>,string> option`
- `execute: NewModule.Instr -> DataPath<'INS> -> Result<DataPath<'INS>,string>`

The new module will have to be included in `CommonTop` as a new `Instr` type, with it's `parse` and `execute` functions included using a match statement.

## Improvements to be made for existing modules
- Only half of the LDM/STM operations have been implemented
- Use more complex match instead of if then else
- More use of D.U.s as types instead of strings
