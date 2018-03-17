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

let tabl = Seq.zip ["LABEL3"; "LABEL2"] [0x1000u; 0x1004u] |> Map.ofSeq
/////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////
let myParseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmLine:string) =
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
    let passOne words = 
        let newSymTab = symtab.Value
        let add =
            match loadAddr with
            | WA a -> a
        let isLabel = 
            match words with
            | opc :: _ ->
                match Map.tryFind opc opCodes with
                | Some _ -> false
                | None -> true
            | _ -> failwithf "idk"            
        match isLabel, words with
        | true, label :: _ -> Some (Map.add label add newSymTab)
        | false, _ -> Some newSymTab
        | _ -> failwithf "idk"
    /// second pass
    /// try to parse 1st word, or 2nd word, as opcode
    /// If 2nd word is opcode 1st word must be label
    let matchLine stab words =
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
                    with Label=Some label; SymTab=stab} 
                  |> IMatch with
            | None -> 
                Error (sprintf "Unimplemented instruction %s" opc)
            | Some pa -> pa
        | _ -> Error (sprintf "Unimplemented instruction %A" words)
    let lineInList =     
        asmLine
        |> removeComment
        |> splitIntoWords
        |> Array.toList
    let newSym = passOne lineInList
    matchLine newSym lineInList


myParseLine (Some tabl) (WA 0u) prog.[0]


