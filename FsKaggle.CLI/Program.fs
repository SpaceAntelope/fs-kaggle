namespace FsKaggle.CLI

module Program =
    open System.IO
    open FsKaggle
    open FsKaggle
    open Argu

    let EnsureKaggleJsonExists path =
        if path |> (File.Exists >> not)
        then failwithf "Could not locate credential file in path '%s'" path
        else path

    let CreateOutputFolderIfMissing(path: string) =
        let fullPath =
            // if System.Text.RegularExpressions.Regex.IsMatch(path.Trim(), "^[\\\\//\\.]") then
            //     Path.Combine(Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly().Location), path)
            //     |> Path.GetFullPath
            // else
            path

        if fullPath |> (File.Exists >> not) then Directory.CreateDirectory(fullPath) |> ignore

        //printf "%s verified..." fullPath
        fullPath

    [<EntryPoint>]
    let main argv =
        let parser = ArgumentParser.Create<CLI.Args>(programName = "FsKaggle.CLI", errorHandler = CLI.arguErrorHandler)
        let results = parser.ParseCommandLine argv

        match results.TryGetResult(CLI.Args.Examples) with
        | Some _ ->
            CLI.PrintExamples()
            0
        | None ->
            let kaggleJsonPath =
                results
                |> CLI.ParseKaggleJsonPath
                |> EnsureKaggleJsonExists
            let destinationFolder =
                results
                |> CLI.ParseOutputFolder
                |> CreateOutputFolderIfMissing

            let datasetInfo = results |> CLI.ParseDatasetInfo
            let overwriteEnabled = results |> CLI.ParseOverwrite
            let whatIfEnabled = results |> CLI.ParseWhatIf

            if not whatIfEnabled then
                { DatasetInfo = datasetInfo
                  Credentials = Path kaggleJsonPath
                  DestinationFolder = destinationFolder
                  Overwrite = overwriteEnabled
                  CancellationToken = None
                  ReportingCallback = Some Reporter.ProgressBar }
                |> Kaggle.DownloadDatasetAsync
                |> Async.RunSynchronously

            0 // return an integer exit code
