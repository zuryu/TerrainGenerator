module Player

open Microsoft.Xna.Framework

type Player() = 
    let mutable x = 50
    let mutable y = 50

    let offset = Vector3(0.0f, 10.0f, 0.0f)

    let mutable angle = 0.0

    member player.GetX() =
        x

    member player.GetY() =
        y

    member player.SetX value =
        x <- value

    member player.SetY value =
        y <- value