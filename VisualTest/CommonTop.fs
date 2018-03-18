////////////////////////////////////////////////////////////////////////////////////
//      Code defined at top level after the instruction processing modules
////////////////////////////////////////////////////////////////////////////////////
module CommonTop 

open CommonLex
open CommonData

/// allows different modules to return different instruction types
type Instr =
    | ISINGMEM of SingleR.Instr 
    | IMULTIMEM of MultiR.Instr
    | ISHIFT of Shift.Instr


/// Note that Instr in Mem and DP modules is NOT same as Instr in this module
/// Instr here is all possible isntruction values combines with a D.U.
/// that tags the Instruction class
/// Similarly ErrInstr
/// Similarly IMatch here is combination of module IMatches
let IMatch (ld: LineData) : Result<Parse<Instr>,string> option =
    let pConv fr fe p = pResultInstrMap fr fe p |> Some
    match ld with
    | SingleR.IMatch pa -> pConv ISINGMEM string pa
    | MultiR.IMatch pa -> pConv IMULTIMEM string pa
    | Shift.IMatch pa -> pConv ISHIFT string pa
    | _ -> None

let IExecute (i:Instr) (d:DataPath<'INS>):Result<DataPath<'INS>,string> =
    match i with
    | ISHIFT x -> Shift.execute x d
    | IMULTIMEM x -> MultiR.execute x d
    | ISINGMEM x -> SingleR.execute x d

type CondInstr = Condition * Instr

let parseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmLine:string) =
    /// put parameters into a LineData record
    let makeLineData opcode operands = {
        OpCode=opcode
        Operands=String.concat "" operands
        Label=None
        LoadAddr = loadAddr
        SymTab = symtab
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
                |> IMatch
            | _ -> None
        
        match pNoLabel, words with
        | Some pa, _ -> pa
        | None, label :: opc :: operands -> 
            match { makeLineData opc operands 
                    with Label=Some label} 
                  |> IMatch with
            | None -> 
                Error (sprintf "Unimplemented instruction %s" opc)
            | Some pa -> pa
        | _ -> Error (sprintf "Unimplemented instruction %A" words)
    asmLine
    |> removeComment
    |> splitIntoWords
    |> Array.toList
    |> matchLine

let multiParseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmMultiLine:string []): Result<Parse<Instr>,string>list =
    /// put parameters into a LineData record
    let makeLineData opcode operands = {
        OpCode=opcode
        Operands=String.concat "" operands
        Label=None
        LoadAddr = loadAddr
        SymTab = symtab
    }
    /// split multiline separated by \n into an array
    /// each element of the array is a newline
    let splitIntoLines ( line:string ) =
                line.Split( ([|'\n'|] : char array), 
                    System.StringSplitOptions.RemoveEmptyEntries)
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
    let secondPass data =
        match data |> IMatch with
        | Some pa -> pa
        | None -> failwithf "idk"

    let firstPass prevLD src = 
        let currAddr = 
            match prevLD.LoadAddr with
            | WA a -> a
        let currSymTab = prevLD.SymTab        
        match src with
        | label :: opc :: operands ->
            match Map.tryFind label SingleR.opCodes with
            | Some _ -> {makeLineData opc operands 
                            with LoadAddr=WA currAddr}, {prevLD with LoadAddr=WA(currAddr+4u)}
            | None -> 
                let newSymTab = 
                    match currSymTab,currAddr with
                    | None, a -> Some (Map.ofList [label,a])
                    | Some s, a -> Some (Map.add label a s)
                {makeLineData opc operands
                    with LoadAddr=WA currAddr;Label=Some label;SymTab=newSymTab}, {prevLD with LoadAddr=WA(currAddr+4u);SymTab=newSymTab}                        

    let dummyLD = {LoadAddr=loadAddr ; Label=None ; SymTab=None ; OpCode="" ; Operands=""}                 
    let asmSplitLineSplitWords = 
        asmMultiLine
        //|> splitIntoLines  
        |> Array.toList 
        |> List.map (removeComment >> splitIntoWords >> Array.toList)
    let listLineData, finalLineData = List.mapFold firstPass dummyLD asmSplitLineSplitWords 
    let listLineDataWSymTab = List.map (fun d -> {d with SymTab=finalLineData.SymTab}) listLineData
    List.map secondPass listLineDataWSymTab

/// Assume that program starts at word address 0x00, each instruction is of size 4u
let parseAndExecute tD asm= 
    let parsedResList = multiParseLine None (WA 0ul) asm
    let exec tD parsedRes = 
        match parsedRes, tD with
        | Ok x, Ok d -> IExecute x.PInstr d
        | Error e, _ -> Error e
        | _, Error e -> Error e
    List.fold exec tD parsedResList
