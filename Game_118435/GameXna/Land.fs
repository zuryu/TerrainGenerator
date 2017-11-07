module Land

open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework
open SimplexNoise
open Helper
open MultiTexture
open System

type Land(width, depth) =

    let vertices = Array.zeroCreate<MultiTexture> (width * depth)
    let indices = Array.zeroCreate<int> ((width - 1) * (depth - 1) * 6)
    let heightData = Array2D.zeroCreate<float> width depth
    let noiseGenerator = new SimplexNoise()

    member this.SetUpVertices() = 
        let mutable minHeight = 100000000.0
        let mutable maxHeight = -10000000.0
        for x = 0 to width - 1 do
            for y = 0 to depth - 1 do
                if heightData.[x, y] < minHeight then
                    minHeight <- heightData.[x, y]
                if heightData.[x, y] > maxHeight then
                    maxHeight <- heightData.[x, y]
        for x = 0 to width - 1 do
            for y = 0 to depth - 1 do
                heightData.[x, y] <- (heightData.[x, y] - minHeight) / (maxHeight - minHeight) * 30.0
        for x = 0 to width - 1 do
            for y = 0 to depth - 1 do
                vertices.[x + y * width].Position <- new Vector3((float32)x, (float32)heightData.[x, y], -(float32)y)

                vertices.[x + y * width].TextureCoordinate.X <- (float32)((float)x / 30.0)
                vertices.[x + y * width].TextureCoordinate.Y <- (float32)((float)y / 30.0)
                vertices.[x + y * width].TextureWeight.X <- MathHelper.Clamp(1.0f - (float32)(Math.Abs((float32)heightData.[x, y] - 0.0f)) / 8.0f, 0.0f, 1.0f)
                vertices.[x + y * width].TextureWeight.Y <- MathHelper.Clamp(1.0f - (float32)(Math.Abs((float32)heightData.[x, y] - 12.0f)) / 6.0f, 0.0f, 1.0f)
                vertices.[x + y * width].TextureWeight.Z <- MathHelper.Clamp(1.0f - (float32)(Math.Abs((float32)heightData.[x, y] - 20.0f)) / 6.0f, 0.0f, 1.0f)
                vertices.[x + y * width].TextureWeight.W <- MathHelper.Clamp(1.0f - (float32)(Math.Abs((float32)heightData.[x, y] - 30.0f)) / 6.0f, 0.0f, 1.0f)

                let mutable total = vertices.[x + y * width].TextureWeight.X
                total <- total + vertices.[x + y * width].TextureWeight.Y
                total <- total + vertices.[x + y * width].TextureWeight.Z
                total <- total + vertices.[x + y * width].TextureWeight.W

                vertices.[x + y * width].TextureWeight.X <- vertices.[x + y * width].TextureWeight.X / total
                vertices.[x + y * width].TextureWeight.Y <- vertices.[x + y * width].TextureWeight.Y / total
                vertices.[x + y * width].TextureWeight.Z <- vertices.[x + y * width].TextureWeight.Z / total
                vertices.[x + y * width].TextureWeight.W <- vertices.[x + y * width].TextureWeight.W / total

    member this.SetUpIndices() = 
        let mutable counter = 0
        for y = 0 to depth - 2 do
            for x = 0 to width - 2 do
                let lowerLeft = x + y * width
                let lowerRight = (x + 1) + y * width
                let topLeft = x + (y + 1) * width
                let topRight = (x + 1) + (y + 1) * width

                indices.[counter] <- topLeft
                counter <- counter + 1
                indices.[counter] <- lowerRight
                counter <- counter + 1
                indices.[counter] <- lowerLeft
                counter <- counter + 1

                indices.[counter] <- topLeft
                counter <- counter + 1
                indices.[counter] <- topRight
                counter <- counter + 1
                indices.[counter] <- lowerRight
                counter <- counter + 1

    member this.LoadHeightData() =
        let heightDataTemp = noiseGenerator.getNoiseMap(width, depth)
        for x = 0 to width - 1 do
            for y = 0 to depth - 1 do
                heightData.[x, y] <- (float)(Helper.floatInTo255 heightDataTemp.[x, y])
    
    member this.CalculateNormals() =
        for i = 0 to vertices.Length - 1 do
            vertices.[i].Normal <- new Vector3((float32)0, (float32)0, (float32)0)
        for i = 0 to (indices.Length - 1) / 3 do
            let index1 = indices.[i * 3]
            let index2 = indices.[i * 3 + 1]
            let index3 = indices.[i * 3 + 2]
            let side1 = vertices.[index1].Position - vertices.[index3].Position
            let side2 = vertices.[index1].Position - vertices.[index2].Position
            let normal = Vector3.Cross(side1, side2)

            vertices.[index1].Normal <- vertices.[index1].Normal + normal
            vertices.[index2].Normal <- vertices.[index2].Normal + normal
            vertices.[index3].Normal <- vertices.[index3].Normal + normal

        for i = 0 to vertices.Length - 1 do
            vertices.[i].Normal.Normalize()

    member this.GetVertices() =
        vertices

    member this.GetIndices() =
        indices

    member this.RaiseLandAt x y =
        vertices.[x + y * width].Position <- new Vector3((float32)x, (float32)heightData.[x, y] + 5.0f, -(float32)y)
        heightData.[x, y] <- heightData.[x, y] + 5.0

    member this.LowerLandAt x y =
        vertices.[x + y * width].Position <- new Vector3((float32)x, (float32)heightData.[x, y] - 5.0f, -(float32)y)
        