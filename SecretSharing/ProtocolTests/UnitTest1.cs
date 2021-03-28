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
        public void TestPredictRating()
        {
            // ARRANGE
            int k = 2;
            int D = 5;
            int M = 3;
            int q = 80;

            int n = 1;
            int m = 1;

            var userItemMatrix = new sbyte[3, 3] { { 1, 2, 3 }, { 4, 0, 5 }, { 4, 2, 1 } };
            var R_ks = Protocols.SplitUserItemMatrixBetweenVendors(userItemMatrix, k);

            SimilarityMatrixAndShares smas = Protocols.CalcSimilarityMatrix(R_ks, D);
            var similarityMatrix = smas.SimilarityMatrix;
            var RShares = smas.RShares;
            var XiRShares = smas.XiRShares;

            // ACT

            double[] averageRatings = new double[M];
            for (int itemIndex = 0; itemIndex < M; itemIndex++)
            {
                averageRatings[itemIndex] = Protocols.ComputeAverageRating(RShares, XiRShares, itemIndex);
            }

            var sm = Protocols.GetSimilarityVectorForTopSimilarItemsToM(similarityMatrix, m, q, true);

            List<double[]> RnShares = new List<double[]>();
            for (int shareCount = 0; shareCount < RShares.Count; shareCount++)
            {
                RnShares.Add(RShares[shareCount].GetHorizontalVector(n));
            }
            double Unm = Protocols.MultiplySharesByVector(RnShares, sm);

            List<double[]> XiRnShares = new List<double[]>();
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

            predictedRating = (int)Math.Round(predictedRating, 0);

            if (predictedRating > 5)
            {
                predictedRating = 5;
            }
            else if (predictedRating < 0)
            {
                predictedRating = 0;
            }

            // ASSERT
            var expected = Protocols.GetPredictedRatingNoCrypto(userItemMatrix, n, m, q);
            Assert.Equal(expected, predictedRating);
        }

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
        public void TestCalcSimilarityMatrix3MediatorsOld()
        {
            // ARRANGE
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 2, 5 }, { 3, 4 } };
            int[] itemsVendorIndex = new int[2] { 0, 1 };

            //ACT
            var similarityMatrix = Protocols.CalcSimilarityMatrixOld(userItemMatrix, 3, itemsVendorIndex);
            var similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);

            //ASSERT
            double Q = 100;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (double)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestCalcSimilarityMatrix3Mediators()
        {
            // ARRANGE
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 2, 5 }, { 3, 4 } };

            //ACT
            var R_ks = Protocols.SplitUserItemMatrixBetweenVendors(userItemMatrix, 2);

            var similarityMatrix = Protocols.CalcSimilarityMatrix(R_ks, 3).SimilarityMatrix;
            var similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);

            //ASSERT
            double Q = 100;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (double)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestCalcSimilarityMatrix5MediatorsOld()
        {
            // ARRANGE
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 2, 5 }, { 3, 4 } };
            int[] itemsVendorIndex = new int[2] { 0, 1 };

            //ACT
            var similarityMatrix = Protocols.CalcSimilarityMatrixOld(userItemMatrix, 5, itemsVendorIndex);
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
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 2, 5 }, { 3, 4 } };

            //ACT
            var R_ks = Protocols.SplitUserItemMatrixBetweenVendors(userItemMatrix, 2);

            var similarityMatrix = Protocols.CalcSimilarityMatrix(R_ks, 5).SimilarityMatrix;
            var similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);

            //ASSERT
            double Q = 100;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (double)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestCalcSimilarityMatrix7MediatorsOld()
        {
            // ARRANGE
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 2, 5 }, { 3, 4 } };
            int[] itemsVendorIndex = new int[2] { 0, 1 };

            //ACT
            var similarityMatrix = Protocols.CalcSimilarityMatrixOld(userItemMatrix, 7, itemsVendorIndex);
            var similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);

            //ASSERT
            double Q = 100;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (double)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestCalcSimilarityMatrix7Mediators()
        {
            // ARRANGE
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 2, 5 }, { 3, 4 } };

            //ACT
            var R_ks = Protocols.SplitUserItemMatrixBetweenVendors(userItemMatrix, 2);

            var similarityMatrix = Protocols.CalcSimilarityMatrix(R_ks, 7).SimilarityMatrix;
            var similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);

            //ASSERT
            double Q = 100;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (double)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestCalcSimilarityMatrix9MediatorsOld()
        {
            // ARRANGE
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 2, 5 }, { 3, 4 } };
            int[] itemsVendorIndex = new int[2] { 0, 1 };

            //ACT
            var similarityMatrix = Protocols.CalcSimilarityMatrixOld(userItemMatrix, 9, itemsVendorIndex);
            var similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);

            //ASSERT
            double Q = 100;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (double)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestCalcSimilarityMatrix9Mediators()
        {
            // ARRANGE
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 2, 5 }, { 3, 4 } };

            //ACT
            var R_ks = Protocols.SplitUserItemMatrixBetweenVendors(userItemMatrix, 2);

            var similarityMatrix = Protocols.CalcSimilarityMatrix(R_ks, 9).SimilarityMatrix;
            var similarityMatrixNoCrypto = Protocols.CalcSimilarityMatrixNoCrypto(userItemMatrix);

            //ASSERT
            double Q = 100;
            var similarityScore = (2 * 5 + 3 * 4) / Math.Sqrt((4 + 9) * (25 + 16));
            var integeredSimilarityScore = (double)Math.Floor((similarityScore * Q) + 0.5);
            Assert.Equal(integeredSimilarityScore, similarityMatrix[0, 1]);
        }

        [Fact]
        public void TestShamirReconstruction()
        {
            // ARRANGE
            double[] vector = new double[1] { 9 };
            var shares = Protocols.ShamirSecretSharing(vector, 5);
            List<ulong> coordinates = new List<ulong>();
            foreach (var share in shares)
            {
                coordinates.Add((ulong)share[0]);
            }

            // ACT
            double secret = 0;
            if (coordinates.Count == 5)
            {
                secret = ((5 * (coordinates[0] - coordinates[3])) - (10 * (coordinates[1] - coordinates[2])) + coordinates[4]) % Protocols.PRIME;
            }

            if (secret < 0)
            {
                secret += Protocols.PRIME;
            }

            // ASSERT
            Assert.Equal(vector[0], secret);
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
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 2, 5 }, { 3, 4 } };
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
            sbyte[,] userItemMatrix = new sbyte[2, 2] { { 0, 5 }, { 3, 4 } };
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
        public void TestAverageRating()
        {
            // ARRANGE
            sbyte[,] matrix = new sbyte[5, 1] { { 0 }, { 1 }, { 2 }, { 3 }, { 4 } };
            sbyte[,] xiMatrix = new sbyte[5, 1] { { 0 }, { 1 }, { 1 }, { 1 }, { 1 } };
            double realAverage = 2.5;
            var RShares = Protocols.ShamirSecretSharingMatrix(matrix, 3);
            var xiRShares = Protocols.ShamirSecretSharingMatrix(xiMatrix, 3);

            // ACT
            double average = Protocols.ComputeAverageRating(RShares, xiRShares, 0);

            // ASSERT
            Assert.Equal(realAverage, average);
        }

        [Fact]
        public void TestVectorObfuscation()
        {
            // ARRANGE
            sbyte[,] matrix = new sbyte[3, 3] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
            var shares = Protocols.ShamirSecretSharingMatrix(matrix, 5);
            var cm = matrix.GetVerticalVector(0).Select(o => (double)o).ToArray();
            var cl = matrix.GetVerticalVector(1).Select(o => (double)o).ToArray();
            var scalarProduct = Protocols.ScalarProductVectors(cm, cl);

            // ACT
            var ObfuscatedShares = Protocols.ObfuscateShares(shares);
            var cmShares = ObfuscatedShares.Select(o => o.GetVerticalVector(0)).ToList();
            var clShares = ObfuscatedShares.Select(o => o.GetVerticalVector(1)).ToList();
            var actualScalarProduct = Protocols.ScalarProductShares(cmShares, clShares);

            // ASSERT
            Assert.Equal(scalarProduct, actualScalarProduct);
        }

        [Fact]
        public void TestVectorObfuscationSum()
        {
            // ARRANGE
            int D = 5;
            sbyte[,] userItemMatrix = new sbyte[2, 3] { { 2, 5, 3 }, { 3, 4, 5 } };
            int[] itemsVendorIndex = new int[3] { 0, 1, 2 };

            var XiRShares = Protocols.SecretShareXiR(userItemMatrix, D);

            double sum1 = 0;
            for (int i = 0; i < 5; i++)
            {
                sum1 += XiRShares[i][0][0];
            }
            sum1 %= Protocols.PRIME;

            // ACT

            List<double[]>[] ObfuscatedXiRShares = Protocols.ObfuscateSharesOld(XiRShares);


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
            sbyte[,] userItemMatrix = new sbyte[4, 4] { { 1, 2, 0, 3 }, { 3, 4, 1, 5 }, { 2, 3, 3, 4 }, { 1, 2, 3, 2 } };
            int[] itemsVendorIndex = new int[4] { 0, 0, 1, 1 };

            double x_dSum = 0;
            double y_dSum = 0;

            // ACT
            double[,] similarityMatrix = Protocols.CalcSimilarityMatrixOld(userItemMatrix, D, itemsVendorIndex);

            var RHatShares = Protocols.SecretShareRHat(userItemMatrix, D);
            var XiRShares = Protocols.SecretShareXiR(userItemMatrix, D);

            List<double[]>[] ObfuscatedXiRShares = Protocols.ObfuscateSharesOld(XiRShares);


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
            sbyte[,] userItemMatrix = new sbyte[4, 4] { { 0, 1, 0, 0 }, { 3, 2, 5, 1 }, { 1, 3, 1, 5 }, { 4, 2, 3, 5 } };
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
            sbyte[,] Rk = new sbyte[4, 4] { { 0, 1, 0, 0 }, { 3, 2, 5, 1 }, { 1, 3, 1, 5 }, { 4, 2, 3, 5 } };

            SimilarityMatrixAndShares smas = Protocols.CalcSimilarityMatrix(new List<sbyte[,]> { Rk }, D);
            similarityMatrix = smas.SimilarityMatrix;
            var RShares = smas.RShares;
            var SqRShare = smas.SqRShares;
            var XiRShares = smas.XiRShares;

            int[] offeredItemIndecis = new int[4] { 0, 1, 2, 3 };

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

            // ASSERT
            Assert.Equal(expectedMostRecommendedItems, mostRecommendedItems);
        }

        [Fact]
        public void TestRankingPredictionOld()
        {
            // ARRANGE
            int selectedUser = 0;
            int q = 2;
            int h = 2;
            int D = 3;
            sbyte[,] userItemMatrix = new sbyte[4, 4] { { 0, 1, 0, 0 }, { 3, 2, 5, 1 }, { 1, 3, 1, 5 }, { 4, 2, 3, 5 } };
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
            var obfuscatedXiRShares = Protocols.ObfuscateSharesOld(XiRShares);

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
            sbyte[,] matrix = new sbyte[2, 5] { { 1, 0, 2, 0, 3 }, { 0, 3, 2, 0, 3 } };
            sbyte[,] expectedMatrix = new sbyte[2, 5] { { 1, 2, 2, 3, 3 }, { 2, 3, 2, 3, 3 } };
            // ACT
            var YP = Protocols.GetYPUserItemMatrix(matrix, new List<int[]> { new int[3] { 0, 1, 2 }, new int[2] { 3, 4 } }, 100);

            // ASSERT
            Assert.Equal(expectedMatrix, YP);

        }

        [Fact]
        public void TestUserItemMatrixSplit()
        {
            // ARRANGE
            sbyte[,] userItemMatrix = new sbyte[4, 4] { { 1, 2, 0, 3 }, { 3, 4, 1, 5 }, { 2, 3, 3, 4 }, { 1, 2, 3, 2 } };
            int N = userItemMatrix.GetLength(0);
            int M = userItemMatrix.GetLength(1);


            // ACT
            var R_ks = Protocols.SplitUserItemMatrixBetweenVendors(userItemMatrix, 3);

            // ASSERT
            int[,] sum = new int[N, M];
            foreach (var R_k in R_ks)
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        if (sum[i, j] == -1)
                        {
                            sum[i, j] = R_k[i, j];
                        }
                        else if (R_k[i, j] != -1)
                        {
                            sum[i, j] += R_k[i, j];
                        }
                    }
                }
            }

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    Assert.Equal(userItemMatrix[i, j], sum[i, j]);
                }
            }
        }

        [Fact]
        public void TestCalcSq()
        {
            // ARRANGE
            sbyte[,] matrix = new sbyte[3, 3] { { 1, 2, -1 }, { 3, -1, 5 }, { -1, 3, 4 } };
            sbyte[,] realSq = new sbyte[3, 3] { { 1, 4, 0 }, { 9, 0, 25 }, { 0, 9, 16 } };


            // ACT
            var sq = matrix.CalcSq();

            // ASSERT
            Assert.Equal(realSq, sq);
        }

        [Fact]
        public void TestCalcXi()
        {
            // ARRANGE
            sbyte[,] matrix = new sbyte[3, 3] { { 1, 0, -1 }, { 3, -1, 5 }, { -1, 3, 4 } };
            sbyte[,] realXi = new sbyte[3, 3] { { 1, 0, 0 }, { 1, 0, 1 }, { 0, 1, 1 } };


            // ACT
            var xi = matrix.CalcXi();

            // ASSERT
            Assert.Equal(realXi, xi);
        }
    }
}