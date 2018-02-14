module Memory 
//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

open CommonLex
open CommonData

type InstrName = LDR | STR
type MemType = B
type OffsetVal = Literal of uint32 | Reg of RName
type OffsetType = Normal | PreIndexed | PostIndexed

type InstrLine = 
    {
        Instr: InstrName;
        Type: MemType option;
        RContents: RName;   // register holding the contents to be loaded/stored to/from (LDR/STR respectively)
        RAdd: RName;        // register holding the address
        Offset: (OffsetVal * OffsetType) option
    }

/// parse error (dummy, but will do)
type ErrInstr = string

let memSpec = {
    InstrC = MEM
    Roots = ["LDR";"STR"]
    Suffixes = [""; "B"]
}


/// map of all possible opcodes recognised
let opCodes = opCodeExpand memSpec
    
// makeLS needs to be InstrName -> LineData -> string -> Result<InstrLine,string>
let makeLS root ls suffix = 
    let typeLS =
        match root with
        | "LDR" | "ldr" -> Ok LDR
        | "STR" | "str" -> Ok STR
        | _ -> Error "Wrong root"
    let operandLst = (ls.Operands).Split(',') |> Array.toList
    let getSuffix suffStr = 
        match suffStr with
        | "B" | "b" -> Some B
        | "" -> None
        | _ -> failwithf "Incorrect suffix for LDR/STR"
    let getRName (srcStr:string) = regNames.[srcStr.Trim [|'[' ; ']'|] ] //maybe check for case where operand is wrong
    let getOffsetVal (valStr:string) = 
        match valStr with
        | dec when dec.Contains("#") -> dec.Trim [|'#' ; ']' ; '!' |] |> uint32 |> Literal //todo: other number bases
        | reg -> getRName reg |> Reg  
        | _ -> failwithf "Incorrect offset value"
    let getRn (reg:string) offset = 
        match reg.StartsWith("["), reg.EndsWith("]"), offset with
        | true, true, None -> getRName reg
        | true, false, Some PreIndexed | true, false, Some Normal -> getRName reg
        | true, true, Some PostIndexed-> getRName reg
        | _, _, _ -> failwithf "Incorrect formatting"

    // Errors from (getSuffix suffix), getRName RAddthing, getOffsetVal valStr, getRn?
    let resSuffix = getSuffix suffix |> Ok
    let rnVal = 
        let rn = operandLst.[1]
        match operandLst.Length with
        | 2 ->     
            Ok (getRn rn None)
        | 3 -> 
            let op2 = operandLst.[2]
            match op2.EndsWith("!"), op2.Contains("]"), op2.EndsWith("]"), op2.Contains("[") with
            | true, true, false, false -> getRn rn (Some PreIndexed) |> Ok
            | false, false, false, false -> getRn rn (Some PostIndexed) |> Ok
            | false, true, true, false -> getRn rn (Some Normal) |> Ok
            | _ , _, _ , _ -> Error "rnVal error" //"Incorrect way of setting offset"                  
        | _ -> Error "rnVal error"        
    // result.bind this shit
    let offsetVal = 
        match operandLst.Length with
        | 2 -> Ok None
        | 3 ->
            let op2 = operandLst.[2]
            let value = getOffsetVal op2
            match op2.EndsWith("!"), op2.Contains("]"), op2.EndsWith("]"), op2.Contains("[") with
            | true, true, false, false -> Some (value, PreIndexed) |> Ok
            | false, false, false, false -> Some (value, PostIndexed) |> Ok
            | false, true, true, false -> Some (value, Normal) |> Ok
            | _ , _, _ , _ -> Error "offset error"//"Incorrect way of setting offset"                  
        | _ -> Error "IDK" 


    let instrDummy = {
        Instr=LDR; Type=None ; RContents=regNames.[operandLst.[0]] ; 
        RAdd=regNames.[operandLst.[0]] ; Offset=None }       // RAdd is dummy 

    match typeLS, resSuffix, rnVal, offsetVal with
    | Ok ls, Ok suff, Ok regVal, Ok offsetVal -> Ok {instrDummy with Instr=ls ; Type=suff ; RAdd=regVal ; Offset=offsetVal}
    | Error _,_,_,_ | _,Error _,_,_ | _,_,Error _,_ | _,_,_,Error _ -> Error "Incorrect formatting"

// let makeLDR lineASM suffix = makeLS LDR lineASM suffix 
// let makeSTR lineASM suffix = makeLS STR lineASM suffix 
                  
/// main function to parse a line of assembler
/// ls contains the line input
/// and other state needed to generate output
/// the result is None if the opcode does not match
/// otherwise it is Ok Parse or Error (parse error string)
let parse (ls: LineData) : Result<Parse<InstrLine>,string> option =
    let parse' (instrC, (root,suffix,pCond)) =
        match instrC with
        | MEM ->
            match makeLS root ls suffix with
            | Ok parsedInstr -> Ok { PInstr= parsedInstr ; PLabel = None ; PSize = 4u; PCond = pCond }
            | Error e -> Error e
        | _ -> Error "Wrong instruction class passed to the Memory module"        
    
    Map.tryFind ls.OpCode opCodes
    |> Option.map parse'



/// Parse Active Pattern used by top-level code
let (|IMatch|_|)  = parse


// PIPELINE SHIT***********************************************************************************************