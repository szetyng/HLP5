module VProgram 

open Expecto
open VisualTest.Visual
open VisualTest.VTest

/// configuration for this testing framework      
/// configuration for expecto. Note that by default tests will be run in parallel
/// this is set by the fields oif testParas above
let expectoConfig = { Expecto.Tests.defaultConfig with 
                        parallel = defaultParas.Parallel
                        parallelWorkers = 4 // try increasing this if CPU use is less than 100%
                }

[<EntryPoint>]
let main _ = 
    initCaches defaultParas
    let rc = runTestsInAssembly expectoConfig [||]
    // runTests defaultConfig parseTest |> ignore
    finaliseCaches defaultParas
    System.Console.ReadKey() |> ignore                
    // 0 // return an integer exit code - 0 if all tests pass
    rc // return an integer exit code - 0 if all tests pass

