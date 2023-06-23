namespace Sudoku.Domain

module Types =
    open System
    open System.Diagnostics

    type TimerState =
        { Duration: TimeSpan
          Stopwatch: Stopwatch }

    type CellStatus =
        | Empty
        | Filled of int

    type Cell =
        { Row: int
          Column: int
          Region: int
          Value: int
          Index: int
          Status: CellStatus }

    // type Cell =
    //     { Label: string
    //       Peers: string list
    //       Status: CellStatus
    //       PossibleValues: int list }

    type CellState = { Cells: Cell list }

    type HighScoreState = { noop: bool }

    type GameState =
        { TimerState: TimerState
          CellState: CellState }

    type AppState =
        { highScoreState: HighScoreState
          gameState: GameState }

    type TimerMsg =
        | Tick
        | Start
        | Pause
        | Reset
        | Restart

    type GameStateMsg = | ResetBoard

    type Links =
        | AvaloniaRepository
        | AvaloniaAwesome
        | FuncUIRepository
        | FuncUISamples

    type HighScoreMsg = OpenUrl of Links

    type GameMsg =
        | TimerMsg of TimerMsg
        | GameStateMsg of GameStateMsg

    type AppMsg =
        | HighScoreMsg of HighScoreMsg
        | GameMsg of GameMsg

    type PauseTimer = TimerState -> TimerState

    type StartTimer = TimerState -> TimerState

    type ResetTimer = TimerState -> TimerState

    type RestartTimer = TimerState -> TimerState
