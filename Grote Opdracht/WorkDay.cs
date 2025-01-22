class WorkDay : IClonable<WorkDay> // This is a linked list
{
    public IndexedLinkedList<Route> workDay;

    public int weekDay; // 0, 1, 2, 3, 4

    public int totalDuration = 0;
    public int maximumDuration = 43200000; // 720 min, 432000 sec //in minutes aka 11.5 hours in a work day

    public WorkDay(int weekDay)
    {
        this.weekDay = weekDay;
        int numRoutes = 3;
        workDay = new IndexedLinkedList<Route>(numRoutes); //Hard coded size may need to be larger

        for (int i = 0; i < numRoutes; i++)
        {
            workDay.InsertLast(new Route());
        }
    }

    /// <summary>
    /// This functions only testifies the changes to the judge and return the values to enact the changes in AddStop
    /// </summary>
    public void StageRandomStop(Delivery delivery, int routeNum, Random rng, Judge judge, out int workDayIndex, out int routeIndex, out int timeDelta, out int routeNumDelta)
    {
        //First calculate variables
        workDayIndex = workDay.getRandomIncluded(rng);

        workDay.nodes[workDayIndex].value.StageRandomStop(delivery, routeNum, rng, judge, out routeIndex, out timeDelta, out routeNumDelta);

        ////TODO
        //if (totalDuration + timeDelta > maximumDuration)
        //    judge.OverrideJudge(Judgement.Fail);

        int newTime = totalDuration + timeDelta;

        //Soft contraint (Assume timeDelta is positive)
        //if (newTime > maximumDuration)
        //{
        //    int overLimit = newTime - maximumDuration;
        //    int penalty = Math.Min(overLimit, timeDelta);

        //    judge.Testify(0, penalty, 0);
        //}

        int penalty = CalculateTimePenalty(timeDelta);
        if (timeDelta > 0)
            judge.Testify(0, penalty, 0);
        else
            judge.Testify(0, -penalty, 0);

    }

    public void AddStop(Delivery delivery, int workDayIndex, int routeIndex, int timeDelta)
    {
        totalDuration += timeDelta;

        delivery.workDayNode = workDay.nodes[workDayIndex];
        workDay.nodes[workDayIndex].value.AddStop(delivery, routeIndex, timeDelta);
    }

    public void StageRemoveStop(Delivery delivery, int routeNum, Judge judge, out int timeDelta, out int routeNumDelta)
    {
        workDay.nodes[delivery.workDayNode.index].value.StageRemoveStop(delivery, routeNum, judge, out timeDelta, out routeNumDelta);

        ////TODO
        //if (totalDuration + timeDelta > maximumDuration)
        //    judge.OverrideJudge(Judgement.Fail);

        int newTime = totalDuration + timeDelta;

        ////Soft contraint (Assume timeDelta is negative)
        //if (totalDuration > maximumDuration)
        //{
        //    int overLimit = newTime - maximumDuration;
        //    int penalty = Math.Min(overLimit, timeDelta);

        //    judge.Testify(0, -penalty, 0);
        //}

        int penalty = CalculateTimePenalty(timeDelta);
        if (timeDelta > 0)
            judge.Testify(0, penalty, 0);
        else
            judge.Testify(0, -penalty, 0);

    }

    public void RemoveStop(Delivery delivery, int timeDelta)
    {
        totalDuration += timeDelta;

        workDay.nodes[delivery.workDayNode.index].value.RemoveStop(delivery, timeDelta);
    }

    /// <summary>
    /// Shuffles between routes on the same day and the same truck.
    /// </summary>
    public void ShuffleWorkDay(int routeNum, Random rng, Judge judge, out int routeNumDelta)
    {
        routeNumDelta = 0;

        Delivery changedDelivery = StageRemoveShuffleWorkDay(routeNum, rng, judge, out int removeTimeDelta, out int removeRouteNumDelta);

        if (changedDelivery == null)
            return;

        StageShuffleWorkDay(changedDelivery, routeNum, rng, judge, out int workDayIndex, out int routeIndex, out int addTimeDelta, out int addRouteNumDelta);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            RemoveStop(changedDelivery, removeTimeDelta);
            AddStop(changedDelivery, workDayIndex, routeIndex, addTimeDelta);

            routeNumDelta = removeRouteNumDelta + addRouteNumDelta;
        }
    }

    Delivery StageRemoveShuffleWorkDay(int routeNum, Random rng, Judge judge, out int timeDelta, out int routeNumDelta)
    {
        int workDayIndex = workDay.getRandomIncluded(rng);

        if (workDay.nodes[workDayIndex].value.route.currentIndex == 0)
        {
            judge.OverrideJudge(Judgement.Fail);
            timeDelta = 0;
            routeNumDelta = 0;
            return null;
        }

        int routeIndex = workDay.nodes[workDayIndex].value.route.getRandomIncluded(rng);

        Delivery removedDelivery = workDay.nodes[workDayIndex].value.route.nodes[routeIndex].value;

        StageRemoveStop(removedDelivery, routeNum, judge, out timeDelta, out routeNumDelta);

        return removedDelivery;
    }

    void StageShuffleWorkDay(Delivery oldDelivery,int routeNum, Random rng, Judge judge, out int workDayIndex, out int routeIndex, out int timeDelta, out int routeNumDelta)
    {
        //First calculate variables
        workDayIndex = (oldDelivery.workDayNode.index + rng.Next(1, workDay.currentIndex + 1)) % (workDay.currentIndex + 1);

        workDay.nodes[workDayIndex].value.StageRandomStop(oldDelivery, routeNum, rng, judge, out routeIndex, out timeDelta, out routeNumDelta);

        ////TODO
        //if (totalDuration + timeDelta > maximumDuration)
        //    judge.OverrideJudge(Judgement.Fail);

        int penalty = CalculateTimePenalty(timeDelta);
        if (timeDelta > 0)
            judge.Testify(0, penalty, 0);
        else
            judge.Testify(0, -penalty, 0);

    }

    public void ShuffleRoute(Random rng, Judge judge)
    {
        int workDayIndex = workDay.getRandomIncluded(rng);

        // Stage and testify
        workDay.nodes[workDayIndex].value.StageShuffleRoute(rng, judge, out Delivery changedDelivery, out Delivery newIndexDelivery, out int removeTimeDelta, out int addTimeDelta, out int timeDelta);

        ////TODO
        //if (totalDuration + timeDelta > maximumDuration)
        //    judge.OverrideJudge(Judgement.Fail);

        int penalty = CalculateTimePenalty(timeDelta);
        if (timeDelta > 0)
            judge.Testify(0, penalty, 0);
        else
            judge.Testify(0, -penalty, 0);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            totalDuration += timeDelta;
            workDay.nodes[workDayIndex].value.ShuffleRoute(changedDelivery, newIndexDelivery, removeTimeDelta, addTimeDelta);
        }
    }

    public int CalculateTimePenalty(int timeDelta)
    {
        int penalty;
        int newTotalTime = totalDuration + timeDelta;
        if (newTotalTime > maximumDuration) //Already was and still is over the limit.
        {
            penalty = timeDelta;
        }
        else if (totalDuration > maximumDuration && newTotalTime < maximumDuration)  //Was over the limit but now no longer is.
        {
            penalty = maximumDuration - totalDuration;
        }
        else if (totalDuration < maximumDuration && newTotalTime > maximumDuration) //Was not over the limit but now it is.
        {
            penalty = totalDuration - maximumDuration;
        }
        else //"Wait, it is not over the limit?"  "Never was..."
        {
            penalty = 0;
        }

        return penalty; //returns penalty


        /*int penalty = 0;
        int newTime = totalDuration + timeDelta;
        if (timeDelta < 0) // improvement
        {
            if (totalDuration > maximumDuration)
            {
                int overLimit = newTime - maximumDuration;
                penalty = Math.Min(overLimit, timeDelta);
            }
        }
        else // timedelta > 0
        {
            if (newTime > maximumDuration)
            {
                int overLimit = newTime - maximumDuration;
                penalty = Math.Min(overLimit, timeDelta);
            }
        }
        return penalty;*/
    }

    /*        if (collectedGarbage > maximumGarbage)
        {
            //120 > 100

            int overLimit = collectedGarbage - maximumGarbage; //120 - 100
            int delta = delivery.address.garbageAmount; // 5
            //int newGarbageAmount = collectedGarbage - delta;

            penalty = Math.Min(overLimit, delta);
            
        }
*/

    public WorkDay Clone()
    {
        WorkDay copy = new WorkDay(0);

        copy.workDay = workDay.Clone();
        copy.weekDay = weekDay;
        copy.totalDuration = totalDuration;

        return copy;
    }

    public override string ToString()
    {
        return workDay.ToString();
    }
}