// for scripting purposes

#load "CommonData.fs"
#load "CommonLex.fs"
#load "DP.fs"
#load "Memory.fs"
#load "CommonTop.fs"
#load "Test.fs"
open CommonData
open CommonLex
open DP
open Memory
open CommonTop
// open Test

let asmLine = "LDRB R10, [R15, #5]!"

let splitIntoWords ( line:string ) =
    line.Split( ([||] : char array), 
        System.StringSplitOptions.RemoveEmptyEntries)

let makeOperands words =
    match words with
    | opc :: operands -> String.concat "" operands
    | _ -> failwithf "NO"

let operands = splitIntoWords asmLine |> Array.toList |> makeOperands
let opList = operands.Split(',') |> Array.toList
let op2 = opList.[2]
op2.Contains("!")

