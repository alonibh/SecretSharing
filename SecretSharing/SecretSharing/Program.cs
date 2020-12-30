namespace SecretSharing
{
    class Program
    {
        static void Main(string[] args)
        {
            Matrix m = new Matrix(3, 3, 8, 1, 5, 1);
            m.SplitToTrainingAndTesting();
        }
    }
}
