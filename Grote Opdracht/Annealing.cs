﻿class Annealing
{
    Statistics statistics = new Statistics();

    public Solution Run()
    {
        Schedule workingSchedule = new Schedule(Input.orders);
        Solution bestSolution = new Solution();
        Random rng = new Random();
        float T = 100; //Dummy value for now
        ulong million = 5000000;
        ulong maxIter = 50 * million; // How many iterations: x * 1.000.000
        Judge judge = new Judge(T, rng);
        int workingScore = bestSolution.score;
        Console.WriteLine(workingScore / 60 / 1000);

        addWeightSum = addWeight;
        removeWeightSum = addWeightSum + removeWeight;
        shuffleScheduleSum = removeWeightSum + shuffleScheduleWeight;
        shuffleWorkDayWeightSum = shuffleScheduleSum + shuffleWorkDayWeight;
        shuffleRouteWeightSum = shuffleWorkDayWeightSum + shuffleRouteWeight;
        swapDeliveriesWeightSum = shuffleRouteWeightSum + swapDeliveriesWeight;
        totalWeightSum = swapDeliveriesWeightSum + 1;

        //Initial solution
        for (int i = 0; i < 5000; i++)
        {
            judge.Reset();
            judge.OverrideJudge(Judgement.Pass);
            //workingSchedule.AddRandomDelivery(rng, judge);
            workingSchedule.AddRandomDelivery(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
                workingScore += judge.score;
        }

        bestSolution.UpdateSolution(workingSchedule, workingScore);

        judge.Reset();

        Console.WriteLine(workingScore / 60 / 1000);

        //Start iterating

        SimmulatedAnnealing(rng, judge, workingScore, workingSchedule, bestSolution, maxIter, T);


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

        return bestSolution;
    }

    public float GetTemperature(float T)
    {
        float alpha = 0.99f; //Parameter to be played around with
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
                if (previousScore == workingScore)
                {
                    judge.T = T;
                }

                previousScore = workingScore;

                Console.WriteLine(bestSolution.score / 60 / 1000);

                if (bestSolution.score < 358500000) // Score < 6000 min
                    return;
            }
        }
    }

    public void RandomWalk(Schedule workingSchedule, Solution solution, ulong iterations)
    {

    }

    int addWeight = 20;
    int removeWeight = 7;
    int shuffleScheduleWeight = 15;
    int shuffleWorkDayWeight = 20;
    int shuffleRouteWeight = 50;
    int swapDeliveriesWeight = 100;

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
                statistics.addScoreDelta += judge.score;
                statistics.addSuccessCount++;

                return workingScore + judge.score;
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
                statistics.removeScoreDelta += judge.score;
                statistics.removeSuccessCount++;

                return workingScore + judge.score;
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
                statistics.shuffleScheduleScoreDelta += judge.score;
                statistics.shuffleScheduleSuccessCount++;

                return workingScore + judge.score;
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
                statistics.shuffleWorkDayScoreDelta += judge.score;
                statistics.shuffleWorkDaySuccessCount++;

                return workingScore + judge.score;
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
                statistics.shuffleWorkDayScoreDelta += judge.score;
                statistics.shuffleWorkDaySuccessCount++;
                
                return workingScore + judge.score;
            }
            else
            {
                statistics.shuffleWorkDayFailCount++;
            }
        }
        else if (weight < swapDeliveriesWeightSum)
        {
            schedule.SwapDeliveries(rng, judge);

            if (judge.GetJudgement() == Judgement.Pass)
            {
                statistics.swapDeliveryScoreDelta += judge.score;
                statistics.swapDeliverySuccessCount++;

                return workingScore + judge.score;
            }
            else
            {
                statistics.swapDeliveryFailCount++;
            }
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
            $"Swap delivery score delta: {swapDeliveryScoreDelta / 60 / 1000} \n" +
            $"Swap delivery success count: {swapDeliverySuccessCount} \n" +
            $"Swap delivery fail count: {swapDeliveryFailCount}";
    }
}