// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake



module MonoStaticLinking =
    open System
    open System.IO
    open System.Reflection

    //https://github.com/mvkra/MkBundleAssemblyScanner
    let loadAssembly providedAssemblyMap (assemblyName: AssemblyName) = 
        printfn "Attempting to load assembly %s" assemblyName.Name
        
        // Brute force scan either provided assemblies or current assembly context (includes GAC)
        match providedAssemblyMap |> Map.tryFind assemblyName.Name with
        | Some(x) -> 
            printfn "Assembly provided in arguments, loading provided assembly %s from %s" assemblyName.Name x
            Assembly.ReflectionOnlyLoadFrom(x)
        | None -> 
            printfn "Assembly not provided in arguments, loading assembly implictly %s" assemblyName.Name
            try
                Assembly.ReflectionOnlyLoad(assemblyName.Name)
            with 
            | ex -> 
                printfn "Assembly loading directly failed, trying full name %s" assemblyName.FullName
                Assembly.ReflectionOnlyLoad(assemblyName.FullName)

    let rec getAssemblyDeps loadAssembly alreadyFetchedAssemblies (assemblyName: AssemblyName) = [
        match alreadyFetchedAssemblies |> Map.tryFind (assemblyName.Name) with
        | Some(ass) -> 
            printfn "Already found assembly %s, skipping" assemblyName.Name
            yield! []
        | None -> 
            let currentAss : Assembly = loadAssembly assemblyName
            printfn "Found assembly %s" assemblyName.Name
            yield currentAss
            let newMap = alreadyFetchedAssemblies |> Map.add assemblyName.Name currentAss
            for refAss in currentAss.GetReferencedAssemblies() do
                printfn "Recursively scanning for dependendies in %s" refAss.Name
                yield! getAssemblyDeps loadAssembly newMap refAss
        ]

    let getAllDependenciesForExecutable allAssemblyNames (executable: string) = [ 
    
        let providedAssemblyMap = 
            allAssemblyNames
            |> Seq.append [executable]
            |> Seq.map (fun x -> (Path.GetFileNameWithoutExtension(x), x))
            |> Map.ofSeq

        let loadAssembly = loadAssembly providedAssemblyMap

        let assemblyName = AssemblyName.GetAssemblyName(executable)
        let currentAssembly: Assembly = loadAssembly assemblyName

        yield! 
            getAssemblyDeps loadAssembly Map.empty assemblyName
            |> List.map (fun x -> x.Location) 
        ]   

    
    let mkbundle workingDir args = Shell.Exec("mkbundle",args, workingDir)

    let linkStatically workingDir allAssemblyNames (executable: string) =
       
        printfn "%A " allAssemblyNames
        let allReferences =
            getAllDependenciesForExecutable allAssemblyNames executable
            |> List.append (allAssemblyNames |> Seq.toList)
            |> List.distinct


        let executableOutput = FileInfo(executable).Name.ToLower().Replace(".exe","d")
        let args = [
            "--nodeps"
            "--static"
            "--skip-scan"
            "-z" //needs zlib1g-dev
            executable
            allReferences |> String.concat " "
            sprintf "-o ./%s" executableOutput
        ]
        printfn "%A" allReferences
        let result = mkbundle workingDir (args |> String.concat " ")
        if result <> 0 then
            failwithf "Static linking of %s failed" executable
        else
            allAssemblyNames |> Seq.iter FileUtils.rm_rf 
            executable |> FileUtils.rm_rf 
        ()


// Directories
let buildDir  = "./build/"
let deployDir = "./deploy/"


// Filesets
let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"

let exeReference dir =
    !! (sprintf "%s/*.exe" dir)
    |> Seq.head
let dllReferences dir =
    !! (sprintf "%s/*.dll" dir)

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir]
)

Target "Build" (fun _ ->
    // compile all projects below src/app/
    MSBuildRelease buildDir "Build" appReferences
    |> Log "AppBuild-Output: "
)

Target "StaticLink" (fun _ ->
    let exe = exeReference buildDir
    let dlls = dllReferences buildDir
    MonoStaticLinking.linkStatically buildDir dlls exe
)

Target "Deploy" (fun _ ->
    !! (buildDir + "/**/*.*")
    -- "*.zip"
    |> Zip buildDir (deployDir + "ApplicationName." + version + ".zip")
)

// Build order
"Clean"
  ==> "Build"
  ==> "StaticLink"
  ==> "Deploy"

// start build
RunTargetOrDefault "StaticLink"
