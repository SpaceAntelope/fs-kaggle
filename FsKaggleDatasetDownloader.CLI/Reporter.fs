namespace FsKaggleDatasetDownloader.CLI

open System
open System.IO
open FsKaggleDatasetDownloader.Core

module Reporter =

    let ProgressBar (info : ReportingData) = 
        let status =
            sprintf "Downloading file [%s] -- %dKB of %.02fMB received at %.2fKB/s" (Path.GetFileName(info.Notes).Replace("\\", "/"))
                info.BytesRead (float info.TotalBytes / 1024.0 / 1024.0) (info.BytesPerSecond/1024.0)

        let percentage = float info.BytesRead / float info.TotalBytes
        let barTotalWidth = Console.WindowWidth - 11 - status.Length

        let barCompleted =
            percentage * float barTotalWidth
            |> Math.Ceiling
            |> int

        let percentageStr =
            sprintf " %.2f%%"
                ((if percentage > 0.99 then 1.0 else percentage)
                 * 100.00)

        printf "[%s] %s\r" ((percentageStr.PadLeft(barCompleted, '|')).PadRight(barTotalWidth, ' ')) status