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

let makeParseLDRTests listIOpairs = makeParseLSTestList "LDR and STR parse tests" listIOpairs 
//let makeParseSTRTests listIOpairs = makeParseLSTestList "STR parse tests" listIOpairs 

[<Tests>]
let t1 = 
    makeParseLDRTests
        [
            "STRB R10, [R15, #5]!" , {Instr=STR ; Type=Some B; RContents=R10; RAdd=R15 ; Offset=Some (Literal 5u, PreIndexed)}
            "LDR R4, [R8], #3", {Instr=LDR ; Type=None; RContents=R4; RAdd=R8 ; Offset=Some (Literal 3u, PostIndexed)}
            "LDRB R7, [R11, #11]", {Instr=LDR ; Type=Some B; RContents=R7; RAdd=R11 ; Offset=Some (Literal 11u, Memory.Normal)} 
            "STR R5, [R2]", {Instr=STR ; Type=None; RContents=R5; RAdd=R2 ; Offset=None}
            // SHOULD FAIL
            //"LDR R10, [R15, ", {Instr=LDR ; Type=None; RContents=R10; RAdd=R15 ; Offset=None} //failing. Good? 
            

            "LDR R10, [R15" , {Instr=LDR ; Type=None; RContents=R10; RAdd=R15 ; Offset=None} //ERROR, NO BRACKETS
            "LDR R10, R15]", {Instr=LDR ; Type=None; RContents=R10; RAdd=R15 ; Offset=None} // ERROR, NO BRACKETS
            "LDR R10, R15", {Instr=LDR ; Type=None; RContents=R10; RAdd=R15 ; Offset=None} // ERROR, NO BRACKETS
            // "LDR R10, [R15, R2!]", {Instr=LDR ; Type=None; RContents=R10; RAdd=R15 ; Offset=None}
            
            
            
            // SHOULD PASS
            //"ldrb r10, [r15, #4]", {Instr=LDR ; Type=Some B; RContents=R10; RAdd=R15 ; Offset=Some (Literal 4u, Memory.Normal)} //failing
        ]    


[<EntryPoint>]
let main argv =
    // printfn "%A" argv
    printfn "Testing LDR/STR parsing!"
    Expecto.Tests.runTestsInAssembly Expecto.Tests.defaultConfig [||] |> ignore
    Console.ReadKey() |> ignore  
    0 // return an integer exit code