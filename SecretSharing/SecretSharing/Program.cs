using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SecretSharing
{
    class Program
    {
        static void Main()
        {
            string dataset = "1M";
            // k - vendors
            // D - mediators
            int q = 10; // num of similar items
            int h = 6; // num of most recomended items to take

            RunTest(dataset, k: 2, D: 5, q, h);
            RunTest(dataset, k: 2, D: 3, q, h);
            RunTest(dataset, k: 5, D: 5, q, h);
            RunTest(dataset, k: 5, D: 3, q, h);
            RunTest(dataset, k: 1, D: 3, q, h);
        }
        static void RunTest(string dataset, int k, int D, int q, int h)
        {
            int[,] userItemMatrix = Protocols.ReadUserItemMatrix($"ratings-distict-{dataset}.dat");

            int N = userItemMatrix.GetLength(0); // users
            int M = userItemMatrix.GetLength(1); // items

            bool loadFromFile = false;
            bool calcAndSaveToFile = !loadFromFile;

            if (D != 3 && D != 5)
            {
                throw new Exception("Number of mediators must be 3 or 5");
            }

            string directoryName = $"k-{k}, D-{D}, Dataset-{dataset}/";
            Directory.CreateDirectory(directoryName);

            #region Spliting the items between the vendors

            List<int[]> vendorsItemIndecis = new List<int[]>(); // The i's entry contains the indecis of all of the items offerd by vendor i
            int[] itemsVendorIndex = new int[M]; // The i's entry contains the index of the vendor that holds that item
            int itemsPerVendor = M / k;

            for (int vendorIndex = 0; vendorIndex < k; vendorIndex++)
            {
                int start = vendorIndex * itemsPerVendor;
                int count;
                if (vendorIndex == k - 1)
                {
                    count = M - ((k - 1) * itemsPerVendor);
                }
                else
                {
                    count = itemsPerVendor;
                }
                vendorsItemIndecis.Add(Enumerable.Range(start, count).ToArray());
                Enumerable.Repeat(vendorIndex, count).ToArray().CopyTo(itemsVendorIndex, start);
            }

            #endregion

            #region Computing the similarity matrix (Protocol 1+2)

            int[,] trainingUserItemMatrix = null;
            int[,] testingUserItemMatrix = null;
            double[,] similarityMatrix = null;

            if (loadFromFile)
            {
                trainingUserItemMatrix = ArraysExtensions.LoadIntMatrixFromFile(directoryName + "trainingUserItemMatrix.txt");
                testingUserItemMatrix = ArraysExtensions.LoadIntMatrixFromFile(directoryName + "testingUserItemMatrix.txt");
                similarityMatrix = ArraysExtensions.LoaddoubleMatrixFromFile(directoryName + "similarityMatrix.txt");
            }
            else if (calcAndSaveToFile)
            {
                var sets = userItemMatrix.SplitToTrainingAndTesting();

                trainingUserItemMatrix = sets.Item1;
                testingUserItemMatrix = sets.Item2;

                trainingUserItemMatrix.SaveToFile(directoryName + "trainingUserItemMatrix.txt");
                testingUserItemMatrix.SaveToFile(directoryName + "testingUserItemMatrix.txt");

                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Database - {dataset}, k={k} D={D}" });

                similarityMatrix = Protocols.CalcSimilarityMatrix(trainingUserItemMatrix, D, itemsVendorIndex, directoryName);

                //double[,] similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(trainingUserItemMatrix);

                similarityMatrix.SaveToFile(directoryName + "similarityMatrix.txt");
            }

            #endregion

            #region Secret sharing R_hat and xiR using AON (Protocol 3)

            List<double[]>[] RHatShares = null;
            List<double[]>[] XiRShares = null;

            if (loadFromFile)
            {
                RHatShares = ArraysExtensions.LoadDoubleMatrixArrayFromFile(directoryName + "RHatShares.txt");
            }
            else if (calcAndSaveToFile)
            {
                var secretSharingWatch = System.Diagnostics.Stopwatch.StartNew();

                RHatShares = Protocols.SecretShareRHat(trainingUserItemMatrix, D);
                XiRShares = Protocols.SecretShareXiR(trainingUserItemMatrix, D);

                secretSharingWatch.Stop();
                var secretSharingTime = new TimeSpan(0, 0, 0, 0, (int)secretSharingWatch.ElapsedMilliseconds);
                Console.WriteLine($"Average time per vendor - Secret Sharing R_hat and xiR done in {(secretSharingTime / k)}");
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Average time per vendor - Secret Sharing R_hat and xiR done in {(secretSharingTime / k)}" });

                RHatShares.SaveToFile(directoryName + "RHatShares.txt");
            }

            #endregion

            #region Obfuscate the shares of xiR (Protocol 4)

            List<double[]>[] obfuscatedXiRShares = null;

            if (loadFromFile)
            {
                obfuscatedXiRShares = ArraysExtensions.LoadDoubleMatrixArrayFromFile(directoryName + "obfuscatedXiRShares.txt");
            }
            else if (calcAndSaveToFile)
            {
                var obfuscationWatch = System.Diagnostics.Stopwatch.StartNew();

                obfuscatedXiRShares = Protocols.ObfuscateShares(XiRShares);

                obfuscationWatch.Stop();
                var obfuscationTime = new TimeSpan(0, 0, 0, 0, (int)obfuscationWatch.ElapsedMilliseconds);
                Console.WriteLine($"Average time per mediator - Obfuscating the shares of xiR done in {obfuscationTime}");
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Average time per mediator - Obfuscate the shares of xiR done in {obfuscationTime}" });

                obfuscatedXiRShares.SaveToFile(directoryName + "obfuscatedXiRShares.txt");
            }

            #endregion

            #region Predict rating (Protocol 5)

            // get only the 30% indexes that dropped in order to compare
            var entriesToCompare = testingUserItemMatrix.GetNonZeroEntries();
            double MAE = 0;
            int entryIndex = 0;
            var averageRatings = trainingUserItemMatrix.GetAverageRatings();

            var vendorWatch = new System.Diagnostics.Stopwatch();
            var mediatorsWatch = new System.Diagnostics.Stopwatch();

            foreach (var entry in entriesToCompare)
            {
                mediatorsWatch.Start();

                int n = entry.Item1;
                int m = entry.Item2;

                double x_dSum = 0;
                double y_dSum = 0;

                var sm = Protocols.GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, m, q, true);
                foreach (var RHatShare in RHatShares)
                {
                    double[] RHat_n = RHatShare.GetHorizontalVector(n);
                    double x_d = Protocols.ScalarProductVectors(RHat_n, sm);
                    x_dSum += x_d;
                }
                foreach (var ObfuscatedXiRShare in obfuscatedXiRShares)
                {
                    double[] XiR_n = ObfuscatedXiRShare.GetHorizontalVector(n);
                    double y_d = Protocols.ScalarProductVectors(XiR_n, sm);
                    y_dSum += y_d;
                }

                x_dSum %= Protocols.PRIME;
                y_dSum %= Protocols.PRIME;

                var averageRating = averageRatings[m];

                mediatorsWatch.Stop();

                vendorWatch.Start();

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

                vendorWatch.Stop();

                double diff = Math.Abs(userItemMatrix[n, m] - predictedRating);

                MAE += diff;
                entryIndex++;
            }
            MAE /= entriesToCompare.Count();
            Console.WriteLine(MAE);

            var mediatorsTime = new TimeSpan(0, 0, 0, 0, (int)(mediatorsWatch.ElapsedMilliseconds / entriesToCompare.Count));
            Console.WriteLine($"Average time per mediator - Predicting a single rating done in {mediatorsTime}");
            File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Average time per mediator - Predicting a single rating done in {mediatorsTime}" });

            var vendorTime = new TimeSpan(0, 0, 0, 0, (int)(vendorWatch.ElapsedMilliseconds / entriesToCompare.Count));
            Console.WriteLine($"Average time for a vendor - Predicting a single rating done in {vendorTime}");
            File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Average time for a vendor - Predicting a single rating done in {vendorTime}" });

            #endregion

            #region Predict ranking (Protocol 6)

            vendorWatch = new System.Diagnostics.Stopwatch();
            mediatorsWatch = new System.Diagnostics.Stopwatch();

            int selectedVendor = 0; // from 0 to k-1
            int selectedUser = 0; // from 0 to N-1

            int[] vendorItems = vendorsItemIndecis[selectedVendor];
            int numOfItems = vendorItems.Length;
            int firstItemIndex = vendorItems[0];

            double[] x = new double[numOfItems];
            double[] y = new double[numOfItems];

            int i = 0;

            mediatorsWatch.Start();

            foreach (var itemIndex in vendorItems)
            {
                var sm = Protocols.GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, itemIndex, q, false);

                foreach (var obfuscatedXiRShare in obfuscatedXiRShares)
                {
                    double[] XiR_n = obfuscatedXiRShare.GetHorizontalVector(selectedUser);
                    double x_d = Protocols.ScalarProductVectors(XiR_n, sm);

                    mediatorsWatch.Stop();

                    vendorWatch.Start();

                    x[i] += x_d;
                    y[i] += XiR_n[itemIndex];

                    vendorWatch.Stop();

                    mediatorsWatch.Start();
                }
                x[i] %= Protocols.PRIME;
                y[i] %= Protocols.PRIME;
                i++;
            }

            List<int> indices = new List<int>();
            for (i = 0; i < numOfItems; i++)
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

            var valueAndIndexArray = valueAndIndex.ToArray();
            Array.Sort(valueAndIndexArray, new ScoreAndIndexComparer());
            int[] mostRecommendedItems = valueAndIndexArray.Take(h).Select(o => o.Item2 + firstItemIndex).ToArray();

            mediatorsWatch.Stop();
            var mediatorsTimePredicrRanking = new TimeSpan(0, 0, 0, 0, (int)mediatorsWatch.ElapsedMilliseconds);
            Console.WriteLine($"Average time per mediator - Predict ranking done in {mediatorsTime}");
            File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Average time per mediator - Predict ranking done in {mediatorsTime}" });

            var vendorTimePredictRanking = new TimeSpan(0, 0, 0, 0, (int)vendorWatch.ElapsedMilliseconds);
            Console.WriteLine($"Average time for a vendor - Predict ranking done in {vendorTime}");
            File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Average time for a vendor - Predict ranking done in {vendorTime}" });

            #endregion
        }
    }
}
