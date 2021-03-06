namespace FsKaggle

open System.Net.Http
open System.Threading
open System.IO
open System
open FSharp.Control.Tasks.V2
open System.Threading.Tasks

module Common =
    let defaultKaggleJsonPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kaggle/kaggle.json")

    let DownloadStreamAsync(options: DownloadOptions) =
        let cancellationToken = options.Token |> Option.defaultValue (CancellationToken())

        task {
            use! response = options.Client.GetAsync
                                (options.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)

            response.EnsureSuccessStatusCode() |> ignore

            let total =
                if response.Content.Headers.ContentLength.HasValue
                then response.Content.Headers.ContentLength.Value
                else -1L

            use! contentStream = response.Content.ReadAsStreamAsync()

            let mutable timeOfLastSample = DateTime.Now
            let mutable dataSinceLastSample = 0L
            let mutable totalBytesRead = 0L
            let mutable isMoreToRead = true

            let buffer = Array.create options.BufferLength 0uy

            while isMoreToRead && not cancellationToken.IsCancellationRequested do
                // Read data and write to stream
                let! read = contentStream.ReadAsync(buffer, 0, options.BufferLength)
                if read > 0 then
                    do! options.WriteAsync(buffer, 0, read)

                    totalBytesRead <- totalBytesRead + int64 read
                    dataSinceLastSample <- dataSinceLastSample + int64 read
                else
                    isMoreToRead <- false

                // Report progress
                match options.ReportOptions with
                | Some reportOptions ->
                    let isTimeToReport =
                        match reportOptions.SampleInterval with
                        | ByteCount interval when dataSinceLastSample >= interval -> true
                        | Time interval when (DateTime.Now - timeOfLastSample) >= interval -> true
                        | _ when not isMoreToRead && dataSinceLastSample > 0L -> true
                        | _ -> false

                    if isTimeToReport then
                        let elapsedSec = (DateTime.Now - timeOfLastSample).TotalSeconds
                        let bytesPerSec = float dataSinceLastSample / elapsedSec

                        { Notes = reportOptions.ReportTitle
                          BytesRead = totalBytesRead
                          TotalBytes = total
                          TimeStamp = DateTime.Now
                          BytesPerSecond = bytesPerSec }
                        |> reportOptions.ReportCallback

                        timeOfLastSample <- DateTime.Now
                        dataSinceLastSample <- 0L
                | None -> ()
        }

    let DownloadFileAsync (url: string) destinationPath (client: HttpClient)
        (cancellationToken: CancellationToken option) (report: ReportCallback option) =
        let bufferLength = 8192

        async {

            use fileStream =
                new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferLength, true)

            do! DownloadStreamAsync
                    { Url = url
                      Client = client
                      Token = cancellationToken
                      BufferLength = bufferLength
                      WriteAsync = fileStream.WriteAsync
                      ReportOptions =
                          report
                          |> Option.map (fun callback ->
                              { ReportTitle = Path.GetFileName(destinationPath)
                                ReportCallback = callback
                                SampleInterval = Time <| TimeSpan.FromMilliseconds(350.0) }) }
                |> Async.AwaitTask
        }
