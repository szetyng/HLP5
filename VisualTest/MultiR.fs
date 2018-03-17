//////////////////////////////////////////////////////////////////////////////////////////
//                   LDM/STM instruction implementation module
//////////////////////////////////////////////////////////////////////////////////////////

module MultiR

open CommonData
open CommonLex
open System.Text.RegularExpressions

type Instr = {
        InstrC:InstrClass; 
        OpCode: string; 
        AMode:string; 
        Rn:RName; 
        WrB:bool; 
        RList:Result<RName list,string> 
        }
let mSpec = {
    InstrC = Shift          
    Roots = ["LDM";"STM"]
    Suffixes = [""; "IA";"FD";"EA";"DB"]
}

let opCodes = opCodeExpand mSpec


let (|RegExpMatch|_|) pattern input =
   let m = Regex.Match(input, pattern) in
   if m.Success then
      Some (List.tail [ for g in m.Groups -> g.Value ]) else None

let parse (ls: LineData) : Result<Parse<Instr>,string> option =
    let parse'(instrC, (root,suffix,pCond)) =
        let initInstr = {InstrC=instrC; OpCode=root; AMode= suffix; Rn=R0; WrB = false; RList=Error ""}
        let (WA la) = ls.LoadAddr 
        let initParse = {PInstr=initInstr; PLabel= ls.Label |> Option.map (fun lab -> lab,la); PSize=4u; PCond=pCond} 
        
        // Fill Instr fields using parsed string list slist  
        let makePInstr (i:Parse<Instr>) (slist:string list) = 
            // split a string at the commas
            let splitIntoWords ( line:string ) =
                line.Split( ([|','|] : char array), 
                    System.StringSplitOptions.RemoveEmptyEntries)

            let slist = slist |> List.map (fun (x:string) -> x.ToUpper()) 
            
            let toBool = function 
                | "" -> false 
                | "!" -> true 
                | _ -> failwithf "Regex failed, not possible."
            
            // Converts an element in the reglist into corresponding RName equivalent
            // Each element can be a either a single register or a range of registers
            // Result is a register list with a single element or a a list of registers
            let parseRegList (x:string): Result<RName list, string> =
                // convert range of register into list of register
                // Example R0-R3 -> R0,R1,R2,R3
                let expandRange (r1:string) (r2:string) =
                    // extract the register number from string
                    // Example: R0 -> 0 
                    let a = r1.[1..] |> int
                    let b = r2.[1..] |> int
                    match a with
                    | a when a <= b -> [a..b] |> Ok
                    | _ -> Error "Invalid reglist range \n"

                let invRMap (x:int list): RName list =
                    List.map (fun x -> inverseRegNums.[x]) x 
                
                // convert parsed string list from regex into list of registers 
                // x can be ["r1-r9";"r1";"r9"] or ["r1";"";""]
                let strToR (x:string list): Result<RName list,string> = 
                    let i1 = x.[0]           
                    let i2 = x.[1]          
                    let i3 = x.[2]
                    match i1,i2,i3 with
                    | _, "", "" ->  [regNames.[i1.ToUpper()]] |> Ok         // if second, third element is empty, single register
                    | _, r1,r2 -> expandRange r1 r2 |> Result.map invRMap   // else it is a range of registers

                let regexRList = "^([rR][0-9]|[rR]1[0-5]|([rR][0-9]|[rR]1[0-5])-([rR][0-9]|[rR]1[0-5]))$"
                                
                match x with
                | RegExpMatch regexRList x -> x |> strToR
                | _ -> Error "Invalid registers in reglist \n"
            
            // make instr values 
            // slist is the string list from regex
            // first element is Rn
            // second element stores the writeback suffix
            // third element stores reglist
            let rn = regNames.[slist.[0]]
            let writeBack = slist.[1] |> toBool         
            let strSplit = splitIntoWords slist.[2] |> Array.toList // split reglist into elements separated by commas
            let rList = List.map parseRegList strSplit  // convert into register list

            // function that turns list of Results into Result list.
            let makeRList r = 
                let checkErr x =                // true if no errors, return error str otherwise
                    x |> List.choose (fun x -> match x with | Error x -> Some x | _ -> None)   
                let validLst x =                // extract Ok values 
                    x |> List.choose (fun x-> match x with | Ok x -> Some x| _ -> None) |> List.concat |> set |> List.ofSeq
                
                if (checkErr r |> List.isEmpty) 
                    then validLst r |> Ok 
                    else checkErr r |> List.fold (+) "" |> Error
            
            // check for writeback, check for STM and LDM conditions
            // throw error when necessary.
            let checkRList x =
                match x with 
                | x when (x |> List.contains R13) -> Error "Register list cannot contain SP/R13" 
                | x when (x |> List.contains rn) && (writeBack) -> Error "Register list cannot contain Rn if writeback is enabled"
                | x when (x |> List.contains R15) && (root = "STM") -> Error "Register list cannot contain PC/R15 for STM" 
                | x when (x |> List.contains R15) && (x |> List.contains R14) && (root = "LDM") -> Error "Register list cannot contain PC/R15 if it contains LR/R14 for LDM" 
                | x when (x |> List.isEmpty) -> Error "Register list cannot be empty"
                | _ -> x |> Ok

            let result = makeRList rList |> Result.bind checkRList

            let x = 
                {initInstr with 
                    Rn = rn
                    WrB = writeBack
                    RList = result
                }
            {i with PInstr= x}

        let regex = "^([rR][0-9]|[rR]1[0-4])(!?),\{(.*)}$"  // Rn cannot be PC/R15
        match ls.Operands with
        | RegExpMatch regex x -> x |> makePInstr initParse |> Ok
        | _ -> Error "Invalid syntax for Rn given"
    
    Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
    |> Option.map parse'

let (|IMatch|_|) = parse

let execute (i:Instr) (d:DataPath<'INS>):Result<DataPath<'INS>,string> = 
    let (|IA|DB|) x = 
        match i.OpCode with
        | "STM" -> match x with
                    | "IA" | "EA" | "" -> IA
                    | "FD" | "DB" -> DB
                    | _ -> failwithf "Invalid Address Mode"
        | "LDM" -> match x with
                   | "IA" | "FD" | "" -> IA 
                   | "EA" | "DB" -> DB 
                   | _ -> failwithf "Invalid Address Mode"
        | _ -> failwithf "Invalid OpCode"

    let mBase = d.Regs.[i.Rn]

    // generate memory access ranges
    let mAdrRange n =
        match i.AMode with
        | IA -> [mBase..4ul..mBase + uint32 (4*(n-1))]
        | DB -> [(mBase - uint32 (4*n))..4ul..(mBase-4u)]

    // update writeback register if specified
    let updateR d =
        let updatedReg rList=
            let n = List.length rList 
            let x:uint32 list = mAdrRange n
            if i.WrB then 
                match i.AMode with
                | IA -> d.Regs.Add (i.Rn,x.[n-1] + 4u) 
                | DB -> d.Regs.Add (i.Rn,x.[0]) 
            else d.Regs  
            
        let updateR' x= {d with Regs = x} 
        i.RList
        |> Result.map updatedReg
        |> Result.map updateR'

    // execute STM
    let store = 
        let makeMap (rList:RName list) = 
            let n = List.length rList            
            let rValues = List.map(fun x -> d.Regs.[x]) rList 

            // invalidAddr will contain elements that are below 0x1000u
            let invalidAddr = List.filter(fun x-> x < 0x1000u) (mAdrRange n)

            match mBase with 
            | x when (x % 4u=0u) ->
                if (List.isEmpty invalidAddr)
                    then List.map2 (fun x y -> (WA x, DataLoc y)) (mAdrRange n) rValues|> Ok
                    else Error "Invalid memory access requested, below 0x1000u"
            | _ -> Error "Address not divisible by 4"

        let updateMap =
            let updateD m = {d with MM = m}
            i.RList 
            |> Result.bind makeMap 
            |> Result.map (List.fold (fun (x:Map<WAddr,MemLoc<Instr>>) i -> x.Add i) d.MM )
            |> Result.map updateD
        
        updateMap |> Result.bind updateR

    // execute LDM
    let load = 
        let makeMap (rList:RName list) =
            let n = List.length rList
            let mem = d.MM
            let memAddr = List.map WA (mAdrRange n)
            let validCheck = List.map (mem.ContainsKey) memAddr

            match mBase with
            | x when (x%4u=0u) ->
                if (List.contains false validCheck) 
                    then Error "Address not found in memory"
                else 
                    let memVal =
                        let x = List.map(fun (x:WAddr)-> mem.[x]) memAddr
                        let f x = 
                            match x with
                            | DataLoc x -> x
                            | _ -> failwithf "Code memory access not allowed."
                        List.map f x
                    
                    List.zip rList memVal |> Ok
            | _ -> Error "Address not divisible by 4"

        let updateMap =
            let updateD m = {d with Regs = m}
            i.RList 
            |> Result.bind makeMap 
            |> Result.map (List.fold (fun (x:Map<RName,uint32>) i -> x.Add i) d.Regs )
            |> Result.map updateD

        updateMap |> Result.bind updateR 
   

    match i.OpCode with
    | "LDM" -> load
    | "STM" -> store
    | _ -> Error "Impossible Instruction"
