module ShiftTests 

open CommonData
open CommonLex
open Shift
open Expecto
open VisualTest.VCommon
open VisualTest.Visual
open VisualTest.VTest



type System.Random with                     /// Generates an infinite sequence of random numbers within the given range.
    member this.GetValues(minValue, maxValue) =
        Seq.initInfinite (fun _ -> this.Next(minValue, maxValue))

/// convenience function, convert int list of size 4 to NZCV status flag record
let intToFlags (s:int list) =
    let toBool = function | 0 -> false | 1 -> true | s -> failwithf "Bad character in flag specification '%d'" s
    match s |> List.map toBool with
    | [ a ; b ; c ; d] -> { FN=a; FZ=b;FC=c;FV=d}
    | _ -> failwithf "Wrong number of characters (should be 4) in flag specification %A" s

let rnd = System.Random()
let rndReg minV maxV = rnd.GetValues(minV,maxV) |> Seq.take 15 |> Seq.toList |> List.map uint32
let rndFlag = rnd.GetValues(0,1) |> Seq.take 4 |> Seq.toList |> intToFlags

let parseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmLine:string) =
    /// put parameters into a LineData record
    let makeLineData opcode operands = {
        OpCode=opcode
        Operands=String.concat "" operands
        Label=None
        LoadAddr = loadAddr
        SymTab = symtab
    }
    /// remove comments from string
    let removeComment (txt:string) =
        txt.Split(';')
        |> function 
            | [|x|] -> x 
            | [||] -> "" 
            | lineWithComment -> lineWithComment.[0]
    /// split line on whitespace into an array
    let splitIntoWords ( line:string ) =
        line.Split( ([||] : char array), 
            System.StringSplitOptions.RemoveEmptyEntries)
    /// try to parse 1st word, or 2nd word, as opcode
    /// If 2nd word is opcode 1st word must be label
    let matchLine words =
        let pNoLabel =
            match words with
            | opc :: operands -> 
                makeLineData opc operands 
                |> parse
            | _ -> None
        
        match pNoLabel, words with
        | Some pa, _ -> pa
        | None, label :: opc :: operands -> 
            match { makeLineData opc operands 
                    with Label=Some label} 
                  |> parse with
            | None -> 
                Error ( (sprintf "Unimplemented instruction %s" opc))
            | Some pa -> pa
        | _ -> Error ( (sprintf "Unimplemented instruction %A" words))
    asmLine
    |> removeComment
    |> splitIntoWords
    |> Array.toList
    |> matchLine

// Given a assembly string, parse and execute, and return the register values and flags 
// in the format that VisualShiftTest does for comparison.
let makeExecute (flags:Flags) (initRegs:uint32 list) (asm:string) =       
    // Generate cpu data from given flags and initRegs      
    let dataExecute  (flags:Flags) (initRegs:uint32 list) =
        let tRegs (x:uint32 list) =  [0..14] |> List.map(fun n-> (register n, x.[n])) |> Map.ofList
        let tMem: MachineMemory<Instr> = Map.empty 
        let tD = {
                    Fl = {N = flags.FN; C = flags.FC; Z = flags.FZ; V = flags.FV}
                    Regs = tRegs initRegs
                    MM = tMem
                 }
        tD 
    // parse asm string          
    let parseExecute = parseLine None (WA 0ul) asm         
    
    // execute using parsed information and data given
    let runExecute parsedInstr tD = 
        match parsedInstr with
        |  (Ok x) -> execute x.PInstr tD               
        |  (Error e) ->  Error e                                 
    
    let resultExecute = runExecute parseExecute (dataExecute flags initRegs)
    let flagsExecute = match resultExecute with | Ok x -> x.Fl | _ -> failwithf "Execute failed!"
    let regsExecute = match resultExecute with | Ok x -> x.Regs | _ -> failwithf "Execute failed!"
    let expectedRegs = regsExecute |> Map.toList |> List.map (fun (n,v) -> R regNums.[n], int v)
    let expectedFlags = {FN=flagsExecute.N;FZ=flagsExecute.Z; FC=flagsExecute.C;FV=flagsExecute.V}
    // return a tuple of registers and flags after parse-execute
    (expectedRegs,expectedFlags)

let MakeParseTests name tList =
    let singleTest i (input,expected)  =
        testCase (sprintf "Parse Test %s #%d" name i) <| fun () ->
        let actual = parseLine None (WA 0ul) input
        Expecto.Expect.equal actual expected (sprintf "Test parsing of %s" input) 
    tList
    |>List.indexed
    |>List.map (fun (x,y) -> singleTest x y)
    |> Expecto.Tests.testList name

let VisualShiftTest paras name src (outExpected: (Out * int) list * Flags) =
    testCase name <| fun () ->
        let flagsActual, outActual = RunVisualWithFlagsOut paras src
        Expecto.Expect.equal flagsActual (outExpected |> snd) "Status flags don't match"
        let outRegsNoted = 
            outExpected 
            |> fst
            |> List.map fst
        let outActualNoted = 
            outActual.Regs 
            |> List.filter (fun (r,_) -> List.contains r outRegsNoted)
            |> List.sort
        Expecto.Expect.equal outActualNoted (outExpected |> fst |> List.sort) <|
            sprintf "Register outputs>\n%A\n<don't match expected outputs, src=%s" outActual.Regs src

let runParseAndExecute paras =  makeExecute paras.InitFlags paras.InitRegs
let posParas = {defaultParas with InitRegs = rndReg 1 1 ; InitFlags = rndFlag }
let negParas = {defaultParas with InitRegs = rndReg -1 -1 ; InitFlags = rndFlag }
let rndParas = {defaultParas with InitRegs = rndReg -100 100 ; InitFlags = rndFlag }
let sTestPos = VisualShiftTest posParas
let sTestNeg = VisualShiftTest negParas
let sTestRnd = VisualShiftTest rndParas

// run both Shift module and VisUAL for given parameters, asm and test name.
let runTest (p:Params) (name:string) (asm:string) = VisualShiftTest p name asm (runParseAndExecute p asm)

let parseResult opcode rd rm sval s = Ok {
                PInstr = {InstrC = Shift;
                          OpCode = opcode;
                          Rd = rd;
                          Rm = rm;
                          Op2 = sval;
                          SBit = s;};
                PLabel = None;
                PSize = 4u;
                PCond = Cal;}
[<Tests>]
let parseTest =
    MakeParseTests "Shift Module parse tests"
        [
            "LSL R1,R2,R3", parseResult "LSL" R1 R2 (Rs R3) ""
            "LSL r1,r2,r3", parseResult "LSL" R1 R2 (Rs R3) ""
            "LSL R1,R1,#255" ,  parseResult "LSL" R1 R1 (Sh 255) ""
            "LSL R15,R1,R2" , Error "Invalid operands given for Shift Module"
            "LSL R1,R1,#-1" , Error "Invalid operands given for Shift Module"                
            "LSL R11,R1,#256" , Error "Invalid operands given for Shift Module"
        ] 

[<Tests>]
let rrxTest =
    testList "RRX Tests"
        [
        runTest posParas "Positive RRX test" "RRX R0,R0"
        runTest negParas "Negative RRX test" "RRX R0,R0"
        runTest rndParas "Random RRX test" "RRX R0,R0"
        runTest posParas "Positive RRXS test" "RRXS R0,R0"
        runTest negParas "Negative RRXS test" "RRXS R0,R0"
        runTest rndParas "Random RRXS test" "RRXS R0,R0"            
        ]

[<Tests>]
let sTest0 =
    testList "Zero Shift Tests"
        [
            runTest posParas "Positive LSR test" "LSR R0,R0,#0"
            runTest negParas "Negative LSR test" "LSR R0,R0,#0"
            runTest rndParas "Random LSR test" "LSR R0,R0,#0"
            runTest posParas "Positive LSRS test" "LSRS R0,R0,#0"
            runTest negParas "Negative LSRS test" "LSRS R0,R0,#0"
            runTest rndParas "Random LSRS test" "LSRS R0,R0,#0"
            runTest posParas "Positive ASR test" "ASR R0,R0,#0"
            runTest negParas "Negative ASR test" "ASR R0,R0,#0"
            runTest rndParas "Random ASR test" "ASR R0,R0,#0"
            runTest posParas "Positive ASRS test" "ASRS R0,R0,#0"
            runTest negParas "Negative ASRS test" "ASRS R0,R0,#0"
            runTest rndParas "Random ASRS test" "ASRS R0,R0,#0"
            runTest posParas "Positive LSL test" "LSL R0,R0,#0"
            runTest negParas "Negative LSL test" "LSL R0,R0,#0"
            runTest rndParas "Random LSL test" "LSL R0,R0,#0"
            runTest posParas "Positive LSLS test" "LSLS R0,R0,#0"
            runTest negParas "Negative LSLS test" "LSLS R0,R0,#0"
            runTest rndParas "Random LSLS test" "LSLS R0,R0,#0"
            runTest posParas "Positive ROR test" "ROR R0,R0,#0"
            runTest negParas "Negative ROR test" "ROR R0,R0,#0"
            runTest rndParas "Random ROR test" "ROR R0,R0,#0"
            runTest posParas "Positive RORS test" "RORS R0,R0,#0"
            runTest negParas "Negative RORS test" "RORS R0,R0,#0"
            runTest rndParas "Random RORS test" "RORS R0,R0,#0"
        ]

[<Tests>]
let sTest1 = 
    testList "Shift length of 1"
        [
            runTest posParas "Positive LSR test" "LSR R0,R0,#1"
            runTest negParas "Negative LSR test" "LSR R0,R0,#1"
            runTest rndParas "Random LSR test" "LSR R0,R0,#1"
            runTest posParas "Positive LSRS test" "LSRS R0,R0,#1"
            runTest negParas "Negative LSRS test" "LSRS R0,R0,#1"
            runTest rndParas "Random LSRS test" "LSRS R0,R0,#1"
            runTest posParas "Positive ASR test" "ASR R0,R0,#1"
            runTest negParas "Negative ASR test" "ASR R0,R0,#1"
            runTest rndParas "Random ASR test" "ASR R0,R0,#1"
            runTest posParas "Positive ASRS test" "ASRS R0,R0,#1"
            runTest negParas "Negative ASRS test" "ASRS R0,R0,#1"
            runTest rndParas "Random ASRS test" "ASRS R0,R0,#1"
            runTest posParas "Positive LSL test" "LSL R0,R0,#1"
            runTest negParas "Negative LSL test" "LSL R0,R0,#1"
            runTest rndParas "Random LSL test" "LSL R0,R0,#1"
            runTest posParas "Positive LSLS test" "LSLS R0,R0,#1"
            runTest negParas "Negative LSLS test" "LSLS R0,R0,#1"
            runTest rndParas "Random LSLS test" "LSLS R0,R0,#1"
            runTest posParas "Positive ROR test" "ROR R0,R0,#1"
            runTest negParas "Negative ROR test" "ROR R0,R0,#1"
            runTest rndParas "Random ROR test" "ROR R0,R0,#1"
            runTest posParas "Positive RORS test" "RORS R0,R0,#1"
            runTest negParas "Negative RORS test" "RORS R0,R0,#1"
            runTest rndParas "Random RORS test" "RORS R0,R0,#1"
        ]
[<Tests>]
let sTestB = 
    testList "Shift length of boundary values 31,32"
        [
            runTest posParas "Positive LSR test" "LSR R0,R0,#32"
            runTest negParas "Negative LSR test" "LSR R0,R0,#32"
            runTest rndParas "Random LSR test" "LSR R0,R0,#32"
            runTest posParas "Positive LSRS test" "LSRS R0,R0,#32"
            runTest negParas "Negative LSRS test" "LSRS R0,R0,#32"
            runTest rndParas "Random LSRS test" "LSRS R0,R0,#32"
            runTest posParas "Positive ASR test" "ASR R0,R0,#32"
            runTest negParas "Negative ASR test" "ASR R0,R0,#32"
            runTest rndParas "Random ASR test" "ASR R0,R0,#32"
            runTest posParas "Positive ASRS test" "ASRS R0,R0,#32"
            runTest negParas "Negative ASRS test" "ASRS R0,R0,#32"
            runTest rndParas "Random ASRS test" "ASRS R0,R0,#32"
            runTest posParas "Positive LSL test" "LSL R0,R0,#31"
            runTest negParas "Negative LSL test" "LSL R0,R0,#31"
            runTest rndParas "Random LSL test" "LSL R0,R0,#31"
            runTest posParas "Positive LSLS test" "LSLS R0,R0,#31"
            runTest negParas "Negative LSLS test" "LSLS R0,R0,#31"
            runTest rndParas "Random LSLS test" "LSLS R0,R0,#31"
            runTest posParas "Positive ROR test" "ROR R0,R0,#31"
            runTest negParas "Negative ROR test" "ROR R0,R0,#31"
            runTest rndParas "Random ROR test" "ROR R0,R0,#31"
            runTest posParas "Positive RORS test" "RORS R0,R0,#31"
            runTest negParas "Negative RORS test" "RORS R0,R0,#31"
            runTest rndParas "Random RORS test" "RORS R0,R0,#31"
        ]
[<Tests>]
let sTestBE = 
    testList "Shift length of boundary exceeded values 32,33"
        [
            runTest posParas "Positive LSR test" "LSR R0,R0,#33"
            runTest negParas "Negative LSR test" "LSR R0,R0,#33"
            runTest rndParas "Random LSR test" "LSR R0,R0,#33"
            runTest posParas "Positive LSRS test" "LSRS R0,R0,#33"
            runTest negParas "Negative LSRS test" "LSRS R0,R0,#33"
            runTest rndParas "Random LSRS test" "LSRS R0,R0,#33"
            runTest posParas "Positive ASR test" "ASR R0,R0,#33"
            runTest negParas "Negative ASR test" "ASR R0,R0,#33"
            runTest rndParas "Random ASR test" "ASR R0,R0,#33"
            runTest posParas "Positive ASRS test" "ASRS R0,R0,#33"
            runTest negParas "Negative ASRS test" "ASRS R0,R0,#33"
            runTest rndParas "Random ASRS test" "ASRS R0,R0,#33"
            runTest posParas "Positive LSL test" "LSL R0,R0,#32"
            runTest negParas "Negative LSL test" "LSL R0,R0,#32"
            runTest rndParas "Random LSL test" "LSL R0,R0,#32"
            runTest posParas "Positive LSLS test" "LSLS R0,R0,#32"
            runTest negParas "Negative LSLS test" "LSLS R0,R0,#32"
            runTest rndParas "Random LSLS test" "LSLS R0,R0,#32"
            runTest posParas "Positive ROR test" "ROR R0,R0,#32"
            runTest negParas "Negative ROR test" "ROR R0,R0,#32"
            runTest rndParas "Random ROR test" "ROR R0,R0,#32"
            runTest posParas "Positive RORS test" "RORS R0,R0,#32"
            runTest negParas "Negative RORS test" "RORS R0,R0,#32"
            runTest rndParas "Random RORS test" "RORS R0,R0,#32"
        ]

            
let posRegParas = {defaultParas with InitRegs = rndReg 30 100 ; InitFlags = rndFlag }
let negRegParas = {defaultParas with InitRegs = rndReg -100 -1 ; InitFlags = rndFlag }
let rndRegParas = {defaultParas with InitRegs = rndReg -100 100 ; InitFlags = rndFlag }

[<Tests>]
let sRegTest = 
    testList "Register valued shift tests"
        [
        runTest posRegParas "Positive LSR test" "LSR R0,R0,R0"
        runTest negRegParas "Negative LSR test" "LSR R0,R0,R0"
        runTest rndRegParas "Random LSR test" "LSR R0,R0,R0"
        runTest posRegParas "Positive LSRS test" "LSRS R0,R0,R0"
        runTest negRegParas "Negative LSRS test" "LSRS R0,R0,R0"
        runTest rndRegParas "Random LSRS test" "LSRS R0,R0,R0"
        runTest posRegParas "Positive ASR test" "ASR R0,R0,R0"
        runTest negRegParas "Negative ASR test" "ASR R0,R0,R0"
        runTest rndRegParas "Random ASR test" "ASR R0,R0,R0"
        runTest posRegParas "Positive ASRS test" "ASRS R0,R0,R0"
        runTest negRegParas "Negative ASRS test" "ASRS R0,R0,R0"
        runTest rndRegParas "Random ASRS test" "ASRS R0,R0,R0"
        runTest posRegParas "Positive LSL test" "LSL R0,R0,R0"
        runTest negRegParas "Negative LSL test" "LSL R0,R0,R0"
        runTest rndRegParas "Random LSL test" "LSL R0,R0,R0"
        runTest posRegParas "Positive LSLS test" "LSLS R0,R0,R0"
        runTest negRegParas "Negative LSLS test" "LSLS R0,R0,R0"
        runTest rndRegParas "Random LSLS test" "LSLS R0,R0,R0"
        runTest posRegParas "Positive ROR test" "ROR R0,R0,R0"
        runTest negRegParas "Negative ROR test" "ROR R0,R0,R0"
        runTest rndRegParas "Random ROR test" "ROR R0,R0,R0"
        runTest posRegParas "Positive RORS test" "RORS R0,R0,R0"
        runTest negRegParas "Negative RORS test" "RORS R0,R0,R0"
        runTest rndRegParas "Random RORS test" "RORS R0,R0,R0"
        ]                        

[<Tests>]
let sRegtoRegTest = 
    testList "Register valued shift to another register tests"
        [
        runTest posRegParas "Positive LSR test" "LSR R0,R2,R1"
        runTest negRegParas "Negative LSR test" "LSR R0,R2,R1"
        runTest rndRegParas "Random LSR test" "LSR R0,R2,R1"
        runTest posRegParas "Positive LSRS test" "LSRS R0,R2,R1"
        runTest negRegParas "Negative LSRS test" "LSRS R0,R2,R1"
        runTest rndRegParas "Random LSRS test" "LSRS R0,R2,R1"
        runTest posRegParas "Positive ASR test" "ASR R0,R2,R1"
        runTest negRegParas "Negative ASR test" "ASR R0,R2,R1"
        runTest rndRegParas "Random ASR test" "ASR R0,R2,R1"
        runTest posRegParas "Positive ASRS test" "ASRS R0,R2,R1"
        runTest negRegParas "Negative ASRS test" "ASRS R0,R2,R1"
        runTest rndRegParas "Random ASRS test" "ASRS R0,R2,R1"
        runTest posRegParas "Positive LSL test" "LSL R0,R2,R1"
        runTest negRegParas "Negative LSL test" "LSL R0,R2,R1"
        runTest rndRegParas "Random LSL test" "LSL R0,R2,R1"
        runTest posRegParas "Positive LSLS test" "LSLS R0,R2,R1"
        runTest negRegParas "Negative LSLS test" "LSLS R0,R2,R1"
        runTest rndRegParas "Random LSLS test" "LSLS R0,R2,R1"
        runTest posRegParas "Positive ROR test" "ROR R0,R2,R1"
        runTest negRegParas "Negative ROR test" "ROR R0,R2,R1"
        runTest rndRegParas "Random ROR test" "ROR R0,R2,R1"
        runTest posRegParas "Positive RORS test" "RORS R0,R2,R1"
        runTest negRegParas "Negative RORS test" "RORS R0,R2,R1"
        runTest rndRegParas "Random RORS test" "RORS R0,R2,R1"   
        ]                                

