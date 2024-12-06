using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

class Solution
{
    //Each truck has 5 routes, one per day.
    private WorkDay[][] solution = new WorkDay[2][] { new WorkDay[5], new WorkDay[5] };

    public float score; //The score in seconds

    public Solution()
    {
        for (int i = 0; i < Input.orderCount; i++)
        {
            score += Input.orders[i].emptyingTime * Input.orders[i].frequency * 3;
        }
    }

    public void UpdateSolution(Schedule schedule, float score)
    {
        this.score = score;

        WorkDay[][] copy = new WorkDay[2][] { new WorkDay[5], new WorkDay[5] };
        for (int i = 0; i < copy.Length; i++) // foreach truck
        {
            for (int j = 0; j < copy[i].Length; j++) // foreach workday
                copy[i][j] = schedule.workDays[i][j].Clone();
        }

        solution = copy;
    }

    /// <summary>
    /// Prints the solution as specified in Format-invoer-checker.docx
    /// Truck.no;Day.no;#Address;AddressID
    /// </summary>
    public string PrintSolution(bool write)
    {
        using (StreamWriter sw = new StreamWriter(@"..\\..\\..\\solution.txt"))
        {
            int truck, day;
            Tuple<string, string>[] addresses;
            int startAddressNumber = 1;

            for (int i = 0; i < solution.Length; i++) // foreach truck
            {
                truck = i + 1; // 0,1 -> 1,2
                for (int j = 0; j < solution[i].Length; j++) // foreach workday
                {
                    startAddressNumber = 1;

                    WorkDay w = solution[i][j];
                    day = w.weekDay + 1;

                    int currentIndex = w.workDay.currentIndex;
                    IndexedLinkedListNode<Route> currentNode = w.workDay.nodes[0];
                    for (int k = 0; k < currentIndex + 1; k++) // foreach route
                    {
                        if (currentNode.value.route.currentIndex == 0)
                        {
                            currentNode = currentNode.next;
                            continue;
                        }

                        addresses = currentNode.value.GetAddresses(startAddressNumber);
                        currentNode = currentNode.next;

                        foreach (Tuple<string, string> address in addresses)
                        {
                            if (write)
                            {
                                sw.WriteLine($"{truck};{day};{address.Item1};{address.Item2}");
                            }
                            else
                            {
                                Console.WriteLine($"{truck};{day};{address.Item1};{address.Item2}");
                            }
                            startAddressNumber++;
                        }

                    }

                }
            }
        }
        return "";
    }
}

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
        //workDays[truck][day1].AddRandomStop(delivery1, rng, judge);
        //workDays[truck][day2].AddRandomStop(delivery2, rng, judge);
        workDays[truck][day1].StageRandomStop(delivery1, rng, judge, out int workDayIndex1, out int routeIndex1, out float timeDelta1);
        workDays[truck][day2].StageRandomStop(delivery2 , rng, judge, out int workDayIndex2, out int routeIndex2, out float timeDelta2);

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
        //workDays[truck][0].AddRandomStop(delivery1, rng, judge);
        //workDays[truck][2].AddRandomStop(delivery2, rng, judge);
        //workDays[truck][4].AddRandomStop(delivery3, rng, judge);

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
        //workDays[truck][days[0]].AddRandomStop(delivery1, rng, judge);
        //workDays[truck][days[1]].AddRandomStop(delivery2, rng, judge);
        //workDays[truck][days[2]].AddRandomStop(delivery3, rng, judge);
        //workDays[truck][days[3]].AddRandomStop(delivery4 , rng, judge);

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
        // To
        int randomTruckTo = rng.Next(0, 2);
        int randomDayTo = rng.Next(0, 5);
        WorkDay wTo = workDays[randomTruckFrom][randomDayFrom];
        int randomRouteIndexTo = wTo.workDay.getRandomIncluded(rng);
        Route rTo = wTo.workDay.nodes[randomRouteIndexTo].value;
        int randomNodeTo = rTo.route.getRandomIncluded(rng);

        IndexedLinkedListNode<Delivery> node = rFrom.route.nodes[randomNodeFrom];

        IndexedLinkedList<Delivery> fromList = rFrom.route;
        IndexedLinkedList<Delivery> toList = rTo.route;

        IndexedLinkedListNode<Delivery> atNode = rTo.route.nodes[randomNodeTo];


        //Second testify
        float testimony = 0; // TODO: FIll in the correct testimony
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

class Delivery : IClonable<Delivery>
{
    public Address address;
    public int truck;
    public int day;
    public IndexedLinkedListNode<Delivery> scheduleNode;
    public IndexedLinkedListNode<Route> workDayNode;
    public IndexedLinkedListNode<Delivery> routeNode;
    public Delivery[] others;

    public Delivery()
    {

    }

    public Delivery(Address address)
    {
        this.address = address;
        this.others = new Delivery[address.frequency-1];
    }

    public Delivery Clone()
    {
        Delivery copy = new Delivery();

        copy.address = address.Clone();

        return copy;
        // throw new NotImplementedException("This shouldn't be called");
    }

    public override string ToString()
    {
        return address.ToString();
    }
} 

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
        WorkDay copy = new WorkDay(0 ,Input.orderCount);

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

class Route : IClonable<Route>
{
    public IndexedLinkedList<Delivery> route;
    
    int collectedGarbage = 0;
    int maximumGarbage = 100000; //Before compression we do not need to calculate the compression

    float duration = 30; //Time to empty at depot is 30 min

    public Route()
    {
        route = new IndexedLinkedList<Delivery>(new Delivery(Address.Depot()), Input.orderCount + 1);
        route.startIndex = 1;
    }

    public void StageRandomStop(Delivery delivery, float maximumTimeLeft, Random rng, Judge judge, out int routeIndex, out float timeDelta)
    {
        //First calculate variables
        routeIndex = 0;

        if (route.startIndex <= route.currentIndex)
            routeIndex = route.getRandomIncluded(rng);

        Address address = delivery.address;
        int prevID = route.nodes[routeIndex].value.address.matrixID;
        int nextID = route.nodes[routeIndex].next.value.address.matrixID;

        int newGarbageAmount = collectedGarbage + delivery.address.garbageAmount;

        //Second testify
        float testimony =
            Input.GetTimeFromTo(prevID, address.matrixID) + //New values are added
            Input.GetTimeFromTo(address.matrixID, nextID) -
            Input.GetTimeFromTo(prevID, nextID) + 
            delivery.address.emptyingTime; //Old value is substracted

        if (routeIndex == 0)
            testimony += 30; //Because the emptying time at depot is 30 min

        if (newGarbageAmount > maximumGarbage || testimony > maximumTimeLeft) //Hard limits
            judge.OverrideJudge(Judgement.Fail);

        judge.Testify(testimony);

        timeDelta = testimony;
    }

    public void AddStop(Delivery delivery, int routeIndex, float timeDelta)
    {
        collectedGarbage += delivery.address.garbageAmount;
        duration += timeDelta; //Same value as testimony as it just calculates the time change in the route.

        delivery.routeNode = route.InsertAfter(delivery, routeIndex);
    }

    /// <summary>
    /// Call this before RemoveStop and pass the corresponding arguments
    /// </summary>
    public void StageRemoveStop(Delivery delivery, Judge judge, out float timeDelta)
    {
        //First calculate variables
        int index = delivery.routeNode.index;

        Address address = delivery.address;
        int prevID = route.nodes[index].prev.value.address.matrixID;
        int nextID = route.nodes[index].next.value.address.matrixID;

        //Second testify
        float testimony =
            Input.GetTimeFromTo(prevID, nextID) - //New value
            (Input.GetTimeFromTo(prevID, address.matrixID) + //Old values are substracted
            Input.GetTimeFromTo(address.matrixID, nextID)) -
            delivery.address.emptyingTime;

        if (route.currentIndex == 1) //There are two nodes
            testimony -= 30; //Minus 30 minutes because you no longer have the 30 min emptying time.

        judge.Testify(testimony);

        timeDelta = testimony;
    }

    /// <summary>
    /// Call tgis after calling StageRemoveStop and use the correspoding returns.
    /// The judgement is assumed to be passed (check before calling).
    /// </summary>
    public void RemoveStop(Delivery delivery, float timeDelta)
    {
        collectedGarbage -= delivery.address.garbageAmount;
        duration += timeDelta; //Same value as testimony as it just calculates the time change in the route.

        route.RemoveNode(delivery.routeNode.index);
    }
    
    //Swaps two deliveries in the same route (depot cycle)
    public void SwapStops(Delivery del1, Delivery del2, Judge judge)
    {
        int index1 = del1.routeNode.index;
        int index2 = del2.routeNode.index;

        Address address1 = del1.address;
        Address address2 = del2.address;

        int thisID1 = address1.matrixID;
        int thisID2 = address2.matrixID;
        int prevID1 = route.nodes[index1].prev.value.address.matrixID;
        int nextID1 = route.nodes[index1].next.value.address.matrixID;
        int prevID2 = route.nodes[index2].prev.value.address.matrixID;
        int nextID2 = route.nodes[index2].next.value.address.matrixID;

        float oldValue = Input.GetTimeFromTo(prevID1, thisID1) + Input.GetTimeFromTo(thisID1, nextID1) + Input.GetTimeFromTo(prevID2, thisID2) + Input.GetTimeFromTo(thisID2, nextID2);
        float newValue = Input.GetTimeFromTo(prevID1, thisID2) + Input.GetTimeFromTo(thisID2, nextID1) + Input.GetTimeFromTo(prevID2, thisID1) + Input.GetTimeFromTo(thisID1, nextID2);

        float testimony = newValue - oldValue;

        judge.Testify(testimony);

        if (judge.GetJudgement() == Judgement.Pass)
            route.SwapNodes(del1.routeNode.index, del2.routeNode.index);
    }

    public Route Clone()
    {
        Route copy = new Route();

        copy.route = route.Clone();
        copy.collectedGarbage = collectedGarbage;
        copy.duration = duration;
        copy.maximumGarbage = maximumGarbage;

        return copy;
    }

    /// <summary>
    /// Returns the each of the Address.no and the AddressID of a Route as a array of tuples
    /// (To be used in Solution.PrintSolution())
    /// </summary>
    public Tuple<string, string>[] GetAddresses(int startAddressNumber)
    {
        // Make an array with length of the amount of nodes (currentIndex + 1)
        Tuple<string, string>[] addresses = new Tuple<string, string>[this.route.currentIndex + 1];

        IndexedLinkedListNode<Delivery> currentNode = this.route.nodes[0].next;
        for (int i = 0; i < this.route.currentIndex + 1; i++)
        {
            string orderId = currentNode.value.address.orderID.ToString();
            int addressNumber = startAddressNumber + i;
            addresses[i] = Tuple.Create(addressNumber.ToString(), orderId);
            currentNode = currentNode.next;
        }

        return addresses;
    }
}

class Address : IClonable<Address>
{
    public string name;
    public int orderID;
    public int matrixID;
    public int garbageAmount; //Total amount of garbage to be picked up at this location
    public float emptyingTime;
    public int containerAmount;
    public int frequency;

    public Address()
    {

    }

    public Address(string s)
    {
        name = s;
        orderID = 0;
        matrixID = 0;
        garbageAmount = 0;
        emptyingTime = 0;
        containerAmount = 0;
        frequency = 1;
    }

    public Address(Order order)
    {
        name = order.location;
        orderID = order.id;
        matrixID = order.matrixID;
        garbageAmount = order.containerVolume * order.containerAmount;
        emptyingTime = order.emptyingTime;
        containerAmount = order.containerAmount;
        frequency = order.frequency;
    }

    public static Address Depot()
    {
        return new Address("Depot");
    }

    public static bool operator == (Address a, Address b)
    {
        return a.matrixID == b.matrixID;
    }

    public static bool operator != (Address a, Address b)
    {
        return a.matrixID != b.matrixID;
    }

    public Address Clone()
    {
        Address clone = new Address();

        clone.name = name;
        clone.orderID = orderID;
        clone.matrixID = matrixID;
        clone.garbageAmount = garbageAmount;
        clone.emptyingTime = emptyingTime;
        clone.frequency = frequency;

        return clone;
    }

    public override string ToString()
    {
        return name;
    }
}