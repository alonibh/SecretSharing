using SecretSharing;
using SecretSharingProtocol;
using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace ProtocolTests
{
    public class UnitTest1
    {
        [Fact]
        public void TestAONSecretSharingAndReconstruction()
        {
            // ARRANGE
            BigInteger[] vector = new BigInteger[5] {
                BigInteger.Parse("5554584612153615861"),
                BigInteger.Parse("15891351815151"),
                BigInteger.Parse("41110135115151"),
                BigInteger.Parse("18151818115151511"),
                BigInteger.Parse("18188151848131841")
            };

            // ACT
            var shares = Protocols.AllOrNothingSecretSharing(vector, 5);
            var secret = Protocols.ReconstructAllOrNothingSecret(shares);

            // ASSERT
            Assert.Equal(vector, secret);
        }

        [Fact]
        public void TestShamirSharingAndReconstruction()
        {
            // ARRANGE
            BigInteger[] secretVector = new BigInteger[6] { 0, 1, 2, 3, 4, 5 };

            // ACT
            var shares = Protocols.ShamirSecretSharing(secretVector, 10);
            var returnedSecretVector = Protocols.ReconstructShamirSecret(shares);

            // ASSERT
            Assert.Equal(secretVector, returnedSecretVector);

        }

        [Fact]
        public void TestCalcSimilarityMatrix()
        {
            // ARRANGE
            int[,] userItemMatrix = new int[2, 2] { { 2, 5 }, { 3, 4 } };

            //ACT
            var similarityMatrix = Protocols.CalcSimilarityMatrix(userItemMatrix, 5);

            //ASSERT
            double Q = 1296859633245;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (BigInteger)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestProductBetweenShares()
        {
            // ARRANGE
            var p = BigInteger.Parse("71");
            ShamirSecretSharingScheme sss = new ShamirSecretSharingScheme();
            BigInteger firstSecret = 4;
            BigInteger secondeSecret = 5;

            var firstShares = sss.Shamir(firstSecret, p, 5, 2, false);
            var secondShares = sss.Shamir(secondeSecret, p, 5, 2, false);

            var sumShares = new List<Coordinate>();
            for (int i = 0; i < firstShares.Count; i++)
            {
                sumShares.Add(new Coordinate(firstShares[i].X, firstShares[i].Y * secondShares[i].Y));
            }

            // ACT
            var res = sss.deShamir(sumShares, p);

            // ASSERT
            Assert.Equal(firstSecret * secondeSecret, res);
        }

        [Fact]
        public void TestScalarProductBetweenShares()
        {
            // ARRANGE
            BigInteger[] firstVector = new BigInteger[3] { 9, 1, 2 };
            BigInteger[] secondVector = new BigInteger[3] { 3, 2, 4 };
            var firstShares = Protocols.ShamirSecretSharing(firstVector, 5);
            var secondShares = Protocols.ShamirSecretSharing(secondVector, 5);

            // ACT
            var res = Protocols.ScalarProductShares(firstShares, secondShares);

            // ASSERT
            Assert.Equal(37, res);
        }

        [Fact]
        public void TestShamirVecrotScalarProductBy2()
        {
            // ARRANGE
            double secret = 17;
            var prime = BigInteger.Parse("1298074214633706835075030044377087");
            ShamirSecretSharingScheme sss = new ShamirSecretSharingScheme();
            var shares = sss.Shamir((BigInteger)secret, prime, 5, 2, false);
            foreach (var share in shares)
            {
                share.X *= 2;
                share.Y *= 2;
            }

            // ACT
            var res = sss.deShamir(shares, prime);

            // ASSERT
            Assert.Equal(secret * 2, (double)res);
        }

        [Fact]
        public void TestSecretShareRHat()
        {
            // ARRANGE
            int[,] userItemMatrix = new int[2, 2] { { 2, 5 }, { 3, 4 } };
            int D = 5;

            // ACT
            var RhatShares = Protocols.SecretShareRHat(userItemMatrix, D);

            // ASSERT
            List<BigInteger[]> Rhat_0Shares = new List<BigInteger[]>();
            for (int i = 0; i < D; i++)
            {
                Rhat_0Shares.Add(RhatShares[i][0]);
            }
            var Rhat_0 = Protocols.ReconstructRHatSecret(Rhat_0Shares);

            Assert.Equal(new double[2] { -0.5, 0.5 }, Rhat_0);
        }

        [Fact]
        public void TestSecretShareXiR()
        {
            // ARRANGE
            int[,] userItemMatrix = new int[2, 2] { { 0, 5 }, { 3, 4 } };
            int D = 5;

            // ACT
            var xiRShares = Protocols.SecretShareXiR(userItemMatrix, D);

            // ASSERT
            List<BigInteger[]> xiR0Shares = new List<BigInteger[]>();
            for (int i = 0; i < D; i++)
            {
                xiR0Shares.Add(xiRShares[i][0]);
            }
            var xiR0 = Protocols.ReconstructAllOrNothingSecret(xiR0Shares);

            Assert.Equal(new BigInteger[2] { 0, 1 }, xiR0);
        }

        [Fact]
        public void TestVectorObfuscationSum()
        {
            // ARRANGE
            int[,] userItemMatrix = new int[2, 3] { { 2, 5, 3 }, { 3, 4, 5 } };
            int D = 5;

            BigInteger[,] similarityMatrix = Protocols.CalcSimilarityMatrix(userItemMatrix, D);

            var XiRShares = Protocols.SecretShareXiR(userItemMatrix, D);

            BigInteger sum1 = 0;
            for (int i = 0; i < 5; i++)
            {
                sum1 += XiRShares[i][0][0];
            }
            sum1 %= Protocols.PRIME;

            // ACT

            List<BigInteger[]>[] ObfuscatedXiRShares = Protocols.ObfuscateShares(XiRShares);


            BigInteger sum2 = 0;
            for (int i = 0; i < 5; i++)
            {
                sum2 += ObfuscatedXiRShares[i][0][0];
            }
            sum2 %= Protocols.PRIME;

            // ASSERT
            Assert.Equal(sum1, sum2);
        }

        [Fact]
        public void TestShamirAddBy2()
        {
            // ARRANGE
            double secret = 17;
            var prime = BigInteger.Parse("1298074214633706835075030044377087");
            ShamirSecretSharingScheme sss = new ShamirSecretSharingScheme();
            var shares = sss.Shamir((BigInteger)secret, prime, 5, 2, false);
            foreach (var share in shares)
            {
                share.Y += 2;
            }

            // ACT
            var res = sss.deShamir(shares, prime);

            // ASSERT
            Assert.Equal(secret + 2, (double)res);
        }

        [Fact]
        public void TestGetMostSimilarItemsToM()
        {
            // ARRANGE
            var similarityMatrix = new BigInteger[4, 4] { { 0, 2, 3, 5 }, { 2, 0, 4, 1 }, { 3, 4, 0, 2 }, { 5, 1, 2, 0 } };

            // ACT
            BigInteger[] items = Protocols.GetMostSimilarItemsToM(similarityMatrix, 1, 2, true);

            // ASSERT
            Assert.Equal(items, new BigInteger[4] { 2, 0, 4, 0 });
        }

        [Fact]
        public void TestRatingPrediction()
        {
            // ARRANGE
            int[,] userItemMatrix = new int[4, 4] { { 1, 2, 0, 3 }, { 3, 4, 1, 5 }, { 2, 3, 3, 4 }, { 1, 2, 3, 2 } };
            int n = 0;
            int m = 2;
            int D = 5;
            int q = 2;

            BigInteger x_dSum = 0;
            BigInteger y_dSum = 0;

            // ACT
            BigInteger[,] similarityMatrix = Protocols.CalcSimilarityMatrix(userItemMatrix, D);

            var RHatShares = Protocols.SecretShareRHat(userItemMatrix, D);
            var XiRShares = Protocols.SecretShareXiR(userItemMatrix, D);

            List<BigInteger[]>[] ObfuscatedXiRShares = Protocols.ObfuscateShares(XiRShares);


            var sm = Protocols.GetMostSimilarItemsToM(similarityMatrix, m, q, true);
            foreach (var RHatShare in RHatShares)
            {
                BigInteger[] RHat_n = RHatShare.GetHorizontalVector(n);
                BigInteger x_d = Protocols.ScalarProductVectors(RHat_n, sm);
                x_dSum += x_d;
            }
            foreach (var obfuscatedXiRShare in ObfuscatedXiRShares)
            {
                BigInteger[] XiR_n = obfuscatedXiRShare.GetHorizontalVector(n);
                BigInteger y_d = Protocols.ScalarProductVectors(XiR_n, sm);
                y_dSum += y_d;
            }

            x_dSum %= Protocols.PRIME;
            y_dSum %= Protocols.PRIME;

            var averageRating = userItemMatrix.GetAverageRatings()[m];
            double predictedRating = averageRating;
            double change = 0;
            if (y_dSum != 0)
            {
                change = (double)(x_dSum / y_dSum) / Protocols.Q;
                // if thats true then x_sum is negative
                if (change > 5)
                {
                    x_dSum = Protocols.PRIME - x_dSum;
                    change = (double)(x_dSum / y_dSum) / Protocols.Q * -1;
                }
                predictedRating += change;
            }
            predictedRating = Math.Round(predictedRating, 0);

            int expectedRating = Protocols.GetPredictedRatingNoCrypto(userItemMatrix, n, m, q);

            // ASSERT
            Assert.Equal(expectedRating, predictedRating);
        }
    }
}