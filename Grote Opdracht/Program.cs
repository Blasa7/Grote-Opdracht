class Program
{
    static void Main(string[] args)
    {
        Input.Parse();

        Console.WriteLine("Do you want to parse from input.txt? y|n");

        string response = Console.ReadLine();

        bool read = false;

        if (response == "y")
            read = true;

        Console.WriteLine("Do you want to write to file? y|n");
        
        response = Console.ReadLine();
        
        bool write = false;
        
        if (response == "y")
            write = true;

        Annealing annealing;

        if (read)
            annealing = Annealing.FromFile(@"..\\..\\..\\Input.txt");
        else
            annealing = Annealing.FromRandom();

        Solution solution = annealing.Run();

        annealing.bestSolution.PrintSolution(write);

        Console.WriteLine("Final score: " + solution.score / 60 / 1000);
    }
}