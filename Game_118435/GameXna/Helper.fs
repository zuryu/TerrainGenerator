module Helper

open System.Diagnostics

type Helper() =
    
    static member floatInTo255(value:float) = 
        let valueNew = (value + 1.0) / 2.0
        if valueNew > 1.0 || valueNew < 0.0 then 
            Debug.Write("Value out of range in floatInTo255: " + (string)valueNew + "\n")
            0
        else (int)(255.0 * valueNew)

    static member fastFloor (numberD:double) = 
        let numberI = (int)numberD
        if numberD < (double)numberI then numberI - 1
        else numberI