using SecretSharingProtocol;
using System.Collections.Generic;
using System.Numerics;

namespace SecretSharing
{
    class Program
    {
        static void Main(string[] args)
        {
            int N = 10; //users
            int M = 50; //items
            int D = 5; //mediators
            // int k = 7; //vendors

            int[,] userItemMatrix = _2DArrayExtensions.CreateRandomUserItemMatrix(N, M, 500, 1, 5, 1);

            var sets = userItemMatrix.SplitToTrainingAndTesting();
            var trainingUserItemMatrix = sets.Item1;
            var testingUserItemMatrix = sets.Item2;

            // 6 Vendors with 7 items each and another and the last one with 8
            // var vendorsMatrices = trainingUserItemMatrix.SplitToVendors(k);

            #region Computing the similarity matrix (Protocol 1+2)

            BigInteger[,] similarityMatrix = Protocols.CalcSimilarityMatrix(trainingUserItemMatrix, D);

            #endregion

            #region Secret sharing R_hat and xiR using AON (Protocol 3)

            var RHatShares = Protocols.SecretShareRHat(trainingUserItemMatrix, D);
            var XiRShares = Protocols.SecretShareXiR(trainingUserItemMatrix, D);

            #endregion

            #region Obfuscate the shares of xiR (Protocol 4)

            List<BigInteger[]>[] ObfuscatedXiRShares = new List<BigInteger[]>[D];
            for (int i = 0; i < D; i++)
            {
                ObfuscatedXiRShares[i] = Protocols.ObfuscateShares(XiRShares[i]);
            }

            #endregion


            #region Predict rating (Protocol 5)
            int n = 5;
            int m = 10;
            BigInteger x_dSum = 0;
            BigInteger y_dSum = 0;

            var sm = Protocols.GetMostSimilarItemsToM(similarityMatrix, 2, 5);
            foreach (var RHatShare in RHatShares)
            {
                BigInteger[] RHat_n = RHatShare.GetHorizontalVector(n);
                BigInteger x_d = Protocols.ScalarProductVectors(RHat_n, sm);
                x_dSum += x_d;
            }
            foreach (var ObfuscatedXiRShare in ObfuscatedXiRShares)
            {
                BigInteger[] XiR_n = ObfuscatedXiRShare.GetHorizontalVector(n);
                BigInteger y_d = Protocols.ScalarProductVectors(XiR_n, sm);
                y_dSum += y_d;
            }

            x_dSum %= Protocols.PRIME;
            y_dSum %= Protocols.PRIME;

            var averageRating = trainingUserItemMatrix.GetAverageRatings()[m];
            double predictedRating = averageRating;
            if (y_dSum != 0)
            {
                predictedRating += (double)(x_dSum / y_dSum) / Protocols.Q;
            }

            #endregion
        }
    }
}
