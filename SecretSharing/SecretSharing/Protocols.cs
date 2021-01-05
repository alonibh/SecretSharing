using SecretSharingProtocol;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SecretSharing
{
    public static class Protocols
    {
        private static readonly BigInteger PRIME = BigInteger.Parse("1298074214633706835075030044377087");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="numOfShares"></param>
        /// <returns></returns>
        public static List<double[]> AllOrNothingSecretSharing(this double[] vector, int numOfShares)
        {
            int p = 17; // modulo base

            List<double[]> shares = new List<double[]>();
            double[] sharesSum = new double[vector.Length];

            for (int i = 0; i < numOfShares - 1; i++)
            {
                double[] share = new double[vector.Length];
                var random = new Random();
                for (int j = 0; j < vector.Length; j++)
                {
                    share[j] = random.Next(0, p);
                    sharesSum[j] += share[j];
                }
                shares.Add(share);
            }

            double[] lastShare = new double[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                lastShare[i] = vector[i] - (sharesSum[i] % p);
                if (lastShare[i] < 0)
                {
                    lastShare[i] += p;
                }
            }
            shares.Add(lastShare);
            return shares;
        }



        /// <summary>
        /// Shamir's secret sharing using a third-party nuget
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="numOfSharesToMake"></param>
        /// <param name="numOfSharesForRecovery"></param>
        /// <returns>A PrimeAndShare for each of the mediators</returns>
        public static List<Coordinate[]> ShamirSecretSharing(this double[] vector, int numOfMediators)
        {
            int numOfSharesForRecovery = (numOfMediators - 1) / 2;

            var shares = new List<Coordinate[]>();
            for (int i = 0; i < numOfMediators; i++)
            {
                shares.Add(new Coordinate[vector.Length]);
            }

            var shamirScheme = new ShamirSecretSharingScheme();

            int shareCount = 0;
            foreach (double entry in vector)
            {
                // Multiply by 2 because secret sharing doesnt work on fractions
                List<Coordinate> entryShares = shamirScheme.Shamir(new BigInteger(entry * 2), PRIME, numOfMediators, numOfSharesForRecovery, false);


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

        public static double[] ReconstructShamirSecret(List<Coordinate[]> shares)
        {
            double[] secretVector = new double[shares[0].Length];
            var shamirScheme = new ShamirSecretSharingScheme();

            for (int shareCount = 0; shareCount < shares[0].Length; shareCount++)
            {
                List<Coordinate> coordinates = new List<Coordinate>();
                for (int indexCount = 0; indexCount < shares.Count; indexCount++)
                {
                    coordinates.Add(shares[indexCount][shareCount]);
                }
                var secret = shamirScheme.deShamir(coordinates, PRIME);

                secretVector[shareCount] = (double)secret / 2;
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

            //divide by 4 because we multiply each entry of the shares of cl and cm by 2
            return (double)secret / 4;
        }

        /// <summary>
        /// Calculates the similarity matrix based on Protocol 1
        /// </summary>
        /// <param name="trainingUserItemMatrix">The user-item matrix</param>
        /// <param name="numOfMediators">D</param>
        /// <returns></returns>
        public static double[,] CalcSimilarityMatrix(double[,] trainingUserItemMatrix, int numOfMediators)
        {
            int items = trainingUserItemMatrix.GetLength(1);
            double[,] similarityMatrix = new double[items, items];

            for (int i = 0; i < items; i++)
            {
                for (int j = i + 1; j < items; j++)
                {
                    double[] cl = trainingUserItemMatrix.GetVerticalVector(i);
                    double[] cm = trainingUserItemMatrix.GetVerticalVector(j);

                    var clShares = cl.ShamirSecretSharing(numOfMediators);
                    var cmShares = cm.ShamirSecretSharing(numOfMediators);
                    double z1 = ScalarProductShares(clShares, cmShares);


                    double[] clPow = Array.ConvertAll(cl, x => x * x);
                    double[] xiCm = Array.ConvertAll(cm, x => x == 0 ? (double)0 : 1);

                    var clPowShares = clPow.ShamirSecretSharing(numOfMediators);
                    var xiCmShares = xiCm.ShamirSecretSharing(numOfMediators);
                    double z2 = ScalarProductShares(clPowShares, xiCmShares);


                    double[] xiCl = Array.ConvertAll(cl, x => x == 0 ? (double)0 : 1);
                    double[] cmPow = Array.ConvertAll(cm, x => x * x);

                    var xiClShares = xiCl.ShamirSecretSharing(numOfMediators);
                    var cmPowShares = cmPow.ShamirSecretSharing(numOfMediators);
                    double z3 = ScalarProductShares(xiClShares, cmPowShares);

                    double similarityScore = 0;
                    if (z2 * z3 != 0)
                    {
                        similarityScore = z1 / (Math.Sqrt(z2 * z3));
                    }
                    similarityMatrix[i, j] = similarityScore;
                    similarityMatrix[j, i] = similarityScore;
                }
            }
            return similarityMatrix;
        }
    }
}
