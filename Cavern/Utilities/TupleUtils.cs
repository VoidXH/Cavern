namespace Cavern.Utilities {
    /// <summary>
    /// Advanced functions for handling tuples.
    /// </summary>
    public static class TupleUtils {
        /// <summary>
        /// From an array of tuples, get only the second &quot;column&quot;.
        /// </summary>
        public static T2[] GetItem2s<T1, T2>(this (T1, T2)[] items) {
            T2[] result = new T2[items.Length];
            for (int i = 0; i < items.Length; i++) {
                result[i] = items[i].Item2;
            }
            return result;
        }
    }
}