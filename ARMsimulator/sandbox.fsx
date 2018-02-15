// for scripting purposes

#load "CommonData.fs"
#load "CommonLex.fs"
#load "DP.fs"
#load "Memory.fs"
#load "CommonTop.fs"
// #load "Test.fs"
open CommonData
open CommonLex
open DP
open Memory
open CommonTop
// open Test

let asmLine = "STR R10, [R15]"
// let splitIntoWords ( line:string ) =
//     line.Split( ([||] : char array), 
//         System.StringSplitOptions.RemoveEmptyEntries)

// let makeOperands words =
//     match words with
//     | opc :: operands -> String.concat "" operands
//     | _ -> failwithf "NO"

// let operands = splitIntoWords asmLine |> Array.toList |> makeOperands
// let opList = operands.Split(',') |> Array.toList
// let op2 = opList.[2]
// op2.Contains("!")

//let myMatch someLine = Memory.parse someLine
let myMatch (ld: LineData) : Result<Parse<Instr>,ErrInstr> option =
    let pConv fr fe p = pResultInstrMap fr fe p |> Some
    match ld with
    | Memory.IMatch pa -> pConv IMEM ERRIMEM pa
    | DP.IMatch pa -> pConv IDP ERRIDP pa
    | _ -> None

let parseLine (asmLine:string) =
    /// put parameters into a LineData record
    let makeLineData opcode operands = {
        OpCode=opcode
        Operands=String.concat "" operands
        Label=None
        LoadAddr = WA 0u // dummy
        SymTab = None // dummy
    }
    /// remove comments from string
    let removeComment (txt:string) =
        txt.Split(';')
        |> function 
            | [|x|] -> x 
            | [||] -> "" 
            | lineWithComment -> lineWithComment.[0]
    /// split line on whitespace into an array
    let splitIntoWords ( line:string ) =
        line.Split( ([||] : char array), 
            System.StringSplitOptions.RemoveEmptyEntries)
    /// try to parse 1st word, or 2nd word, as opcode
    /// If 2nd word is opcode 1st word must be label
    let matchLine words =
        let pNoLabel =
            match words with
            | opc :: operands -> 
                makeLineData opc operands 
                |> myMatch
            | _ -> None
        match pNoLabel, words with
        | Some pa, _ -> pa
        | None, label :: opc :: operands -> 
            match { makeLineData opc operands 
                    with Label=Some label} 
                  |> IMatch with
            | None -> 
                failwithf "error"
            | Some pa -> failwithf "error"
        | _ -> failwithf "error"
    asmLine
    |> removeComment
    |> splitIntoWords
    |> Array.toList
    |> matchLine

parseLine asmLine

// let onlyParseLine (asmLine:string) = 
// /// put parameters into a LineData record
//     let makeLineData opcode operands = {
//         OpCode=opcode
//         Operands=String.concat "" operands
//         Label=None
//         LoadAddr = WA 0u // dummy
//         SymTab = None // dummy
//     }
//     /// remove comments from string
//     let removeComment (txt:string) =
//         txt.Split(';')
//         |> function 
//             | [|x|] -> x 
//             | [||] -> "" 
//             | lineWithComment -> lineWithComment.[0]
//     /// split line on whitespace into an array
//     let splitIntoWords ( line:string ) =
//         line.Split( ([||] : char array), 
//             System.StringSplitOptions.RemoveEmptyEntries)
//     /// try to parse 1st word, or 2nd word, as opcode
//     /// If 2nd word is opcode 1st word must be label
//     let matchLine words =
//         let pNoLabel =
//             match words with
//             | opc :: operands -> 
//                 makeLineData opc operands 
//                 |> Memory.parse
//             | _ -> None
        
//         match pNoLabel with
//         | Some (Ok pa) -> pa.PInstr
//         | _ -> failwithf "Please write proper tests"
//     asmLine
//     |> removeComment
//     |> splitIntoWords
//     |> Array.toList
//     |> matchLine


// onlyParseLine asmLine

