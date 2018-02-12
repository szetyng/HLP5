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
        | Some (Ok pa) -> pa.PInstr
        | _ -> failwithf "Please write proper tests"
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

let makeParseLDRTests listIOpairs = makeParseLSTestList "LDR parse tests" listIOpairs
let makeParseSTRTests listIOpairs = makeParseLSTestList "STR parse tests" listIOpairs

[<Tests>]
let t1 = 
    makeParseLDRTests
        [
            "LDR R10, [R15, #5]!" , {Instr=LDR ; Type=None; RContents=R10; RAdd=R15 ; Offset=Some (Literal 5u, PreIndexed)}
            "LDR R10, [R15" , {Instr=LDR ; Type=None; RContents=R10; RAdd=R15 ; Offset=None} //ERROR, NO BRACKETS
            
        ]    


[<EntryPoint>]
let main argv =
    // printfn "%A" argv
    printfn "Testing LDR/STR parsing!"
    Expecto.Tests.runTestsInAssembly Expecto.Tests.defaultConfig [||] |> ignore
    Console.ReadKey() |> ignore  
    0 // return an integer exit code


