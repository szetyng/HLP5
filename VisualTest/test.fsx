#load "CommonData.fs"
#load "CommonLex.fs"
#load "Shift.fs"
#load "MultiR.fs"
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


let splitIntoWords ( line:string ) =
                line.Split( ([|'\n'|] : char array), 
                    System.StringSplitOptions.RemoveEmptyEntries)

let asm = "LABEL LSR R1,R1,#2 \n LABEL1 STM R1, {R1,R2}"
let parseAndExecute asm tD = 
    let parsedRes = parseLine None (WA 0ul) asm
    match parsedRes with
    | Ok x -> IExecute x.PInstr tD
    | (Error e) -> Error e


let prog = asm |> splitIntoWords 

parseAndExecute prog.[1] tD
