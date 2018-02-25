// To be split into several modules?
module Test

open VisualTest.VCommon
open VisualTest.VData
open VisualTest.VLog
open VisualTest.Visual
open VisualTest.VTest
open VisualTest.VProgram

open CommonData
open CommonLex
open Memory
open DP
open CommonTop

open Expecto
open FsCheck
open System

// dummy paras
let myTestParas = {defaultParas with 
                    InitRegs = [4u ; 0x1004u ; 0u ; 0x100Cu ; 40u ; // R0 - R4
                                0x1000u ; 60u ; 0x1030u ; 80u ; 0x1024u ;       // R5 - R9
                                100u ; 0x101Cu ; 120u ; 0x1020u ; 0x44400000u]   // R10 - R14
                    MemReadBase = 0x1000u}
//let myTestParas = defaultParas

// dummy memory
let testMemValList = [
                        10u ; 20u ; 30u ; 40u ;             // 0x1000 - 0x100C
                        50u ; 60u ; 70u ; 80u ;             // 0x1010 - 0x101C
                        0x44400000u ; 100u ; 110u ; 120u ; 140u      // 0x1020 - 0x1030
                    ]       

// dummy CPUdata
let testCPU:DataPath<Memory.Instr> = {
    Fl = {N=false ; C=false ; Z=false ; V=false};
    Regs = Seq.zip [R0;R1;R2;R3;R4;R5;R6;R7;R8;R9;R10;R11;R12;R13;R14] myTestParas.InitRegs
            |> List.ofSeq
            |> Map.ofList
    MM = 
        let addrList = List.map WA [myTestParas.MemReadBase..4u..myTestParas.MemReadBase+(12u*4u)]
        Seq.zip addrList (List.map DataLoc testMemValList) 
        |> Map.ofSeq
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

let VisualMemUnitTest name (actualOut: DataPath<Memory.Instr>) paras inpAsm = // expOutRegs expOutMem = 
    testCase name <| fun () ->
        let expectedOut = RunVisualWithFlagsOut paras inpAsm testMemValList
        let addrList = List.map WA [paras.MemReadBase..4u..paras.MemReadBase+(12u*4u)]
        let memLocList = List.map DataLoc expectedOut.State.VMemData |> List.rev
        let expectedMemMap = 
            memLocList
            |> Seq.zip addrList
            |> Map.ofSeq 
        Expecto.Expect.equal actualOut.MM expectedMemMap <|
            sprintf "Memory doesn't match for assembler line: %s" inpAsm  

        let expectedRegMap = 
            expectedOut.Regs
            |> List.map (fun (R nr, v) -> register nr, uint32 v)
            |> List.sort
            |> List.take 15 // to remove R15
            |> Map.ofList                                      
        Expecto.Expect.equal actualOut.Regs expectedRegMap <|
            sprintf "Registers don't match for assembler line: %s" inpAsm         

let execErrorUnitTest name errActual errExpected inpAsm = 
    testCase name <| fun () ->
    Expecto.Expect.equal errActual errExpected <|
        sprintf "Error executing line: %s" inpAsm

let makeExecTestList execName listNameInpErr = 
    let makeOneTest name inp err = 
        match execute inp testCPU with
        | Ok resData -> VisualMemUnitTest name resData myTestParas inp
        | Error e -> execErrorUnitTest name e err inp        
    listNameInpErr
    |> List.map (fun (name, inpStr, ifErr) -> makeOneTest name inpStr ifErr)
    |> Expecto.Tests.testList execName

[<Tests>]
let parseUnitTest = 
    //let makeParseLSTests listIOpairs = makeParseLSTestList "LDR and STR parse tests" listIOpairs 
    makeParseLSTestList "LDR and STR parse tests"
        [
            "STRB R10, [R15, #5]!" , Ok {InstrN=STR ; Type=Some B; RContents=R10; RAdd=R15 ; Offset=Some (Literal 5, PreIndexed)}
            "LDR R4, [R8], #3", Ok {InstrN=LDR ; Type=None; RContents=R4; RAdd=R8 ; Offset=Some (Literal 3, PostIndexed)}
            "LDRB R7, [R11, #11]", Ok {InstrN=LDR ; Type=Some B; RContents=R7; RAdd=R11 ; Offset=Some (Literal 11, Memory.Normal)} 
            "STR R5, [R2]", Ok {InstrN=STR ; Type=None; RContents=R5; RAdd=R2 ; Offset=None}
            "LDR R10, [R15, ", Error "Incorrect formatting" //failing. Good? 
            "LDR R10, [R15" , Error "Incorrect formatting" //ERROR, NO BRACKETS
            "LDR R10, R15]", Error "Incorrect formatting" // ERROR, NO BRACKETS
            "LDR R10, R15", Error "Incorrect formatting" // ERROR, NO BRACKETS
            "LDR R10, [R15, R2!]", Error "Incorrect formatting"
        ]    

[<Tests>]
let execUnitTest = 
    // testList "Executing LDR/STR tests"
    makeExecTestList "Executing LDR/STR tests"
        [
            "Normal STR" , "STR R0, [R1]" , ""
            "Normal LDR" , "LDR R2, [R3]" , ""
            "Normal STRB" , "STRB R4, [R5]" , ""
            "Normal LDRB" , "LDRB R6, [R7]" , ""
            "Normal offset STR" , "STR R8, [R9, #-8]" , ""
            "Normal offset LDR" , "LDR R9, [R11, R0]" , ""
            "Pre-indexed offset STRB" , "STRB R10, [R11, #7]!" , ""
            "Pre-indexed offset LDRB" , "LDRB R10, [R11, #7]!" , ""
            "Post-indexed offset LDRB" , "LDRB R10, [R11], R0" , ""
            "Post-indexed offset LDR" , "LDR R11, [R3], #16" , ""
            "Memory access error" , "STR R8, [R9, #5]" , "Memory address accessed must be divisible by 4"
            "Moar STRB" , "STRB R14, [R1, #0xA]" , ""
            "Moar LDRB" , "LDRB R1, [R5, #34]!" , ""
        ]

    

[<EntryPoint>]
let main argv =
    // printfn "%A" argv
    // printfn "Testing LDR/STR"
    // Expecto.Tests.runTestsInAssembly Expecto.Tests.defaultConfig [||] |> ignore
    // Console.ReadKey() |> ignore  
    // 0 // return an integer exit code
    
    initCaches myTestParas
    let rc = runTestsInAssembly expectoConfig [||]
    finaliseCaches myTestParas
    System.Console.ReadKey() |> ignore                
    rc // return an integer exit code - 0 if all tests pass

