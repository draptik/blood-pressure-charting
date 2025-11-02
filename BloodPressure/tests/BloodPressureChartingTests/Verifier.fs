[<RequireQualifiedAccess>]
module Verifier

open System.Threading.Tasks

open Argon
open DiffEngine
open VerifyTests
open VerifyXunit

let setupDiffRunner = DiffRunner.Disabled <- true

let customizedVerifySettings =
  let settings = VerifySettings()
  settings.UseDirectory "VerifiedSnapshots"
  settings.AddExtraSettings(fun s -> s.NullValueHandling <- NullValueHandling.Include)

  // Scrubbing
  settings.ScrubLinesWithReplace(fun line ->
    System.Text.RegularExpressions.Regex.Replace(line, "id=\"[\w-]+\"", "id=\"SCRUBBED\""))

  settings.ScrubLinesWithReplace(fun line ->
    System.Text.RegularExpressions.Regex.Replace(line, "#clip\w+", "#clipSCRUBBED"))

  settings.ScrubLinesWithReplace(fun line ->
    System.Text.RegularExpressions.Regex.Replace(line, "trace scatter trace\w+", "trace scatter SCRUBBED"))

  settings.ScrubLinesWithReplace(fun line ->
    System.Text.RegularExpressions.Regex.Replace(line, "clip-path=\"url\(#[\w-]+\)", "clip-path=\"url(#SCRUBBED)"))

  settings

let inline verify_internal_with_settings (settings: VerifySettings) (value: 't :> obj) =
  Verifier.Verify(value :> obj, settings).ToTask() :> Task

let inline verify_xml_internal_using_settings (settings: VerifySettings) (value: string) =
  Verifier.VerifyXml(value, settings).ToTask() :> Task

// The public API -------------------------------------------------------------
let verify value =
  setupDiffRunner
  verify_internal_with_settings customizedVerifySettings value

let verifyXml value =
  setupDiffRunner
  verify_xml_internal_using_settings customizedVerifySettings value