namespace Sudoku

module Timer =
    open System
    open Avalonia.FuncUI.DSL
    open Avalonia.Controls
    open Avalonia.Layout
    open Avalonia.FuncUI.Helpers
    open System.Diagnostics
    open Monadic
    open Sudoku.Domain.Types
    open Avalonia.Media

    let startStopwatch (stopwatch: Stopwatch) = stopwatch.Start()

    let newTimerState (stopwatch: Stopwatch) =
        { Duration = TimeSpan.FromSeconds 0
          Stopwatch = stopwatch }

    let init = new Stopwatch() |> tee startStopwatch |> newTimerState

    let pauseTimer state =
        state |> tee (fun st -> st.Stopwatch.Stop())

    let startTimer state =
        state |> tee (fun st -> st.Stopwatch.Start())

    let restartTimer state =
        state |> tee (fun st -> st.Stopwatch.Restart())

    let resetTimer state =
        state |> tee (fun st -> st.Stopwatch.Reset())

    let updateTimer state =
        { state with
            Duration = state.Stopwatch.Elapsed }

    let update (msg: TimerMsg) (state: TimerState) : TimerState =
        match msg with
        | Tick -> updateTimer state
        | Start -> startTimer state
        | Pause -> pauseTimer state
        | Reset -> resetTimer state
        | Restart -> restartTimer state


    let displayTime (state: TimerState) : string =
        match state.Duration.Hours with
        | 0 -> String.Format("{0:mm\\:ss}", state.Duration)
        | _ -> String.Format("{0:hh\\:mm\\:ss}", state.Duration)


    let view (state: TimerState) (dispatch) =
        DockPanel.create
            [ DockPanel.children
                  [ Border.create
                        [ Border.dock Dock.Right
                          Border.padding 20.
                          Border.child (
                              TextBlock.create
                                  [ TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                    TextBlock.dock Dock.Top
                                    TextBlock.fontSize 48.0
                                    TextBlock.textAlignment TextAlignment.Center
                                    TextBlock.minWidth 400.0
                                    TextBlock.text (displayTime state) ]
                          ) ] ] ]



module SidePanel =
    open Sudoku.Domain.Types
    open Avalonia.FuncUI.DSL
    open Avalonia.Controls
    open Avalonia.Layout
    open Avalonia.FuncUI
    open Avalonia.FuncUI.Types
    open Sudoku.Domain.Types

    let init = ()

    let gameControls (state: GameState) (dispatch) : IView =
        StackPanel.create
            [ StackPanel.verticalAlignment VerticalAlignment.Center
              StackPanel.horizontalAlignment HorizontalAlignment.Center
              StackPanel.spacing 20.
              StackPanel.children
                  [ Button.create
                        [ Button.content "Play"
                          Button.classes [ "success" ]
                          Button.horizontalContentAlignment HorizontalAlignment.Center
                          Button.minWidth 200.
                          Button.onClick (fun _ -> dispatch (TimerMsg Start)) ]
                    Button.create
                        [ Button.content "Pause"
                          Button.classes [ "normal" ]
                          Button.horizontalContentAlignment HorizontalAlignment.Center
                          Button.minWidth 200.
                          Button.onClick (fun _ -> dispatch (TimerMsg Pause)) ]
                    Button.create
                        [ Button.content "New Game"
                          Button.classes [ "danger" ]
                          Button.horizontalContentAlignment HorizontalAlignment.Center
                          Button.minWidth 200.
                          Button.onClick (fun _ ->
                              dispatch (GameStateMsg ResetBoard)
                              dispatch (TimerMsg Restart)) ]
                    Button.create
                        [ Button.content "Exit"
                          Button.classes [ "danger" ]
                          Button.horizontalContentAlignment HorizontalAlignment.Center
                          Button.minWidth 200.
                          Button.onClick (fun _ -> exit 0) ] ] ]
        |> generalize
    //TextBlock.create [ TextBlock.text "some game controls" ] |> generalize

    let settingsView (state: GameState) (dispatch) : IView =
        TextBlock.create [ TextBlock.text "some settings" ] |> generalize

    let cellDetails (state: GameState) (dispatch) : IView =
        TextBlock.create [ TextBlock.text "some cell details go here" ] |> generalize

    let sidePanelTabView (state: GameState) (dispatch) : IView =
        TabControl.create
            [ TabControl.tabStripPlacement Dock.Top
              TabControl.horizontalAlignment HorizontalAlignment.Center
              TabControl.margin 20.
              TabControl.padding 20.
              TabControl.viewItems
                  [ TabItem.create
                        [ TabItem.header "Cell Details"
                          TabItem.fontSize 16.
                          TabItem.content (cellDetails state dispatch) ]
                    TabItem.create
                        [ TabItem.header "Game Settings"
                          TabItem.fontSize 16.
                          TabItem.content (settingsView state dispatch) ]
                    TabItem.create
                        [ TabItem.header "Game Controls"
                          TabItem.fontSize 16.
                          TabItem.content (gameControls state dispatch) ] ] ]
        |> generalize


    let view (state: GameState) (dispatch) =
        StackPanel.create
            [ StackPanel.orientation Orientation.Vertical
              StackPanel.children [ Timer.view state.TimerState dispatch; sidePanelTabView state dispatch ] ]

module SudokuLogic =
    open System
    open Sudoku.Domain.Types

    // Attempt to generate sudoku board using a translation of the following example in VB.NET.
    // https://www.codeproject.com/Articles/23206/Sudoku-Algorithm-Generates-a-Valid-Sudoku-in-0-018



    let getRowFromIndex index = index / 9

    let getColumnFromIndex index = index % 9

    let getRegionFromIndex index =
        let row = getRowFromIndex index
        let column = getColumnFromIndex index

        match row with
        | row when List.contains row [ 0..2 ] ->
            match column with
            | column when List.contains column [ 0..2 ] -> 1
            | column when List.contains column [ 3..5 ] -> 2
            | _ -> 3
        | row when List.contains row [ 3..5 ] ->
            match column with
            | column when List.contains column [ 0..2 ] -> 4
            | column when List.contains column [ 3..5 ] -> 5
            | _ -> 6
        | _ ->
            match column with
            | column when List.contains column [ 0..2 ] -> 7
            | column when List.contains column [ 3..5 ] -> 8
            | _ -> 9

    let createSquare index value =
        { Row = getRowFromIndex index
          Column = getColumnFromIndex index
          Region = getRegionFromIndex index
          Status = Filled value
          Value = value
          Index = index }


    let initAvailableChoices () = Array.create 81 [| 1..9 |]

    let initSquaresList () : Cell option[] = Array.create 81 None

    let availableCount (available: int array array) index = available[index] |> Array.length

    let isPeer cellOne cellTwo =
        cellOne.Row = cellTwo.Row
        || cellOne.Column = cellTwo.Column
        || cellOne.Region = cellTwo.Region


    let hasConflicts (cells: Cell option array) (cellToTest: Cell) =
        cells
        |> Array.map (fun square ->
            match square with
            | None -> false
            | Some square ->
                match isPeer square cellToTest with
                | true -> square.Value = cellToTest.Value
                | false -> false)
        |> Array.where (fun conflict -> conflict = true)
        |> Array.length
        |> fun conflicts -> conflicts > 0

    let generateGrid () =
        let available = initAvailableChoices ()
        let squares = initSquaresList ()
        let random = Random()
        let mutable cellCounter: int = 0

        while cellCounter < 81 do
            let count = availableCount available cellCounter

            if count <> 0 then
                let availableIndex = random.Next(count - 1)
                let availableValue = available[cellCounter][availableIndex]
                let newSquare = createSquare cellCounter availableValue

                match hasConflicts squares newSquare with
                | false ->
                    squares[cellCounter] <- Some newSquare
                    available[cellCounter] <- Array.removeAt availableIndex available[cellCounter]
                    cellCounter <- cellCounter + 1
                | true -> available[cellCounter] <- Array.removeAt availableIndex available[cellCounter]
            else
                available[cellCounter] <- [| 1..9 |]
                squares[cellCounter - 1] <- None
                cellCounter <- cellCounter - 1

        squares |> Array.choose id |> List.ofArray

    let init = { Cells = generateGrid () }

module SudokuBoard =
    open Sudoku.Domain.Types
    open Avalonia.FuncUI.DSL
    open Avalonia.Controls
    open Avalonia.Layout
    open Avalonia.Controls.Primitives
    open Avalonia

    let thickLine = 5
    let thinLine = 2

    let noLine = 0

    let borderColor = "gray"

    let update (msg: GameStateMsg) (state: CellState) =
        match msg with
        | ResetBoard ->
            { state with
                Cells = SudokuLogic.generateGrid () }

    let getRightSideThickness cell =
        match cell.Column with
        | 8 -> thickLine
        | _ -> noLine

    let getLeftSideThickness cell =
        match cell.Column with
        | col when List.contains col [ 0; 3; 6 ] -> thickLine
        | _ -> thinLine

    let getTopSideThickness cell =
        match cell.Row with
        | row when List.contains row [ 0; 3; 6 ] -> thickLine
        | _ -> thinLine

    let getBottomSideThickness cell =
        match cell.Row with
        | 8 -> thickLine
        | _ -> noLine

    let getBorderThickness cell =
        (getLeftSideThickness cell),
        (getTopSideThickness cell),
        (getRightSideThickness cell),
        (getBottomSideThickness cell)



    let view (state: CellState) (dispatch) =

        UniformGrid.create
            [ UniformGrid.columns 9
              UniformGrid.rows 9
              UniformGrid.minHeight 500
              UniformGrid.minWidth 500
              UniformGrid.margin (0, 50)
              UniformGrid.background "#39383b"
              UniformGrid.horizontalAlignment HorizontalAlignment.Center
              UniformGrid.verticalAlignment VerticalAlignment.Center
              UniformGrid.children (
                  state.Cells
                  |> List.map (fun cell ->
                      let (left, top, right, bottom) = getBorderThickness cell

                      Border.create
                          [ Border.borderThickness (left, top, right, bottom)
                            Border.borderBrush borderColor
                            Border.cornerRadius -2
                            Border.child (
                                TextBlock.create
                                    [ TextBlock.horizontalAlignment HorizontalAlignment.Center
                                      TextBlock.verticalAlignment VerticalAlignment.Center
                                      TextBlock.fontSize 24
                                      TextBlock.text (
                                          match cell.Status with
                                          | Filled value -> value |> string
                                          | Empty -> cell.Index |> string
                                      ) ]
                            ) ])
              ) ]


module GameBoard =
    open Sudoku.Domain.Types
    open Avalonia.FuncUI.DSL
    open Avalonia.Controls
    open Avalonia.Layout

    let init: GameState =
        { TimerState = Timer.init
          CellState = SudokuLogic.init }

    let view (state: GameState) (dispatch) =
        StackPanel.create
            [ StackPanel.orientation Orientation.Horizontal
              StackPanel.horizontalAlignment HorizontalAlignment.Center
              StackPanel.verticalAlignment VerticalAlignment.Center
              StackPanel.children [ SudokuBoard.view state.CellState dispatch; SidePanel.view state dispatch ]

              ]

    let update (msg: GameMsg) (state: GameState) : GameState =
        match msg with
        | TimerMsg timerMsg ->
            Timer.update timerMsg state.TimerState
            |> fun timerState -> { state with TimerState = timerState }
        | GameStateMsg gameStateMsg ->
            SudokuBoard.update gameStateMsg state.CellState
            |> fun cellState -> { state with CellState = cellState }
