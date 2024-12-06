
class WorkDay : IClonable<WorkDay> // This is a linked list
{
    public IndexedLinkedList<Route> workDay;

    public int weekDay; // 0, 1, 2, 3, 4

    float totalDuration = 0;
    float maximumDuration = 690; //in minutes aka 11.5 hours in a work day

    public WorkDay(int weekDay, int maximumSize)
    {
        this.weekDay = weekDay;
        Route start = new Route();//TODO MAKE SOME SYSTEM TO ADD MORE ROUTES TO A WORKDDAY ITS ALWAYS 1
        workDay = new IndexedLinkedList<Route>(start, 10); //Hard coded size may need to be larger
        workDay.InsertLast(new Route()); //TODO maybe add some smarter way to do this
        workDay.InsertLast(new Route());
        workDay.InsertLast(new Route());
    }

    /// <summary>
    /// This functions only testifies the changes to the judge and return the values to enact the changes in AddStop
    /// </summary>
    public void StageRandomStop(Delivery delivery, Random rng, Judge judge, out int workDayIndex, out int routeIndex, out float timeDelta)
    {
        //First calculate variables
        workDayIndex = workDay.getRandomIncluded(rng);

        float maximumTimeLeft = maximumDuration - totalDuration;

        workDay.nodes[workDayIndex].value.StageRandomStop(delivery, maximumTimeLeft, rng, judge, out routeIndex, out timeDelta);

        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);
    }

    public void AddStop(Delivery delivery, int workDayIndex, int routeIndex, float timeDelta)
    {
        totalDuration += timeDelta;

        delivery.workDayNode = workDay.nodes[workDayIndex];
        workDay.nodes[workDayIndex].value.AddStop(delivery, routeIndex, timeDelta);
    }

    public void StageRemoveStop(Delivery delivery, Judge judge, out float timeDelta)
    {
        workDay.nodes[delivery.workDayNode.index].value.StageRemoveStop(delivery, judge, out timeDelta);

        if (totalDuration + timeDelta > maximumDuration)
            judge.OverrideJudge(Judgement.Fail);
    }

    public void RemoveStop(Delivery delivery, float timeDelta)
    {
        totalDuration += timeDelta;

        workDay.nodes[delivery.workDayNode.index].value.RemoveStop(delivery, timeDelta);
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

