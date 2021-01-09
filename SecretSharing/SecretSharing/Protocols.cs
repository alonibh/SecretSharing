using SecretSharingProtocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SecretSharing
{
    public static class Protocols
    {
        public static readonly BigInteger PRIME = BigInteger.Parse("1298074214633706835075030044377087");
        public static readonly double Q = 1296859633245;


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

            for (int i = 0; i < items; i++)
            {
                for (int j = i + 1; j < items; j++)
                {
                    BigInteger[] cl = userItemMatrix.GetVerticalVector(i).Select(o => (BigInteger)o).ToArray();
                    BigInteger[] cm = userItemMatrix.GetVerticalVector(j).Select(o => (BigInteger)o).ToArray();

                    var clShares = ShamirSecretSharing(cl, numOfShares);
                    var cmShares = ShamirSecretSharing(cm, numOfShares);
                    double z1 = ScalarProductShares(clShares, cmShares);


                    BigInteger[] clPow = Array.ConvertAll(cl, x => x * x);
                    BigInteger[] xiCm = Array.ConvertAll(cm, x => x == 0 ? (BigInteger)0 : 1);

                    var clPowShares = ShamirSecretSharing(clPow, numOfShares);
                    var xiCmShares = ShamirSecretSharing(xiCm, numOfShares);
                    double z2 = ScalarProductShares(clPowShares, xiCmShares);


                    BigInteger[] xiCl = Array.ConvertAll(cl, x => x == 0 ? (BigInteger)0 : 1);
                    BigInteger[] cmPow = Array.ConvertAll(cm, x => x * x);

                    var xiClShares = ShamirSecretSharing(xiCl, numOfShares);
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
                }
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
                        RHatShares[shareCount] = new List<BigInteger[]>();
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
                        xiRShares[shareCount] = new List<BigInteger[]>();
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
                        obfescatedShares[shareCount] = new List<BigInteger[]>();

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
        public static BigInteger[] GetMostSimilarItemsToM(BigInteger[,] similarityMatrix, int m, int q)
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
                sm[item.Item2] = item.Item1;
            }

            return sm;
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

}
