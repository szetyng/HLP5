////////////////////////////////////////////////////////////////////////////////////
//      Code defined at top level after the instruction processing modules
////////////////////////////////////////////////////////////////////////////////////
module CommonTop 

open CommonLex
open CommonData
open SingleR

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

/// All possible opcodes accepted by the assembler
let opCodes = 
    Map.fold (fun newMap k v -> Map.add k v newMap) SingleR.opCodes MultiR.opCodes
    |> Map.fold (fun newMap k v -> Map.add k v newMap) Shift.opCodes



/// Accepts an array of multiple instruction lines stored as strings
/// Does a two pass parsing
let multiParseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmMultiLine:string []) =
// let multiParseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmMultiLine:string []): Result<Parse<Instr>,string>list =
    /// put parameters into a LineData record
    let makeLineData opcode operands = {
        OpCode=opcode
        Operands=String.concat "" operands
        Label=None
        LoadAddr = loadAddr
        SymTab = symtab
    }
    let makeUpper (txt:string) : string = txt.ToUpper()
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

    /// fully parses the LineData 
    /// sends the LineData to 
    let secondPass data : Result<Parse<Instr>,string> =
        match data |> IMatch with
        | Some pa -> pa
        | None -> Error "Instruction not yet implemented"

    /// prevLD: to be threaded through the list to keep track of address & symbol table
    /// src: a single line of the instruction, stored as a list of strings
    /// processes each line into a LineData with relevant label, address and a continually updated symbol table
    /// second LineData in output tuple is the processed prevLD, holding the final symbol table
    let firstPass (prevLD:LineData) (src:string list) = 
        /// address of this line of instruction
        /// will be incremented by 4 at the end
        /// further implementation for instructions with different types of memory addresses to be included
        let (WA currAddr) = prevLD.LoadAddr
        /// current symbol table of the program
        let currSymTab = prevLD.SymTab        
        match src with
        // 2nd word is an opcode, so 1st word is a label
        | label :: opc :: operands ->
            match Map.tryFind opc opCodes with          
            | Some _ -> 
                /// updated symbol table with the label and address of this line included in it
                let newSymTab =  
                    match currSymTab,currAddr with
                    | None, a -> Some (Map.ofList [label,a])
                    | Some s, a -> Some (Map.add label a s)
                {makeLineData opc operands
                    with LoadAddr=WA currAddr;Label=Some label;SymTab=newSymTab}, 
                    {prevLD with LoadAddr=WA(currAddr+4u);SymTab=newSymTab}                
            | None ->  
                match src with
                // 1st word is an opcode, there are no labels
                | opc' :: operands' ->
                    match Map.tryFind opc' opCodes with
                    | Some _ -> {makeLineData opc' operands'
                                        with LoadAddr=WA currAddr}, {prevLD with LoadAddr=WA(currAddr+4u)}
                    | _ -> failwithf "Instruction not yet implemented"
                | _ -> failwithf "Instruction not yet implemented"                                                                   
        | _ -> failwithf "Instruction not yet implemented"                                       

    /// LineData dummy to keep track of address and symbol table
    let dummyLD = {LoadAddr=loadAddr ; Label=None ; SymTab=None ; OpCode="" ; Operands=""}        
    /// list of list of string
    /// each line is an element of the outer list 
    /// each word in the line is an element of the inner list
    let asmSplitLineSplitWords = 
        asmMultiLine  
        |> Array.toList 
        |> List.map (makeUpper >> removeComment >> splitIntoWords >> Array.toList)    
    let listLineData, finalLineData = List.mapFold firstPass dummyLD asmSplitLineSplitWords   

    // To check symbolTable, uncomment the following line
    // printfn "symbolTable = %A" finalLineData.SymTab

    // Update all line data with the correct symbol table
    // Pass each line's LineData to module-specific parsers
    let res = List.map ((fun d -> {d with SymTab=finalLineData.SymTab}) >> secondPass) listLineData
    (res,finalLineData.SymTab)
/// Accepts a single line of instruction and store it in an array
/// To pass to multiParseLine
let parseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmLine:string) =
    multiParseLine symtab loadAddr (Array.create 1 asmLine)
    |> fst |> List.exactlyOne

/// Accepts an array of multiple instruction lines stored as strings
/// Parses each line in a two pass assembler to get multiple Parse types
/// Executes each line on tD consecutively
let fullExecute tD asm = 
    let parsedResList = multiParseLine None (WA 0ul) asm |> fst
    let exec tD parsedRes = 
        match parsedRes, tD with
        | Ok x, Ok d -> IExecute x.PInstr d
        | Error e, _ -> Error e
        | _, Error e -> Error e
    List.fold exec tD parsedResList
