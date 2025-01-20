
using System.Net.Http.Headers;

class Annealing
{
    #region Variable Declarations
    public Solution bestSolution = new Solution();
    Schedule workingSchedule = new Schedule();
    int workingScore;

    ulong iterations = 100000000; //million : 1000000, billion : 1000000000, trillion : 1000000000000, infinite : 18446744073709551615
    ulong modeIterations = 100000000;
    float alpha = 0.99f;

    Random rng = new Random();
    Judge judge;

    bool insertRandomStart = false; //Whether or not to insert a number of nodes regardless of score before local search.
    bool deleteRandomStart = false;

    bool debugMessages = true;

    Statistics statistics = new Statistics();

    #endregion

    #region Init Annealing
    /// <summary>
    /// Make new Annealing through the static factory functions.
    /// </summary>
    private Annealing() { }

    public static Annealing FromRandom()
    {
        Annealing annealing = new Annealing();

        annealing.workingScore = annealing.bestSolution.score;
        
        annealing.judge = new Judge(annealing.rng);

        annealing.bestSolution.UpdateSolution(annealing.workingSchedule, annealing.workingScore, 0, 0);

        return annealing;
    }

    /// <summary>
    /// Reads a solution from a specified path
    /// </summary>
    public static Annealing FromFile(string path)
    {
        Annealing annealing = new Annealing();

        (Schedule schedule, int timePenalty, int garbagePenalty) = Schedule.LoadSchedule(path, out annealing.workingScore);

        annealing.workingSchedule = schedule;

        annealing.judge = new Judge(annealing.rng);
        annealing.judge.timePenalty = timePenalty;
        annealing.judge.garbagePenalty = garbagePenalty;

        annealing.bestSolution.UpdateSolution(annealing.workingSchedule, annealing.workingScore, timePenalty, garbagePenalty);

        return annealing;
    }

    #endregion

    #region Multi Threading Territory ( PROCEED WITH CAUTION )

    public Solution ParallelRun(ulong iter, int numOfThreads)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        Task<Solution>[] tasks = new Task<Solution>[numOfThreads];
        Weights[] weights = new Weights[numOfThreads];
        iterations = iter;

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = Weights.StartWeight();
        }

        //Set temperature values:
        float beginT = 100000f;
        float endT = 1f;

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

        ulong runs = iter / modeIterations;

        for (ulong r = 0; r < runs; r++)
        {
            //Multi Threading
            for (int i = 0; i < weights.Length; i++)
            {
                int threadID = i + 1;
                int j = i;
                tasks[i] = (Task.Run(() =>
                {
                    //Each thread gets their own schedule
                    Schedule threadSchedule = Schedule.FromSolution(bestSolution, out int threadScore);
                    Solution threadBestSolution = new Solution();
                    Random threadRandom = new Random();
                    Judge threadJudge = new Judge(threadRandom);
                    threadJudge.beginT = beginT;
                    return ParallelSimulatedAnnealing(threadID, threadRandom, threadJudge, threadScore, threadSchedule, threadBestSolution, iterations, beginT, endT, weights[j], cts.Token);
                }, cts.Token));
            }

            Task.WaitAll(tasks.ToArray()); // Wait for all threads to finish

            // for testing purpoises.
            Solution threadBestSolution = tasks[0].Result;
            int bestThreadID = 1;

            for (int i = 0; i < tasks.Length; i++)
            {
                Solution threadSolution = tasks[i].Result;
                if (threadSolution.score < threadBestSolution.score)
                {
                    threadBestSolution = threadSolution;
                    bestThreadID = i + 1;
                }
            }

            if (threadBestSolution.score < bestSolution.score)
            {
                bestSolution = threadBestSolution;
            }

            Console.WriteLine($"Thread {bestThreadID} had the best solution.");

            if (cts.IsCancellationRequested)
            {
                break;
            }
        }

        cts.Dispose();

        return bestSolution;
    }

    public Solution ParallelSimulatedAnnealing(int ID, Random rng, Judge judge, int workingScore, Schedule workingSchedule, Solution bestSolution, ulong iterations, float beginT, float endT, Weights weights, CancellationToken cts)
    {
        bestSolution.UpdateSolution(workingSchedule, workingScore, judge.timePenalty, judge.garbagePenalty);

        //Set initial T
        judge.T = beginT;

        //Set initial temperature
        ulong redInterval = GetReductionInterval(modeIterations, beginT, endT);

        workingScore = RandomWalk(rng, judge, workingScore,workingSchedule, bestSolution, 50, weights);


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
                    return bestSolution;
                }
            }

            if(i % 10000000 == 0)
            {
                double progress = ((double)(i % modeIterations) / modeIterations);
                weights.DynamicallyUpdateWeights(progress);
                weights.RecalculateWeights();

                Console.WriteLine($"Thread {ID}, Best Score: {bestSolution.score / 60 / 1000}, Working score: {workingScore / 60 / 1000}, Progress: {(int)((double)(i % modeIterations) / modeIterations * 100)}%, Temperature: {judge.T}");
            }

            //Apply operation
            workingScore = TryIterate(workingScore, workingSchedule, rng, judge, weights);

            //If better solution found
            if (workingScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, workingScore, judge.timePenalty, judge.garbagePenalty);
                workingScore = bestSolution.score;
            }

            judge.Reset();

            //End of one temperate cycle
            if (i % modeIterations == 0 && i > 0)
            {
                weights.ResetWeights();
                weights.RecalculateWeights();

                return bestSolution;
            }
        }

        return bestSolution;
    }

    #endregion

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
        bestSolution.UpdateSolution(workingSchedule, workingScore, judge.timePenalty, judge.garbagePenalty);

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
                bestSolution.UpdateSolution(workingSchedule, workingScore, judge.timePenalty, judge.garbagePenalty);
                workingScore = bestSolution.score;
            }

            // Print bestScore, workingScore and progress every million iterations
            if (i % 1000000 == 0) 
            {
                double progress = ((double)(i % modeIterations) / modeIterations);
                Console.WriteLine("Best score: " + (bestSolution.score / 60 / 1000) + ", Working score: " + (workingScore / 60 / 1000) + ", Progress " + (int) (progress*100) + "%, Mode Progress " + (int)((double)(i % modeIterations) / modeIterations * 100) + "%, Temperature: " + judge.T);
                Console.WriteLine(("Time penalty: " + judge.timePenalty / (1000 * 60), judge.garbagePenalty));
                weights.DynamicallyUpdateWeights(progress);

                //Console.WriteLine((judge.timeDelta, judge.timePenalty, judge.garbagePenalty));

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q) // Quit the program when the user presses 'q'
                    {
                        //bestSolution.UpdateSolution(workingSchedule, workingScore, judge.timePenalty, judge.garbagePenalty);
                        Console.WriteLine($"Interrupted by user after {i/1000000} million iterations");
                        Console.WriteLine((judge.timePenalty, judge.garbagePenalty));
                        return;
                    }
                    else if (key.Key == ConsoleKey.P)
                    {
                        bestSolution.UpdateSolution(workingSchedule, workingScore, judge.timePenalty, judge.garbagePenalty);
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

    public int RandomWalk(Random rng, Judge judge, int workingScore, Schedule workingSchedule, Solution bestSolution, ulong iterations, Weights weights)
    {
        for (ulong i = 0; i < iterations; i++)
        {
            judge.Reset();
            judge.OverrideJudge(Judgement.Pass);
            workingScore = TryIterate(workingScore, workingSchedule, rng, judge, weights);

            if (workingScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, workingScore, judge.timePenalty, judge.garbagePenalty);
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
    public int timePenalty;
    public int garbagePenalty;

    public int minRoutes = 14;
    public int maxRoutes = 15;

    public double garbagePenaltyMultiplier = 10;
    public double timePenaltyMultiplier = 1 / 500;
    public float beginT;

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
        this.timePenalty += timePenaltyDelta;
        this.garbagePenalty += garbagePenaltyDelta;
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

            double maxWeightMultiplier = 1000;
            double weight = (beginT - T) / beginT * maxWeightMultiplier;
            double weightedGarbagePenalty = garbagePenalty * garbagePenaltyMultiplier * weight;
            double weightedTimePenalty = timePenalty * timePenaltyMultiplier * weight;
            double numerator = -(timeDelta + weightedTimePenalty + weightedGarbagePenalty);

            double frac = numerator / T; // '-', because we want to minimize here
            double res = Math.Exp(frac);
            if (res >= rng.NextDouble())
                judgement = Judgement.Pass;
            else
                judgement = Judgement.Fail;
        }

        return judgement;
    }

    public void Reset()
    {
        timeDelta = 0;
        timePenalty = 0;
        garbagePenalty = 0;
        judgement = Judgement.Undecided;
    }
}

class Weights()
{
    readonly int baseAddWeight = 200;
    readonly int baseRemoveWeight = 100;
    readonly int baseShuffleScheduleWeight = 100;
    readonly int baseShuffleWorkDayWeight = 200;
    readonly int baseShuffleRouteWeight = 400;

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

    /// <summary>
    /// Dynamically change the weights as we progress through the algorithm.
    /// Increase the chances of removal and shuffling the closer we are to the end
    /// </summary>
    /// <param name="progress">Progress should be between 0.0-1.0</param>
    public void DynamicallyUpdateWeights(double progress)
    {
        addWeight = (int)(baseAddWeight * (1 - progress));
        removeWeight = (int)(baseAddWeight * progress * 2);
        shuffleScheduleWeight = (int)(baseAddWeight * progress);
        shuffleWorkDayWeight = (int)(baseAddWeight * progress);
        shuffleRouteWeight = (int)(baseAddWeight * progress);

        RecalculateWeights();
    }

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