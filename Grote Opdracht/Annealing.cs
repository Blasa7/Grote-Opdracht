class Annealing
{
    public Solution Run()
    {
        Schedule workingSchedule = new Schedule(Input.orders);
        Solution bestSolution = new Solution();
        Random rng = new Random();
        float T = 10; //Dummy value for now
        ulong maxIter = 50000000; //1 million for now (100000000)
        Judge judge = new Judge(T, rng);
        int workingScore = bestSolution.score;
        Console.WriteLine(workingScore / 60 / 1000);

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
        for (ulong i = 0; i < maxIter; i++)
        {
            if (i % 10000 == 0)
            {
                judge.T = GetTemperature(T);
                //Console.WriteLine(bestSolution.score / 60 / 1000);
                if (bestSolution.score < 357000000)
                    break;
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

    public int TryIterate(int workingScore, Schedule schedule, Random rng, Judge judge)
    {
        float weight = rng.NextSingle();

        if (weight < 0.1)
        {
            if (schedule.unfulfilledAddresses.currentIndex > 0)
            {
                schedule.AddRandomDelivery(rng, judge);
            }
        }
        else if (weight < 0.15)
        {
            schedule.RemoveRandomDelivery(rng, judge);
        }
        else if (weight < 0.3) //Not too many times
        {
            schedule.ShuffleSchedule(rng, judge);
        }
        else if (weight < 0.5)
        {
            schedule.ShuffleWorkDay(rng, judge);
        }
        else
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