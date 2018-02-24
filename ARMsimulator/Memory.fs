module Memory 
//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

open CommonLex
open CommonData
open System.Xml
open VisualTest
open System

type InstrName = LDR | STR
type MemType = B
type OffsetVal = Literal of uint32 | Reg of RName
type OffsetType = Normal | PreIndexed | PostIndexed

type Instr = 
    {
        InstrN: InstrName;
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
let makeLS (root:string) ls suffix = 
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
    /// converts string to RName
    let getRName (regStr:string) = 
        let regNameStr = regStr.Trim [|'[' ; ']'|]
        match Map.containsKey regNameStr regNames with
        | true -> regNames.[regNameStr] |> Ok
        | false -> Error "Invalid register name"
    /// converts string to OffsetVal 
    /// can be literal or stored in register   
    let getOffsetVal (valStr:string) = 
        match valStr with
        | dec when dec.Contains("#") -> 
            dec.Trim [|'#' ; ']' ; '!' |] 
            |> uint32 |> Literal |> Ok //todo: other number bases
        | reg -> 
            match reg.Trim [|']' ; '!'|] |> getRName with 
            | Ok r -> Ok (Reg r)
            | Error e -> Error e
        | _ -> Error "Incorrect offset format" //ie forgetting '#'
    /// converts string to RName, to get RAdd
    let getRn ((reg:string),offset) = 
        match reg.StartsWith("["), reg.EndsWith("]"), offset, getRName reg with
        | true, true, None, Ok r  -> Ok r
        | true, false, Some PreIndexed, Ok r | true, false, Some Normal, Ok r -> Ok r
        | true, true, Some PostIndexed, Ok r-> Ok r
        | _, _, _, Error e -> Error e
        | _ -> Error "Incorrect offset format" 

    let rContents = Result.bind getRName (Ok operandLst.[0])
    // RAdd
    let rnVal = 
        let rn = operandLst.[1]
        match operandLst.Length with
        // two operands represent no offset
        | 2 ->     
            Ok (rn,None) |> Result.bind getRn
        // three operands represent some kind of offset        
        | 3 -> 
            let op2 = operandLst.[2] // represents the offset itself
            match op2.EndsWith("]!"), op2.EndsWith("]"), op2.Contains("!"), op2.Contains("[") with
            | true, false, true, false -> Ok (rn,(Some PreIndexed)) |> Result.bind getRn
            | false, false, false, false -> Ok (rn,(Some PostIndexed)) |> Result.bind getRn
            | false, true, false, false -> Ok (rn,(Some Normal)) |> Result.bind getRn
            | _ -> Error "Incorrect rnVal format" //"Incorrect way of setting offset"                  
        | _ -> Error "Incorrect formatting"        
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
        InstrN=typeLS; Type=resSuffix ; RContents=R1 ; 
        RAdd=R1 ; Offset=None }      

    match rContents, rnVal, offsetVal with
    | Ok rc, Ok ra, Ok offsetVal -> Ok {instrDummy with RContents=rc ; RAdd=ra ; Offset=offsetVal}
    | _ -> Error "Incorrect formatting"

        
/// main function to parse a line of assembler
/// ls contains the line input
/// and other state needed to generate output
/// the result is None if the opcode does not match
/// otherwise it is Ok Parse or Error (parse error string)
let parse (ls: LineData) : Result<Parse<Instr>,string> option =
    let parse' (instrC, (root,suffix,pCond)) =
        match instrC with
        | MEM ->
            match makeLS root ls suffix with
            | Ok parsedInstr -> Ok { PInstr= parsedInstr ; PLabel = None ; PSize = 4u; PCond = pCond }
            | Error e -> Error e
        | _ -> failwithf "Wrong instruction class passed to the Memory module"        // or error?
    Map.tryFind ls.OpCode opCodes
    |> Option.map parse'

/// Parse Active Pattern used by top-level code
let (|IMatch|_|)  = parse
//**********************************************Execution************************************************************//
let executeMemInstr (ins:Instr) (data: DataPath<Instr>) =
    let instrName = ins.InstrN
    let isByte = ins.Type
    let off = ins.Offset
    let regAdd = ins.RAdd
    let regCont = ins.RContents
    let macMem = data.MM
    let macRegs = data.Regs

    let executeLOAD payload memLoc offsAdd d = 
        let newRegs = 
            let loadedReg = macRegs.Add (regCont, payload)
            match off with
            | None | Some (_, Normal) -> loadedReg
            | Some (vOrR, PostIndexed) ->
                match vOrR with
                | Literal v -> Map.add regAdd (memLoc + v + offsAdd) loadedReg
                | Reg r -> macRegs.[r] |> fun v -> Map.add regAdd (memLoc + v + offsAdd) loadedReg             
            | Some (_, PreIndexed) -> Map.add regAdd (memLoc+offsAdd) loadedReg        
        {d with Regs=newRegs}   

    let executeSTORE payload memLoc offsAdd d = 
        let newMem = macMem.Add ((WA memLoc) , (DataLoc payload))
        let newRegs = 
            match off with 
            | None | Some (_, Normal) -> macRegs
            | Some(_, PreIndexed) -> macRegs.Add (regAdd, memLoc+offsAdd)
            | Some(vOrR, PostIndexed) -> // shit, STRB
                match vOrR with
                | Literal v -> macRegs.Add (regAdd, memLoc + v + offsAdd)
                | Reg r -> macRegs.[r] |> fun v -> macRegs.Add (regAdd, memLoc + v + offsAdd)
        {d with Regs=newRegs ; MM=newMem}  

    let getPayload memLoc = 
        match memLoc with
        | Some add ->
            match macMem.[WA add] with
            | DataLoc da -> da
            | Code _ -> failwithf "What? Should not access this memory location"
        | None -> macRegs.[regCont]

    let getEffecPayload payload offsAdd shift = 
        match offsAdd with
        | 0u -> payload, 0xFFFFFF00u
        | 1u -> shift payload 8, 0xFFFF00FFu
        | 2u -> shift payload 16, 0xFF00FFFFu
        | 3u -> shift payload 24, 0x00FFFFFFu
        | _ -> failwithf "Impossible. Modulo 4"

    let executeLSWord src execType memLoc d = 
        getPayload src
        |> fun p -> execType p memLoc 0u d     
           
    // let executeLDR memLoc d = 
    //     getPayload (Some memLoc)
    //     |> fun p -> executeLOAD p memLoc 0u d    
    
    // let executeSTR memLoc d = 
    //     getPayload None
    //     |> fun p -> executeSTORE p memLoc 0u d       

    let executeLDRB baseAdd offsAdd d = // return smolPayload and baseAdd, then it's normal LDR?
        let prepReg = Map.add regCont 0u macRegs
        
        getPayload (Some baseAdd)
        |> fun p -> getEffecPayload p offsAdd (>>>)
        |> fun (shiftedP, _) -> shiftedP &&& 0xFFu
        |> fun effecP -> executeLOAD effecP baseAdd offsAdd ({d with Regs=prepReg})

        // let payload = getPayload (Some baseAdd)
        // let smolPayload = 
        //     getEffecPayload payload offsAdd (>>>)
        //     |> fun (p, _) -> p &&& 0xFFu 
        // executeLOAD smolPayload baseAdd offsAdd ({d with Regs=prepReg})          

    let executeSTRB baseAdd offsAdd d = // return shiftedPayload and baseAdd, then it's normal STR
        let payload = (getPayload None) % 256u
        let restOfWord = 
            match macMem.[WA baseAdd] with
            | DataLoc p -> p
            | Code _ -> failwithf "Not allowed to access this part of memory"
        let shiftedPayload = 
            getEffecPayload payload offsAdd (<<<)
            |> fun (p, mask) -> p ||| (restOfWord &&& mask)
        executeSTORE shiftedPayload baseAdd offsAdd d             


    let executeLS typeLS isByte d = 
        // register stores address, get that address
        let add = macRegs.[regAdd]
        // address might have an offset, get the effective address
        let effecAdd, offsAddForB = 
            let effecAdd' = 
                match off with
                | None | Some (_, PostIndexed) -> add
                | Some (vOrR, Normal) | Some (vOrR, PreIndexed) -> 
                    match vOrR with
                    | Literal v -> add + v
                    | Reg r -> add + macRegs.[r]   
            match isByte, effecAdd' % 4u with  
            | None, 0u ->
                match macMem.[WA effecAdd'] with 
                | DataLoc _ -> Ok effecAdd', None 
                | Code _ -> Error "Not allowed to access this part of memory", None  
            | Some B, offs -> 
                let wordAddr = effecAdd' - offs
                match macMem.[WA wordAddr] with
                | DataLoc _ -> Ok wordAddr, Some offs                     
                | Code _ -> Error "Not allowed to access this part of the memory", None                            
            | None, _ -> Error "Memory address accessed must be divisible by 4", None                     
        match typeLS, isByte, offsAddForB with
        | LDR, None, _ -> Result.map (fun memLoc -> executeLSWord (Some memLoc) executeLOAD memLoc d) effecAdd
        //| LDR, None, _ -> Result.map (fun memLoc -> executeLDR memLoc d) effecAdd
        | STR, None, _ -> Result.map (fun memLoc -> executeLSWord None executeSTORE memLoc d) effecAdd
        //| STR, None, _ -> Result.map (fun memLoc -> executeSTR memLoc d) effecAdd
        | LDR, Some B, Some o -> Result.map (fun memLoc -> executeLDRB memLoc o d) effecAdd
        | STR, Some B, Some o -> Result.map (fun memLoc -> executeSTRB memLoc o d) effecAdd
        | _ -> failwithf "Impossible"
 
    executeLS instrName isByte data
