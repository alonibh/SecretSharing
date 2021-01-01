using System;
using System.Collections.Generic;

namespace SecretSharing
{
    public static class _2DArrayExtensions
    {
        public static double[,] CreateRandomUserItemMatrix(int N, int M, int numR, double scaleStart, double scaleEnd, double scaleInterval)
        {
            var userItemMatrix = new double[N, M];
            List<double> scales = new List<double>();
            for (double i = scaleStart; i <= scaleEnd; i += scaleInterval)
            {
                scales.Add(i);
            }

            int entriesWithRating = 0;
            int entriesWithoutRating = 0;
            var random = new Random();
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    if (entriesWithRating > numR)
                    {
                        userItemMatrix[i, j] = 0;
                        entriesWithoutRating++;
                    }
                    else if (N * M - entriesWithoutRating == numR)
                    {
                        int index = random.Next(scales.Count);
                        userItemMatrix[i, j] = scales[index];
                        entriesWithRating++;
                    }
                    else
                    {
                        if (random.Next(0, 2) == 1)
                        {
                            int index = random.Next(scales.Count);
                            userItemMatrix[i, j] = scales[index];
                            entriesWithRating++;
                        }
                        else
                        {
                            userItemMatrix[i, j] = 0;
                            entriesWithoutRating++;
                        }
                    }
                }
            }
            return userItemMatrix;
        }

        public static (double[,], double[,]) SplitToTrainingAndTesting(this double[,] userItemMatrix) // 70/30 hard-coded
        {
            int numR = 0;
            int n = userItemMatrix.GetLength(0);
            int m = userItemMatrix.GetLength(1);
            foreach (double entry in userItemMatrix)
            {
                if (entry != 0)
                {
                    numR++;
                }
            }
            int trainingEntriesLeft = (int)(numR * 0.7);
            int testingEntriesLeft = numR - trainingEntriesLeft;

            var traingingMatrix = new double[n, m];
            var testingMatrix = new double[n, m];

            var random = new Random();
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    if (userItemMatrix[i, j] == 0)
                    {
                        testingMatrix[i, j] = 0;
                        traingingMatrix[i, j] = 0;
                    }
                    else if (trainingEntriesLeft == 0)
                    {
                        testingMatrix[i, j] = userItemMatrix[i, j];
                        traingingMatrix[i, j] = 0;
                        testingEntriesLeft--;
                    }
                    else if (testingEntriesLeft == 0)
                    {
                        testingMatrix[i, j] = 0;
                        traingingMatrix[i, j] = userItemMatrix[i, j];
                        trainingEntriesLeft--;
                    }
                    else if (random.Next(1, 11) <= 7)
                    {
                        testingMatrix[i, j] = 0;
                        traingingMatrix[i, j] = userItemMatrix[i, j];
                        trainingEntriesLeft--;
                    }
                    else
                    {
                        testingMatrix[i, j] = userItemMatrix[i, j];
                        traingingMatrix[i, j] = 0;
                        testingEntriesLeft--;
                    }
                }
            }

            return (traingingMatrix, testingMatrix);
        }

        /// <summary>
        /// Split it in the vertical distribution scenario into K matrices of (almost equal) dimensions
        /// N x M_k, where M_k is eithr the upper or the lower bound of M/K
        /// </summary>
        /// <param name="numOfVendors">K</param>
        /// <returns></returns>
        public static List<double[,]> SplitToVendors(this double[,] userItemMatrix, int numOfVendors)
        {
            int N = userItemMatrix.GetLength(0);
            int M = userItemMatrix.GetLength(1);
            int size = M / numOfVendors;
            int lastSize = (M % numOfVendors) + size;
            List<double[,]> splittedUserItemMatrix = new List<double[,]>();
            for (int i = 0; i < numOfVendors; i++)
            {
                double[,] vendorUserItemMatrix;
                if (i == numOfVendors - 1)
                {
                    vendorUserItemMatrix = new double[N, lastSize];
                    Array.Copy(userItemMatrix, N * i * size, vendorUserItemMatrix, 0, N * lastSize);
                    splittedUserItemMatrix.Add(vendorUserItemMatrix);
                }
                else
                {
                    vendorUserItemMatrix = new double[N, size];
                    Array.Copy(userItemMatrix, N * i * size, vendorUserItemMatrix, 0, N * size);
                    splittedUserItemMatrix.Add(vendorUserItemMatrix);
                }
            }

            return splittedUserItemMatrix;
        }

        /// <summary>
        /// In order to retrive the ratings vector from all the users from the user-item matrix 
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static double[] GetVerticalVector(this double[,] matrix, int index)
        {
            int length = matrix.GetLength(0);
            double[] vector = new double[length];
            for (int i = 0; i < length; i++)
            {
                vector[i] = matrix[i, index];
            }
            return vector;
        }

    }
}
