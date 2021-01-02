using System;

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

            double[,] userItemMatrix = _2DArrayExtensions.CreateRandomUserItemMatrix(N, M, 500, 1, 5, 1);

            var sets = userItemMatrix.SplitToTrainingAndTesting();
            var trainingUserItemMatrix = sets.Item1;
            var testingUserItemMatrix = sets.Item2;

            // 6 Vendors with 7 items each and another and the last one with 8
            // var vendorsMatrices = trainingUserItemMatrix.SplitToVendors(k);

            #region Computing the similarity matrix

            double[] cl = trainingUserItemMatrix.GetVerticalVector(0);
            double[] cm = trainingUserItemMatrix.GetVerticalVector(1);

            var clShares = cl.ShamirSecretSharing(D);
            var cmShares = cm.ShamirSecretSharing(D);
            double z1 = Protocols.ScalarProductShares(clShares, cmShares);


            double[] clPow = Array.ConvertAll(cl, x => x * x);
            double[] xiCm = Array.ConvertAll(cm, x =>  x==0 ? (double)0 : 1);

            var clPowShares = clPow.ShamirSecretSharing(D);
            var xiCmShares = xiCm.ShamirSecretSharing(D);
            double z2 = Protocols.ScalarProductShares(clPowShares, xiCmShares);


            double[] xiCl = Array.ConvertAll(cl, x => x == 0 ? (double)0 : 1);
            double[] cmPow = Array.ConvertAll(cm, x => x * x);

            var xiClShares = xiCl.ShamirSecretSharing(D);
            var cmPowShares = cmPow.ShamirSecretSharing(D);
            double z3 = Protocols.ScalarProductShares(xiClShares, cmPowShares);

            double similarity = 0;
            if(z2*z3 != 0)
            {
                similarity = z1 / (Math.Sqrt(z2 * z3));
            }

            #endregion

        }
    }
}
