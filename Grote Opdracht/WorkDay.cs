﻿class WorkDay : IClonable<WorkDay> // This is a linked list
{
    public IndexedLinkedList<Route> workDay;

    public int weekDay; // 0, 1, 2, 3, 4

    public int totalDuration = 0;
    public int maximumDuration = 43200000; // 720 min, 432000 sec //in minutes aka 11.5 hours in a work day

    public WorkDay(int weekDay)
    {
        this.weekDay = weekDay;
        int numRoutes = 10;
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

        //TODO
        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);

        ////TODO make soft contraint
        //if (totalDuration + timeDelta > maximumDuration)
        //{
        //    int penalty;
        //    if (totalDuration > maximumDuration) // time was already exceeded
        //    {
        //        penalty = timeDelta * timePenaltyMultiplier;
        //    }
        //    else // time will now be exceeded
        //    {
        //        penalty = (totalDuration + timeDelta - maximumDuration) * timePenaltyMultiplier; // only add excess
        //    }
        //    int timePenaltyDelta = penalty;

        //    //Testify only timePenalty here, others are done in Route.StageRandomStop above
        //    judge.Testify(0, timePenaltyDelta, 0);
        //}

        //int timePenaltyDelta = CalculateTimePenalty(timeDelta);
        //judge.Testify(0, timePenaltyDelta, 0);

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

        //TODO
        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);

        //int timePenaltyDelta = CalculateTimePenalty(timeDelta);
        //judge.Testify(0, timePenaltyDelta, 0);
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

        //TODO
        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);

        //int timePenaltyDelta = CalculateTimePenalty(timeDelta);
        //judge.Testify(0, timePenaltyDelta, 0);

    }

    public void ShuffleRoute(Random rng, Judge judge)
    {
        int workDayIndex = workDay.getRandomIncluded(rng);

        // Stage and testify
        workDay.nodes[workDayIndex].value.StageShuffleRoute(rng, judge, out Delivery changedDelivery, out Delivery newIndexDelivery, out int removeTimeDelta, out int addTimeDelta, out int timeDelta);

        //TODO
        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);

        //int timePenaltyDelta = CalculateTimePenalty(timeDelta);
        //judge.Testify(0, timePenaltyDelta, 0);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            totalDuration += timeDelta;
            workDay.nodes[workDayIndex].value.ShuffleRoute(changedDelivery, newIndexDelivery, removeTimeDelta, addTimeDelta);
        }
    }

    public int CalculateTimePenalty(int timeDelta)
    {
        int penalty = 0;
        if (timeDelta < 0) // improvement
        {
            if (totalDuration > maximumDuration) // it was exceeded
            {
                if (totalDuration + timeDelta < maximumDuration) // remove the excess penalty
                {
                    penalty = (maximumDuration - totalDuration);
                }
                else // it's still is exceeded
                {
                    penalty = timeDelta; // ((totaldur + timedelta) - totaldur)
                }
            }
        }
        else // timedelta > 0, unlikely but possible
        {
            if (totalDuration < maximumDuration) // it was NOT exceeded
            {
                if (totalDuration + timeDelta > maximumDuration) // it now will be exceeded
                {
                    penalty = (totalDuration + timeDelta - maximumDuration); // add the excess
                }
            }
            else // it was, and still will be exceeded
            {
                penalty = timeDelta; // ((totaldur + timedelta) - totaldur)
            }
        }
        return penalty;
    }

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