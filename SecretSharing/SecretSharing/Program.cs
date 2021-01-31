using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecretSharing
{
    class Program
    {
        static void Main()
        {
            string dataset = "1M";
            // k - vendors
            // D - mediators
            int q = 80; // num of similar items
            int h = 20; // num of most recomended items to take

            RunTest(dataset, k: 2, D: 5, q, h, percentOfFakeCells: 5);
            RunTest(dataset, k: 2, D: 3, q, h, percentOfFakeCells: 5);
            RunTest(dataset, k: 5, D: 5, q, h, percentOfFakeCells: 5);
            RunTest(dataset, k: 5, D: 3, q, h, percentOfFakeCells: 5);
            RunTest(dataset, k: 1, D: 3, q, h, percentOfFakeCells: 5);
        }
        static void RunTest(string dataset, int k, int D, int q, int h, int percentOfFakeCells)
        {
            #region Settings

            bool loadFromFile = false;
            bool saveToFile = true;
            bool predictRating = true;
            bool predictRanking = true;

            int[,] userItemMatrix = Protocols.ReadUserItemMatrix($"ratings-distict-{dataset}.dat");

            int N = userItemMatrix.GetLength(0); // users
            int M = userItemMatrix.GetLength(1); // items

            if (D != 3 && D != 5 && D != 7)
            {
                throw new Exception("Number of mediators must be 3 or 5");
            }

            string directoryName = $"k-{k}, D-{D}, Dataset-{dataset}/";
            Directory.CreateDirectory(directoryName);

            #endregion

            #region Spliting the items between the vendors

            List<int[]> vendorsItemsIndecis = new List<int[]>(); // The i's entry contains the indecis of all of the items offerd by vendor i
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
                vendorsItemsIndecis.Add(Enumerable.Range(start, count).ToArray());
                Enumerable.Repeat(vendorIndex, count).ToArray().CopyTo(itemsVendorIndex, start);
            }

            #endregion

            #region Computing the similarity matrix (Protocol 1+2)

            int[,] trainingUserItemMatrix = null;
            int[,] testingUserItemMatrix = null;
            double[,] similarityMatrix = null;
            int[,] YPUserItemMatrix = null;
            double[,] YPsimilarityMatrix = null;

            if (loadFromFile)
            {
                trainingUserItemMatrix = Extensions.LoadIntMatrixFromFile(directoryName + "trainingUserItemMatrix.txt");
                testingUserItemMatrix = Extensions.LoadIntMatrixFromFile(directoryName + "testingUserItemMatrix.txt");
                similarityMatrix = Extensions.LoadDoubleMatrixFromFile(directoryName + "similarityMatrix.txt");
                YPsimilarityMatrix = Extensions.LoadDoubleMatrixFromFile(directoryName + "YPsimilarityMatrix.txt");
                YPUserItemMatrix = Extensions.LoadIntMatrixFromFile(directoryName + "YPUserItemMatrix.txt");
            }
            else
            {
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Database - {dataset}, k={k} D={D}" });

                var sets = userItemMatrix.SplitToTrainingAndTesting();

                trainingUserItemMatrix = sets.Item1;
                testingUserItemMatrix = sets.Item2;

                similarityMatrix = Protocols.CalcSimilarityMatrix(trainingUserItemMatrix, D, itemsVendorIndex, directoryName);

                YPUserItemMatrix = Protocols.GetYPUserItemMatrix(trainingUserItemMatrix, vendorsItemsIndecis, percentOfFakeCells);
                YPsimilarityMatrix = Protocols.CalcSimilarityMatrixNoCrypto(YPUserItemMatrix);
            }

            if (saveToFile)
            {
                trainingUserItemMatrix.SaveToFile(directoryName + "trainingUserItemMatrix.txt");
                testingUserItemMatrix.SaveToFile(directoryName + "testingUserItemMatrix.txt");
                similarityMatrix.SaveToFile(directoryName + "similarityMatrix.txt");
                YPsimilarityMatrix.SaveToFile(directoryName + "YPsimilarityMatrix.txt");
                YPUserItemMatrix.SaveToFile(directoryName + "YPUserItemMatrix.txt");
            }

            #endregion

            #region Secret sharing R_hat and xiR using AON (Protocol 3)

            List<double[]>[] RHatShares = null;
            List<double[]>[] XiRShares = null;

            var secretSharingWatch = System.Diagnostics.Stopwatch.StartNew();

            RHatShares = Protocols.SecretShareRHat(trainingUserItemMatrix, D);
            XiRShares = Protocols.SecretShareXiR(trainingUserItemMatrix, D);

            secretSharingWatch.Stop();
            var secretSharingTime = new TimeSpan(0, 0, 0, 0, (int)secretSharingWatch.ElapsedMilliseconds);
            Console.WriteLine($"Protocol 3 - Average runtime for each vendor is {(secretSharingTime / k).ToCustomTimeSpanFormat()}");
            File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Protocol 3 - Average runtime for each vendor is {(secretSharingTime / k).ToCustomTimeSpanFormat()}" });

            #endregion

            #region Obfuscate the shares of xiR (Protocol 4)

            List<double[]>[] obfuscatedXiRShares = null;

            var obfuscationWatch = System.Diagnostics.Stopwatch.StartNew();

            obfuscatedXiRShares = Protocols.ObfuscateShares(XiRShares);

            obfuscationWatch.Stop();
            var obfuscationTime = new TimeSpan(0, 0, 0, 0, (int)obfuscationWatch.ElapsedMilliseconds);
            Console.WriteLine($"Protocol 4 - Average runtime for each mediator is {(obfuscationTime / D).ToCustomTimeSpanFormat()}");
            File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Protocol 4 - Average runtime for each mediator is  {(obfuscationTime / D).ToCustomTimeSpanFormat()}" });

            #endregion

            #region Predict rating (Protocol 5)

            var vendorWatch = new System.Diagnostics.Stopwatch();
            var mediatorsWatch = new System.Diagnostics.Stopwatch();

            if (predictRating)
            {
                double YP_MAE = 0;

                // get only the 30% indexes that dropped in order to compare
                var entriesToCompare = testingUserItemMatrix.GetNonZeroEntries();
                double MAE = 0;
                int entryIndex = 0;
                var averageRatings = trainingUserItemMatrix.GetAverageRatings();

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

                    if (predictedRating > 5)
                    {
                        predictedRating = 5;
                    }
                    else if (predictedRating < 0)
                    {
                        predictedRating = 0;
                    }

                    vendorWatch.Stop();

                    double diff = Math.Abs(userItemMatrix[n, m] - predictedRating);
                    MAE += diff;

                    int YPPredictedRating = Protocols.GetPredictedRatingNoCrypto(YPUserItemMatrix, n, m, q, YPsimilarityMatrix);
                    double YPdiff = Math.Abs(userItemMatrix[n, m] - YPPredictedRating);
                    YP_MAE += YPdiff;

                    entryIndex++;
                }
                MAE /= entriesToCompare.Count();
                YP_MAE /= entriesToCompare.Count();

                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"The MAE is - {MAE}" });
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"The MAE For the YP Protocol is - {YP_MAE}" });

                var mediatorsTime = new TimeSpan(0, 0, 0, 0, (int)(mediatorsWatch.ElapsedMilliseconds / entriesToCompare.Count));
                Console.WriteLine($"Protocol 5 - Average runtime for each mediator is {(mediatorsTime / D).ToCustomTimeSpanFormat(true)}");
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Protocol 5 - Average runtime for each mediator is {(mediatorsTime / D).ToCustomTimeSpanFormat(true)}" });

                var vendorTime = new TimeSpan(0, 0, 0, 0, (int)(vendorWatch.ElapsedMilliseconds / entriesToCompare.Count));
                Console.WriteLine($"Protocol 5 - Average runtime for each vendor is {vendorTime.ToCustomTimeSpanFormat(true)}");
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Protocol 5 - Average runtime for each vendor is {vendorTime.ToCustomTimeSpanFormat(true)}" });
            }

            #endregion

            #region Predict ranking (Protocol 6)

            if (predictRanking)
            {

                vendorWatch = new System.Diagnostics.Stopwatch();
                mediatorsWatch = new System.Diagnostics.Stopwatch();

                int selectedVendor = 0; // from 0 to k-1
                int selectedUser = 0; // from 0 to N-1

                int[] vendorItems = vendorsItemsIndecis[selectedVendor];
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
                Console.WriteLine($"Protocol 6 - Average runtime for each mediator is {(mediatorsTimePredicrRanking / D).ToCustomTimeSpanFormat(true)}");
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Protocol 6 - Average runtime for each mediator is {(mediatorsTimePredicrRanking / D).ToCustomTimeSpanFormat(true)}" });

                var vendorTimePredictRanking = new TimeSpan(0, 0, 0, 0, (int)vendorWatch.ElapsedMilliseconds);
                Console.WriteLine($"Protocol 6 - Average runtime for each vendor is {vendorTimePredictRanking.ToCustomTimeSpanFormat(true)}");
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Protocol 6 - Average runtime for each vendor is {vendorTimePredictRanking.ToCustomTimeSpanFormat(true)}" });

                #endregion

                #region Measure AUC

                List<(int, bool)> itemsThatCanBeOffered = new List<(int, bool)>();
                foreach (var item in vendorItems)
                {
                    if (trainingUserItemMatrix[selectedUser, item] == 0)
                    {
                        if (testingUserItemMatrix[selectedUser, item] == 0)
                        {
                            itemsThatCanBeOffered.Add((item, false));
                        }
                        else
                        {
                            itemsThatCanBeOffered.Add((item, true));
                        }
                    }
                }

                int[] zeroItems = itemsThatCanBeOffered.Where(o => o.Item2 == false).Select(o => o.Item1).ToArray();
                int[] oneItems = itemsThatCanBeOffered.Where(o => o.Item2 == true).Select(o => o.Item1).ToArray();
                double rankingAUC = 0;
                double ratingAUC = 0;
                double YP_AUC = 0;

                int counter = 0;
                foreach (var zeroItem in zeroItems)
                {
                    int zeroItemRating = Protocols.GetPredictedRatingNoCrypto(trainingUserItemMatrix, selectedUser, zeroItem, q, similarityMatrix);
                    int YPZeroItemRating = Protocols.GetPredictedRatingNoCrypto(YPUserItemMatrix, selectedUser, zeroItem, q, YPsimilarityMatrix);
                    object myLock = new object();

                    Parallel.ForEach(oneItems, (oneItem) =>
                    {
                        int oneItemRating = Protocols.GetPredictedRatingNoCrypto(trainingUserItemMatrix, selectedUser, oneItem, q, similarityMatrix);
                        int YPOneItemRating = Protocols.GetPredictedRatingNoCrypto(YPUserItemMatrix, selectedUser, oneItem, q, YPsimilarityMatrix);

                        lock (myLock)
                        {
                            if (!mostRecommendedItems.Contains(zeroItem) || mostRecommendedItems.Contains(oneItem))
                            {
                                rankingAUC++;
                            }

                            if (oneItemRating >= zeroItemRating)
                            {
                                ratingAUC++;
                            }

                            if (YPOneItemRating >= YPZeroItemRating)
                            {
                                YP_AUC++;
                            }
                        }
                    });

                    Console.WriteLine($"AUC counter - {counter++} / {zeroItems.Length}");
                }

                rankingAUC = rankingAUC / (zeroItems.Length * oneItems.Length);
                ratingAUC = ratingAUC / (zeroItems.Length * oneItems.Length);
                YP_AUC = YP_AUC / (zeroItems.Length * oneItems.Length);

                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"The AUC for ranking is - {rankingAUC}" });
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"The AUC for rating is - {ratingAUC}" });
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"The AUC for YP rating is - {YP_AUC}" });

            }
            #endregion
        }
    }
}
