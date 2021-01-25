using LumenWorks.Framework.IO.Csv;
using MathNet.Numerics.Distributions;
using SecretSharingProtocol;
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
        /// Shamir's secret sharing using a third-party nuget
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="numOfSharesToMake"></param>
        /// <param name="numOfSharesForRecovery"></param>
        /// <returns>Shares array for each of the mediators</returns>
        public static List<Coordinate[]> ShamirSecretSharing(double[] vector, int numOfShares)
        {
            var shares = new List<Coordinate[]>();
            for (int i = 0; i < numOfShares; i++)
            {
                shares.Add(new Coordinate[vector.Length]);
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
                        int x = i + 1;
                        var y = lastY + a;
                        lastY = y;

                        shares[i][shareCount] = new Coordinate(x, y);
                    }
                    shareCount++;
                }
            }

            if (numOfShares == 5)
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
                        int x = i + 1;
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

                        shares[i][shareCount] = new Coordinate(x, y);
                    }
                    shareCount++;
                }
            }

            return shares;
        }

        /// <summary>
        /// Protocol 2 - Computing the scalar product between two vectors that are shared between D mediators
        /// </summary>
        /// <param name="clShares"></param>
        /// <param name="cmShares"></param>
        /// <returns></returns>
        public static double ScalarProductShares(List<Coordinate[]> clShares, List<Coordinate[]> cmShares)
        {
            BigCoordinate[][] multShares = new BigCoordinate[clShares.Count][];

            for (int indexCount = 0; indexCount < clShares.Count; indexCount++)
            {
                BigCoordinate[] multCoordinates = new BigCoordinate[clShares[0].Length];
                for (int shareCount = 0; shareCount < clShares[0].Length; shareCount++)
                {
                    var newX = clShares[indexCount][shareCount].X;
                    var newY = (BigInteger)clShares[indexCount][shareCount].Y * (BigInteger)cmShares[indexCount][shareCount].Y;

                    multCoordinates[shareCount] = new BigCoordinate(newX, newY);
                }
                multShares[indexCount] = multCoordinates;
            }

            List<BigCoordinate> coordinates = new List<BigCoordinate>();
            for (int i = 0; i < clShares.Count; i++)
            {
                double sumX = 0;
                BigInteger sumY = 0;
                for (int j = 0; j < clShares[0].Length; j++)
                {
                    sumX += multShares[i][j].X;
                    sumY += multShares[i][j].Y;
                }
                coordinates.Add(new BigCoordinate(sumX, sumY));
            }

            double secret = 0;
            if (coordinates.Count == 3)
            {
                secret = (double)((3 * (coordinates[0].Y - coordinates[1].Y) + coordinates[2].Y) % (BigInteger)PRIME);
            }
            if (coordinates.Count == 5)
            {
                secret = (double)(((5 * (coordinates[0].Y - coordinates[3].Y)) - (10 * (coordinates[1].Y - coordinates[2].Y)) + coordinates[4].Y) % (BigInteger)PRIME);
            }

            if (secret < 0)
            {
                secret += PRIME;
            }

            return secret;
        }

        /// <summary>
        /// Calculates the similarity matrix based on Protocol 1, and convert it to big integers
        /// </summary>
        /// <param name="userItemMatrix">The user-item matrix</param>
        /// <param name="numOfShares">D</param>
        /// <returns></returns>
        public static double[,] CalcSimilarityMatrix(int[,] userItemMatrix, int numOfShares, int[] itemsVendorIndex)
        {
            int items = userItemMatrix.GetLength(1);
            double[,] similarityMatrix = new double[items, items];
            List<Coordinate[]>[] clSharesArray = new List<Coordinate[]>[items];
            List<Coordinate[]>[] clPowSharesArray = new List<Coordinate[]>[items];
            List<Coordinate[]>[] xiClSharesArray = new List<Coordinate[]>[items];

            Console.WriteLine($"Phase 1 started");

            var watch = System.Diagnostics.Stopwatch.StartNew();
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
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var time = new TimeSpan(0, 0, 0, 0, (int)elapsedMs);
            Console.WriteLine($"Phase 1 - done in {time}");

            for (int i = 0; i < items; i++)
            {
                watch = System.Diagnostics.Stopwatch.StartNew();

                Parallel.For(i + 1, items, (j) =>
                {
                    double similarityScore = 0;

                    // If both items belong to the same vendor - do the computation without crypto
                    if (itemsVendorIndex[i] == itemsVendorIndex[j])
                    {
                        similarityScore = CalcSimilarityScoreNoCrypto(userItemMatrix, i, j);
                    }
                    else
                    {
                        double z1 = ScalarProductShares(clSharesArray[i], clSharesArray[j]);

                        double z2 = ScalarProductShares(clPowSharesArray[i], xiClSharesArray[j]);

                        double z3 = ScalarProductShares(xiClSharesArray[i], clPowSharesArray[j]);

                        var mult = z2 * z3;
                        if (mult != 0)
                        {
                            similarityScore = z1 / (Math.Sqrt(mult));
                        }
                    }
                    if (similarityScore != 0)
                    {
                        //Convert to integer value
                        similarityMatrix[i, j] = Math.Floor((similarityScore * Q) + 0.5);
                        similarityMatrix[j, i] = Math.Floor((similarityScore * Q) + 0.5);
                    }
                });
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                time = new TimeSpan(0, 0, 0, 0, (int)elapsedMs);
                Console.WriteLine($"{i}/{items} done in {time}");
            }

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

                for (int j = i + 1; j < items; j++)
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
                }
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

            double[] secretWithFractions = new double[shares[0].Length];
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
        public static double[] GetMostSimilarItemsToM(double[,] similarityMatrix, int m, int q, bool isPositivesOnly)
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

        public static int GetPredictedRatingNoCrypto(int[,] userItemMatrix, int n, int m, int q)
        {
            var averageRating = userItemMatrix.GetAverageRatings()[m];

            double[,] similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);
            var smNoCrypto = Protocols.GetMostSimilarItemsToM(similarityMatrixNoCrypto, m, q, true);

            double[] RHatn = new double[userItemMatrix.GetLength(1)];
            for (int i = 0; i < userItemMatrix.GetLength(1); i++)
            {
                int xiR = userItemMatrix[n, i] == 0 ? 0 : 1;
                var averageRatingNoCrypto = userItemMatrix.GetAverageRatings()[i];
                RHatn[i] = (userItemMatrix[n, i] - averageRatingNoCrypto) * xiR;
            }

            var mult1 = Protocols.ScalarProductVectors(smNoCrypto, RHatn);
            var xiRn = userItemMatrix.GetXi().GetHorizontalVector(n).Select(o => (double)o).ToArray();

            var mult2 = Protocols.ScalarProductVectors(smNoCrypto, xiRn);

            var change = (double)mult1 / (double)mult2;
            int predictedRating = (int)Math.Round(averageRating + change, 0);
            return predictedRating;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numOfItems"></param>
        /// <param name="numOfVendors"></param>
        /// <returns>A tuple of number of items and starting index</returns>
        public static (int, int)[] CreateRandomSplits(int numOfItems, int numOfVendors)
        {
            double mean = 0;
            double stdDev = 1;

            Normal normalDist = new Normal(mean, stdDev);
            List<double> x_k = new List<double>(); // Random Gaussian values
            for (int i = 0; i < numOfVendors; i++)
            {
                var sample = normalDist.Sample();
                x_k.Add(sample);
            }

            List<double> y_k = new List<double>();
            for (int i = 0; i < numOfVendors; i++)
            {
                y_k.Add((x_k[i] * numOfItems) / (10 * numOfVendors));
            }

            double a = y_k.Sum() / numOfVendors;

            for (int i = 0; i < numOfVendors; i++)
            {
                y_k[i] -= a;
            }

            List<double> m_k = new List<double>();
            for (int i = 0; i < numOfVendors - 1; i++)
            {
                m_k.Add(Math.Floor((numOfItems / numOfVendors) + y_k[i] + 0.5));
            }
            double lastSum = numOfItems - m_k.Sum();
            m_k.Add(lastSum);

            for (int i = 0; i < numOfVendors; i++)
            {
                File.AppendAllText("stats.txt", m_k[i].ToString() + "\n");
                Console.WriteLine(m_k[i]);
            }
            File.AppendAllText("stats.txt", "\n");
            Console.WriteLine("---------------------------------------------");
            return null;
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
