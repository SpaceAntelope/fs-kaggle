namespace FsKaggleDatasetDownloader.Interop

open System
open System.Threading
open FsKaggleDatasetDownloader.Core
open FsKaggleDatasetDownloader
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
    member val KaggleJsonPath: string = null with get, set
    member val DestinationFolder: string = null with get, set
    member val Overwrite: bool = false with get, set
    member val CancellationToken: Nullable<CancellationToken> = Nullable() with get, set
    member val ReportingCallback: Action<ReportingData> = null with get, set

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

module Kaggle =

    let DownloadDatasetAsync(options: DownloadDatasetOptions) =
        options
        |> DownloadDatasetOptions.convert
        |> Kaggle.DownloadDatasetAsync
        |> Async.StartAsTask
