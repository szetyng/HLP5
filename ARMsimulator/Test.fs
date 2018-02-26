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

let myTestParas = {defaultParas with 
                    InitRegs = [1u ; 4u ; 8u ; 0x1003u ; 0x9104080u ; // R0 - R4
                                0x1000u ; 0xAB165482u ; 0x1010u ; 0x458DE9Bu ; 0x1020u ;       // R5 - R9
                                0x541CEEABu ; 0x1024u ; 0xD751CB1Fu ; 0x102Cu ; 0x44438ADCu]   // R10 - R14
                    MemReadBase = 0x1000u}

let testMemValList = [
                        0xD751CB1Fu ; 0xAB165482u ; 0x458DE9Bu ; 0x541CEEABu ;             // 0x1000 - 0x100C
                        0x9104080u ; 0x44438ADCu ; 0x444030F0u ; 0xFF00EA21u ;             // 0x1010 - 0x101C
                        0x44400000u ; 0x54C08F0u ; 0xABCDEF48u ; 0x891CECABu ; 0x778220EDu     // 0x1020 - 0x1030
                    ]       

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
        | _ -> failwithf "What?"
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

let makeExecLSTestList execName listNameInpErr = 
    let makeOneTest name inp err = 
        match execute inp testCPU with
        | Ok resData -> VisualMemUnitTest name resData myTestParas inp
        | Error e -> execErrorUnitTest name e err inp        
    listNameInpErr
    |> List.map (fun (name, inpStr, ifErr) -> makeOneTest name inpStr ifErr)
    |> Expecto.Tests.testList execName

[<Tests>]
let parseUnitTest = 
    makeParseLSTestList "LDR and STR parse tests"
        [
            "STRB R10, [R15, #5]!" , Ok {InstrN=STR ; Type=Some B; RContents=R10; RAdd=R15 ; Offset=Some (Literal 5, PreIndexed)}
            "LDR R4, [R8], #3", Ok {InstrN=LDR ; Type=None; RContents=R4; RAdd=R8 ; Offset=Some (Literal 3, PostIndexed)}
            "LDRB R7, [R11, #11]", Ok {InstrN=LDR ; Type=Some B; RContents=R7; RAdd=R11 ; Offset=Some (Literal 11, Memory.Normal)} 
            "STR R5, [R2]", Ok {InstrN=STR ; Type=None; RContents=R5; RAdd=R2 ; Offset=None}
            "LDR R0, [R3, ", Error "Incorrect formatting" 
            "STR R1, [R6" , Error "Incorrect formatting" 
            "LDRB R13, R7]", Error "Incorrect formatting" 
            "STRB R2, R4", Error "Incorrect formatting" 
            "LDR R10, [R11, R2!]", Error "Incorrect formatting"
            "STR R8, [R1], 22", Error "Incorrect formatting"
        ]    

[<Tests>]
let execUnitTest = 
    // testList "Executing LDR/STR tests"
    makeExecLSTestList "Executing LDR/STR tests"
        [
            "Normal STR" , "STR R4, [R5]" , ""
            "Normal LDR" , "LDR R1, [R7]" , ""
            "Normal STRB" , "STRB R6, [R3]" , "" // R3 stores address that is not word-aligned
            "Normal LDRB" , "LDRB R2, [R3]" , ""

            "Normal offset STR" , "STR R8, [R9, #-20]" , "" 
            "Normal offset LDR" , "LDR R3, [R11, R2]" , ""
            "Normal offset STRB" , "STRB R10, [R13, R0]" , ""
            "Normal offset LDRB" , "LDRB R4, [R5, #28]", ""

            "Pre-indexed offset STR" , "STR R12, [R5, #8]!" , ""
            "Pre-indexed offset LDR" , "LDR R5, [R3, R0]!" , "" // addr is not div by 4, but effective addr is
            "Pre-indexed offset STRB" , "STRB R4, [R3, #-0b1]!" , "" // R3 is not word-aligned
            "Pre-indexed offset LDRB" , "LDRB R6, [R11, #-0b100]!" , ""

            // Allows post-indexed addressing to update the register value to anything
            "Post-indexed offset STR" , "STR R6, [R11], R1", ""
            "Post-indexed offset LDR" , "LDR R11, [R5], #-5" , ""
            "Post-indexed offset STRB" , "STRB R8, [R9], #0xA9" , ""
            "Post-indexed offset LDRB" , "LDRB R10, [R7], R3" , ""

            "Memory access error - word aligned" , "STR R8, [R9, #5]" , "Memory address accessed must be divisible by 4"
            "Memory access error - not allowed" , "LDR R0, [R5, #-4]!" , "Not allowed to access this part of memory"
            "Parse error" , "STR R0, [R7 R0]", "Incorrect formatting"
        ]


[<EntryPoint>]
let main argv =
    initCaches myTestParas
    let rc = runTestsInAssembly expectoConfig [||]
    finaliseCaches myTestParas
    System.Console.ReadKey() |> ignore                
    rc // return an integer exit code - 0 if all tests pass

