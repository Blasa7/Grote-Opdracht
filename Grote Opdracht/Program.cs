﻿class Program
{
    static void Main(string[] args)
    {
        Input.Parse();

        Console.WriteLine("Do you want to write to file?");
        string s = Console.ReadLine();
        bool write = false;
        if (s == "y")
        {
            write = true;
        }

        Annealing annealing = new Annealing();
        //Solution solution = annealing.Run();

        //Console.WriteLine(solution.score / 60 / 1000);

        Solution.LoadSolution();


    }
}