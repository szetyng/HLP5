module MultiRTests 

open CommonData
open CommonLex
open CommonTest
open MultiR
open Expecto
open VisualTest.VCommon
open VisualTest.VTest


/// Generate testing data for execution
let memVal = []
let regVal = [defaultParas.MemReadBase;4120u;4144u] @ [3ul..14ul] 

let tD = genTestData memVal regVal

let parseAndExecute asm tD = 
    let parsedRes = CommonTop.parseLine None (WA 0ul) asm
    match parsedRes with
    | Ok x -> CommonTop.IExecute x.PInstr tD
    | (Error e) -> Error e

let executeMultiR asm tD: DataPath<Instr> =
    match parseAndExecute asm tD with
    | Ok x -> x
    | _ -> failwithf "Not testing for errors here"

let executeErr asm tD: string = 
    match parseAndExecute asm tD with
    | Error e -> e
    | _ -> failwithf "Not testing for correctness here"

let MakeParseTests name tList =
    let singleTest i (input,expected)  =
        testCase (sprintf "Parse Test %s #%d" name i) <| fun () ->
        let actual = CommonTop.parseLine None (WA 0ul) input 

        Expecto.Expect.equal actual expected (sprintf "Test parsing of %s" input) 
    tList
    |>List.indexed
    |>List.map (fun (x,y) -> singleTest x y)
    |> Expecto.Tests.testList name

let MakeExErrTests name tList =
    let singleTest i (src,tD,expected)  =
        testCase (sprintf "Execute Errors Test %s #%d" name i) <| fun () ->
        let actual = executeErr src tD
        Expecto.Expect.equal actual expected (sprintf "Test execution errors of %s" src) 
    tList
    |>List.indexed
    |>List.map (fun (x,y) -> singleTest x y)
    |> Expecto.Tests.testList name

let parseResult opcode dir rn w reglist = 
    let pConv p = pResultInstrMap CommonTop.IMULTIMEM string p
    Ok {
        PInstr = {InstrC = Shift;
                  OpCode = opcode;
                  AMode = dir;
                  Rn = rn;
                  WrB = w;
                  RList = reglist;
                };
        PLabel = None;
        PSize = 4u;
        PCond = Cal;
        } |> pConv
        
[<Tests>]
let parseTestBasic = 
    MakeParseTests "Basic Syntax"
        [
            "LDM R1,{r1}", parseResult "LDM" "" R1 false (Ok [R1])
            "LDMIA R1,{r1}", parseResult "LDM" "IA" R1 false (Ok [R1])
            "LDMFD R1,{r1}", parseResult "LDM" "FD" R1 false (Ok [R1])
            "LDMDB R1,{r1}", parseResult "LDM" "DB" R1 false (Ok [R1])
            "LDMEA R1,{r1}", parseResult "LDM" "EA" R1 false (Ok [R1])
            "STM R1,{r1}", parseResult "STM" "" R1 false (Ok [R1])
            "STMIA R1,{r1}", parseResult "STM" "IA" R1 false (Ok [R1])
            "STMFD R1,{r1}", parseResult "STM" "FD" R1 false (Ok [R1])
            "STMDB R1,{r1}", parseResult "STM" "DB" R1 false (Ok [R1])
            "STMEA R1,{r1}", parseResult "STM" "EA" R1 false (Ok [R1])
            "LDM R1!,{r2}", parseResult "LDM" "" R1 true (Ok [R2])             // writeback
            "LDM r1,{r2}", parseResult "LDM" "" R1 false (Ok [R2])              // lowercase
        ]

[<Tests>]
let parseTestReglist = 
    MakeParseTests "reglist syntax"
        [
            "LDM R1,{r1,r2}", parseResult "LDM" "" R1 false (Ok [R1;R2])
            "LDM R1,{r2,r1}", parseResult "LDM" "" R1 false (Ok [R1;R2])
            "LDM R1,{r1-r2}", parseResult "LDM" "" R1 false (Ok [R1;R2])
            "LDM R1,{r1-r1}", parseResult "LDM" "" R1 false (Ok [R1])
            "LDM R1,{r1-r2,r3}", parseResult "LDM" "" R1 false (Ok [R1;R2;R3])
            "LDM R1,{r1,r2-r3}", parseResult "LDM" "" R1 false (Ok [R1;R2;R3])
            "LDM R1,{r1,r2-r3,r4-r5}", parseResult "LDM" "" R1 false (Ok [R1;R2;R3;R4;R5])
            "LDM R1,{r1,r2-r3,r2-r5}", parseResult "LDM" "" R1 false (Ok [R1;R2;R3;R4;R5])
            "LDM R1,{r15}", parseResult "LDM" "" R1 false (Ok [R15])
        ]    


let parseLDMResult x = parseResult "LDM" "" R1 true x
[<Tests>]
let parseTestError =
    MakeParseTests "Errors"
        [
            "LDM R15,{r1}", Error "Invalid syntax for Rn given"
            "LDM R15,{}", Error "Invalid syntax for Rn given"
            "LDM R15,{r1 r2}", Error "Invalid syntax for Rn given"
            "LDM R1!,{r1-r2}", parseLDMResult (Error "Register list cannot contain Rn if writeback is enabled")
            "LDM R1!,{}", parseLDMResult  (Error "Register list cannot be empty")
            "LDM R1!,{}", parseLDMResult  (Error "Register list cannot be empty")       
            "LDM R1!,{r9-r2}", parseLDMResult  (Error "Invalid reglist range \n")     
            "LDM R1!,{r9-r2,r1}", parseLDMResult  (Error "Invalid reglist range \n")     
            "LDM R1!,{r2-r22}", parseLDMResult  (Error "Invalid registers in reglist \n")     
            "LDM R1!,{r16}", parseLDMResult  (Error "Invalid registers in reglist \n")     
            "LDM R1!,{r13}", parseLDMResult  (Error "Register list cannot contain SP/R13")     
            "LDM R1!,{r12-r14}", parseLDMResult  (Error "Register list cannot contain SP/R13")     
            "LDM R1!,{r14-r15}", parseLDMResult  (Error "Register list cannot contain PC/R15 if it contains LR/R14 for LDM")
            "STM R1!,{r15}", parseResult "STM" "" R1 true  (Error "Register list cannot contain PC/R15 for STM")     
            "STM R1!,{r14-r15}", parseResult "STM" "" R1 true  (Error "Register list cannot contain PC/R15 for STM")   
        ]

[<Tests>]
let tD' = CommonTest.genTestData memVal [0ul..14ul]
let executeErrTest =
    MakeExErrTests "Execute Error Tests"
      [
        "LDM r1,{r2}", tD, "Address not found in memory" 
        "STM r1,{r2}", tD', "Address not divisible by 4" 
        "LDM r1,{r2}", tD', "Address not divisible by 4" 
        "LDMFD r0,{r2}", tD', "Address not found in memory" 
        "STM r0,{r2}", tD', "Invalid memory access requested, below 0x1000u" 
      ]

let testParas = {defaultParas with InitRegs = regVal}
[<Tests>]
let executeStoreTest = 
    testList "STM tests"
        [
            VisualUnitMemTest testParas "STMIA Test" "STMIA R0,{R4-R9}" memVal tD 6
            VisualUnitMemTest testParas "STMDB Test" "STMDB R1,{R4-R9}" memVal tD 6
            VisualUnitMemTest testParas "STMEA Test with Writeback" "STMEA R0!,{R4-R9}" memVal tD 6
            VisualUnitMemTest testParas "STMIA Test with Writeback" "STMIA R0!,{R4-R9}" memVal tD 6
            VisualUnitMemTest testParas "STMFD Test with Writeback" "STMFD R1!,{R4-R9}" memVal tD 6
            VisualUnitMemTest testParas "STMDB Test with Writeback" "STMDB R1!,{R4-R9}" memVal tD 6
        ]
let loadMemVal = [1ul;2ul;3ul;4ul;5ul;6ul]
let loadTD = CommonTest.genTestData loadMemVal regVal
[<Tests>]
let executeLoadTest = 
    testList "LDM tests"
        [
            VisualUnitMemTest testParas "LDMIA Test" "LDMIA R0,{R4-R9}" loadMemVal loadTD 6
            VisualUnitMemTest testParas "LDMEA Test" "LDMEA R1,{R4-R9}" loadMemVal loadTD 6
            VisualUnitMemTest testParas "LDMIA Test with Writeback" "LDMIA R0!,{R4-R9}" loadMemVal loadTD 6
            VisualUnitMemTest testParas "LDMFD Test with Writeback" "LDMFD R0!,{R4-R9}" loadMemVal loadTD 6
            VisualUnitMemTest testParas "LDMEA Test with Writeback" "LDMEA R1!,{R4-R9}" loadMemVal loadTD 6
            VisualUnitMemTest testParas "LDMDB Test with Writeback" "LDMDB R1!,{R4-R9}" loadMemVal loadTD 6
        ]