module Memory 
//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

open CommonLex
open CommonData
open System.Xml

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
    
// makeLS needs to return Result<InstrLine,string>
let makeLS root ls suffix = 
    let typeLS =
        match root with
        | "LDR" -> LDR
        | "STR" -> STR
        | _ -> failwithf "What? Wrong root" // won't happen bc root is expanded from memSpec
    let getSuffix suffStr = 
        match suffStr with
        | "B" -> Some B
        | "" -> None
        | _ -> failwithf "What? Incorrect suffix for LDR/STR"  // wont' happen bc suffix is expanded from memSpec
    let resSuffix = getSuffix suffix     

    // no more failwithf's, very possible for errors here
    let operandLst = (ls.Operands).Split(',') |> Array.toList    
    let getRName (regStr:string) = 
        let regNameStr = regStr.Trim [|'[' ; ']'|]
        match Map.containsKey regNameStr regNames with
        | true -> regNames.[regNameStr] |> Ok
        | false -> Error "Invalid register name"
    let getOffsetVal (valStr:string) = 
        match valStr with
        | dec when dec.Contains("#") -> 
            dec.Trim [|'#' ; ']' ; '!' |] 
            |> uint32 |> Literal |> Ok //todo: other number bases
        | reg -> 
            match reg.Trim [|']' ; '!'|] |> getRName with 
            | Ok r -> Ok (Reg r)
            | Error e -> Error e
        | _ -> failwithf "Incorrect offset value" // will never happen?
    let getRn ((reg:string),offset) = 
        match reg.StartsWith("["), reg.EndsWith("]"), offset, getRName reg with
        | true, true, None, Ok r  -> Ok r
        | true, false, Some PreIndexed, Ok r | true, false, Some Normal, Ok r -> Ok r
        | true, true, Some PostIndexed, Ok r-> Ok r
        | _, _, _, Error e -> Error e
        | _ -> Error "Incorrect formatting of offset" 

    let rContents = Result.bind getRName (Ok operandLst.[0])
    let rnVal = 
        let rn = operandLst.[1]
        match operandLst.Length with
        | 2 ->     
            Ok (rn,None) |> Result.bind getRn
        | 3 -> 
            let op2 = operandLst.[2]
            match op2.EndsWith("]!"), op2.EndsWith("]"), op2.Contains("!"), op2.Contains("[") with
            | true, false, true, false -> Ok (rn,(Some PreIndexed)) |> Result.bind getRn
            | false, false, false, false -> Ok (rn,(Some PostIndexed)) |> Result.bind getRn
            | false, true, false, false -> Ok (rn,(Some Normal)) |> Result.bind getRn
            | _ -> Error "rnVal error" //"Incorrect way of setting offset"                  
        | _ -> Error "rnVal error"        
    let offsetVal = 
        match operandLst.Length with
        | 2 -> Ok None
        | 3 ->
            let op2 = operandLst.[2]
            let value = Ok op2 |> Result.bind getOffsetVal 
            match op2.EndsWith("]!"), op2.EndsWith("]"), op2.Contains("!"), value with
            | true, false, true, Ok v -> Some (v, PreIndexed) |> Ok
            | false, false, false, Ok v -> Some (v, PostIndexed) |> Ok
            | false, true, false, Ok v -> Some (v, Normal) |> Ok
            | _, _, _, Error e -> Error e
            | _ -> Error "Formatting offset error"                
        | _ -> Error "Too many operands" 

    let instrDummy = {
        Instr=typeLS; Type=resSuffix ; RContents=R1 ; 
        RAdd=R1 ; Offset=None }       // RAdd is dummy 

    match rContents, rnVal, offsetVal with
    | Ok rc, Ok ra, Ok offsetVal -> Ok {instrDummy with RContents=rc ; RAdd=ra ; Offset=offsetVal}
    | _ -> Error "Incorrect formatting"
    //| Error _,_,_ | _,Error _,_ | _,_,Error _ -> Error "Incorrect formatting"
        
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
//**********************************************Execution************************************************************//
let executeMemInstr (ins:InstrLine) (data: DataPath<InstrLine>) =
    let instrName = ins.Instr
    let off = ins.Offset
    let regAdd = ins.RAdd
    let regCont = ins.RContents
    let macMem = data.MM
    let macRegs = data.Regs

    let executeLDR memLoc d = 
        // add: address stored in the regSource
        // memLoc: address containing value we want to use to load to regDest (ie consider offset)

        // get value we want to load to regDest
        let payload = 
            match macMem.[WA memLoc] with
            | DataLoc d -> d
            | Code _ -> failwithf "Should not access this memory location"
        // update registers with the payload (ie load the contents)
        let newRegs = 
            match off with
            | None | Some (_, Normal) -> macRegs.Add (regCont,payload)
            | Some(_, PreIndexed) | Some(_, PostIndexed) ->
                macRegs.Add (regCont,payload)
                |> Map.add regAdd memLoc // update offset if nec    
        {d with Regs=newRegs}   
    let executeSTR add memLoc d =
        // add: address where we want to store contents to
        let payload = macRegs.[regCont]
        // updating memory
        let newMem = macMem.Add ((WA add) , (DataLoc payload))
        let newRegs = 
            match off with 
            | Some(_, PreIndexed) | Some(_, PostIndexed) -> macRegs.Add (regAdd, memLoc)
            | _ -> macRegs
        {d with Regs=newRegs ; MM=newMem}        
    let executeLS typeLS d = 
        let add = macRegs.[regAdd]
        let memLoc = 
            match off with
            | None -> add
            | Some (Literal v, _) -> add + v
            | Some (Reg _, _) -> add // FIX THIS 
        match typeLS with
        | LDR -> executeLDR memLoc d
        | STR -> executeSTR add memLoc d

    executeLS instrName data
