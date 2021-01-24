﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SecretSharing
{
    class Program
    {
        static void Main(string[] args)
        {
            int[,] userItemMatrix = Protocols.ReadUserItemMatrix("ratings-distict-100K.dat");

            int N = userItemMatrix.GetLength(0); // users
            int M = userItemMatrix.GetLength(1); // items
            int k = 7; // vendors
            int D = 5; // mediators
            int q = 10; // num of similar items
            int h = 6; // num of most recomended items to take

            bool loadFromFile = false;
            bool calcAndSaveToFile = true;

            #region Computing the similarity matrix (Protocol 1+2)

            int[,] trainingUserItemMatrix;
            int[,] testingUserItemMatrix;
            double[,] similarityMatrix;

            //Protocols.RunProtocol2RuntimeTest(trainingUserItemMatrix, D);
            //return;

            if (loadFromFile)
            {
                trainingUserItemMatrix = _2DArrayExtensions.LoadIntMatrixFromFile("trainingUserItemMatrix.txt");
                testingUserItemMatrix = _2DArrayExtensions.LoadIntMatrixFromFile("testingUserItemMatrix.txt");
                similarityMatrix = _2DArrayExtensions.LoaddoubleMatrixFromFile("similarityMatrix.txt");

            }
            else if (calcAndSaveToFile)
            {
                var sets = userItemMatrix.SplitToTrainingAndTesting();

                trainingUserItemMatrix = sets.Item1;
                testingUserItemMatrix = sets.Item2;

                //trainingUserItemMatrix.SaveToFile("trainingUserItemMatrix.txt");
                //testingUserItemMatrix.SaveToFile("testingUserItemMatrix.txt");

                similarityMatrix = Protocols.CalcSimilarityMatrix(trainingUserItemMatrix, D);
                //double[,] similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(trainingUserItemMatrix);

                //similarityMatrix.SaveToFile("similarityMatrix.txt");
            }

            return;

            #endregion

            #region Secret sharing R_hat and xiR using AON (Protocol 3)

            List<double[]>[] RHatShares;
            List<double[]>[] XiRShares;

            if (loadFromFile)
            {
                RHatShares = _2DArrayExtensions.LoaddoubleMatrixArrayFromFile("RHatShares.txt");
                XiRShares = _2DArrayExtensions.LoaddoubleMatrixArrayFromFile("XiRShares.txt");

            }
            else if (calcAndSaveToFile)
            {

                RHatShares = Protocols.SecretShareRHat(trainingUserItemMatrix, D);
                RHatShares.SaveToFile("RHatShares.txt");

                XiRShares = Protocols.SecretShareXiR(trainingUserItemMatrix, D);
                XiRShares.SaveToFile("XiRShares.txt");
            }

            #endregion

            #region Obfuscate the shares of xiR (Protocol 4)

            List<double[]>[] ObfuscatedXiRShares = Protocols.ObfuscateShares(XiRShares);

            #endregion

            #region Predict rating (Protocol 5)

            // get only the 30% indexes that dropped in order to compare
            var entriesToCompare = testingUserItemMatrix.GetNonZeroEntries();
            double MAE = 0;
            int entryIndex = 0;
            var averageRatings = trainingUserItemMatrix.GetAverageRatings();

            foreach (var entry in entriesToCompare)
            {
                int n = entry.Item1;
                int m = entry.Item2;

                double x_dSum = 0;
                double y_dSum = 0;

                var sm = Protocols.GetMostSimilarItemsToM(similarityMatrix, m, q, true);
                foreach (var RHatShare in RHatShares)
                {
                    double[] RHat_n = RHatShare.GetHorizontalVector(n);
                    double x_d = Protocols.ScalarProductVectors(RHat_n, sm);
                    x_dSum += x_d;
                }
                foreach (var ObfuscatedXiRShare in ObfuscatedXiRShares)
                {
                    double[] XiR_n = ObfuscatedXiRShare.GetHorizontalVector(n);
                    double y_d = Protocols.ScalarProductVectors(XiR_n, sm);
                    y_dSum += y_d;
                }

                x_dSum %= Protocols.PRIME;
                y_dSum %= Protocols.PRIME;

                var averageRating = averageRatings[m];
                double predictedRating = averageRating;
                double change = 0;
                if (y_dSum != 0)
                {
                    change = (double)(x_dSum / y_dSum) / Protocols.Q;
                    // if thats true that x_sum is negative
                    if (change > 5)
                    {
                        x_dSum = Protocols.PRIME - x_dSum;
                        change = (double)(x_dSum / y_dSum) / Protocols.Q * -1;
                    }
                    predictedRating += change;
                }

                predictedRating = (int)Math.Round(predictedRating, 0);
                double diff = Math.Abs(userItemMatrix[n, m] - predictedRating);

                Console.WriteLine(entryIndex + "/" + entriesToCompare.Count);
                MAE += diff;
                entryIndex++;
            }
            MAE /= entriesToCompare.Count();
            Console.WriteLine(MAE);
            return;

            #endregion

            #region Predict ranking(Protocol 6)

            int selectedVendor = 6; // from 0 to k-1
            int itemsPerVendor = M / k;
            int start = selectedVendor * itemsPerVendor;
            int count;
            if (selectedVendor == k - 1)
            {
                count = M - ((k - 1) * itemsPerVendor);
            }
            else
            {
                count = itemsPerVendor;
            }

            int[] vendorItems = Enumerable.Range(start, count).ToArray();

            int selectedUser = 7;

            double[] x = new double[count];
            double[] y = new double[count];
            int i = 0;
            foreach (var itemIndex in vendorItems)
            {
                var sm = Protocols.GetMostSimilarItemsToM(similarityMatrix, itemIndex, q, false);

                foreach (var RHatShare in RHatShares)
                {
                    double[] RHat_n = RHatShare.GetHorizontalVector(selectedUser);
                    double x_d = Protocols.ScalarProductVectors(RHat_n, sm);
                    x[i] += x_d;

                    y[i] += RHat_n[itemIndex];

                }
                x[i] %= Protocols.PRIME;
                y[i] %= Protocols.PRIME;
                i++;
            }
            List<int> indices = new List<int>();
            for (i = 0; i < count; i++)
            {
                if (y[i] == 0)
                {
                    indices.Add(i);
                }
            }
            List<Tuple<double, int>> valueAndIndex = new List<Tuple<double, int>>();
            foreach (var index in indices)
            {
                valueAndIndex.Add(new Tuple<double, int>(x[index], index));
            }
            Array.Sort(valueAndIndex.ToArray(), new ScoreAndIndexComparer());
            valueAndIndex = valueAndIndex.Take(h).ToList();
            int[] mostRecommendedItems = valueAndIndex.Select(o => o.Item2 + start).ToArray();

            #endregion
        }
    }
}
