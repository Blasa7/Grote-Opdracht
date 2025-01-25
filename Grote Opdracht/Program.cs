class Program
{
    static void Main(string[] args)
    {
        Input.Parse();

        //Read from input.txt
        Console.WriteLine("Do you want to parse from input.txt? y|n");
        string response = Console.ReadLine();

        bool read = false;
        if (response == "y")
            read = true;

        //Write to file
        Console.WriteLine("Do you want to write to file? y|n");
        response = Console.ReadLine();
        
        bool write = false;
        if (response == "y")
            write = true;

        //Create an Annealing instance
        Annealing annealing;
        if (read)
            annealing = Annealing.FromFile(@"..\\..\\..\\Input.txt");
        else
            annealing = Annealing.FromRandom();

        //The solution to be worked on
        Solution solution;

        //Multi threading
        int numOfThreads;

        Console.WriteLine("Enter amount of threads to run with");
        Console.WriteLine($"You have {Environment.ProcessorCount - 1} logical processors available");
        Console.WriteLine("(One is used for quitting behaviour):");

        response = Console.ReadLine();
        try
        {
            numOfThreads = int.Parse(response);
        }
        catch
        {
            numOfThreads = Environment.ProcessorCount - 1; //default
        }

        Console.WriteLine($"Running with {numOfThreads} threads");
        Console.WriteLine($"You can always press 'q' to stop running");
        solution = annealing.ParallelRun(numOfThreads);

        annealing.bestValidSolution.PrintSolution(write);

        Console.WriteLine("Final score: " + solution.score / 60 / 1000);
    }
}