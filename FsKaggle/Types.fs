namespace FsKaggle

open System
open System.Threading
open System.Threading.Tasks
open System.Net.Http

type ProgressData =
    { TimeStamp: DateTime
      Notes: string
      BytesRead: int64
      TotalBytes: int64
      BytesPerSecond: float }

type ReportCallback = ProgressData -> unit

type WriteAsyncCallback = array<byte> * int * int -> Task

type SampleInterval =
    | ByteCount of int64
    | Time of TimeSpan

type ReportOptions =
    { ReportTitle: string
      ReportCallback: ReportCallback
      SampleInterval: SampleInterval }

type DownloadOptions =
    { Url: string
      Client: HttpClient
      Token: CancellationToken option
      BufferLength: int

      WriteAsync: WriteAsyncCallback
      ReportOptions: ReportOptions option }
