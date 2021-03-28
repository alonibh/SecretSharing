using System;
using System.Collections.Generic;

namespace SecretSharing
{
    public class SimilarityMatrixAndShares
    {
        public double[,] SimilarityMatrix { get; set; }
        public List<uint[,]> RShares { get; set; }
        public List<uint[,]> SqRShares { get; set; }
        public List<uint[,]> XiRShares { get; set; }
        public TimeSpan EachMediatorTime { get; set; }
        public TimeSpan VendorTime { get; set; }
    }
}