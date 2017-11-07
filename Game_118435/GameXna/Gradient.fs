module Gradient

type Gradient(x:float, y:float) =
    member this.getX() =
        x

    member this.getY() =
        y