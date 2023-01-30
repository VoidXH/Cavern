using Cavern.Channels;
using Cavern.Format.Decoders;
using Cavern.Format.Transcoders;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;

static class Program {
    /// <summary>
    /// Entry point to the program.
    /// </summary>
    static void Main(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("Please drag and drop a file or add it as an argument.");
            BeforeExit();
            return;
        }

        if (!File.Exists(args[0])) {
            Console.WriteLine("The file in the argument does not exist.");
            BeforeExit();
            return;
        }

        Console.WriteLine("Loading the file, this might take a while...");
        RIFFWaveTester decoder;
        try {
            decoder = new RIFFWaveTester(new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read,
                10 * 1024 * 1024, FileOptions.SequentialScan));
        } catch (Exception e) {
            Console.WriteLine("This file is either corrupt or not an ADM BWF:");
            Console.WriteLine(e.Message);
            BeforeExit();
            return;
        }

        AudioDefinitionModel adm = decoder.ADM;
        if (adm == null) {
            Console.WriteLine("This file does not contain an AXML block, thus it's not a valid ADM BWF file.");
            BeforeExit();
            return;
        }

        Console.WriteLine("Validating...");
        List<string> errors = adm.Validate();
        long admLength = (long)(adm.GetLength().TotalSeconds * decoder.SampleRate + .5);
        if (decoder.Length != admLength) {
            errors.Add($"The PCM length in the RIFF header ({decoder.Length}) does not match with the ADM program length ({admLength}).");
        }

        if (decoder.DBMD != null) {
            Console.WriteLine("Validating Dolby Atmos conformity...");
            List<string> tracks = adm.Objects[0].Tracks;
            if (tracks.Count != 10) {
                errors.Add("An invalid number of bed channels are present.");
            }
            ADMPackFormat pack = adm.PackFormats.FirstOrDefault(x => x.ID == adm.Objects[0].PackFormat);
            if (pack != null) {
                tracks = pack.ChannelFormats;
                for (int i = 0, c = tracks.Count; i < c; i++) {
                    ADMChannelFormat map = adm.ChannelFormats.FirstOrDefault(x => x.ID == tracks[i]);
                    if (map == null || map.Blocks.Count != 1 ||
                        map.Blocks[0].Position != ChannelPrototype.AlternativePositions[bedChannels[i]]) {
                        errors.Add($"Bed channel {tracks[i]} is invalid.");
                    }
                }
            }
        }

        if (errors.Count == 0) {
            Console.WriteLine("This is a valid ADM BWF file.");
        } else {
            Console.WriteLine("The following errors invalidate this ADM BWF file:");
            for (int i = 0, c = errors.Count; i < c; i++) {
                Console.WriteLine(errors[i]);
            }
        }

        BeforeExit();
    }

    /// <summary>
    /// Displays the press any key to exit message and waits for a key press.
    /// </summary>
    static void BeforeExit() {
        Console.WriteLine("Press any key to exit the program...");
        Console.ReadKey();
    }

    /// <summary>
    /// Indexes of Dolby Atmos beds (7.1.2) in the <see cref="ReferenceChannel"/> enum.
    /// </summary>
    static readonly byte[] bedChannels = { 0, 1, 2, 3, 6, 7, 4, 5, 17, 18 };
}