using System;
using System.Collections.Generic;

namespace SecretSharing
{
    public class Matrix
    {
        private readonly double[,] _matrix;
        private readonly int _n;
        private readonly int _m;
        private readonly int _numR;

        // For mocking only, will change from real DB
        public Matrix(int N, int M, int numR, double scaleStart, double scaleEnd, double scaleInterval)
        {
            _n = N;
            _m = M;
            _numR = numR;
            _matrix = new double[N, M];

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
                        _matrix[i, j] = 0;
                        entriesWithoutRating++;
                    }
                    else if (N * M - entriesWithoutRating == numR)
                    {
                        int index = random.Next(scales.Count);
                        _matrix[i, j] = scales[index];
                        entriesWithRating++;

                    }
                    else
                    {
                        if (random.Next(0, 2) == 1)
                        {
                            int index = random.Next(scales.Count);
                            _matrix[i, j] = scales[index];
                            entriesWithRating++;
                        }
                        else
                        {
                            _matrix[i, j] = 0;
                            entriesWithoutRating++;
                        }
                    }
                }
            }
        }

        public double Get(int x, int y)
        {
            return _matrix[x, y];
        }

        public void Set(int x, int y, double value)
        {
            _matrix[x, y] = value;
        }

        public (double[,], double[,]) SplitToTrainingAndTesting() // 70/30 hard-coded
        {
            int trainingEntriesLeft = (int)(_numR * 0.7);
            int testingEntriesLeft = _numR - trainingEntriesLeft;

            var traingingMatrix = new double[_n, _m];
            var testingMatrix = new double[_n, _m];

            var random = new Random();
            for (int i = 0; i < _n; i++)
            {
                for (int j = 0; j < _m; j++)
                {
                    if (_matrix[i, j] == 0)
                    {
                        testingMatrix[i, j] = 0;
                        traingingMatrix[i, j] = 0;
                    }
                    else if (trainingEntriesLeft == 0)
                    {
                        testingMatrix[i, j] = _matrix[i, j];
                        traingingMatrix[i, j] = 0;
                        testingEntriesLeft--;
                    }
                    else if (testingEntriesLeft == 0)
                    {
                        testingMatrix[i, j] = 0;
                        traingingMatrix[i, j] = _matrix[i, j];
                        trainingEntriesLeft--;
                    }
                    else if (random.Next(1, 11) <= 7)
                    {
                        testingMatrix[i, j] = 0;
                        traingingMatrix[i, j] = _matrix[i, j];
                        trainingEntriesLeft--;
                    }
                    else
                    {
                        testingMatrix[i, j] = _matrix[i, j];
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
        public List<Matrix> SplitToVendors(int numOfVendors)
        {
            return null;
        }
    }
}
