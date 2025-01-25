class Annealing
{
    #region Variable Declarations
    //Results
    public Solution bestSolution = new Solution(); // Best solution score wise, may be or time or garbage amount
    public Solution bestValidSolution = new Solution(); // Best solution that is valid

    //Schedule and score that are used while executing single threaded local search.
    Schedule workingSchedule = new Schedule();
    int workingScore;

    //General settings
    ulong iterations = ulong.MaxValue; // Basically infinite. Stop the program by pressing 'q' instead
    ulong modeIterations = 100000000;
    readonly float alpha = 0.99f;

    float beginT = 100000f;
    float endT = 1f;


    //Global random instance
    Random rng = new Random();

    #endregion

    #region Constructors
    /// <summary>
    /// Make new Annealing through the static factory functions.
    /// </summary>
    private Annealing() { }

    public static Annealing FromRandom()
    {
        Annealing annealing = new Annealing();

        annealing.workingScore = annealing.bestSolution.score;

        annealing.bestSolution.UpdateSolution(annealing.workingSchedule, annealing.workingScore);

        return annealing;
    }

    /// <summary>
    /// Reads a solution from a specified path
    /// </summary>
    public static Annealing FromFile(string path)
    {
        Annealing annealing = new Annealing();

        Schedule schedule = Schedule.LoadSchedule(path, out annealing.workingScore);

        annealing.workingSchedule = schedule;

        annealing.bestSolution.UpdateSolution(annealing.workingSchedule, annealing.workingScore);

        return annealing;
    }
    #endregion

    #region Multi Threading

    public Solution ParallelRun(int numOfThreads)
    {
        CancellationTokenSource cts = new CancellationTokenSource(); //Used to interrupts threads.
        Task[] tasks = new Task[numOfThreads];

        //Thread variables
        Weights[] annealingWeights = new Weights[numOfThreads];
        Weights[] randomWalkWeights = new Weights[numOfThreads];
        Judge[] judges = new Judge[numOfThreads];
        Solution[] threadBestSolutions = new Solution[numOfThreads];
        Solution[] threadBestValidSolutions = new Solution[numOfThreads];
        Schedule[] threadSchedules = new Schedule[numOfThreads];

        for (int i = 0; i < numOfThreads; i++)
        {
            annealingWeights[i] = Weights.StartWeight();
            randomWalkWeights[i] = Weights.StartWeight();
        }

        //Start one thread that handles the Q press for quitting
        Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    cts.Cancel(); // Signal cancellation
                    return;
                }
            }
        });

        int bestSolutionTotalGarbagePenalty = 0;
        int bestSolutionTotalTimePenalty = 0;

        ulong difficulty = 0; // Increase when run with no improvement decrease when run with improvment
        ulong modeIterationsDelta = modeIterations / 20;

        ulong runs = iterations / modeIterations;

        for (ulong r = 0; r < runs; r++)
        {
            //Multi Threading
            for (int i = 0; i < numOfThreads; i++)
            {
                int j = i;
                int threadRandomSeed = rng.Next();

                tasks[j] = Task.Run(() =>
                {
                    //Each thread needs its own schedule, best solution, random and judge.
                    threadSchedules[j] = Schedule.FromSolution(bestSolution, out int threadScore);
                    threadBestSolutions[j] = new Solution() { score = bestSolution.score };
                    threadBestValidSolutions[j] = new Solution() { score = bestValidSolution.score }; 
                    Random threadRandom = new Random(threadRandomSeed);
                    judges[j] = new Judge(threadRandom);
                    judges[j].beginT = this.beginT;
                    judges[j].endT = this.endT;
                    judges[j].totalGarbagePenalty = bestSolutionTotalGarbagePenalty;
                    judges[j].totalTimePenalty = bestSolutionTotalTimePenalty;
                    
                    ParallelSimulatedAnnealing(j, threadRandom, judges[j], threadScore, threadSchedules[j], threadBestSolutions[j], threadBestValidSolutions[j], modeIterations, annealingWeights[j], randomWalkWeights[j], cts.Token);
                }, cts.Token);
            }

            Task.WaitAll(tasks); // Wait for all threads to finish

            Solution threadBestSolution = threadBestSolutions[0];
            Solution threadBestValidSolution = threadBestValidSolutions[0];

            int bestSolutionThreadID = 0;
            int bestValidSolutionThreadID = 0;

            for (int i = 0; i < numOfThreads; i++)
            {

                if (threadBestSolutions[i].score < threadBestSolution.score)
                {
                    threadBestSolution = threadBestSolutions[i];
                    bestSolutionThreadID = i;
                }
                if (threadBestValidSolutions[i].score < threadBestValidSolution.score)
                {
                    threadBestValidSolution = threadBestValidSolutions[i];
                    bestValidSolutionThreadID = i;
                }
            }

            // A new best solution was found update the best solution.
            if (threadBestSolution.score < bestSolution.score)
            {
                difficulty--;
                modeIterations -= modeIterationsDelta;

                bestSolutionTotalGarbagePenalty = judges[bestSolutionThreadID].totalGarbagePenalty;
                bestSolutionTotalTimePenalty = judges[bestSolutionThreadID].totalTimePenalty;
                bestSolution.UpdateSolution(threadSchedules[bestSolutionThreadID], threadBestSolution.score);
            }
            else
            {
                difficulty++;
                modeIterations += modeIterationsDelta;
            }

            // A new best valid solution was found update the best valid solution.
            if (threadBestValidSolution.score < bestValidSolution.score)
            {
                bestValidSolution = threadBestValidSolution;
            }

            Console.WriteLine($"Thread {bestSolutionThreadID} had the best solution and thread {bestValidSolutionThreadID} has the best valid solution!");

            if (cts.IsCancellationRequested)
            {
                break;
            }
        }

        cts.Dispose();

        return bestValidSolution;
    }

    public void ParallelSimulatedAnnealing(int ID, Random rng, Judge judge, int workingScore, Schedule workingSchedule, Solution bestSolution, Solution bestValidSolution, ulong iterations, Weights annealingWeights, Weights randomWalkWeights, CancellationToken cts)
    {
        bestSolution.UpdateSolution(workingSchedule, workingScore);

        judge.T = judge.beginT;

        ulong redInterval = GetReductionInterval(iterations, judge.beginT, judge.endT);

        workingScore = RandomWalk(rng, judge, workingScore,workingSchedule, bestSolution, 50, randomWalkWeights);

        for (ulong i = 0; i < iterations; i++)
        {
            // Decrease the temperature every X iterations
            if (i % redInterval == 0)
            {
                judge.T *= alpha;
            }

            if (i % 1000000 == 0)
            {
                if (cts.IsCancellationRequested)
                {
                    //random cancellation messages because why not
                    switch (rng.Next(0, 5))
                    {
                        case 0:
                            Console.WriteLine($"Thread {ID} aborted its mission.");
                            break;
                        case 1:
                            Console.WriteLine($"Thread {ID} gave up on its dream.");
                            break;
                        case 2:
                            Console.WriteLine($"Thread {ID} returned to base.");
                            break;
                        case 3:
                            Console.WriteLine($"Thread {ID} has fallen.");
                            break;
                        case 4:
                            Console.WriteLine($"Thread {ID} will be remembered.");
                            break;
                    }
                    return;
                }
            }

            // Print every 10000000 iterations
            if(i % 10000000 == 0)
            {
                double progress = (double)i / (double)iterations;
                annealingWeights.RecalculateWeights();

                Console.WriteLine($"Thread {ID}, Best valid score: {bestValidSolution.score / 60 / 1000}, Best Score: {bestSolution.score / 60 / 1000}, Working score: {workingScore / 60 / 1000}, Progress: {Math.Ceiling(progress * 100)}%, Temperature: {judge.T}" + ", Time Penalty: " + judge.totalTimePenalty + ", Garbage Penalty: " + judge.totalGarbagePenalty);
            }

            //Apply operation
            workingScore = TryIterate(workingScore, workingSchedule, rng, judge, annealingWeights);

            //A better solution was found
            if (workingScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, workingScore);
            }

            // A better valid solution was found
            if (workingScore < bestValidSolution.score && judge.totalGarbagePenalty <= 0 && judge.totalTimePenalty <= 0)
            {
                bestValidSolution.UpdateSolution(workingSchedule, workingScore);
            }

            judge.Reset();
        }

        return;
    }
    #endregion

    public ulong GetReductionInterval(ulong totalIter, float beginT, float endT)
    {
        float reductionTimes = (float)Math.Log(endT / beginT, alpha);
        float reductionInterval = totalIter / reductionTimes;

        return (ulong) reductionInterval;
    }

    public static int RandomWalk(Random rng, Judge judge, int workingScore, Schedule workingSchedule, Solution bestSolution, ulong iterations, Weights weights)
    {
        for (ulong i = 0; i < iterations; i++)
        {
            judge.Reset();
            judge.OverrideJudge(Judgement.Pass);
            workingScore = TryIterate(workingScore, workingSchedule, rng, judge, weights);

            if (workingScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, workingScore);
                workingScore = bestSolution.score;
            }
        }

        judge.Reset();

        Console.WriteLine("After random walk score: " + workingScore / 60 / 1000);

        return workingScore;
    }

    public static int TryIterate(int workingScore, Schedule schedule, Random rng, Judge judge, Weights weights)
    {
        int weight = rng.Next(0, weights.totalWeightSum); 

        if (weight < weights.addWeightSum)
        {
            schedule.AddRandomDelivery(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                return workingScore + judge.timeDelta;
            }
        }
        else if (weight < weights.removeWeightSum)
        {
            schedule.RemoveRandomDelivery(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                return workingScore + judge.timeDelta;
            }
        }
        else if (weight < weights.shuffleScheduleSum)
        {
            schedule.ShuffleSchedule(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                return workingScore + judge.timeDelta;
            }
        }
        else if (weight < weights.shuffleWorkDayWeightSum)
        {
            schedule.ShuffleWorkDay(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                return workingScore + judge.timeDelta;
            }
        }
        else if (weight < weights.shuffleRouteWeightSum)
        {
            schedule.ShuffleRoute(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            { 
                return workingScore + judge.timeDelta;
            }
        }
        return workingScore;
    }
}