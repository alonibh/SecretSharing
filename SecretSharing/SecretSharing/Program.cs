using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecretSharing
{
    class Program
    {
        static void Main()
        {
            string[] datasets = new string[4] { "100K", "1M", "10M", "20M" }; //test,
            // k - vendors
            // D - mediators
            int q = 80; // num of similar items
            int h = 10; // num of most recomended items to take

            foreach (var dataset in datasets)
            {
                MeasureOfflinePart1(dataset, k: 1, D: 3, q, h);
                MeasureOfflinePart1(dataset, k: 1, D: 5, q, h);
                MeasureOfflinePart1(dataset, k: 1, D: 7, q, h);
                MeasureOfflinePart1(dataset, k: 1, D: 9, q, h);
            }

            //RunTestOldVersion(dataset, k: 1, D: 9, q, h, 5);
            //RunTest(dataset, k: 2, D: 3, q, h);
        }

        static void MeasureOfflinePart1(string dataset, int k, int D, int q, int h)
        {
            Console.WriteLine($"Dataset - {dataset}, k={k}, D={D} Started");

            string directoryName = $"k-{k}, D-{D}, Dataset-{dataset}/";
            string fileName = directoryName + "MeasureOfflinePart1.txt";
            Directory.CreateDirectory(directoryName);
            File.AppendAllLines(fileName, new string[1] { $"Database - {dataset}, k={k} D={D}" });

            #region Settings

            sbyte[,] userItemMatrix = Protocols.ReadUserItemMatrix($"ratings-distict-{dataset}.dat");

            int N = userItemMatrix.GetLength(0); // users
            int M = userItemMatrix.GetLength(1); // items

            if (D != 3 && D != 5 && D != 7 && D != 9)
            {
                throw new Exception("Number of mediators must be 3, 5, 7 or 9");
            }

            #endregion

            #region Computing the similarity matrix and the shares (Protocol 1+2)

            Console.WriteLine("MeasureOfflinePart1");

            Protocols.SimulateSingleVendorWorkInComputingSimilarityMatrix(userItemMatrix, D, fileName);

            ushort[,] someRShare = null;
            ushort[,] someXiRShare = null;
            ushort[,] someSqRShare = null;
            if (dataset != "20M")
            {
                someRShare = Protocols.CreateRandomMatrixShare(N, M);
                someXiRShare = Protocols.CreateRandomMatrixShare(N, M);
                someSqRShare = Protocols.CreateRandomMatrixShare(N, M);
            }

            Protocols.SimulateSingleMediatorWorkInComputingSimilarityMatrix(N, M, someRShare, someXiRShare, someSqRShare, D, fileName);

            someRShare = null;
            someXiRShare = null;
            someSqRShare = null;

            #endregion

            #region MeasureOfflinePart2

            Console.WriteLine("MeasureOfflinePart2");

            fileName = directoryName + "MeasureOfflinePart2.txt";

            Protocols.SimulateSingleMediatorWorkInComputingOfflinePart2(D, userItemMatrix, q, fileName);

            #endregion

            #region MeasureOnlinePredictRating

            Console.WriteLine("MeasureOnlinePredictRating");

            fileName = directoryName + "MeasureOnlinePredictRating.txt";

            Protocols.SimulateSingleMediatorWorkInOnlinePredictRating(M, D, fileName);

            #endregion

            #region MeasureOnlinePredictRanking

            Console.WriteLine("MeasureOnlinePredictRanking");

            fileName = directoryName + "MeasureOnlinePredictRanking.txt";

            Protocols.SimulateSingleMediatorWorkInOnlinePredictRanking(M, q, fileName);

            Protocols.SimulateSingleVendorWorkInOnlinePredictRanking(M, D, h, fileName);

            #endregion

        }

        static void RunTest(string dataset, int k, int D, int q, int h)
        {
            #region Settings

            sbyte[,] userItemMatrix = Protocols.ReadUserItemMatrix($"ratings-distict-{dataset}.dat");

            int N = userItemMatrix.GetLength(0); // users
            int M = userItemMatrix.GetLength(1); // items

            if (D != 3 && D != 5 && D != 7 && D != 9)
            {
                throw new Exception("Number of mediators must be 3, 5, 7 or 9");
            }

            string directoryName = $"k-{k}, D-{D}, Dataset-{dataset}/";
            Directory.CreateDirectory(directoryName);

            #endregion

            #region Computing the similarity matrix and the shares (Protocol 1+2)

            sbyte[,] trainingUserItemMatrix = null;
            sbyte[,] testingUserItemMatrix = null;
            double[,] similarityMatrix = null;
            List<uint[,]> RShares = null;
            List<uint[,]> SqRShare = null;
            List<uint[,]> XiRShares = null;
            List<sbyte[,]> R_ks;


            var sets = userItemMatrix.SplitToTrainingAndTesting();

            trainingUserItemMatrix = sets.Item1;
            testingUserItemMatrix = sets.Item2;

            R_ks = Protocols.SplitUserItemMatrixBetweenVendors(trainingUserItemMatrix, k);

            SimilarityMatrixAndShares smas = Protocols.CalcSimilarityMatrix(R_ks, D);
            similarityMatrix = smas.SimilarityMatrix;
            RShares = smas.RShares;
            SqRShare = smas.SqRShares;
            XiRShares = smas.XiRShares;

            #endregion

            #region Predict rating

            var entriesToCompare = testingUserItemMatrix.GetNonZeroEntries();

            double[] averageRatings = new double[M];
            for (int itemIndex = 0; itemIndex < M; itemIndex++)
            {
                averageRatings[itemIndex] = Protocols.ComputeAverageRating(RShares, XiRShares, itemIndex);
            }

            foreach (var entry in entriesToCompare)
            {
                int n = entry.Item1;
                int m = entry.Item2;

                var sm = Protocols.GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, m, q, true);

                List<uint[]> RnShares = new List<uint[]>();
                for (int shareCount = 0; shareCount < RShares.Count; shareCount++)
                {
                    RnShares.Add(RShares[shareCount].GetHorizontalVector(n));
                }
                double Unm = Protocols.MultiplySharesByVector(RnShares, sm);

                List<uint[]> XiRnShares = new List<uint[]>();
                for (int shareCount = 0; shareCount < RShares.Count; shareCount++)
                {
                    XiRnShares.Add(XiRShares[shareCount].GetHorizontalVector(n));
                }

                double Wnm = Protocols.MultiplySharesByVector(XiRnShares, sm);

                double Vnm = Protocols.CalcVnm(XiRnShares, sm, averageRatings);

                double predictedRating = averageRatings[m];
                if (Wnm != 0)
                {
                    var addon = ((Unm - Vnm) / Wnm);
                    predictedRating += addon;
                }

                var expected = Protocols.GetPredictedRatingNoCrypto(trainingUserItemMatrix, n, m, q);
                predictedRating = (int)Math.Round(predictedRating, 0);

                if (predictedRating > 5)
                {
                    predictedRating = 5;
                }
                else if (predictedRating < 0)
                {
                    predictedRating = 0;
                }

                double diff = Math.Abs(userItemMatrix[n, m] - predictedRating);
            }

            #endregion

            #region Predict ranking (Protocol 3)

            int selectedVendor = 0;
            int selectedUser = R_ks[selectedVendor].GetFirstNotNullEntry().Item1;

            int[] offeredItemIndecis = Protocols.GetItemsOfferedByVendor(R_ks[selectedVendor]);

            List<ulong[]> Xs = new List<ulong[]>();
            for (int mediatorIndex = 0; mediatorIndex < D; mediatorIndex++)
            {
                var xiRShareVector = XiRShares[mediatorIndex].GetHorizontalVector(selectedUser);
                var Xd = Protocols.GenerateXd(q, offeredItemIndecis, similarityMatrix, xiRShareVector);
                Xs.Add(Xd);
            }

            List<double> scores = new List<double>();
            foreach (var itemIndex in offeredItemIndecis)
            {
                var score = Protocols.ReconstructShamirSecret(Xs.Select(o => o[itemIndex]).ToList());
                scores.Add(score);
            }


            List<Tuple<double, int>> valueAndIndex = new List<Tuple<double, int>>();
            foreach (var index in offeredItemIndecis)
            {
                valueAndIndex.Add(new Tuple<double, int>(scores[index], index));
            }

            var valueAndIndexArray = valueAndIndex.ToArray();
            Array.Sort(valueAndIndexArray, new ScoreAndIndexComparer());
            int[] mostRecommendedItems = valueAndIndexArray.Take(h).Select(o => o.Item2).ToArray();

            #endregion
        }

        static void RunTestOldVersion(string dataset, int k, int D, int q, int h, int percentOfFakeCells)
        {
            #region Settings

            bool loadFromFile = false;
            bool saveToFile = false;
            bool predictRating = false;
            bool predictRanking = false;

            sbyte[,] userItemMatrix = Protocols.ReadUserItemMatrix($"ratings-distict-{dataset}.dat");

            int N = userItemMatrix.GetLength(0); // users
            int M = userItemMatrix.GetLength(1); // items

            if (D != 3 && D != 5 && D != 7 && D != 9)
            {
                throw new Exception("Number of mediators must be 3, 5, 7 or 9");
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

            sbyte[,] trainingUserItemMatrix = null;
            sbyte[,] testingUserItemMatrix = null;
            double[,] similarityMatrix = null;
            sbyte[,] YPUserItemMatrix = null;
            double[,] YPsimilarityMatrix = null;

            if (loadFromFile)
            {
                trainingUserItemMatrix = Extensions.LoadSbyteMatrixFromFile(directoryName + "trainingUserItemMatrix.txt");
                testingUserItemMatrix = Extensions.LoadSbyteMatrixFromFile(directoryName + "testingUserItemMatrix.txt");
                similarityMatrix = Extensions.LoadDoubleMatrixFromFile(directoryName + "similarityMatrix.txt");
                YPsimilarityMatrix = Extensions.LoadDoubleMatrixFromFile(directoryName + "YPsimilarityMatrix.txt");
                YPUserItemMatrix = Extensions.LoadSbyteMatrixFromFile(directoryName + "YPUserItemMatrix.txt");
            }
            else
            {
                File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Database - {dataset}, k={k} D={D}" });

                var sets = userItemMatrix.SplitToTrainingAndTesting();

                trainingUserItemMatrix = sets.Item1;
                testingUserItemMatrix = sets.Item2;

                similarityMatrix = Protocols.CalcSimilarityMatrixOld(trainingUserItemMatrix, D, itemsVendorIndex, directoryName);

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

            var secretSharingWatch = Stopwatch.StartNew();

            RHatShares = Protocols.SecretShareRHat(trainingUserItemMatrix, D);
            XiRShares = Protocols.SecretShareXiR(trainingUserItemMatrix, D);

            secretSharingWatch.Stop();
            var secretSharingTime = new TimeSpan(0, 0, 0, 0, (int)secretSharingWatch.ElapsedMilliseconds);
            Console.WriteLine($"Protocol 3 - Average runtime for each vendor is {(secretSharingTime / k).ToCustomTimeSpanFormat()}");
            File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Protocol 3 - Average runtime for each vendor is {(secretSharingTime / k).ToCustomTimeSpanFormat()}" });

            #endregion

            #region Obfuscate the shares of xiR (Protocol 4)

            List<double[]>[] obfuscatedXiRShares = null;

            var obfuscationWatch = Stopwatch.StartNew();

            obfuscatedXiRShares = Protocols.ObfuscateSharesOld(XiRShares);

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

        static void GenerateDatasets()
        {
            List<Tuple<string, int, int>> datasets = new List<Tuple<string, int, int>>
            {
                new Tuple<string, int, int>("WN0",100000,10000 ),
                new Tuple<string, int, int>("WN1",250000,10000 ),
                new Tuple<string, int, int>("WN2",500000,10000 ),
                new Tuple<string, int, int>("WN3",1000000,10000 ),
                new Tuple<string, int, int>("WN4",2000000,10000 ),

                new Tuple<string, int, int>("WM0",100000,10000 ),
                new Tuple<string, int, int>("WM1",100000,25000 ),
                new Tuple<string, int, int>("WM2",100000,50000 ),
                new Tuple<string, int, int>("WM3",100000,100000 ),
                new Tuple<string, int, int>("WM4",100000,200000 )

            };
            foreach (var dataset in datasets)
            {
                int N = dataset.Item2;
                int M = dataset.Item3;
                string fileName = dataset.Item1;

                int A = 0;
                Random rand = new Random();

                List<string> lines = new List<string>();
                int counter = 0;
                int selectedRandomNumber = rand.Next(0, 50);

                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        if (selectedRandomNumber == counter % 50)
                        {
                            lines.Add($"{i} {j} {rand.Next(1, 6)}");
                            A++;
                        }
                        if (counter == 50)
                        {
                            selectedRandomNumber = rand.Next(0, 50);
                            counter = 0;
                        }
                        counter++;
                    }
                    if (i % 1000 == 0)
                    {
                        File.AppendAllLines($"{fileName}.dat", lines);
                        lines.Clear();
                        Console.WriteLine(i);
                    }
                }
                lines.Add($"{N - 1} {M - 1} {rand.Next(1, 6)}");
                File.AppendAllLines($"{fileName}.dat", lines);
                Console.WriteLine(A);
            }
        }
    }
}
