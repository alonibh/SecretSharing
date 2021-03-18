using System.Collections.Generic;

namespace SecretSharing
{
    public class SimilarityMatrixAndShares
    {
        public List<double[,]> RShares { get;set; }
        public List<double[,]> SqRShares { get; set; }
        public List<double[,]> XiRShares { get; set; }
        public double[,] SimilarityMatrix { get; set; }
    }
}