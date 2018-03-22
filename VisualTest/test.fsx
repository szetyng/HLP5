//////////////////////////////////////////////////////////////////////////////////////
//                  Setup functions for ARM simulator
//////////////////////////////////////////////////////////////////////////////////////

#load "CommonData.fs"
#load "CommonLex.fs"
#load "Modules/Shift.fs"
#load "Modules/MultiR.fs"
#load "Modules/SingleR.fs"
#load "CommonTop.fs"

open CommonData
open System.IO

/// Generate test data for simulation, assume memory address starts at 0x1000
let genTestData memVal regVal: DataPath<'INS> =
    let tRegs (x:uint32 list) =  [0..14] |> List.map(fun n-> (register n, x.[n])) |> Map.ofList
    let tMem memVal: MachineMemory<'INS> = 
        let n = List.length memVal |> uint32
        let memBase = 0x1000u
        let waList = List.map WA [memBase..4u..(memBase+ 4u*(n-1u))]
        let valList = List.map DataLoc memVal
        List.zip waList valList |> Map.ofList     
    let testData = { 
                Fl = {N = false; C =false; Z = false; V = false}
                Regs = tRegs regVal
                MM = tMem memVal
             }
    testData   

/// Run program on a given CPU state and output result to file
let simulateARM tD prog=
    let result = CommonTop.fullExecute (Ok tD) prog
    let printRes a b = a + sprintf "%A \t %A \n" (fst b) (snd b)

    let printRegisters result = 
        match result with
        | Ok x ->
            let reg = x.Regs |> Map.toList |> List.splitAt 10
            let printRes' a b = a + sprintf "%A \t \t %A \n" (fst b) (snd b)
            let res = List.fold printRes' "" (fst reg)
            List.fold printRes res (snd reg)
        | Error e -> sprintf "%A" e

    let printMem result = 
        match result with
        | Ok x ->
            let mem = x.MM |> Map.toList 
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


//////////////////////////////////////////////////////////////////////////////////////
//                  Example script on using ARM simulator
//////////////////////////////////////////////////////////////////////////////////////

// Define values in memory and registers
let memVal = [
                0xD751CB1Fu ; 0xAB165482u ; 0x458DE9Bu ; 0x541CEEABu ;             // 0x1000 - 0x100C
                0x9104080u ; 0x44438ADCu ; 0x444030F0u ; 0xFF00EA21u ;             // 0x1010 - 0x101C
                0x44400000u ; 0x54C08F0u ; 0xABCDEF48u ; 0x891CECABu ; 0x778220EDu     // 0x1020 - 0x1030
            ]  
let regVal = [0x1000u;4120u;4144u] @ [3ul..14ul] 

// Generate CPU State
let tD = genTestData memVal regVal     

// Read program
let prog = File.ReadAllLines("input.txt")

// Run program on CPU State
simulateARM tD prog

// Example of multipass parsing
CommonTop.fullExecute (Ok tD) prog
CommonTop.multiParseLine None (WA 0ul) prog

let labelMap = 
    Map.ofList [ 
        "LABEL",0u ; "LABEL1",4u 
    ] 

Some labelMap