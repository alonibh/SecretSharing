using System;
using System.Collections.Generic;
using System.IO;

namespace SecretSharing
{
    public static class Extensions
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

        public static (sbyte[,], sbyte[,]) SplitToTrainingAndTesting(this sbyte[,] userItemMatrix) // 70/30 hard-coded
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

            var traingingMatrix = new sbyte[n, m];
            var testingMatrix = new sbyte[n, m];

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

        public static T[] GetVerticalVector<T>(this T[,] matrix, int index)
        {
            int length = matrix.GetLength(0);
            T[] vector = new T[length];
            for (int i = 0; i < length; i++)
            {
                vector[i] = matrix[i, index];
            }
            return vector;
        }

        public static T[] SetVerticalVector<T>(this T[,] matrix, T[] vector, int index)
        {
            int length = matrix.GetLength(0);
            for (int i = 0; i < length; i++)
            {
                matrix[i, index] = vector[i];
            }
            return vector;
        }

        public static T[] GetHorizontalVector<T>(this T[,] matrix, int index)
        {
            int length = matrix.GetLength(1);
            T[] vector = new T[length];
            for (int i = 0; i < length; i++)
            {
                vector[i] = matrix[index, i];
            }
            return vector;
        }

        public static double[] GetAverageRatings(this sbyte[,] userItemMatrix)
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
                    {
                        nonZeroRatings++;
                    }
                }
                if (nonZeroRatings == 0)
                {
                    averageRatings[i] = 0;
                }
                else
                {
                    averageRatings[i] = ratingSum / nonZeroRatings;
                }
            }
            return averageRatings;
        }

        public static double[,] GetAdjustedUserItemMatrix(this sbyte[,] userItemMatrix, double Q)
        {
            int users = userItemMatrix.GetLength(0);
            int items = userItemMatrix.GetLength(1);
            double[,] adjustedUserItemMatrix = new double[users, items];

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
                        adjustedUserItemMatrix[i, j] = Math.Floor((adjustedRating * Q) + 0.5);
                    }
                }
            }

            return adjustedUserItemMatrix;
        }

        public static double[] GetHorizontalVector(this List<double[]> matrix, int n)
        {
            int length = matrix.Count;
            double[] vector = new double[length];
            for (int i = 0; i < length; i++)
            {
                vector[i] = matrix[i][n];
            }
            return vector;
        }

        public static sbyte[,] GetXi(this sbyte[,] matrix)
        {
            int users = matrix.GetLength(0);
            int items = matrix.GetLength(1);
            sbyte[,] xiMatrix = new sbyte[users, items];

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

        public static uint[] GetXi(this uint[] vector)
        {
            int length = vector.Length;
            uint[] xiVector = new uint[length];

            for (int i = 0; i < length; i++)
            {
                if (vector[i] == 0)
                {
                    xiVector[i] = 0;
                }
                else
                {
                    xiVector[i] = 1;
                }
            }

            return xiVector;
        }

        public static uint[] GetSq(this uint[] vector)
        {
            int length = vector.Length;
            uint[] sqVector = new uint[length];

            for (int i = 0; i < length; i++)
            {
                sqVector[i] = sqVector[i] * sqVector[i];
            }

            return sqVector;
        }

        public static sbyte[] GetXi(this sbyte[] vector)
        {
            int length = vector.Length;
            sbyte[] xiVector = new sbyte[length];

            for (int i = 0; i < length; i++)
            {
                if (vector[i] == 0)
                {
                    xiVector[i] = 0;
                }
                else
                {
                    xiVector[i] = 1;
                }
            }

            return xiVector;
        }

        public static (int, int) GetFirstNotNullEntry(this sbyte[,] matrix)
        {
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(1);

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    if (matrix[i, j] != -1)
                    {
                        return (i, j);
                    }
                }
            }

            throw new Exception("Vendor does not offer any items");
        }

        public static void SaveToFile(this double[,] matrix, string path)
        {
            List<string> lines = new List<string>();
            int n = matrix.GetLength(0);
            int m = matrix.GetLength(1);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    lines.Add(i + " " + j + " " + matrix[i, j]);
                }
            }
            File.WriteAllLines(path, lines);
        }

        public static double[,] LoadDoubleMatrixFromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            int N = int.Parse(lines[lines.Length - 1].Split()[0]) + 1;
            int M = int.Parse(lines[lines.Length - 1].Split()[1]) + 1;
            double[,] matrix = new double[N, M];

            foreach (var line in lines)
            {
                int n = int.Parse(line.Split()[0]);
                int m = int.Parse(line.Split()[1]);
                double rating = double.Parse(line.Split()[2]);
                matrix[n, m] = rating;
            }

            return matrix;
        }

        public static void SaveToFile(this List<double[]>[] matrixArray, string path)
        {
            List<string> lines = new List<string>();
            int length = matrixArray.Length;
            int count = matrixArray[0].Count;
            int items = matrixArray[0][0].Length;
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    for (int k = 0; k < items; k++)
                    {
                        lines.Add(i + " " + j + " " + k + " " + matrixArray[i][j][k]);
                    }
                }
            }
            File.WriteAllLines(path, lines);
        }

        public static List<double[]>[] LoadDoubleMatrixArrayFromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            int length = int.Parse(lines[lines.Length - 1].Split()[0]) + 1;
            int count = int.Parse(lines[lines.Length - 1].Split()[1]) + 1;
            int items = int.Parse(lines[lines.Length - 1].Split()[2]) + 1;
            var matrixArray = new List<double[]>[length];
            foreach (var line in lines)
            {
                var splitted = line.Split();
                int n = int.Parse(splitted[0]);
                int m = int.Parse(splitted[1]);
                int item = int.Parse(splitted[2]);
                double rating = double.Parse(splitted[3]);
                if (matrixArray[n] == null)
                {
                    matrixArray[n] = new List<double[]>();
                    for (int i = 0; i < count; i++)
                    {
                        matrixArray[n].Add(new double[items]);
                    }
                }

                matrixArray[n][m][item] = rating;
            }

            return matrixArray;
        }

        public static void SaveToFile(this int[,] matrix, string path)
        {
            List<string> lines = new List<string>();
            int n = matrix.GetLength(0);
            int m = matrix.GetLength(1);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    lines.Add(i + " " + j + " " + matrix[i, j]);
                }
            }
            File.WriteAllLines(path, lines);
        }

        public static void SaveToFile(this sbyte[,] matrix, string path)
        {
            List<string> lines = new List<string>();
            int n = matrix.GetLength(0);
            int m = matrix.GetLength(1);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    lines.Add(i + " " + j + " " + matrix[i, j]);
                }
            }
            File.WriteAllLines(path, lines);
        }

        public static int[,] LoadIntMatrixFromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            int N = int.Parse(lines[lines.Length - 1].Split()[0]) + 1;
            int M = int.Parse(lines[lines.Length - 1].Split()[1]) + 1;
            int[,] matrix = new int[N, M];

            foreach (var line in lines)
            {
                int n = int.Parse(line.Split()[0]);
                int m = int.Parse(line.Split()[1]);
                int rating = int.Parse(line.Split()[2]);
                matrix[n, m] = rating;
            }

            return matrix;
        }

        public static sbyte[,] LoadSbyteMatrixFromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            int N = int.Parse(lines[lines.Length - 1].Split()[0]) + 1;
            int M = int.Parse(lines[lines.Length - 1].Split()[1]) + 1;
            sbyte[,] matrix = new sbyte[N, M];

            foreach (var line in lines)
            {
                int n = int.Parse(line.Split()[0]);
                int m = int.Parse(line.Split()[1]);
                sbyte rating = sbyte.Parse(line.Split()[2]);
                matrix[n, m] = rating;
            }

            return matrix;
        }
        public static void SaveToFile(this double[] vector, string path)
        {
            List<string> lines = new List<string>();
            foreach (var entry in vector)
            {
                lines.Add(entry.ToString());
            }

            File.WriteAllLines(path, lines);
        }

        public static double[] LoadVectorFromFile(string path)
        {
            var lines = File.ReadAllLines(path);

            double[] vector = new double[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                vector[i] = double.Parse(lines[i]);
            }

            return vector;
        }

        public static List<(int, int)> GetNonZeroEntries(this sbyte[,] matrix)
        {
            List<(int, int)> indecis = new List<(int, int)>();
            int n = matrix.GetLength(0);
            int m = matrix.GetLength(1);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    if (matrix[i, j] != 0)
                    {
                        indecis.Add((i, j));
                    }
                }
            }
            return indecis;
        }

        /// <summary>
        /// For the YP Protocol
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static int GetAverageRating(this sbyte[,] matrix)
        {
            int sum = 0;
            int n = matrix.GetLength(0);
            int m = matrix.GetLength(1);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    sum += matrix[i, j];
                }
            }
            int average = sum / (n * m);
            return average;
        }

        public static sbyte[,] GetVerticalSubMatrix(this sbyte[,] matrix, int[] indecis)
        {
            sbyte[,] subMatrix = new sbyte[matrix.GetLength(0), indecis.Length];
            int start = 0;
            foreach (var index in indecis)
            {
                var vector = matrix.GetVerticalVector(index);
                subMatrix.SetVerticalVector(vector, start);
                start++;
            }
            return subMatrix;
        }

        public static sbyte[,] PlaceFakeCells(this sbyte[,] matrix, int numOfCellsToPlaceFakeRating, sbyte fakeRating)
        {
            var fakeMatrix = matrix.Clone() as sbyte[,];
            Random random = new Random();
            while (numOfCellsToPlaceFakeRating > 0)
            {
                int i = random.Next(matrix.GetLength(0));
                int j = random.Next(matrix.GetLength(1));
                if (fakeMatrix[i, j] == 0)
                {
                    fakeMatrix[i, j] = fakeRating;
                    numOfCellsToPlaceFakeRating--;
                }
            }

            return fakeMatrix;
        }

        public static sbyte[,] CalcSq(this sbyte[,] matrix)
        {
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(1);

            sbyte[,] sq = new sbyte[N, M];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    if (matrix[i, j] != -1)
                    {
                        sq[i, j] = (sbyte)Math.Pow(matrix[i, j], 2);
                    }
                }
            }
            return sq;
        }

        public static sbyte[,] CalcXi(this sbyte[,] matrix)
        {
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(1);

            sbyte[,] xi = new sbyte[N, M];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    if (matrix[i, j] != -1)
                    {
                        xi[i, j] = (sbyte)(matrix[i, j] == 0 ? 0 : 1);
                    }
                }
            }
            return xi;
        }

        public static uint[,] AddShare(this uint[,] matrix, uint[,] share)
        {
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(1);
            uint[,] sumMatrix = new uint[N, M];

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    sumMatrix[i, j] = matrix[i, j] + share[i, j];
                }
            }

            return sumMatrix;
        }

        public static double[,] AddShare(this double[,] matrix, double[,] share)
        {
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(1);
            double[,] sumMatrix = new double[N, M];

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    sumMatrix[i, j] = matrix[i, j] + share[i, j];
                }
            }

            return sumMatrix;
        }

        public static double[,] ApplyModulo(this double[,] matrix, double modulo)
        {
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(1);
            double[,] modMatrix = new double[N, M];

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    modMatrix[i, j] = matrix[i, j] % modulo;
                }
            }

            return modMatrix;
        }

        public static uint[,] ApplyModulo(this uint[,] matrix, ulong modulo)
        {
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(1);
            uint[,] modMatrix = new uint[N, M];

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    modMatrix[i, j] = (uint)(matrix[i, j] % modulo);
                }
            }

            return modMatrix;
        }

        public static void CopySubMatrix(this sbyte[,] matrix, sbyte[,] subMatrix, int verticalIndexStart)
        {
            for (int i = 0; i < subMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < subMatrix.GetLength(1); j++)
                {
                    matrix[i, j + verticalIndexStart] = subMatrix[i, j];
                }
            }
        }

        public static string ToCustomTimeSpanFormat(this TimeSpan timespan, bool includeMs = true)
        {
            string formatted = $"{timespan.Hours}h {timespan.Minutes}m {timespan.Seconds}s";
            if (includeMs)
            {
                formatted += $" {timespan.Milliseconds}ms";
            }

            return formatted;
        }
    }
}
