using System;
using System.Collections.Generic;
using System.Numerics;

namespace SecretSharing
{
    public static class _2DArrayExtensions
    {
        public static int[,] CreateRandomUserItemMatrix(int N, int M, int numR, int scaleStart, int scaleEnd, int scaleInterval)
        {
            var userItemMatrix = new int[N, M];
            List<int> scales = new List<int>();
            for (int i = scaleStart; i <= scaleEnd; i += scaleInterval)
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

        public static (int[,], int[,]) SplitToTrainingAndTesting(this int[,] userItemMatrix) // 70/30 hard-coded
        {
            int numR = 0;
            int n = userItemMatrix.GetLength(0);
            int m = userItemMatrix.GetLength(1);
            foreach (int entry in userItemMatrix)
            {
                if (entry != 0)
                {
                    numR++;
                }
            }
            int trainingEntriesLeft = (int)(numR * 0.7);
            int testingEntriesLeft = numR - trainingEntriesLeft;

            var traingingMatrix = new int[n, m];
            var testingMatrix = new int[n, m];

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
        public static List<int[,]> SplitToVendors(this int[,] userItemMatrix, int numOfVendors)
        {
            int N = userItemMatrix.GetLength(0);
            int M = userItemMatrix.GetLength(1);
            int size = M / numOfVendors;
            int lastSize = (M % numOfVendors) + size;
            List<int[,]> splittedUserItemMatrix = new List<int[,]>();
            for (int i = 0; i < numOfVendors; i++)
            {
                int[,] vendorUserItemMatrix;
                if (i == numOfVendors - 1)
                {
                    vendorUserItemMatrix = new int[N, lastSize];
                    Array.Copy(userItemMatrix, N * i * size, vendorUserItemMatrix, 0, N * lastSize);
                    splittedUserItemMatrix.Add(vendorUserItemMatrix);
                }
                else
                {
                    vendorUserItemMatrix = new int[N, size];
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
        public static int[] GetVerticalVector(this int[,] matrix, int index)
        {
            int length = matrix.GetLength(0);
            int[] vector = new int[length];
            for (int i = 0; i < length; i++)
            {
                vector[i] = matrix[i, index];
            }
            return vector;
        }

        public static BigInteger[] GetVerticalVector(this BigInteger[,] matrix, int index)
        {
            int length = matrix.GetLength(0);
            BigInteger[] vector = new BigInteger[length];
            for (int i = 0; i < length; i++)
            {
                vector[i] = matrix[i, index];
            }
            return vector;
        }

        /// <summary>
        /// In order to retrive r_n from the user-item matrix 
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static double[] GetHorizontalVector(this double[,] matrix, int index)
        {
            int length = matrix.GetLength(1);
            double[] vector = new double[length];
            for (int i = 0; i < length; i++)
            {
                vector[i] = matrix[index, i];
            }
            return vector;
        }

        public static double[] GetAverageRatings(this int[,] userItemMatrix)
        {
            int users = userItemMatrix.GetLength(0);
            int items = userItemMatrix.GetLength(1);
            double[] averageRatings = new double[items];

            for (int i = 0; i < items; i++)
            {
                double ratingSum = 0;
                double nonZeroRatings = 0;
                for (int j = 0; j < users; j++)
                {
                    ratingSum += userItemMatrix[j, i];
                    if (userItemMatrix[j, i] != 0)
                        nonZeroRatings++;
                }
                averageRatings[i] = ratingSum / nonZeroRatings;
            }
            return averageRatings;
        }

        public static BigInteger[,] GetAdjustedUserItemMatrix(this int[,] userItemMatrix, double Q)
        {
            int users = userItemMatrix.GetLength(0);
            int items = userItemMatrix.GetLength(1);
            BigInteger[,] adjustedUserItemMatrix = new BigInteger[users, items];

            double[] averageRatings = userItemMatrix.GetAverageRatings();
            for (int i = 0; i < users; i++)
            {
                for (int j = 0; j < items; j++)
                {
                    if (userItemMatrix[i, j] == 0)
                    {
                        adjustedUserItemMatrix[i, j] = 0;
                    }
                    else
                    {
                        var adjustedRating = userItemMatrix[i, j] - averageRatings[j];
                        adjustedUserItemMatrix[i, j] = (BigInteger)Math.Floor((adjustedRating * Q) + 0.5);
                    }
                }
            }

            return adjustedUserItemMatrix;
        }

        public static BigInteger[] GetHorizontalVector(this List<BigInteger[]> matrix, int n)
        {
            int length = matrix.Count;
            BigInteger[] vector = new BigInteger[length];
            for (int i = 0; i < length; i++)
            {
                vector[i] = matrix[i][n];
            }
            return vector;
        }

        public static int[,] GetXi(this int[,] matrix)
        {
            int users = matrix.GetLength(0);
            int items = matrix.GetLength(1);
            int[,] xiMatrix = new int[users, items];

            for (int i = 0; i < users; i++)
            {
                for (int j = 0; j < items; j++)
                {
                    if (matrix[i, j] == 0)
                    {
                        xiMatrix[i, j] = 0;
                    }
                    else
                    {
                        xiMatrix[i, j] = 1;
                    }
                }
            }

            return xiMatrix;
        }
    }
}
