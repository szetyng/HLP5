module SingleR 
//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

open CommonLex
open CommonData

type InstrName = LDR | STR
type MemType = B
type OffsetVal = Literal of int | Reg of RName
type OffsetType = Normal | PreIndexed | PostIndexed

type Instr = 
    {
        /// to represent either LDR or STR
        InstrN: InstrName;
        /// to represent the suffix B, if present
        Type: MemType option;
        /// to represent the data-sotring register
        /// RDest in LDR{B}
        /// RSrc in STR{B}
        RContents: RName;   // register holding the contents to be loaded/stored to/from (LDR/STR respectively)
        /// to represent the address-storing register
        /// RSrc in LDR{B}
        /// RDest in STR{B}
        RAdd: RName;        // register holding the address
        /// to represent the type of offset and its value, if present
        /// offset value can be a literal or stored in a register
        Offset: (OffsetVal * OffsetType) option
    }

/// parse error (dummy, but will do)
type ErrInstr = string

let hexa = 
    Seq.zip (['0'..'9'] @ ['A'..'F']) [0..15]
    |> Map.ofSeq

let bina = 
    Seq.zip ['0' ; '1'] [0 ; 1] 
    |> Map.ofSeq

type NrBase = Hex | Bin

let memSpec = {
    InstrC = MEM
    Roots = ["LDR";"STR"]
    Suffixes = [""; "B"]
}


/// map of all possible opcodes recognised
let opCodes = opCodeExpand memSpec
    
// real parsing done here    
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
        match Map.tryFind regNameStr regNames with
        | Some r -> Ok r
        | None -> Error "Invalid register name"
    
    /// converts string to OffsetVal 
    /// can be literal or stored in register   
    let getOffsetVal (valStr:string) = 
        /// converts a hexadecimal or binary number stored as a string
        /// into its correct int value
        let baseToInt (nrStr:string) (ba:NrBase) =
            let getBaInInt = function Hex -> 16 | Bin -> 2
            let actualConv ba lst =
                let getMap = function Hex -> hexa | Bin -> bina           
                match List.choose (fun n -> Map.tryFind n (getMap ba)) lst with
                | goodList when goodList.Length = lst.Length-> Ok goodList
                | _ -> Error "Not a proper number"
            /// when ba=16 and n is 0,1,2,... represents 16^0, 16^1, 16^2 and so on
            let timey = 
                function | 0 -> 1 
                         | n' ->  List.reduce (*) [for _ in 1..n' -> getBaInInt ba]
            // convert string to list of chars
            let charLst, neg = 
                let l = [for c in nrStr -> c]
                match l.[0] with
                | '-' -> List.filter ((<>)'-') l, true
                | _ -> l, false 
            let nr = 
                actualConv ba charLst
                |> Result.map List.rev
                |> Result.map (fun lst -> List.map2 (*) [for i in 0..(charLst.Length)-1 -> timey i] lst) // calc val of each digit
                |> Result.map (fun l -> List.reduce (+) l) // adds them together to get decimal value
            match neg with
            | true -> Result.map (fun x -> 0 - x) nr
            | false -> nr
        let (|GetLit|_|) (nrBase:string) (valStr:string) =
            match valStr.StartsWith(nrBase) with
            | true -> 
                let x = valStr.Substring(nrBase.Length) 
                Some (x.Trim [|']' ; '!'|])
            | false -> None            
        match valStr with
        | GetLit "#0x" hex -> Result.map Literal (baseToInt hex Hex)
        | GetLit "#0b" bin -> Result.map Literal (baseToInt bin Bin)
        | GetLit "#" dec -> dec |> int |> Literal |> Ok
        | reg -> reg.Trim [|']' ; '!'|] |> getRName |> Result.map Reg
        | _ -> Error "Incorrect offset format" //ie forgetting '#'         

    /// converts string to RName, to get RAdd
    let getRn ((reg:string),offset) = 
        match reg.StartsWith("["), reg.EndsWith("]"), offset with
        | true, true, None  -> getRName reg 
        | true, false, Some PreIndexed | true, false, Some Normal -> getRName reg 
        | true, true, Some PostIndexed -> getRName reg
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
            | _ -> Error "Incorrect rnVal format"                 
        | _ -> Error "Incorrect formatting"        
    let offsetVal = 
        match operandLst.Length with
        // two operands represent no offset
        | 2 -> Ok None
        // three operands represent some kind of offset
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
        | _ -> failwithf "Wrong instruction class passed to the Memory module" // not error bc of opCodes map
    Map.tryFind ls.OpCode opCodes
    |> Option.map parse'

/// Parse Active Pattern used by top-level code
let (|IMatch|_|)  = parse

///////////////////////////////////////////////
///////////////////////////////////////////////
//  Execution code begins
///////////////////////////////////////////////
///////////////////////////////////////////////

/// Called from CommonTop to execute ins on data
/// ins and data corresponds to Instr type of Memory module
let execute (ins:Instr) (data: DataPath<'INS>) =
    let instrName = ins.InstrN
    let isByte = ins.Type
    let off = ins.Offset
    let regAdd = ins.RAdd
    let regCont = ins.RContents
    let macMem = data.MM
    let macRegs = data.Regs

    /// Updates the register map to load wordy payload into a register 
    /// Also updates register RAdd if required by pre- or post-indexing 
    let executeLOAD (memLoc:WAddr) (offsAdd:uint32) (d:DataPath<'INS>) (payload:uint32) = 
        let newRegs = 
            let loadedReg = macRegs.Add (regCont, payload)
            match off, memLoc with
            | None, _ | Some (_, Normal), _ -> loadedReg
            | Some (vOrR, PostIndexed), WA addr ->
                match vOrR with
                | Literal v -> Map.add regAdd (addr + (uint32 v) + offsAdd) loadedReg
                | Reg r -> macRegs.[r] |> fun v -> Map.add regAdd (addr + (uint32 v) + offsAdd) loadedReg             
            | Some (_, PreIndexed), WA addr -> Map.add regAdd (addr + offsAdd) loadedReg        
        {d with Regs=newRegs}   
    /// Updates the memory map to store wordy payload into memory
    /// Also updates register RAdd if required by pre- or post-indexing
    let executeSTORE (memLoc:WAddr) (offsAdd:uint32) (d:DataPath<'INS>) (payload:uint32) = 
        let newMem = macMem.Add ((memLoc) , (DataLoc payload))
        let newRegs = 
            match off, memLoc with 
            | None, _ | Some (_, Normal), _ -> macRegs
            | Some(_, PreIndexed), WA addr -> macRegs.Add (regAdd, addr+offsAdd)
            | Some(vOrR, PostIndexed), WA addr -> // shit, STRB
                match vOrR with
                | Literal v -> macRegs.Add (regAdd, addr + (uint32 v) + offsAdd)
                | Reg r -> macRegs.[r] |> fun v -> macRegs.Add (regAdd, addr + (uint32 v) + offsAdd)
        {d with Regs=newRegs ; MM=newMem}  

    let getPayload memLoc = 
        match memLoc with
        | Some add ->
            match macMem.[add] with
            | DataLoc da -> da
            | Code _ -> failwithf "What? Should not access this memory location"
        | None -> macRegs.[regCont]
    let getShiftedPayloadMask offsAdd shift payload = 
        match offsAdd with
        | 0u -> payload, 0xFFFFFF00u
        | 1u -> shift payload 8, 0xFFFF00FFu
        | 2u -> shift payload 16, 0xFF00FFFFu
        | 3u -> shift payload 24, 0x00FFFFFFu
        | _ -> failwithf "Impossible. Modulo 4"

    /// Get payload from memory or from register, based on type of LS instruction
    /// Some src: address where payload is located for LDR instruction
    /// None: payload is stored in register, for STR instruction  
    let executeLSWord src execType (memLoc:WAddr) (d:DataPath<'INS>) : DataPath<'INS> = 
        getPayload src
        |> fun p -> execType memLoc 0u d p     
    /// Processes 32-bit word found in word-aligned base address 
    /// by locating the relevant byte and converting it into small 8-bit long payload
    /// and clears register RDest preemptively. 
    /// Normal LDR with the byte-y payload. 
    /// Passes base address and offset for correct pre-/post-indexing
    let executeLDRB (baseAdd: WAddr) (offsAdd: uint32) (d:DataPath<'INS>) : DataPath<'INS> =
        let prepReg = Map.add regCont 0u macRegs
       
        getPayload (Some baseAdd) // get all 4 bytes from word-alligned base address in memory
        |> getShiftedPayloadMask offsAdd (>>>)
        |> fun (shiftedP, _) -> shiftedP &&& 0xFFu // only get relevant byte
        |> executeLOAD baseAdd offsAdd ({d with Regs=prepReg})     
    /// Processes 32-bit word found in register RSrc 
    /// into its least significant 8-bit byte-y version.
    /// Shifts byte into correct position of the rest of the 32-bit word in the base address.
    /// Normal STR with this tacked-on payload.
    /// Passes base address and offset for correct pre-/post-indexing
    let executeSTRB (baseAdd: WAddr) (offsAdd: uint32) (d:DataPath<'INS>) : DataPath<'INS> = 
        // will be ANDed with relevant mask to clear the byte-address (base addr + offset addr)
        let restOfWord = 
            match macMem.[baseAdd] with
            | DataLoc p -> p
            | Code _ -> failwithf "Not allowed to access this part of memory"
                
        getPayload None
        |> fun p -> p % 256u // get LS 8bits of value in register RSrc -> byte
        |> getShiftedPayloadMask offsAdd (<<<) // shift the byte-y payload into correct position
        |> fun (shiftedP, mask) -> shiftedP ||| (restOfWord &&& mask) // get correct word in word-alligned base address
        |> executeSTORE baseAdd offsAdd d               

    /// Extracts address from RAdd and uses it to calculate effective address 
    /// Calculates base address and offset for byte-addresing
    /// Identify opcode and passes the processed addresses into their relevant execute functions
    let executeLS typeLS isByte d = 
        // register stores address, get that address (src add for load, dest add for store)
        let add = macRegs.[regAdd]
        // address might have a normal/pre-indexed offset, get the effective address
        let effecAdd, offsAddForB = 
            let effecAdd' = 
                match off with
                | None | Some (_, PostIndexed) -> add
                | Some (vOrR, Normal) | Some (vOrR, PreIndexed) -> 
                    match vOrR with
                    | Literal v -> add + (uint32 v)
                    | Reg r -> add + macRegs.[r]   
            match isByte, effecAdd' % 4u with  
            | None, 0u ->
                match Map.tryFind (WA effecAdd') macMem with
                | Some (DataLoc _) -> Ok (WA effecAdd'), None 
                | _ -> Error "Not allowed to access this part of memory", None    
            // LDRB/STRB: break down byte address into word-aligned base address and its offset            
            | Some B, offs -> 
                let wordAddr = effecAdd' - offs
                match Map.tryFind (WA wordAddr) macMem with
                | Some (DataLoc _) -> Ok (WA wordAddr), Some offs                     
                | _ -> Error "Not allowed to access this part of the memory", None                            
            | None, _ -> Error "Memory address accessed must be divisible by 4", None                     
        match typeLS, isByte, offsAddForB with
        | LDR, None, _ -> Result.map (fun memLoc -> executeLSWord (Some memLoc) executeLOAD memLoc d) effecAdd
        | STR, None, _ -> Result.map (fun memLoc -> executeLSWord None executeSTORE memLoc d) effecAdd
        | LDR, Some B, Some o -> Result.map (fun memLoc -> executeLDRB memLoc o d) effecAdd
        | STR, Some B, Some o -> Result.map (fun memLoc -> executeSTRB memLoc o d) effecAdd
        | _ -> failwithf "Impossible"
 
    executeLS instrName isByte data
