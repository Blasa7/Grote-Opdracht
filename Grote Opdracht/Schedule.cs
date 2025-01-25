using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using static System.Formats.Asn1.AsnWriter;

class Schedule
{
    /// <summary>
    /// 5 indexed linked lists track which deliveries are being made on each day.
    /// </summary>
    public IndexedLinkedList<Delivery>[] schedule = new IndexedLinkedList<Delivery>[5];

    //2 trucks each 5 workdays
    public WorkDay[][] workDays = new WorkDay[2][] { new WorkDay[5], new WorkDay[5] };

    public IndexedLinkedList<Address> unfulfilledAddresses;

    public int routeNum = 0;

    public Schedule()
    {
        for (int i = 0; i < schedule.Length; i++)
            schedule[i] = new IndexedLinkedList<Delivery>(Input.orderCount);

        for (int i = 0; i < workDays.Length; i++)
        {
            for (int j = 0; j < workDays[i].Length; j++)
            {
                workDays[i][j] = new WorkDay(j);
            }
        }

        unfulfilledAddresses = new IndexedLinkedList<Address>(Input.orders.Length);

        for (int i = 0; i < Input.orders.Length; i++)
        {
            unfulfilledAddresses.InsertLast(new Address(Input.orders[i]));
        }
    }

    #region AddRandom

    public void AddRandomDelivery(Random rng, Judge judge)
    {

        if (unfulfilledAddresses.currentIndex == 0)
        {
            judge.OverrideJudge(Judgement.Fail);
            return;
        }


        //Get a random unfulfilled address
        int index = unfulfilledAddresses.GetRandomIncluded(rng);
        Address address = unfulfilledAddresses.nodes[index].value;

        Delivery[] deliveries = new Delivery[address.frequency];
        int[] workDayIndexes = new int[address.frequency];
        int[] routeIndexes = new int[address.frequency];
        int[] timeDeltas = new int[address.frequency];

        for (int i = 0; i < address.frequency; i++)
        {
            deliveries[i] = new Delivery(address);
            deliveries[i].truck = rng.Next(0, 2);
        }

        for (int i = 0; i < address.frequency; i++) // For each delivery
        {
            for (int j = 0; j < address.frequency - 1; j++) // For each delivery except the one from the previous loop
            {
                deliveries[i].others[j] = deliveries[(i + j + 1) % address.frequency]; 
            }
        }

        //The method to calculate the day differs for each frequency
        switch (address.frequency)
        {
            case 1: //Any random day of the week
                {
                    deliveries[0].day = rng.Next(0, 5);
                    break;
                }
            case 2: //Monday - Thursday or Tuesday - Friday
                {
                    int offset = rng.Next(0, 2);

                    deliveries[0].day = 0 + offset; //0 or 1
                    deliveries[1].day = 3 + offset; //3 or 4
                    break;
                }
            case 3: //Monday - Wendsday - Friday
                {
                    deliveries[0].day = 0;
                    deliveries[1].day = 2;
                    deliveries[2].day = 4;
                    break;
                }
            case 4: //All days except one
                {
                    int skipDay = rng.Next(0, 5);

                    for (int i = 0, day = 0; i < 4; i++, day++)
                    {
                        if (day == skipDay)
                            day++;

                        deliveries[i].day = day;
                    }
                    break;
                }
        }

        //Penalty gets removed
        int timeDelta = -address.emptyingTime * 3 * address.frequency;

        judge.Testify(timeDelta, 0,0);

        //Stage functions to randomly add are called

        int stagedRouteNum = routeNum;

        for (int i = 0; i < address.frequency; i++)
        {
            workDays[deliveries[i].truck][deliveries[i].day].StageAddStop(deliveries[i], stagedRouteNum, rng, judge, out workDayIndexes[i], out routeIndexes[i], out timeDeltas[i], out int routeNumDelta);
            stagedRouteNum += routeNumDelta;
        }

        if (stagedRouteNum > judge.maxRoutes)
        {
            judge.OverrideJudge(Judgement.Fail);
        }

        if (judge.GetJudgement() == Judgement.Pass)
        {
            for (int i = 0; i < address.frequency; i++)
            {
                deliveries[i].scheduleNode = schedule[deliveries[i].day].InsertLast(deliveries[i]);

                workDays[deliveries[i].truck][deliveries[i].day].AddStop(deliveries[i], workDayIndexes[i], routeIndexes[i], timeDeltas[i]);
            }

            unfulfilledAddresses.RemoveNode(index);
            routeNum = stagedRouteNum;
        }
    }

    #endregion AddRandom

    #region RemoveRandom

    public void RemoveRandomDelivery(Random rng, Judge judge)
    {
        //First calculate variables
        int weekDay = rng.Next(0, 5);

        if (schedule[weekDay].currentIndex == -1)
            return;

        int index = schedule[weekDay].GetRandomIncluded(rng);

        Delivery delivery = schedule[weekDay].nodes[index].value; //Get a random delivery

        //Second testify
        int timeDelta = delivery.address.emptyingTime * delivery.address.frequency * 3;

        judge.Testify(timeDelta, 0, 0);

        //Third call other functions that need to testify
        int stagedRouteNum = routeNum;

        workDays[delivery.truck][delivery.day].StageRemoveStop(delivery, stagedRouteNum, true, judge, out int workDayTimeDelta, out int routeNumDelta);

        stagedRouteNum += routeNumDelta;

        int[] otherWorkDayTimeDeltas = new int[delivery.others.Length];
        int[] otherRouteNumDeltas = new int[delivery.others.Length];

        for (int i = 0; i < delivery.others.Length; i++)
        {
            workDays[delivery.others[i].truck][delivery.others[i].day].StageRemoveStop(delivery.others[i], stagedRouteNum, true, judge, out otherWorkDayTimeDeltas[i], out routeNumDelta);
            stagedRouteNum += routeNumDelta;
        }

        //Fourth check judgement

        if (judge.GetJudgement() == Judgement.Pass)
        {
            //We need to remove all the stops in all the routes in all the workdays foreach truck as well
            schedule[delivery.day].RemoveNode(delivery.scheduleNode.index);
            workDays[delivery.truck][delivery.day].RemoveStop(delivery, workDayTimeDelta);

            for (int i = 0; i < delivery.others.Length; i++)
            {
                schedule[delivery.others[i].day].RemoveNode(delivery.others[i].scheduleNode.index);
                workDays[delivery.others[i].truck][delivery.others[i].day].RemoveStop(delivery.others[i], otherWorkDayTimeDeltas[i]);
            }

            unfulfilledAddresses.InsertLast(delivery.address); //Only once!
            routeNum = stagedRouteNum;
        }
    }

    #endregion RemoveRandom

    #region Shuffle
    public void ShuffleSchedule(Random rng, Judge judge)
    {
        Delivery removedDelivery = StageRemoveShuffleSchedule(routeNum, rng, judge, out int[] removeTimeDeltas, out int routeNumDelta);

        if (removedDelivery == null)
            return;

        StageShuffleSchedule(removedDelivery, routeNum + routeNumDelta, rng, judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out int[] addTimeDeltas, out routeNumDelta);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            RemoveDelivery(removedDelivery, removeTimeDeltas);
            AddDeliveries(deliveries, workDayIndexes, routeIndexes, addTimeDeltas);

            routeNum += routeNumDelta;
        }
    }

    Delivery StageRemoveShuffleSchedule(int routeNum, Random rng, Judge judge, out int[] timeDeltas, out int routeNumDelta)
    {
        //First calculate variables
        int weekDay = rng.Next(0, 5);

        if (schedule[weekDay].currentIndex == -1)
        {
            judge.OverrideJudge(Judgement.Fail);
            timeDeltas = null;
            routeNumDelta = 0;
            return null;
        }

        int index = schedule[weekDay].GetRandomIncluded(rng);

        Delivery delivery = schedule[weekDay].nodes[index].value; //Get a random delivery

        if (delivery.address.frequency == 3)
            judge.OverrideJudge(Judgement.Fail);

        timeDeltas = new int[delivery.address.frequency];

        //Second testify
        int timeDelta = delivery.address.emptyingTime * delivery.address.frequency * 3; //Not strictly neede for shuffle

        //TODO
        judge.Testify(timeDelta, 0, 0);


        //Third call other functions that need to testify
        int stagedRouteNum = routeNum;

        workDays[delivery.truck][delivery.day].StageRemoveStop(delivery, routeNum, true, judge, out timeDeltas[0], out routeNumDelta);

        stagedRouteNum += routeNumDelta;

        for (int i = 0; i < delivery.others.Length; i++)
        { 
            workDays[delivery.others[i].truck][delivery.others[i].day].StageRemoveStop(delivery.others[i], routeNum, true, judge, out timeDeltas[i + 1], out routeNumDelta);
            stagedRouteNum += routeNumDelta;
        }

        routeNumDelta = stagedRouteNum - routeNum;

        return delivery;
    }

    //Only call if the judgement is pass!
    void RemoveDelivery(Delivery delivery, int[] timeDeltas)
    {
        //We need to remove all the stops in all the routes in all the workdays foreach truck as well
        schedule[delivery.day].RemoveNode(delivery.scheduleNode.index);
        workDays[delivery.truck][delivery.day].RemoveStop(delivery, timeDeltas[0]);

        for (int i = 0; i < delivery.others.Length; i++)
        {
            schedule[delivery.others[i].day].RemoveNode(delivery.others[i].scheduleNode.index);
            workDays[delivery.others[i].truck][delivery.others[i].day].RemoveStop(delivery.others[i], timeDeltas[i + 1]);
        }

        //unfulfilledAddresses.InsertLast(delivery.address); //Only once!
    }

    void StageShuffleSchedule(Delivery oldDelivery, int routeNum, Random rng, Judge judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out int[] timeDeltas, out int routeNumDelta)
    {
        deliveries = new Delivery[oldDelivery.address.frequency];
        workDayIndexes = new int[oldDelivery.address.frequency];
        routeIndexes = new int[oldDelivery.address.frequency];
        timeDeltas = new int[oldDelivery.address.frequency];

        int[] weekDays = new int[oldDelivery.address.frequency];

        for (int i = 0; i < oldDelivery.address.frequency; i++)
        {
            deliveries[i] = new Delivery(oldDelivery.address);
            deliveries[i].truck = rng.Next(0, 2);
        }

        for (int i = 0; i < oldDelivery.address.frequency; i++)
        {
            for (int j = 0; j < oldDelivery.address.frequency - 1; j++)
            {
                deliveries[i].others[j] = deliveries[(i + j + 1) % oldDelivery.address.frequency]; //Maybe double check this
            }
        }

        switch (oldDelivery.address.frequency) //Frequency 3 is not allowed to switch.
        {
            case 1:
                weekDays[0] = (oldDelivery.day + rng.Next(1, 5)) % 5; //Get shifted by 0-3 mod 4 sot it is on a different day of the week.
                break;
            case 2:
                {
                    int offset = 1 - (oldDelivery.day % 3); // 0 mod 3 = 0, 1 mod 3 = 1, 3 mod 3 = 0, 4 mod 3 = 1, it just works ...

                    weekDays[0] = 0 + offset; //0 or 1 but not the same as old delivery
                    weekDays[1] = 3 + offset; //3 or 4 but not the same as old delivery
                    break;
                }
            case 4:
                {
                    int offset = rng.Next(1, 5);
                    weekDays[0] = (oldDelivery.day + offset) % 5;
                    weekDays[1] = (oldDelivery.others[0].day + offset) % 5;
                    weekDays[2] = (oldDelivery.others[1].day + offset) % 5;
                    weekDays[3] = (oldDelivery.others[2].day + offset) % 5;
                    break;
                }
        }

        int timeDelta = -oldDelivery.address.emptyingTime * 3 * oldDelivery.address.frequency; //Not strictly needed for shuffle

        //TODO
        judge.Testify(timeDelta, 0, 0);

        int stagedRouteNum = routeNum;

        for (int i = 0; i < oldDelivery.address.frequency; i++)
        {
            deliveries[i].day = weekDays[i];

            workDays[deliveries[i].truck][deliveries[i].day].StageAddStop(deliveries[i], stagedRouteNum, rng, judge, out workDayIndexes[i], out routeIndexes[i], out timeDeltas[i], out routeNumDelta);

            stagedRouteNum += routeNumDelta;
        }

        routeNumDelta = stagedRouteNum - routeNum;
    }

    void AddDeliveries(Delivery[] deliveries, int[] workDayIndexes, int[] routeIndexes, int[] timeDeltas)
    {
        for (int i = 0; i < deliveries.Length; i++)
        {
            deliveries[i].scheduleNode = schedule[deliveries[i].day].InsertLast(deliveries[i]);
            workDays[deliveries[i].truck][deliveries[i].day].AddStop(deliveries[i], workDayIndexes[i], routeIndexes[i], timeDeltas[i]);
        }
    }

    #endregion Shuffle

    #region ShuffleLowerLevel

    public void ShuffleWorkDay(Random rng, Judge judge)
    {
        //Get a random workday.
        int truck = rng.Next(0, 2);
        int day = rng.Next(0, 5);

        workDays[truck][day].ShuffleWorkDay(routeNum, rng, judge, out int routeNumDelta);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            routeNum += routeNumDelta;
        }
    }

    public void ShuffleRoute(Random rng, Judge judge)
    {
        //Get a random workday.
        int truck = rng.Next(0, 2);
        int day = rng.Next(0, 5);

        workDays[truck][day].ShuffleRoute(rng, judge);
    }

    #endregion ShuffleLowerLevel

    /// <summary>
    /// Parses a solution file and transforms it into a Schedule
    /// </summary>
    public static Schedule LoadSchedule(string path, out int score)
    {
        List<List<(int, int, int, int)>> routes = new List<List<(int, int, int, int)>>();

        using (StreamReader streamReader = new StreamReader(path))
        {
            int i = 0; //This is the route number in order given in the solution.


            string line = streamReader.ReadLine();

            if (line != null)
                routes.Add(new List<(int, int, int, int)>()); //The first route.

            while (line != null)
            {
                string[] split = line.Split(';');
                (int truck, int day, int routeIndex, int orderId) lineValue = (int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]));

                routes[i].Add(lineValue);

                line = streamReader.ReadLine();

                if (lineValue.orderId == 0 && line != null)
                {
                    i++;

                    routes.Add(new List<(int, int, int, int)>());
                }
            }
        }

        score = 0;

        Schedule schedule = new Schedule(); //The schedule we will fill out.

        Dictionary<int, List<Delivery>> orderDeliveries = new Dictionary<int, List<Delivery>>(); //Maps an order number to all deliveries that are made for that order.

        int[][] workDayIndexes = new int[2][] { new int[5], new int[5] }; //The default value of int is 0, so each time we encounter a route on a certain truck and day combination we increase that index by 1.

        //First we will insert each node into a route
        for (int i = 0; i < routes.Count; i++) //For each route.
        {
            int prevID = Input.depotMatrixID; //Since every route start at the depot.

            for (int j = 0; j < routes[i].Count; j++) //For each node in the route.
            {
                (int truck, int day, int routeIndex, int orderId) parse = routes[i][j]; //For readability.

                if (parse.orderId == 0) //We dont want to add the depot to the route.
                    continue;

                Address address;

                if (parse.orderId != 0) //Order id 0 means the depot.
                    address = new Address(Input.byOrderId[parse.orderId]);
                else
                    address = Address.Depot();

                Delivery delivery = new Delivery(address);

                if (!orderDeliveries.ContainsKey(parse.orderId))
                    orderDeliveries.Add(parse.orderId, new List<Delivery>()); //Create a new list if this is the first delivery of and order. 

                orderDeliveries[parse.orderId].Add(delivery);

                //Now fill the values out on the delivery.
                delivery.truck = parse.truck - 1; //The input starts counting from 1.
                delivery.day = parse.day - 1; //The input starts counting from 1.

                int timeDelta = Input.GetTimeFromTo(prevID, address.matrixID) + address.emptyingTime; //The time delta consists of the time from the previous address to the current one plus the emptying time of the current address.

                if (j == routes[i].Count - 2) //If the addres is just before the depot (the second last element in this list) then add the traver time from the address to the depot plus the depot emptying time.
                    timeDelta += Input.GetTimeFromTo(address.matrixID, Input.depotMatrixID) + Address.Depot().emptyingTime;

                schedule.AddDelivery(delivery, workDayIndexes[parse.truck - 1][parse.day - 1], j, timeDelta);
                
                score += timeDelta;

                prevID = address.matrixID;
            }

            //A route was fully added so we can increase workdayIndexes for later routes that are added to the same truck day combination
            workDayIndexes[routes[i][0].Item1 - 1][routes[i][0].Item2 - 1]++; //These indexes are just truck/day.
        }

        //Now it is important to update each delivery that we just added to reference other deliveries of the same order.
        foreach (List<Delivery> deliveries in orderDeliveries.Values)
        {
            if (deliveries[0].address.orderID == 0)
                continue;

            for (int i = 0; i < deliveries.Count; i++) //For each delivery.
            {
                for (int j = 0; j < deliveries.Count - 1; j++) //For each delivery except the one from the previous loop.
                {
                    deliveries[i].others[j] = deliveries[(i + j + 1) % deliveries.Count];
                }
            }
        }

        IndexedLinkedListNode<Address> currentNode = schedule.unfulfilledAddresses.nodes[0]; //The list is initially filled with every order value so we need to check for each value in this list wether it is present in the parse input.

        //Since we are bypassing some of the normal steps we need to update the unfulfilled orders list with the correct values.
        for (int i = 0; i < schedule.unfulfilledAddresses.nodes.Length; i++)
        {
            Address address = currentNode.value;

            if (orderDeliveries.ContainsKey(address.orderID))
                schedule.unfulfilledAddresses.RemoveNode(currentNode.index);

            currentNode = currentNode.next;
        }

        currentNode = schedule.unfulfilledAddresses.nodes[0];

        //As a last step we need to add the penalties for the unfulfilled orders to the score.
        for (int i = 0; i < schedule.unfulfilledAddresses.currentIndex + 1; i++)
        {
            Address address = currentNode.value;

            score += address.emptyingTime * address.frequency * 3;

            currentNode = currentNode.next;
        }

        schedule.routeNum = routes.Count;

        return schedule;
    }

    // Used in LoadSchedule
    public void AddDelivery(Delivery delivery, int workDayIndex, int routeIndex, int timeDelta)
    {
        delivery.scheduleNode = schedule[delivery.day].InsertLast(delivery);
        workDays[delivery.truck][delivery.day].AddStop(delivery, workDayIndex, routeIndex, timeDelta);
    }

    public static Schedule FromSolution(Solution solution, out int score) //Score should be the same as in solution
    {
        score = 0;

        Schedule schedule = new Schedule(); //The schedule we will fill out.

        Dictionary<int, List<Delivery>> orderDeliveries = new Dictionary<int, List<Delivery>>(); //Maps an order number to all deliveries that are made for that order.

        int[][] workDayIndexes = new int[2][] { new int[5], new int[5] }; //The default value of int is 0, so each time we encounter a route on a certain truck and day combination we increase that index by 1.

        //First we will insert each node into a route
        for (int t = 0; t < 2; t++)
        {
            for (int d = 0; d < 5 ; d++)
            {
                //int prevID = Input.depotMatrixID; //Since every route start at the depot.

                for (int r = 0; r < solution.solution[t][d].workDay.currentIndex + 1; r++)
                {
                    IndexedLinkedListNode<Delivery> previous = solution.solution[t][d].workDay.nodes[r].value.route.nodes[0];

                    if (solution.solution[t][d].workDay.nodes[r].value.route.currentIndex > 0)
                    {
                        schedule.routeNum++;

                        for (int n = 1; n < solution.solution[t][d].workDay.nodes[r].value.route.currentIndex + 1; n++)
                        {
                            IndexedLinkedListNode<Delivery> current = previous.next;

                            Address address = current.value.address;

                            Delivery delivery = new Delivery(address);

                            if (!orderDeliveries.ContainsKey(address.orderID))
                                orderDeliveries.Add(address.orderID, new List<Delivery>()); //Create a new list if this is the first delivery of and order. 

                            orderDeliveries[address.orderID].Add(delivery);

                            //Now fill the values out on the delivery.
                            delivery.truck = t; // The input starts counting from 1.
                            delivery.day = d; // The input starts counting from 1.

                            int timeDelta = Input.GetTimeFromTo(previous.value.address.matrixID, address.matrixID) + address.emptyingTime; //The time delta consists of the time from the previous address to the current one plus the emptying time of the current address.

                            if (n == solution.solution[t][d].workDay.nodes[r].value.route.currentIndex) //If the addres is just before the depot (the second last element in this list) then add the traver time from the address to the depot plus the depot emptying time.
                                timeDelta += Input.GetTimeFromTo(address.matrixID, Input.depotMatrixID) + Address.Depot().emptyingTime;

                            schedule.AddDelivery(delivery, workDayIndexes[t][d], n - 1, timeDelta);

                            score += timeDelta;

                            previous = current;
                        }

                        //A route was fully added so we can increase workdayIndexes for later routes that are added to the same truck day combination
                        workDayIndexes[t][d]++; //These indexes are just truck/day.
                    }
                    
                }
            }
        }

        //Now it is important to update each delivery that we just added to reference other deliveries of the same order.
        foreach (List<Delivery> deliveries in orderDeliveries.Values)
        {
            if (deliveries[0].address.orderID == 0)
                continue;

            for (int i = 0; i < deliveries.Count; i++) //For each delivery.
            {
                for (int j = 0; j < deliveries.Count - 1; j++) //For each delivery except the one from the previous loop.
                {
                    deliveries[i].others[j] = deliveries[(i + j + 1) % deliveries.Count];
                }
            }
        }

        IndexedLinkedListNode<Address> currentNode = schedule.unfulfilledAddresses.nodes[0]; //The list is initially filled with every order value so we need to check for each value in this list wether it is present in the parse input.

        //Since we are bypassing some of the normal steps we need to update the unfulfilled orders list with the correct values.
        for (int i = 0; i < schedule.unfulfilledAddresses.nodes.Length; i++)
        {
            Address address = currentNode.value;

            if (orderDeliveries.ContainsKey(address.orderID))
                schedule.unfulfilledAddresses.RemoveNode(currentNode.index);

            currentNode = currentNode.next;
        }

        currentNode = schedule.unfulfilledAddresses.nodes[0];

        //As a last step we need to add the penalties for the unfulfilled orders to the score.
        for (int i = 0; i < schedule.unfulfilledAddresses.currentIndex + 1; i++)
        {
            Address address = currentNode.value;

            score += address.emptyingTime * address.frequency * 3;

            currentNode = currentNode.next;
        }

        return schedule;
    }
}
