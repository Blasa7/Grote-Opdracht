using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;

class Schedule
{
    /// <summary>
    /// 5 indexed linked lists track which deliveries are being made on each day.
    /// </summary>
    public IndexedLinkedList<Delivery>[] schedule = new IndexedLinkedList<Delivery>[5];

    //2 trucks each 5 workdays
    public WorkDay[][] workDays = new WorkDay[2][] { new WorkDay[5], new WorkDay[5] };

    public IndexedLinkedList<Address> unfulfilledAddresses;

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

    public void AddRandomDelivery(Random rng, Judge judge) //Shorter more readable? equivalent of the previous add method, maybe a fraction fast
    {

        if (unfulfilledAddresses.currentIndex == 0)
        {
            judge.OverrideJudge(Judgement.Fail);
            return;
        }


        //Get a random unfulfilled address
        int index = unfulfilledAddresses.getRandomIncluded(rng);
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
        int testimony = -address.emptyingTime * 3 * address.frequency;

        judge.Testify(testimony);

        //Stage functions to randomly add are called
        for (int i = 0; i < address.frequency; i++)
        {
            workDays[deliveries[i].truck][deliveries[i].day].StageRandomStop(deliveries[i], rng, judge, out workDayIndexes[i], out routeIndexes[i], out timeDeltas[i]);
        }

        if (judge.GetJudgement() == Judgement.Pass)
        {
            for (int i = 0; i < address.frequency; i++)
            {
                deliveries[i].scheduleNode = schedule[deliveries[i].day].InsertLast(deliveries[i]);

                workDays[deliveries[i].truck][deliveries[i].day].AddStop(deliveries[i], workDayIndexes[i], routeIndexes[i], timeDeltas[i]);
            }

            unfulfilledAddresses.RemoveNode(index);
        }
    }

    #endregion AddRandom

    #region RemoveRandom

    //Maybe add remove
    public void RemoveRandomDelivery(Random rng, Judge judge)
    {
        //First calculate variables
        int weekDay = rng.Next(0, 5);

        if (schedule[weekDay].currentIndex == -1)
            return;

        int index = schedule[weekDay].getRandomIncluded(rng);

        Delivery delivery = schedule[weekDay].nodes[index].value; //Get a random delivery

        //Second testify
        int testimony = delivery.address.emptyingTime * delivery.address.frequency * 3;

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[delivery.truck][delivery.day].StageRemoveStop(delivery, judge, out int timeDelta);

        int[] otherTimeDeltas = new int[delivery.others.Length];

        for (int i = 0; i < delivery.others.Length; i++)
            workDays[delivery.others[i].truck][delivery.others[i].day].StageRemoveStop(delivery.others[i], judge, out otherTimeDeltas[i]);

        //Fourth check judgement

        if (judge.GetJudgement() == Judgement.Pass)
        {
            //We need to remove all the stops in all the routes in all the workdays foreach truck as well
            schedule[delivery.day].RemoveNode(delivery.scheduleNode.index);
            workDays[delivery.truck][delivery.day].RemoveStop(delivery, timeDelta);

            for (int i = 0; i < delivery.others.Length; i++)
            {
                schedule[delivery.others[i].day].RemoveNode(delivery.others[i].scheduleNode.index);
                workDays[delivery.others[i].truck][delivery.others[i].day].RemoveStop(delivery.others[i], otherTimeDeltas[i]);
            }

            unfulfilledAddresses.InsertLast(delivery.address); //Only once!
        }
    }

    #endregion RemoveRandom

    #region Shuffle
    public void ShuffleSchedule(Random rng, Judge judge)
    {
        Delivery removedDelivery = StageRemoveShuffleSchedule(rng, judge, out int[] removeTimeDeltas);

        if (removedDelivery == null)
            return;

        StageShuffleSchedule(removedDelivery, rng, judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out int[] addTimeDeltas);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            RemoveDelivery(removedDelivery, removeTimeDeltas);
            AddDeliveries(deliveries, workDayIndexes, routeIndexes, addTimeDeltas);
        }
    }

    Delivery StageRemoveShuffleSchedule(Random rng, Judge judge, out int[] timeDeltas)
    {
        //First calculate variables
        int weekDay = rng.Next(0, 5);

        if (schedule[weekDay].currentIndex == -1)
        {
            timeDeltas = null;
            return null;
        }

        int index = schedule[weekDay].getRandomIncluded(rng);

        Delivery delivery = schedule[weekDay].nodes[index].value; //Get a random delivery

        if (delivery.address.frequency == 3)
            judge.OverrideJudge(Judgement.Fail);

        timeDeltas = new int[delivery.address.frequency];

        //Second testify
        int testimony = delivery.address.emptyingTime * delivery.address.frequency * 3; //Not strictly neede for shuffle

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[delivery.truck][delivery.day].StageRemoveStop(delivery, judge, out timeDeltas[0]);

        for (int i = 0; i < delivery.others.Length; i++)
            workDays[delivery.others[i].truck][delivery.others[i].day].StageRemoveStop(delivery.others[i], judge, out timeDeltas[i + 1]);

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

    void StageShuffleSchedule(Delivery oldDelivery, Random rng, Judge judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out int[] timeDeltas)
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
                weekDays[0] = (oldDelivery.day + rng.Next(1, 4)) % 5; //Get shifted by 0-3 mod 4 sot it is on a different day of the week.
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
                    int offset = rng.Next(0, 4);
                    weekDays[0] = (oldDelivery.day + offset) % 5;
                    weekDays[1] = (oldDelivery.others[0].day + offset) % 5;
                    weekDays[2] = (oldDelivery.others[1].day + offset) % 5;
                    weekDays[3] = (oldDelivery.others[2].day + offset) % 5;
                    break;
                }
        }

        int testimony = -oldDelivery.address.emptyingTime * 3 * oldDelivery.address.frequency; //Not strictly needed for shuffle

        judge.Testify(testimony);

        for (int i = 0; i < oldDelivery.address.frequency; i++)
        {
            deliveries[i].day = weekDays[i];

            workDays[deliveries[i].truck][deliveries[i].day].StageRandomStop(deliveries[i], rng, judge, out workDayIndexes[i], out routeIndexes[i], out timeDeltas[i]);
        }
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

        workDays[truck][day].ShuffleWorkDay(rng, judge);
    }

    public void ShuffleRoute(Random rng, Judge judge)
    {
        //Get a random workday.
        int truck = rng.Next(0, 2);
        int day = rng.Next(0, 5);

        workDays[truck][day].ShuffleRoute(rng, judge);
    }

    #endregion ShuffleLowerLevel

    public void SwapDeliveries(Random rng, Judge judge)
    {
        //First calculate variables
        int weekDay = rng.Next(0, 5);

        if (schedule[weekDay].currentIndex == -1)
        {
            judge.OverrideJudge(Judgement.Fail);
            return;
        }

        int index = schedule[weekDay].getRandomIncluded(rng);

        Delivery delivery = schedule[weekDay].nodes[index].value;

        int otherTruck = rng.Next(0, 2);

        int randomWorkDayIndex = workDays[otherTruck][weekDay].workDay.getRandomIncluded(rng);

        while (otherTruck == delivery.truck && randomWorkDayIndex == delivery.workDayNode.index)
        {
            randomWorkDayIndex = workDays[otherTruck][weekDay].workDay.getRandomIncluded(rng);
        }

        if (workDays[otherTruck][weekDay].workDay.nodes[randomWorkDayIndex].value.route.currentIndex < workDays[otherTruck][weekDay].workDay.nodes[randomWorkDayIndex].value.route.startIndex)
        {
            judge.OverrideJudge(Judgement.Fail);
            return;
        }

        int randomRouteIndex = workDays[otherTruck][weekDay].workDay.nodes[randomWorkDayIndex].value.route.getRandomIncluded(rng);

        Delivery otherDelivery = workDays[otherTruck][weekDay].workDay.nodes[randomWorkDayIndex].value.route.nodes[randomRouteIndex].value;

        int index1 = delivery.routeNode.index;
        int index2 = otherDelivery.routeNode.index;

        Address address1 = delivery.address;
        Address address2 = otherDelivery.address;

        int thisID1 = address1.matrixID;
        int thisID2 = address2.matrixID;
        int prevID1 = delivery.routeNode.prev.value.address.matrixID;//list1.nodes[index1].prev.value.address.matrixID;
        int nextID1 = delivery.routeNode.next.value.address.matrixID;
        int prevID2 = otherDelivery.routeNode.prev.value.address.matrixID;
        int nextID2 = otherDelivery.routeNode.next.value.address.matrixID;

        //initial distances between the nodes and their neighbors + emptying time
        int oldValue1 = Input.GetTimeFromTo(prevID1, thisID1) + Input.GetTimeFromTo(thisID1, nextID1) + address1.emptyingTime;
        int oldValue2 = Input.GetTimeFromTo(prevID2, thisID2) + Input.GetTimeFromTo(thisID2, nextID2) + address2.emptyingTime;
        //new distances between the nodes and their new neighbors + swapped emptying time
        int newValue1 = Input.GetTimeFromTo(prevID1, thisID2) + Input.GetTimeFromTo(thisID2, nextID1) + address2.emptyingTime;
        int newValue2 = Input.GetTimeFromTo(prevID2, thisID1) + Input.GetTimeFromTo(thisID1, nextID2) + address1.emptyingTime;

        int list1Delta = newValue1 - oldValue1; //difference in score for truck 1
        int list2Delta = newValue2 - oldValue2; //difference in score for truck 2

        int garbage1Delta = address2.garbageAmount - address1.garbageAmount; //difference in garbage for truck 1
        int garbage2Delta = -garbage1Delta;                                  //difference in garbage for truck 2

        // The same workday and the same truck
        if (delivery.workDayNode.index == otherDelivery.workDayNode.index && delivery.truck == otherDelivery.truck)
        {
            // Check if the new time on the same workday is within time
            if (workDays[delivery.truck][weekDay].totalDuration + list1Delta + list2Delta > workDays[delivery.truck][weekDay].maximumDuration)
                judge.OverrideJudge(Judgement.Fail);

            // Check garbage
            if (delivery.workDayNode.value.collectedGarbage + garbage1Delta > delivery.workDayNode.value.maximumGarbage)
                judge.OverrideJudge(Judgement.Fail);

            if (otherDelivery.workDayNode.value.collectedGarbage + garbage2Delta > otherDelivery.workDayNode.value.maximumGarbage)
                judge.OverrideJudge(Judgement.Fail);
        }
        else
        {
            if (workDays[delivery.truck][weekDay].totalDuration + list1Delta > workDays[delivery.truck][weekDay].maximumDuration ||
                delivery.workDayNode.value.collectedGarbage + garbage1Delta > delivery.workDayNode.value.maximumGarbage)
                judge.OverrideJudge(Judgement.Fail);


            if (workDays[otherTruck][weekDay].totalDuration + list2Delta > workDays[otherTruck][weekDay].maximumDuration ||
                otherDelivery.workDayNode.value.collectedGarbage + garbage2Delta > otherDelivery.workDayNode.value.maximumGarbage)
                judge.OverrideJudge(Judgement.Fail);
        }

        judge.Testify(list1Delta + list2Delta);

        if (judge.GetJudgement() == Judgement.Pass)
        {            
            //Swapping values of the Deliveries, this isn't done in the generic SwapNodes function

            //Updating the collected garbage
            delivery.workDayNode.value.collectedGarbage += garbage1Delta;
            otherDelivery.workDayNode.value.collectedGarbage += garbage2Delta;

            //Updating the duration
            workDays[delivery.truck][weekDay].totalDuration += list1Delta;
            workDays[otherTruck][weekDay].totalDuration += list2Delta;

            delivery.workDayNode.value.duration += list1Delta;
            otherDelivery.workDayNode.value.duration += list2Delta;

            SwapNodes<Delivery>(delivery.routeNode, otherDelivery.routeNode, delivery.workDayNode.value.route, otherDelivery.workDayNode.value.route);
            (delivery.workDayNode, otherDelivery.workDayNode) = (otherDelivery.workDayNode, delivery.workDayNode);
            (delivery.truck, otherDelivery.truck) = (otherDelivery.truck, delivery.truck);

        }
    }

    public void SwapNodes<T>
    (IndexedLinkedListNode<T> node1, IndexedLinkedListNode<T> node2, IndexedLinkedList<T> list1, IndexedLinkedList<T> list2)
        where T : IClonable<T>
    {
        //Nodes are from the same list, call the existing swap function in IndexedLinkedList.cs
        if (list1 == list2)
            list1.SwapNodes(node1.index, node2.index);

        //create temp values for swapping
        IndexedLinkedListNode<T> prevNode1 = node1.prev;
        IndexedLinkedListNode<T> nextNode1 = node1.next;

        //swap the node's indices
        //(node1.index, node2.index) = (node2.index, node1.index);

        //First points to Second's neighbors.
        node1.prev = node2.prev;
        node1.next = node2.next;

        //Second's neighbors point to First.
        node1.prev.next = node1;
        node1.next.prev = node1;

        //Second points to First's neighbors.
        node2.prev = prevNode1;
        node2.next = nextNode1;

        //First's neighbors point to Second.
        node2.prev.next = node2;
        node2.next.prev = node2;

        //Physically swap the nodes from one array to the other
        list1.nodes[node1.index] = node2;
        list2.nodes[node2.index] = node1;
        //(node1.index, node2.index) = (node2.index, node1.index);

        int tempIndex = node1.index;

        node1.index = node2.index;
        node2.index = tempIndex;
    }

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
                
                if (parse.orderId != 0) //If the addres is the depot we do NOT want to add it at the end of the route. We still want to calculate the time difference though.
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

        return schedule;
    }

    public void AddDelivery(Delivery delivery, int workDayIndex, int routeIndex, int timeDelta)
    {
        delivery.scheduleNode = schedule[delivery.day].InsertLast(delivery);
        workDays[delivery.truck][delivery.day].AddStop(delivery, workDayIndex, routeIndex, timeDelta);
    }
}
