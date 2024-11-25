using System;
using System.Collections.Generic;
using System.Linq;

class Annealing
{
    public Solution Run()
    {
        Solution currentSolution = new Solution();
        Solution neighbour;
        Random rng = new Random();
        float T = 10; //Dummy value for now
        int maxIter = 1000000; //1 million for now

        for (int i=0; i < maxIter; i++)
        {
            T = GetTemperature(T);
            neighbour = GetNeighbour(currentSolution);
            if (AcceptNeighbour(currentSolution, neighbour, T, rng))
            {
                currentSolution = neighbour;
            }
        }

        return currentSolution;

    }

    public Solution GetNeighbour(Solution curry)
    {
        //Dummy value
        return curry;
    }

    public float GetTemperature(float T)
    {
        //Dummy value
        return T;
    }

    public bool AcceptNeighbour(Solution current, Solution neighbour, float T, Random rng)
    {
        //Dummy values
        int oldScore = 0, newScore = 0;

        double frac = (newScore - oldScore) / T;
        double res = Math.Exp(frac);
        if (res >= rng.NextDouble())
            return true;
        return false;
    }
}

class Judge
{
    int score;
    Judgement judgement;

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
        if (judgement == Judgement.Undecided)
        {
            //Decide here
            return Judgement.Pass;
        }
        else
            return judgement;
    }
}

enum Judgement
{
    Fail = 0,
    Pass = 1,
    Undecided
}