namespace FsKaggleDatasetDownloader.CLI

module Program =
    open System
    open System.IO
    open System.Net.Http
    open FsKaggleDatasetDownloader.Core
    open FsKaggleDatasetDownloader.Kaggle
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

        let results =
            ArgumentParser.Create<CLI.Args>(programName = "FsKaggleDatasetDownloader.CLI", errorHandler = CLI.arguErrorHandler)
                .ParseCommandLine argv

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

        if whatIfEnabled then
            0
        else
            use client = new HttpClient()

            { DatasetInfo = datasetInfo
              AuthorizedClient =
                  kaggleJsonPath
                  |> Credentials.LoadFrom
                  |> Credentials.AuthorizeClient client
              DestinationFolder = destinationFolder
              Overwrite = overwriteEnabled
              CancellationToken = None
              ReportingCallback = Some Reporter.ProgressBar }
            |> DownloadDatasetAsync
            |> Async.RunSynchronously

            0 // return an integer exit code
