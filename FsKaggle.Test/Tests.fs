namespace FsKaggle.Test

open System
open Xunit
open System.Net.Http
open System.Net
open System.IO
open System.Threading
open FsKaggle.Common
open FsKaggle.CLI
open FsKaggle
open Xunit.Abstractions
open FsUnit.Xunit


module Common =

    [<Fact>]
    let ``DownloadStreamAsync Test Progress Reporting with 10MB``() =

        let payloadSize = 10_485_760

        use message = new HttpResponseMessage(HttpStatusCode.OK)
        message.Content <- new ByteArrayContent(Array.create<byte> payloadSize 1uy)

        let mockHandler =
            { new System.Net.Http.HttpMessageHandler() with
                member x.SendAsync(request, cancellationToken) = async { return message } |> Async.StartAsTask }

        use client = new HttpClient(mockHandler)
        use memstr = new MemoryStream()

        let reportResult = ResizeArray<ProgressData>()
        let desiredSamples = 64
        let bufferSize = payloadSize / desiredSamples
        let sampleInterval = ByteCount <| int64 bufferSize

        let report (info: ProgressData) = reportResult.Add info

        DownloadStreamAsync
            { BufferLength = bufferSize
              Url = "http://0.0.0.0"
              Client = client
              Token = Some(CancellationToken())
              WriteAsync = memstr.WriteAsync
              ReportOptions =
                  Some
                      { ReportTitle = "Test"
                        SampleInterval = sampleInterval
                        ReportCallback = report } }
        |> Async.AwaitTask
        |> Async.RunSynchronously

        Assert.Equal(int64 payloadSize, memstr.Length)
        Assert.Equal(desiredSamples, reportResult.Count)

    [<Fact>]
    let ``DownloadStreamAsync Test Progress Reporting with Time interval at 10MB``() =

        let payloadSize = 10_485_760 * 100

        use message = new HttpResponseMessage(HttpStatusCode.OK)
        message.Content <- new ByteArrayContent(Array.create<byte> payloadSize 1uy)
       
        let mockHandler =
            { new System.Net.Http.HttpMessageHandler() with
                member x.SendAsync(request, cancellationToken) = async { return message } |> Async.StartAsTask }

        use client = new HttpClient(mockHandler)

        use memstr = new MemoryStream()

        let reportResult = ResizeArray<ProgressData>()
        let bufferSize = 128
        let expectedInterval = 200.0
        let sampleInterval = Time <| TimeSpan.FromMilliseconds(expectedInterval)

        let report (info: ProgressData) =
            reportResult.Add(info)

        DownloadStreamAsync
            { BufferLength = bufferSize
              Url = "http://0.0.0.0"
              Client = client
              Token = Some(CancellationToken())
              WriteAsync = memstr.WriteAsync
              ReportOptions =
                  Some
                      { ReportTitle = "Test"
                        SampleInterval = sampleInterval
                        ReportCallback = report } }
        |> Async.AwaitTask
        |> Async.RunSynchronously

        int64 payloadSize  |> should equal memstr.Length
        reportResult.Count |> should be (greaterThan 1)

        let actualInterval =
            reportResult
            |> Seq.map (fun report -> report.TimeStamp)
            |> Seq.pairwise
            |> Seq.averageBy (fun (x, y) -> (y - x).TotalMilliseconds)

        let errorTolerance = 0.15*expectedInterval

        actualInterval |> should (equalWithin errorTolerance) expectedInterval

    type DownloadFileTest(outputHelper: ITestOutputHelper) =
        let output = outputHelper
        let tempPath = Path.GetTempFileName()
        do output.WriteLine(sprintf "Temp file used for this test: [%s]" tempPath)

        [<Fact>]
        member x.``Download data to temporary file``() =

            let payloadSize = 1_048_576

            use message = new HttpResponseMessage(HttpStatusCode.OK)
            message.Content <- new ByteArrayContent(Array.create<byte> payloadSize 1uy)

            let mockHandler =
                { new System.Net.Http.HttpMessageHandler() with
                    member x.SendAsync(request, cancellationToken) = async { return message } |> Async.StartAsTask }

            use client = new HttpClient(mockHandler)

            DownloadFileAsync "http://0.0.0.0" tempPath client None None |> Async.RunSynchronously

            let fileInfo = FileInfo tempPath
            Assert.True(fileInfo.Exists)
            Assert.Equal(fileInfo.Length, int64 payloadSize)

        interface IDisposable with
            member x.Dispose() =
                if File.Exists tempPath then File.Delete tempPath

module Kaggle =
    type CredentialsTest(outputHelper: ITestOutputHelper) =
        let output = outputHelper
        let tempPath = Path.GetTempFileName()

        do
            output.WriteLine(sprintf "Temp file used for this test: [%s]" tempPath)
            File.WriteAllText(tempPath, """{"username":"areslazarus","key":"1234567890abcdefghijklmnopqrstuv"}""")

        [<Fact>]
        member x.``Load credentials from temporary file``() =

            let creds = Credentials.LoadFromPath tempPath

            Assert.Equal("areslazarus", creds.username)
            Assert.Equal("1234567890abcdefghijklmnopqrstuv", creds.key)

        [<Fact>]
        member x.``Load credentials from string``() =
            let creds =
                tempPath
                |> File.ReadAllText
                |> Credentials.LoadFromString

            Assert.Equal("areslazarus", creds.username)
            Assert.Equal("1234567890abcdefghijklmnopqrstuv", creds.key)

        [<Fact>]
        member x.``Authorize http client for Kaggle API``() =
            let creds = Credentials.LoadFromPath tempPath

            use client = new HttpClient()

            let (AuthorizedClient authClient) = Credentials.AuthorizeClient client creds

            Assert.Equal
                ("Basic YXJlc2xhemFydXM6MTIzNDU2Nzg5MGFiY2RlZmdoaWprbG1ub3BxcnN0dXY=",
                 authClient.DefaultRequestHeaders.Authorization.ToString())

        interface IDisposable with
            member x.Dispose() =
                if File.Exists tempPath then File.Delete tempPath


    type MsgHandlerFixture() =
        let owner = "TheDatasetOwner"
        let datasetName = "TheDataset"
        let datasetFileName = "samples.csv"
        let fileSize = 1048576
        let datasetSize = fileSize * 10
        let downloadDatafileMsg =
            new HttpResponseMessage(HttpStatusCode.OK, Content = new ByteArrayContent(Array.create<byte> fileSize 1uy))
        let downloadDatasetMsg =
            new HttpResponseMessage(HttpStatusCode.OK,
                                    Content = new ByteArrayContent(Array.create<byte> datasetSize 2uy))
        let badRequestMsg = new HttpResponseMessage(HttpStatusCode.BadRequest)

        let handler =
            { new System.Net.Http.HttpMessageHandler() with
                member x.SendAsync(request, cancellationToken) =
                    if request.RequestUri.AbsoluteUri =
                        sprintf "%sdatasets/download/%s/%s/%s" Constants.BaseApiUrl owner datasetName datasetFileName then
                        async { return downloadDatafileMsg } |> Async.StartAsTask
                    elif request.RequestUri.AbsoluteUri =
                             sprintf "%sdatasets/download/%s/%s" Constants.BaseApiUrl owner datasetName then
                        async { return downloadDatasetMsg } |> Async.StartAsTask
                    else
                        failwithf "We don't like %s" request.RequestUri.AbsoluteUri }
        //async { return badRequestMsg } |> Async.StartAsTask }

        member x.Handler = handler

        interface IDisposable with
            member x.Dispose() =
                downloadDatafileMsg.Dispose()
                downloadDatasetMsg.Dispose()
                badRequestMsg.Dispose()

    type KaggleApiTest(outputHelper: ITestOutputHelper, msgHandler: MsgHandlerFixture) =
        let output = outputHelper
        let handler = msgHandler.Handler

        let credentialsPath = Path.GetTempFileName()
        let datasetZipPath = Path.Combine(Path.GetTempPath(), "TheDataset.zip")
        let datasetFilePath = Path.Combine(Path.GetTempPath(), "samples.zip")

        let clearFiles() =
            if File.Exists credentialsPath then File.Delete credentialsPath
            if File.Exists datasetZipPath then File.Delete datasetZipPath
            if File.Exists datasetFilePath then File.Delete datasetFilePath

        let datasetOptions client request =
            { DatasetInfo =
                  { Owner = "TheDatasetOwner"
                    Dataset = "TheDataset"
                    Request = request }
              Credentials = Client client
              DestinationFolder = Path.GetTempPath()
              Overwrite = false
              CancellationToken = None
              ReportingCallback = None }

        do
            clearFiles()
            File.WriteAllText
                (credentialsPath, """{"username":"areslazarus","key":"1234567890abcdefghijklmnopqrstuv"}""")

        [<Fact>]
        member x.``Download dataset file from Kaggle API``() =
            use client = new HttpClient(handler)

            Filename "samples.csv"
            |> datasetOptions (AuthorizedClient client)
            |> Kaggle.DownloadDatasetAsync
            |> Async.RunSynchronously

            let fileInfo = FileInfo datasetFilePath
            Assert.True(fileInfo.Exists)
            Assert.Equal(1048576L, fileInfo.Length)

        [<Fact>]
        member x.``Download full dataset from Kaggle API``() =
            use client = new HttpClient(handler)

            DatasetFile.All
            |> datasetOptions (AuthorizedClient client)
            |> Kaggle.DownloadDatasetAsync
            |> Async.RunSynchronously

            let fileInfo = FileInfo datasetZipPath
            Assert.True(fileInfo.Exists)
            Assert.Equal(10485760L, fileInfo.Length)

        interface IClassFixture<MsgHandlerFixture>


        interface IDisposable with
            member x.Dispose() = clearFiles()

module CLI =
    open Argu
    open CLI

    [<Fact>]
    let ``Example: selfishgene historical-hourly-weather-data``() =
        let args = [| "selfishgene"; "historical-hourly-weather-data" |]
        let results = ArgumentParser.Create<CLI.Args>().ParseCommandLine args

        let expected =
            { Owner = args.[0]
              Dataset = args.[1]
              Request = All }

        let actual = ParseDatasetInfo results

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Example: selfishgene historical-hourly-weather-data -f humidity.csv``() =
        let args = [| "selfishgene"; "historical-hourly-weather-data"; "-f"; "humidity.csv" |]
        let results = ArgumentParser.Create<CLI.Args>().ParseCommandLine args

        let expected =
            { Owner = args.[0]
              Dataset = args.[1]
              Request = Filename "humidity.csv" }

        let actual = ParseDatasetInfo results

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Example: selfishgene historical-hourly-weather-data -f humidity.csv -o Data"``() =
        let args = [| "selfishgene"; "historical-hourly-weather-data"; "-f"; "humidity.csv"; "-o"; "Data" |]
        let results = ArgumentParser.Create<CLI.Args>().ParseCommandLine args

        let expected =
            { Owner = args.[0]
              Dataset = args.[1]
              Request = Filename args.[3] }

        let actual = ParseDatasetInfo results

        Assert.Equal(expected, actual)
        Assert.Equal("Data", ParseOutputFolder results)


    [<Fact>]
    let ``Example: selfishgene historical-hourly-weather-data -f humidity.csv -o Data -c tempFile``() =
        let kagglePath = Path.GetTempFileName()
        let args =
            [| "selfishgene"; "historical-hourly-weather-data"; "-f"; "humidity.csv"; "-o"; "Data"; "-c"; kagglePath |]
        let results = ArgumentParser.Create<CLI.Args>().ParseCommandLine args

        let expected =
            { Owner = args.[0]
              Dataset = args.[1]
              Request = Filename args.[3] }

        let actual = ParseDatasetInfo results

        Assert.Equal(expected, actual)
        Assert.Equal("Data", ParseOutputFolder results)
        Assert.Equal(kagglePath, ParseKaggleJsonPath results)
