namespace FsKaggleDatasetDownloader.CLI

open Argu
open System
open System.IO
open FsKaggleDatasetDownloader.Kaggle

module CLI =

    let PrintExamples() =
        let printex example =
            let current = Console.ForegroundColor
            Console.ForegroundColor <- ConsoleColor.Green
            printfn "\t\t%s\n" example
            Console.ForegroundColor <- current

        printfn "EXAMPLES:"
        printfn "\tDownload the entire dataset from https://www.kaggle.com/selfishgene/historical-hourly-weather-data :"
        printex "\tFsKaggleDatasetDownloader.CLI selfishgene historical-hourly-weather-data"
        printfn "\tOnly download the file humidity.csv from within the dataset:"
        printex "\tFsKaggleDatasetDownloader.CLI selfishgene historical-hourly-weather-data -f humidity.csv"
        printfn "\tDownload said file in a Data folder in your user directory:"
        printex "\tFsKaggleDatasetDownloader.CLI selfishgene historical-hourly-weather-data -f humidity.csv -o Data"
        printfn "\tLook for the kaggle API token in a kaggle.json file within the same directory:"
        printex
            "\tFsKaggleDatasetDownloader.CLI selfishgene historical-hourly-weather-data -f humidity.csv -o Data -c kaggle.json"


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
        | [<Unique; MainCommand; AltCommandLine("-ds")>] Dataset of Owner: string * Name: string
        | [<Unique; AltCommandLine("-o")>] Output of string
        | [<Unique; AltCommandLine("-f")>] File of string
        | [<Unique>] Overwrite
        | [<Unique>] WhatIf
        //| [<Unique;AltCommandLine("-ex")>] Examples
        interface IArgParserTemplate with
            member arg.Usage =
                match arg with
                | Credentials _ -> "Path to credentials file, normally kaggle.json. Defaults to ~/.kaggle/kaggle.json"
                //| Examples _ -> "Show usage examples."
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
            | None -> DatasetFile.CompleteDatasetZipped

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
