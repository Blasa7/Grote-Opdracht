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

        //Number of iterations
        ulong iter;
        Console.WriteLine("Enter amount of iterations (in million) (press Enter for infinite)");
        response = Console.ReadLine();
        if (response == "")
            iter = ulong.MaxValue;
        else
            iter = ulong.Parse(response) * 1000000;


        //The solution to be worked on
        Solution solution;

        //Multi threading
        Console.WriteLine("Do you want to run in parallel? y|n");
        response = Console.ReadLine();
        if (response == "y")
        {
            int numOfThreads;

            Console.WriteLine("How many threads?");
            Console.WriteLine($"You have {Environment.ProcessorCount - 1} logical processors available");
            Console.WriteLine("(One is used for quitting behaviour)");
            response = Console.ReadLine();
            try
            {
                numOfThreads = int.Parse(response);
            }
            catch
            {
                numOfThreads = Environment.ProcessorCount - 1; //default
            }

            solution = annealing.ParallelRun(iter, numOfThreads);
        }
        else 
        {
            solution = annealing.Run(iter);
        }

        annealing.bestSolution.PrintSolution(write);

        Console.WriteLine("Final score: " + solution.score / 60 / 1000);
    }
}