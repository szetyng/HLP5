module FlexOp
open CommonData

type Operand = 
    | Lit of uint32
    | Reg of RName
type Shift = {Reg: RName; Value: Operand}
type Op2 = 
    | Literal of uint32
    | Register of RName
    | LSL of Shift
    | ASR of Shift
    | LSR of Shift
    | ROR of Shift
    | RRX of RName

let allowedLiterals = // list of all allowed literals.. do I need map?
    [0..2..30] 
    |> List.allPairs [0u..255u] 
    |> List.map (fun (lit,n) -> (lit >>> n) + (lit <<< 32-n))

// check if given uint32 immediate expression is allowed
let makeLiteral (lit: uint32) = 
    if List.contains lit allowedLiterals
        then lit
        else failwithf "Invalid immediate expression"

// input Op2, DataPath -> return uint32 equal to Op2 value
let flexOp2 (op2:Op2) (cpuData:DataPath<'INS>) = 
    let shiftVal shift doShift =
        let rv = cpuData.Regs.[shift.Reg]
        let shiftV = 
            match shift.Value with
            | Lit n -> makeLiteral n
            | Reg n -> cpuData.Regs.[n] % 32u // works on uint32
        doShift rv (int shiftV)
    let doROR a n =
        (a >>> n) ||| (a <<< (32-n))
    match op2 with
    | Literal literalData -> makeLiteral literalData
    | Register register -> cpuData.Regs.[register]
    | LSL shift -> shiftVal shift (<<<)
    | ASR shift -> shiftVal shift (fun a b -> (int a) >>> b |> uint32)
    | LSR shift -> shiftVal shift (>>>)
    | ROR shift -> shiftVal shift doROR
    | RRX r     -> (cpuData.Regs.[r] >>> 1) + (1u <<< 31)

