module MultiTexture

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

type MultiTexture =
    struct
        val mutable Position : Vector3
        val mutable Normal : Vector3
        val mutable TextureCoordinate : Vector4
        val mutable TextureWeight : Vector4
    
        static member vertexElements : VertexElement[] = 
            [|
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0)
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
                new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0)
                new VertexElement(40, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1)
            |] 

        static member VertexDeclaration = new VertexDeclaration(MultiTexture.vertexElements)
    end