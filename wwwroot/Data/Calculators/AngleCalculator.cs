namespace CalculatorApp.Calculators
{
    internal static class AngleCalculator
    {
        public static double CosineDegree(double degree)
        {
            return Math.Cos(degree / 180 * Math.PI);
        }

        public static double SineDegree(double degree)
        {
            return Math.Sin(degree / 180 * Math.PI);
        }

        public static double TanDegree(double degree)
        {
            return Math.Tan(degree / 180 * Math.PI);
        }

        public static double ArcCosineDegree(double x)
        {
            return Math.Acos(x) * 180 / Math.PI;
        }

        public static double ArcSineDegree(double x)
        {
            return Math.Asin(x) * 180 / Math.PI;
        }

        public static double ArcTanDegree(double x)
        {
            return Math.Atan(x) * 180 / Math.PI;
        }
    }
}
