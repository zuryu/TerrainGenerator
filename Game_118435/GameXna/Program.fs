open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Content
open System.IO
open System.Diagnostics
open Microsoft.Kinect
open Land
open MultiTexture

type XnaGame() as this =
    inherit Game()

// Variables //////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    do this.Content.RootDirectory <- "XnaGameContent"
    let graphics = new GraphicsDeviceManager(this)

    let mutable spriteBatch : SpriteBatch = null
    let mutable device : GraphicsDevice = null
    let mutable effect : Effect = null

    let mutable viewMatrix : Matrix = new Matrix()
    let mutable projectionMatrix : Matrix = new Matrix()

    let mutable angle = 0.0
    let width = 128
    let height = 128

    let mutable vertexBuffer : VertexBuffer = null
    let mutable indexBuffer : IndexBuffer = null

    let map = new Land(width, height)

    let mutable mKinect : KinectSensor = null 
    let mutable colorData : Color[] = null
    let mutable mColorImage : Texture2D = null
    let mutable skeletonData : Skeleton[] = null
    let mutable skeleton : Skeleton = null
    let resolution = new Vector2(640.0f, 480.0f)
    let mutable sprite : Texture2D = null
    
    let playerPos = new Vector2(50.0f, 50.0f)

    let mutable cameraPosition = new Vector3(130.0f, 40.0f, -40.0f)
    let mutable leftRightRot = MathHelper.PiOver2
    let mutable upDownRot = -MathHelper.Pi / 10.0f
    let rotationSpeed = 0.3f
    let moveSpeed = 30.0f

    let mutable originalMouseState : MouseState = Mouse.GetState() 

    let mutable grassTexture : Texture2D = null 
    let mutable sandTexture : Texture2D = null 
    let mutable rockTexture : Texture2D = null 
    let mutable snowTexture : Texture2D = null 
    
    let rec KinectSearch (potentialSensor : KinectSensor, sensorNumber, maxSensors) = 
        if not (potentialSensor.Status = KinectStatus.Connected) then
            if sensorNumber + 1 < maxSensors then
                KinectSearch (KinectSensor.KinectSensors.[sensorNumber + 1], (sensorNumber + 1), maxSensors)
            else 
                Debug.Write("No Kinect sensor found.")
        mKinect <- potentialSensor

// XNA Methods /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    override game.Initialize() =
        graphics.GraphicsProfile <- GraphicsProfile.HiDef
        graphics.PreferredBackBufferWidth <- 1280
        graphics.PreferredBackBufferHeight <- 480
        graphics.IsFullScreen <- false
        graphics.ApplyChanges() 
        this.Window.Title <- "Game Project - 118435"

        this.InitializeKinect()

        base.Initialize()

    override game.LoadContent() =
        device <- graphics.GraphicsDevice
        spriteBatch <- new SpriteBatch(device)
        effect <- this.Content.Load<Effect>("effect3")
        map.LoadHeightData()
        map.SetUpVertices()
        map.SetUpIndices()
        map.CalculateNormals()
        game.SetUpCamera()
        game.CopyToBuffer()
        Mouse.SetPosition(device.Viewport.Width / 2, device.Viewport.Height / 2)
        originalMouseState <- Mouse.GetState()
        sprite <- game.Content.Load<Texture2D>("Sprite")
        game.LoadTextures()
        
        
    override game.Update gameTime = 
        let keyState = Keyboard.GetState()
        if keyState.IsKeyDown(Keys.Delete) then
            angle <- angle + 0.05
        if keyState.IsKeyDown(Keys.PageDown) then
            angle <- angle - 0.05       
        if not(skeleton = null) then     
            game.CheckForGestures skeleton.Joints.[JointType.Head] skeleton.Joints.[JointType.HandRight] skeleton.Joints.[JointType.HandLeft] skeleton.Joints.[JointType.KneeLeft]
        let timePassed = (float32)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f
        game.ProcessInput timePassed   
        if GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed then
            game.Exit() 
        base.Update gameTime

    override game.Draw gameTime = 
        let rs = new RasterizerState()
        //rs.FillMode <- FillMode.WireFrame
        device.RasterizerState <- rs
        device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.White, 1.0f, 0)

        effect.CurrentTechnique <- effect.Techniques.["MultiTextured"]
        effect.Parameters.["xTexture0"].SetValue(sandTexture)
        effect.Parameters.["xTexture1"].SetValue(grassTexture)
        effect.Parameters.["xTexture2"].SetValue(rockTexture)
        effect.Parameters.["xTexture3"].SetValue(snowTexture)
        let lightDirection = new Vector3(1.0f, -1.0f, -1.0f)
        lightDirection.Normalize()
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)
        effect.Parameters.["xAmbient"].SetValue(0.4f)
        effect.Parameters.["xEnableLighting"].SetValue(true)
        effect.Parameters.["xView"].SetValue(viewMatrix)
        effect.Parameters.["xProjection"].SetValue(projectionMatrix)
        let worldMatrix = Matrix.CreateTranslation((float32)(-(float)width/2.0), 0.0f, (float32)((float)height /2.0)) * Matrix.CreateRotationY((float32)angle)
        effect.Parameters.["xWorld"].SetValue(Matrix.Identity)
        for pass in effect.CurrentTechnique.Passes do
            pass.Apply()

        device.Indices <- indexBuffer
        device.SetVertexBuffer(vertexBuffer)
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, map.GetVertices().Length, 0, map.GetIndices().Length / 3)

        // Uncomment the following block to see the Kinect camera and skeleton tracking working.
//        spriteBatch.Begin()
//
//        game.DrawColorImage()
//        game.DrawSkeleton resolution sprite
//
//        spriteBatch.End()
//
//        base.Draw gameTime
    
// Setup Methods /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    member game.SetUpCamera() = 
        game.UpdateViewMatrix()
        projectionMatrix <- Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 300.0f)

    member game.CopyToBuffer() = 
        vertexBuffer <- new VertexBuffer(device, MultiTexture.VertexDeclaration, map.GetVertices().Length, BufferUsage.WriteOnly)
        vertexBuffer.SetData(map.GetVertices())
        indexBuffer <- new IndexBuffer(device, (typeof<int> : Type), map.GetIndices().Length, BufferUsage.WriteOnly)
        indexBuffer.SetData(map.GetIndices())   
        
    member game.UpdateViewMatrix() =
        let cameraRotation = Matrix.CreateRotationX(upDownRot) * Matrix.CreateRotationY(leftRightRot)
        let cameraOriginalTarget = new Vector3(0.0f, 0.0f, -1.0f)
        let cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation)
        let cameraFinalTarget = cameraPosition + cameraRotatedTarget
        let cameraOriginalUp = new Vector3(0.0f, 1.0f, 0.0f)
        let cameraRotatedUp = Vector3.Transform(cameraOriginalUp, cameraRotation)
        viewMatrix <- Matrix.CreateLookAt(cameraPosition, cameraFinalTarget, cameraRotatedUp)

    member game.ProcessInput (timePassed : float32) = 
        if not(GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X = 0.0f) then
            leftRightRot <- leftRightRot - rotationSpeed * GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X * timePassed * 8.0f
            game.UpdateViewMatrix()
        if not(GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.Y = 0.0f) then
            upDownRot <- upDownRot - rotationSpeed * GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.Y * timePassed * 8.0f
            game.UpdateViewMatrix()
        let currentMouseState = Mouse.GetState()
        if not(currentMouseState = originalMouseState) then
            let xDifference = (float32)currentMouseState.X - (float32)originalMouseState.X
            let yDifference = (float32)currentMouseState.Y - (float32)originalMouseState.Y
            leftRightRot <- leftRightRot - rotationSpeed * xDifference * timePassed
            upDownRot <- upDownRot - rotationSpeed * yDifference * timePassed
            Mouse.SetPosition(device.Viewport.Width / 2, device.Viewport.Height / 2)
            game.UpdateViewMatrix()

        let mutable move = new Vector3(0.0f, 0.0f, 0.0f)
        if not(GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.X = 0.0f) then
            move <- move + new Vector3(GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.X, 0.0f, 0.0f)
        if not(GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y = 0.0f) then
            move <- move + new Vector3(0.0f, 0.0f, -GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y)
        let keyState = Keyboard.GetState()
        if keyState.IsKeyDown Keys.Up || keyState.IsKeyDown Keys.W then
            move <- move + new Vector3(0.0f, 0.0f, -1.0f)
        if keyState.IsKeyDown Keys.Down || keyState.IsKeyDown Keys.S then
            move <- move + new Vector3(0.0f, 0.0f, 1.0f)
        if keyState.IsKeyDown Keys.Right || keyState.IsKeyDown Keys.D then
            move <- move + new Vector3(1.0f, 0.0f, 0.0f)
        if keyState.IsKeyDown Keys.Left || keyState.IsKeyDown Keys.A then
            move <- move + new Vector3(-1.0f, 0.0f, 0.0f)
        if keyState.IsKeyDown Keys.Q then
            move <- move + new Vector3(0.0f, 1.0f, 0.0f)
        if keyState.IsKeyDown Keys.Z then
            move <- move + new Vector3(0.0f, -1.0f, 0.0f)
        game.AddToCameraPosition (move * timePassed)

    member game.AddToCameraPosition valueToAdd = 
        let cameraRotation = Matrix.CreateRotationX(upDownRot) * Matrix.CreateRotationY(leftRightRot)
        let rotatedVector = Vector3.Transform(valueToAdd, cameraRotation)
        cameraPosition <- cameraPosition + moveSpeed * rotatedVector
        game.UpdateViewMatrix()

    member game.LoadTextures() =
        grassTexture <- game.Content.Load<Texture2D>("grass1")
        sandTexture <- game.Content.Load<Texture2D>("sand1")
        rockTexture <- game.Content.Load<Texture2D>("rock1")
        snowTexture <- game.Content.Load<Texture2D>("snow1")

// Kinect Methods /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    member game.InitializeKinect() =
        if KinectSensor.KinectSensors.Count > 0 then
            KinectSearch(KinectSensor.KinectSensors.[0], 0, KinectSensor.KinectSensors.Count)
        if not(mKinect = null) then
            mKinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30)
            mKinect.ColorFrameReady.AddHandler(new EventHandler<ColorImageFrameReadyEventArgs>(game.mKinect_ColorFrameReady))
            mKinect.SkeletonStream.Enable()
            mKinect.SkeletonFrameReady.AddHandler(new EventHandler<SkeletonFrameReadyEventArgs>(game.mKinect_SkeletonFrameReady))
            mKinect.Start()

    member game.mKinect_ColorFrameReady (sender : obj) (e : ColorImageFrameReadyEventArgs) = 
        let frame = e.OpenColorImageFrame()
        if not(frame = null) then
            let pixels = Array.zeroCreate<byte> frame.PixelDataLength            
            frame.CopyPixelDataTo(pixels)
            colorData <- Array.zeroCreate<Color> (pixels.Length / 4)
            let mutable offset = 0
            for i = 0 to colorData.Length - 1 do
                colorData.[i] <- new Color((int)pixels.[offset + 2], (int)pixels.[offset + 1], (int)pixels.[offset])
                offset <- offset + 4
            frame.Dispose()

    member game.DrawColorImage() =
        if not(colorData = null) then
            mColorImage <- new Texture2D(device, 640, 480)
            mColorImage.SetData<Color>(colorData)
            spriteBatch.Draw(mColorImage, new Rectangle(0, 0, 640, 480), Color.White)           

    member game.mKinect_SkeletonFrameReady (sender : obj) (e : SkeletonFrameReadyEventArgs) =
        let skeletonFrame = e.OpenSkeletonFrame()
        if not(skeletonFrame = null) then
            if skeletonData = null || not(skeletonData.Length = skeletonFrame.SkeletonArrayLength) then
                skeletonData <- Array.zeroCreate<Skeleton> skeletonFrame.SkeletonArrayLength
            skeletonFrame.CopySkeletonDataTo(skeletonData)
            skeletonFrame.Dispose()
        if not(skeletonData = null) then
            for skel in skeletonData do
                if skel.TrackingState = SkeletonTrackingState.Tracked then
                    skeleton <- skel
        
    member game.DrawSkeleton (resolution : Vector2) img =
        if not(skeleton = null) then
            for joint in (Seq.cast<Joint> skeleton.Joints) do
                let position = new Vector2(((((float32)0.5 * joint.Position.X) + (float32)0.5) * resolution.X), (((-(float32)0.5 * joint.Position.Y) + (float32)0.5) * resolution.Y))
                spriteBatch.Draw(img, new Rectangle((int)position.X, (int)position.Y, 10, 10), Color.Red)

    member game.CheckForGestures (head : Joint) (rightHand : Joint) (leftHand : Joint) (knee : Joint)=
        if rightHand.Position.Y > head.Position.Y && leftHand.Position.Y > head.Position.Y then
            if cameraPosition.X > 0.0f && cameraPosition.X < (float32)width && cameraPosition.Y > 0.0f && cameraPosition.Y < (float32)height then
               map.RaiseLandAt ((int)cameraPosition.X) ((int)cameraPosition.Y)
               game.CopyToBuffer()

        if rightHand.Position.Y > knee.Position.Y && leftHand.Position.Y > knee.Position.Y then
            if cameraPosition.X > 0.0f && cameraPosition.X < (float32)width && cameraPosition.Y > 0.0f && cameraPosition.Y < (float32)height then
               map.LowerLandAt ((int)cameraPosition.X) ((int)cameraPosition.Y)
               game.CopyToBuffer()


// Start Game /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

let game = new XnaGame()
try game.Run()
finally game.Dispose()