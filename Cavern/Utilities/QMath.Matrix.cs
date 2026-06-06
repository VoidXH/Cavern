using System;

namespace Cavern.Utilities {
    // Operations on matrices
    partial class QMath {
        /// <summary>
        /// Get the eigenvalues and eigenvectors of a symmetric matrix using the Jacobi algorithm.
        /// </summary>
        public static (double[] eigenvalues, double[][] eigenvectors) SymmetricEigenvalueDecomposition(double[][] matrix, int iterations = 1000) {
            double[][] workingMatrix = matrix.DeepCopy1D();
            double[] eigenvalues = new double[matrix.Length];
            double[][] eigenvectors = new double[matrix.Length][];
            for (int i = 0; i < matrix.Length; i++) {
                eigenvectors[i] = new double[matrix.Length];
                eigenvectors[i][i] = 1;
            }

            const double eps = 1e-12;
            for (int iter = 0; iter < iterations; iter++) {
                double maxVal = 0;
                int p = 0;
                int q = 1;

                // Find the largest off-diagonal element
                for (int y = 0; y < matrix.Length - 1; y++) {
                    for (int x = y + 1; x < matrix.Length; x++) {
                        double val = Math.Abs(workingMatrix[y][x]);
                        if (val > maxVal) {
                            maxVal = val;
                            p = y;
                            q = x;
                        }
                    }
                }

                if (maxVal < eps) {
                    break; // Converged
                }

                // Calculate rotation angle
                double theta;
                if (Math.Abs(workingMatrix[p][p] - workingMatrix[q][q]) < eps) {
                    theta = Math.PI / 4.0 * Math.Sign(workingMatrix[p][q]);
                } else {
                    theta = 0.5 * Math.Atan2(2.0 * workingMatrix[p][q], workingMatrix[p][p] - workingMatrix[q][q]);
                }

                double cos = Math.Cos(theta);
                double sin = Math.Sin(theta);

                // Rotate matrix
                for (int x = 0; x < matrix.Length; x++) {
                    if (x != p && x != q) {
                        double pxOld = workingMatrix[p][x];
                        double qxOld = workingMatrix[q][x];
                        workingMatrix[p][x] = workingMatrix[x][p] = cos * pxOld - sin * qxOld;
                        workingMatrix[q][x] = workingMatrix[x][q] = sin * pxOld + cos * qxOld;
                    }
                }

                double app = workingMatrix[p][p];
                double aqq = workingMatrix[q][q];
                double apq = workingMatrix[p][q];
                workingMatrix[p][p] = cos * cos * app - 2 * sin * cos * apq + sin * sin * aqq;
                workingMatrix[q][q] = sin * sin * app + 2 * sin * cos * apq + cos * cos * aqq;
                workingMatrix[p][q] = workingMatrix[q][p] = 0.0;

                // Rotate eigenvectors
                for (int y = 0; y < matrix.Length; y++) {
                    double ypOld = eigenvectors[y][p];
                    double yqOld = eigenvectors[y][q];
                    eigenvectors[y][p] = cos * ypOld - sin * yqOld;
                    eigenvectors[y][q] = sin * ypOld + cos * yqOld;
                }
            }

            // Extract diagonals as eigenvalues
            for (int y = 0; y < matrix.Length; y++) {
                eigenvalues[y] = workingMatrix[y][y];
            }

            return (eigenvalues, eigenvectors);
        }
    }
}
