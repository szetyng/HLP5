module CommonTest 

open CommonData
open Expecto
open VisualTest.VCommon
open VisualTest.VData
open VisualTest.Visual
open VisualTest.VTest

/// Generate test data for simulation
let genTestData memVal regVal: DataPath<'INS> =
    let tRegs (x:uint32 list) =  [0..14] |> List.map(fun n-> (register n, x.[n])) |> Map.ofList
    let tMem memVal: MachineMemory<'INS> = 
        let n = List.length memVal |> uint32
        let memBase = defaultParas.MemReadBase
        let waList = List.map WA [memBase..4u..(memBase+ 4u*(n-1u))]
        let valList = List.map DataLoc memVal
        List.zip waList valList |> Map.ofList     
    let testData = { 
                Fl = {N = false; C =false; Z = false; V = false}
                Regs = tRegs regVal
                MM = tMem memVal
             }
    testData   

/// Given a list of memory values and the base address, store them by writing assembler
let STOREALLMEM memVals memBase = 
    let n = List.length memVals |> uint32
    let mAddrList = [memBase..4u..(memBase + (n-1u)*4u)]
    List.zip mAddrList memVals
    |> List.map (fun (a,v) -> STORELOC v a)
    |> String.concat ""

/// Split a program by line breaks
let splitIntoLines ( line:string ) =
                line.Split( ([|'\n'|] : char array), 
                    System.StringSplitOptions.RemoveEmptyEntries)

/// Run VisUAL, initialize using paras and memVal, and run src, then read 13 words back to registers during postlude.
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


/// Run a unit test and compare the results between actual result (VisUAL) and expected (execute function).
let VisualUnitMemTest paras name src memVal tD numMem  =
    testCase name <| fun () ->
        let outActual = RunVisualMem memVal paras  src
        let outExpected = 
            let res = CommonTop.fullExecute (Ok tD) (splitIntoLines src) 
            match res with
            | Ok x -> x
            | Error e -> failwithf "Error"

        let memActual:MachineMemory<'INS>= 
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

