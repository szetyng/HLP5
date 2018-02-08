module Memory 
//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

open CommonLex
open CommonData
open System.Runtime.Remoting.Metadata.W3cXsd2001

/// sample specification for set of instructions

// change these types as required

/// instruction (dummy: must change)
// type Instr =  {MemDummy: Unit}
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
        RContents: RName; // RDest
        RAdd: RName; // RSrc
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
    let getRName (srcStr:string) = regNames.[srcStr.Trim [|'[' ; ']'|] ] //maybe check for case where operand is wrong
    let getOffsetVal (valStr:string) = 
        match valStr with
        | dec when dec.Contains("#") -> dec.Trim [|'#' ; ']' ; '!' |] |> uint32 |> Literal
        | reg -> getRName reg |> Reg  
        | _ -> failwithf "Incorrect offset value"

    let instrDummy = {
        Instr=typeLS; Type=suffix ; RContents=regNames.[operandLst.[0]] ; // suffix option?
        RAdd=getRName operandLst.[1] ; Offset=None }        
    match operandLst.Length with
    | 2 -> instrDummy
    | 3 ->      
        let op2 = operandLst.[2]
        let offsetVal = getOffsetVal op2
        match op2.Contains("!"), op2.Contains("]") with
        | true, _ -> {instrDummy with Offset=Some (offsetVal,PreIndexed)}
        | false, false -> {instrDummy with Offset=Some (offsetVal,PostIndexed)}
        | false, true -> {instrDummy with Offset=Some (offsetVal,Normal)}
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
                | "LDR" -> makeLDR ls suffix //SUFFIX IS A STRING
                | "STR" -> makeSTR ls suffix
                | _ -> failwithf "What?"
            Ok { PInstr= parsedInstr ; PLabel = None ; PSize = 4u; PCond = pCond }
        | _ -> Error "Wrong instruction class passed to the Memory module"        
    
    Map.tryFind ls.OpCode opCodes
    |> Option.map parse'



/// Parse Active Pattern used by top-level code
let (|IMatch|_|)  = parse