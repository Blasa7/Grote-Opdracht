using System;
using System.Collections.Generic;
using System.Linq;

class Annealing
{
    public Solution Run()
    {
        Schedule workingSchedule = new Schedule(Input.orders);
        Solution bestSolution = new Solution();
        Random rng = new Random();
        float T = 0.99999f; //Dummy value for now
        int maxIter = 1000000; //1 million for now (100000000)
        Judge judge = new Judge(T, rng);
        float workingScore = bestSolution.score;
        Console.WriteLine(workingScore);

        //Initial solution

        /*for (int i = 0; i < Input.orderCount; i++)
        {
            judge.OverrideJudge(Judgement.Pass);

            workingSchedule.AddRandomDelivery(rng, judge);
        }

        workingScore += judge.score;
        bestSolution.UpdateSolution(workingSchedule, workingScore);

        judge.Reset();*/

        //Start iterating

        for (int i = 0; i < maxIter; i++)
        {
            T = GetTemperature(T);
            judge.T = T;

            float neighborScore = TryIterate(workingScore, workingSchedule, rng, judge);

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

                    for (int l = 0; l <r.value.route.currentIndex; l++)
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

    public float TryIterate(float workingScore, Schedule schedule, Random rng, Judge judge)
    {
        float weight = rng.NextSingle();

        if (weight < 0.2)
        {
            if (schedule.unfulfilledAddresses.currentIndex > 0)
            {
                schedule.AddRandomDelivery(rng, judge);
            }
        }
        else if (weight < 0.4)
        {
            schedule.RemoveRandomDelivery(rng, judge);
        }
        else if (weight < 0.6)
        {
            schedule.ShuffleSchedule(rng, judge);
        }
        else if (weight < 0.8)
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
    public float score; //newScore - oldScore (negative score suggests improvement!)
    Judgement judgement;

    public float T;
    public Random rng;

    public Judge(float T, Random rng)
    {
        this.T = T;
        this.rng = rng;

        Reset();
    }

    public void Testify(float weight)
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