using Cavern.QuickEQ.Crossover;
using Cavern.WPF.Consts;

namespace Cavern.WPF.Utils {
    /// <summary>
    /// Used to display a crossover type's name on a UI in the user's language and contain which <see cref="CrossoverType"/> it is.
    /// </summary>
    public class CrossoverTypeOnUI(CrossoverType type) {
        /// <summary>
        /// The displayed channel.
        /// </summary>
        public CrossoverType Type => type;

        /// <inheritdoc/>
        public override string ToString() => Type.Translate();
    }
}