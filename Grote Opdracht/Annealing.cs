class Annealing
{
    public Solution bestSolution = new Solution();
    Schedule workingSchedule = new Schedule();
    int workingScore;

    ulong iterations = 100000000; //million : 1000000, billion : 1000000000, trillion : 1000000000000, infinite : 18446744073709551615
    ulong modeIterations = 250000000;
    float alpha = 0.99f;

    Random rng = new Random();
    Judge judge;

    bool insertRandomStart = false; //Whether or not to insert a number of nodes regardless of score before local search.
    bool deleteRandomStart = false;

    bool debugMessages = true;

    Statistics statistics = new Statistics();

    /// <summary>
    /// Make new Annealing through the static factory functions.
    /// </summary>
    private Annealing() { RecalculateWeights(); }

    public static Annealing FromRandom()
    {
        Annealing annealing = new Annealing();

        annealing.workingScore = annealing.bestSolution.score;
        
        annealing.judge = new Judge(annealing.rng);

        return annealing;
    }

    /// <summary>
    /// Reads a solution from a specified path
    /// </summary>
    public static Annealing FromFile(string path)
    {
        Annealing annealing = new Annealing();

        annealing.workingSchedule = Schedule.LoadSchedule(path, out annealing.workingScore);

        annealing.judge = new Judge(annealing.rng);

        annealing.bestSolution.UpdateSolution(annealing.workingSchedule, annealing.workingScore);

        return annealing;
    }

    private void RecalculateWeights()
    {
        addWeightSum = addWeight;
        removeWeightSum = addWeightSum + removeWeight;
        shuffleScheduleSum = removeWeightSum + shuffleScheduleWeight;
        shuffleWorkDayWeightSum = shuffleScheduleSum + shuffleWorkDayWeight;
        shuffleRouteWeightSum = shuffleWorkDayWeightSum + shuffleRouteWeight;
        swapDeliveriesWeightSum = shuffleRouteWeightSum + swapDeliveriesWeight;
        totalWeightSum = swapDeliveriesWeightSum + 1;
    }

    public Solution Run(ulong iter)
    {

        Console.WriteLine(iter);
        iterations = iter;

        Console.WriteLine("Initial score: " + workingScore / 60 / 1000);

        //Initial solution

        if (insertRandomStart)
        {
            for (int i = 0; i < 5000; i++)
            {
                judge.Reset();
                judge.OverrideJudge(Judgement.Pass);
                workingSchedule.AddRandomDelivery(rng, judge);

                if (judge.GetJudgement() == Judgement.Pass)
                    workingScore += judge.timeDelta;
            }

            bestSolution.UpdateSolution(workingSchedule, workingScore);

            judge.Reset();

            Console.WriteLine("After inserting score: " + workingScore / 60 / 1000);
        }

        if (deleteRandomStart)
        {
            for (int i = 0; i < 250; i++)
            {
                judge.Reset();
                judge.OverrideJudge(Judgement.Pass);
                workingSchedule.RemoveRandomDelivery(rng, judge);

                if (judge.GetJudgement() == Judgement.Pass)
                    workingScore += judge.timeDelta;
            }

            bestSolution.UpdateSolution(workingSchedule, workingScore);

            judge.Reset();

            Console.WriteLine("After deleting score: " + workingScore / 60 / 1000);
        }

        RecalculateWeights();

        //Start iterating

        float beginT = 10000000f;
        //float beginT = float.MaxValue;
        float endT = 0.0001f; 

        SimmulatedAnnealing(rng, judge, workingScore, workingSchedule, bestSolution, iterations, beginT, endT);

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

        //Set initial T
        judge.T = beginT;

        //Set inital temp
        ulong redInterval = GetReductionInterval(modeIterations, beginT, endT);

        for (ulong i = 0; i < iterations; i++)
        {
            if (Schedule.GlobalNumOfRoutes > Schedule.GlobalMaxOfRoutes)
                Console.WriteLine(Schedule.GlobalNumOfRoutes);

            // Decrease the temperature every X iterations
            if (i % redInterval == 0)
            {
                judge.T *= alpha;
            }

            workingScore = TryIterate(workingScore, workingSchedule, rng, judge);

            if (workingScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, workingScore);
                workingScore = bestSolution.score;
            }

            judge.Reset();

            // Print bestScore, workingScore and progress every million iterations
            if (i % 1000000 == 0) 
            {
                Console.WriteLine("Best score: " + (bestSolution.score / 60 / 1000) + ", Working score: " + (workingScore / 60 / 1000) + ", Progress " + (int)((double)i / iterations * 100) + "%, Mode Progress " + (int)((double)(i % modeIterations) / modeIterations * 100) + "%, Temperature: " + judge.T);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q) // Quit the program when the user presses 'q'
                    {
                        bestSolution.UpdateSolution(workingSchedule, workingScore);
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

            if (i % modeIterations == 0 && i > 0)
            {
                judge.T = beginT;
                
                Console.WriteLine("Swapped modes!");
            }

        }
    }

    public void RandomWalk(Random rng, Judge judge, int workingScore, Schedule workingSchedule, Solution bestSolution, ulong iterations)
    {

    }

    int addWeight = 2;  // bug with these combo of these 3
    int removeWeight = 1; //
    int shuffleScheduleWeight = 2; //
    int shuffleWorkDayWeight = 1;
    int shuffleRouteWeight = 4;
    int swapDeliveriesWeight = 0;

    int addWeightSum;
    int removeWeightSum;
    int shuffleScheduleSum;
    int shuffleWorkDayWeightSum;
    int shuffleRouteWeightSum;
    int swapDeliveriesWeightSum;
    int totalWeightSum;

    public int TryIterate(int workingScore, Schedule schedule, Random rng, Judge judge)
    {
        int weight = rng.Next(0, totalWeightSum); 

        if (weight < addWeightSum)
        {
            schedule.AddRandomDelivery(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                statistics.addScoreDelta += judge.timeDelta;
                statistics.addSuccessCount++;

                return workingScore + judge.timeDelta;
            }
            else
            {
                statistics.addFailCount++;
            }
        }
        else if (weight < removeWeightSum)
        {
            schedule.RemoveRandomDelivery(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                statistics.removeScoreDelta += judge.timeDelta;
                statistics.removeSuccessCount++;

                return workingScore + judge.timeDelta;
            }
            else
            {
                statistics.removeFailCount++;
            }
        }
        else if (weight < shuffleScheduleSum)
        {
            schedule.ShuffleSchedule(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                statistics.shuffleScheduleScoreDelta += judge.timeDelta;
                statistics.shuffleScheduleSuccessCount++;

                return workingScore + judge.timeDelta;
            }
            else
            {
                statistics.shuffleScheduleFailCount++;
            }
        }
        else if (weight < shuffleWorkDayWeightSum)
        {
            schedule.ShuffleWorkDay(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                statistics.shuffleWorkDayScoreDelta += judge.timeDelta;
                statistics.shuffleWorkDaySuccessCount++;

                return workingScore + judge.timeDelta;
            }
            else
            {
                statistics.shuffleWorkDayFailCount++;
            }
        }
        else if (weight < shuffleRouteWeightSum)
        {
            schedule.ShuffleRoute(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                statistics.shuffleRouteScoreDelta += judge.timeDelta;
                statistics.shuffleRouteSuccessCount++;
                
                return workingScore + judge.timeDelta;
            }
            else
            {
                statistics.shuffleRouteFailCount++;
            }
        }
        //else if (weight < swapDeliveriesWeightSum)
        //{
        //    schedule.CompleteRandomSwap(rng, judge);

        //    if (judge.GetJudgement() == Judgement.Pass)
        //    {
        //        statistics.swapDeliveryScoreDelta += judge.timeDelta;
        //        statistics.swapDeliverySuccessCount++;

        //        return workingScore + judge.timeDelta;
        //    }
        //    else
        //    {
        //        statistics.swapDeliveryFailCount++;
        //    }
        //} 

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
    public int scoreDelta; //newScore - oldScore (negative score suggests improvement!)
    public int timeDelta;
    public int timePenalty;
    public int garbagePenalty;
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
            double frac = -(timeDelta + timePenalty + garbagePenalty) / T; // '-', because we want to minimize here
            double res = Math.Exp(frac);
            if (res >= rng.NextDouble())
                judgement = Judgement.Pass;//return Judgement.Pass;
            else
                judgement = Judgement.Fail;
        }

        return judgement;
    }

    public void Reset()
    {
        scoreDelta = 0;
        timeDelta = 0;
        judgement = Judgement.Undecided;
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