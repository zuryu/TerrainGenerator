module SimplexNoise

open System.Diagnostics
open System
open Helper
//open ImageMaker
open Gradient

type SimplexNoise() =
    let mutable persistence = 0.4
    let mutable frequency = 5.0
    let mutable amplitude = 1.0
    let mutable lacunarity = 2.0
    let perm = Array.zeroCreate<byte> 512
    let permMod4 = Array.zeroCreate<byte> 512

    member this.F2 = 0.366025403
    member this.G2 = 0.211324865

    member this.gradients = [|new Gradient(1.0, 1.0); new Gradient(-1.0, 1.0);
                              new Gradient(1.0, -1.0); new Gradient(-1.0, -1.0); 
                              new Gradient(1.0, 0.0); new Gradient(-1.0, 0.0); 
                              new Gradient(0.0, 1.0); new Gradient(0.0, -1.0);                       
                              |]

    member this.p = [|(byte)118;(byte)176;(byte)44;(byte)64;(byte)106;(byte)19;(byte)173;
                        (byte)162;(byte)91;(byte)117;(byte)23;(byte)159;(byte)154;(byte)134;
                        (byte)165;(byte)211;(byte)194;(byte)148;(byte)208;(byte)30;(byte)141;
                        (byte)179;(byte)38;(byte)232;(byte)147;(byte)213;(byte)72;(byte)170;
                        (byte)87;(byte)205;(byte)55;(byte)243;(byte)108;(byte)217;(byte)245;
                        (byte)48;(byte)204;(byte)233;(byte)110;(byte)111;(byte)252;(byte)164;
                        (byte)34;(byte)124;(byte)125;(byte)228;(byte)240;(byte)81;(byte)175;
                        (byte)129;(byte)113;(byte)157;(byte)58;(byte)98;(byte)198;(byte)22;
                        (byte)50;(byte)253;(byte)127;(byte)144;(byte)103;(byte)31;(byte)199;
                        (byte)77;(byte)160;(byte)66;(byte)51;(byte)15;(byte)20;(byte)35;
                        (byte)242;(byte)61;(byte)40;(byte)84;(byte)93;(byte)150;(byte)221;
                        (byte)143;(byte)193;(byte)80;(byte)69;(byte)152;(byte)68;(byte)59;
                        (byte)133;(byte)215;(byte)74;(byte)209;(byte)73;(byte)249;(byte)33;
                        (byte)82;(byte)99;(byte)132;(byte)94;(byte)126;(byte)47;(byte)18;
                        (byte)241;(byte)27;(byte)14;(byte)101;(byte)17;(byte)76;(byte)4;
                        (byte)200;(byte)185;(byte)169;(byte)54;(byte)109;(byte)166;(byte)96;
                        (byte)191;(byte)114;(byte)153;(byte)192;(byte)151;(byte)65;(byte)0;
                        (byte)239;(byte)42;(byte)88;(byte)136;(byte)180;(byte)92;(byte)26;
                        (byte)187;(byte)186;(byte)155;(byte)97;(byte)234;(byte)52;(byte)85;
                        (byte)237;(byte)227;(byte)158;(byte)244;(byte)37;(byte)102;(byte)46;
                        (byte)184;(byte)210;(byte)9;(byte)138;(byte)202;(byte)95;(byte)1;
                        (byte)161;(byte)130;(byte)207;(byte)247;(byte)197;(byte)120;(byte)226;
                        (byte)78;(byte)16;(byte)146;(byte)236;(byte)70;(byte)11;(byte)139;
                        (byte)75;(byte)190;(byte)63;(byte)196;(byte)10;(byte)168;(byte)112;
                        (byte)89;(byte)123;(byte)60;(byte)214;(byte)156;(byte)6;(byte)119;
                        (byte)163;(byte)248;(byte)105;(byte)250;(byte)178;(byte)195;(byte)229;
                        (byte)137;(byte)203;(byte)67;(byte)25;(byte)225;(byte)71;(byte)135;
                        (byte)216;(byte)43;(byte)57;(byte)62;(byte)115;(byte)223;(byte)230;
                        (byte)32;(byte)231;(byte)56;(byte)5;(byte)189;(byte)39;(byte)41;
                        (byte)86;(byte)107;(byte)45;(byte)251;(byte)3;(byte)100;(byte)218;
                        (byte)24;(byte)13;(byte)122;(byte)7;(byte)220;(byte)183;(byte)238;
                        (byte)222;(byte)116;(byte)140;(byte)224;(byte)219;(byte)182;(byte)79;
                        (byte)206;(byte)28;(byte)171;(byte)188;(byte)21;(byte)181;(byte)36;
                        (byte)145;(byte)83;(byte)90;(byte)149;(byte)212;(byte)167;(byte)2;
                        (byte)246;(byte)235;(byte)12;(byte)29;(byte)121;(byte)142;(byte)174;
                        (byte)201;(byte)172;(byte)49;(byte)255;(byte)8;(byte)131;(byte)53;
                        (byte)177;(byte)128;(byte)254;(byte)104|]


    member this.setPerm() =
        for i = 0 to 511 do
            perm.[i] <- this.p.[i &&& 255]
            permMod4.[i] <- (perm.[i] % (byte)8)

    static member gradientDot(grad:Gradient, x, y) =
        ((float)(grad.getX()) * x) + ((float)(grad.getY()) * y)

    member this.getNoise(x:float, y:float) =
        // Corners of the simplex triangle
        let mutable n0 = 0.0
        let mutable n1 = 0.0
        let mutable n2 = 0.0
        // Skew
        let s = (x + y) * this.F2   //Skew factor 
        let xs = x + s
        let ys = y + s
        let i = Helper.fastFloor xs
        let j = Helper.fastFloor ys
        // Unskew
        let t = (float)(i + j) * this.G2    //Unskew factor
        let X0 = (float)i - t
        let Y0 = (float)j - t
        let x0 = x - X0
        let y0 = y - Y0
        // Work out the simplex
        let mutable i1 = 0
        let mutable j1 = 0
        if x0 > y0 then
            i1 <- 1
            j1 <- 0
        else
            i1 <- 0
            j1 <- 1
        let x1 = x0 - (float)i1 + this.G2
        let y1 = y0 - (float)j1 + this.G2
        let x2 = x0 - 1.0 + 2.0 * this.G2
        let y2 = y0 - 1.0 + 2.0 * this.G2
        // Work out the gradients
        let ii = i &&& 255
        let jj = j &&& 255
        let gi0 = permMod4.[ii + (int)perm.[jj]]
        let gi1 = permMod4.[ii + i1 + (int)perm.[jj + j1]]
        let gi2 = permMod4.[ii + 1 + (int)perm.[jj + 1]]
        // Calculate the first corner
        let mutable t0 = 0.5 - x0 * x0 - y0 * y0
        if t0 < 0.0 then n0 <- 0.0
        else
            t0 <- t0 * t0
            n0 <- t0 * t0 * SimplexNoise.gradientDot(this.gradients.[(int)gi0], x0, y0)
        // Calculate the second corner
        let mutable t1 = 0.5 - x1 * x1 - y1 * y1
        if t1 < 0.0 then n1 <- 0.0
        else
            t1 <- t1 * t1
            n1 <- t1 * t1 * SimplexNoise.gradientDot(this.gradients.[(int)gi1], x1, y1)
        // Calculate the third corner
        let mutable t2 = 0.5 - x2 * x2 - y2 * y2
        if t2 < 0.0 then n2 <- 0.0
        else
            t2 <- t2 * t2
            n2 <- t2 * t2 * SimplexNoise.gradientDot(this.gradients.[(int)gi2], x2, y2)

        (n0+n1+n2) * 70.0

//    member this.fractal(numberOfOctaves, x, y) = 
//        let mutable output = 0.0
//        for i = 0 to numberOfOctaves - 1 do
//            output <- output + (amplitude * this.getNoise(x * frequency, y * frequency))
//            frequency <- frequency * lacunarity
//            amplitude <- amplitude * persistence
//        output

//    member this.fractal(numberOfOctaves, x, y) = 
//        let mutable output = 0.0
//        let mutable octaveFrequency = frequency
//        let mutable octaveAmplitude = amplitude
//        let mutable totalAmplitude = 0.0
//        for i = 0 to numberOfOctaves - 1 do
//            output <- output + (octaveAmplitude * this.getNoise(x * octaveFrequency, y * octaveFrequency))
//            octaveFrequency <- octaveFrequency * lacunarity
//            octaveAmplitude <- octaveAmplitude * persistence
//            totalAmplitude <- totalAmplitude + octaveAmplitude
//        output / totalAmplitude

    member this.fractal(octaves, amplitude, frequency, x, y) =
        let mutable noiseSum = 0.0
        let mutable octaveFrequency = frequency
        let mutable octaveAmplitude = 1.0
        let mutable totalAmplitude = 0.0
        for octave = 0 to octaves - 1 do
            noiseSum <- noiseSum + this.getNoise(x * octaveFrequency, y * octaveFrequency) * octaveAmplitude
            octaveFrequency <- octaveFrequency * lacunarity
            totalAmplitude <- totalAmplitude + octaveAmplitude
            octaveAmplitude <- octaveAmplitude * persistence
        noiseSum / totalAmplitude

//    member this.getNoiseMap(width, height) = 
//        this.setPerm()
//        let freq = frequency / (float)width
//        let noise = Array2D.zeroCreate<float> width height
//        for x = 0 to width - 1 do
//            for y = 0 to height - 1 do
//                noise.[x, y] <- this.getNoise((float)x * freq, (float)y * freq)
//                Debug.Write((string)noise.[x, y] + "\n")
//        noise

    member this.getNoiseMap(width, height) = 
        this.setPerm()
        let freq = frequency / (float)width
        let noise = Array2D.zeroCreate<float> width height
        for x = 0 to width - 1 do
            for y = 0 to height - 1 do
                noise.[x, y] <- this.fractal(6, 0.5, 0.05, (float)x + 0.5, (float)y + 0.5)
                //Debug.Write((string)noise.[x, y] + "\n")
        noise
            


        //(((h &&& 1) ? -u : u) + ((h &&& 2) ? -2.0f*v : 2.0f*v))

//    static member getPerm(permTable: int array) = 
//        let random = System.Random()
//        for i = 0 to 600 do
//            let from = random.Next(permTable.Length)
//            let toNumber = random.Next(permTable.Length)
//
//            let temp = permTable.[from]
//            permTable.SetValue(permTable.[toNumber], from)
//            permTable.SetValue(temp, toNumber)
//
//        for number in permTable do
//            Debug.Write("(byte)" + (string)number + ";")


//    static member getNoise x y =
//        //Skew the input space 
//        let s = (x + y) * F2
//    member this.noise = Array2D.zeroCreate<float>

//    member this.createNoise() = 
//        0
//
//    member this.interpolateNoise() = 
//        0
//
//    member this.smoothNoise() =
//        0




//    let (octaves : Octave array) = Array.zeroCreate numberOfOctaves
//    let (frequencies : double array) = Array.zeroCreate numberOfOctaves
//    let (amplitudes : double array) = Array.zeroCreate numberOfOctaves
//
//    member this.noiseSetup(numberOfOctaves:int, persistence:double) =
//
//    static member getNoise(x:int, y:int) = 
////        0
//[<EntryPoint>]
//let main args =
//    let im = new ImageMaker()
//    let sn = new SimplexNoise()
//    let b = (byte)257
//    //let map = sn.getNoiseMap(10, 10)
//    im.createImage(100, 100, "Images\\withoutFractals.png", sn.getNoiseMap(100, 100))
//    0