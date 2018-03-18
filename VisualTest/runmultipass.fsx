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

let tD:DataPath<Instr> = { 
            Fl = {N = false; C =false; Z = false; V = false}
            Regs = tRegs regVal
            MM = tMem memVal
         }

let splitIntoLines ( line:string ) =
                line.Split( ([|'\n'|] : char array), 
                    System.StringSplitOptions.RemoveEmptyEntries)

let asm = "LABEL LSR R4,R1,#2 \n LABEL1 STM R1, {R1,R2} \n LABEL2 LDR R2, [R3]"


let tabl = Seq.zip ["LABEL3"; "LABEL2"] [0x1000u; 0x1004u] |> Map.ofSeq
/////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////

let multiParseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmMultiLine:string) =
    /// put parameters into a LineData record
    let makeLineData opcode operands = {
        OpCode=opcode
        Operands=String.concat "" operands
        Label=None
        LoadAddr = loadAddr
        SymTab = symtab
    }
    /// split multiline separated by \n into an array
    /// each element of the array is a newline
    let splitIntoLines ( line:string ) =
                line.Split( ([|'\n'|] : char array), 
                    System.StringSplitOptions.RemoveEmptyEntries)
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
    let secondPass data =
        match data |> IMatch with
        | Some pa -> pa
        | None -> failwithf "idk"

    let firstPass prevLD src = 
        let currAddr = 
            match prevLD.LoadAddr with
            | WA a -> a
        let currSymTab = prevLD.SymTab        
        match src with
        | label :: opc :: operands ->
            match Map.tryFind label SingleR.opCodes with
            | Some _ -> {makeLineData opc operands 
                            with LoadAddr=WA currAddr}, {prevLD with LoadAddr=WA(currAddr+4u)}
            | None -> 
                let newSymTab = 
                    match currSymTab,currAddr with
                    | None, a -> Some (Map.ofList [label,a])
                    | Some s, a -> Some (Map.add label a s)
                {makeLineData opc operands
                    with LoadAddr=WA currAddr;Label=Some label;SymTab=newSymTab}, {prevLD with LoadAddr=WA(currAddr+4u);SymTab=newSymTab}                        

    let dummyLD = {LoadAddr=loadAddr ; Label=None ; SymTab=None ; OpCode="" ; Operands=""}                 
    let asmSplitLine = splitIntoLines asmMultiLine |> Array.toList
    let asmSplitLineSplitWords = asmSplitLine |> List.map removeComment |> List.map splitIntoWords |> List.map Array.toList
    let listLineData, finalLineData = List.mapFold firstPass dummyLD asmSplitLineSplitWords 
    let listLineDataWSymTab = List.map (fun d -> {d with SymTab=finalLineData.SymTab}) listLineData
    List.map secondPass listLineDataWSymTab

let asm1 = "LABEL2 LDR R2, [R3] \n LABEL LSR R4,R1,#2 \n LABEL1 STM R1, {R1,R2} "

multiParseLine None (WA 4u) asm1