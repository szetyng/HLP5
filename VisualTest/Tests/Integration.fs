module Integration

open CommonData
open CommonTest
open Expecto
open VisualTest.VCommon
open VisualTest.VTest

/// Generate testing data for execution
let memVal = []
let regVal = [defaultParas.MemReadBase;4120u;4144u] @ [3ul..14ul] 
let tD = genTestData memVal regVal   



/// Make Expecto test for label checking in Symbol Table 
let MakeLabelTest name tList =
    let singleTest i (input,expected)  =
        testCase (sprintf "Label Test %s #%d" name i) <| fun () ->
        let actual = CommonTop.multiParseLine None (WA 0ul) input |> snd

        Expecto.Expect.equal actual expected (sprintf "Test parsing of %A" input) 
    tList
    |>List.indexed
    |>List.map (fun (x,y) -> singleTest x y)
    |> Expecto.Tests.testList name

let labelMap = 
    Map.ofList [ 
        "LABEL",0u ; "LABEL1",4u 
    ] 

let labelMap1 = 
    Map.ofList [ 
        "LABEL",0u ; "LABEL1",8u 
    ] 

[<Tests>]
let labelTest =
    MakeLabelTest "Single Pass Test"
        [
            [|"LABEL LSR R4,R1,#2"; "LABEL1 STM R1, {R1,R2}"|], Some labelMap
            [|"LABEL LSR R4,R1,#2"; "STM R1, {R1,R2}"; "LABEL1 STM R1, {R1,R2}"|], Some labelMap1
            [|"LDR R2, [R8]"; "STRB R5, [R0], #0x12"|], None
        ]

/// Test with empty memory 
let testProgram1 = "STMIA R0,{R4-R9} \n LSR R4,R1,#2" 
let testProgram2 = "ldr r2, [r0] \n str r7, [r0,#8]! \n lsl r5,r1,#4"
let testName = "Multi-Line program test"
let testParas = {defaultParas with InitRegs = regVal}
let numMem = 6          // number of memory addresses to check. R4-R9, hence 6 memory addresses to check
[<Tests>]
let testCaseOne = 
    testList "Program tests"
        [
            VisualUnitMemTest testParas testName testProgram1 memVal tD numMem
        ] 

/// Test with loaded memory          
let loadMemVal = [1ul;2ul;3ul;4ul;5ul;6ul]
let loadTD = genTestData loadMemVal regVal

[<Tests>]
let testCaseTwo = 
    testList "Program tests with memory"
        [
            VisualUnitMemTest testParas "Multi-Line program test 1" testProgram1 loadMemVal loadTD numMem
            VisualUnitMemTest testParas "Multi-Line program test 2" testProgram2 loadMemVal loadTD numMem
        ]