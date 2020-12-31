using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretSharing
{
    public static class _2DArrayExtensions
    {
        /// <summary>
        /// Split it in the vertical distribution scenario into K matrices of (almost equal) dimensions
        /// N x M_k, where M_k is eithr the upper or the lower bound of M/K
        /// </summary>
        /// <param name="numOfVendors">K</param>
        /// <returns></returns>
        public static List<double[,]> SplitToVendors(this double[,] userItemMatrix, int numOfVendors)
        {
            int N = userItemMatrix.GetLength(0);
            int M = userItemMatrix.GetLength(1);
            int size = M / numOfVendors;
            int lastSize = (M % numOfVendors) + size;
            List<double[,]> splittedUserItemMatrix = new List<double[,]>();
            for (int i = 0; i < numOfVendors; i++)
            {
                double[,] vendorUserItemMatrix;
                if (i == numOfVendors-1)
                {
                    vendorUserItemMatrix = new double[N, lastSize];
                    Array.Copy(userItemMatrix, N * i * size, vendorUserItemMatrix, 0, N * lastSize);
                    splittedUserItemMatrix.Add(vendorUserItemMatrix);
                }
                else
                {
                    vendorUserItemMatrix = new double[N, size];
                    Array.Copy(userItemMatrix, N * i * size, vendorUserItemMatrix, 0, N * size);
                    splittedUserItemMatrix.Add(vendorUserItemMatrix);
                }
            }

            return splittedUserItemMatrix;
        }
        public static double[] GetVerticalVector(this double[,] matrix, int index)
        {
            int length = matrix.GetLength(0);
            double[] vector = new double[length];
            for(int i=0;i< length; i++)
            {
                vector[i] = matrix[i, index];
            }
            return vector;
        }
    }
}
