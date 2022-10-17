namespace Cavern.Format.Decoders.EnhancedAC3 {
    enum NonStandardBedChannel {
        FrontLeft = 0,
        FrontRight = 1,
        Center = 2,
        LowFrequencyEffects = 3,
        SurroundLeft = 4,
        SurroundRight = 5,
        RearLeft = 6,
        RearRight = 7,
        TopFrontLeft = 8,
        TopFrontRight = 9,
        TopSurroundLeft = 10,
        TopSurroundRight = 11,
        TopRearLeft = 12,
        TopRearRight = 13,
        WideLeft = 14,
        WideRight = 15,
        LowFrequencyEffects2 = 16,
        Max = 17
    }

    enum ObjectAnchor {
        Room,
        Screen,
        Speaker
    }
}