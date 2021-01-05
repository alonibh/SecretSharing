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

            double[,] similarityMatrix = Protocols.CalcSimilarityMatrix(trainingUserItemMatrix, D);
            

            #endregion

        }
    }
}
