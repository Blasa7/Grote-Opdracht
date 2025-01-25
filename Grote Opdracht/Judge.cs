public class Judge
{
    public int timeDelta;
    public int timePenaltyDelta;
    public int garbagePenaltyDelta;

    public int totalTimePenalty;
    public int totalGarbagePenalty;

    public bool updatedPenalties = false;

    public int minRoutes = 14;
    public int maxRoutes = 16;

    public double garbagePenaltyMultiplier = 4.32 * 8;
    public double timePenaltyMultiplier = 0.08;

    public float beginT;
    public float endT;

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
        this.timePenaltyDelta += timePenaltyDelta;
        this.garbagePenaltyDelta += garbagePenaltyDelta;
    }

    public void OverrideJudge(Judgement judgement)
    {
        this.judgement = judgement;
    }

    public Judgement GetJudgement()
    {
        if (judgement == Judgement.Undecided) //If no function has overidden the judgement
        {
            double maxWeightMultiplier = 9;
            double weight = ((beginT - T) / beginT) * maxWeightMultiplier + 1;
            double weightedGarbagePenalty = garbagePenaltyDelta * garbagePenaltyMultiplier * weight;
            double weightedTimePenalty = timePenaltyDelta * timePenaltyMultiplier * weight;
            double numerator = -(timeDelta + weightedTimePenalty + weightedGarbagePenalty);

            double frac = numerator / T; // '-', because we want to minimize here
            double res = Math.Exp(frac);
            if (res >= rng.NextDouble())
            {
                judgement = Judgement.Pass;
            }
            else
                judgement = Judgement.Fail;
        }

        if (!updatedPenalties && judgement == Judgement.Pass)
        {
            totalTimePenalty += timePenaltyDelta;
            totalGarbagePenalty += garbagePenaltyDelta;
            updatedPenalties = true;
        }

        return judgement;
    }

    public void Reset()
    {
        updatedPenalties = false;
        timeDelta = 0;
        timePenaltyDelta = 0;
        garbagePenaltyDelta = 0;
        judgement = Judgement.Undecided;
    }
}

public enum Judgement
{
    Fail = 0,
    Pass = 1,
    Undecided = -1
}
