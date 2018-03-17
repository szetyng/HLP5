////////////////////////////////////////////////////////////////////////////////////
//      Code defined at top level after the instruction processing modules
////////////////////////////////////////////////////////////////////////////////////

module CommonTop

open CommonLex
open CommonData

/// allows different modules to return different instruction types
type Instr =
    | IMEM of MultiR.Instr
    | IDP of Shift.Instr


/// Note that Instr in Mem and DP modules is NOT same as Instr in this module
/// Instr here is all possible isntruction values combines with a D.U.
/// that tags the Instruction class
/// Similarly ErrInstr
/// Similarly IMatch here is combination of module IMatches
let IMatch (ld: LineData) : Result<Parse<Instr>,string> option =
    let pConv fr fe p = pResultInstrMap fr fe p |> Some
    match ld with
    | MultiR.IMatch pa -> pConv IMEM string pa
    | Shift.IMatch pa -> pConv IDP string pa
    | _ -> None

let IExecute (i:Instr) (d:DataPath<'INS>):Result<DataPath<'INS>,string> =
    match i with
    | IDP x -> Shift.execute x d
    | IMEM x -> MultiR.execute x d


type CondInstr = Condition * Instr

let parseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmLine:string) =
    /// put parameters into a LineData record
    let makeLineData opcode operands = {
        OpCode=opcode
        Operands=String.concat "" operands
        Label=None
        LoadAddr = loadAddr
        SymTab = symtab
    }
    /// remove comments from string
    let removeComment (txt:string) =
        txt.Split(';')
        |> function 
            | [|x|] -> x 
            | [||] -> "" 
            | lineWithComment -> lineWithComment.[0]
    /// split line on whitespace into an array
    let splitIntoWords ( line:string ) =
        line.Split( ([||] : char array), 
            System.StringSplitOptions.RemoveEmptyEntries)
    /// try to parse 1st word, or 2nd word, as opcode
    /// If 2nd word is opcode 1st word must be label
    let matchLine words =
        let pNoLabel =
            match words with
            | opc :: operands -> 
                makeLineData opc operands 
                |> IMatch
            | _ -> None
        
        match pNoLabel, words with
        | Some pa, _ -> pa
        | None, label :: opc :: operands -> 
            match { makeLineData opc operands 
                    with Label=Some label} 
                  |> IMatch with
            | None -> 
                Error (sprintf "Unimplemented instruction %s" opc)
            | Some pa -> pa
        | _ -> Error (sprintf "Unimplemented instruction %A" words)
    asmLine
    |> removeComment
    |> splitIntoWords
    |> Array.toList
    |> matchLine
