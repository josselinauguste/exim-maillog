#r "packages/FSharp.Text.RegexProvider/lib/net40/FSharp.Text.RegexProvider.dll"
#load "zipper.fs"

open System.IO
open System
open FSharp.Text.RegexProvider

(*
  Types
*)
type LogLine = {
  Id: string
  Recipient: string;
  Timestamp: DateTime;
  Flag: Flag
}
and Flag = Arrival | Delivery

(*
  Parser Exim4
*)
module Exim4 =
  type Exim4Regex = Regex<"""^(?<Timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\s(?<Id>[0-9a-zA-Z\-]+)\s(?<Flag>=>|<=|->)\s(?<Recipient>[^\s]+@[^\s]+\.\w+)""">

  let parseFlag = function
      | "=>" -> Delivery
      | "->" -> Delivery
      | "<=" -> Arrival
      | _ -> raise (Exception "Malformed flag")

  let parse (context: DirectoryInfo) (raw: string) =
    let eximMatch = raw |> Exim4Regex().TypedMatch
    if eximMatch.Success then
      Some {
        Id = eximMatch.Id.Value;
        Recipient = eximMatch.Recipient.Value;
        Timestamp = DateTime.Parse(eximMatch.Timestamp.Value);
        Flag = eximMatch.Flag.Value |> parseFlag
      }
    else
      None

(*
  Logique d'agrégation de logs rotatés
*)
module LogRotated =
  let parse parser files =
    let readLines (file: FileInfo) =
      if file.Extension = ".gz" then
        Zipper.unzipLines file.FullName
      else
        File.ReadLines file.FullName

    files
    |> List.filter (fun (f: FileInfo) -> f.Name.StartsWith "mainlog")
    |> List.map (fun (f: FileInfo) -> (f.Directory, (readLines f) |> List.ofSeq))
    |> List.fold (fun logs (dir: DirectoryInfo, lines) -> logs @ (List.map (parser dir) lines)) []
    |> List.choose id

let parseExim4Logs = LogRotated.parse Exim4.parse

(*
  Let's do this
*)
let logLines =
  DirectoryInfo("./data").GetFiles("*.*", SearchOption.AllDirectories)
  |> List.ofArray
  |> parseExim4Logs

logLines
|> List.filter (fun l -> l.Flag = Delivery)
|> List.groupBy (fun l -> l.Timestamp.Date)
|> List.map (fun (date, ls) -> (date, (ls |> List.length)))
