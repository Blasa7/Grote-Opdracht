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

    public void StageDelivery(Address address, Random rng, Judge judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out float[] timeDeltas)
    {
        //int index = unfulfilledAddresses.getRandomIncluded(rng);
        //IndexedLinkedListNode<Address> node = unfulfilledAddresses.nodes[index];
        //Address address = node.value;

        switch (address.frequency)
        {
            case 1:
                StageRandomOneTimeDelivery(address, rng, judge, out deliveries, out workDayIndexes, out routeIndexes, out timeDeltas);
                break;
            case 2:
                StageRandomTwoTimeDelivery(address, rng, judge, out deliveries, out workDayIndexes, out routeIndexes, out timeDeltas);
                break;
            case 3:
                StageRandomThreeTimeDelivery(address, rng, judge, out deliveries, out workDayIndexes, out routeIndexes, out timeDeltas);
                break;
            default:
                StageRandomFourTimeDelivery(address, rng, judge, out deliveries, out workDayIndexes, out routeIndexes, out timeDeltas);
                break;
        }
    }

    public void StageRandomOneTimeDelivery(Address address, Random rng, Judge judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out float[] timeDeltas)
    {
        //First calculate variables
        deliveries = new Delivery[1];
        workDayIndexes = new int[1];
        routeIndexes = new int[1];
        timeDeltas = new float[1];

        deliveries[0] = new Delivery(address);

        int weekDay = rng.Next(0, 5);
        int truck = rng.Next(0, 2);

        //Second testify
        float testimony = -address.emptyingTime * 3; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[truck][weekDay].StageRandomStop(deliveries[0], rng, judge, out workDayIndexes[0], out routeIndexes[0], out timeDeltas[0]);

        deliveries[0].truck = truck;
        deliveries[0].day = weekDay;
    }

    public void StageRandomTwoTimeDelivery(Address address, Random rng, Judge judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out float[] timeDeltas)
    {
        //First calculate variables
        deliveries = new Delivery[2];
        workDayIndexes = new int[2];
        routeIndexes = new int[2];
        timeDeltas = new float[2];

        deliveries[0] = new Delivery(address);
        deliveries[1] = new Delivery(address);

        deliveries[0].others[0] = deliveries[1];
        deliveries[1].others[0] = deliveries[0];

        int timeSlot = rng.Next(0, 2); //Monday-Thursday or Tuesday-Friday
        int truck = rng.Next(0, 2);

        int day1 = 0 + timeSlot; //0 or 1
        int day2 = 3 + timeSlot; //3 or 4

        //Second testify
        float testimony = -address.emptyingTime * 3 * 2; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[truck][day1].StageRandomStop(deliveries[0], rng, judge, out workDayIndexes[0], out routeIndexes[0], out timeDeltas[0]);
        workDays[truck][day2].StageRandomStop(deliveries[1], rng, judge, out workDayIndexes[1], out routeIndexes[1], out timeDeltas[1]);

        deliveries[0].truck = truck;
        deliveries[0].day = day1;

        deliveries[1].truck = truck;
        deliveries[1].day = day2;
    }

    public void StageRandomThreeTimeDelivery(Address address, Random rng, Judge judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out float[] timeDeltas)
    {
        //First calculate variables
        deliveries = new Delivery[3];
        workDayIndexes = new int[3];
        routeIndexes = new int[3];
        timeDeltas = new float[3];

        //beetje van dit enz.
        deliveries[0] = new Delivery(address);
        deliveries[1] = new Delivery(address);
        deliveries[2] = new Delivery(address);

        //dit en dat
        deliveries[0].others[0] = deliveries[1];
        deliveries[0].others[1] = deliveries[2];

        deliveries[1].others[0] = deliveries[0];
        deliveries[1].others[1] = deliveries[2];

        deliveries[2].others[0] = deliveries[0];
        deliveries[2].others[1] = deliveries[1];

        int truck = rng.Next(0, 2);

        //Second testify
        float testimony = -address.emptyingTime * 3 * 3;

        judge.Testify(testimony);

        //Third call other functions that need to testify;
        workDays[truck][0].StageRandomStop(deliveries[0], rng, judge, out workDayIndexes[0], out routeIndexes[0], out timeDeltas[0]);
        workDays[truck][2].StageRandomStop(deliveries[1], rng, judge, out workDayIndexes[1], out routeIndexes[1], out timeDeltas[1]);
        workDays[truck][4].StageRandomStop(deliveries[2], rng, judge, out workDayIndexes[2], out routeIndexes[2], out timeDeltas[2]);

        deliveries[0].truck = truck;
        deliveries[0].day = 0;

        deliveries[1].truck = truck;
        deliveries[1].day = 2;

        deliveries[2].truck = truck;
        deliveries[2].day = 4;
    }

    public void StageRandomFourTimeDelivery(Address address, Random rng, Judge judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out float[] timeDeltas)
    {
        //First calculate variables
        deliveries = new Delivery[3];
        workDayIndexes = new int[3];
        routeIndexes = new int[3];
        timeDeltas = new float[3];

        //en een beetje van dat
        deliveries[0] = new Delivery(address);
        deliveries[1] = new Delivery(address);
        deliveries[2] = new Delivery(address);
        deliveries[3] = new Delivery(address);

        //zo en zo
        deliveries[0].others[0] = deliveries[1];
        deliveries[0].others[1] = deliveries[2];
        deliveries[0].others[2] = deliveries[3];

        deliveries[1].others[0] = deliveries[0];
        deliveries[1].others[1] = deliveries[2];
        deliveries[1].others[2] = deliveries[3];

        deliveries[2].others[0] = deliveries[0];
        deliveries[2].others[1] = deliveries[1];
        deliveries[2].others[2] = deliveries[3];

        deliveries[3].others[0] = deliveries[0];
        deliveries[3].others[1] = deliveries[1];
        deliveries[3].others[2] = deliveries[2];

        int truck = rng.Next(0, 2);

        int skipDay = rng.Next(0, 5);
        int[] days = new int[4];

        for (int i = 0, day = 0; i < 4; i++, day++)
        {
            if (day == skipDay)
                day++;

            days[i] = day;
        }

        //Second testify
        float testimony = -address.emptyingTime * 3 * 4;

        judge.Testify(testimony);

        //Third call other functions that need to testify

        workDays[truck][days[0]].StageRandomStop(deliveries[0], rng, judge, out workDayIndexes[0], out routeIndexes[0], out timeDeltas[0]);
        workDays[truck][days[1]].StageRandomStop(deliveries[1], rng, judge, out workDayIndexes[1], out routeIndexes[1], out timeDeltas[1]);
        workDays[truck][days[2]].StageRandomStop(deliveries[2], rng, judge, out workDayIndexes[2], out routeIndexes[2], out timeDeltas[2]);
        workDays[truck][days[3]].StageRandomStop(deliveries[3], rng, judge, out workDayIndexes[3], out routeIndexes[3], out timeDeltas[3]);

        deliveries[0].truck = truck;
        deliveries[0].day = days[0];

        deliveries[1].truck = truck;
        deliveries[1].day = days[1];

        deliveries[2].truck = truck;
        deliveries[2].day = days[2];

        deliveries[3].truck = truck;
        deliveries[3].day = days[3];
    }

    //Check judge before calling
    public void AddDelivery(Delivery delivery, int workDayIndex, int routeIndex, float timeDelta)
    {
        delivery.truck = delivery.truck;
        delivery.day = delivery.day;
        delivery.scheduleNode = schedule[delivery.day].InsertLast(delivery);

        workDays[delivery.truck][delivery.day].AddStop(delivery, workDayIndex, routeIndex, timeDelta);
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
        float testimony = -address.emptyingTime * 3; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        //workDays[truck][weekDay].AddRandomStop(delivery, rng, judge);
        workDays[truck][weekDay].StageRandomStop(delivery, rng, judge, out int workDayIndex, out int routeIndex, out float timeDelta);

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
        int truck = rng.Next(0, 2);

        int day1 = 0 + timeSlot; //0 or 1
        int day2 = 3 + timeSlot; //3 or 4

        //Second testify
        float testimony = -address.emptyingTime * 3 * 2; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[truck][day1].StageRandomStop(delivery1, rng, judge, out int workDayIndex1, out int routeIndex1, out float timeDelta1);
        workDays[truck][day2].StageRandomStop(delivery2, rng, judge, out int workDayIndex2, out int routeIndex2, out float timeDelta2);

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            workDays[truck][day1].AddStop(delivery1, workDayIndex1, routeIndex1, timeDelta1);
            workDays[truck][day2].AddStop(delivery2, workDayIndex2, routeIndex2, timeDelta2);

            delivery1.truck = truck;
            delivery2.truck = truck;

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

        int truck = rng.Next(0, 2);

        //Second testify
        float testimony = -address.emptyingTime * 3 * 3;

        judge.Testify(testimony);

        //Third call other functions that need to testify

        workDays[truck][0].StageRandomStop(delivery1, rng, judge, out int workDayIndex1, out int routeIndex1, out float timeDelta1);
        workDays[truck][2].StageRandomStop(delivery2, rng, judge, out int workDayIndex2, out int routeIndex2, out float timeDelta2);
        workDays[truck][4].StageRandomStop(delivery3, rng, judge, out int workDayIndex3, out int routeIndex3, out float timeDelta3);

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            workDays[truck][0].AddStop(delivery1, workDayIndex1, routeIndex1, timeDelta1);
            workDays[truck][2].AddStop(delivery2, workDayIndex2, routeIndex2, timeDelta2);
            workDays[truck][4].AddStop(delivery3, workDayIndex3, routeIndex3, timeDelta3);

            delivery1.truck = truck;
            delivery2.truck = truck;
            delivery3.truck = truck;

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

        int truck = rng.Next(0, 2);

        int skipDay = rng.Next(0, 5);
        int[] days = new int[4];

        for (int i = 0, day = 0; i < 4; i++, day++)
        {
            if (day == skipDay)
                day++;

            days[i] = day;
        }

        //Second testify
        float testimony = -address.emptyingTime * 3 * 4;

        judge.Testify(testimony);

        //Third call other functions that need to testify

        workDays[truck][days[0]].StageRandomStop(delivery1, rng, judge, out int workDayIndex1, out int routeIndex1, out float timeDelta1);
        workDays[truck][days[1]].StageRandomStop(delivery2, rng, judge, out int workDayIndex2, out int routeIndex2, out float timeDelta2);
        workDays[truck][days[2]].StageRandomStop(delivery3, rng, judge, out int workDayIndex3, out int routeIndex3, out float timeDelta3);
        workDays[truck][days[3]].StageRandomStop(delivery4, rng, judge, out int workDayIndex4, out int routeIndex4, out float timeDelta4);


        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            workDays[truck][days[0]].AddStop(delivery1, workDayIndex1, routeIndex1, timeDelta1);
            workDays[truck][days[1]].AddStop(delivery2, workDayIndex2, routeIndex2, timeDelta2);
            workDays[truck][days[2]].AddStop(delivery3, workDayIndex3, routeIndex3, timeDelta3);
            workDays[truck][days[3]].AddStop(delivery4, workDayIndex4, routeIndex4, timeDelta4);

            delivery1.truck = truck;
            delivery2.truck = truck;
            delivery3.truck = truck;
            delivery4.truck = truck;

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

    public Delivery StageRemoveRandomDelivery(Random rng, Judge judge, out float[] timeDeltas)
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

        timeDeltas = new float[delivery.address.frequency];

        //Second testify
        float testimony = delivery.address.emptyingTime * delivery.address.frequency * 3;

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[delivery.truck][delivery.day].StageRemoveStop(delivery, judge, out timeDeltas[0]);

        for (int i = 0; i < delivery.others.Length; i++)
            workDays[delivery.others[i].truck][delivery.others[i].day].StageRemoveStop(delivery.others[i], judge, out timeDeltas[i + 1]);

        return delivery;
    }

    //Only call if the judgement is pass!
    public void RemoveDelivery(Delivery delivery, float[] timeDeltas)
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
        float testimony = delivery.address.emptyingTime * delivery.address.frequency * 3;

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[delivery.truck][delivery.day].StageRemoveStop(delivery, judge, out float timeDelta);

        float[] otherTimeDeltas = new float[delivery.others.Length];

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

    /// <summary>
    /// Capable of swapping places of two deliveries between any route lists.
    /// </summary>
    public void SwapDeliveries(Delivery del1, Delivery del2, IndexedLinkedList<Delivery> list1, IndexedLinkedList<Delivery> list2, Judge judge)
    {
        int index1 = del1.routeNode.index;
        int index2 = del2.routeNode.index;

        Address address1 = del1.address;
        Address address2 = del2.address;

        int thisID1 = address1.matrixID;
        int thisID2 = address2.matrixID;
        int prevID1 = list1.nodes[index1].prev.value.address.matrixID;
        int nextID1 = list1.nodes[index1].next.value.address.matrixID;
        int prevID2 = list2.nodes[index2].prev.value.address.matrixID;
        int nextID2 = list2.nodes[index2].next.value.address.matrixID;

        //initial distances between the nodes and their neighbors
        float oldValue = Input.GetTimeFromTo(prevID1, thisID1) + Input.GetTimeFromTo(thisID1, nextID1) + Input.GetTimeFromTo(prevID2, thisID2) + Input.GetTimeFromTo(thisID2, nextID2);
        //new distances between the nodes and their new neighbors
        float newValue = Input.GetTimeFromTo(prevID1, thisID2) + Input.GetTimeFromTo(thisID2, nextID1) + Input.GetTimeFromTo(prevID2, thisID1) + Input.GetTimeFromTo(thisID1, nextID2);

        float testimony = newValue - oldValue;

        judge.Testify(testimony);

        if (judge.GetJudgement() == Judgement.Pass)
            SwapNodes(del1.routeNode, del2.routeNode, list1, list2);
    }

    /// <summary>
    /// Takes two nodes from separate Indexed Linked Lists and warps space and time itself to swap them.
    /// </summary>

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
        (node1.index, node2.index) = (node2.index, node1.index);

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
        (node1.index, node2.index) = (node2.index, node1.index);
    }

    public void ShuffleNode2(Random rng, Judge judge)
    {
        Delivery removedDelivery = StageRemoveRandomDelivery(rng, judge, out float[] removeTimeDeltas);

        if (removedDelivery == null)
            return;

        StageDelivery(removedDelivery.address, rng, judge, out Delivery[] deliveries, out int[] workDayIndexes, out int[] routeIndexes, out float[] addTimeDeltas);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            RemoveDelivery(removedDelivery, removeTimeDeltas);

            for (int i = 0; i < deliveries.Length; i++)
            {
                AddDelivery(deliveries[i], workDayIndexes[i], routeIndexes[i], addTimeDeltas[i]);
            }
        }
    }

    public void ShuffleNode(Random rng, Judge judge)
    {
        // First calculate variables

        // From
        int randomTruckFrom = rng.Next(0, 2);
        int randomDayFrom = rng.Next(0, 5);
        WorkDay wFrom = workDays[randomTruckFrom][randomDayFrom];
        int randomRouteIndexFrom = wFrom.workDay.getRandomIncluded(rng);
        Route rFrom = wFrom.workDay.nodes[randomRouteIndexFrom].value;
        int randomNodeFrom = rFrom.route.getRandomIncluded(rng);

        IndexedLinkedListNode<Delivery> node = rFrom.route.nodes[randomNodeFrom];
        IndexedLinkedList<Delivery> fromList = rFrom.route;

        // To
        int randomTruckTo = rng.Next(0, 2);
        int randomDayTo = rng.Next(0, 5);
        WorkDay wTo = workDays[randomTruckFrom][randomDayFrom];
        int randomRouteIndexTo = wTo.workDay.getRandomIncluded(rng);
        Route rTo = wTo.workDay.nodes[randomRouteIndexTo].value;
        int randomNodeTo = rTo.route.getRandomIncluded(rng);

        IndexedLinkedListNode<Delivery> atNode = rTo.route.nodes[randomNodeTo];
        IndexedLinkedList<Delivery> toList = rTo.route;

        int fromID = node.value.address.matrixID;
        int toID = atNode.value.address.matrixID;
        int fromPrevID = node.prev.value.address.matrixID;
        int fromNextID = node.next.value.address.matrixID;
        int toPrevID = atNode.prev.value.address.matrixID;
        int toNextID = atNode.next.value.address.matrixID;


        float oldValue = Input.GetTimeFromTo(fromPrevID, fromID) + Input.GetTimeFromTo(fromID, fromNextID) + Input.GetTimeFromTo(toID, toNextID);
        float newValue = Input.GetTimeFromTo(fromPrevID, fromNextID) + Input.GetTimeFromTo(toID, fromID) + Input.GetTimeFromTo(fromID, toNextID);

        float testimony = newValue - oldValue;
        // TO DO: Potentially add penalty for bas delivery schedule

        judge.Testify(testimony);


        if (judge.GetJudgement() == Judgement.Pass)
        {
            if (fromList == toList)
            {
                fromList.ShuffleNode(node.index, atNode.index);
            }

            fromList.RemoveNode(node.index);
            toList.InsertAfter(node.value, atNode.index);
        }
    }
}

