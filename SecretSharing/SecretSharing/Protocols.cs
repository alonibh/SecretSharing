using System;
using System.Collections.Generic;

namespace SecretSharing
{
    public static class Protocols
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="numOfShares"></param>
        /// <param name="p">The field size</param>
        /// <returns></returns>
        public static List<double[]> AllOrNothingSecretSharing(this double[] vector, int numOfShares, int p)
        {
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
    }
}
