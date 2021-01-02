using SecretSharing;
using SecretSharingProtocol;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace ProtocolTests
{
    public class UnitTest1
    {
        [Fact]
        public void TestShamirSharingAndReconstruction()
        {
            //ARRANGE
            double[] secretVector = new double[11] { 0, 0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5 };

            // ACT
            var shares = secretVector.ShamirSecretSharing(10);
            var returnedSecretVector = Protocols.ReconstructShamirSecret(shares);

            //ASSERT
            Assert.Equal(secretVector, returnedSecretVector);

        }

        [Fact]
        public void TestProductBetweenShares()
        {
            //ARRANGE
            var p = BigInteger.Parse("71");
            ShamirSecretSharingScheme sss = new ShamirSecretSharingScheme();
            BigInteger firstSecret = 4;
            BigInteger secondeSecret = 5;

            var firstShares = sss.Shamir(firstSecret, p, 5, 2, false);
            var secondShares = sss.Shamir(secondeSecret, p, 5, 2, false);

            var sumShares = new List<Coordinate>();
            for(int i = 0; i < firstShares.Count; i++)
            {
                sumShares.Add(new Coordinate(firstShares[i].X, firstShares[i].Y * secondShares[i].Y));
            }

            // ACT
            var res = sss.deShamir(sumShares, p);

            //ASSERT
            Assert.Equal(firstSecret * secondeSecret, res);
        }

        [Fact]
        public void TestScalarProductBetweenShares()
        {
            //ARRANGE
            double[] firstVector = new double[3] {0.5,1,2 };
            double[] secondVector = new double[3] {3,3,0.5 };
            var firstShares = firstVector.ShamirSecretSharing(5);
            var secondShares = secondVector.ShamirSecretSharing(5);

            // ACT
            var res = Protocols.ScalarProductShares(firstShares, secondShares);

            //ASSERT
            Assert.Equal(5.5, res);
        }

        [Fact]
        public void TestShamirVecrotScalarProductBy2()
        {
            //ARRANGE
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

            //ASSERT
            Assert.Equal(secret * 2, (double)res);
        }
    }
}
