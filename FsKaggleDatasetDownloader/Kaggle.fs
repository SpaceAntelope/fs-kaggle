namespace FsKaggleDatasetDownloader

open System.Threading
open System.IO
open FsKaggleDatasetDownloader.Core
open System.Net.Http
open System.Net.Http.Headers
open System.Text.Json
open System

module Kaggle =

    module Constants =
        let BaseApiUrl = "https://www.kaggle.com/api/v1/"

    type AuthorizedClient = AuthorizedClient of HttpClient

    /// <summary>Deserialization of kaggle.json</summary>
    type Credentials() =
        member val username: string = null with get, set
        member val key: string = null with get, set

        static member LoadFrom(path: string): Credentials =
            use reader = new StreamReader(path)
            let json = reader.ReadToEnd()
            JsonSerializer.Deserialize(json)

        static member AuthorizeClient (client: HttpClient) (auth: Credentials) =
            let authToken =
                sprintf "%s:%s" auth.username auth.key
                |> Text.ASCIIEncoding.ASCII.GetBytes
                |> Convert.ToBase64String

            client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Basic", authToken)

            AuthorizedClient client

    type DatasetFile =
        | Filename of string
        | CompleteDatasetZipped
        member x.ToOption() =
            match x with
            | Filename filename -> Some filename
            | _ -> None

    type DatasetInfo =
        { Owner: string
          Dataset: string
          Request: DatasetFile }
        member x.ToUrl() =
            match x.Request with
            | Filename filename ->
                sprintf "%sdatasets/download/%s/%s/%s" Constants.BaseApiUrl x.Owner x.Dataset filename
            | CompleteDatasetZipped -> sprintf "%sdatasets/download/%s/%s" Constants.BaseApiUrl x.Owner x.Dataset

    type DownloadDatasetOptions =
        { DatasetInfo: DatasetInfo
          AuthorizedClient: AuthorizedClient
          DestinationFolder: string
          Overwrite: bool
          CancellationToken: CancellationToken option
          ReportingCallback: (ReportingData -> unit) option }

    let DownloadDatasetAsync(options: DownloadDatasetOptions) =
        let url = options.DatasetInfo.ToUrl()
        let fileName =
            options.DatasetInfo.Request.ToOption() |> Option.defaultValue (options.DatasetInfo.Dataset + ".zip")
        let destinationFile = Path.Combine(options.DestinationFolder, fileName)

        if File.Exists destinationFile then
            if options.Overwrite
            then File.Delete destinationFile
            else failwithf "File [%s] already exists." destinationFile

        let (AuthorizedClient client) = options.AuthorizedClient

        DownloadFileAsync url destinationFile client (options.CancellationToken) (options.ReportingCallback)