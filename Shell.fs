namespace Sudoku

/// This is the main module of your application
/// here you handle all of your child pages as well as their
/// messages and their updates, useful to update multiple parts
/// of your application, Please refer to the `view` function
/// to see how to handle different kinds of "*child*" controls
module Shell =
    open Elmish
    open Avalonia.Controls
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI
    open Avalonia.FuncUI.Hosts
    open Avalonia.FuncUI.Elmish
    open Avalonia.Threading
    open System
    open Domain.Types

    let init =
        let highScoreState, aboutCmd = HighScores.init
        let gameState = GameBoard.init

        { highScoreState = highScoreState
          gameState = gameState },
        // If your children controls don't emit any commands
        // in the init function, you can just return Cmd.none
        // otherwise, you can use a batch operation on all of them
        // you can add more init commands as you need
        Cmd.batch [ aboutCmd ]

    let update (msg: AppMsg) (state: AppState) : AppState * Cmd<_> =
        match msg with
        | HighScoreMsg highScoreMsg ->
            let highScoreState, cmd = HighScores.update highScoreMsg state.highScoreState

            { state with
                highScoreState = highScoreState },
            // map the message to the kind of message
            // your child control needs to handle
            Cmd.map HighScoreMsg cmd
        | GameMsg gameMsg ->
            let gameState = GameBoard.update gameMsg state.gameState
            { state with gameState = gameState }, Cmd.none

    let view (state: AppState) (dispatch) =
        DockPanel.create
            [ DockPanel.children
                  [ TabControl.create
                        [ TabControl.tabStripPlacement Dock.Top
                          TabControl.viewItems
                              [ TabItem.create
                                    [ TabItem.header "Game"
                                      TabItem.content (GameBoard.view state.gameState (GameMsg >> dispatch)) ]
                                TabItem.create
                                    [ TabItem.header "High Scores"
                                      TabItem.content (HighScores.view state.highScoreState (HighScoreMsg >> dispatch)) ] ] ] ] ]


    /// Create a timer that emits a Timer.Tick message every 100 milliseconds.
    /// The timer uses this dispatcher to know when to update the display.
    /// The interval doesn't matter, as long as it occurs more than once per second
    /// to prevent strange pause/start behavior. The Timer needs to handle its own
    /// timekeeping separate of this ticker.
    let ticker dispatch =
        new DispatcherTimer(
            TimeSpan.FromMilliseconds 100,
            DispatcherPriority.Normal,
            fun _ _ -> dispatch (GameMsg(TimerMsg Tick))
        )
        |> fun timer -> timer.Start()

    /// This is the main window of your application
    /// you can do all sort of useful things here like setting heights and widths
    /// as well as attaching your dev tools that can be super useful when developing with
    /// Avalonia
    type MainWindow() as this =
        inherit HostWindow()

        do
            base.Title <- "F# Sudoku"
            base.Width <- 1100.0
            base.Height <- 700.0
            base.MinWidth <- 1100.0
            base.MinHeight <- 700.0

            //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
            //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

            Elmish.Program.mkProgram (fun () -> init) update view
            |> Program.withHost this
            |> Program.withSubscription (fun _ -> Cmd.ofSub ticker)
            |> Program.run
