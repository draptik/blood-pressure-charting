namespace BloodPressureCharting

open System
open FsToolkit.ErrorHandling
open Microsoft.FSharp.Core
open Plotly.NET
open Plotly.NET.LayoutObjects
open Plotly.NET.StyleParam
open Plotly.NET.ImageExport

type ImportError = FileNotFound of string

type AppError =
  | ImporterError of ImportError
  | InvalidDateFormatError of string
  | NotAnIntError of string
  | OtherError of string

type AppErrors = AppError list

module Importer =
  let tryImportData (path: string) : Result<string[], AppError> =
    try
      System.IO.File.ReadAllLines(path) |> Ok
    with
    | :? System.IO.FileNotFoundException as e -> e.Message |> FileNotFound |> ImporterError |> Error
    | e -> e.Message |> OtherError |> Error

module Data =
  type TimeStamp = DateTime
  type Systolic = int
  type Diastolic = int
  type Pulse = int
  type Comment = string

  type Measurement = {
    TimeStamp: TimeStamp
    Systolic: Systolic
    Diastolic: Diastolic
    Pulse: Pulse
    Comment: Comment option
  }

  type Measurements = Measurement list

  let tryParseInt s : Result<int, AppError> =
    try
      s |> int |> Ok
    with :? FormatException ->
      s |> NotAnIntError |> Error

  let tryParseTimeStamp (input: string) : Result<TimeStamp, AppError> =
    let format = "yyyy-MM-dd-H:mm"

    try
      DateTime.ParseExact(input, format, System.Globalization.CultureInfo.InvariantCulture)
      |> Ok
    with :? FormatException ->
      input |> InvalidDateFormatError |> Error

  let tryParseMeasurement (line: string) : Result<Measurement, AppError> =
    let parts = line.Split(',')

    if parts.Length < 4 then
      Error(OtherError "Invalid number of parts")
    else
      let timeStampRaw = parts[0]
      let systolicRaw = parts[1]
      let diastolicRaw = parts[2]
      let pulseRaw = parts[3]
      let comment = if parts.Length > 4 then Some(parts[4].Trim()) else None

      let parseResult =
        result {
          let! timeStamp =
            tryParseTimeStamp timeStampRaw
            |> Result.mapError (fun _ -> OtherError $"Invalid timestamp: '{timeStampRaw}' in line '{line}'")

          let! systolic =
            tryParseInt systolicRaw
            |> Result.mapError (fun _ -> OtherError $"Invalid systolic value: '{systolicRaw}' in line '{line}'")

          let! diastolic =
            tryParseInt diastolicRaw
            |> Result.mapError (fun _ -> OtherError $"Invalid diastolic value: '{diastolicRaw}' in line '{line}'")

          let! pulse =
            tryParseInt pulseRaw
            |> Result.mapError (fun _ -> OtherError $"Invalid pulse value: '{pulseRaw}' in line '{line}'")

          return {
            TimeStamp = timeStamp
            Systolic = systolic
            Diastolic = diastolic
            Pulse = pulse
            Comment = comment
          }
        }

      parseResult

  // This function aggregates a list of `Result<Measurement, AppError>`:
  //
  // In case of any errors (!), it accumulates them.
  // In case of success, it accumulates the measurements.
  let accumulateResults (results: Result<Measurement, AppError> list) : Result<Measurements, AppErrors> =
    let folder acc result =
      match acc, result with
      | Error appErrors, Ok _ ->
        // previous errors present, current result is Ok -> return errors
        Error appErrors
      | Ok _, Error e ->
        // previous result was Ok, current result is Error -> return current error
        Error [ e ]
      | Ok measurements, Ok measurement ->
        // both are Ok -> accumulate measurements
        Ok(measurement :: measurements)
      | Error appErrors, Error e ->
        // both are Error -> accumulate errors
        Error(e :: appErrors)

    let initialAcc = Ok([]: Measurement list)

    let accumulatedResults = List.fold folder initialAcc results

    accumulatedResults |> Result.map List.rev // reverse the list to ensure chronological order

  let tryParseMeasurements (lines: string seq) : Result<Measurements, AppErrors> =
    lines |> Seq.map tryParseMeasurement |> List.ofSeq |> accumulateResults

  (*
        Layout inspired by:

        Home blood pressure data visualization for the management of
        hypertension: using human factors and design principles

        https://www.ncbi.nlm.nih.gov/pmc/articles/PMC8340525/
    *)
  let generateChart (measurements: Measurements) =

    let systolicColor = Color.fromString "green"
    let diastolicColor = Color.fromString "orange"

    let layoutTemplate = Layout.init (Width = 1200)

    let timeStamps = measurements |> List.map (_.TimeStamp)
    let systolic = measurements |> List.map (_.Systolic)
    let diastolic = measurements |> List.map (_.Diastolic)
    let comments = measurements |> List.map (_.Comment)

    let xMin = timeStamps |> List.min
    let xMax = timeStamps |> List.max
    let yMax = (systolic @ diastolic) |> List.max |> (+) 10

    let healthySystolicMin = 90
    let healthySystolicMax = 140
    let healthyDiastolicMin = 60
    let healthyDiastolicMax = 90

    let defaultShapeOpacity = 0.2

    let createShape x0 x1 y0 y1 color =
      Shape.init (
        ShapeType = ShapeType.Rectangle,
        X0 = x0,
        X1 = x1,
        Y0 = y0,
        Y1 = y1,
        Opacity = defaultShapeOpacity,
        FillColor = color
      )

    let shapeSystolic =
      createShape xMin xMax healthySystolicMin healthySystolicMax systolicColor

    let shapeDiastolic =
      createShape xMin xMax healthyDiastolicMin healthyDiastolicMax diastolicColor

    let createScatter x y comments name color =
      let points =
        List.zip3 x y comments
        |> List.mapi (fun i (xi, yi, maybeComment) ->
          // Symbol depends on presence of comment
          let symbol =
            match maybeComment with
            | Some _ -> MarkerSymbol.Square
            | None -> MarkerSymbol.Circle

          // increase size of measurement when it has a comment
          let size =
            match maybeComment with
            | Some _ -> 10
            | None -> 6

          // this ensures that the legend is only displayed for the first entry
          let showLegend = i = 0

          // low level marker configuration
          let marker = TraceObjects.Marker.init (Color = color, Symbol = symbol, Size = size)

          let comment =
            match maybeComment with
            | Some c -> c
            | None -> ""

          Chart.Point([ xi ], [ yi ], Name = name, Marker = marker, MultiText = [ comment ], ShowLegend = false))

      // we have to manually add the line which connects the points
      let line = Chart.Line(x, y, Name = name, LineColor = color, ShowLegend = true)

      Chart.combine (line :: points)

    Chart.combine [
      createScatter timeStamps systolic comments "Systolic" systolicColor
      createScatter timeStamps diastolic comments "Diastolic" diastolicColor
    ]
    |> Chart.withXAxisStyle (TitleText = "time", MinMax = (xMin, xMax))
    |> Chart.withYAxisStyle (TitleText = "blood pressure [mmHg]", MinMax = (0, yMax))
    |> Chart.withTitle "Blood Pressure Chart"
    |> Chart.withShapes [ shapeSystolic; shapeDiastolic ]
    |> Chart.withLayout layoutTemplate

  let showInBrowser (measurements: Measurements) : unit =
    measurements |> generateChart |> Chart.show

  /// Currently only used for testing
  let toSvg (chart: GenericChart) =
    chart |> Chart.toSVGString (Width = 840, Height = 480)