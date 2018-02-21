// To be split into several modules?
module Test

open VisualTest.VCommon
open VisualTest.VData
open VisualTest.VLog
open VisualTest.Visual
open VisualTest.VTest
open VisualTest.VProgram

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
// let makeExecLDRTestList name listIOpairs = 
//     let makeOneTest i (inp, outp) = 
//         testCase (sprintf "%s:%d" name i) <| fun () ->
//         Expect.equal (execute inp dataDummy) outp (sprintf "Executing '%s'" inp)
//     listIOpairs
//     |> List.indexed
//     |> List.map (fun (i,pair) -> (makeOneTest i pair))
//     |> Expecto.Tests.testList name    


//[<Tests>]
let t1 = 
    //let makeParseLSTests listIOpairs = makeParseLSTestList "LDR and STR parse tests" listIOpairs 
    makeParseLSTestList "LDR and STR parse tests"
        [
            "STRB R10, [R15, #5]!" , Ok {Instr=STR ; Type=Some B; RContents=R10; RAdd=R15 ; Offset=Some (Literal 5u, PreIndexed)}
            "LDR R4, [R8], #3", Ok {Instr=LDR ; Type=None; RContents=R4; RAdd=R8 ; Offset=Some (Literal 3u, PostIndexed)}
            "LDRB R7, [R11, #11]", Ok {Instr=LDR ; Type=Some B; RContents=R7; RAdd=R11 ; Offset=Some (Literal 11u, Memory.Normal)} 
            "STR R5, [R2]", Ok {Instr=STR ; Type=None; RContents=R5; RAdd=R2 ; Offset=None}
            "LDR R10, [R15, ", Error "Incorrect formatting" //failing. Good? 
            "LDR R10, [R15" , Error "Incorrect formatting" //ERROR, NO BRACKETS
            "LDR R10, R15]", Error "Incorrect formatting" // ERROR, NO BRACKETS
            "LDR R10, R15", Error "Incorrect formatting" // ERROR, NO BRACKETS
            "LDR R10, [R15, R2!]", Error "Incorrect formatting"
            // SHOULD PASS
            //"ldrb r10, [r15, #4]", Ok {Instr=LDR ; Type=Some B; RContents=R10; RAdd=R15 ; Offset=Some (Literal 4u, Memory.Normal)} //failing
        ]    

// [<Tests>]
// let t2 = 
//     makeExecLDRTestList "LDR and STR execution tests"
//         [
//             "LDR R0, [R1]", Ok dataDummy
//         ]    

let testParas = {defaultParas with 
                    InitRegs = [0u ; 10u ; 20u ; 30u ; 40u ; 50u ; 60u ; 70u ; 
                                80u ; 90u ; 100u ; 110u ; 120u ; 130u ; 140u] ;
                    MemReadBase = 0x1000u}

let testMemValList = 
    [
        10u ; 20u ; 30u ; 40u ; 50u ; 60u ; 70u ; 80u ; 90u ; 100u ; 110u ; 120u ; 130u ; 140u
    ]   
    |> List.map DataLoc    

let testCPU:DataPath<InstrLine> = {
    Fl = {N=false ; C=false ; Z=false ; V=false};
    Regs = Seq.zip [R0;R1;R2;R3;R4;R5;R6;R7;R8;R9;R10;R11;R12;R13;R14;R15] testParas.InitRegs
            |> List.ofSeq
            |> Map.ofList
    MM = 
        let addrList = List.map WA [testParas.MemReadBase..4u..testParas.MemReadBase+(13u*4u)]
        Seq.zip addrList testMemValList 
        |> Map.ofSeq
}   

 

let VisualMemUnitTest name (actualOut: DataPath<InstrLine>) paras inpAsm = 
    testCase name <| fun () ->
        let _, expectedOut = RunVisualWithFlagsOut paras inpAsm
        let expectedState = decodeStateFromRegs expectedOut.RegsAfterPostlude
        let expectedMemValList = List.map DataLoc expectedState.VMemData
        let addrList = List.map WA [paras.MemReadBase..4u..paras.MemReadBase+(13u*4u)]
        let expectedMemMap = 
            expectedMemValList
            |> List.allPairs addrList
            |> List.distinct
            |> Map.ofList    
        Expecto.Expect.equal actualOut.MM expectedMemMap "Memory doesn't match"    

let makeTestList name listIOpairs = 
    let makeOneTest (inp, outp) = 
        //testCase (sprintf "%s:%d" name i) <| fun () ->
        let initFl = {N=false ; C=false ; Z=false ; V=false}
        let initRegMap = 
            Seq.zip [R0;R1;R2;R3;R4;R5;R6;R7;R8;R9;R10;R11;R12;R13;R14] testParas.InitRegs
            |> Map.ofSeq
        let addrList = List.map WA [testParas.MemReadBase..4u..testParas.MemReadBase+(13u*4u)]
        let initMemMap = 
            Seq.zip addrList testMemValList
            |> Map.ofSeq  
        let initData = {Fl=initFl ; Regs=initRegMap ; MM=initMemMap}                     
        let actualResData = execute inp initData // MAGIC HERE
        match actualResData with
        | Ok dat -> 
            VisualMemUnitTest name dat testParas outp
        | _ -> failwithf "find way to test error?"        
    listIOpairs
    |> List.indexed
    |> List.map (fun (_,pair) -> makeOneTest pair)
    |> Expecto.Tests.testList name

let testMem = 
    makeTestList "LDR and STR execution tests"
        [
            "LDR R0, [R1]", "LDR R0, [R1]" 
            "STR R0, [R1]", "STR R0, [R1]" 
        ]


[<EntryPoint>]
let main argv =
    // printfn "%A" argv
    //printfn "Testing LDR/STR"
    //Expecto.Tests.runTestsInAssembly Expecto.Tests.defaultConfig [||] |> ignore
    //Console.ReadKey() |> ignore  
    //0 // return an integer exit code
    
    // initCaches testParas
    // let rc = runTestsInAssembly expectoConfig [||]
    // finaliseCaches testParas
    // Console.ReadKey() |> ignore
    // rc
    CommonTop.funfunc |> ignore
    0
