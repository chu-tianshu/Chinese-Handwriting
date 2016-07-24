namespace App2
{
    public class Fuzzy
    {
        public static double Gamma(double x, double a, double b)
        {
            if (x < a) return 1.0;
            else if (x > b) return 0.0;
            else return ((b - x * 1.0) / (b - a));
        }

        public static double Lambda(double x, double b, double c)
        {
            if (x < b) return 0.0;
            else if (x > c) return 1.0;
            else return ((x * 1.0 - b) / (c - b));
        }

        public static double Delta(double x, double a, double b, double c)
        {
            if (x < a || x > c) return 0.0;
            else
            {
                if (x <= b) return ((x * 1.0 - a) / (b - a));
                else return ((c - x * 1.0) / (c - b));
            }
        }

        public static double CenterOfAreaLow(double a, double b, double muon)
        {
            double numerator = 0.0;
            double denominator = 0.0;

            for (double x = 0; x < b; x += 0.01)
            {
                if (Gamma(x, a, b) < muon) break;

                numerator += x * Gamma(x, a, b);
                denominator += Gamma(x, a, b);
            }

            return numerator / denominator;
        }

        public static double CenterOfAreaHigh(double b, double c, double muon)
        {
            double numerator = 0.0;
            double denominator = 0.0;

            for (double x = b; x < 1; x += 0.01)
            {
                if (Lambda(x, b, c) > muon) break;

                numerator += x * Lambda(x, b, c);
                denominator += Lambda(x, b, c);
            }

            return numerator / denominator;
        }
    }
}