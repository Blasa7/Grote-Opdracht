class Program
{
    static void Main(string[] args)
    {
        Input.Parse();

        Annealing annealing = new Annealing();
        Solution solution = annealing.Run();

        Console.WriteLine(solution.score.ToString());

        // solution.PrintSolution();
    }
}