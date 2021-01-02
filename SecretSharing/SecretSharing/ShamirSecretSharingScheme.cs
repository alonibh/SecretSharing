using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

namespace SecretSharingProtocol
{
    public class ShamirSecretSharingScheme
    {
        //--------------------------------------------------------------
        // Shamir secret sharing
        //--------------------------------------------------------------
        public List<Coordinate> Shamir(BigInteger secret, BigInteger p, int n, int k, bool is_random_x)
        {
            BigInteger[] Koeff = new BigInteger[k - 1];
            BigInteger[] rand_x = new BigInteger[n];

            Dictionary<BigInteger, BigInteger> total_keys = new Dictionary<BigInteger, BigInteger>();
            Random rnd = new Random();

            // --------------------------------------
            // Creating random koeff a[i]. That will be deleted after creating of shades
            // --------------------------------------
            for (int i = 0; i < k - 1; i++) // gets random a(n), n = 1 ... (k - 1)
            {
                Koeff[i] = MathMod(BigInteger.Abs(getRandom((i + 1) * 2)), p);
            }

            // --------------------------------------
            // creating random X-part of shades ( N times, for N peoples)
            // --------------------------------------
            if (is_random_x)
            {
                for (int i = 0; i < n; i++) // gets random x, x count = n
                {
                    rand_x[i] = MathMod(rnd.Next(1, int.MaxValue - 1), p); // p - BigInt, rand_x[i] - int, can't be rnd.Next of 2 type of value :c
                }
            }
            else
            {
                for (int i = 0; i < n; i++) // such non-random!
                {
                    rand_x[i] = MathMod((i + 1), p);
                }
            }

            // --------------------------------------
            // starting Shamir secret sharing
            // --------------------------------------
            BigInteger result = new BigInteger();
            BigInteger powered = new BigInteger();

            for (int i = 0; i < n; i++)
            {
                result = result + secret;

                for (int j = 1; j < k; j++) // calculate polynomial function for current i, where i = 1 .. n
                {
                    powered = BigInteger.Pow(rand_x[i], k - j);
                    result += Koeff[j - 1] * powered;
                }

                result = MathMod(result, p);

                if (!total_keys.ContainsKey(rand_x[i]))
                {
                    total_keys.Add(rand_x[i], result);
                }

                result = 0; // set null for next calculates
            }

            List<Coordinate> coordinates = new List<Coordinate>();
            foreach(var kvp in total_keys)
            {
                coordinates.Add(new Coordinate(kvp.Key, kvp.Value));
            }

            return coordinates; // return pair of (x, y) points
        }

        //--------------------------------------------------------------
        // Recovering secret by some shades
        // for theoretical GO TO WIKIPEDIA, reference - shamir secret sharing, lagrange interpolation
        //--------------------------------------------------------------
        public BigInteger deShamir(List<Coordinate> coordinates, BigInteger p)
        {
            int k = coordinates.Count;

            BigInteger secret = new BigInteger(0);
            BigInteger high_part = new BigInteger(1);   // high part of lagrange intepolation
            BigInteger low_part = new BigInteger(1);    // low part of lagrange interpolation

            // first_kv - shade number 1
            // second_kv - shade number 2
            foreach (var first_coordinate in coordinates)
            {
                foreach (var second_coordinate in coordinates)
                {
                    if (first_coordinate.X != second_coordinate.X)
                    {
                        high_part = high_part * ((-1) * second_coordinate.X); // -1 caused that we need x - xj, but our x = 0, that now (-xj)
                        low_part = low_part * (first_coordinate.X - second_coordinate.X); // calculate low part
                    }
                }

                // Lagrange interpolation have division, but we need only integer numbers, so
                //  a * a^(-1) = 1
                // but we can make some fun and:
                //  a * a^(-1) = 1 (mod p)
                // so, when we have modulo, we can change a^(-1) to another number, that give us similar result ^.^
                //  a * b = 1 (mod p).
                // b can be calculated by Evklid algorithm in form 'ax + by = gcd(a,b)'  (*1)
                // so we set 2 numbers, a = low_part and b = p, and gets gcd and 2 numbers, x and y.
                // We know that gcd(prime_number, any_number) = 1, so change (*1) to our rules:
                // low_part * x + p*y = 1
                // In this form y always be the '0' (ave MATH!), so we get
                // low_part*x = 1
                // where x - our (low_part)^(-1)
                // MAGIIIIIIIIIIC!
                iVector temp = Evklid(low_part, p);

                low_part = MathMod(temp.y, p);

                high_part = MathMod((high_part * low_part), p); // let high part temporary storage all lart of 'li' (see lagrange interpolation)

                secret += high_part * first_coordinate.Y; // summ_all( y * li )
                high_part = 1;
                low_part = 1;
            }
            secret = MathMod(secret, p); // Let restrict out sercet by out square. 
            return secret; // Done. You are delicious ^_^
        }


        //--------------------------------------------------------------
        // Other Help Functions
        //--------------------------------------------------------------

        // Allow to get from string KV-pair of shades
        private List<Coordinate> SplitPair(string input_string)
        {
            Dictionary<BigInteger, BigInteger> return_value = new Dictionary<BigInteger, BigInteger>();

            Regex a = new Regex(@"\[\d*\]\s|\(|\)"); // remove '[<number>] ' from string and remove '(' and ')'
            input_string = a.Replace(input_string, "");

            a = new Regex(@"\n"); // split big string by '\n'
            string[] arr = a.Split(input_string);

            a = new Regex(@"\,\s"); // split one pair by ','

            // Save all shades(points) for dictionary. 
            foreach (string str in arr)
            {
                if (str != "")
                {
                    string[] kv = a.Split(str);
                    BigInteger key = BigInteger.Parse(kv[0]);
                    BigInteger value = BigInteger.Parse(kv[1]);
                    return_value.Add(key, value);
                }
            }

            List<Coordinate> coordinates = new List<Coordinate>();
            foreach (var kvp in return_value)
            {
                coordinates.Add(new Coordinate(kvp.Key, kvp.Value));
            }

            return coordinates;
        }

        // Getting random BigInteger value. Careful, one length = one number!
        // Function bugged, need to rewrite it ._.
        public BigInteger getRandom(int length)
        {
            Random random = new Random();
            random.Next();
            byte[] data = new byte[length];
            random.NextBytes(data);
            return new BigInteger(data);
        }

        // Mathematical modulo, that caused by C# modulo(%) is work wrong for mathematical needs.
        public static BigInteger MathMod(BigInteger a, BigInteger b)
        {
            return (BigInteger.Abs(a * b) + a) % b;
        }

        public iVector Evklid(BigInteger a, BigInteger b)
        {
            iVector u = new iVector(a, 1, 0);
            iVector v = new iVector(b, 0, 1);
            iVector T = new iVector(0, 0, 0);
            BigInteger q = 0;

            while (v.x != 0)
            {
                q = u.x / v.x;

                T.x = MathMod(u.x, v.x);

                T.y = u.y - q * v.y;
                T.z = u.z - q * v.z;

                u.set(v); // u = v, but we need to CHANGE value, not CHANGE POINTER.
                v.set(T); // u = v, but we need to CHANGE value, not CHANGE POINTER.
            }

            return u;
        }

        public class iVector
        {
            public BigInteger x;
            public BigInteger y;
            public BigInteger z;

            public iVector()
            {
                x = 0;
                y = 0;
                z = 0;
            }

            public void set(iVector sec)
            {
                this.x = sec.x;
                this.y = sec.y;
                this.z = sec.z;
            }

            public iVector(BigInteger nx, BigInteger ny, BigInteger nz)
            {
                x = nx; y = ny; z = nz;
            }

            public iVector(int nx, int ny, int nz)
            {
                x = nx; y = ny; z = nz;
            }

            public string toString()
            {
                return "(" + x + ", " + y + ", " + z + ")";
            }
        }
    }
}
