class Annealing
{
    #region Variable Declarations
    //Results
    public Solution bestSolution = new Solution(); // Best solution score wise, may be or time or garbage amount
    public Solution bestValidSolution = new Solution(); // Best solution that is valid

    //Schedule and score that are used while executing single threaded local search.
    Schedule workingSchedule = new Schedule();
    int workingScore;
    //Judge that is used while executing single threaded local search.
    Judge judge;

    //General settings
    ulong iterations = ulong.MaxValue; // basically infinite. Stop the program by pressing 'q' instead
    ulong modeIterations = 200000000;
    float alpha = 0.99f;

    float beginT = 35000f;
    float endT = 1f;


    //Global random instance
    Random rng = new Random();

    //Debug related items
    bool debugMessages = false;
    Statistics statistics = new Statistics();

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
        
        annealing.judge = new Judge(annealing.rng);

        annealing.bestSolution.UpdateSolution(annealing.workingSchedule, annealing.workingScore);

        return annealing;
    }

    /// <summary>
    /// Reads a solution from a specified path
    /// </summary>
    public static Annealing FromFile(string path)
    {
        Annealing annealing = new Annealing();

        (Schedule schedule, int timePenalty, int garbagePenalty) = Schedule.LoadSchedule(path, out annealing.workingScore);
        (Schedule schedule2, _, _) = Schedule.LoadSchedule(path, out _); //Assuming only use valid inputs.

        annealing.workingSchedule = schedule;

        annealing.judge = new Judge(annealing.rng);

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
            /*randomWalkWeights[i] = new Weights();
            randomWalkWeights[i].addWeight = 100;
            randomWalkWeights[i].removeWeight = 10;
            randomWalkWeights[i].shuffleScheduleWeight = 10;
            randomWalkWeights[i].shuffleWorkDayWeight = 10;
            randomWalkWeights[i].shuffleRouteWeight = 5;
            randomWalkWeights[i].ResetWeights();
            randomWalkWeights[i].RecalculateWeights();*/
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
                    threadSchedules[j] = Schedule.FromSolution(bestSolution, out int threadScore);//Schedule threadSchedule = Schedule.FromSolution(bestSolution, out int threadScore);
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
                //Solution threadSolution = threadBestSolutions[i];

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
                bestSolution.UpdateSolution(threadSchedules[bestSolutionThreadID], threadBestSolution.score);// = threadBestSolution;
            }
            else
            {
                difficulty++;
                modeIterations += modeIterationsDelta;
            }

            // A new best valid solution was found update the best valid solution.
            if (threadBestValidSolution.score < bestValidSolution.score)
            {//small chance this cause some weird reference bug
                bestValidSolution = threadBestValidSolution;//bestValidSolution.UpdateSolution(threadSchedules[bestValidSolutionThreadID], threadBestValidSolution.score, 0, 0);// = threadBestSolution;
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

            if(i % 10000000 == 0)
            {
                double progress = (double)i / (double)iterations;//((double)(i % modeIterations) / modeIterations);
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

            ////End of one temperate cycle
            //if (i % modeIterations == 0 && i > 0)
            //{
            //    weights.ResetWeights();
            //    weights.RecalculateWeights();

            //    return;// bestSolution;
            //}
        }

        annealingWeights.ResetWeights();
        annealingWeights.RecalculateWeights();

        randomWalkWeights.ResetWeights();
        randomWalkWeights.RecalculateWeights();

        return;// bestSolution;
    }

    #endregion

    #region Non Parallel
    public Solution Run(ulong iter)
    {
        //The total number of iterations, specified through input at the start.
        Console.WriteLine(iter);
        iterations = iter;

        //Displays the initial score.
        Console.WriteLine("Initial score: " + workingScore / 60 / 1000);

        //Start iterating
        //Set temperature values:

        float beginT = 100000;
        //float beginT = float.MaxValue;
        float endT = 1f;

        judge.beginT = beginT;

        SimmulatedAnnealing(rng, judge, workingScore, workingSchedule, bestSolution, iterations, beginT, endT);

        //Display debug prints after end of process.
        if (debugMessages)
            DebugMessages();

        return bestSolution;
    }

    public ulong GetReductionInterval(ulong totalIter, float beginT, float endT)
    {
        float reductionTimes = (float) Math.Log(endT / beginT, alpha);
        float reductionInterval = totalIter / reductionTimes;
        Console.WriteLine("Reduction times: " + reductionTimes + ", Reduction interval: " + reductionInterval);

        return (ulong) reductionInterval;
    }

    public void SimmulatedAnnealing(Random rng, Judge judge, int workingScore, Schedule workingSchedule, Solution bestSolution, ulong iterations, float beginT, float endT)
    { 
        bestSolution.UpdateSolution(workingSchedule, workingScore);

        Weights weights = Weights.StartWeight();

        //Set initial T
        judge.T = beginT;

        //Set inital temp
        ulong redInterval = GetReductionInterval(modeIterations, beginT, endT);

        for (ulong i = 0; i < iterations; i++)
        {
            // Decrease the temperature every X iterations
            if (i % redInterval == 0)
            {
                judge.T *= alpha;
            }

            workingScore = TryIterate(workingScore, workingSchedule, rng, judge, weights);

            if (workingScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, workingScore);
                workingScore = bestSolution.score;
                if (judge.totalTimePenalty == 0 && judge.totalGarbagePenalty < 0) // Solution is Valid
                {
                    bestValidSolution = bestSolution;
                }

            }

            // Print bestScore, workingScore and progress every million iterations
            if (i % 1000000 == 0) 
            {

                double progress = ((double)(i % modeIterations) / modeIterations);
                Console.WriteLine("Best score: " + (bestSolution.score / 60 / 1000) + ", Working score: " + (workingScore / 60 / 1000) + ", Progress " + (int) (progress*100) + "%, Mode Progress " + (int)((double)(i % modeIterations) / modeIterations * 100) + "%, Temperature: " + judge.T + ", Time Penalty: " + judge.totalTimePenalty + ", Garbage Penalty: " + judge.totalGarbagePenalty);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q) // Quit the program when the user presses 'q'
                    {
                        Console.WriteLine($"Interrupted by user after {i/1000000} million iterations");
                        return;
                    }
                    else if (key.Key == ConsoleKey.P)
                    {
                        bestSolution.UpdateSolution(workingSchedule, workingScore);
                        workingScore = bestSolution.score;
                    }
                }
            }

            judge.Reset();

            if (i % modeIterations == 0 && i > 0)
            {
                judge.T = beginT;

                ulong randomWalkIterations = 100;
                workingScore = RandomWalk(rng, judge, workingScore, workingSchedule, bestSolution, randomWalkIterations, weights);

                weights.ResetWeights();
                weights.RecalculateWeights();

                Console.WriteLine("Reset!");
            }

        }
    }

    public void GoUntilValid(Judge judge, Weights weights, Random rng, Solution bestSolution, Schedule bestSchedule)
    {
        //Set new weights
        weights.addWeight = 1;
        weights.removeWeight = 200;
        weights.shuffleScheduleWeight = 10;
        weights.shuffleWorkDayWeight = 10;
        weights.shuffleRouteWeight = 100;

        weights.RecalculateWeights();

        while (judge.totalTimePenalty > 0 || judge.totalGarbagePenalty > 0)
        {
            workingScore = TryIterate(workingScore, workingSchedule, rng, judge, weights);

            if (workingScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, workingScore);
                workingScore = bestSolution.score;
                bestSchedule = workingSchedule;
            }
        }

        //UpdateSolution();
        Console.WriteLine($"Found valid solution");
        
    }

    #endregion

    public int RandomWalk(Random rng, Judge judge, int workingScore, Schedule workingSchedule, Solution bestSolution, ulong iterations, Weights weights)
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

    public int TryIterate(int workingScore, Schedule schedule, Random rng, Judge judge, Weights weights)
    {
        int weight = rng.Next(0, weights.totalWeightSum); 

        if (weight < weights.addWeightSum)
        {
            schedule.AddRandomDelivery(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                //statistics.addScoreDelta += judge.timeDelta;
                //statistics.addSuccessCount++;

                return workingScore + judge.timeDelta;
            }
            else
            {
                //statistics.addFailCount++;
            }
        }
        else if (weight < weights.removeWeightSum)
        {
            schedule.RemoveRandomDelivery(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                //statistics.removeScoreDelta += judge.timeDelta;
                //statistics.removeSuccessCount++;

                return workingScore + judge.timeDelta;
            }
            else
            {
                //statistics.removeFailCount++;
            }
        }
        else if (weight < weights.shuffleScheduleSum)
        {
            schedule.ShuffleSchedule(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                //statistics.shuffleScheduleScoreDelta += judge.timeDelta;
                //statistics.shuffleScheduleSuccessCount++;

                return workingScore + judge.timeDelta;
            }
            else
            {
                //statistics.shuffleScheduleFailCount++;
            }
        }
        else if (weight < weights.shuffleWorkDayWeightSum)
        {
            schedule.ShuffleWorkDay(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                //statistics.shuffleWorkDayScoreDelta += judge.timeDelta;
                //statistics.shuffleWorkDaySuccessCount++;

                return workingScore + judge.timeDelta;
            }
            else
            {
               // statistics.shuffleWorkDayFailCount++;
            }
        }
        else if (weight < weights.shuffleRouteWeightSum)
        {
            schedule.ShuffleRoute(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
               // statistics.shuffleRouteScoreDelta += judge.timeDelta;
                //statistics.shuffleRouteSuccessCount++;
                
                return workingScore + judge.timeDelta;
            }
            else
            {
               //statistics.shuffleRouteFailCount++;
            }
        }

        return workingScore;
    }

    /// <summary>
    /// Prints the schedule for debug purposes
    /// </summary>
    void DebugMessages()
    {
        for (int i = 0; i < workingSchedule.workDays.Length; i++)
        {
            for (int j = 0; j < workingSchedule.workDays[i].Length; j++)
            {
                IndexedLinkedListNode<Route> r = workingSchedule.workDays[i][j].workDay.nodes[0];

                Console.WriteLine(workingSchedule.workDays[i][j].totalDuration);

                float sumDuration = 0;

                for (int k = 0; k < workingSchedule.workDays[i][j].workDay.currentIndex + 1; k++)
                {
                    IndexedLinkedListNode<Delivery> d = r.value.route.nodes[0];

                    for (int l = 0; l < r.value.route.currentIndex; l++)
                    {
                        d = d.next;
                    }

                    sumDuration += r.value.duration;
                    Console.WriteLine("Truck: " + i + ", Day: " + j + ", Route: " + k + " , Nodes: " + r.value.route.currentIndex + " , Duration: " + sumDuration);


                    r = r.next;
                }

            }
        }

        Console.WriteLine(statistics);
    }
}

class Judge
{
    //public int scoreDelta; //newScore - oldScore (negative score suggests improvement!)
    public int timeDelta;
    public int timePenaltyDelta;
    public int garbagePenaltyDelta;

    public int totalTimePenalty;
    public int totalGarbagePenalty;

    public bool updatedPenalties = false;

    public int minRoutes = 14;
    public int maxRoutes = 16;

    public double garbagePenaltyMultiplier = 4.32 * 7.3;//10;
    public double timePenaltyMultiplier = 1 * 0.07;//100

    public float beginT;
    public float endT;

    Judgement judgement;

    public float T;
    public Random rng;

    public Judge(Random rng)
    {
        this.rng = rng;

        Reset();
    }

    public void Testify(int timeDelta, int timePenaltyDelta, int garbagePenaltyDelta)
    {
        this.timeDelta += timeDelta;
        this.timePenaltyDelta += timePenaltyDelta;
        this.garbagePenaltyDelta += garbagePenaltyDelta;
    }

    public void OverrideJudge(Judgement judgement)
    {
        this.judgement = judgement;
    }

    public Judgement GetJudgement()
    {
        if (judgement == Judgement.Undecided) //If no function has overidden the judgement
        {
            //double weight = beginT - T;
            //double maxWeightMultiplier = 1000;
            //double weight = Math.Pow((beginT - T) / beginT * maxWeightMultiplier, 2);
            double maxWeightMultiplier = 9;
            double weight = ((beginT - T) / beginT) * maxWeightMultiplier + 1;
            double weightedGarbagePenalty = garbagePenaltyDelta * garbagePenaltyMultiplier * weight;
            double weightedTimePenalty = timePenaltyDelta * timePenaltyMultiplier * weight;
            double numerator = -(timeDelta + weightedTimePenalty + weightedGarbagePenalty);

            double frac = numerator / T; // '-', because we want to minimize here
            double res = Math.Exp(frac);
            if (res >= rng.NextDouble())
            {
                judgement = Judgement.Pass;
            }
            else
                judgement = Judgement.Fail;
        }

        if (!updatedPenalties && judgement == Judgement.Pass)
        {
            totalTimePenalty += timePenaltyDelta;
            totalGarbagePenalty += garbagePenaltyDelta;
            updatedPenalties = true;
        }

        return judgement;
    }

    public void Reset()
    {
        updatedPenalties = false;
        timeDelta = 0;
        timePenaltyDelta = 0;
        garbagePenaltyDelta = 0;
        judgement = Judgement.Undecided;
    }
}

class Weights()
{
    readonly int baseAddWeight = 100;//200;//200;
    readonly int baseRemoveWeight = 50;//100;
    readonly int baseShuffleScheduleWeight = 200;//200;//100;//100;
    readonly int baseShuffleWorkDayWeight = 200;//200;//200;
    readonly int baseShuffleRouteWeight = 200;//400;//400;

    public int addWeight;
    public int removeWeight;
    public int shuffleScheduleWeight;
    public int shuffleWorkDayWeight;
    public int shuffleRouteWeight;

    public int addWeightSum;
    public int removeWeightSum;
    public int shuffleScheduleSum;
    public int shuffleWorkDayWeightSum;
    public int shuffleRouteWeightSum;
    public int totalWeightSum;

    public void ResetWeights()
    {
        addWeight = baseAddWeight;
        removeWeight = baseRemoveWeight;
        shuffleScheduleWeight = baseShuffleScheduleWeight;
        shuffleWorkDayWeight = baseShuffleWorkDayWeight;
        shuffleRouteWeight = baseShuffleRouteWeight;
    }

    public void RecalculateWeights()
    {
        addWeightSum = addWeight;
        removeWeightSum = addWeightSum + removeWeight;
        shuffleScheduleSum = removeWeightSum + shuffleScheduleWeight;
        shuffleWorkDayWeightSum = shuffleScheduleSum + shuffleWorkDayWeight;
        shuffleRouteWeightSum = shuffleWorkDayWeightSum + shuffleRouteWeight;
        totalWeightSum = shuffleRouteWeightSum + 1;
    }

    public static Weights StartWeight()
    {
        Weights weights = new Weights();
        weights.ResetWeights();
        weights.RecalculateWeights();

        return weights;
    }
}

enum Judgement
{
    Fail = 0,
    Pass = 1,
    Undecided = -1
}

class Statistics()
{
    public long addScoreDelta;
    public long addSuccessCount;
    public long addFailCount;

    public long removeScoreDelta;
    public long removeSuccessCount;
    public long removeFailCount;

    public long shuffleScheduleScoreDelta;
    public long shuffleScheduleSuccessCount;
    public long shuffleScheduleFailCount;

    public long shuffleWorkDayScoreDelta;
    public long shuffleWorkDaySuccessCount;
    public long shuffleWorkDayFailCount;

    public long shuffleRouteScoreDelta;
    public long shuffleRouteSuccessCount;
    public long shuffleRouteFailCount;

    public long swapDeliveryScoreDelta;
    public long swapDeliverySuccessCount;
    public long swapDeliveryFailCount;


    public override string ToString()
    {
        return
            $"Statistics: \n" +
            $"Add score delta: {addScoreDelta / 60 / 1000} \n" +
            $"Add success count: {addSuccessCount} \n" +
            $"Add fail count: {addFailCount} \n" +
            $"Remove score delta: {removeScoreDelta / 60 / 1000} \n" +
            $"Remove success count: {removeSuccessCount} \n" +
            $"Remove fail count: {removeFailCount} \n" +
            $"Shuffle schedule score delta: {shuffleScheduleScoreDelta / 60 / 1000} \n" +
            $"Shuffle schedule success count: {shuffleScheduleSuccessCount} \n" +
            $"Shuffle schedule fail count: {shuffleScheduleFailCount} \n" +
            $"Shuffle workday score delta: {shuffleWorkDayScoreDelta / 60 / 1000} \n" +
            $"Shuffle workday success count: {shuffleWorkDaySuccessCount} \n" +
            $"Shuffle workday fail count: {shuffleWorkDayFailCount} \n" +
            $"Shuffle route score delta: {shuffleRouteScoreDelta / 60 / 1000} \n" +
            $"Shuffle route success count: {shuffleRouteSuccessCount} \n" +
            $"Shuffle route fail count: {shuffleRouteFailCount} \n" +
            $"Swap delivery score delta: {swapDeliveryScoreDelta / 60 / 1000} \n" +
            $"Swap delivery success count: {swapDeliverySuccessCount} \n" +
            $"Swap delivery fail count: {swapDeliveryFailCount}";
    }
}