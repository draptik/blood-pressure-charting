namespace BloodPressureCharting

open System
open Plotly.NET
open Plotly.NET.LayoutObjects
open Plotly.NET.StyleParam

type ImportError =
    | FileNotFound of string

type AppError =
    | ImporterError of ImportError
    | OtherError of string
    
module Importer =
    let tryImportData (path: string) : Result<string[], AppError> =
        try
            System.IO.File.ReadAllLines(path) |> Ok
        with
        | :? System.IO.FileNotFoundException as e ->
            e.Message |> FileNotFound |> ImporterError |> Error
        | e ->
            e.Message |> OtherError |> Error

module Data =
    type TimeStamp = DateTime
    type Systolic = int
    type Diastolic = int
    type Pulse = int
    type Measurement = {
        TimeStamp: TimeStamp
        Systolic: Systolic
        Diastolic: Diastolic
        Pulse: Pulse
    }
    type Measurements = Measurement list
    
    let parseTimeStamp (input: string) =
        let format = "yyyy-MM-dd-HH:mm"
        DateTime.ParseExact(input, format, System.Globalization.CultureInfo.InvariantCulture)
    
    let parseMeasurement (line: string) =
        let parts = line.Split(' ')
        {
            TimeStamp = parseTimeStamp  parts[0]
            Systolic = int parts[1]
            Diastolic = int parts[2]
            Pulse = int parts[3]
        }
        
    let parseMeasurements (lines: string seq) =
        lines
        |> Seq.map parseMeasurement
        |> Seq.toList

    (*
        Layout inspired by: 

        Home blood pressure data visualization for the management of 
        hypertension: using human factors and design principles
        
        https://www.ncbi.nlm.nih.gov/pmc/articles/PMC8340525/
    *)
    let plot (measurements: Measurements) =

        let systolicColor = Color.fromString "green"
        let diastolicColor = Color.fromString "orange"
        
        let layoutTemplate =
            Layout.init(
                Width = 1200
            )

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
            Shape.init(
                ShapeType = ShapeType.Rectangle,
                X0 = x0,
                X1 = x1,
                Y0 = y0,
                Y1 = y1,
                Opacity = defaultShapeOpacity,
                FillColor = color
            )
        
        let shapeSystolic = createShape xMin xMax healthySystolicMin healthySystolicMax systolicColor
        let shapeDiastolic = createShape xMin xMax healthyDiastolicMin healthyDiastolicMax diastolicColor
        
        let createLine x y name color =
            Chart.Line (
                x = x,
                y = y,
                Name = name,
                LineColor = color,
                ShowMarkers = true,
                MarkerSymbol = MarkerSymbol.Circle)

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