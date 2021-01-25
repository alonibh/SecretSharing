using System.Numerics;

namespace SecretSharingProtocol
{
    public class BigCoordinate
    {
        public double X { get; set; }
        public BigInteger Y { get; set; }

        public BigCoordinate(double x, BigInteger y)
        {
            X = x;
            Y = y;
        }
    }
}
