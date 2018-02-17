// To be split into several modules?
module Test

open CommonTop
open CommonData
open CommonLex
open Memory
open DP

open Expecto
open FsCheck
open System
/// test the initProjectLexer code
//let test = parseLine None (WA 0u)

//let test = parseLine None (WA 0u) "STR R10, [R15]"

let regDummyList = 
    [ 
        R0,0x100u ; R1,0x104u ; R2,4u ; R3,2u ; R4,1u ; R5,0u ; R6,0u ; R7,0u;
        R8,0u ; R9,0u ; R10,0u ; R11,0u ; R12,0u ; R13,0u ; R14,0u ; R15,0u
    ]
let memDummyList = 
    [
        WA 0x100u, DataLoc 100u ; WA 0x104u, DataLoc 104u ; 
        WA 0x108u, DataLoc 108u ; WA 0x10Cu, DataLoc 112u      
    ]

let dataDummy : DataPath<Memory.InstrLine> = {
    Fl = {N=false ; C=false ; Z=false ; V=false} ;
    Regs = Map.ofList regDummyList ;
    MM = Map.ofList memDummyList
}

let onlyParseLine (asmLine:string) = 
    let makeLineData opcode operands = {
        OpCode=opcode
        Operands=String.concat "" operands
        Label=None
        LoadAddr = WA 0u // dummy
        SymTab = None // dummy
    }
    let splitIntoWords ( line:string ) =
        line.Split( ([||] : char array), 
            System.StringSplitOptions.RemoveEmptyEntries)
    let matchLine words =
        let pNoLabel =
            match words with
            | opc :: operands -> 
                makeLineData opc operands 
                |> Memory.parse
            | _ -> None
        match pNoLabel with
        | Some (Ok pa) -> Ok pa.PInstr
        | Some (Error e) -> Error e
        | x ->
            printfn "WHAT %A" x
            failwithf "Please write proper tests"
    asmLine
    |> splitIntoWords
    |> Array.toList
    |> matchLine

let makeParseLSTestList name listIOpairs = 
    let makeOneTest i (inp,outp) = 
        testCase (sprintf "%s:%d" name i) <| fun () ->
        Expect.equal (onlyParseLine inp) outp (sprintf "Parsing '%s'" inp)
    listIOpairs 
    |> List.indexed
    |> List.map (fun (i,pair) -> (makeOneTest i pair))
    |> Expecto.Tests.testList name
let makeExecLDRTestList name listIOpairs = 
    let makeOneTest i (inp, outp) = 
        testCase (sprintf "%s:%d" name i) <| fun () ->
        Expect.equal (execute inp dataDummy) outp (sprintf "Executing '%s'" inp)
    listIOpairs
    |> List.indexed
    |> List.map (fun (i,pair) -> (makeOneTest i pair))
    |> Expecto.Tests.testList name    


// [<Tests>]
// let t1 = 
//     //let makeParseLSTests listIOpairs = makeParseLSTestList "LDR and STR parse tests" listIOpairs 
//     makeParseLSTestList "LDR and STR parse tests"
//         [
//             "STRB R10, [R15, #5]!" , Ok {Instr=STR ; Type=Some B; RContents=R10; RAdd=R15 ; Offset=Some (Literal 5u, PreIndexed)}
//             "LDR R4, [R8], #3", Ok {Instr=LDR ; Type=None; RContents=R4; RAdd=R8 ; Offset=Some (Literal 3u, PostIndexed)}
//             "LDRB R7, [R11, #11]", Ok {Instr=LDR ; Type=Some B; RContents=R7; RAdd=R11 ; Offset=Some (Literal 11u, Memory.Normal)} 
//             "STR R5, [R2]", Ok {Instr=STR ; Type=None; RContents=R5; RAdd=R2 ; Offset=None}
//             "LDR R10, [R15, ", Error "Incorrect formatting" //failing. Good? 
//             "LDR R10, [R15" , Error "Incorrect formatting" //ERROR, NO BRACKETS
//             "LDR R10, R15]", Error "Incorrect formatting" // ERROR, NO BRACKETS
//             "LDR R10, R15", Error "Incorrect formatting" // ERROR, NO BRACKETS
//             "LDR R10, [R15, R2!]", Error "Incorrect formatting"
//             // SHOULD PASS
//             //"ldrb r10, [r15, #4]", Ok {Instr=LDR ; Type=Some B; RContents=R10; RAdd=R15 ; Offset=Some (Literal 4u, Memory.Normal)} //failing
//         ]    

[<Tests>]
let t2 = 
    makeExecLDRTestList "LDR and STR execution tests"
        [
            "LDR R0, [R1]", Ok dataDummy
        ]    


[<EntryPoint>]
let main argv =
    // printfn "%A" argv
    printfn "Testing LDR/STR parsing!"
    Expecto.Tests.runTestsInAssembly Expecto.Tests.defaultConfig [||] |> ignore
    Console.ReadKey() |> ignore  
    0 // return an integer exit code