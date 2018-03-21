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




let MakeLabelTest name tList =
    let singleTest i (input,expected)  =
        testCase (sprintf "Label Test %s #%d" name i) <| fun () ->
        // let pConv p = pResultInstrMap Instr string p
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
        ]

let testParas = {defaultParas with InitRegs = regVal}
let numMem = 6          // number of memory addresses to check. R4-R9, hence 6 memory addresses to check
[<Tests>]
let executeStoreTest = 
    testList "Program tests"
        [
            VisualUnitMemTest testParas "STMIA Test" "STMIA R0,{R4-R9} \n LSR R4,R1,#2" memVal tD numMem
        ] 


        
        
// let loadMemVal = [1ul;2ul;3ul;4ul;5ul;6ul]
// let loadTD = {tD with MM = tMem loadMemVal}
// [<Tests>]
// let executeLoadTest = 
//     testList "LDM tests"
//         [
//             VisualUnitMemTest testParas "LDMIA Test" "LDMIA R0,{R4-R9}" loadMemVal loadTD 6
//             VisualUnitMemTest testParas "LDMEA Test" "LDMEA R1,{R4-R9}" loadMemVal loadTD 6
//             VisualUnitMemTest testParas "LDMIA Test with Writeback" "LDMIA R0!,{R4-R9}" loadMemVal loadTD 6
//             VisualUnitMemTest testParas "LDMFD Test with Writeback" "LDMFD R0!,{R4-R9}" loadMemVal loadTD 6
//             VisualUnitMemTest testParas "LDMEA Test with Writeback" "LDMEA R1!,{R4-R9}" loadMemVal loadTD 6
//             VisualUnitMemTest testParas "LDMDB Test with Writeback" "LDMDB R1!,{R4-R9}" loadMemVal loadTD 6
//         ]