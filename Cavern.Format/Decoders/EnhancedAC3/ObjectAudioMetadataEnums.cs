namespace Cavern.Format.Decoders.EnhancedAC3 {
    enum OAMDBedChannel {
        Max = 10,
        Fronts = 9,
        Center = 8,
        LowFrequencyEffects = 7,
        Surrounds = 6,
        Rears = 5,
        TopFronts = 4,
        TopSurrounds = 3,
        TopRears = 2,
        Wides = 1,
        LowFrequencyEffects2 = 0
    }

    enum NonStandardBedChannel {
        Max = 17,
        FrontLeft = 16,
        FrontRight = 15,
        Center = 14,
        LowFrequencyEffects = 13,
        SurroundLeft = 12,
        SurroundRight = 11,
        RearLeft = 10,
        RearRight = 9,
        TopFrontLeft = 8,
        TopFrontRight = 7,
        TopSurroundLeft = 6,
        TopSurroundRight = 5,
        TopRearLeft = 4,
        TopRearRight = 3,
        WideLeft = 2,
        WideRight = 1,
        LowFrequencyEffects2 = 0
    }

    enum ObjectAnchor {
        Room,
        Screen,
        Speaker
    }
}