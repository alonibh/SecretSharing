using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SecretSharing
{
    class Program
    {
        static void Main(string[] args)
        {
            //(int, int)[] pairs = new (int, int)[12]
            //{
            //    (1000, 1),
            //    (1000, 2),
            //    (1000, 4),
            //    (1000, 8),
            //    (1000, 16),
            //    (1000, 32),
            //    (2000, 1),
            //    (2000, 2),
            //    (2000, 4),
            //    (2000, 8),
            //    (2000, 16),
            //    (2000, 32)
            //};
            //foreach (var pair in pairs)
            //{
            //    File.AppendAllText("stats.txt", $"K = {pair.Item1}, N = {pair.Item2}" + "\n");

            //    for (int j = 0; j < 10; j++)
            //    {
            //        Protocols.CreateRandomSplits(pair.Item1, pair.Item2);
            //    }
            //}
            //return;

            int[,] userItemMatrix = Protocols.ReadUserItemMatrix("ratings-distict-100K.dat");

            int N = userItemMatrix.GetLength(0); //users
            int M = userItemMatrix.GetLength(1); //items
            int k = 7; //vendors
            int D = 5; //mediators
            int q = 10; // num of similar items
            int h = 6; // num of most recomended items to take

            bool loadFromFile = false;
            bool calcAndSaveToFile = true;

            #region Computing the similarity matrix (Protocol 1+2)

            int[,] trainingUserItemMatrix;
            int[,] testingUserItemMatrix;
            BigInteger[,] similarityMatrix;

            //Protocols.RunProtocol2RuntimeTest(trainingUserItemMatrix, D);
            //return;

            if (loadFromFile)
            {
                trainingUserItemMatrix = _2DArrayExtensions.LoadIntMatrixFromFile("trainingUserItemMatrix.txt");
                testingUserItemMatrix = _2DArrayExtensions.LoadIntMatrixFromFile("testingUserItemMatrix.txt");
                similarityMatrix = _2DArrayExtensions.LoadBigIntegerMatrixFromFile("similarityMatrix.txt");

            }
            else if (calcAndSaveToFile)
            {
                var sets = userItemMatrix.SplitToTrainingAndTesting();

                trainingUserItemMatrix = sets.Item1;
                testingUserItemMatrix = sets.Item2;

                //trainingUserItemMatrix.SaveToFile("trainingUserItemMatrix.txt");
                //testingUserItemMatrix.SaveToFile("testingUserItemMatrix.txt");

                similarityMatrix = Protocols.CalcSimilarityMatrix(trainingUserItemMatrix, D);
                //BigInteger[,] similarityMatrix = Protocols.CalcSimilarityMatrixNoCrypto(trainingUserItemMatrix);

                //similarityMatrix.SaveToFile("similarityMatrix.txt");
            }

            return;

            #endregion

            #region Secret sharing R_hat and xiR using AON (Protocol 3)

            List<BigInteger[]>[] RHatShares;
            List<BigInteger[]>[] XiRShares;

            if (loadFromFile)
            {
                RHatShares = _2DArrayExtensions.LoadBigIntegerMatrixArrayFromFile("RHatShares.txt");
                XiRShares = _2DArrayExtensions.LoadBigIntegerMatrixArrayFromFile("XiRShares.txt");

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

            List<BigInteger[]>[] ObfuscatedXiRShares = Protocols.ObfuscateShares(XiRShares);

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

                BigInteger x_dSum = 0;
                BigInteger y_dSum = 0;

                var sm = Protocols.GetMostSimilarItemsToM(similarityMatrix, m, q, true);
                foreach (var RHatShare in RHatShares)
                {
                    BigInteger[] RHat_n = RHatShare.GetHorizontalVector(n);
                    BigInteger x_d = Protocols.ScalarProductVectors(RHat_n, sm);
                    x_dSum += x_d;
                }
                foreach (var ObfuscatedXiRShare in ObfuscatedXiRShares)
                {
                    BigInteger[] XiR_n = ObfuscatedXiRShare.GetHorizontalVector(n);
                    BigInteger y_d = Protocols.ScalarProductVectors(XiR_n, sm);
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

            BigInteger[] x = new BigInteger[count];
            BigInteger[] y = new BigInteger[count];
            int i = 0;
            foreach (var itemIndex in vendorItems)
            {
                var sm = Protocols.GetMostSimilarItemsToM(similarityMatrix, itemIndex, q, false);

                foreach (var RHatShare in RHatShares)
                {
                    BigInteger[] RHat_n = RHatShare.GetHorizontalVector(selectedUser);
                    BigInteger x_d = Protocols.ScalarProductVectors(RHat_n, sm);
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
            List<Tuple<BigInteger, int>> valueAndIndex = new List<Tuple<BigInteger, int>>();
            foreach (var index in indices)
            {
                valueAndIndex.Add(new Tuple<BigInteger, int>(x[index], index));
            }
            Array.Sort(valueAndIndex.ToArray(), new ScoreAndIndexComparer());
            valueAndIndex = valueAndIndex.Take(h).ToList();
            int[] mostRecommendedItems = valueAndIndex.Select(o => o.Item2 + start).ToArray();

            #endregion
        }
    }
}
