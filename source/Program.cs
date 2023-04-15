using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;

namespace gotosan
{
    public class Program
    {
        private static void Main(string[] args) {
            // Check that there is a valid gotochan file to be run
            if (args.Length == 0) {
                return;
            }
            else if (File.Exists(args[0]) == false) {
                return;
            }

            // Display version information
            Console.WriteLine($"gotosan v{Gotosan.Version}");
            Console.WriteLine();

            // Read the code
            string Code = File.ReadAllText(args[0]);

            // Parse the code
            string ParsedCode = Gotosan.Parse(Code);

            // Run the code
            ClearInput();
            Gotosan.Run(ParsedCode).Wait();

            // End of code, wait for user to press enter
            Console.WriteLine("\n");
            Console.Write("end of program");
            ClearInput();
            while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
        }
        private static void ClearInput() {
            while (Console.KeyAvailable) {
                Console.ReadKey(true);
            }
        }
        private static void ParseBenchmark(string Code) {
            long StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Start benchmark
            Console.WriteLine(Gotosan.Parse(Code)); 
            Console.WriteLine($"Parsed in {(DateTimeOffset.Now.ToUnixTimeMilliseconds() - StartTime) / 1000d} seconds."); // End benchmark
            Console.WriteLine("-------------------------");
        }
        
    }
}
