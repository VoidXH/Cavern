namespace Test.Cavernize.Logic.Utilities;

/// <summary>
/// Frequently reused console handling.
/// </summary>
public static class ConsoleUtils {
    /// <summary>
    /// Perform an <paramref name="action"/> and return its standard output and error.
    /// </summary>
    public static (string output, string error) Redirect(Action action) {
        TextWriter originalOutput = Console.Out;
        TextWriter originalError = Console.Error;
        using StringWriter outputWriter = new();
        using StringWriter errorWriter = new();
        Console.SetOut(outputWriter);
        Console.SetError(errorWriter);
        try {
            action();
        } finally {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
        return (outputWriter.ToString(), errorWriter.ToString());
    }
}
