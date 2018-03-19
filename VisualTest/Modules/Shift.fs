//////////////////////////////////////////////////////////////////////////////////////////
//                   Shift instruction implementation module
//////////////////////////////////////////////////////////////////////////////////////////

module Shift 

open CommonData
open CommonLex
open System.Text.RegularExpressions

type SVal = Sh of int | Rs of RName | RRX
type Instr = {InstrC:InstrClass ;OpCode: string; Rd: RName; Rm: RName; Op2: SVal; SBit: string}

/// Specification for Shift instructions
let dPSpec = {
    InstrC = Shift          
    Roots = ["LSL";"LSR";"ASR";"ROR";"RRX"]
    Suffixes = [""; "S"]
}

/// Map of all possible opcodes recognised
let opCodes = opCodeExpand dPSpec
let (|RegExpMatch|_|) pattern input =
   let m = Regex.Match(input, pattern) in
   if m.Success then
      Some (List.tail [ for g in m.Groups -> g.Value ]) else None

let parse (ls: LineData) : Result<Parse<Instr>,string> option =
    let parse' (instrC, (root,suffix,pCond)) =
        let initInstr = {
                        InstrC=instrC; 
                        OpCode=root; 
                        Rd=R0; 
                        Rm=R0; 
                        Op2=Rs R0; 
                        SBit= suffix
                        }
        let (WA la) = ls.LoadAddr 
        let initParse = {
                        PInstr=initInstr; 
                        PLabel= ls.Label |> Option.map (fun lab -> lab,la); 
                        PSize=4u; 
                        PCond=pCond
                        } 
        
        // Generates op2 value depending on parsed string s
        let checkSVal (s:string) = 
            if s = "" 
                then RRX 
            else
                // if s is not a register, it must be a shift length.
                match regNames.TryFind(s) with             
                | Some r -> Rs r
                | None -> Sh <| int (s.[1..])               
        
        // Fill Instr fields using parsed string list slist  
        let makePInstr (i:Parse<Instr>) (slist:string list) = 
            let slist = slist |> List.map (fun (x:string) -> x.ToUpper()) 
            let x = 
                {initInstr with 
                    Rd = regNames.[slist.[0]]
                    Rm = regNames.[slist.[1]]
                    Op2 = checkSVal slist.[2]
                }
            {i with PInstr= x}
        
        let regex = "^([rR][0-9]|[rR]1[0-4]),([rR][0-9]|[rR]1[0-4]),([rR][0-9]|[rR]1[0-4]|#([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5]))$"
        let regexRRX = "^([rR][0-9]|[rR]1[0-4]),([rR][0-9]|[rR]1[0-4])()$"                  // () adds empty string to end.
        
        match ls.Operands with 
           | RegExpMatch regex x when root <> "RRX" -> x |> makePInstr initParse |> Ok
           | RegExpMatch regexRRX x when root = "RRX" -> x |> makePInstr initParse |> Ok
           | _ -> Error ("Invalid operands given for Shift Module")

    Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
    |> Option.map parse'

/// Parse Active Pattern used by top-level code
let (|IMatch|_|) = parse

let execute (i:Instr) (d:DataPath<'INS>): Result<DataPath<'INS>,string> = 
    let intToBool = function
        | 1 -> true
        | 0 -> false
        | _ -> failwithf "Invalid binary value."
    
    let boolToInt = function
        | true -> 1
        | false -> 0
    
    let setN x =                           // set = 1 if result of operation is negative.
        x &&& 0x80000000ul >>> 31         // if MSB = 1, N = true, else falses
        |> int |> intToBool
    
    let setZ = function                   // set = 1 if result of operation is zero.
        | 0ul -> true
        | _ -> false

    // Function that updates Flags if S is set using the given flags and dataPath
    let checkS (updatedFlag:Flags) (d:DataPath<'INS>) : DataPath<'INS> = 
        match i.SBit with
        | "S" -> {d with Fl = updatedFlag}   
        | "" -> d
        | _ -> failwithf "What?"

    let nBitFlag n = (d.Regs.[i.Rm] <<< (32-n)) >>> 31 |> int |> intToBool          // select Rm[n-1] bit and convert to bool
    let nBitFlag' n = (d.Regs.[i.Rm] <<< (n-1)) >>> 31 |> int |> intToBool          // select Rm[32-n] bit and convert to bool
    
    let regLSR (r:RName) (n:int): uint32 = 
        if (0 <=n) && (n<= 31) then d.Regs.[r] >>> n else 0ul
    
    let flagLSR (n:int) (res:uint32): Flags =
        let cF = 
            match n with 
                | 0 -> d.Fl.C                                       // do not update C flag
                | n when (0 < n) && (n < 33) -> nBitFlag n          // Rm[n-1] 
                | n when (n<0) || (n >= 33) -> false                // for LSR, C = 0 for n > 32                                 
                | _ -> failwithf "Invalid shift value"
        {d.Fl with C = cF; Z = setZ res; N = setN res}

    let regASR (r:RName) (n:int): uint32 = 
        let msbRes = 
            let msb' = d.Regs.[i.Rm] &&& 0x80000000ul >>> 31
            match msb' with
                | 0ul -> 0ul
                | 1ul -> 0xFFFFFFFFul
                | _ -> failwithf "Invalid binary value"    
        if (0 <=n) && (n<= 31) then d.Regs.[r] |> int32 >>> n |> uint32 else msbRes                // all bits in the result is set to MSB of Rm

    let flagASR (n:int) (res:uint32): Flags =
        let cF = 
            match n with 
            | 0 -> d.Fl.C                                            // do not update C flag
            | n when (0 < n) && (n < 33) -> nBitFlag n               // Rm[n-1] 
            | n when (n >= 33) || (n<0) -> d.Regs.[i.Rm] &&& 0x80000000ul >>> 31 |> int |> intToBool       // for ASR, C = MSB of Rm for n > 32, n = 33, etc
            | _ -> failwithf "Invalid shift value"
        {d.Fl with C = cF; Z = setZ res; N = setN res}
    
    let regLSL (r:RName) (n:int): uint32 = 
        if (0 <=n) && (n<= 31) then d.Regs.[r] <<< n else 0ul

    let flagLSL (n:int) (res:uint32): Flags =
        let cF = 
            match n with 
                | 0 -> d.Fl.C                                           // do not update C flag
                | n when  (0 < n) && (n < 33) -> nBitFlag' n           // Rm[32-n] to be used for C flag  
                | n when (n<0) || (n >= 33) -> false                   // for LSL, C = 0 for n > 32
                | _ -> failwithf "Invalid shift value"
        {d.Fl with C = cF; Z = setZ res; N = setN res}

    let regROR (r:RName) (n:int): uint32 = 
         (d.Regs.[r] >>> n) ||| (d.Regs.[r] <<< (32-n))

    let flagROR (n:int) (res:uint32): Flags =
        let cF = 
            match n with
            | 0 -> d.Fl.C
            | n when n=32 -> nBitFlag 32
            // | n when (n%32=0) -> nBitFlag 32
            | n when (n%32=0) -> false
            | _ -> nBitFlag (n%32)
        {d.Fl with C = cF; Z = setZ res; N = setN res}
    
    let makeShift (makeReg) (makeFlag) (i:Instr) (d:DataPath<'INS>)  =
        let sV = 
            match i.Op2 with
            | Sh x -> x
            | Rs x -> d.Regs.[x] |> int
            | RRX -> failwithf "Not possible"

        let res = makeReg i.Rm sV
        let resFlag = makeFlag sV res
        {d with Regs = d.Regs.Add(i.Rd, res )} |> checkS resFlag

    let makeLSL = makeShift regLSL flagLSL
    let makeLSR = makeShift regLSR flagLSR
    let makeASR = makeShift regASR flagASR
    let makeROR = makeShift regROR flagROR

    let makeRRX: DataPath<'INS> =
        let regRRX (r:RName): uint32 = 
            d.Regs.[r] >>> 1 ||| ((d.Fl.C |> boolToInt |> uint32) <<< 31)
        let flagRRX (res:uint32): Flags =  
            let cF = nBitFlag 1                                                     // Rm[0]
            {d.Fl with C = cF; Z = setZ res; N = setN res}
        let resReg = regRRX i.Rm                                                    // Apply RRX on Rm
        let resFlag = flagRRX resReg                                                // update flags using result
        {d with Regs = d.Regs.Add(i.Rd, resReg )} 
        |> checkS resFlag                                                           // update flag only if S is set

    match i.OpCode with
    | "RRX" -> Ok (makeRRX)
    | "LSR" -> Ok (makeLSR i d)
    | "ASR" -> Ok (makeASR i d)
    | "LSL" -> Ok (makeLSL i d)
    | "ROR" -> Ok (makeROR i d)
    | _ -> Error ("Impossible Shift instruction")
