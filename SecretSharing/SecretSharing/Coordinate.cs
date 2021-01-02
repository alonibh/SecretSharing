using System.Numerics;

namespace SecretSharingProtocol
{
    public class Coordinate
    {
        public BigInteger X { get; set; }
        public BigInteger Y { get; set; }

        public Coordinate(BigInteger x, BigInteger y)
        {
            X = x;
            Y = y;
        }
    }
}
