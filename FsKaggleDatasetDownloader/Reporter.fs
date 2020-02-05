namespace FsKaggleDatasetDownloader

open System
open System.IO
open FsKaggleDatasetDownloader.Core

module Reporter =

    let inline asMB (bytes: int64) = float bytes / (1024.0*1024.0)
    
    let ProgressBar (info : ReportingData) = 
        let filename = Path.GetFileName(info.Notes).Replace("\\", "/")
        let status =
            sprintf "%.02f of %.02fMB @ %.2fKB/s" 
                (asMB info.BytesRead) (asMB info.TotalBytes) (info.BytesPerSecond/1024.0)

        let percentage = float info.BytesRead / float info.TotalBytes
        let barTotalWidth = 
            try 
                Console.WindowWidth - 11 - status.Length - filename.Length
            with _ -> 
                100 - status.Length

        let barCompleted =
            percentage * float barTotalWidth
            |> Math.Ceiling
            |> int

        let percentageStr =
            sprintf " %.2f%%"
                ((if percentage > 0.99 then 1.0 else percentage)
                 * 100.00)

        // printf has issues with putting \r at the end on a jupyter notebook it seems
        System.Console.Write(sprintf "%s [%s] %s\r" filename ((percentageStr.PadLeft(barCompleted, '|')).PadRight(barTotalWidth, ' ')) status)