namespace FsKaggle.Interop

open System
open System.IO
open System.Threading
open FsKaggle.Common
open FsKaggle
open System.Net.Http

[<AllowNullLiteral>]
type DatasetInfo() =
    member val Owner: string = null with get, set
    member val Dataset: string = null with get, set
    member val Request: string = null with get, set

module DatasetInfo =
    let convert (source: DatasetInfo): Kaggle.DatasetInfo =
        { Owner = source.Owner
          Dataset = source.Dataset
          Request =
              match source.Request with
              | null -> Kaggle.DatasetFile.CompleteDatasetZipped
              | filename -> Kaggle.DatasetFile.Filename filename }

type DownloadDatasetOptions() =
    member val DatasetInfo: DatasetInfo = null with get, set
    member val KaggleJsonPath: string = Common.defaultKaggleJsonPath with get, set
    member val DestinationFolder: string = "." with get, set
    member val Overwrite: bool = false with get, set
    member val CancellationToken: Nullable<CancellationToken> = Nullable() with get, set
    member val ReportingCallback: Action<ProgressData> = Action<ProgressData>(Reporter.ProgressBar) with get, set

module DownloadDatasetOptions =
    let convert (source: DownloadDatasetOptions): Kaggle.DownloadDatasetOptions =
        { DatasetInfo = source.DatasetInfo |> DatasetInfo.convert
          Credentials = Kaggle.CredentialsSource.Path source.KaggleJsonPath
          DestinationFolder = source.DestinationFolder
          Overwrite = source.Overwrite
          CancellationToken =
              source.CancellationToken
              |> box
              |> Option.ofObj
              |> Option.map (fun token -> token :?> CancellationToken)
          ReportingCallback =
              source.ReportingCallback
              |> Option.ofObj
              |> Option.map (fun action -> action.Invoke) }

[<Sealed>]
type Kaggle private () =

    static member DownloadDatasetAsync(options: DownloadDatasetOptions) =
        options
        |> DownloadDatasetOptions.convert
        |> FsKaggle.Kaggle.DownloadDatasetAsync
        |> Async.StartAsTask

    static member DownloadDatasetAsync(options: DatasetInfo) =
        DownloadDatasetOptions(DatasetInfo = options)
        |> DownloadDatasetOptions.convert
        |> FsKaggle.Kaggle.DownloadDatasetAsync
        |> Async.StartAsTask
