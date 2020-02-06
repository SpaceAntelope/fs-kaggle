namespace FsKaggle

open System.Threading
open System.IO
open FsKaggle.Core
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

        static member LoadFromPath(path: string): Credentials =
            use reader = new StreamReader(path)
            let json = reader.ReadToEnd()
            JsonSerializer.Deserialize(json)

        static member LoadFromString(source: string): Credentials = JsonSerializer.Deserialize(source)

        static member AuthorizeClient (client: HttpClient) (auth: Credentials) =
            let authToken =
                sprintf "%s:%s" auth.username auth.key
                |> Text.ASCIIEncoding.ASCII.GetBytes
                |> Convert.ToBase64String

            client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Basic", authToken)

            AuthorizedClient client

    type CredentialsSource =
        | Path of string
        | Source of string
        | Creds of Credentials
        | Client of AuthorizedClient

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
          Credentials: CredentialsSource
          DestinationFolder: string
          Overwrite: bool
          CancellationToken: CancellationToken option
          ReportingCallback: (ReportingData -> unit) option }


    let DownloadDatasetAsync(options: DownloadDatasetOptions) =
        let url = options.DatasetInfo.ToUrl()

        let fileName =
            options.DatasetInfo.Request.ToOption()
            |> Option.map (fun filename -> Path.GetFileNameWithoutExtension(filename) + ".zip")
            |> Option.defaultValue (options.DatasetInfo.Dataset + ".zip")

        let destinationFile = Path.Combine(options.DestinationFolder, fileName)

        if File.Exists destinationFile then
            if options.Overwrite
            then File.Delete destinationFile
            else failwithf "File [%s] already exists." destinationFile

        let authClient =
            match options.Credentials with
            | Source str -> Credentials.LoadFromString str |> Credentials.AuthorizeClient(new HttpClient())
            | Path path -> Credentials.LoadFromPath path |> Credentials.AuthorizeClient(new HttpClient())
            | Creds creds -> creds |> Credentials.AuthorizeClient(new HttpClient())
            | Client client -> client

        async {
            let (AuthorizedClient client) = authClient
            try
                do! DownloadFileAsync url destinationFile client (options.CancellationToken)
                        (options.ReportingCallback)
            finally
                client.Dispose()
        }
