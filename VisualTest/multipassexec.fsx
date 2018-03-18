//////////////////////////////////////////////////////////////////////////////////////
//                  Example script on using ARM simulator
//////////////////////////////////////////////////////////////////////////////////////

#load "CommonData.fs"
#load "CommonLex.fs"
#load "Shift.fs"
#load "MultiR.fs"
#load "SingleR.fs"
#load "CommonTop.fs"

open CommonData
open CommonTop
open System.IO

/// Define values in memory and registers
let memVal = []
let regVal = [0x1000u;4120u;4144u] @ [3ul..14ul] 

/// Generate test data for simulation
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
 

let prog = File.ReadAllLines("input.txt")

let result = fullExecute (Ok tD) prog

let printRegisters result = 
    match result with
    | Ok x ->
        let reg = x.Regs |> Map.toList |> List.splitAt 10
        let printRes a b = a + sprintf "%A \t \t %A \n" (fst b) (snd b)
        let printRes' a b = a + sprintf "%A \t %A \n" (fst b) (snd b)
        let res = List.fold printRes "" (fst reg)
        List.fold printRes' res (snd reg)

    | Error e -> sprintf "%A" e

let printMem result = 
    match result with
    | Ok x ->
        let mem = x.MM |> Map.toList 
        let printRes a b = a + sprintf "%A \t \t %A \n" (fst b) (snd b)
        List.fold printRes "" mem
    | Error e -> sprintf "%A" e

File.WriteAllText("output.txt","Instructions\n\n")
File.AppendAllText("output.txt", File.ReadAllText("input.txt"))
File.AppendAllText("output.txt", "\n\nInitial Register State \n\n")
File.AppendAllText("output.txt", printRegisters (Ok tD))
File.AppendAllText("output.txt", "\n\nInitial Memory State \n\n")
File.AppendAllText("output.txt", printMem (Ok tD))
File.AppendAllText("output.txt", "\n\nRegister State \n\n")
File.AppendAllText("output.txt", printRegisters result)
File.AppendAllText("output.txt", "\n\nMemory State \n\n")
File.AppendAllText("output.txt", printMem result)



fullExecute (Ok tD) prog
multiParseLine None (WA 0ul) prog


