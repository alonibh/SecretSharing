using SecretSharing;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ProtocolTests
{
    public class UnitTest1
    {
        [Fact]
        public void TestAONSecretSharingAndReconstruction()
        {
            // ARRANGE
            double[] vector = new double[5] {
                double.Parse("214748647"),
                double.Parse("214743647"),
                double.Parse("214783647"),
                double.Parse("214483647"),
                double.Parse("217483647")
            };

            // ACT
            var shares = Protocols.AllOrNothingSecretSharing(vector, 5);
            var secret = Protocols.ReconstructAllOrNothingSecret(shares);

            // ASSERT
            Assert.Equal(vector, secret);
        }

        [Fact]
        public void TestCalcSimilarityMatrix3Mediators()
        {
            // ARRANGE
            int[,] userItemMatrix = new int[2, 2] { { 2, 5 }, { 3, 4 } };
            int[] itemsVendorIndex = new int[2] { 0, 1 };

            //ACT
            var similarityMatrix = Protocols.CalcSimilarityMatrix(userItemMatrix, 3, itemsVendorIndex);
            var similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);

            //ASSERT
            double Q = 100;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (double)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestCalcSimilarityMatrix5Mediators()
        {
            // ARRANGE
            int[,] userItemMatrix = new int[2, 2] { { 2, 5 }, { 3, 4 } };
            int[] itemsVendorIndex = new int[2] { 0, 1 };

            //ACT
            var similarityMatrix = Protocols.CalcSimilarityMatrix(userItemMatrix, 5, itemsVendorIndex);
            var similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);

            //ASSERT
            double Q = 100;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (double)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestScalarProductBetweenShares()
        {
            // ARRANGE
            double[] firstVector = new double[3] { 9, 1, 2 };
            double[] secondVector = new double[3] { 3, 2, 4 };
            var firstShares = Protocols.ShamirSecretSharing(firstVector, 5);
            var secondShares = Protocols.ShamirSecretSharing(secondVector, 5);

            // ACT
            var res = Protocols.ScalarProductShares(firstShares, secondShares);

            // ASSERT
            Assert.Equal(37, res);
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
            List<double[]> Rhat_0Shares = new List<double[]>();
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
            List<double[]> xiR0Shares = new List<double[]>();
            for (int i = 0; i < D; i++)
            {
                xiR0Shares.Add(xiRShares[i][0]);
            }
            var xiR0 = Protocols.ReconstructAllOrNothingSecret(xiR0Shares);

            Assert.Equal(new double[2] { 0, 1 }, xiR0);
        }

        [Fact]
        public void TestVectorObfuscationSum()
        {
            // ARRANGE
            int D = 5;
            int[,] userItemMatrix = new int[2, 3] { { 2, 5, 3 }, { 3, 4, 5 } };
            int[] itemsVendorIndex = new int[3] { 0, 1, 2 };


            double[,] similarityMatrix = Protocols.CalcSimilarityMatrix(userItemMatrix, D, itemsVendorIndex);

            var XiRShares = Protocols.SecretShareXiR(userItemMatrix, D);

            double sum1 = 0;
            for (int i = 0; i < 5; i++)
            {
                sum1 += XiRShares[i][0][0];
            }
            sum1 %= Protocols.PRIME;

            // ACT

            List<double[]>[] ObfuscatedXiRShares = Protocols.ObfuscateShares(XiRShares);


            double sum2 = 0;
            for (int i = 0; i < 5; i++)
            {
                sum2 += ObfuscatedXiRShares[i][0][0];
            }
            sum2 %= Protocols.PRIME;

            // ASSERT
            Assert.Equal(sum1, sum2);
        }

        [Fact]
        public void TestGetMostSimilarItemsToM()
        {
            // ARRANGE
            var similarityMatrix = new double[4, 4] { { 0, 2, 3, 5 }, { 2, 0, 4, 1 }, { 3, 4, 0, 2 }, { 5, 1, 2, 0 } };

            // ACT
            double[] items = Protocols.GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, 1, 2, true);

            // ASSERT
            Assert.Equal(items, new double[4] { 2, 0, 4, 0 });
        }

        [Fact]
        public void TestRatingPrediction()
        {
            // ARRANGE
            int n = 0;
            int m = 2;
            int D = 5;
            int q = 2;
            int[,] userItemMatrix = new int[4, 4] { { 1, 2, 0, 3 }, { 3, 4, 1, 5 }, { 2, 3, 3, 4 }, { 1, 2, 3, 2 } };
            int[] itemsVendorIndex = new int[4] { 0, 0, 1, 1 };

            double x_dSum = 0;
            double y_dSum = 0;

            // ACT
            double[,] similarityMatrix = Protocols.CalcSimilarityMatrix(userItemMatrix, D, itemsVendorIndex);

            var RHatShares = Protocols.SecretShareRHat(userItemMatrix, D);
            var XiRShares = Protocols.SecretShareXiR(userItemMatrix, D);

            List<double[]>[] ObfuscatedXiRShares = Protocols.ObfuscateShares(XiRShares);


            var sm = Protocols.GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, m, q, true);
            foreach (var RHatShare in RHatShares)
            {
                double[] RHat_n = RHatShare.GetHorizontalVector(n);
                double x_d = Protocols.ScalarProductVectors(RHat_n, sm);
                x_dSum += x_d;
            }
            foreach (var obfuscatedXiRShare in ObfuscatedXiRShares)
            {
                double[] XiR_n = obfuscatedXiRShare.GetHorizontalVector(n);
                double y_d = Protocols.ScalarProductVectors(XiR_n, sm);
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

        [Fact]
        public void TestRankingPrediction()
        {
            // ARRANGE
            int selectedUser = 0;
            int q = 2;
            int h = 2;
            int D = 3;
            int[,] userItemMatrix = new int[4, 4] { { 0, 1, 0, 0 }, { 3, 2, 5, 1 }, { 1, 3, 1, 5 }, { 4, 2, 3, 5 } };
            var similarityMatrix = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);
            var userRatings = userItemMatrix.GetHorizontalVector(selectedUser);
            int numOfItems = userRatings.Length;
            var sHat = new double[numOfItems];
            int i = 0;

            #region No Crypto

            for (i = 0; i < numOfItems; i++)
            {
                if (userRatings[i] == 0) // only if this item has not been rated yet
                {
                    double[] similarityVectorForMostSimilarItems = Protocols.GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, i, q, false);
                    for (int j = 0; j < numOfItems; j++)
                    {
                        if (similarityVectorForMostSimilarItems[j] != 0 && userRatings[j] != 0)
                        {
                            sHat[i] += similarityMatrix[i, j];
                        }
                    }
                }
            }

            Tuple<double, int>[] sHatScoreAndIndex = new Tuple<double, int>[numOfItems];

            for (i = 0; i < numOfItems; i++)
            {
                sHatScoreAndIndex[i] = new Tuple<double, int>(sHat[i], i);
            }

            Array.Sort(sHatScoreAndIndex, new ScoreAndIndexComparer());

            var expectedMostRecommendedItems = sHatScoreAndIndex.Take(h).Select(o => o.Item2).ToArray();

            #endregion

            // ACT

            var XiRShares = Protocols.SecretShareXiR(userItemMatrix, D);
            var obfuscatedXiRShares = Protocols.ObfuscateShares(XiRShares);

            double[] x = new double[numOfItems];
            double[] y = new double[numOfItems];

            i = 0;
            foreach (var itemIndex in Enumerable.Range(0, 4))
            {
                var sm = Protocols.GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, itemIndex, q, false);

                foreach (var obfuscatedXiRShare in obfuscatedXiRShares)
                {
                    double[] XiR_n = obfuscatedXiRShare.GetHorizontalVector(selectedUser);
                    double x_d = Protocols.ScalarProductVectors(XiR_n, sm);
                    x[i] += x_d;

                    y[i] += XiR_n[itemIndex];

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
            int[] mostRecommendedItems = valueAndIndexArray.Take(h).Select(o => o.Item2).ToArray();

            // ASSERT
            Assert.Equal(expectedMostRecommendedItems, mostRecommendedItems);
        }

        [Fact]
        public void TestYPRatingPredictionMatrix()
        {

            // ARRANGE
            int[,] matrix = new int[2, 5] { { 1, 0, 2, 0, 3 }, { 0, 3, 2, 0, 3 } };
            int[,] expectedMatrix = new int[2, 5] { { 1, 2, 2, 3, 3 }, { 2, 3, 2, 3, 3 } };
            // ACT
            var YP = Protocols.GetYPUserItemMatrix(matrix, new List<int[]> { new int[3] { 0, 1, 2 }, new int[2] { 3, 4 } }, 100);

            // ASSERT
            Assert.Equal(expectedMatrix, YP);

        }
    }
}