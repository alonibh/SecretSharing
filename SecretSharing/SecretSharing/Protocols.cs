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
        public static readonly BigInteger PRIME = BigInteger.Parse("1298074214633706835075030044377087");
        public static readonly double Q = 1296859633245;

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


        public static List<BigInteger[]> AllOrNothingSecretSharing(BigInteger[] vector, int numOfShares)
        {
            List<BigInteger[]> shares = new List<BigInteger[]>();
            BigInteger[] sharesSum = new BigInteger[vector.Length];

            for (int i = 0; i < numOfShares - 1; i++)
            {
                BigInteger[] share = new BigInteger[vector.Length];
                for (int j = 0; j < vector.Length; j++)
                {
                    share[j] = RandomBigIntegerBelow(PRIME);
                    sharesSum[j] += share[j];
                }
                shares.Add(share);
            }

            BigInteger[] lastShare = new BigInteger[vector.Length];
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

        public static BigInteger[] ReconstructAllOrNothingSecret(List<BigInteger[]> shares)
        {
            BigInteger[] secret = new BigInteger[shares[0].Length];
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

        public static BigInteger ScalarProductVectors(BigInteger[] vector1, BigInteger[] vector2)
        {
            int length = vector1.Length;
            BigInteger sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum += vector1[i] * vector2[i];
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
            return sum;
        }

        /// <summary>
        /// Shamir's secret sharing using a third-party nuget
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="numOfSharesToMake"></param>
        /// <param name="numOfSharesForRecovery"></param>
        /// <returns>Shares array for each of the mediators</returns>
        public static List<Coordinate[]> ShamirSecretSharing(BigInteger[] vector, int numOfShares)
        {
            int numOfSharesForRecovery = (numOfShares - 1) / 2;

            var shares = new List<Coordinate[]>();
            for (int i = 0; i < numOfShares; i++)
            {
                shares.Add(new Coordinate[vector.Length]);
            }

            var shamirScheme = new ShamirSecretSharingScheme();

            int shareCount = 0;
            foreach (int entry in vector)
            {
                List<Coordinate> entryShares = shamirScheme.Shamir(entry, PRIME, numOfShares, numOfSharesForRecovery, false);

                int indexCount = 0;
                foreach (var share in entryShares)
                {
                    shares[indexCount][shareCount] = share;
                    indexCount++;
                }
                shareCount++;
            }

            return shares;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shares">List of arrays of shares, where each array contains one share of eash secret</param>
        /// <returns></returns>
        public static BigInteger[] ReconstructShamirSecret(List<Coordinate[]> shares)
        {
            BigInteger[] secretVector = new BigInteger[shares[0].Length];
            var shamirScheme = new ShamirSecretSharingScheme();

            for (int shareCount = 0; shareCount < shares[0].Length; shareCount++)
            {
                List<Coordinate> coordinates = new List<Coordinate>();
                for (int indexCount = 0; indexCount < shares.Count; indexCount++)
                {
                    coordinates.Add(shares[indexCount][shareCount]);
                }
                var secret = shamirScheme.deShamir(coordinates, PRIME);

                secretVector[shareCount] = secret;
            }

            return secretVector;
        }

        /// <summary>
        /// Protocol 2 - Computing the scalar product between two vectors that are shared between D mediators
        /// </summary>
        /// <param name="clShares"></param>
        /// <param name="cmShares"></param>
        /// <returns></returns>
        public static double ScalarProductShares(List<Coordinate[]> clShares, List<Coordinate[]> cmShares)
        {
            List<Coordinate[]> multShares = new List<Coordinate[]>();

            for (int indexCount = 0; indexCount < clShares.Count; indexCount++)
            {
                Coordinate[] multCoordinates = new Coordinate[clShares[0].Length];
                for (int shareCount = 0; shareCount < clShares[0].Length; shareCount++)
                {
                    var newX = clShares[indexCount][shareCount].X;
                    var newY = clShares[indexCount][shareCount].Y * cmShares[indexCount][shareCount].Y;
                    multCoordinates[shareCount] = new Coordinate(newX, newY);
                }
                multShares.Add(multCoordinates);
            }

            List<Coordinate> coordinates = new List<Coordinate>();
            for (int i = 0; i < clShares.Count; i++)
            {
                BigInteger sumX = 0;
                BigInteger sumY = 0;
                for (int j = 0; j < clShares[0].Length; j++)
                {
                    sumX += multShares[i][j].X;
                    sumY += multShares[i][j].Y;
                }
                coordinates.Add(new Coordinate(sumX, sumY));
            }

            var shamirScheme = new ShamirSecretSharingScheme();

            var secret = shamirScheme.deShamir(coordinates, PRIME);

            return (double)secret;
        }

        /// <summary>
        /// Calculates the similarity matrix based on Protocol 1, and convert it to big integers
        /// </summary>
        /// <param name="userItemMatrix">The user-item matrix</param>
        /// <param name="numOfShares">D</param>
        /// <returns></returns>
        public static BigInteger[,] CalcSimilarityMatrix(int[,] userItemMatrix, int numOfShares)
        {
            int items = userItemMatrix.GetLength(1);
            BigInteger[,] similarityMatrix = new BigInteger[items, items];
            List<Coordinate[]>[] clSharesArray = new List<Coordinate[]>[items];
            List<Coordinate[]>[] clPowSharesArray = new List<Coordinate[]>[items];
            List<Coordinate[]>[] xiClSharesArray = new List<Coordinate[]>[items];

            var watch = System.Diagnostics.Stopwatch.StartNew();
            Parallel.For(0, items, (i) =>
            {
                BigInteger[] cl = userItemMatrix.GetVerticalVector(i).Select(o => (BigInteger)o).ToArray();
                var clShares = ShamirSecretSharing(cl, numOfShares);
                clSharesArray[i] = clShares;

                BigInteger[] clPow = Array.ConvertAll(cl, x => x * x);
                var clPowShares = ShamirSecretSharing(clPow, numOfShares);
                clPowSharesArray[i] = clPowShares;

                BigInteger[] xiCl = Array.ConvertAll(cl, x => x == 0 ? (BigInteger)0 : 1);
                var xiClShares = ShamirSecretSharing(xiCl, numOfShares);
                xiClSharesArray[i] = xiClShares;
            });
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            Console.WriteLine($"Phase 1 - done in {elapsedMs} MS");

            for (int i = 0; i < items; i++)
            {
                watch = System.Diagnostics.Stopwatch.StartNew();

                var options = new ParallelOptions();
                options.MaxDegreeOfParallelism = 75;
                Parallel.For(i + 1, items, options, (j) =>
                 {
                     double z1 = ScalarProductShares(clSharesArray[i], clSharesArray[j]);

                     double z2 = ScalarProductShares(clPowSharesArray[i], xiClSharesArray[j]);

                     double z3 = ScalarProductShares(xiClSharesArray[i], clPowSharesArray[j]);

                     double similarityScore = 0;
                     if (z2 * z3 != 0)
                     {
                         similarityScore = z1 / (Math.Sqrt(z2 * z3));
                     }

                     //Convert to integer value
                     similarityMatrix[i, j] = (BigInteger)Math.Floor((similarityScore * Q) + 0.5);
                     similarityMatrix[j, i] = (BigInteger)Math.Floor((similarityScore * Q) + 0.5);
                 });
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                Console.WriteLine($"{i}/{items} done in {elapsedMs} MS");
            }

            return similarityMatrix;
        }

        public static void RunProtocol2RuntimeTest(int[,] userItemMatrix, int numOfShares)
        {
            int times = 300;
            int timesLeft = times;
            int items = userItemMatrix.GetLength(1);
            BigInteger[,] similarityMatrix = new BigInteger[items, items];
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < items; i++)
            {
                BigInteger[] cl = userItemMatrix.GetVerticalVector(i).Select(o => (BigInteger)o).ToArray();
                var clShares = ShamirSecretSharing(cl, numOfShares);
                BigInteger[] clPow = Array.ConvertAll(cl, x => x * x);
                var clPowShares = ShamirSecretSharing(clPow, numOfShares);
                BigInteger[] xiCl = Array.ConvertAll(cl, x => x == 0 ? (BigInteger)0 : 1);
                var xiClShares = ShamirSecretSharing(xiCl, numOfShares);

                for (int j = i + 1; j < items; j++)
                {
                    if (timesLeft == 0)
                    {
                        break;
                    }

                    BigInteger[] cm = userItemMatrix.GetVerticalVector(j).Select(o => (BigInteger)o).ToArray();

                    var cmShares = ShamirSecretSharing(cm, numOfShares);
                    double z1 = ScalarProductShares(clShares, cmShares);

                    BigInteger[] xiCm = Array.ConvertAll(cm, x => x == 0 ? (BigInteger)0 : 1);

                    var xiCmShares = ShamirSecretSharing(xiCm, numOfShares);
                    double z2 = ScalarProductShares(clPowShares, xiCmShares);

                    BigInteger[] cmPow = Array.ConvertAll(cm, x => x * x);

                    var cmPowShares = ShamirSecretSharing(cmPow, numOfShares);
                    double z3 = ScalarProductShares(xiClShares, cmPowShares);

                    double similarityScore = 0;
                    if (z2 * z3 != 0)
                    {
                        similarityScore = z1 / (Math.Sqrt(z2 * z3));
                    }

                    //Convert to integer value
                    similarityMatrix[i, j] = (BigInteger)Math.Floor((similarityScore * Q) + 0.5);
                    similarityMatrix[j, i] = (BigInteger)Math.Floor((similarityScore * Q) + 0.5);

                    // the code that you want to measure comes here
                    Console.WriteLine(j + "/" + items);
                    timesLeft--;

                }
                if (timesLeft == 0)
                {
                    break;
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs / times);
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
                Console.WriteLine(i + "/" + items);
            }
            return similarityMatrix;
        }

        /// <summary>
        /// Secret sharing the adjusted user-item matrix - protocol 3
        /// </summary>
        /// <param name="userItemMatrix"></param>
        /// <returns>Array of marices - each matrix has M vectors of shares</returns>
        public static List<BigInteger[]>[] SecretShareRHat(int[,] userItemMatrix, int numOfShares)
        {
            // Array of marices - each matrix has M vectors of shares
            // For example RHatShares[2][1] is the second column of r-hat of the third mediator
            List<BigInteger[]>[] RHatShares = new List<BigInteger[]>[numOfShares];
            BigInteger[,] adjustedUserItemMatrix = userItemMatrix.GetAdjustedUserItemMatrix(Q);
            for (int i = 0; i < adjustedUserItemMatrix.GetLength(1); i++)
            {
                var adjustedRatings = adjustedUserItemMatrix.GetVerticalVector(i);
                var shares = AllOrNothingSecretSharing(adjustedRatings, numOfShares);
                int shareCount = 0;
                foreach (var share in shares)
                {
                    if (RHatShares[shareCount] == null)
                    {
                        RHatShares[shareCount] = new List<BigInteger[]>();
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
        public static double[] ReconstructRHatSecret(List<BigInteger[]> shares)
        {
            BigInteger[] secret = new BigInteger[shares[0].Length];
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
                if (secret[i] + 10 * (BigInteger)Q > PRIME)
                {
                    secret[i] = secret[i] - PRIME;
                }
                secretAsDouble[i] = Math.Round(((double)secret[i] / Q), 3);
            }
            return secretAsDouble;
        }

        /// <summary>
        /// Secret sharing the xi of the user-item matrix - protocol 3
        /// </summary>
        /// <param name="userItemMatrix"></param>
        /// <returns>Array of marices - each matrix has M vectors of shares</returns>
        public static List<BigInteger[]>[] SecretShareXiR(int[,] userItemMatrix, int numOfShares)
        {
            List<BigInteger[]>[] xiRShares = new List<BigInteger[]>[numOfShares];

            int[,] xiR = userItemMatrix.GetXi();
            for (int i = 0; i < xiR.GetLength(1); i++)
            {
                var xiRatings = xiR.GetVerticalVector(i).Select(o => (BigInteger)o).ToArray();
                var shares = AllOrNothingSecretSharing(xiRatings, numOfShares);
                int shareCount = 0;
                foreach (var share in shares)
                {
                    if (xiRShares[shareCount] == null)
                    {
                        xiRShares[shareCount] = new List<BigInteger[]>();
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
        public static List<BigInteger[]>[] ObfuscateShares(List<BigInteger[]>[] shares)
        {
            int numOfShares = shares.Length;
            int users = shares[0][0].Length;
            List<BigInteger[]>[] obfescatedShares = new List<BigInteger[]>[numOfShares];

            for (int i = 0; i < shares[0].Count; i++)
            {
                BigInteger[] vector = new BigInteger[users];
                for (int j = 0; j < users; j++)
                {
                    BigInteger sum = 0;
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
                        obfescatedShares[shareCount] = new List<BigInteger[]>();
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
        public static BigInteger[] GetMostSimilarItemsToM(BigInteger[,] similarityMatrix, int m, int q, bool isPositivesOnly)
        {
            int vectorLength = similarityMatrix.GetLength(0);
            Tuple<BigInteger, int>[] similarityScoreAndIndex = new Tuple<BigInteger, int>[vectorLength];
            for (int i = 0; i < vectorLength; i++)
            {
                similarityScoreAndIndex[i] = new Tuple<BigInteger, int>(similarityMatrix[m, i], i);
            }
            Array.Sort(similarityScoreAndIndex, new ScoreAndIndexComparer());
            similarityScoreAndIndex = similarityScoreAndIndex.Take(q).ToArray();
            BigInteger[] sm = new BigInteger[vectorLength];

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

        public static double[] GetMostSimilarItemsToM(double[,] similarityMatrix, int m, int q, bool isPositivesOnly)
        {
            int vectorLength = similarityMatrix.GetLength(0);
            Tuple<double, int>[] similarityScoreAndIndex = new Tuple<double, int>[vectorLength];
            for (int i = 0; i < vectorLength; i++)
            {
                similarityScoreAndIndex[i] = new Tuple<double, int>(similarityMatrix[m, i], i);
            }
            Array.Sort(similarityScoreAndIndex, new ScoreAndIndexComparerDouble());
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

        private static BigInteger RandomBigIntegerBelow(BigInteger N)
        {
            byte[] bytes = N.ToByteArray();
            BigInteger R;
            var random = new Random();

            do
            {
                random.NextBytes(bytes);
                bytes[bytes.Length - 1] &= 0x7F; //force sign bit to positive
                R = new BigInteger(bytes);
            } while (R >= N);

            return R;
        }
    }

    public class ScoreAndIndexComparer : IComparer<Tuple<BigInteger, int>>
    {
        public int Compare(Tuple<BigInteger, int> x, Tuple<BigInteger, int> y)
        {
            return x.Item1.CompareTo(y.Item1) * -1;
        }
    }
    public class ScoreAndIndexComparerDouble : IComparer<Tuple<double, int>>
    {
        public int Compare(Tuple<double, int> x, Tuple<double, int> y)
        {
            return x.Item1.CompareTo(y.Item1) * -1;
        }
    }

}
