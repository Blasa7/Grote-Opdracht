class WorkDay : IClonable<WorkDay> // This is a linked list
{
    public IndexedLinkedList<Route> workDay;

    public int weekDay; // 0, 1, 2, 3, 4

    public int totalDuration = 0;
    public int maximumDuration = 43200000; // 720 min, 432000 sec //in minutes aka 11.5 hours in a work day

    public WorkDay(int weekDay, int maximumSize)
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
    public void StageRandomStop(Delivery delivery, Random rng, Judge judge, out int workDayIndex, out int routeIndex, out int timeDelta)
    {
        //First calculate variables
        workDayIndex = workDay.getRandomIncluded(rng);

        workDay.nodes[workDayIndex].value.StageRandomStop(delivery, rng, judge, out routeIndex, out timeDelta);

        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);
    }

    public void AddStop(Delivery delivery, int workDayIndex, int routeIndex, int timeDelta)
    {
        totalDuration += timeDelta;

        delivery.workDayNode = workDay.nodes[workDayIndex];
        workDay.nodes[workDayIndex].value.AddStop(delivery, routeIndex, timeDelta);
    }

    public void StageRemoveStop(Delivery delivery, Judge judge, out int timeDelta)
    {
        workDay.nodes[delivery.workDayNode.index].value.StageRemoveStop(delivery, judge, out timeDelta);

        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);
    }

    public void RemoveStop(Delivery delivery, int timeDelta)
    {
        totalDuration += timeDelta;

        workDay.nodes[delivery.workDayNode.index].value.RemoveStop(delivery, timeDelta);
    }

    /// <summary>
    /// Shuffles between routes on the same day and the same truck.
    /// </summary>
    public void ShuffleWorkDay(Random rng, Judge judge)
    {
        Delivery changedDelivery = StageRemoveShuffleWorkDay(rng, judge, out int removeTimeDelta);

        if (changedDelivery == null)
            return;

        StageShuffleWorkDay(changedDelivery, rng, judge, out int workDayIndex, out int routeIndex, out int addTimeDelta);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            RemoveStop(changedDelivery, removeTimeDelta);
            AddStop(changedDelivery, workDayIndex, routeIndex, addTimeDelta);
        }
    }

    Delivery StageRemoveShuffleWorkDay(Random rng, Judge judge, out int timeDelta)
    {
        int workDayIndex = workDay.getRandomIncluded(rng);

        if (workDay.nodes[workDayIndex].value.route.currentIndex == 0)
        {
            judge.OverrideJudge(Judgement.Fail);
            timeDelta = 0;
            return null;
        }

        int routeIndex = workDay.nodes[workDayIndex].value.route.getRandomIncluded(rng);

        Delivery removedDelivery = workDay.nodes[workDayIndex].value.route.nodes[routeIndex].value;

        StageRemoveStop(removedDelivery, judge, out timeDelta);

        return removedDelivery;
    }

    void StageShuffleWorkDay(Delivery oldDelivery, Random rng, Judge judge, out int workDayIndex, out int routeIndex, out int timeDelta)
    {
        //First calculate variables
        workDayIndex = (oldDelivery.workDayNode.index + rng.Next(1, workDay.currentIndex + 1)) % (workDay.currentIndex + 1);

        workDay.nodes[workDayIndex].value.StageRandomStop(oldDelivery, rng, judge, out routeIndex, out timeDelta);

        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);
    }

    public void ShuffleRoute(Random rng, Judge judge)
    {
        int workDayIndex = workDay.getRandomIncluded(rng);

        // Stage and testify
        workDay.nodes[workDayIndex].value.StageShuffleRoute(rng, judge, out Delivery changedDelivery, out Delivery newIndexDelivery, out int removeTimeDelta, out int addTimeDelta, out int timeDelta);

        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            totalDuration += timeDelta;
            workDay.nodes[workDayIndex].value.ShuffleRoute(changedDelivery, newIndexDelivery, removeTimeDelta, addTimeDelta);
        }
    }

    public WorkDay Clone()
    {
        WorkDay copy = new WorkDay(0, Input.orderCount);

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