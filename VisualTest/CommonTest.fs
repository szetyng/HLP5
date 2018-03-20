module CommonTest 

open CommonData
open CommonLex
open MultiR
open Expecto
open VisualTest.VCommon
open VisualTest.VData
open VisualTest.Visual
open VisualTest.VTest


/// Generate testing data for execution
let memVal = []
let regVal = [defaultParas.MemReadBase;4120u;4144u] @ [3ul..14ul] 
let tRegs (x:uint32 list) =  [0..14] |> List.map(fun n-> (register n, x.[n])) |> Map.ofList
let tMem memVal: MachineMemory<Instr> = 
    let n = List.length memVal |> uint32
    let memBase = defaultParas.MemReadBase
    let waList = List.map WA [memBase..4u..(memBase+ 4u*(n-1u))]
    let valList = List.map DataLoc memVal
    List.zip waList valList |> Map.ofList     
let tD = {
            Fl = {N = false; C =false; Z = false; V = false}
            Regs = tRegs regVal
            MM = tMem memVal
         }

// given a list of memory values and the base address, store them by writing assembler
let STOREALLMEM memVals memBase = 
    let n = List.length memVals |> uint32
    let mAddrList = [memBase..4u..(memBase + (n-1u)*4u)]
    List.zip mAddrList memVals
    |> List.map (fun (a,v) -> STORELOC v a)
    |> String.concat ""
let splitIntoLines ( line:string ) =
                line.Split( ([|'\n'|] : char array), 
                    System.StringSplitOptions.RemoveEmptyEntries)

// Run VisUAL, initialize using paras and memVal, and run src, then read 13 words back to registers during postlude.
let RunVisualMem memVal paras src = 
    let memPrelude = 
        STOREALLMEM memVal paras.MemReadBase +
        SETALLREGS paras.InitRegs +
        "\r\n"
    let memPostlude =
        READMEMORY paras.MemReadBase 
    let res = RunVisual {paras with Prelude = memPrelude; Postlude = memPostlude} src
    match res with
    | Error e -> failwithf "Error %A" e
    | Ok vso -> vso



// let executeErr asm tD: string = 
//     match parseAndExecute asm tD with
//     | Error e -> e
//     | _ -> failwithf "Not testing for correctness here"

/// Run a unit test and compare the results between actual result (VisUAL) and expected (execute function).
let VisualUnitMemTest paras name src memVal tD numMem  =
    testCase name <| fun () ->
        let outActual = RunVisualMem memVal paras  src
        let outExpected = 
            let res = CommonTop.fullExecute (Ok tD) (splitIntoLines src) 
            match res with
            | Ok x -> x
            | Error e -> failwithf "Error"

        let memActual:MachineMemory<Instr>= 
            let memVal = outActual.State.VMemData
            // reverse is necessary to get values loaded using LDMIA, so reverse the stack to get the original  
            let memValFromRegs =  List.map CommonData.DataLoc memVal |> List.rev |> List.take numMem
            let n = List.length memValFromRegs |> uint32
            let memAddr = List.map WA [paras.MemReadBase..4u..(paras.MemReadBase + (n-1u)*4u)]
            
            List.zip memAddr memValFromRegs
            |> Map.ofList
        
        Expecto.Expect.equal memActual outExpected.MM <|    
        sprintf "Memory outputs>\n%A\n<don't match expected outputs, src=%s" (outActual.State.VMemData|> List.rev) src
    
        let regActual =
            outActual.Regs
            |> List.map (fun (R n, r) -> register n, uint32 r)
            |> List.sort
            |> List.take 15
            |> Map.ofList

        Expecto.Expect.equal regActual outExpected.Regs <|
        sprintf "Register outputs>\n%A\n<don't match expected outputs, src=%s" outActual.Regs src


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