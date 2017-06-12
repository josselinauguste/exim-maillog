module Zipper

open System.Diagnostics
open System.IO

let exec command arguments =
  let p = new Process()
  p.StartInfo.FileName <- command
  p.StartInfo.Arguments <- arguments
  p.StartInfo.UseShellExecute <- false
  p.StartInfo.RedirectStandardOutput <- true
  p.Start() |> ignore
  p.StandardOutput.ReadToEnd()

let unzip filename =
  exec "gunzip" (sprintf "-c %s" filename)

let unzipLines filename =
  (unzip filename).Split '\n' |> Seq.ofArray