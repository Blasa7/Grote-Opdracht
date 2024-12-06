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
        float T = 10; //Dummy value for now
        int maxIter = 1000000; //1 million for now (100000000)
        Judge judge = new Judge(T, rng);
        float workingScore = bestSolution.score;
        Console.WriteLine(workingScore);

        for (int i = 0; i < maxIter; i++)
        {
            T = GetTemperature(T);
            judge.T = T;

            workingScore = TryIterate(workingScore, workingSchedule, rng, judge);

            if (workingScore < bestSolution.score)
            {
                bestSolution.UpdateSolution(workingSchedule, workingScore);
            }

            judge.Reset();
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

        if (weight < 0.5 && weight > 0.2)
        {
            if (schedule.unfulfilledAddresses.currentIndex > 0)
            {
                schedule.AddRandomDelivery(rng, judge);
            }
        }
        else if (weight < 0.2)
        {
            schedule.RemoveRandomDelivery(rng, judge);
        }
        else // shuffle half the time (for now)
        {
            schedule.ShuffleNode(rng, judge);
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