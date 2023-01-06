using Cavern.Format.Decoders;

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
        RIFFWaveDecoder decoder;
        try {
            decoder = new RIFFWaveDecoder(new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read,
                10 * 1024 * 1024, FileOptions.SequentialScan));
        } catch (Exception e) {
            Console.WriteLine("This file is either corrupt or not an ADM BWF:");
            Console.WriteLine(e.Message);
            BeforeExit();
            return;
        }

        if (decoder.ADM == null) {
            Console.WriteLine("This file does not contain an AXML block, thus it's not a valid ADM BWF file.");
            BeforeExit();
            return;
        }

        Console.WriteLine("Validating...");
        List<string> errors = decoder.ADM.Validate();
        long admLength = (long)(decoder.ADM.GetLength().TotalSeconds * decoder.SampleRate +.5);
        if (decoder.Length != admLength) {
            errors.Add("The PCM length in the RIFF header does not match with the ADM program length.");
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
}