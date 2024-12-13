class Annealing
{
    public static RunMode runMode = RunMode.TestRoutes;

    public Solution bestSolution = new Solution();
    Schedule workingSchedule = new Schedule();
    int workingScore;


    float T = 1;

    ulong iterations = 100000000; //million : 1000000, billion : 1000000000, trillion : 1000000000000, infinite : 18446744073709551615

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
        
        annealing.judge = new Judge(annealing.T, annealing.rng);

        return annealing;
    }

    public static Annealing FromFile(string path)
    {
        Annealing annealing = new Annealing();

        annealing.workingSchedule = Schedule.LoadSchedule(path, out annealing.workingScore);

        annealing.judge = new Judge(annealing.T, annealing.rng);

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
                //workingSchedule.AddRandomDelivery(rng, judge);
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
                //workingSchedule.AddRandomDelivery(rng, judge);
                workingSchedule.RemoveRandomDelivery(rng, judge);

                if (judge.GetJudgement() == Judgement.Pass)
                    workingScore += judge.timeDelta;
            }

            bestSolution.UpdateSolution(workingSchedule, workingScore);

            judge.Reset();

            Console.WriteLine("After deleting score: " + workingScore / 60 / 1000);
        }

        //Start iterating

        SimmulatedAnnealing(rng, judge, workingScore, workingSchedule, bestSolution, iterations, T);

        if (debugMessages)
            DebugMessages();

        return bestSolution;
    }

    public float GetTemperature(float T)
    {
        float alpha = 0.9999f; //Parameter to be played around with
        return T*alpha;
    }

    public void IteratedLocalSearch(Schedule workingSchedule, Solution bestSolution, ulong iterations)
    {

    }

    public void SimmulatedAnnealing(Random rng, Judge judge, int workingScore, Schedule workingSchedule, Solution bestSolution, ulong iterations, float T)
    { 
        int previousScore = -1;

        for (ulong i = 0; i < iterations; i++)
        {
            // Decrease the temperature every X iterations
            if (i % 10000 == 0)
            {
                judge.T = GetTemperature(judge.T);
            }

            workingScore = TryIterate(workingScore, workingSchedule, rng, judge);

            if (workingScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, workingScore);
                workingScore = bestSolution.score;
            }

            judge.Reset();

            // Increase the temperature if the score doesn't increase after Y iterations
            if (i % 1000000 == 0) 
            {
                //if (previousScore == workingScore)
                //{
                //    judge.T += T;//T;
                //}

                previousScore = workingScore;

                Console.WriteLine("Best score: " + (bestSolution.score / 60 / 1000) + ", Working score: " + (workingScore / 60 / 1000) + ", Progress " + (int)((double)i / iterations * 100) + "%");

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

            if (i % 1000000000 == 0)
            {
                SwapMode();
            }

        }
    }

    public void RandomWalk(Schedule workingSchedule, Solution solution, ulong iterations)
    {

    }

    public void SwapMode()
    {
        runMode = (RunMode)(((int)runMode + 1) % 2);

        switch (runMode)
        {
            case RunMode.TestRoutes:
                {
                    T = 10;
                    break;
                }
            case RunMode.RefineRoutes:
                {
                    T = 1;
                    break;
                }
        }

        judge.T = T;

        SwapWeights(runMode);
    }


    public void SwapWeights(RunMode mode)
    {
        switch (mode)
        {
            case RunMode.TestRoutes:
            {
                addWeight = 50;
                removeWeight = 35;
                shuffleScheduleWeight = 0;
                shuffleWorkDayWeight = 5;
                shuffleRouteWeight = 10;
                swapDeliveriesWeight = 5;
                break;
            }
            case RunMode.RefineRoutes:
            {
                addWeight = 20;
                removeWeight = 20;
                shuffleScheduleWeight = 5;
                shuffleWorkDayWeight = 5;
                shuffleRouteWeight = 50;
                swapDeliveriesWeight = 50;
                break;
            }
        }

        RecalculateWeights();
    }


    int addWeight; 
    int removeWeight;
    int shuffleScheduleWeight;
    int shuffleWorkDayWeight;
    int shuffleRouteWeight;
    int swapDeliveriesWeight;

    int addWeightSum;
    int removeWeightSum;
    int shuffleScheduleSum;
    int shuffleWorkDayWeightSum;
    int shuffleRouteWeightSum;
    int swapDeliveriesWeightSum;
    int totalWeightSum;

    public int TryIterate(int workingScore, Schedule schedule, Random rng, Judge judge)
    {
        int weight = rng.Next(0, totalWeightSum);//rng.NextSingle(); 

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
        else if (weight < shuffleScheduleSum) //Not too many times
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
        else if (weight < swapDeliveriesWeightSum)
        {
            schedule.CompleteRandomSwap(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                statistics.swapDeliveryScoreDelta += judge.timeDelta;
                statistics.swapDeliverySuccessCount++;

                return workingScore + judge.timeDelta;
            }
            else
            {
                statistics.swapDeliveryFailCount++;
            }
        } 

        return workingScore;
    }

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
    Judgement judgement;

    public float T;
    public Random rng;

    public Judge(float T, Random rng)
    {
        this.T = T;
        this.rng = rng;

        Reset();
    }

    public void Testify(int scoreDelta, int timeDelta)//int weight)
    {
        switch (Annealing.runMode)
        {
            case RunMode.TestRoutes:
                this.scoreDelta += scoreDelta;
                break;
            case RunMode.RefineRoutes:
                this.scoreDelta += timeDelta;
                break;
        }

        this.timeDelta += timeDelta;
    }

    public void OverrideJudge(Judgement judgement)
    {
        this.judgement = judgement;
    }

    public Judgement GetJudgement()
    {
        if (judgement == Judgement.Undecided) //If no function has overidden the judgement
        {
            double frac = -scoreDelta / T; // '-', because we want to minimize here
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

public enum RunMode : int 
{
    TestRoutes = 0,
    RefineRoutes = 1
}