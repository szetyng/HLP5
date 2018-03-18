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



