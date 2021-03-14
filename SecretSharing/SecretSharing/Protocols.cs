﻿using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SecretSharing
{
    public static class Protocols
    {
        public static readonly double PRIME = 2147483647;

        public static readonly double Q = 100;

        public static readonly Random random = new Random();

        public static int[,] ReadUserItemMatrix(string path)
        {
            List<UserRating> ratings = new List<UserRating>();

            if (path.EndsWith("csv"))
            {

                var csvTable = new DataTable();
                using (var csvReader = new CsvReader(new StreamReader(File.OpenRead(path)), true))
                {
                    csvTable.Load(csvReader);
                }
                for (int i = 0; i < csvTable.Rows.Count; i++)
                {
                    ratings.Add(new UserRating
                    {
                        UserId = int.Parse(csvTable.Rows[i][0].ToString()),
                        ItemId = int.Parse(csvTable.Rows[i][1].ToString()),
                        Rating = int.Parse(csvTable.Rows[i][2].ToString())
                    });
                }
            }
            else if (path.EndsWith("dat"))
            {
                string[] lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    string[] splitted = line.Split();
                    ratings.Add(new UserRating
                    {
                        UserId = int.Parse(splitted[0]),
                        ItemId = int.Parse(splitted[1]),
                        Rating = int.Parse(splitted[2])
                    });
                }
            }

            int N = ratings.Max(o => o.UserId);
            int M = ratings.Max(o => o.ItemId);

            var userItemMatrix = new int[N, M];
            foreach (var rating in ratings)
            {
                userItemMatrix[rating.UserId - 1, rating.ItemId - 1] = rating.Rating;
            }

            return userItemMatrix;
        }

        public static List<int?[,]> SplitUserItemMatrixBetweenVendors(int[,] userItemMatrix, int numOfVendors)
        {
            int N = userItemMatrix.GetLength(0);
            int M = userItemMatrix.GetLength(1);
            List<int?[,]> R_ks = new List<int?[,]>();
            for (int i = 0; i < numOfVendors; i++)
            {
                R_ks.Add(new int?[N, M]);
            }

            Random r = new Random();
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    int selectedVendor = r.Next(0, numOfVendors);
                    R_ks[selectedVendor][i, j] = userItemMatrix[i, j];
                }
            }

            // Add overlaps
            for (int vendorIndex = 0; vendorIndex < numOfVendors; vendorIndex++)
            {
                for (int lap = 0; lap < 50; lap++)
                {
                    int i = r.Next(0, N);
                    int j = r.Next(0, M);
                    if (R_ks[vendorIndex][i, j] == null)
                        R_ks[vendorIndex][i, j] = 0;
                }
            }
            return R_ks;
        }

        public static double[,] CalcSimilarityMatrix(List<int?[,]> R_ks, int numOfMediators, string directoryName)
        {
            int N = R_ks[0].GetLength(0);
            int M = R_ks[0].GetLength(1);

            List<double[,]> RShares = new List<double[,]>();
            List<double[,]> SqRShares = new List<double[,]>();
            List<double[,]> XiRShares = new List<double[,]>();

            for (int mediatorIndex = 0; mediatorIndex < numOfMediators; mediatorIndex++)
            {
                RShares.Add(new double[N, M]);
                SqRShares.Add(new double[N, M]);
                XiRShares.Add(new double[N, M]);
            }

            foreach (var R_k in R_ks)
            {
                int?[,] sq = R_k.CalcSq();
                int?[,] xi = R_k.CalcXi();

                var RkShares = ShamirSecretSharingMatrix(R_k, numOfMediators);
                var SqRkShares = ShamirSecretSharingMatrix(sq, numOfMediators);
                var XiRkShares = ShamirSecretSharingMatrix(xi, numOfMediators);

                for (int mediatorIndex = 0; mediatorIndex < numOfMediators; mediatorIndex++)
                {
                    RShares[mediatorIndex].AddShare(RkShares[mediatorIndex]);

                    SqRShares[mediatorIndex].AddShare(SqRkShares[mediatorIndex]);

                    XiRShares[mediatorIndex].AddShare(XiRkShares[mediatorIndex]);
                }
            }

            double[,] similarityMatrix = new double[M, M];


            for (int i = 0; i < M; i++)
            {
                List<double[]> clShares = new List<double[]>();
                foreach (var share in RShares)
                {
                    clShares.Add(share.GetVerticalVector(i));
                }

                List<double[]> SqClShares = new List<double[]>();
                foreach (var share in SqRShares)
                {
                    SqClShares.Add(share.GetVerticalVector(i));
                }

                List<double[]> XiClShares = new List<double[]>();
                foreach (var share in XiRShares)
                {
                    XiClShares.Add(share.GetVerticalVector(i));
                }

                Parallel.For(i + 1, M, (j) =>
                {
                    List<double[]> cmShares = new List<double[]>();
                    foreach (var share in RShares)
                    {
                        cmShares.Add(share.GetVerticalVector(j));
                    }

                    List<double[]> SqCmShares = new List<double[]>();
                    foreach (var share in SqRShares)
                    {
                        SqCmShares.Add(share.GetVerticalVector(j));
                    }

                    List<double[]> XiCmShares = new List<double[]>();
                    foreach (var share in XiRShares)
                    {
                        XiCmShares.Add(share.GetVerticalVector(j));
                    }

                    double similarityScore = 0;

                    double z1 = ScalarProductShares(clShares, cmShares);

                    double z2 = ScalarProductShares(SqClShares, XiCmShares);

                    double z3 = ScalarProductShares(XiClShares, SqCmShares);

                    var mult = z2 * z3;
                    if (mult != 0)
                    {
                        similarityScore = z1 / (Math.Sqrt(mult));
                    }

                    if (similarityScore != 0)
                    {
                        //Convert to integer value
                        similarityMatrix[i, j] = Math.Floor((similarityScore * Q) + 0.5);
                        similarityMatrix[j, i] = Math.Floor((similarityScore * Q) + 0.5);
                    }

                });
            }

            return similarityMatrix;
        }

        public static List<double[]> AllOrNothingSecretSharing(double[] vector, int numOfShares)
        {
            List<double[]> shares = new List<double[]>();
            double[] sharesSum = new double[vector.Length];

            for (int i = 0; i < numOfShares - 1; i++)
            {
                double[] share = new double[vector.Length];
                for (int j = 0; j < vector.Length; j++)
                {
                    share[j] = random.Next(1, (int)PRIME);
                    sharesSum[j] += share[j];
                }
                shares.Add(share);
            }

            double[] lastShare = new double[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                lastShare[i] = vector[i] - (sharesSum[i] % PRIME);
                if (lastShare[i] < 0)
                {
                    lastShare[i] += PRIME;
                }
            }
            shares.Add(lastShare);
            return shares;
        }

        public static double[] ReconstructAllOrNothingSecret(List<double[]> shares)
        {
            double[] secret = new double[shares[0].Length];
            for (int i = 0; i < shares[0].Length; i++)
            {
                for (int j = 0; j < shares.Count; j++)
                {
                    secret[i] += shares[j][i];
                }

                secret[i] = secret[i] % PRIME;
            }
            return secret;
        }

        public static double ScalarProductVectors(double[] vector1, double[] vector2)
        {
            int length = vector1.Length;
            double sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum += vector1[i] * vector2[i];
            }
            return sum % PRIME;
        }

        /// <summary>
        /// Shamir's secret sharing specific for the case of 3/5/7 shares 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="numOfSharesToMake"></param>
        /// <param name="numOfSharesForRecovery"></param>
        /// <returns>Shares array for each of the mediators</returns>
        public static List<double[]> ShamirSecretSharing(double[] vector, int numOfShares)
        {
            var shares = new List<double[]>();
            for (int i = 0; i < numOfShares; i++)
            {
                shares.Add(new double[vector.Length]);
            }

            if (numOfShares == 3)
            {
                int shareCount = 0;
                foreach (int entry in vector)
                {
                    double a = random.Next(2, int.MaxValue);
                    double lastY = entry;

                    for (int i = 0; i < 3; i++)
                    {
                        var y = lastY + a;
                        lastY = y;

                        shares[i][shareCount] = y;
                    }
                    shareCount++;
                }
            }

            else if (numOfShares == 5)
            {
                int shareCount = 0;
                foreach (double entry in vector)
                {
                    double a = random.Next(2, int.MaxValue);
                    double b = random.Next(2, int.MaxValue);

                    double B = a + b;
                    double B2 = b + b;
                    double B3 = B + B2;
                    double B5 = B3 + B2;
                    double B7 = B5 + B2;
                    double B9 = B7 + B2;
                    double lastY = 0;

                    for (int i = 0; i < 5; i++)
                    {
                        double y = 0;
                        switch (i)
                        {
                            case 0:
                                y = entry + B;
                                break;
                            case 1:
                                y = lastY + B3;
                                break;
                            case 2:
                                y = lastY + B5;
                                break;
                            case 3:
                                y = lastY + B7;
                                break;
                            case 4:
                                y = lastY + B9;
                                break;
                        }
                        lastY = y;

                        shares[i][shareCount] = y;
                    }
                    shareCount++;
                }
            }

            else if (numOfShares == 7)
            {
                int shareCount = 0;
                foreach (double entry in vector)
                {
                    double a = random.Next(2, int.MaxValue);
                    double b = random.Next(2, int.MaxValue);
                    double c = random.Next(2, int.MaxValue);

                    double B = a + b + c;
                    double B1 = 6 * c;
                    double B2 = 2 * b;
                    double B3 = B + B2 + B1;
                    double B5 = B3 + B2 + 2 * B1;
                    double B7 = B5 + B2 + 3 * B1;
                    double B9 = B7 + B2 + 4 * B1;
                    double B11 = B9 + B2 + 5 * B1;
                    double B13 = B11 + B2 + 6 * B1;

                    double lastY = 0;

                    for (int i = 0; i < 7; i++)
                    {
                        double y = 0;
                        switch (i)
                        {
                            case 0:
                                y = entry + B;
                                break;
                            case 1:
                                y = lastY + B3;
                                break;
                            case 2:
                                y = lastY + B5;
                                break;
                            case 3:
                                y = lastY + B7;
                                break;
                            case 4:
                                y = lastY + B9;
                                break;
                            case 5:
                                y = lastY + B11;
                                break;
                            case 6:
                                y = lastY + B13;
                                break;
                        }
                        lastY = y;

                        shares[i][shareCount] = y;
                    }
                    shareCount++;
                }
            }

            return shares;
        }

        /// <summary>
        /// Shamir's secret sharing specific for the case of 3/5/7 shares 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="numOfSharesToMake"></param>
        /// <param name="numOfSharesForRecovery"></param>
        /// <returns>Shares matrix for each of the mediators</returns>
        public static List<double[,]> ShamirSecretSharingMatrix(int?[,] matrix, int numOfShares)
        {
            var shares = new List<double[,]>();
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(0);

            for (int i = 0; i < numOfShares; i++)
            {
                shares.Add(new double[N, M]);
            }

            if (numOfShares == 3)
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        double a = random.Next(2, int.MaxValue);
                        double lastY = matrix[i, j] ?? 0;

                        for (int shareIndex = 0; shareIndex < 3; shareIndex++)
                        {
                            var y = lastY + a;
                            lastY = y;

                            shares[shareIndex][i, j] = y;
                        }
                    }
                }
            }

            else if (numOfShares == 5)
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        double entry = matrix[i, j] ?? 0;

                        double a = random.Next(2, int.MaxValue);
                        double b = random.Next(2, int.MaxValue);

                        double B = a + b;
                        double B2 = b + b;
                        double B3 = B + B2;
                        double B5 = B3 + B2;
                        double B7 = B5 + B2;
                        double B9 = B7 + B2;
                        double lastY = 0;

                        for (int shareIndex = 0; shareIndex < 5; shareIndex++)
                        {
                            double y = 0;
                            switch (shareIndex)
                            {
                                case 0:
                                    y = entry + B;
                                    break;
                                case 1:
                                    y = lastY + B3;
                                    break;
                                case 2:
                                    y = lastY + B5;
                                    break;
                                case 3:
                                    y = lastY + B7;
                                    break;
                                case 4:
                                    y = lastY + B9;
                                    break;
                            }
                            lastY = y;

                            shares[shareIndex][i, j] = y;
                        }
                    }
                }
            }

            else if (numOfShares == 7)
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        double entry = matrix[i, j] ?? 0;

                        double a = random.Next(2, int.MaxValue);
                        double b = random.Next(2, int.MaxValue);
                        double c = random.Next(2, int.MaxValue);

                        double B = a + b + c;
                        double B1 = 6 * c;
                        double B2 = 2 * b;
                        double B3 = B + B2 + B1;
                        double B5 = B3 + B2 + 2 * B1;
                        double B7 = B5 + B2 + 3 * B1;
                        double B9 = B7 + B2 + 4 * B1;
                        double B11 = B9 + B2 + 5 * B1;
                        double B13 = B11 + B2 + 6 * B1;

                        double lastY = 0;

                        for (int shareIndex = 0; shareIndex < 7; shareIndex++)
                        {
                            double y = 0;
                            switch (shareIndex)
                            {
                                case 0:
                                    y = entry + B;
                                    break;
                                case 1:
                                    y = lastY + B3;
                                    break;
                                case 2:
                                    y = lastY + B5;
                                    break;
                                case 3:
                                    y = lastY + B7;
                                    break;
                                case 4:
                                    y = lastY + B9;
                                    break;
                                case 5:
                                    y = lastY + B11;
                                    break;
                                case 6:
                                    y = lastY + B13;
                                    break;
                            }
                            lastY = y;

                            shares[shareIndex][i, j] = y;
                        }
                    }
                }
            }

            return shares;
        }

        /// <summary>
        /// Place the vendor average rating instead of some of the zero cells
        /// </summary>
        /// <param name="trainingUserItemMatrix"></param>
        /// <param name="vendorsItemsIndecis"></param>
        /// <param name="percentOfFakeCells"></param>
        /// <returns></returns>
        public static int[,] GetYPUserItemMatrix(int[,] trainingUserItemMatrix, List<int[]> vendorsItemsIndecis, int percentOfFakeCells)
        {
            int N = trainingUserItemMatrix.GetLength(0); // users
            int M = trainingUserItemMatrix.GetLength(1); // items

            int[,] YPPredictedRatings = new int[N, M];

            foreach (var vendorItemsIndecis in vendorsItemsIndecis)
            {
                int[,] vendorUserItemMatrix = trainingUserItemMatrix.GetVerticalSubMatrix(vendorItemsIndecis);
                double ratingsSum = 0;
                double ratingsCount = 0;
                double emeptyCells = 0;
                int users = vendorUserItemMatrix.GetLength(0);
                int items = vendorUserItemMatrix.GetLength(1);

                for (int i = 0; i < users; i++)
                {
                    for (int j = 0; j < items; j++)
                    {
                        int rating = vendorUserItemMatrix[i, j];
                        if (rating != 0)
                        {
                            ratingsSum += rating;
                            ratingsCount++;
                        }
                        else
                        {
                            emeptyCells++;
                        }
                    }
                }


                int averageRating = (int)(ratingsSum / ratingsCount);
                int numOfCellsToPlaceFakeRating = (int)((emeptyCells / 100) * percentOfFakeCells);
                var vendorMatrixWithFakeCells = vendorUserItemMatrix.PlaceFakeCells(numOfCellsToPlaceFakeRating, averageRating);

                YPPredictedRatings.CopySubMatrix(vendorMatrixWithFakeCells, vendorItemsIndecis[0]);
            }
            return YPPredictedRatings;
        }

        /// <summary>
        /// Protocol 1 - Computing the scalar product between two vectors that are shared between D mediators
        /// </summary>
        /// <param name="clShares"></param>
        /// <param name="cmShares"></param>
        /// <returns></returns>
        public static double ScalarProductShares(List<double[]> clShares, List<double[]> cmShares)
        {
            BigInteger[][] multShares = new BigInteger[clShares.Count][];

            for (int indexCount = 0; indexCount < clShares.Count; indexCount++)
            {
                BigInteger[] multCoordinates = new BigInteger[clShares[0].Length];
                for (int shareCount = 0; shareCount < clShares[0].Length; shareCount++)
                {
                    var newY = (BigInteger)clShares[indexCount][shareCount] * (BigInteger)cmShares[indexCount][shareCount];

                    multCoordinates[shareCount] = newY;
                }
                multShares[indexCount] = multCoordinates;
            }

            List<BigInteger> coordinates = new List<BigInteger>();
            for (int i = 0; i < clShares.Count; i++)
            {
                BigInteger sumY = 0;
                for (int j = 0; j < clShares[0].Length; j++)
                {
                    sumY += multShares[i][j];
                }
                coordinates.Add(sumY);
            }

            double secret = 0;
            if (coordinates.Count == 3)
            {
                secret = (double)((3 * (coordinates[0] - coordinates[1]) + coordinates[2]) % (BigInteger)PRIME);
            }
            else if (coordinates.Count == 5)
            {
                secret = (double)(((5 * (coordinates[0] - coordinates[3])) - (10 * (coordinates[1] - coordinates[2])) + coordinates[4]) % (BigInteger)PRIME);
            }
            else if (coordinates.Count == 7)
            {
                secret = (double)(((7 * (coordinates[0] - coordinates[5])) + (21 * (coordinates[4] - coordinates[1])) + (35 * (coordinates[2] - coordinates[3])) + coordinates[6]) % (BigInteger)PRIME);
            }

            if (secret < 0)
            {
                secret += PRIME;
            }

            return secret;
        }

        /// <summary>
        /// Calculates the similarity matrix based on Protocol 2, and convert it to big integers
        /// </summary>
        /// <param name="userItemMatrix">The user-item matrix</param>
        /// <param name="numOfShares">D</param>
        /// <returns></returns>
        public static double[,] CalcSimilarityMatrixOld(int[,] userItemMatrix, int numOfShares, int[] itemsVendorIndex, string directoryName = "")
        {
            int k = itemsVendorIndex.Last() + 1;
            int items = userItemMatrix.GetLength(1);
            double[,] similarityMatrix = new double[items, items];
            List<double[]>[] clSharesArray = new List<double[]>[items];
            List<double[]>[] clPowSharesArray = new List<double[]>[items];
            List<double[]>[] xiClSharesArray = new List<double[]>[items];

            long totalVendorsDuration = 0;
            long totalMediatorsDuration = 0;

            #region Phase 1 - The vendors calculating the similarity matrix for items from the same vendor

            Console.WriteLine($"Phase 1 started");

            var vendorPhase1Watch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < items; i++)
            {
                var similarityScoreWatch = System.Diagnostics.Stopwatch.StartNew();

                Parallel.For(i + 1, items, (j) =>
                {

                    // If both items belong to the same vendor - do the computation without crypto
                    if (itemsVendorIndex[i] == itemsVendorIndex[j])
                    {
                        double similarityScore = 0;

                        similarityScore = CalcSimilarityScoreNoCrypto(userItemMatrix, i, j);

                        if (similarityScore != 0)
                        {
                            //Convert to integer value
                            similarityMatrix[i, j] = Math.Floor((similarityScore * Q) + 0.5);
                            similarityMatrix[j, i] = Math.Floor((similarityScore * Q) + 0.5);
                        }
                    }

                });

                similarityScoreWatch.Stop();
                var similarityScoreTime = new TimeSpan(0, 0, 0, 0, (int)similarityScoreWatch.ElapsedMilliseconds);
                Console.WriteLine($"{i}/{items} done in {similarityScoreTime.ToCustomTimeSpanFormat()}");
            }

            vendorPhase1Watch.Stop();
            totalVendorsDuration += vendorPhase1Watch.ElapsedMilliseconds;

            #endregion

            #region Phase 2 - Creating the shares

            Console.WriteLine($"Phase 2 started");

            int count = 0;

            var vendorPhase2Watch = System.Diagnostics.Stopwatch.StartNew();

            Parallel.For(0, items, (i) =>
            {
                double[] cl = userItemMatrix.GetVerticalVector(i).Select(o => (double)o).ToArray();
                var clShares = ShamirSecretSharing(cl, numOfShares);
                clSharesArray[i] = clShares;

                double[] clPow = Array.ConvertAll(cl, x => x * x);
                var clPowShares = ShamirSecretSharing(clPow, numOfShares);
                clPowSharesArray[i] = clPowShares;

                double[] xiCl = Array.ConvertAll(cl, x => x == 0 ? (double)0 : 1);
                var xiClShares = ShamirSecretSharing(xiCl, numOfShares);
                xiClSharesArray[i] = xiClShares;

                Console.WriteLine($"{count}/{items}");
                count++;
            });

            vendorPhase2Watch.Stop();
            var phase2Time = new TimeSpan(0, 0, 0, 0, (int)vendorPhase2Watch.ElapsedMilliseconds);
            Console.WriteLine($"Phase 2 - done in {phase2Time.ToCustomTimeSpanFormat()}");
            totalVendorsDuration += vendorPhase2Watch.ElapsedMilliseconds;

            #endregion

            #region Phase 3 - The mediators calculating the similarity matrix using the shares

            Console.WriteLine($"Phase 3 started");

            var mediatorPhase3Watch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < items; i++)
            {
                var similarityScoreWatch = System.Diagnostics.Stopwatch.StartNew();

                Parallel.For(i + 1, items, (j) =>
                 {
                     // If the items belong to different vendors - do the computation using the shares
                     if (itemsVendorIndex[i] != itemsVendorIndex[j])
                     {
                         double similarityScore = 0;

                         double z1 = ScalarProductShares(clSharesArray[i], clSharesArray[j]);

                         double z2 = ScalarProductShares(clPowSharesArray[i], xiClSharesArray[j]);

                         double z3 = ScalarProductShares(xiClSharesArray[i], clPowSharesArray[j]);

                         var mult = z2 * z3;
                         if (mult != 0)
                         {
                             similarityScore = z1 / (Math.Sqrt(mult));
                         }
                         if (similarityScore != 0)
                         {
                             //Convert to integer value
                             similarityMatrix[i, j] = Math.Floor((similarityScore * Q) + 0.5);
                             similarityMatrix[j, i] = Math.Floor((similarityScore * Q) + 0.5);
                         }
                     }
                 });

                similarityScoreWatch.Stop();
                var similarityScoreTime = new TimeSpan(0, 0, 0, 0, (int)similarityScoreWatch.ElapsedMilliseconds);
                Console.WriteLine($"{i}/{items} done in {similarityScoreTime.ToCustomTimeSpanFormat()}");
            }

            mediatorPhase3Watch.Stop();
            totalMediatorsDuration += mediatorPhase3Watch.ElapsedMilliseconds;

            #endregion

            var vendorsTime = new TimeSpan(0, 0, 0, 0, (int)totalVendorsDuration);
            Console.WriteLine($"Protocol 2 - Average runtime for each vendor is {(vendorsTime / k).ToCustomTimeSpanFormat()}");
            File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Protocol 2 - Average runtime for each vendor is {(vendorsTime / k).ToCustomTimeSpanFormat()}" });

            var mediatorsTime = new TimeSpan(0, 0, 0, 0, (int)totalMediatorsDuration);
            Console.WriteLine($"Protocol 2 - Average runtime for each mediators is {mediatorsTime.ToCustomTimeSpanFormat()}");
            File.AppendAllLines(directoryName + "Times.txt", new string[1] { $"Protocol 2 - Average runtime for each mediators is {mediatorsTime.ToCustomTimeSpanFormat()}" });

            return similarityMatrix;
        }

        public static double CalcSimilarityScoreNoCrypto(int[,] userItemMatrix, int i, int j)
        {
            double[] cl = userItemMatrix.GetVerticalVector(i).Select(o => (double)o).ToArray();
            double[] clPow = Array.ConvertAll(cl, x => x * x);
            double[] xiCl = Array.ConvertAll(cl, x => x == 0 ? (double)0 : 1);

            double[] cm = userItemMatrix.GetVerticalVector(j).Select(o => (double)o).ToArray();

            double z1 = (double)ScalarProductVectors(cl, cm);

            double[] xiCm = Array.ConvertAll(cm, x => x == 0 ? (double)0 : 1);

            double z2 = (double)ScalarProductVectors(clPow, xiCm);

            double[] cmPow = Array.ConvertAll(cm, x => x * x);

            double z3 = (double)ScalarProductVectors(xiCl, cmPow);

            double similarityScore = 0;
            if (z2 * z3 != 0)
            {
                similarityScore = z1 / (Math.Sqrt(z2 * z3));
            }
            return similarityScore;
        }

        public static double[,] CalcSimilarityMatrixNoCrypto(int[,] userItemMatrix)
        {
            int items = userItemMatrix.GetLength(1);
            double[,] similarityMatrix = new double[items, items];

            for (int i = 0; i < items; i++)
            {
                double[] cl = userItemMatrix.GetVerticalVector(i).Select(o => (double)o).ToArray();
                double[] clPow = Array.ConvertAll(cl, x => x * x);
                double[] xiCl = Array.ConvertAll(cl, x => x == 0 ? (double)0 : 1);

                Parallel.For(i + 1, items, (j) =>
                {
                    double[] cm = userItemMatrix.GetVerticalVector(j).Select(o => (double)o).ToArray();

                    double z1 = (double)ScalarProductVectors(cl, cm);

                    double[] xiCm = Array.ConvertAll(cm, x => x == 0 ? (double)0 : 1);

                    double z2 = (double)ScalarProductVectors(clPow, xiCm);

                    double[] cmPow = Array.ConvertAll(cm, x => x * x);

                    double z3 = (double)ScalarProductVectors(xiCl, cmPow);

                    double similarityScore = 0;
                    if (z2 * z3 != 0)
                    {
                        similarityScore = z1 / (Math.Sqrt(z2 * z3));
                    }

                    //Convert to integer value
                    similarityMatrix[i, j] = Math.Floor((similarityScore * Q) + 0.5);
                    similarityMatrix[j, i] = Math.Floor((similarityScore * Q) + 0.5);
                });
            }
            return similarityMatrix;
        }

        /// <summary>
        /// Secret sharing the adjusted user-item matrix - protocol 3
        /// </summary>
        /// <param name="userItemMatrix"></param>
        /// <returns>Array of marices - each matrix has M vectors of shares</returns>
        public static List<double[]>[] SecretShareRHat(int[,] userItemMatrix, int numOfShares)
        {
            // Array of marices - each matrix has M vectors of shares
            // For example RHatShares[2][1] is the second column of r-hat of the third mediator
            List<double[]>[] RHatShares = new List<double[]>[numOfShares];
            double[,] adjustedUserItemMatrix = userItemMatrix.GetAdjustedUserItemMatrix(Q);
            for (int i = 0; i < adjustedUserItemMatrix.GetLength(1); i++)
            {
                var adjustedRatings = adjustedUserItemMatrix.GetVerticalVector(i);
                var shares = AllOrNothingSecretSharing(adjustedRatings, numOfShares);
                int shareCount = 0;
                foreach (var share in shares)
                {
                    if (RHatShares[shareCount] == null)
                    {
                        RHatShares[shareCount] = new List<double[]>();
                    }

                    RHatShares[shareCount].Add(share);
                    shareCount++;
                }
            }
            return RHatShares;
        }

        /// <summary>
        /// This is a special reconstruction because Rhat was not an integer valued and got converted, also it could be negative.
        /// </summary>
        /// <param name="shares"></param>
        /// <returns></returns>
        public static double[] ReconstructRHatSecret(List<double[]> shares)
        {
            double[] secret = new double[shares[0].Length];
            double[] secretAsDouble = new double[shares[0].Length];

            for (int i = 0; i < shares[0].Length; i++)
            {
                for (int j = 0; j < shares.Count; j++)
                {
                    secret[i] += shares[j][i];
                }

                secret[i] = secret[i] % PRIME;

                // if it was negative
                if (secret[i] + 10 * Q > PRIME)
                {
                    secret[i] = secret[i] - PRIME;
                }
                secretAsDouble[i] = Math.Round((secret[i] / Q), 3);
            }
            return secretAsDouble;
        }

        /// <summary>
        /// Secret sharing the xi of the user-item matrix - protocol 3
        /// </summary>
        /// <param name="userItemMatrix"></param>
        /// <returns>Array of marices - each matrix has M vectors of shares</returns>
        public static List<double[]>[] SecretShareXiR(int[,] userItemMatrix, int numOfShares)
        {
            List<double[]>[] xiRShares = new List<double[]>[numOfShares];

            int[,] xiR = userItemMatrix.GetXi();
            for (int i = 0; i < xiR.GetLength(1); i++)
            {
                var xiRatings = xiR.GetVerticalVector(i).Select(o => (double)o).ToArray();
                var shares = AllOrNothingSecretSharing(xiRatings, numOfShares);
                int shareCount = 0;
                foreach (var share in shares)
                {
                    if (xiRShares[shareCount] == null)
                    {
                        xiRShares[shareCount] = new List<double[]>();
                    }

                    xiRShares[shareCount].Add(share);
                    shareCount++;
                }
            }
            return xiRShares;
        }

        /// <summary>
        /// Obfuscate the values of the shares according to Protocol 4
        /// </summary>
        /// <param name="xiRShares"></param>
        /// <returns></returns>
        public static List<double[]>[] ObfuscateShares(List<double[]>[] shares)
        {
            int numOfShares = shares.Length;
            int users = shares[0][0].Length;
            List<double[]>[] obfescatedShares = new List<double[]>[numOfShares];

            for (int i = 0; i < shares[0].Count; i++)
            {
                double[] vector = new double[users];
                for (int j = 0; j < users; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < numOfShares; k++)
                    {
                        sum += shares[k][i][j];
                    }
                    vector[j] = sum;
                }
                var AONshares = AllOrNothingSecretSharing(vector, numOfShares);
                int shareCount = 0;
                foreach (var AONshare in AONshares)
                {
                    if (obfescatedShares[shareCount] == null)
                    {
                        obfescatedShares[shareCount] = new List<double[]>();
                    }

                    obfescatedShares[shareCount].Add(AONshare);
                    shareCount++;
                }
            }
            return obfescatedShares;
        }

        /// <summary>
        /// Get sm as in protocol 5 step 3
        /// </summary>
        /// <param name="similarityMatrix"></param>
        /// <param name="m"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static double[] GetSimilarityVectorForTopSimilarItemsToM(double[,] similarityMatrix, int m, int q, bool isPositivesOnly)
        {
            int vectorLength = similarityMatrix.GetLength(0);
            Tuple<double, int>[] similarityScoreAndIndex = new Tuple<double, int>[vectorLength];

            for (int i = 0; i < vectorLength; i++)
            {
                similarityScoreAndIndex[i] = new Tuple<double, int>(similarityMatrix[m, i], i);
            }

            Array.Sort(similarityScoreAndIndex, new ScoreAndIndexComparer());
            similarityScoreAndIndex = similarityScoreAndIndex.Take(q).ToArray();
            double[] sm = new double[vectorLength];

            foreach (var item in similarityScoreAndIndex)
            {
                if (isPositivesOnly)
                {
                    if (item.Item1 > 0)
                    {
                        sm[item.Item2] = item.Item1;
                    }
                }
                else
                {
                    sm[item.Item2] = item.Item1;
                }
            }

            return sm;
        }

        public static int GetPredictedRatingNoCrypto(int[,] userItemMatrix, int n, int m, int q, double[,] similarityMatrixNoCrypto = null)
        {

            var averageRatings = userItemMatrix.GetAverageRatings();
            var averageRating = averageRatings[m];

            if (similarityMatrixNoCrypto == null)
            {
                similarityMatrixNoCrypto = CalcSimilarityMatrixNoCrypto(userItemMatrix);
            }

            var smNoCrypto = GetSimilarityVectorForTopSimilarItemsToM(similarityMatrixNoCrypto, m, q, true);

            double[] RHatn = new double[userItemMatrix.GetLength(1)];
            Parallel.For(0, userItemMatrix.GetLength(1), (i) =>
            {
                int xiR = userItemMatrix[n, i] == 0 ? 0 : 1;
                if (xiR == 1)
                {
                    var currentItemAverageRating = averageRatings[i];
                    RHatn[i] = (userItemMatrix[n, i] - currentItemAverageRating);
                }
            });

            var mult1 = ScalarProductVectors(smNoCrypto, RHatn);
            var xiRn = userItemMatrix.GetHorizontalVector(n).GetXi().Select(o => (double)o).ToArray();

            var mult2 = ScalarProductVectors(smNoCrypto, xiRn);

            double change = 0;
            if (mult1 != 0 && mult2 != 0)
            {
                change = (double)mult1 / (double)mult2;
            }

            int predictedRating = (int)Math.Round(averageRating + change, 0);

            if (predictedRating > 5)
            {
                predictedRating = 5;
            }
            else if (predictedRating < 0)
            {
                predictedRating = 0;
            }

            return predictedRating;
        }

        public static double[] FindShamirCoefficients(int D)
        {
            double[] Cs = Enumerable.Repeat((double)1, D).ToArray();
            double[] Xs = Enumerable.Range(1, D).Select(o => (double)o * D).ToArray();
            for (int i = 0; i < D; i++)
            {
                for (int j = 0; j < D; j++)
                {
                    if (i != j)
                    {
                        Cs[i] *= (Xs[i] - Xs[j]);
                    }
                }
            }

            double xMult = Xs.Aggregate((a, x) => a * x);

            double[] coeffs = new double[D];
            for (int i = 0; i < D; i++)
            {
                coeffs[i] = xMult / (Xs[i] * Cs[i]);
            }

            return coeffs;
        }
    }

    public class ScoreAndIndexComparer : IComparer<Tuple<double, int>>
    {
        public int Compare(Tuple<double, int> x, Tuple<double, int> y)
        {
            return x.Item1.CompareTo(y.Item1) * -1;
        }
    }

}
