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
