using System;

using Cavern.Utilities;

namespace Cavern.Filters.Interfaces {
    /// <summary>
    /// A filter implemented in both Cavern and <see cref="CavernAmp"/>.
    /// </summary>
    public interface IMultiplatformFilter : IFilter, IDisposable { }
}
