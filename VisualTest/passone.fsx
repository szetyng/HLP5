#load "CommonData.fs"
#load "CommonLex.fs"
#load "Shift.fs"
#load "MultiR.fs"
#load "SingleR.fs"
#load "CommonTop.fs"

open CommonData
open CommonLex
open Shift
open MultiR
open CommonTop
open System.Text.RegularExpressions


/// Generate testing data for execution
let memVal = []
let regVal = [0x1000u;4120u;4144u] @ [3ul..14ul] 
let tRegs (x:uint32 list) =  [0..14] |> List.map(fun n-> (register n, x.[n])) |> Map.ofList
let tMem memVal: MachineMemory<'INS> = 
    let n = List.length memVal |> uint32
    let memBase = 0x1000u
    let waList = List.map WA [memBase..4u..(memBase+ 4u*(n-1u))]
    let valList = List.map DataLoc memVal
    List.zip waList valList |> Map.ofList     

let tD = { 
            Fl = {N = false; C =false; Z = false; V = false}
            Regs = tRegs regVal
            MM = tMem memVal
         }

let splitIntoLines ( line:string ) =
                line.Split( ([|'\n'|] : char array), 
                    System.StringSplitOptions.RemoveEmptyEntries)

let asm = "LABEL LSR R4,R1,#2 \n LABEL1 STM R1, {R1,R2}"
/// Assume that program has been parsed and is valid.
let parseAndExecute tD asm= 
    let parsedRes = parseLine None (WA 0ul) asm
    match parsedRes, tD with
    | Ok x, Ok d -> IExecute x.PInstr d
    | Error e, _ -> Error e
    | _, Error e -> Error e
let prog = asm |> splitIntoLines 
let parseSingle = parseAndExecute (Ok tD) prog.[1]   // example of parsing a single line

Array.fold parseAndExecute (Ok tD) prog


// ST's
// let execMultiLine src cpuData = 
//     let lineLst = splitIntoLines src
//     let parseAndExecute symTab tD asm = 
//         let parsedRes = parseLine symTab (WA 0ul) asm
//         match parsedRes, tD with
//         | Ok x, Ok d -> IExecute x.PInstr d
//         | Error e, _ -> Error e
//         | _, Error e -> Error e
//     Array.fold (parseAndExecute None) (Ok cpuData) lineLst
let tab = ["LABEL1"; "LABEL2"]
let tab2 = [0x1000u; 0x1004u]
let tabl = Seq.zip tab tab2 |> Map.ofSeq
/////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////

let firstPass (symtab: SymbolTable option) (loadAddr: WAddr) (asmLine:string) =
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
                match Map.tryFind opc opCodes with
                | Some _ -> Ok (makeLineData opc operands) //|> IMatch
                | None -> Error "Not an opcode"
            | _ -> failwithf "None"
        
        match pNoLabel, words with
        | Ok lD, _ -> lD
        | _, label :: opc :: operands -> 
            match { makeLineData opc operands 
                    with Label=Some label} with
            | lD -> 
                let oldSymTab = lD.SymTab
                {lD with SymTab=oldSymTab}              
            | _ -> 
                failwithf "Uimplementer instructions"
        | _ -> failwithf "Uimplementer instructions"
    asmLine
    |> removeComment
    |> splitIntoWords
    |> Array.toList
    |> matchLine

let execMultiLine src cpuData = 
    let lineLst = splitIntoLines src

    let parseAndExecute symTab tD asm = 
        let parsedRes = parseLine symTab (WA 0ul) asm

        match parsedRes, tD with
        | Ok x, Ok d -> IExecute x.PInstr d
        | Error e, _ -> Error e
        | _, Error e -> Error e

    let passOne state oneLine = 
        let x = firstPass state.SymTab state.LoadAddr oneLine
        match x with
        | pa -> pa, pa
        | _ -> failwithf "Error"

    let state = {LoadAddr=WA 0u; Label = None ; SymTab= Some tabl ; OpCode = ""; Operands= ""}
    // needs to return list of line data, thus, need to change pass2's parseLine
    let newListLD, newSymTabSrc = Array.mapFold passOne state lineLst
    let newSymTab = newSymTabSrc.SymTab
    Array.map (fun lD -> {lD with SymTab = newSymTab}) newListLD



execMultiLine asm tD

