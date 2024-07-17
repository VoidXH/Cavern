using System.Runtime.Serialization;

namespace Cavern.Filters.Interfaces {
    /// <summary>
    /// A filter that requires its sample rate to match with the output sample rate.
    /// </summary>
    public interface ISampleRateDependentFilter {
        /// <summary>
        /// Sample rate of the filter.
        /// </summary>
        /// <remarks>All implementations shall be attributed with <see cref="IgnoreDataMemberAttribute"/> and per-filter changes should
        /// not be allowed for the user. This is the job of the filter handling subsystems.</remarks>
        [IgnoreDataMember]
        int SampleRate { get; set; }
    }
}