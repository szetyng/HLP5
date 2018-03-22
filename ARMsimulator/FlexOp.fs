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
    
// check if given uint32 immediate expression is alloweRRXd
let makeLiteral (lit:uint32) =
    match List.contains lit allowedLiterals with
    | true -> Some lit
    | false -> None
    
// input Op2, DataPath -> return uint32 equal to Op2 value
// no need to makeLiteral literal data because makeLiteral is used in parse
// flexOp2 is only used in execute
let flexOp2 (op2:Op2) (cpuData:DataPath<'INS>) = 
    let shiftVal shift doShift =
        let rv = cpuData.Regs.[shift.Reg]
        let shiftV = 
            match shift.Value with
            | Lit n -> n
            | Reg n -> cpuData.Regs.[n] % 32u // works on uint32
        doShift rv (int shiftV)
    let doROR a n =
        (a >>> n) ||| (a <<< (32-n))
    match op2 with
    | Literal literalData -> literalData
    | Register register -> cpuData.Regs.[register]
    | LSL shift -> shiftVal shift (<<<)
    | ASR shift -> shiftVal shift (fun a b -> (int a) >>> b |> uint32)
    | LSR shift -> shiftVal shift (>>>)
    | ROR shift -> shiftVal shift doROR
    | RRX r     -> (cpuData.Regs.[r] >>> 1) + (1u <<< 31)
