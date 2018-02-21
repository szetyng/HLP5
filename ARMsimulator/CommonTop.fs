////////////////////////////////////////////////////////////////////////////////////
//      Code defined at top level after the instruction processing modules
////////////////////////////////////////////////////////////////////////////////////
module CommonTop 

open CommonLex
open CommonData
open DP
open System.Security.Principal

/// allows different modules to return different instruction types
type Instr =
    | IMEM of Memory.InstrLine //changed?
    | IDP of DP.Instr

/// allows different modules to return different error info
/// by default all return string so this is not needed
type ErrInstr =
    | ERRIMEM of Memory.ErrInstr
    | ERRIDP of DP.ErrInstr
    | ERRTOPLEVEL of string

/// Note that Instr in Mem and DP modules is NOT same as Instr in this module
/// Instr here is all possible isntruction values combines with a D.U.
/// that tags the Instruction class
/// Similarly ErrInstr
/// Similarly IMatch here is combination of module IMatches
let IMatch (ld: LineData) : Result<Parse<Instr>,ErrInstr> option =
    let pConv fr fe p = pResultInstrMap fr fe p |> Some
    match ld with
    | Memory.IMatch pa -> pConv IMEM ERRIMEM pa
    | DP.IMatch pa -> pConv IDP ERRIDP pa
    | _ -> None



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
                Error (ERRTOPLEVEL (sprintf "Unimplemented instruction %s" opc))
            | Some pa -> pa
        | _ -> Error (ERRTOPLEVEL (sprintf "Unimplemented instruction %A" words))
    asmLine
    |> removeComment
    |> splitIntoWords
    |> Array.toList
    |> matchLine



// let memDummyList = 
//     [WA 0x100u, DataLoc 0x2000u ; WA 0x104u, DataLoc 0x202u]

// let seepeeyouData = {
//     Fl = {N=false ; C=false ; Z=false ; V=false} ;
//     Regs = Map.ofList [ 
//             R0,0u ; R1,3u ; R2,0x412u ; R3,0u ; R4,0u ; R5,0u ; R6,0u ; R7,0u;
//             R8,0u ; R9,0u ; R10,0u ; R11,0u ; R12,0u ; R13,0u ; R14,0u ; R15,0u
//         ] ;
//     MM = Map.ofList memDummyList
// }

//let asmLine = "STR R10, [R5]"
//let parsed = parseLine None (WA 0u) asmLine 

let executeAnyInstr (instr:Instr) (d:DataPath<Memory.InstrLine>) = //lazy way out
    let execute d =
        match instr with
        | IMEM ins -> Memory.executeMemInstr ins d
        | IDP _ -> failwithf "not yet implemented"
    execute d  

// extract Instr from Result<Parse<Instr>,errortype>
let execute asmLine d = 
    parseLine None (WA 0u) asmLine
    |> fun p ->
        match p with
        | Ok ({PInstr=ins} as pr) -> executeAnyInstr ins d
        | _ -> failwithf "Idk"

// open VisualTest.VCommon
// open VisualTest.VData
// open VisualTest.VLog
// open VisualTest.Visual
// open VisualTest.VTest
// open VisualTest.VProgram

// let testParas = {defaultParas with 
//                     InitRegs = [0u ; 0x1004u ; 0x1000u ; 30u ; 40u ; 50u ; 60u ; 70u ; 
//                                 80u ; 90u ; 100u ; 110u ; 120u ; 130u ; 140u] ;
//                     MemReadBase = 0x1000u}
// let testMemValList = 
//     [
//         10u ; 20u ; 30u ; 40u ; 50u ; 60u ; 70u ; 80u ; 90u ; 100u ; 110u ; 120u ; 130u ; 140u
//     ]   
//     |> List.map DataLoc    

// let testCPU:DataPath<Memory.InstrLine> = {
//     Fl = {N=false ; C=false ; Z=false ; V=false};
//     Regs = Seq.zip [R0;R1;R2;R3;R4;R5;R6;R7;R8;R9;R10;R11;R12;R13;R14] testParas.InitRegs
//             |> List.ofSeq
//             |> Map.ofList
//     MM = 
//         let addrList = List.map WA [testParas.MemReadBase..4u..testParas.MemReadBase+(13u*4u)]
//         Seq.zip addrList testMemValList 
//         |> Map.ofSeq
// }                 


// let funfunc =                    
//     printfn "%A" (execute "STR R3, [R2]" testCPU) // MAGIC HERE

    // DATA IS WRONG?

          