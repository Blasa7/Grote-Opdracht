using System;
using System.Collections.Generic;
using System.Linq;

class Annealing
{
    public Solution Run()
    {
        Solution currentSolution = new Solution();
        currentSolution.GenerateInitialSolution();
        Solution bestSolution = Solution.Copy(currentSolution);
        Solution neighbour;
        Random rng = new Random();
        float T = 10; //Dummy value for now
        int maxIter = 1000000; //1 million for now
        Judge judge = new Judge(T, rng);

        for (int i=0; i < maxIter; i++)
        {
            T = GetTemperature(T);
            judge.T = T;
            neighbour = GetNeighbour(currentSolution, rng, judge);
            if (currentSolution.score < bestSolution.score)
            {
                bestSolution = Solution.Copy(currentSolution);
            }
        }

        return currentSolution;

    }

    public Solution GetNeighbour(Solution current, Random rng, Judge judge)
    {
        judge.Reset();
        //Dummy value

        //Things like 2-opt, swap nodes, insert here
        return current;
    }

    public float GetTemperature(float T)
    {
        float alpha = 0.95f; //Parameter to be played around with
        return T*alpha;
    }

}

class Judge
{
    float score; //newScore - oldScore (negative score suggests improvement!)
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
                return Judgement.Pass;
            return Judgement.Fail;
        }
        else
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
    Undecided
}