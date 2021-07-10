using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecretSharing
{
    public static class Protocols
    {
        public static readonly ulong PRIME = 2147483647;

        public static readonly int Q = 100;

        public static readonly Random random = new Random();

        public static void MakeDistinct(string path)
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
                        Rating = (sbyte)(double.Parse(csvTable.Rows[i][2].ToString()) * 2)
                    });
                }
                string[] lines = new string[ratings.Count];
                for (int i = 0; i < csvTable.Rows.Count; i++)
                {
                    lines[i] = $"{ratings[i].UserId} {ratings[i].ItemId} {ratings[i].Rating}";
                }
                File.AppendAllLines(@"ratings.dat", lines);
            }

            if (path.EndsWith("dat"))
            {
                string[] linesToRead = File.ReadAllLines(path);
                foreach (var line in linesToRead)
                {
                    string[] splitted = line.Split();
                    ratings.Add(new UserRating
                    {
                        UserId = int.Parse(splitted[0]),
                        ItemId = int.Parse(splitted[1]),
                        Rating = sbyte.Parse(splitted[2])
                    });
                }
                int N = ratings.Max(o => o.UserId);
                int M = ratings.Max(o => o.ItemId);

                List<UserRating>[] ratingsArray = new List<UserRating>[M];
                foreach (var rating in ratings)
                {
                    if (ratingsArray[rating.ItemId - 1] != null)
                        ratingsArray[rating.ItemId - 1].Add(rating);
                    else
                    {
                        ratingsArray[rating.ItemId - 1] = new List<UserRating>() { rating };
                    }
                }

                for (int i = M - 1; i > 0; i--)
                {
                    if (ratingsArray[i] != null)
                    {
                        var firstEmeptyIndex = ratingsArray.TakeWhile(o => o != null).Count();
                        if (firstEmeptyIndex > i)
                        {
                            ratingsArray = ratingsArray.TakeWhile(o => o != null).ToArray();
                            break;
                        }

                        ratingsArray[firstEmeptyIndex] = new List<UserRating>();
                        foreach (var rating in ratingsArray[i])
                        {
                            ratingsArray[firstEmeptyIndex].Add(new UserRating { UserId = rating.UserId, ItemId = firstEmeptyIndex + 1, Rating = rating.Rating });
                        }
                        ratingsArray[i] = null;
                    }
                }

                var linesToWrite = new List<string>();
                for (int i = 0; i < ratingsArray.Length; i++)
                {
                    foreach (var rating in ratingsArray[i])
                    {
                        linesToWrite.Add($"{rating.UserId} {rating.ItemId} {rating.Rating}");
                    }
                }
                File.AppendAllLines("ratings-distict.dat", linesToWrite);
            }
        }

        public static sbyte[,] ReadUserItemMatrix(string path)
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
                        Rating = sbyte.Parse(csvTable.Rows[i][2].ToString())
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
                        Rating = sbyte.Parse(splitted[2])
                    });
                }
            }

            int N = ratings.Max(o => o.UserId);
            int M = ratings.Max(o => o.ItemId);

            var userItemMatrix = new sbyte[N, M];
            foreach (var rating in ratings)
            {
                userItemMatrix[rating.UserId - 1, rating.ItemId - 1] = rating.Rating;
            }
            return userItemMatrix;
        }

        public static List<sbyte[,]> SplitUserItemMatrixBetweenVendors(sbyte[,] userItemMatrix, int numOfVendors)
        {
            int N = userItemMatrix.GetLength(0);
            int M = userItemMatrix.GetLength(1);
            List<sbyte[,]> R_ks = new List<sbyte[,]>();

            for (int vendorIndex = 0; vendorIndex < numOfVendors; vendorIndex++)
            {
                var emepty = new sbyte[N, M];
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        emepty[i, j] = -1;
                    }
                }

                R_ks.Add(emepty);
            }


            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    int selectedVendor = random.Next(0, numOfVendors);
                    R_ks[selectedVendor][i, j] = userItemMatrix[i, j];
                }
            }

            // Add overlaps
            for (int vendorIndex = 0; vendorIndex < numOfVendors; vendorIndex++)
            {
                for (int lap = 0; lap < 50; lap++)
                {
                    int i = random.Next(0, N);
                    int j = random.Next(0, M);
                    if (R_ks[vendorIndex][i, j] == -1)
                    {
                        R_ks[vendorIndex][i, j] = 0;
                    }
                }
            }
            return R_ks;
        }

        public static ushort[,] CreateRandomMatrixShare(int n, int m)
        {
            ushort[,] matrix = new ushort[n, m];
            int maxRangeForRandom = 65535;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    matrix[i, j] = (ushort)random.Next(2, maxRangeForRandom);
                }
            }
            return matrix;
        }

        public static List<ushort[]> CreateRandomShares(int n, int numOfShares)
        {
            List<ushort[]> shares = new List<ushort[]>();
            int maxRangeForRandom = 65535;
            for (int i = 0; i < numOfShares; i++)
            {
                ushort[] vector = new ushort[n];
                for (int j = 0; j < n; j++)
                {
                    vector[j] = (ushort)random.Next(2, maxRangeForRandom);
                }
                shares.Add(vector);
            }
            return shares;
        }

        public static void SimulateSingleMediatorWorkInComputingOfflinePart2(int numOfShares, sbyte[,] userItemMatrix, int q, string fileName)
        {
            int N = userItemMatrix.GetLength(0);
            int M = userItemMatrix.GetLength(1);

            List<ushort[]>[] ratingVectorsSharesArray = new List<ushort[]>[100];
            List<ushort[]>[] XiRatingVectorsSharesArray = new List<ushort[]>[100];

            for (int i = 0; i < 100; i++)
            {
                var ratingVectorShares = CreateRandomShares(N, numOfShares);
                ratingVectorsSharesArray[i] = ratingVectorShares;

                var XiRatingVectorShares = CreateRandomShares(N, numOfShares);
                XiRatingVectorsSharesArray[i] = XiRatingVectorShares;
            }

            List<ulong>[] Xds = new List<ulong>[100];
            List<ulong>[] Yds = new List<ulong>[100];

            var XWatch = Stopwatch.StartNew();

            for (int itemIndex = 0; itemIndex < 100; itemIndex++)
            {
                foreach (var share in ratingVectorsSharesArray[itemIndex])
                {
                    if (Xds[itemIndex] == null)
                    {
                        Xds[itemIndex] = new List<ulong>();
                    }
                    Xds[itemIndex].Add(share.Sum());
                }

                foreach (var share in XiRatingVectorsSharesArray[itemIndex])
                {
                    if (Yds[itemIndex] == null)
                    {
                        Yds[itemIndex] = new List<ulong>();
                    }
                    Yds[itemIndex].Add(share.Sum());
                }
            }

            XWatch.Stop();
            var XTime = new TimeSpan(0, 0, 0, 0, (int)XWatch.ElapsedMilliseconds);
            XTime = (XTime * M) / (100 * numOfShares);
            File.AppendAllLines(fileName, new string[1] { $"X time is: {XTime.ToCustomTimeSpanFormat()}" });


            var YWatch = Stopwatch.StartNew();

            double[] averageRatings = new double[100];

            for (int itemIndex = 0; itemIndex < 100; itemIndex++)
            {
                List<ulong> xCoordinates = new List<ulong>();

                foreach (var xd in Xds[itemIndex])
                {
                    xCoordinates.Add(xd);
                }
                var x = ReconstructShamirSecret(xCoordinates);

                List<ulong> yCoordinates = new List<ulong>();
                foreach (var yd in Yds[itemIndex])
                {
                    yCoordinates.Add(yd);
                }
                var y = ReconstructShamirSecret(yCoordinates);

                if (y != 0)
                {
                    averageRatings[itemIndex] = x / y;
                }
            }

            YWatch.Stop();
            var YTime = new TimeSpan(0, 0, 0, 0, (int)YWatch.ElapsedMilliseconds);
            YTime = (YTime * M) / (100 * numOfShares);
            File.AppendAllLines(fileName, new string[1] { $"Y time is: {YTime.ToCustomTimeSpanFormat()}" });

            var similarityMatrix = CreateRandomMatrix(userItemMatrix.GetLength(1));
            averageRatings = new double[M];

            for (int i = 0; i < M; i++)
            {
                averageRatings[i] = random.NextDouble() * 5;
            }

            var ZWatch = Stopwatch.StartNew();

            for (int m = 0; m < M; m++)
            {
                var sm = GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, m, q, true);
                var sTagM = GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, m, q, false);
                var temp = sTagM;
                ulong[] cl = new ulong[averageRatings.Length];
                for (int i = 0; i < cl.Length; i++)
                {
                    cl[i] = (ulong)Math.Round(Q * sm[i] * averageRatings[i], 0);
                }
            }

            ZWatch.Stop();
            var ZTime = new TimeSpan(0, 0, 0, 0, (int)ZWatch.ElapsedMilliseconds);
            ZTime = ZTime / numOfShares;
            File.AppendAllLines(fileName, new string[1] { $"Z time is: {ZTime.ToCustomTimeSpanFormat()}" });

            File.AppendAllLines(fileName, new string[1] { $"Total time for each mediator computing offline part 2: {(XTime + YTime + ZTime).ToCustomTimeSpanFormat()}" });
        }

        public static void SimulateSingleVendorWorkInComputingSimilarityMatrix(sbyte[,] Rk, int numOfShares, string fileName)
        {
            var AWatch = Stopwatch.StartNew();

            sbyte[,] sq = Rk.CalcSq();
            sbyte[,] xi = Rk.CalcXi();

            AWatch.Stop();
            var ATime = new TimeSpan(0, 0, 0, 0, (int)AWatch.ElapsedMilliseconds);

            File.AppendAllLines(fileName, new string[1] { $"A time is: {ATime.ToCustomTimeSpanFormat()}" });

            var FDWatch = new Stopwatch();
            for (int i = 0; i < 10000000; i++)
            {
                uint scalar = (uint)random.Next(0, 2147483647);
                FDWatch.Start();
                ShamirSecretSharingNoStoring(scalar, numOfShares);
                FDWatch.Stop();
            }

            var FDTime = new TimeSpan(0, 0, 0, 0, (int)FDWatch.ElapsedMilliseconds);
            FDTime = FDTime * 3 * Rk.GetLength(0) * Rk.GetLength(1) / 10000000;
            File.AppendAllLines(fileName, new string[1] { $"F({numOfShares})*3*N*M: {FDTime.ToCustomTimeSpanFormat()}" });

            var totalVendorTime = ATime + FDTime;

            File.AppendAllLines(fileName, new string[1] { $"Total time for a single vendor computing the similarity matrix: {totalVendorTime.ToCustomTimeSpanFormat()}" });
        }

        public static void SimulateSingleMediatorWorkInComputingSimilarityMatrix(int N, int M, ushort[,] someRShare, ushort[,] someXiRShare, ushort[,] someSqRShare, int numOfShares, string fileName)
        {
            if (someRShare != null)
            {
                var lines7To9Watch = Stopwatch.StartNew();

                uint[,] RShare = new uint[N, M];
                uint[,] SqRShare = new uint[N, M];
                uint[,] XiRShare = new uint[N, M];

                RShare.AddShare(someRShare);
                SqRShare.AddShare(someSqRShare);
                XiRShare.AddShare(someXiRShare);

                lines7To9Watch.Stop();
                var lines7To9Time = new TimeSpan(0, 0, 0, 0, (int)lines7To9Watch.ElapsedMilliseconds);

                File.AppendAllLines(fileName, new string[1] { $"M1: {lines7To9Time.ToCustomTimeSpanFormat()}" });

                RShare = null;
                SqRShare = null;
                XiRShare = null;
            }

            var someCmShare = CreateRandomShares(N, numOfShares)[0];

            var someClShare = CreateRandomShares(N, numOfShares)[0];

            var M2Watch = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                Parallel.For(0, 100, (j) =>
                {
                    ulong z1 = ScalarProductVectors(someCmShare, someClShare);
                });
            }

            M2Watch.Stop();
            var K2Time = new TimeSpan(0, 0, 0, 0, (int)M2Watch.ElapsedMilliseconds);
            K2Time = (K2Time * M * (M - 1) * 3) / (10000 * 2);
            File.AppendAllLines(fileName, new string[1] { $"K2: {K2Time.ToCustomTimeSpanFormat()}" });

            List<ulong> coordinates = new List<ulong>();
            for (int i = 0; i < numOfShares; i++)
            {
                coordinates.Add((ulong)random.Next(0, 2147483647));
            }

            var GDWatch = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                Parallel.For(0, 100, (j) =>
                {
                    ulong z1 = ReconstructShamirSecret(coordinates);
                });
            }

            GDWatch.Stop();
            var K3Time = new TimeSpan(0, 0, 0, 0, (int)GDWatch.ElapsedMilliseconds);
            K3Time = (K3Time * 3 * M * (M - 1)) / (2 * 10000 * numOfShares);
            File.AppendAllLines(fileName, new string[1] { $"K3: {K3Time.ToCustomTimeSpanFormat()}" });
        }

        public static void SimulateSingleMediatorWorkInOnlinePredictRating(int numberOfItems, int numOfShares, string fileName)
        {
            List<uint[]> smVectors = new List<uint[]>();
            for (int i = 0; i < 100; i++)
            {
                var smVector = new uint[numberOfItems];
                for (int j = 0; j < numberOfItems; j++)
                {
                    smVector[j] = (uint)random.Next(0, 6);
                }
                smVectors.Add(smVector);
            }

            List<ushort[]>[] sharesArray = new List<ushort[]>[100];

            for (int i = 0; i < 100; i++)
            {
                var smVector = smVectors[i];

                var smShares = CreateRandomShares(smVector.Length, numOfShares);
                sharesArray[i] = smShares;
            }

            double[] vector = new double[numberOfItems];
            for (int i = 0; i < numberOfItems; i++)
            {
                vector[i] = random.Next(0, 6);
            }

            var XWatch = new Stopwatch();
            var YWatch = new Stopwatch();

            foreach (var shares in sharesArray)
            {
                List<ulong> xds = new List<ulong>();

                XWatch.Start();

                foreach (var share in shares)
                {
                    double xd = 0;
                    for (int itemCounter = 0; itemCounter < vector.Length; itemCounter++)
                    {
                        xd += share[itemCounter] * vector[itemCounter];
                    }
                    xd = Math.Round(xd, 0);
                    xds.Add((ulong)xd);
                }

                XWatch.Stop();

                YWatch.Start();

                var secret = ReconstructShamirSecret(xds);
                var temp = secret;

                YWatch.Stop();
            }

            var XTime = new TimeSpan(0, 0, 0, 0, (int)XWatch.ElapsedMilliseconds);
            XTime = XTime / 100;
            File.AppendAllLines(fileName, new string[1] { $"X time is: {XTime.ToCustomTimeSpanFormat()}" });

            var YTime = new TimeSpan(0, 0, 0, 0, (int)YWatch.ElapsedMilliseconds);
            YTime = YTime / 100;
            File.AppendAllLines(fileName, new string[1] { $"Y time is: {YTime.ToCustomTimeSpanFormat()}" });

            File.AppendAllLines(fileName, new string[1] { $"Total time for each mediator computing online predict rating: {(3 * (XTime + YTime)).ToCustomTimeSpanFormat()}" });
        }

        public static void SimulateSingleMediatorWorkInOnlinePredictRanking(int M, int q, string fileName)
        {
            var XWatch = Stopwatch.StartNew();

            CreatePermutation(M);

            XWatch.Stop();
            var XTime = new TimeSpan(0, 0, 0, 0, (int)XWatch.ElapsedMilliseconds);
            File.AppendAllLines(fileName, new string[1] { $"X time is: {XTime.ToCustomTimeSpanFormat()}" });

            int[] topItemsSimilarityVector = new int[M];
            for (int i = 0; i < q; i++)
            {
                topItemsSimilarityVector[i] = random.Next(0, 100);
            }

            int[] xiRVector = new int[M];
            for (int i = 0; i < M; i++)
            {
                xiRVector[i] = random.Next(0, (int)PRIME);
            }

            var YWatch = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                List<ulong> Xd = new List<ulong>();
                List<long> XdShares = new List<long>();
                List<long> oneMinusXiShares = new List<long>();
                long addon = Q * q + 1;
                for (int itemIndex = 0; itemIndex < M; itemIndex++)
                {
                    long Xdm = addon;
                    Xdm += (long)ScalarProductVectors(topItemsSimilarityVector, xiRVector);
                    XdShares.Add(Xdm);
                    oneMinusXiShares.Add(1 - xiRVector[itemIndex]);
                }

                for (int shareIndex = 0; shareIndex < XdShares.Count; shareIndex++)
                {
                    long mult = XdShares[shareIndex] * oneMinusXiShares[shareIndex];
                    var mod = ModForNegative(mult);
                    Xd.Add(mod);
                }
            }

            YWatch.Stop();
            var YTime = new TimeSpan(0, 0, 0, 0, (int)YWatch.ElapsedMilliseconds);
            YTime = YTime / 100;
            File.AppendAllLines(fileName, new string[1] { $"Y*|M| time is: {YTime.ToCustomTimeSpanFormat()}" });

            File.AppendAllLines(fileName, new string[1] { $"Total time for each mediator computing online predict ranking: {(XTime + YTime).ToCustomTimeSpanFormat()}" });
        }

        public static void SimulateSingleVendorWorkInOnlinePredictRanking(int M, int numberOfShares, int h, string fileName)
        {
            var vendorWatch = new Stopwatch();
            for (int i = 0; i < 100; i++)
            {
                List<ulong> coordinates = new List<ulong>(numberOfShares);
                for (int j = 0; j < M; j++)
                {
                    coordinates.Add((ulong)random.Next(0, (int)PRIME));
                }

                vendorWatch.Start();

                ReconstructShamirSecret(coordinates);
                coordinates.Sort();
                var top = coordinates.Take(h);
                var temp = top;

                vendorWatch.Stop();
            }

            vendorWatch.Stop();
            var vendorTime = new TimeSpan(0, 0, 0, 0, (int)vendorWatch.ElapsedMilliseconds);
            vendorTime = vendorTime / 100;

            File.AppendAllLines(fileName, new string[1] { $"Total time for a single vendor computing online predict ranking: {vendorTime.ToCustomTimeSpanFormat()}" });
        }

        public static int[] CreatePermutation(int size)
        {
            int[] array = Enumerable.Range(0, size).ToArray();

            var permutation = array.OrderBy(x => random.Next()).ToArray();

            return permutation;
        }

        public static SimilarityMatrixAndShares CalcSimilarityMatrix(List<sbyte[,]> R_ks, int numOfMediators)
        {
            var mediatorsWatch = new Stopwatch();
            var vendorWatch = new Stopwatch();

            int N = R_ks[0].GetLength(0);
            int M = R_ks[0].GetLength(1);

            List<uint[,]> RShares = new List<uint[,]>();
            List<uint[,]> SqRShares = new List<uint[,]>();
            List<uint[,]> XiRShares = new List<uint[,]>();

            for (int mediatorIndex = 0; mediatorIndex < numOfMediators; mediatorIndex++)
            {
                RShares.Add(new uint[N, M]);
                SqRShares.Add(new uint[N, M]);
                XiRShares.Add(new uint[N, M]);
            }

            Console.WriteLine("Starting creating shares");

            foreach (var R_k in R_ks)
            {
                vendorWatch.Start();

                sbyte[,] sq = R_k.CalcSq();
                sbyte[,] xi = R_k.CalcXi();

                var RkShares = ShamirSecretSharingMatrix(R_k, numOfMediators);
                var SqRkShares = ShamirSecretSharingMatrix(sq, numOfMediators);
                var XiRkShares = ShamirSecretSharingMatrix(xi, numOfMediators);

                vendorWatch.Stop();

                mediatorsWatch.Start();

                for (int mediatorIndex = 0; mediatorIndex < numOfMediators; mediatorIndex++)
                {
                    RShares[mediatorIndex].AddShare(RkShares[mediatorIndex]);
                    RShares[mediatorIndex].ApplyModulo(PRIME);

                    SqRShares[mediatorIndex].AddShare(SqRkShares[mediatorIndex]);
                    SqRShares[mediatorIndex].ApplyModulo(PRIME);

                    XiRShares[mediatorIndex].AddShare(XiRkShares[mediatorIndex]);
                    XiRShares[mediatorIndex].ApplyModulo(PRIME);
                }

                mediatorsWatch.Stop();
            }

            var creatingSharesTime = new TimeSpan(0, 0, 0, 0, (int)vendorWatch.ElapsedMilliseconds + (int)mediatorsWatch.ElapsedMilliseconds);
            Console.WriteLine($"Done creating shares in {creatingSharesTime.ToCustomTimeSpanFormat()}");

            double[,] similarityMatrix = new double[M, M];
            List<uint[]>[] clSharesArray = new List<uint[]>[M];
            List<uint[]>[] SqClSharesArray = new List<uint[]>[M];
            List<uint[]>[] XiClSharesArray = new List<uint[]>[M];

            mediatorsWatch.Start();

            Console.WriteLine("Starting organizing shares for each mediator");


            Parallel.For(0, M, (i) =>
            {
                List<uint[]> clShares = new List<uint[]>();
                foreach (var share in RShares)
                {
                    clShares.Add(share.GetVerticalVector(i));
                }
                clSharesArray[i] = clShares;

                List<uint[]> SqClShares = new List<uint[]>();
                foreach (var share in SqRShares)
                {
                    SqClShares.Add(share.GetVerticalVector(i));
                }
                SqClSharesArray[i] = SqClShares;

                List<uint[]> XiClShares = new List<uint[]>();
                foreach (var share in XiRShares)
                {
                    XiClShares.Add(share.GetVerticalVector(i));
                }
                XiClSharesArray[i] = XiClShares;
            });

            Console.WriteLine("Done organizing shares for each mediator");

            for (int i = 0; i < M; i++)
            {
                var similarityWatch = Stopwatch.StartNew();

                Parallel.For(i + 1, M, (j) =>
                {
                    double similarityScore = 0;

                    ulong z1 = ScalarProductShares(clSharesArray[i], clSharesArray[j]);

                    ulong z2 = ScalarProductShares(SqClSharesArray[i], XiClSharesArray[j]);

                    ulong z3 = ScalarProductShares(XiClSharesArray[i], SqClSharesArray[j]);

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

                similarityWatch.Stop();
                var similarityScoreTime = new TimeSpan(0, 0, 0, 0, (int)similarityWatch.ElapsedMilliseconds);
                Console.WriteLine($"{i}/{M} done in {similarityScoreTime.ToCustomTimeSpanFormat()}");
            }

            mediatorsWatch.Stop();

            var eachMediatorTime = new TimeSpan(0, 0, 0, 0, (int)mediatorsWatch.ElapsedMilliseconds);
            var vendorTime = new TimeSpan(0, 0, 0, 0, (int)vendorWatch.ElapsedMilliseconds);

            return new SimilarityMatrixAndShares
            {
                SimilarityMatrix = similarityMatrix,
                RShares = RShares,
                SqRShares = SqRShares,
                XiRShares = XiRShares,
                EachMediatorTime = eachMediatorTime,
                VendorTime = vendorTime
            };
        }

        /// <summary>
        /// Calculates the similarity matrix based on Protocol 1, and convert it to big integers
        /// </summary>
        /// <param name="userItemMatrix">The user-item matrix</param>
        /// <param name="numOfShares">D</param>
        /// <returns></returns>
        public static double[,] CalcSimilarityMatrixOld(sbyte[,] userItemMatrix, int numOfShares, int[] itemsVendorIndex, string directoryName = "")
        {
            int k = itemsVendorIndex.Last() + 1;
            int items = userItemMatrix.GetLength(1);
            double[,] similarityMatrix = new double[items, items];
            List<uint[]>[] clSharesArray = new List<uint[]>[items];
            List<uint[]>[] clPowSharesArray = new List<uint[]>[items];
            List<uint[]>[] xiClSharesArray = new List<uint[]>[items];

            long totalVendorsDuration = 0;
            long totalMediatorsDuration = 0;

            #region Phase 1 - The vendors calculating the similarity matrix for items from the same vendor

            Console.WriteLine($"Phase 1 started");

            var vendorPhase1Watch = Stopwatch.StartNew();

            for (int i = 0; i < items; i++)
            {
                var similarityScoreWatch = Stopwatch.StartNew();

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

            var vendorPhase2Watch = Stopwatch.StartNew();

            for (int i = 0; i < items; i++)
            {
                sbyte[] cl = userItemMatrix.GetVerticalVector(i).ToArray();
                var clShares = ShamirSecretSharing(cl, numOfShares);
                clSharesArray[i] = clShares;

                var clPow = Array.ConvertAll(cl, x => x * x);
                var clPowShares = ShamirSecretSharing(clPow, numOfShares);
                clPowSharesArray[i] = clPowShares;

                var xiCl = Array.ConvertAll(cl, x => x == 0 ? 0 : 1);
                var xiClShares = ShamirSecretSharing(xiCl, numOfShares);
                xiClSharesArray[i] = xiClShares;

                Console.WriteLine($"{count}/{items}");
                count++;
            }

            vendorPhase2Watch.Stop();
            var phase2Time = new TimeSpan(0, 0, 0, 0, (int)vendorPhase2Watch.ElapsedMilliseconds);
            Console.WriteLine($"Phase 2 - done in {phase2Time.ToCustomTimeSpanFormat()}");
            totalVendorsDuration += vendorPhase2Watch.ElapsedMilliseconds;

            #endregion

            #region Phase 3 - The mediators calculating the similarity matrix using the shares

            Console.WriteLine($"Phase 3 started");

            var mediatorPhase3Watch = Stopwatch.StartNew();

            for (int i = 0; i < items; i++)
            {
                var similarityScoreWatch = Stopwatch.StartNew();
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

        /// <summary>
        /// Shamir's secret sharing specific for the case of 3/5/7/9 shares
        /// This function should not run in parallel
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="numOfSharesToMake"></param>
        /// <param name="numOfSharesForRecovery"></param>
        /// <returns>Shares matrix for each of the mediators</returns>
        public static List<uint[,]> ShamirSecretSharingMatrix(sbyte[,] matrix, int numOfShares)
        {
            var shares = new List<uint[,]>();
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(1);
            int maxRangeForRandom = 65535;

            for (int i = 0; i < numOfShares; i++)
            {
                shares.Add(new uint[N, M]);
            }

            if (numOfShares == 3)
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        uint a = (uint)random.Next(2, maxRangeForRandom);

                        uint lastY = 0;
                        if (matrix[i, j] != -1)
                            lastY = (uint)matrix[i, j];

                        for (int shareIndex = 0; shareIndex < 3; shareIndex++)
                        {
                            uint y = lastY + a;
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
                        uint entry = 0;
                        if (matrix[i, j] != -1)
                            entry = (uint)matrix[i, j];

                        uint a = (uint)random.Next(2, maxRangeForRandom);
                        uint b = (uint)random.Next(2, maxRangeForRandom);

                        uint B = a + b;
                        uint B2 = b + b;
                        uint B3 = B + B2;
                        uint B5 = B3 + B2;
                        uint B7 = B5 + B2;
                        uint B9 = B7 + B2;
                        uint lastY = 0;

                        for (int shareIndex = 0; shareIndex < 5; shareIndex++)
                        {
                            uint y = 0;
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
                        uint entry = 0;
                        if (matrix[i, j] != -1)
                            entry = (uint)matrix[i, j];

                        uint a = (uint)random.Next(2, maxRangeForRandom);
                        uint b = (uint)random.Next(2, maxRangeForRandom);
                        uint c = (uint)random.Next(2, maxRangeForRandom);

                        uint B = a + b + c;
                        uint B1 = 6 * c;
                        uint B2 = 2 * b;
                        uint B3 = B + B2 + B1;
                        uint B5 = B3 + B2 + 2 * B1;
                        uint B7 = B5 + B2 + 3 * B1;
                        uint B9 = B7 + B2 + 4 * B1;
                        uint B11 = B9 + B2 + 5 * B1;
                        uint B13 = B11 + B2 + 6 * B1;

                        uint lastY = 0;

                        for (int shareIndex = 0; shareIndex < 7; shareIndex++)
                        {
                            uint y = 0;
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

            else if (numOfShares == 9)
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        uint entry = 0;
                        if (matrix[i, j] != -1)
                            entry = (uint)matrix[i, j];
                        uint a = (uint)random.Next(2, maxRangeForRandom);
                        uint b = (uint)random.Next(2, maxRangeForRandom);
                        uint c = (uint)random.Next(2, maxRangeForRandom);
                        uint d = (uint)random.Next(2, maxRangeForRandom);

                        uint B = a + b + c + d;
                        uint B1 = 2 * d;
                        uint B2 = 6 * c;
                        uint B3 = 2 * b;
                        uint B5 = B + B3 + B2 + 7 * B1;
                        uint B7 = B5 + B3 + 2 * B2 + 25 * B1;
                        uint B9 = B7 + B3 + 3 * B2 + 55 * B1;
                        uint B11 = B9 + B3 + 4 * B2 + 97 * B1;
                        uint B13 = B11 + B3 + 5 * B2 + 151 * B1;
                        uint B15 = B13 + B3 + 6 * B2 + 217 * B1;
                        uint B17 = B15 + B3 + 7 * B2 + 295 * B1;
                        uint B19 = B17 + B3 + 8 * B2 + 385 * B1;

                        uint lastY = 0;

                        for (int shareIndex = 0; shareIndex < 9; shareIndex++)
                        {
                            uint y = 0;
                            switch (shareIndex)
                            {
                                case 0:
                                    y = entry + B;
                                    break;
                                case 1:
                                    y = lastY + B5;
                                    break;
                                case 2:
                                    y = lastY + B7;
                                    break;
                                case 3:
                                    y = lastY + B9;
                                    break;
                                case 4:
                                    y = lastY + B11;
                                    break;
                                case 5:
                                    y = lastY + B13;
                                    break;
                                case 6:
                                    y = lastY + B15;
                                    break;
                                case 7:
                                    y = lastY + B17;
                                    break;
                                case 8:
                                    y = lastY + B19;
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

        public static void ShamirSecretSharingMatrixNoStoring(sbyte[,] matrix, int numOfShares)
        {
            int N = matrix.GetLength(0);
            int M = matrix.GetLength(1);
            int maxRangeForRandom = 65535;

            if (numOfShares == 3)
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        uint a = (uint)random.Next(2, maxRangeForRandom);

                        uint lastY = 0;
                        if (matrix[i, j] != -1)
                            lastY = (uint)matrix[i, j];

                        for (int shareIndex = 0; shareIndex < 3; shareIndex++)
                        {
                            uint y = lastY + a;
                            lastY = y;
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
                        uint entry = 0;
                        if (matrix[i, j] != -1)
                            entry = (uint)matrix[i, j];

                        uint a = (uint)random.Next(2, maxRangeForRandom);
                        uint b = (uint)random.Next(2, maxRangeForRandom);

                        uint B = a + b;
                        uint B2 = b + b;
                        uint B3 = B + B2;
                        uint B5 = B3 + B2;
                        uint B7 = B5 + B2;
                        uint B9 = B7 + B2;
                        uint lastY = 0;

                        for (int shareIndex = 0; shareIndex < 5; shareIndex++)
                        {
                            uint y = 0;
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
                        uint entry = 0;
                        if (matrix[i, j] != -1)
                            entry = (uint)matrix[i, j];

                        uint a = (uint)random.Next(2, maxRangeForRandom);
                        uint b = (uint)random.Next(2, maxRangeForRandom);
                        uint c = (uint)random.Next(2, maxRangeForRandom);

                        uint B = a + b + c;
                        uint B1 = 6 * c;
                        uint B2 = 2 * b;
                        uint B3 = B + B2 + B1;
                        uint B5 = B3 + B2 + 2 * B1;
                        uint B7 = B5 + B2 + 3 * B1;
                        uint B9 = B7 + B2 + 4 * B1;
                        uint B11 = B9 + B2 + 5 * B1;
                        uint B13 = B11 + B2 + 6 * B1;

                        uint lastY = 0;

                        for (int shareIndex = 0; shareIndex < 7; shareIndex++)
                        {
                            uint y = 0;
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
                        }
                    }
                }
            }

            else if (numOfShares == 9)
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        uint entry = 0;
                        if (matrix[i, j] != -1)
                            entry = (uint)matrix[i, j];
                        uint a = (uint)random.Next(2, maxRangeForRandom);
                        uint b = (uint)random.Next(2, maxRangeForRandom);
                        uint c = (uint)random.Next(2, maxRangeForRandom);
                        uint d = (uint)random.Next(2, maxRangeForRandom);

                        uint B = a + b + c + d;
                        uint B1 = 2 * d;
                        uint B2 = 6 * c;
                        uint B3 = 2 * b;
                        uint B5 = B + B3 + B2 + 7 * B1;
                        uint B7 = B5 + B3 + 2 * B2 + 25 * B1;
                        uint B9 = B7 + B3 + 3 * B2 + 55 * B1;
                        uint B11 = B9 + B3 + 4 * B2 + 97 * B1;
                        uint B13 = B11 + B3 + 5 * B2 + 151 * B1;
                        uint B15 = B13 + B3 + 6 * B2 + 217 * B1;
                        uint B17 = B15 + B3 + 7 * B2 + 295 * B1;
                        uint B19 = B17 + B3 + 8 * B2 + 385 * B1;

                        uint lastY = 0;

                        for (int shareIndex = 0; shareIndex < 9; shareIndex++)
                        {
                            uint y = 0;
                            switch (shareIndex)
                            {
                                case 0:
                                    y = entry + B;
                                    break;
                                case 1:
                                    y = lastY + B5;
                                    break;
                                case 2:
                                    y = lastY + B7;
                                    break;
                                case 3:
                                    y = lastY + B9;
                                    break;
                                case 4:
                                    y = lastY + B11;
                                    break;
                                case 5:
                                    y = lastY + B13;
                                    break;
                                case 6:
                                    y = lastY + B15;
                                    break;
                                case 7:
                                    y = lastY + B17;
                                    break;
                                case 8:
                                    y = lastY + B19;
                                    break;
                            }
                            lastY = y;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Shamir's secret sharing specific for the case of 3/5/7/9 shares 
        /// This function should not run in parallel
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="numOfSharesToMake"></param>
        /// <param name="numOfSharesForRecovery"></param>
        /// <returns>Shares array for each of the mediators</returns>
        public static List<uint[]> ShamirSecretSharing(sbyte[] vector, int numOfShares)
        {
            int maxRangeForRandom = 65535;
            var shares = new List<uint[]>();
            for (int i = 0; i < numOfShares; i++)
            {
                shares.Add(new uint[vector.Length]);
            }

            if (numOfShares == 3)
            {
                int shareCount = 0;
                foreach (int entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint lastY = (uint)entry;

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
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b;
                    uint B2 = b + b;
                    uint B3 = B + B2;
                    uint B5 = B3 + B2;
                    uint B7 = B5 + B2;
                    uint B9 = B7 + B2;
                    uint lastY = 0;

                    for (int i = 0; i < 5; i++)
                    {
                        uint y = 0;
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
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);
                    uint c = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b + c;
                    uint B1 = 6 * c;
                    uint B2 = 2 * b;
                    uint B3 = B + B2 + B1;
                    uint B5 = B3 + B2 + 2 * B1;
                    uint B7 = B5 + B2 + 3 * B1;
                    uint B9 = B7 + B2 + 4 * B1;
                    uint B11 = B9 + B2 + 5 * B1;
                    uint B13 = B11 + B2 + 6 * B1;

                    uint lastY = 0;

                    for (int i = 0; i < 7; i++)
                    {
                        uint y = 0;
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

            else if (numOfShares == 9)
            {
                int shareCount = 0;
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);
                    uint c = (uint)random.Next(2, maxRangeForRandom);
                    uint d = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b + c + d;
                    uint B1 = 2 * d;
                    uint B2 = 6 * c;
                    uint B3 = 2 * b;
                    uint B5 = B + B3 + B2 + 7 * B1;
                    uint B7 = B5 + B3 + 2 * B2 + 25 * B1;
                    uint B9 = B7 + B3 + 3 * B2 + 55 * B1;
                    uint B11 = B9 + B3 + 4 * B2 + 97 * B1;
                    uint B13 = B11 + B3 + 5 * B2 + 151 * B1;
                    uint B15 = B13 + B3 + 6 * B2 + 217 * B1;
                    uint B17 = B15 + B3 + 7 * B2 + 295 * B1;
                    uint B19 = B17 + B3 + 8 * B2 + 385 * B1;

                    uint lastY = 0;

                    for (int i = 0; i < 9; i++)
                    {
                        uint y = 0;
                        switch (i)
                        {
                            case 0:
                                y = entry + B;
                                break;
                            case 1:
                                y = lastY + B5;
                                break;
                            case 2:
                                y = lastY + B7;
                                break;
                            case 3:
                                y = lastY + B9;
                                break;
                            case 4:
                                y = lastY + B11;
                                break;
                            case 5:
                                y = lastY + B13;
                                break;
                            case 6:
                                y = lastY + B15;
                                break;
                            case 7:
                                y = lastY + B17;
                                break;
                            case 8:
                                y = lastY + B19;
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

        public static List<uint[]> ShamirSecretSharing(uint[] vector, int numOfShares)
        {
            int maxRangeForRandom = 65535;
            var shares = new List<uint[]>();
            for (int i = 0; i < numOfShares; i++)
            {
                shares.Add(new uint[vector.Length]);
            }

            if (numOfShares == 3)
            {
                int shareCount = 0;
                foreach (int entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint lastY = (uint)entry;

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
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b;
                    uint B2 = b + b;
                    uint B3 = B + B2;
                    uint B5 = B3 + B2;
                    uint B7 = B5 + B2;
                    uint B9 = B7 + B2;
                    uint lastY = 0;

                    for (int i = 0; i < 5; i++)
                    {
                        uint y = 0;
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
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);
                    uint c = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b + c;
                    uint B1 = 6 * c;
                    uint B2 = 2 * b;
                    uint B3 = B + B2 + B1;
                    uint B5 = B3 + B2 + 2 * B1;
                    uint B7 = B5 + B2 + 3 * B1;
                    uint B9 = B7 + B2 + 4 * B1;
                    uint B11 = B9 + B2 + 5 * B1;
                    uint B13 = B11 + B2 + 6 * B1;

                    uint lastY = 0;

                    for (int i = 0; i < 7; i++)
                    {
                        uint y = 0;
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

            else if (numOfShares == 9)
            {
                int shareCount = 0;
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);
                    uint c = (uint)random.Next(2, maxRangeForRandom);
                    uint d = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b + c + d;
                    uint B1 = 2 * d;
                    uint B2 = 6 * c;
                    uint B3 = 2 * b;
                    uint B5 = B + B3 + B2 + 7 * B1;
                    uint B7 = B5 + B3 + 2 * B2 + 25 * B1;
                    uint B9 = B7 + B3 + 3 * B2 + 55 * B1;
                    uint B11 = B9 + B3 + 4 * B2 + 97 * B1;
                    uint B13 = B11 + B3 + 5 * B2 + 151 * B1;
                    uint B15 = B13 + B3 + 6 * B2 + 217 * B1;
                    uint B17 = B15 + B3 + 7 * B2 + 295 * B1;
                    uint B19 = B17 + B3 + 8 * B2 + 385 * B1;

                    uint lastY = 0;

                    for (int i = 0; i < 9; i++)
                    {
                        uint y = 0;
                        switch (i)
                        {
                            case 0:
                                y = entry + B;
                                break;
                            case 1:
                                y = lastY + B5;
                                break;
                            case 2:
                                y = lastY + B7;
                                break;
                            case 3:
                                y = lastY + B9;
                                break;
                            case 4:
                                y = lastY + B11;
                                break;
                            case 5:
                                y = lastY + B13;
                                break;
                            case 6:
                                y = lastY + B15;
                                break;
                            case 7:
                                y = lastY + B17;
                                break;
                            case 8:
                                y = lastY + B19;
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

        public static List<uint[]> ShamirSecretSharing(int[] vector, int numOfShares)
        {
            int maxRangeForRandom = 65535;
            var shares = new List<uint[]>();
            for (int i = 0; i < numOfShares; i++)
            {
                shares.Add(new uint[vector.Length]);
            }

            if (numOfShares == 3)
            {
                int shareCount = 0;
                foreach (int entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint lastY = (uint)entry;

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
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b;
                    uint B2 = b + b;
                    uint B3 = B + B2;
                    uint B5 = B3 + B2;
                    uint B7 = B5 + B2;
                    uint B9 = B7 + B2;
                    uint lastY = 0;

                    for (int i = 0; i < 5; i++)
                    {
                        uint y = 0;
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
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);
                    uint c = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b + c;
                    uint B1 = 6 * c;
                    uint B2 = 2 * b;
                    uint B3 = B + B2 + B1;
                    uint B5 = B3 + B2 + 2 * B1;
                    uint B7 = B5 + B2 + 3 * B1;
                    uint B9 = B7 + B2 + 4 * B1;
                    uint B11 = B9 + B2 + 5 * B1;
                    uint B13 = B11 + B2 + 6 * B1;

                    uint lastY = 0;

                    for (int i = 0; i < 7; i++)
                    {
                        uint y = 0;
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

            else if (numOfShares == 9)
            {
                int shareCount = 0;
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);
                    uint c = (uint)random.Next(2, maxRangeForRandom);
                    uint d = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b + c + d;
                    uint B1 = 2 * d;
                    uint B2 = 6 * c;
                    uint B3 = 2 * b;
                    uint B5 = B + B3 + B2 + 7 * B1;
                    uint B7 = B5 + B3 + 2 * B2 + 25 * B1;
                    uint B9 = B7 + B3 + 3 * B2 + 55 * B1;
                    uint B11 = B9 + B3 + 4 * B2 + 97 * B1;
                    uint B13 = B11 + B3 + 5 * B2 + 151 * B1;
                    uint B15 = B13 + B3 + 6 * B2 + 217 * B1;
                    uint B17 = B15 + B3 + 7 * B2 + 295 * B1;
                    uint B19 = B17 + B3 + 8 * B2 + 385 * B1;

                    uint lastY = 0;

                    for (int i = 0; i < 9; i++)
                    {
                        uint y = 0;
                        switch (i)
                        {
                            case 0:
                                y = entry + B;
                                break;
                            case 1:
                                y = lastY + B5;
                                break;
                            case 2:
                                y = lastY + B7;
                                break;
                            case 3:
                                y = lastY + B9;
                                break;
                            case 4:
                                y = lastY + B11;
                                break;
                            case 5:
                                y = lastY + B13;
                                break;
                            case 6:
                                y = lastY + B15;
                                break;
                            case 7:
                                y = lastY + B17;
                                break;
                            case 8:
                                y = lastY + B19;
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

        public static void ShamirSecretSharingNoStoring(uint scalar, int numOfShares)
        {
            int maxRangeForRandom = 65535;

            if (numOfShares == 3)
            {
                uint a = (uint)random.Next(2, maxRangeForRandom);
                uint lastY = scalar;

                for (int i = 0; i < 3; i++)
                {
                    var y = lastY + a;
                    lastY = y;
                }
            }

            else if (numOfShares == 5)
            {
                uint a = (uint)random.Next(2, maxRangeForRandom);
                uint b = (uint)random.Next(2, maxRangeForRandom);

                uint B = a + b;
                uint B2 = b + b;
                uint B3 = B + B2;
                uint B5 = B3 + B2;
                uint B7 = B5 + B2;
                uint B9 = B7 + B2;
                uint lastY = 0;

                for (int i = 0; i < 5; i++)
                {
                    uint y = 0;
                    switch (i)
                    {
                        case 0:
                            y = scalar + B;
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
                }
            }

            else if (numOfShares == 7)
            {
                uint a = (uint)random.Next(2, maxRangeForRandom);
                uint b = (uint)random.Next(2, maxRangeForRandom);
                uint c = (uint)random.Next(2, maxRangeForRandom);

                uint B = a + b + c;
                uint B1 = 6 * c;
                uint B2 = 2 * b;
                uint B3 = B + B2 + B1;
                uint B5 = B3 + B2 + 2 * B1;
                uint B7 = B5 + B2 + 3 * B1;
                uint B9 = B7 + B2 + 4 * B1;
                uint B11 = B9 + B2 + 5 * B1;
                uint B13 = B11 + B2 + 6 * B1;

                uint lastY = 0;

                for (int i = 0; i < 7; i++)
                {
                    uint y = 0;
                    switch (i)
                    {
                        case 0:
                            y = scalar + B;
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
                }
            }

            else if (numOfShares == 9)
            {
                uint a = (uint)random.Next(2, maxRangeForRandom);
                uint b = (uint)random.Next(2, maxRangeForRandom);
                uint c = (uint)random.Next(2, maxRangeForRandom);
                uint d = (uint)random.Next(2, maxRangeForRandom);

                uint B = a + b + c + d;
                uint B1 = 2 * d;
                uint B2 = 6 * c;
                uint B3 = 2 * b;
                uint B5 = B + B3 + B2 + 7 * B1;
                uint B7 = B5 + B3 + 2 * B2 + 25 * B1;
                uint B9 = B7 + B3 + 3 * B2 + 55 * B1;
                uint B11 = B9 + B3 + 4 * B2 + 97 * B1;
                uint B13 = B11 + B3 + 5 * B2 + 151 * B1;
                uint B15 = B13 + B3 + 6 * B2 + 217 * B1;
                uint B17 = B15 + B3 + 7 * B2 + 295 * B1;
                uint B19 = B17 + B3 + 8 * B2 + 385 * B1;

                uint lastY = 0;

                for (int i = 0; i < 9; i++)
                {
                    uint y = 0;
                    switch (i)
                    {
                        case 0:
                            y = scalar + B;
                            break;
                        case 1:
                            y = lastY + B5;
                            break;
                        case 2:
                            y = lastY + B7;
                            break;
                        case 3:
                            y = lastY + B9;
                            break;
                        case 4:
                            y = lastY + B11;
                            break;
                        case 5:
                            y = lastY + B13;
                            break;
                        case 6:
                            y = lastY + B15;
                            break;
                        case 7:
                            y = lastY + B17;
                            break;
                        case 8:
                            y = lastY + B19;
                            break;
                    }
                    lastY = y;
                }
            }
        }

        public static void ShamirSecretSharingNoStoring(uint[] vector, int numOfShares)
        {
            int maxRangeForRandom = 65535;

            if (numOfShares == 3)
            {
                int shareCount = 0;
                foreach (int entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint lastY = (uint)entry;

                    for (int i = 0; i < 3; i++)
                    {
                        var y = lastY + a;
                        lastY = y;
                    }
                    shareCount++;
                }
            }

            else if (numOfShares == 5)
            {
                int shareCount = 0;
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b;
                    uint B2 = b + b;
                    uint B3 = B + B2;
                    uint B5 = B3 + B2;
                    uint B7 = B5 + B2;
                    uint B9 = B7 + B2;
                    uint lastY = 0;

                    for (int i = 0; i < 5; i++)
                    {
                        uint y = 0;
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
                    }
                    shareCount++;
                }
            }

            else if (numOfShares == 7)
            {
                int shareCount = 0;
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);
                    uint c = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b + c;
                    uint B1 = 6 * c;
                    uint B2 = 2 * b;
                    uint B3 = B + B2 + B1;
                    uint B5 = B3 + B2 + 2 * B1;
                    uint B7 = B5 + B2 + 3 * B1;
                    uint B9 = B7 + B2 + 4 * B1;
                    uint B11 = B9 + B2 + 5 * B1;
                    uint B13 = B11 + B2 + 6 * B1;

                    uint lastY = 0;

                    for (int i = 0; i < 7; i++)
                    {
                        uint y = 0;
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
                    }
                    shareCount++;
                }
            }

            else if (numOfShares == 9)
            {
                int shareCount = 0;
                foreach (uint entry in vector)
                {
                    uint a = (uint)random.Next(2, maxRangeForRandom);
                    uint b = (uint)random.Next(2, maxRangeForRandom);
                    uint c = (uint)random.Next(2, maxRangeForRandom);
                    uint d = (uint)random.Next(2, maxRangeForRandom);

                    uint B = a + b + c + d;
                    uint B1 = 2 * d;
                    uint B2 = 6 * c;
                    uint B3 = 2 * b;
                    uint B5 = B + B3 + B2 + 7 * B1;
                    uint B7 = B5 + B3 + 2 * B2 + 25 * B1;
                    uint B9 = B7 + B3 + 3 * B2 + 55 * B1;
                    uint B11 = B9 + B3 + 4 * B2 + 97 * B1;
                    uint B13 = B11 + B3 + 5 * B2 + 151 * B1;
                    uint B15 = B13 + B3 + 6 * B2 + 217 * B1;
                    uint B17 = B15 + B3 + 7 * B2 + 295 * B1;
                    uint B19 = B17 + B3 + 8 * B2 + 385 * B1;

                    uint lastY = 0;

                    for (int i = 0; i < 9; i++)
                    {
                        uint y = 0;
                        switch (i)
                        {
                            case 0:
                                y = entry + B;
                                break;
                            case 1:
                                y = lastY + B5;
                                break;
                            case 2:
                                y = lastY + B7;
                                break;
                            case 3:
                                y = lastY + B9;
                                break;
                            case 4:
                                y = lastY + B11;
                                break;
                            case 5:
                                y = lastY + B13;
                                break;
                            case 6:
                                y = lastY + B15;
                                break;
                            case 7:
                                y = lastY + B17;
                                break;
                            case 8:
                                y = lastY + B19;
                                break;
                        }
                        lastY = y;
                    }
                    shareCount++;
                }
            }
        }

        /// <summary>
        /// Protocol 2 - Computing the scalar product between two vectors that are shared between D mediators
        /// </summary>
        /// <param name="clShares"></param>
        /// <param name="cmShares"></param>
        /// <returns></returns>

        public static ulong ScalarProductShares(List<uint[]> clShares, List<uint[]> cmShares)
        {
            ulong[] coordinates = new ulong[clShares.Count];

            for (int indexCount = 0; indexCount < clShares.Count; indexCount++)
            {
                for (int shareCount = 0; shareCount < clShares[0].Length; shareCount++)
                {
                    var newY = clShares[indexCount][shareCount] * (ulong)cmShares[indexCount][shareCount];

                    coordinates[indexCount] += newY;
                }
            }

            var secret = ReconstructShamirSecret(coordinates.ToList());

            return secret;
        }

        public static ulong ScalarProductShares(List<ushort[]> clShares, List<ushort[]> cmShares)
        {
            ulong[] coordinates = new ulong[clShares.Count];

            for (int indexCount = 0; indexCount < clShares.Count; indexCount++)
            {
                for (int shareCount = 0; shareCount < clShares[0].Length; shareCount++)
                {
                    var newY = clShares[indexCount][shareCount] * (ulong)cmShares[indexCount][shareCount];

                    coordinates[indexCount] += newY;
                }
            }

            var secret = ReconstructShamirSecret(coordinates.ToList());

            return secret;
        }

        public static ulong[] GenerateXd(int q, int[] offeredItemIndecis, double[,] similarityMatrix, double[] xiRShareVector)
        {
            List<ulong> Xd = new List<ulong>();
            List<double> XdShares = new List<double>();
            List<double> oneMinusXiShares = new List<double>();
            double addon = Q * q + 1;
            foreach (var itemIndex in offeredItemIndecis)
            {
                double Xdm = addon;
                double[] topItemsSimilarityVector = GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, itemIndex, q, false);
                Xdm += ScalarProductVectors(topItemsSimilarityVector, xiRShareVector);
                XdShares.Add(Xdm);
                oneMinusXiShares.Add(1 - xiRShareVector[itemIndex]);
            }

            for (int i = 0; i < XdShares.Count; i++)
            {
                long mult = (long)(XdShares[i] * oneMinusXiShares[i]);
                var mod = ModForNegative(mult);
                Xd.Add(mod);
            }

            return Xd.ToArray();
        }

        public static ulong[] GenerateXd(int q, int[] offeredItemIndecis, double[,] similarityMatrix, uint[] xiRShareVector)
        {
            List<ulong> Xd = new List<ulong>();
            List<double> XdShares = new List<double>();
            List<double> oneMinusXiShares = new List<double>();
            double addon = Q * q + 1;
            foreach (var itemIndex in offeredItemIndecis)
            {
                double Xdm = addon;
                double[] topItemsSimilarityVector = GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, itemIndex, q, false);
                Xdm += ScalarProductVectors(topItemsSimilarityVector, xiRShareVector);
                XdShares.Add(Xdm);
                oneMinusXiShares.Add(1 - xiRShareVector[itemIndex]);
            }

            for (int i = 0; i < XdShares.Count; i++)
            {
                long mult = (long)(XdShares[i] * oneMinusXiShares[i]);
                var mod = ModForNegative(mult);
                Xd.Add(mod);
            }

            return Xd.ToArray();
        }

        public static int[] GetItemsOfferedByVendor(sbyte[,] Rk)
        {
            List<int> itemIndecis = new List<int>();
            int N = Rk.GetLength(0);
            int M = Rk.GetLength(1);
            for (int itemIndex = 0; itemIndex < M; itemIndex++)
            {
                var itemRatings = Rk.GetVerticalVector(itemIndex);
                for (int userIndex = 0; userIndex < N; userIndex++)
                {
                    if (itemRatings[userIndex] != -1)
                    {
                        itemIndecis.Add(itemIndex);
                        break;
                    }
                }
            }
            return itemIndecis.ToArray();
        }

        public static double CalcVnm(List<double[]> XiRnShares, double[] sm, double[] averageRatings)
        {
            double[] cl = new double[averageRatings.Length];
            for (int i = 0; i < cl.Length; i++)
            {
                cl[i] = Math.Round(Q * sm[i] * averageRatings[i], 0);
            }
            double Vnm = MultiplySharesByVector(XiRnShares, cl) / Q;

            return Vnm;
        }

        public static double CalcVnm(List<uint[]> XiRnShares, double[] sm, double[] averageRatings)
        {
            double[] cl = new double[averageRatings.Length];
            for (int i = 0; i < cl.Length; i++)
            {
                cl[i] = Math.Round(Q * sm[i] * averageRatings[i], 0);
            }
            double Vnm = MultiplySharesByVector(XiRnShares, cl) / Q;

            return Vnm;
        }

        public static double MultiplySharesByVector(List<double[]> shares, double[] vector)
        {
            List<ulong> xds = new List<ulong>();
            foreach (var share in shares)
            {
                double xd = 0;
                for (int itemCounter = 0; itemCounter < vector.Length; itemCounter++)
                {
                    xd += share[itemCounter] * vector[itemCounter];
                }
                xd = Math.Round(xd, 0);
                xds.Add((ulong)xd);
            }

            var secret = ReconstructShamirSecret(xds);

            return secret;
        }

        public static double MultiplySharesByVector(List<uint[]> shares, double[] vector)
        {
            List<ulong> xds = new List<ulong>();
            foreach (var share in shares)
            {
                double xd = 0;
                for (int itemCounter = 0; itemCounter < vector.Length; itemCounter++)
                {
                    xd += share[itemCounter] * vector[itemCounter];
                }
                xd = Math.Round(xd, 0);
                xds.Add((ulong)xd);
            }

            var secret = ReconstructShamirSecret(xds);

            return secret;
        }

        public static double ComputeAverageRating(List<uint[,]> RShares, List<uint[,]> XiRShares, int m)
        {
            List<double> Xds = new List<double>();
            foreach (var share in RShares)
            {
                Xds.Add(share.GetVerticalVector(m).Select(o => (double)o).Sum());
            }

            List<double> Yds = new List<double>();
            foreach (var share in XiRShares)
            {
                Yds.Add(share.GetVerticalVector(m).Select(o => (double)o).Sum());
            }

            List<ulong> xCoordinates = new List<ulong>();

            foreach (var xd in Xds)
            {
                xCoordinates.Add((ulong)xd);
            }
            var x = ReconstructShamirSecret(xCoordinates);

            List<ulong> yCoordinates = new List<ulong>();
            foreach (var yd in Yds)
            {
                yCoordinates.Add((ulong)yd);
            }
            var y = ReconstructShamirSecret(yCoordinates);

            if (y == 0)
            {
                return 0;
            }

            return (double)x / y;
        }

        public static double ComputeAverageRating(List<double[,]> RShares, List<double[,]> XiRShares, int m)
        {
            List<double> Xds = new List<double>();
            foreach (var share in RShares)
            {
                Xds.Add(share.GetVerticalVector(m).Sum());
            }

            List<double> Yds = new List<double>();
            foreach (var share in XiRShares)
            {
                Yds.Add(share.GetVerticalVector(m).Sum());
            }

            List<ulong> xCoordinates = new List<ulong>();

            foreach (var xd in Xds)
            {
                xCoordinates.Add((ulong)xd);
            }
            var x = ReconstructShamirSecret(xCoordinates);

            List<ulong> yCoordinates = new List<ulong>();
            foreach (var yd in Yds)
            {
                yCoordinates.Add((ulong)yd);
            }
            var y = ReconstructShamirSecret(yCoordinates);

            if (y == 0)
            {
                return 0;
            }

            return x / y;
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

        public static ulong ScalarProductVectors(int[] vector1, int[] vector2)
        {
            int length = vector1.Length;
            ulong sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum += (ulong)(vector1[i] * vector2[i]);
            }

            return sum % PRIME;
        }

        public static ulong ScalarProductVectors(ushort[] vector1, ushort[] vector2)
        {
            int length = vector1.Length;
            ulong sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum += (ulong)(vector1[i] * vector2[i]);
            }

            return sum % PRIME;
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

        public static double ScalarProductVectors(double[] vector1, uint[] vector2)
        {
            int length = vector1.Length;
            double sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum += vector1[i] * vector2[i];
            }

            return sum % PRIME;
        }

        public static ulong ReconstructShamirSecret(List<ulong> coordinates)
        {
            ulong secret = 0;
            if (coordinates.Count == 3)
            {
                secret = ((3 * (coordinates[0] - coordinates[1]) + coordinates[2]) % PRIME);
            }
            else if (coordinates.Count == 5)
            {
                secret = ((5 * (coordinates[0] - coordinates[3])) - (10 * (coordinates[1] - coordinates[2])) + coordinates[4]) % PRIME;
            }
            else if (coordinates.Count == 7)
            {
                secret = ((7 * (coordinates[0] - coordinates[5])) + (21 * (coordinates[4] - coordinates[1])) + (35 * (coordinates[2] - coordinates[3])) + coordinates[6]) % PRIME;
            }
            else if (coordinates.Count == 9)
            {
                secret = ((9 * (coordinates[0] - coordinates[7])) + (36 * (coordinates[6] - coordinates[1])) + (84 * (coordinates[2] - coordinates[5])) + (126 * (coordinates[4] - coordinates[3])) + coordinates[8]) % PRIME;
            }

            if (secret < 0)
            {
                secret += PRIME;
            }

            return secret;
        }

        /// <summary>
        /// Place the vendor average rating instead of some of the zero cells
        /// </summary>
        /// <param name="trainingUserItemMatrix"></param>
        /// <param name="vendorsItemsIndecis"></param>
        /// <param name="percentOfFakeCells"></param>
        /// <returns></returns>
        public static sbyte[,] GetYPUserItemMatrix(sbyte[,] trainingUserItemMatrix, List<int[]> vendorsItemsIndecis, int percentOfFakeCells)
        {
            int N = trainingUserItemMatrix.GetLength(0); // users
            int M = trainingUserItemMatrix.GetLength(1); // items

            sbyte[,] YPPredictedRatings = new sbyte[N, M];

            foreach (var vendorItemsIndecis in vendorsItemsIndecis)
            {
                sbyte[,] vendorUserItemMatrix = trainingUserItemMatrix.GetVerticalSubMatrix(vendorItemsIndecis);
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


                sbyte averageRating = (sbyte)(ratingsSum / ratingsCount);
                int numOfCellsToPlaceFakeRating = (int)((emeptyCells / 100) * percentOfFakeCells);
                var vendorMatrixWithFakeCells = vendorUserItemMatrix.PlaceFakeCells(numOfCellsToPlaceFakeRating, averageRating);

                YPPredictedRatings.CopySubMatrix(vendorMatrixWithFakeCells, vendorItemsIndecis[0]);
            }
            return YPPredictedRatings;
        }

        public static double CalcSimilarityScoreNoCrypto(sbyte[,] userItemMatrix, int i, int j)
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

        public static uint[,] CreateRandomMatrix(int m)
        {
            uint[,] similarityMatrix = new uint[m, m];

            for (int i = 0; i < m; i++)
            {
                for (int j = i + 1; j < m; j++)
                {

                    double similarityScore = random.NextDouble();
                    //Convert to integer value
                    similarityMatrix[i, j] = (uint)Math.Floor((similarityScore * Q) + 0.5);
                    similarityMatrix[j, i] = (uint)Math.Floor((similarityScore * Q) + 0.5);
                }
            }
            return similarityMatrix;
        }

        public static double[,] CalcSimilarityMatrixNoCrypto(sbyte[,] userItemMatrix)
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
        public static List<double[]>[] SecretShareRHat(sbyte[,] userItemMatrix, int numOfShares)
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
        public static List<double[]>[] SecretShareXiR(sbyte[,] userItemMatrix, int numOfShares)
        {
            List<double[]>[] xiRShares = new List<double[]>[numOfShares];

            sbyte[,] xiR = userItemMatrix.GetXi();
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
        /// Obfuscate the values of the shares according to Protocol 3 - removed
        /// </summary>
        /// <param name="xiRShares"></param>
        /// <returns></returns>
        public static List<uint[,]> ObfuscateShares(List<uint[,]> shares)
        {
            List<uint[,]> obfescatedShares = new List<uint[,]>();
            int N = shares[0].GetLength(0);
            int M = shares[0].GetLength(1);

            sbyte[,] zeroedMatrix = new sbyte[N, M];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    zeroedMatrix[i, j] = 0;
                }
            }

            var R0 = ShamirSecretSharingMatrix(zeroedMatrix, shares.Count);

            for (int shareIndex = 0; shareIndex < shares.Count; shareIndex++)
            {
                shares[shareIndex].AddShare(R0[shareIndex]);
                obfescatedShares.Add(shares[shareIndex]);
            }

            return obfescatedShares;
        }

        /// <summary>
        /// Obfuscate the values of the shares according to Protocol 4
        /// </summary>
        /// <param name="xiRShares"></param>
        /// <returns></returns>
        public static List<double[]>[] ObfuscateSharesOld(List<double[]>[] shares)
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

        public static uint[] GetSimilarityVectorForTopSimilarItemsToM(uint[,] similarityMatrix, int m, int q, bool isPositivesOnly)
        {
            int vectorLength = similarityMatrix.GetLength(0);
            Tuple<uint, int>[] similarityScoreAndIndex = new Tuple<uint, int>[vectorLength];

            for (int i = 0; i < vectorLength; i++)
            {
                similarityScoreAndIndex[i] = new Tuple<uint, int>(similarityMatrix[m, i], i);
            }

            Array.Sort(similarityScoreAndIndex, new ScoreAndIndexComparerUint());
            similarityScoreAndIndex = similarityScoreAndIndex.Take(q).ToArray();
            uint[] sm = new uint[vectorLength];

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

        public static int GetPredictedRatingNoCrypto(sbyte[,] userItemMatrix, int n, int m, int q, double[,] similarityMatrixNoCrypto = null)
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

        public static ulong ModForNegative(long x)
        {
            long prime = 2147483647;
            return (ulong)((x % prime + prime) % prime);
        }
    }

    public class ScoreAndIndexComparer : IComparer<Tuple<double, int>>
    {
        public int Compare(Tuple<double, int> x, Tuple<double, int> y)
        {
            return x.Item1.CompareTo(y.Item1) * -1;
        }
    }

    public class ScoreAndIndexComparerUint : IComparer<Tuple<uint, int>>
    {
        public int Compare(Tuple<uint, int> x, Tuple<uint, int> y)
        {
            return x.Item1.CompareTo(y.Item1) * -1;
        }
    }

}
