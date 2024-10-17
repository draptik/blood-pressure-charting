﻿namespace BloodPressureCharting

open System
open Microsoft.FSharp.Core
open Plotly.NET
open Plotly.NET.LayoutObjects
open Plotly.NET.StyleParam

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
        Comment: Comment
    }

    type Measurements = Measurement list

    let tryParseInt s : Result<int, AppError> =
        try
            s |> int |> Ok
        with :? FormatException ->
            s |> NotAnIntError |> Error

    let tryParseTimeStamp (input: string) : Result<TimeStamp, AppError> =
        let format = "yyyy-MM-dd-HH:mm"

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
            let timeStamp = parts[0]
            let systolic = parts[1]
            let diastolic = parts[2]
            let pulse = parts[3]
            let comment = if parts.Length > 4 then parts[4] else ""

            // probably a better way to do this. Applicative validation?
            match tryParseTimeStamp timeStamp with
            | Error e -> Error e
            | Ok timeStamp ->
                match tryParseInt systolic with
                | Error _ -> Error(OtherError $"Invalid Systolic '{systolic}'")
                | Ok systolic ->
                    match tryParseInt diastolic with
                    | Error _ -> Error(OtherError $"Invalid Diastolic '{diastolic}'")
                    | Ok diastolic ->
                        match tryParseInt pulse with
                        | Error _ -> Error(OtherError $"Invalid Pulse '{pulse}'")
                        | Ok pulse ->
                            Ok {
                                TimeStamp = timeStamp
                                Systolic = systolic
                                Diastolic = diastolic
                                Pulse = pulse
                                Comment = comment
                            }

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
    let plot (measurements: Measurements) =

        let systolicColor = Color.fromString "green"
        let diastolicColor = Color.fromString "orange"

        let layoutTemplate = Layout.init (Width = 1200)

        let timeStamps = measurements |> List.map (_.TimeStamp)
        let systolic = measurements |> List.map (_.Systolic)
        let diastolic = measurements |> List.map (_.Diastolic)

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

        let createLine x y name color =
            Chart.Line(
                x = x,
                y = y,
                Name = name,
                LineColor = color,
                ShowMarkers = true,
                MarkerSymbol = MarkerSymbol.Circle
            )

        Chart.combine [
            createLine timeStamps systolic "Systolic" systolicColor
            createLine timeStamps diastolic "Diastolic" diastolicColor
        ]
        |> Chart.withXAxisStyle (TitleText = "time", MinMax = (xMin, xMax))
        |> Chart.withYAxisStyle (TitleText = "blood pressure [mmHg]", MinMax = (0, yMax))
        |> Chart.withTitle "Blood Pressure Chart"
        |> Chart.withShapes [ shapeSystolic; shapeDiastolic ]
        |> Chart.withLayout layoutTemplate
        |> Chart.show