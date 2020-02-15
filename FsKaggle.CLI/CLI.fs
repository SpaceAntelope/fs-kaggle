namespace FsKaggle.CLI

open Argu
open System
open System.IO
open FsKaggle

module CLI =

    let PrintExamples() =
        let printex example =
            let current = Console.ForegroundColor
            Console.ForegroundColor <- ConsoleColor.Green
            printfn "    %s\n" example
            Console.ForegroundColor <- current

        printfn "If you are not using this CLI as a dotnet tool, remember to substitute \"fskaggle\" with the name of your executable."
        printfn ""
        printfn "EXAMPLES:"
        printfn ""
        printfn "    Download the entire dataset from https://www.kaggle.com/dataset-owner/dataset-name :"
        printex "    fskaggle dataset-owner dataset-name"
        printfn "    Only download the file file.csv from within the dataset:"
        printex "    fskaggle dataset-owner dataset-name -f file.csv"
        printfn "    Download file in ./Data:"
        printex "    fskaggle dataset-owner dataset-name -f file.csv -o Data"
        printfn "    Same, but with the kaggle API token in a kaggle.json file within the same directory and also overwrite any existing files:"
        printex
            "    fskaggle dataset-owner dataset-name -f file.csv -o Data -c kaggle.json --overwrite"

    let arguErrorHandler =
        ProcessExiter
            (colorizer =
                function
                | ErrorCode.HelpText ->
                    PrintExamples()
                    None
                | _ -> Some ConsoleColor.Red)

    [<NoAppSettings>]
    type Args =
        | [<Unique; AltCommandLine("-c")>] Credentials of string
        | [<Unique; MainCommand>] Dataset of Owner: string * Name: string
        | [<Unique; AltCommandLine("-o")>] Output of string
        | [<Unique; AltCommandLine("-f")>] File of string
        | [<Unique>] Overwrite
        | [<Unique>] WhatIf
        | [<Unique;AltCommandLine("-x");AltCommandLine("-ex")>] Examples
        interface IArgParserTemplate with
            member arg.Usage =
                match arg with
                | Credentials _ -> "Path to credentials file, normally kaggle.json. Defaults to ~/.kaggle/kaggle.json"
                | Examples _ -> "Show usage examples."
                | Dataset _ -> "The name of the dataset and the kaggle user under whom it is published."
                | Output _ ->
                    "The folder to download the dataset in. If missing we'll attempt to create it. Default is current folder."
                | File _ ->
                    "Download a particular file from the dataset. If ommited the entire dataset will be downloaded as a zip."
                | Overwrite -> "Overwrite already existing files."
                | WhatIf -> "Validate all arguements but don't actually query kaggle."

    let ParseDatasetInfo(results: ParseResults<Args>) =
        let (owner, dataset) = results.GetResult(Args.Dataset)

        let downloadMode =
            match results.TryGetResult Args.File with
            | Some file -> DatasetFile.Filename file
            | None -> DatasetFile.All

        { Owner = owner
          Dataset = dataset
          Request = downloadMode }

    let ParseKaggleJsonPath(results: ParseResults<Args>) =
        results.GetResult
            (Args.Credentials,
             defaultValue =
                 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kaggle/kaggle.json"))

    let ParseOutputFolder(results: ParseResults<Args>) = results.GetResult(Args.Output, defaultValue = ".")

    let ParseWhatIf(results: ParseResults<Args>) = results.TryGetResult Args.WhatIf |> Option.isSome

    let ParseOverwrite(results: ParseResults<Args>) = results.TryGetResult Args.Overwrite |> Option.isSome
