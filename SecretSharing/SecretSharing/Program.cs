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
            int[,] userItemMatrix = Protocols.ReadUserItemMatrix("ratings-distict-100K.dat");

            int N = userItemMatrix.GetLength(0); //users
            int M = userItemMatrix.GetLength(1); //items
            int k = 7; //vendors
            int q = 10; // num of similar items
            int h = 6; // num of most recomended items to take

            //var sets = userItemMatrix.SplitToTrainingAndTesting();
            //var trainingUserItemMatrix = sets.Item1;
            //var testingUserItemMatrix = sets.Item2;

            //trainingUserItemMatrix.SaveToFile("trainingUserItemMatrix.txt");
            var trainingUserItemMatrix = _2DArrayExtensions.LoadIntMatrixFromFile("trainingUserItemMatrix.txt");

            //testingUserItemMatrix.SaveToFile("testingUserItemMatrix.txt");
            var testingUserItemMatrix = _2DArrayExtensions.LoadIntMatrixFromFile("testingUserItemMatrix.txt");

            #region Computing the similarity matrix (Protocol 1+2)

            //BigInteger[,] similarityMatrix = Protocols.CalcSimilarityMatrix(trainingUserItemMatrix, D);
            //BigInteger[,] similarityMatrix = Protocols.CalcSimilarityMatrixNoCrypto(trainingUserItemMatrix);

            //similarityMatrix.SaveToFile("similarityMatrix.txt");
            var similarityMatrix = _2DArrayExtensions.LoadBigIntegerMatrixFromFile("similarityMatrix.txt");

            #endregion

            #region Secret sharing R_hat and xiR using AON (Protocol 3)

            //var RHatShares = Protocols.SecretShareRHat(trainingUserItemMatrix, D);
            //RHatShares.SaveToFile("RHatShares.txt");
            var RHatShares = _2DArrayExtensions.LoadBigIntegerMatrixArrayFromFile("RHatShares.txt");

            //var XiRShares = Protocols.SecretShareXiR(trainingUserItemMatrix, D);
            //XiRShares.SaveToFile("XiRShares.txt");
            var XiRShares = _2DArrayExtensions.LoadBigIntegerMatrixArrayFromFile("XiRShares.txt");

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
