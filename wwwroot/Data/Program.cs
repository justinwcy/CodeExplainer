using CalculatorApp.Calculators;

namespace CalculatorApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var x = 180;
            var y = 0.5;

            Console.WriteLine($"{x} + {y} = {BasicCalculator.Add(x, y)}");
            Console.WriteLine($"{x} - {y} = {BasicCalculator.Minus(x, y)}");
            Console.WriteLine($"{x} * {y} = {BasicCalculator.Multiply(x, y)}");
            Console.WriteLine($"{x} / {y} = {BasicCalculator.Divide(x, y)}");
            Console.WriteLine();
            
            Console.WriteLine($"cos({x}) = {AngleCalculator.CosineDegree(x)}");
            Console.WriteLine($"sin({x}) = {AngleCalculator.SineDegree(x)}");
            Console.WriteLine($"tan({x}) = {AngleCalculator.TanDegree(x)}");
            Console.WriteLine();

            Console.WriteLine($"acos({y}) = {AngleCalculator.ArcCosineDegree(y)}");
            Console.WriteLine($"asin({y}) = {AngleCalculator.ArcSineDegree(y)}");
            Console.WriteLine($"atan({y}) = {AngleCalculator.ArcTanDegree(y)}");
            Console.WriteLine();

            Console.WriteLine($"cosh({y}) = {HyperbolicCalculator.Cosh(y)}");
            Console.WriteLine($"sinh({y}) = {HyperbolicCalculator.Sinh(y)}");
            Console.WriteLine($"tanh({y}) = {HyperbolicCalculator.Tanh(y)}");
            Console.WriteLine();
        }
    }
}
