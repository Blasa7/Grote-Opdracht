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

        ulong iter;
        Console.WriteLine("Enter amount of iterations (in million)");

        response = Console.ReadLine();
        if (response == "")
            iter = 18446744073709551615;
        else
            iter = ulong.Parse(response) * 1000000;

        Solution solution = annealing.Run(iter);

        annealing.bestSolution.PrintSolution(write);

        Console.WriteLine("Final score: " + solution.score / 60 / 1000);
    }
}