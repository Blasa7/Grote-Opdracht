class Annealing
{
    Statistics statistics;

    public Solution Run()
    {
        Schedule workingSchedule = new Schedule(Input.orders);
        Solution bestSolution = new Solution();
        Random rng = new Random();
        float T = 10; //Dummy value for now
        int randomWalks = 50;
        ulong randomWalkIterations = 100;
        ulong maxIter = 10000000;//50000000; //1 million for now (100000000)
        Judge judge = new Judge(T, rng);
        int workingScore = bestSolution.score;
        Console.WriteLine(workingScore / 60 / 1000);

        addWeightSum = addWeight;
        removeWeightSum = addWeightSum + removeWeight;
        shuffleScheduleSum = removeWeightSum + shuffleScheduleWeight;
        shuffleWorkDayWeightSum = shuffleScheduleSum + shuffleWorkDayWeight;
        shuffleRouteWeightSum = shuffleWorkDayWeightSum + shuffleRouteWeight;
        totalWeightSum = shuffleRouteWeightSum + 1;

        //Initial solution
        for (int i = 0; i < 5000; i++)
        {
            judge.Reset();
            judge.OverrideJudge(Judgement.Pass);
            workingSchedule.AddRandomDelivery(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
                workingScore += judge.score;
        }

        bestSolution.UpdateSolution(workingSchedule, workingScore);

        judge.Reset();

        Console.WriteLine(workingScore / 60 / 1000);

        //Start iterating

        for (int w = 0; w < randomWalks; w++)
        {
            Console.WriteLine("Random walk: " + w);
            Console.WriteLine(workingScore / 60 / 1000);
            for (ulong i = 0; i < randomWalkIterations; i++)
            {
                judge.OverrideJudge(Judgement.Pass);

                workingScore = TryIterate(workingScore, workingSchedule, rng, judge);

                if (workingScore < bestSolution.score)
                {
                    bestSolution.UpdateSolution(workingSchedule, workingScore);
                    workingScore = bestSolution.score;
                }

                judge.Reset();
            }

            Console.WriteLine(workingScore / 60 / 1000);

            for (ulong i = 0; i < maxIter; i++)
            {
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
            }

            Console.WriteLine(workingScore / 60 / 1000);
        }

        for (ulong i = 0; i < maxIter; i++)
        {
            if (i % 10000 == 0)
            {
                judge.T = GetTemperature(judge.T);
            }

            int neighborScore = TryIterate(workingScore, workingSchedule, rng, judge);

            if (neighborScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, neighborScore);
                workingScore = bestSolution.score;
            }

            judge.Reset();
        }



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

        return bestSolution;
    }

    public float GetTemperature(float T)
    {
        float alpha = 0.95f; //Parameter to be played around with
        return T*alpha;
    }

    public void IteratedLocalSearch(Schedule workingSchedule, Solution bestSolution, ulong iterations)
    {

    }

    public void SimmulatedAnnealing(Schedule workingSchedule, Solution bestSolution, ulong iterations, int T)
    {

    }

    public void RandomWalk(Schedule workingSchedule, Solution solution, ulong iterations)
    {

    }

    int addWeight = 20;
    int removeWeight = 7;
    int shuffleScheduleWeight = 15;
    int shuffleWorkDayWeight = 20;
    int shuffleRouteWeight = 50;

    int addWeightSum;
    int removeWeightSum;
    int shuffleScheduleSum;
    int shuffleWorkDayWeightSum;
    int shuffleRouteWeightSum;
    int totalWeightSum;

    public int TryIterate(int workingScore, Schedule schedule, Random rng, Judge judge)
    {
        int weight = rng.Next(0, totalWeightSum);//rng.NextSingle();

        if (weight < addWeightSum)
        {
            if (schedule.unfulfilledAddresses.currentIndex > 0)
            {
                schedule.AddRandomDelivery(rng, judge);
            }
        }
        else if (weight < removeWeightSum)
        {
            schedule.RemoveRandomDelivery(rng, judge);
        }
        else if (weight < shuffleScheduleSum) //Not too many times
        {
            schedule.ShuffleSchedule(rng, judge);
        }
        else if (weight < shuffleWorkDayWeightSum)
        {
            schedule.ShuffleWorkDay(rng, judge);
        }
        else if (weight < shuffleRouteWeightSum)
        {
            schedule.ShuffleRoute(rng, judge);
        }

        if (judge.GetJudgement() == Judgement.Pass)
        {
            return workingScore + judge.score;
        }
        return workingScore;
    }
}

class Judge
{
    public int score; //newScore - oldScore (negative score suggests improvement!)
    Judgement judgement;

    public float T;
    public Random rng;

    public Judge(float T, Random rng)
    {
        this.T = T;
        this.rng = rng;

        Reset();
    }

    public void Testify(int weight)
    {
        score += weight;
    }

    public void OverrideJudge(Judgement judgement)
    {
        this.judgement = judgement;
    }

    public Judgement GetJudgement()
    {
        if (judgement == Judgement.Undecided) //If no function has overidden the judgement
        {
            double frac = -score / T; // '-', because we want to minimize here
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
        score = 0;
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

}