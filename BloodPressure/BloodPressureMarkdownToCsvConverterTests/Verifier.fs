[<RequireQualifiedAccess>]
module Verifier

open System.Threading.Tasks

open Argon
open DiffEngine
open VerifyTests
open VerifyXunit

let setupDiffRunner =
    DiffRunner.Disabled <- true

let customizedVerifySettings =
    let settings = VerifySettings ()
    settings.UseDirectory "VerifiedSnapshots"
    settings.AddExtraSettings (fun s -> s.NullValueHandling <- NullValueHandling.Include)
    settings

let inline verify_internal_with_settings (settings: VerifySettings) (value: 't :> obj) =
    Verifier.Verify(value :> obj, settings).ToTask () :> Task

// The public API
let verify value =
    setupDiffRunner
    verify_internal_with_settings customizedVerifySettings value
