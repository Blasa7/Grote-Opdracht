public class Weights()
{
    public int addWeight = 100;
    public int removeWeight = 50;
    public int shuffleScheduleWeight = 200;
    public int shuffleWorkDayWeight = 200;
    public int shuffleRouteWeight = 200;

    public int addWeightSum;
    public int removeWeightSum;
    public int shuffleScheduleSum;
    public int shuffleWorkDayWeightSum;
    public int shuffleRouteWeightSum;
    public int totalWeightSum;

    public void ResetWeights()
    {
        addWeight = 100;
        removeWeight = 50;
        shuffleScheduleWeight = 200;
        shuffleWorkDayWeight = 200;
        shuffleRouteWeight = 200;
    }

    public void RecalculateWeights()
    {
        addWeightSum = addWeight;
        removeWeightSum = addWeightSum + removeWeight;
        shuffleScheduleSum = removeWeightSum + shuffleScheduleWeight;
        shuffleWorkDayWeightSum = shuffleScheduleSum + shuffleWorkDayWeight;
        shuffleRouteWeightSum = shuffleWorkDayWeightSum + shuffleRouteWeight;
        totalWeightSum = shuffleRouteWeightSum + 1;
    }

    public static Weights StartWeight()
    {
        Weights weights = new Weights();
        weights.ResetWeights();
        weights.RecalculateWeights();

        return weights;
    }
}
