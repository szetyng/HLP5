// for scripting purposes

#load "CommonData.fs"
#load "CommonLex.fs"
#load "DP.fs"
#load "Memory.fs"
#load "CommonTop.fs"
// #load "Test.fs"
open CommonData
open CommonLex
//open DP
open Memory
open CommonTop
// open Test

let memDummyList = 
    [WA 0x100u, DataLoc 0x2000u ; WA 0x104u, DataLoc 0x202u]

let seepeeyouData = {
    Fl = {N=false ; C=false ; Z=false ; V=false} ;
    Regs = Map.ofList [ 
            R0,0u ; R1,3u ; R2,0x412u ; R3,0u ; R4,0u ; R5,0x100u ; R6,0u ; R7,0u;
            R8,0u ; R9,0u ; R10,0u ; R11,0u ; R12,0u ; R13,0u ; R14,0u ; R15,0u
        ] ;
    MM = Map.ofList memDummyList
}

let asmLine = "LDR R10, [R5]"
//let parsed = parseLine None (WA 0u) asmLine 

let executeAnyInstr (instr:Instr) (d:DataPath<Memory.InstrLine>) = //lazy way out
    let execute d =
        match instr with
        | IMEM ins -> Memory.executeMemInstr ins d
        | IDP _ -> failwithf "not yet implemented"
    execute d  

// extract Instr from Result<Parse<Instr>,errortype>
let exec parsedIns = 
    match parsedIns with
    | Ok ({PInstr=ins} as pr) -> executeAnyInstr ins seepeeyouData
    | _ -> failwithf "idk"

let x = 
    parseLine None (WA 0u) asmLine  
    |> exec
let regmap = x.Regs
regmap.[R10]