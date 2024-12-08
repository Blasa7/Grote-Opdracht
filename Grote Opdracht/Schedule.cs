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

    }

    public Schedule(Order[] orders)
    {
        for (int i = 0; i < schedule.Length; i++)
            schedule[i] = new IndexedLinkedList<Delivery>(Input.orderCount);

        for (int i = 0; i < workDays.Length; i++)
        {
            for (int j = 0; j < workDays[i].Length; j++)
            {
                workDays[i][j] = new WorkDay(j, Input.orderCount);
            }
        }

        unfulfilledAddresses = new IndexedLinkedList<Address>(orders.Length);

        for (int i = 0; i < orders.Length; i++)
        {
            unfulfilledAddresses.InsertLast(new Address(orders[i]));
        }
    }

    #region AddRandom

    public void AddRandomTwoTimeDelivery(Random rng, Judge judge)
    {
        int index = unfulfilledAddresses.getRandomIncluded(rng);
        IndexedLinkedListNode<Address> node = unfulfilledAddresses.nodes[index];
        Address address = node.value;

        if (address.frequency == 2)
        {
            AddRandomTwoTimeDelivery(address, rng, judge);
        }
        else
        {
            judge.OverrideJudge(Judgement.Fail);
        }

        if (judge.GetJudgement() == Judgement.Pass)
        {
            unfulfilledAddresses.RemoveNode(index);
        }
    }

    public void AddRandomThreeTimeDelivery(Random rng, Judge judge)
    {
        int index = unfulfilledAddresses.getRandomIncluded(rng);
        IndexedLinkedListNode<Address> node = unfulfilledAddresses.nodes[index];
        Address address = node.value;

        if (address.frequency == 3)
        {
            AddRandomThreeTimeDelivery(address, rng, judge);
        }
        else
        {
            judge.OverrideJudge(Judgement.Fail);
        }

        if (judge.GetJudgement() == Judgement.Pass)
        {
            unfulfilledAddresses.RemoveNode(index);
        }
    }

    public void AddRandomFourTimeDelivery(Random rng, Judge judge)
    {
        int index = unfulfilledAddresses.getRandomIncluded(rng);
        IndexedLinkedListNode<Address> node = unfulfilledAddresses.nodes[index];
        Address address = node.value;

        if (address.frequency == 4)
        {
            AddRandomFourTimeDelivery(address, rng, judge);
        }
        else
        {
            judge.OverrideJudge(Judgement.Fail);
        }

        if (judge.GetJudgement() == Judgement.Pass)
        {
            unfulfilledAddresses.RemoveNode(index);
        }
    }

    public void AddRandomDelivery(Random rng, Judge judge)
    {
        int index = unfulfilledAddresses.getRandomIncluded(rng);
        IndexedLinkedListNode<Address> node = unfulfilledAddresses.nodes[index];
        Address address = node.value;

        switch (address.frequency)
        {
            case 1:
                AddRandomOneTimeDelivery(address, rng, judge);
                break;
            case 2:
                AddRandomTwoTimeDelivery(address, rng, judge);
                break;
            case 3:
                AddRandomThreeTimeDelivery(address, rng, judge);
                break;
            case 4:
                AddRandomFourTimeDelivery(address, rng, judge);
                break;
        }

        if (judge.GetJudgement() == Judgement.Pass)
        {
            unfulfilledAddresses.RemoveNode(index);
        }
        else
        {
            int i = 1;
        }
    }

    /// <summary>
    /// Adds a one time delivery to the a random truck at random.
    /// </summary>
    void AddRandomOneTimeDelivery(Address address, Random rng, Judge judge)
    {
        //First calculate variables
        Delivery delivery = new Delivery(address);

        int weekDay = rng.Next(0, 5);
        int truck = rng.Next(0, 2);

        //Second testify
        int testimony = -address.emptyingTime * 3; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        //workDays[truck][weekDay].AddRandomStop(delivery, rng, judge);
        workDays[truck][weekDay].StageRandomStop(delivery, rng, judge, out int workDayIndex, out int routeIndex, out int timeDelta);

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            delivery.truck = truck;
            delivery.day = weekDay;
            delivery.scheduleNode = schedule[weekDay].InsertLast(delivery);

            workDays[truck][weekDay].AddStop(delivery, workDayIndex, routeIndex, timeDelta);
        }
    }

    void AddRandomTwoTimeDelivery(Address address, Random rng, Judge judge)
    {
        //First calculate variables
        Delivery delivery1 = new Delivery(address);
        Delivery delivery2 = new Delivery(address);

        delivery1.others[0] = delivery2;
        delivery2.others[0] = delivery1;

        int timeSlot = rng.Next(0, 2); //Monday-Thursday or Tuesday-Friday
        int[] trucks = new int[2] { rng.Next(0, 2), rng.Next(0, 2) };

        trucks[0] = rng.Next(0, 2);
        trucks[1] = rng.Next(0, 2);

        int day1 = 0 + timeSlot; //0 or 1
        int day2 = 3 + timeSlot; //3 or 4

        //Second testify
        int testimony = -address.emptyingTime * 3 * 2; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[trucks[0]][day1].StageRandomStop(delivery1, rng, judge, out int workDayIndex1, out int routeIndex1, out int timeDelta1);
        workDays[trucks[1]][day2].StageRandomStop(delivery2, rng, judge, out int workDayIndex2, out int routeIndex2, out int timeDelta2);

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            workDays[trucks[0]][day1].AddStop(delivery1, workDayIndex1, routeIndex1, timeDelta1);
            workDays[trucks[1]][day2].AddStop(delivery2, workDayIndex2, routeIndex2, timeDelta2);

            delivery1.truck = trucks[0];
            delivery2.truck = trucks[1];

            delivery1.day = day1;
            delivery2.day = day2;

            delivery1.scheduleNode = schedule[day1].InsertLast(delivery1);
            delivery2.scheduleNode = schedule[day2].InsertLast(delivery2);

            //NOTE:
            //2+ Frequency Deliveries are put on the same truck here, but in practice could be split
        }
    }

    void AddRandomThreeTimeDelivery(Address address, Random rng, Judge judge)
    {
        //First calculate variables
        Delivery delivery1 = new Delivery(address);
        Delivery delivery2 = new Delivery(address);
        Delivery delivery3 = new Delivery(address);

        delivery1.others[0] = delivery2;
        delivery1.others[1] = delivery3;

        delivery2.others[0] = delivery1;
        delivery2.others[1] = delivery3;

        delivery3.others[0] = delivery1;
        delivery3.others[1] = delivery2;

        int[] trucks = new int[3] { rng.Next(0, 2), rng.Next(0, 2), rng.Next(0, 2) };

        //Second testify
        int testimony = -address.emptyingTime * 3 * 3;

        judge.Testify(testimony);

        //Third call other functions that need to testify

        workDays[trucks[0]][0].StageRandomStop(delivery1, rng, judge, out int workDayIndex1, out int routeIndex1, out int timeDelta1);
        workDays[trucks[1]][2].StageRandomStop(delivery2, rng, judge, out int workDayIndex2, out int routeIndex2, out int timeDelta2);
        workDays[trucks[2]][4].StageRandomStop(delivery3, rng, judge, out int workDayIndex3, out int routeIndex3, out int timeDelta3);

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            workDays[trucks[0]][0].AddStop(delivery1, workDayIndex1, routeIndex1, timeDelta1);
            workDays[trucks[1]][2].AddStop(delivery2, workDayIndex2, routeIndex2, timeDelta2);
            workDays[trucks[2]][4].AddStop(delivery3, workDayIndex3, routeIndex3, timeDelta3);

            delivery1.truck = trucks[0];
            delivery2.truck = trucks[1];
            delivery3.truck = trucks[2];

            delivery1.day = 0;
            delivery2.day = 2;
            delivery3.day = 4;

            delivery1.scheduleNode = schedule[0].InsertLast(delivery1);
            delivery2.scheduleNode = schedule[2].InsertLast(delivery2);
            delivery3.scheduleNode = schedule[4].InsertLast(delivery3);
        }
    }

    void AddRandomFourTimeDelivery(Address address, Random rng, Judge judge)
    {
        //First calculate variables
        Delivery delivery1 = new Delivery(address);
        Delivery delivery2 = new Delivery(address);
        Delivery delivery3 = new Delivery(address);
        Delivery delivery4 = new Delivery(address);

        delivery1.others[0] = delivery2;
        delivery1.others[1] = delivery3;
        delivery1.others[2] = delivery4;

        delivery2.others[0] = delivery1;
        delivery2.others[1] = delivery3;
        delivery2.others[2] = delivery4;

        delivery3.others[0] = delivery1;
        delivery3.others[1] = delivery2;
        delivery3.others[2] = delivery4;

        delivery4.others[0] = delivery1;
        delivery4.others[1] = delivery2;
        delivery4.others[2] = delivery3;

        int[] trucks = new int[4] { rng.Next(0, 2), rng.Next(0, 2), rng.Next(0, 2), rng.Next(0, 2) };

        int skipDay = rng.Next(0, 5);
        int[] days = new int[4];

        for (int i = 0, day = 0; i < 4; i++, day++)
        {
            if (day == skipDay)
                day++;

            days[i] = day;
        }

        //Second testify
        int testimony = -address.emptyingTime * 3 * 4;

        judge.Testify(testimony);

        //Third call other functions that need to testify

        workDays[trucks[0]][days[0]].StageRandomStop(delivery1, rng, judge, out int workDayIndex1, out int routeIndex1, out int timeDelta1);
        workDays[trucks[1]][days[1]].StageRandomStop(delivery2, rng, judge, out int workDayIndex2, out int routeIndex2, out int timeDelta2);
        workDays[trucks[2]][days[2]].StageRandomStop(delivery3, rng, judge, out int workDayIndex3, out int routeIndex3, out int timeDelta3);
        workDays[trucks[3]][days[3]].StageRandomStop(delivery4, rng, judge, out int workDayIndex4, out int routeIndex4, out int timeDelta4);


        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            workDays[trucks[0]][days[0]].AddStop(delivery1, workDayIndex1, routeIndex1, timeDelta1);
            workDays[trucks[1]][days[1]].AddStop(delivery2, workDayIndex2, routeIndex2, timeDelta2);
            workDays[trucks[2]][days[2]].AddStop(delivery3, workDayIndex3, routeIndex3, timeDelta3);
            workDays[trucks[3]][days[3]].AddStop(delivery4, workDayIndex4, routeIndex4, timeDelta4);

            delivery1.truck = trucks[0];
            delivery2.truck = trucks[1];
            delivery3.truck = trucks[2];
            delivery4.truck = trucks[3];

            delivery1.day = days[0];
            delivery2.day = days[1];
            delivery3.day = days[2];
            delivery4.day = days[3];

            delivery1.scheduleNode = schedule[days[0]].InsertLast(delivery1);
            delivery2.scheduleNode = schedule[days[1]].InsertLast(delivery2);
            delivery3.scheduleNode = schedule[days[2]].InsertLast(delivery3);
            delivery4.scheduleNode = schedule[days[3]].InsertLast(delivery4);
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
}
