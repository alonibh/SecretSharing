using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PaillierExt;
using Extreme.Mathematics;
using System.Diagnostics;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            double enc512 = 0.57815;
            double dec512 = 0.5567;
            double exp512 = 0.18125;

            double enc1024 = 3.6647;
            double dec1024 = 3.6258;
            double exp1024 = 1.04455;

            int[] ms = new int[4] { 1682, 3706, 10677, 26744 };
            int[] ns = new int[4] { 943, 6040, 71567, 138493 };
            int Q = 80;

            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine("---------------------");
                Console.WriteLine(2 * ms[i] * ns[i] * enc1024 / 1000);
                Console.WriteLine("---------------------");
            }



            return;

            var p = Extreme.Mathematics.BigInteger.Parse("77754835605246977810004309746331619839353207427403980724034656242668010281269");
            var q = Extreme.Mathematics.BigInteger.Parse("99772147318901127955209368155788182924982547129739383311872062863568509093191");
            var n = p * q;
            var lambda = Extreme.Mathematics.BigInteger.LeastCommonMultiple(p - 1, q - 1);
            BigInteger G = 84;
            BigInteger Lambda = new BigInteger("3878883456381820109925311894572022524349352646376909422624589631979857626326101948996913895924929602288306731199777466933268522490698285230754633761682460", 10);
            BigInteger N = new BigInteger("7757766912763640219850623789144045048698705292753818845249179263959715252652381424976751939955624418254515582202319269621094188345432477180615504042739379", 10);

            var Lx = G.modPow(Lambda, N * N);
            var Miu = ((Lx - 1) / N).modInverse(N);


            byte[] data = Enumerable.Range(0, 63).Select(o => (byte)o).ToArray();


            var encryptor = new PaillierEncryptor(
                new PaillierKeyStruct
                {
                    N = N,
                    G = G,
                    Lambda = Lambda,
                    Miu = Miu,
                    Padding = PaillierPaddingMode.LeadingZeros

                });

            byte[] encrypted = null;

            var encryptWatch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                encrypted = encryptor.ProcessData(data);
            }
            encryptWatch.Stop();
            var encryptTime = new TimeSpan(0, 0, 0, 0, (int)encryptWatch.ElapsedMilliseconds / 1000);
            Console.WriteLine(encryptTime);

            var decryptor = new PaillierDecryptor(
                new PaillierKeyStruct
                {
                    N = N,
                    G = G,
                    Lambda = Lambda,
                    Miu = Miu,
                    Padding = PaillierPaddingMode.LeadingZeros

                });

            byte[] decrypted = null;

            var decryptWatch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                decrypted = decryptor.ProcessData(encrypted);
            }
            decryptWatch.Stop();
            var decryptTime = new TimeSpan(0, 0, 0, 0, (int)decryptWatch.ElapsedMilliseconds / 1000);
            Console.WriteLine(decryptTime);
        }
    }
}
