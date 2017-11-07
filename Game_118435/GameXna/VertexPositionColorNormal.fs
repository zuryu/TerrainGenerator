module VertexPositionColorNormal

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

type VertexPositionColorNormal =
    struct
        val mutable Position : Vector3
        val mutable Color : Color
        val mutable Normal : Vector3

        static member vertexElements : VertexElement[] = 
            [|
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0)
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0)
                new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
            |] 

        static member VertexDeclaration = new VertexDeclaration(VertexPositionColorNormal.vertexElements)

    end