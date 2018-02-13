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

// WHAT TYPES OF InstrLine LOOK LIKE
// LDR{B}{cond} RDest, [RSrc]               -> RSrc contains an add, loads contents from that add to RDest
// LDR{B}{cond} RDest, [RSrc {, OFFSET}]    -> Offset
// LDR{B}{cond} RDest, [RSrc, OFFSET]!      -> Pre-indexed offset
// LDR{B}{cond} RDest, [RSrc], OFFSET       -> Post-indexed offset
// STR{B}{cond} RD, [RS]                    -> RS contains an add, stores content from RD to that add
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
    
let makeLS typeLS ls suffix = 
    let operandLst = (ls.Operands).Split(',') |> Array.toList
    let getSuffix suffStr = 
        match suffStr with
        | "B" -> Some B
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

    let instrDummy = {
        Instr=typeLS; Type=getSuffix suffix ; RContents=regNames.[operandLst.[0]] ; 
        RAdd=regNames.[operandLst.[0]] ; Offset=None }       // RAdd is dummy 
    match operandLst.Length with
    | 2 -> 
        let rn = operandLst.[1]
        let rnVal = getRn rn None
        {instrDummy with RAdd = rnVal}
    | 3 -> 
        let rn = operandLst.[1]
        let op2 = operandLst.[2]
        let offsetVal = getOffsetVal op2
        match op2.EndsWith("!"), op2.Contains("]"), op2.EndsWith("]"), op2.Contains("[") with
        | true, true, false, false -> 
            let rnVal = getRn rn (Some PreIndexed)
            {instrDummy with RAdd=rnVal ; Offset=Some (offsetVal,PreIndexed)}
        | false, false, false, false -> 
            let rnVal = getRn rn (Some PostIndexed)
            {instrDummy with RAdd=rnVal ; Offset=Some (offsetVal,PostIndexed)}
        | false, true, true, false -> 
            let rnVal = getRn rn (Some Normal)
            {instrDummy with RAdd=rnVal ; Offset=Some (offsetVal,Normal)}
        | _ , _, _ , _ -> failwithf "Incorrect way of setting offset"    
    | _ -> failwithf "Incorrect number of operands"    

let makeLDR lineASM suffix = makeLS LDR lineASM suffix 
let makeSTR lineASM suffix = makeLS STR lineASM suffix 
                  
/// main function to parse a line of assembler
/// ls contains the line input
/// and other state needed to generate output
/// the result is None if the opcode does not match
/// otherwise it is Ok Parse or Error (parse error string)
let parse (ls: LineData) : Result<Parse<InstrLine>,string> option =
    let parse' (instrC, (root,suffix,pCond)) =
        match instrC with
        | MEM ->
            let parsedInstr = 
                match root with
                | "LDR" -> makeLDR ls suffix
                | "STR" -> makeSTR ls suffix
                | _ -> failwithf "What? InstrC = MEM but root != LDR or STR" // top level code not expecting Result here
            Ok { PInstr= parsedInstr ; PLabel = None ; PSize = 4u; PCond = pCond }
        | _ -> Error "Wrong instruction class passed to the Memory module"        
    
    Map.tryFind ls.OpCode opCodes
    |> Option.map parse'



/// Parse Active Pattern used by top-level code
let (|IMatch|_|)  = parse