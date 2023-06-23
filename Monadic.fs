namespace Sudoku

module Monadic =

    type Action<'a> = 'a -> unit

    let tee (action: Action<'a>) (input: 'a) : 'a =
        action input
        input
