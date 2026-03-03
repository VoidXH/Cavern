namespace Cavern.Utilities {
    partial class QMath {
        /// <summary>
        /// Gets <paramref name="n"/> over <paramref name="k"/>.
        /// </summary>
        public static long Combination(long n, long k) {
            if (k < 0 || k > n) {
                return 0;
            }
            if (k == 0 || k == n) {
                return 1;
            }

            if (k > n / 2) {
                k = n - k; // Optimization: n over k is the same as n over (n - k)
            }

            long result = 1;
            for (int i = 1; i <= k; i++) {
                checked {
                    result = result * (n - i + 1) / i;
                }
            }
            return result;
        }

        /// <summary>
        /// Gets <paramref name="n"/>!.
        /// </summary>
        public static long Factorial(int n) {
            long result = 1;
            for (byte i = 2; i <= n; i++) {
                result *= i;
            }
            return result;
        }
    }
}
