module Compare 
open CommonData
open CommonLex
open FlexOp
open System

type OpInstr = CMP | CMN
type Instr = {
    OpCode: OpInstr
    OpCond: Condition //condition is NOT option bc "" is included in CommonLex.condMap
    Op1: RName
    Op2: Op2 //FlexOp.Op2
    }

/// parse error (dummy, but will do)
type ErrInstr = string

/////////////////////////////////////////////////////////////////////////////////////////////
//-----------------------------------------PARSING-----------------------------------------//
/////////////////////////////////////////////////////////////////////////////////////////////

let dPSpec = {
    InstrC = CM
    Roots = ["CMP";"CMN"]
    Suffixes = [""]
}

/// map of all possible opcodes recognised
let opCodes = opCodeExpand dPSpec

// the result is None if the opcode does not match
// otherwise it is Ok Parse or Error (parse error string)
// case sensitive: lower case not recognised - may need to string.Upper the instr line
let parse (ls: LineData) : Result<Parse<Instr>,string> option =
    let (WA la) = ls.LoadAddr

    // return dummy Instr with updated OpCode, OpFlag, OpCond
    let parseOpCode (ls:LineData) :Result<Instr,string> =
        let getRoot root =
            match root with
            | "CMP" -> CMP
            | "CMN" -> CMN
            | _ -> failwithf "Incorrect root"
        let makeInstr (ld:LineData) = 
            match opCodes.TryFind ld.OpCode with
            | Some (_, (r,s,c))
                -> Ok {OpCode= getRoot r; OpCond= c; Op1= R0; Op2 = Register(R0)}
            | None -> failwithf "Incorrect opcode" // should not happen since tryfind is already done by parse
        makeInstr ls

    // get dummy Instr from parseOpCode and update with parsed operands info
    // ins to be pipelined via Result.bind from parseOpCode
    let parseOperand (ls: LineData) (ins:Instr) :Result<Instr,string> =
        let wordList = // [dest ; op1 ; op2 ; SHIFT_op ; #expression]
            ls.Operands.Split([|' ';','|], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.toList
        let (|CheckReg|_|) (str:string) = // check if opStr is register, return RName option
            match str.StartsWith("R") with
            | true  -> regNames.TryFind str
            | false -> None            
        let (|CheckLit|_|) (str:string) = // check if opStr is Lit, return Lit option
            let mutable uval = 0u
            if System.UInt32.TryParse(str.Substring(1), &uval) 
                then makeLiteral uval
                else None

        let excludePC reg = // only for op2 if wordList.Length > 3
            match reg with
            | R15 -> Error "Cannot use PC register for op2 with shift"
            | _ -> Ok reg
        let getReg str = //None not allowed bc input string is already sorted as Reg/Num/Shift
            match str with
            | CheckReg reg -> Ok reg
            | _ -> Error "Invalid register name"
        let getOperand opStr :Result<Operand,string> = 
            match opStr with
            | CheckReg r -> Reg r |> Ok
            | CheckLit l -> Lit l |> Ok
            | _ -> Error "Invalid operands"  
        let getShift str =
            let initShiftReg =
                getReg wordList.[1]
                |> Result.bind excludePC
                |> Result.bind (fun x -> Ok {Reg = x; Value = Reg(R0)})
            let updateSVal sft :Result<Shift,string> =
                getOperand wordList.[3]
                |> Result.bind (fun x -> Ok {sft with Value = x})
            let shiftToOp2 sft =
                match str with
                | "LSL" -> Ok (LSL sft)
                | "ASR" -> Ok (ASR sft)
                | "LSR" -> Ok (LSR sft)
                | "ROR" -> Ok (ROR sft)
                | _ -> Error "Invalid shift operation"
            initShiftReg 
            |> Result.bind updateSVal 
            |> Result.bind shiftToOp2
        
        //update the pipelined Instr with parsed information
        let updateOp1 ins = // op1 must be a register
            getReg wordList.[0] |> Result.bind (fun x -> Ok {ins with Op1 = x})
        let updateOp2 ins =
            match wordList.Length with
            | 2 -> 
                let operandToOp2 op =
                    match op with
                    | Lit l -> Ok {ins with Op2 = Literal l}
                    | Reg r -> Ok {ins with Op2 = Register r}
                getOperand wordList.[1] 
                |> Result.bind operandToOp2
            | 3 -> //RRX(excludePC getReg op2)
                if wordList.[2] = "RRX"
                    then getReg wordList.[1]
                        |> Result.bind excludePC
                        |> Result.bind (fun x -> Ok {ins with Op2 = RRX(x)})
                    else Error "Invalid shift operation"
            | 4 -> getShift wordList.[2]
                |> Result.bind (fun x -> Ok {ins with Op2 = x})
            | _ -> Error "Invalid operand"
        updateOp1 ins
        |> Result.bind updateOp2

    let parsedInstr ld :Result<Instr,string> = 
        parseOpCode ld |> Result.bind (parseOperand ld)
    let parse' (instrC, (root,suffix,pCond)) =
        match instrC with
        | CM -> 
            match parsedInstr ls with 
            | Ok parsed -> Ok {PInstr= parsed ; PLabel= None ; PSize= 4u; PCond= pCond}
            | Error e -> Error e
        | _ -> failwithf "Wrong instruction class to Compare"
    Map.tryFind ls.OpCode opCodes
    |> Option.map parse'


/// Parse Active Pattern used by top-level code
let (|IMatch|_|) = parse

/////////////////////////////////////////////////////////////////////////////////////////////
//----------------------------------------EXECUTION----------------------------------------//
/////////////////////////////////////////////////////////////////////////////////////////////

// execute parsed instruction
// no error as all pattern matches are DU
// error only rises from parse
let execute (ins: Instr) (d: DataPath<Instr>) =
    // read execution conditions
    let readCondition ins d = // return true to execute, false to ignore instructon
        match (ins.OpCond, d.Fl) with
        | Ceq, fl -> if fl.Z = true then true else false
        | Cne, fl -> if fl.Z = false then true else false
        | Cmi, fl -> if fl.N = true then true else false
        | Cpl, fl -> if fl.N = false then true else false
        | Chs, fl -> if fl.C = true then true else false
        | Clo, fl -> if fl.C = false then true else false
        | Cvs, fl -> if fl.V = true then true else false
        | Cvc, fl -> if fl.V = false then true else false
        | Chi, fl -> if (fl.C=true)&&(fl.Z=false) then true else false
        | Cls, fl -> if (fl.C=false)||(fl.Z=true) then true else false
        | Cge, fl -> if (fl.N = fl.V) then true else false
        | Clt, fl -> if (fl.N <> fl.V) then true else false
        | Cgt, fl -> if fl.Z=false && fl.N = fl.V then true else false
        | Cle, fl -> if fl.Z=true || fl.N <> fl.V then true else false 
        | Cal, _  -> true
        | Cnv, _  -> false

    // arithmetics bit
    let callOpValues ins d = // call uint32 values represented by op1, op2
        (d.Regs.[ins.Op1], flexOp2 ins.Op2 d)
    let arithmetic (a,b) = // returns arithmetic result in uint64
        match ins.OpCode with
        | CMP -> uint64 a - uint64 b
        | CMN -> uint64 b - uint64 a

    // update flags
    let updateFlag d dVal =
        let setFlagN ((dV,dP) :uint64*DataPath<Instr>) =
            if int32 dV < 0
                then dV, {dP with Fl = {dP.Fl with N = true}}
                else dV, {dP with Fl = {dP.Fl with N = false}}
        let setFlagZ ((dV,dP) :uint64*DataPath<Instr>) =
            if int dV = 0
                then dV, {dP with Fl = {dP.Fl with Z = true}}
                else dV, {dP with Fl = {dP.Fl with Z = false}}
        let setFlagC ((dV,dP) :uint64*DataPath<Instr>) =
            if uint64 (uint32 dV) = uint64 dV
                then dV, {dP with Fl = {dP.Fl with C = false}}
                else dV, {dP with Fl = {dP.Fl with C = true}}
        let setFlagV ((dV,dP) :uint64*DataPath<Instr>) =
            if int64 (int32 dV) = int64 dV
                then {dP with Fl = {dP.Fl with V = false}}
                else {dP with Fl = {dP.Fl with V = true}}
        (dVal,d) |> setFlagN |> setFlagZ |> setFlagC |> setFlagV

    // update registers
    let updatePC (d: DataPath<Instr>) = // PC register update
        {d with Regs = d.Regs.Add(R15, (d.Regs.[R15] + 4u))}
    
    // execution
    if readCondition ins d = true
        then callOpValues ins d
            |> arithmetic //value to be stored in destination register, in uint64
            |> updateFlag d //flags updated
            |> updatePC // PC register updated
        else updatePC d //update PC register only