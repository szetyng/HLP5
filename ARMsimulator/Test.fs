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

let myTestParas = {defaultParas with 
                    InitRegs = [4u ; 0x1004u ; 0u ; 0x100Cu ; 40u ; // R0 - R4
                                0x1000u ; 60u ; 0x1030u ; 80u ; 0x1024u ;       // R5 - R9
                                100u ; 0x101Cu ; 120u ; 0x1014u ; 140u]   // R10 - R14
                    MemReadBase = 0x1000u}
//let myTestParas = defaultParas
let testMemValList = [
                        10u ; 20u ; 30u ; 40u ;             // 0x1000 - 0x100C
                        50u ; 60u ; 70u ; 80u ;             // 0x1010 - 0x101C
                        90u ; 100u ; 110u ; 120u ; 130u     // 0x1020 - 0x1030
                    ]       

let testCPU:DataPath<Memory.InstrLine> = {
    Fl = {N=false ; C=false ; Z=false ; V=false};
    Regs = Seq.zip [R0;R1;R2;R3;R4;R5;R6;R7;R8;R9;R10;R11;R12;R13;R14] myTestParas.InitRegs
            |> List.ofSeq
            |> Map.ofList
    MM = 
        let addrList = List.map WA [myTestParas.MemReadBase..4u..myTestParas.MemReadBase+(12u*4u)]
        Seq.zip addrList (List.map DataLoc testMemValList) 
        |> Map.ofSeq
} 

let VisualMemUnitTest name (actualOut: DataPath<InstrLine>) paras inpAsm = // expOutRegs expOutMem = 
    testCase name <| fun () ->
        let expectedOut = RunVisualWithFlagsOut paras inpAsm testMemValList
        let addrList = List.map WA [paras.MemReadBase..4u..paras.MemReadBase+(12u*4u)]
        let memLocList = List.map DataLoc expectedOut.State.VMemData |> List.rev
        let expectedMemMap = 
            memLocList
            |> Seq.zip addrList
            |> Map.ofSeq 
        Expecto.Expect.equal actualOut.MM expectedMemMap "Memory doesn't match"   

        let expectedRegMap = 
            expectedOut.Regs
            |> List.map (fun (R nr, v) -> register nr, uint32 v)
            |> List.sort
            |> List.take 15 // to remove R15
            |> Map.ofList                                      
        Expecto.Expect.equal actualOut.Regs expectedRegMap "Registers don't match"

        // let regAffectedName = 
        //     expOutRegs |> List.map fst
        // let visOutRegsRelevant = 
        //     expectedOut.Regs
        //     |> List.filter (fun (r,_) -> List.contains r regAffectedName)
        //     |> List.sort
        // Expecto.Expect.equal visOutRegsRelevant (expOutRegs |> List.sort) <|
        //         sprintf "Register outputs>\n%A\n<don't match expected outputs, src=%s" expectedOut.Regs inpAsm            

let VisualErrorUnitTest name errActual errExpected  = 
    testCase name <| fun () ->
    Expecto.Expect.equal errActual errExpected "Memory doesn't match"  



let makeExecTest name inpStr ifError = //outReg outMem = 
    printfn "parse %A" (onlyParseLine inpStr)
    match execute inpStr testCPU with
    | Ok resData -> 
        printfn "resData %A" resData
        VisualMemUnitTest name resData myTestParas inpStr //outReg outMem
    //| Error _ -> failwithf "error"
    | Error e -> 
        printfn "error %A" e
        VisualErrorUnitTest name e ifError
    // printfn "Please work %A" (execute inpStr testCPU)
    // VisualMemUnitTest name testCPU myTestParas inpStr outReg


[<Tests>]
let tMem = 
    testList "Executing LDR/STR tests" 
        [
            // makeExecTest "Normal STR" "STR R0, [R1]" ""
            // makeExecTest "Normal LDR" "LDR R2, [R3]" ""
            // makeExecTest "Normal STRB" "STRB R4, [R5]" ""
            // makeExecTest "Normal LDRB" "LDRB R6, [R7]" ""
            makeExecTest "Normal offset STR" "STR R8, [R9, #5]" "Incorrect formatting"
            // makeExecTest "Normal offset LDR" "LDR R9, [R11, R0]" ""
            // makeExecTest "Pre-indexed offset STRB" "STRB R10, [R11, #8]!" ""
            // makeExecTest "Post-indexed offset LDR" "LDR R11, [R3], #16" ""
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

