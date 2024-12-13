using System.Dynamic;
using System.Net;

class Route : IClonable<Route>
{
    public IndexedLinkedList<Delivery> route;

    public int collectedGarbage = 0;
    public int maximumGarbage = 100000; //Before compression we do not need to calculate the compression

    /// <summary>
    /// The total distance of this route, including driving time, collecting time, etc..
    /// </summary>
    public int duration;

    /// <summary>
    /// The total driving time of this route.
    /// </summary>
    public int drivingTime;

    /// <summary>
    /// The driving time of this route, excluding time from & to depot.
    /// Idea is: some routes go all over the place (bad). These have a high relevant driving time.
    /// </summary>
    public int relevantDrivingTime;

    /// <summary>
    /// The driving time from and to the depot.
    /// </summary>
    public int depotDrivingTime { get { return drivingTime - relevantDrivingTime; } }

    // example
    // ...a - b - c - d - e - f - a...
    // a = depot
    // total distance = ab + bc + cd + de + ef + fa
    // relevant distance = bc + cd + de + ef

    // remove c
    // ...a - b - d - e - f - a...
    // total distance = ab + bd + de + ef + fa  ( -bc  -cd  +bd )   
    // relevant distance = bd + de + ef         ( -bc  -cd  +bd )   all same

    // remove b
    //...a - d - e - f - a...
    // total distance = ad + de + ef + fa   ( -ab  -bd  +ad )       
    // relevant distance = de + ef          ( -bd )                 only the middle

    // add g after f
    //...a - d - e - f - g - a...
    // total distance = ad + de + ef + fg   ( -fa  +fg  +ga )       
    // relevant distance = de + ef + fg     ( +fg )                 only the middle


    public Route()
    {
        route = new IndexedLinkedList<Delivery>(new Delivery(Address.Depot()), Input.orderCount + 1);
        route.startIndex = 1;
    }

    public void StageRandomStop(Delivery delivery, Random rng, Judge judge, out int routeIndex, out int timeDelta)
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
        int testimony =
            Input.GetTimeFromTo(prevID, address.matrixID) + //New values are added
            Input.GetTimeFromTo(address.matrixID, nextID) -
            Input.GetTimeFromTo(prevID, nextID) +
            delivery.address.emptyingTime; //Old value is substracted

        if (routeIndex == 0)
            testimony += 1800000;//30; //Because the emptying time at depot is 30 min

        if (newGarbageAmount > maximumGarbage) //Hard limits
            judge.OverrideJudge(Judgement.Fail);

        judge.Testify(testimony);

        timeDelta = testimony;
    }

    /// <summary>
    /// Add delivery to the route after routeIndex
    /// </summary>
    public void AddStop(Delivery delivery, int routeIndex, int timeDelta)
    {
        collectedGarbage += delivery.address.garbageAmount;
        duration += timeDelta; //Same value as testimony as it just calculates the time change in the route.

        int drivetimeDelta = timeDelta - delivery.address.emptyingTime;
        drivingTime += drivetimeDelta;

        //prepare the IDs
        int prevID = route.nodes[routeIndex].value.address.matrixID;
        int thisID = delivery.address.matrixID;
        int nextID = route.nodes[routeIndex].next.value.address.matrixID;

        int relevantDelta = drivetimeDelta;
        if (routeIndex == 0)
        {
            relevantDelta = Input.GetTimeFromTo(thisID, nextID);
        }
        else if (routeIndex == route.currentIndex) 
        {
            relevantDelta = Input.GetTimeFromTo(prevID, thisID);
        }

        relevantDrivingTime += relevantDelta;

        delivery.routeNode = route.InsertAfter(delivery, routeIndex);
    }

    /// <summary>
    /// Call this before RemoveStop and pass the corresponding arguments
    /// </summary>
    public void StageRemoveStop(Delivery delivery, Judge judge, out int timeDelta)
    {
        //First calculate variables
        int index = delivery.routeNode.index;

        Address address = delivery.address;
        int prevID = route.nodes[index].prev.value.address.matrixID;
        int nextID = route.nodes[index].next.value.address.matrixID;

        //Second testify
        int testimony =
            Input.GetTimeFromTo(prevID, nextID) - //New value
            (Input.GetTimeFromTo(prevID, address.matrixID) + //Old values are substracted.
            Input.GetTimeFromTo(address.matrixID, nextID)) -
            delivery.address.emptyingTime;

        if (route.currentIndex == 1) //There are two nodes
            testimony -= 1800000;//30; //Minus 30 minutes because you no longer have the 30 min emptying time.

        judge.Testify(testimony);

        timeDelta = testimony;

    }

    /// <summary>
    /// Call this after calling StageRemoveStop and use the correspoding returns.
    /// The judgement is assumed to be passed (check before calling).
    /// </summary>
    public void RemoveStop(Delivery delivery, int timeDelta)
    {
        collectedGarbage -= delivery.address.garbageAmount;
        duration += timeDelta; //Same value as testimony as it just calculates the time change in the route.

        int drivetimeDelta = timeDelta + delivery.address.emptyingTime;
        drivingTime += drivetimeDelta;

        //prepare the IDs
        int removedIndex = delivery.routeNode.index;
        int prevID = route.nodes[removedIndex].prev.value.address.matrixID;
        int thisID = delivery.address.matrixID;
        int nextID = route.nodes[removedIndex].next.value.address.matrixID;

        int relevantDelta = drivetimeDelta;
        if (removedIndex == 1)
        {
            relevantDelta = -Input.GetTimeFromTo(thisID, nextID);
        }
        else if (removedIndex == route.currentIndex)
        {
            relevantDelta = -Input.GetTimeFromTo(prevID, thisID);
        }

        relevantDrivingTime += relevantDelta;

        route.RemoveNode(delivery.routeNode.index);
    }


    /// <summary>
    /// Shuffle a node within the same Route
    /// </summary>
    public void StageShuffleRoute(Random rng, Judge judge, out Delivery changedDelivery, out Delivery newIndexDelivery, out int removeTimeDelta, out int addTimeDelta, out int timeDelta)
    {

        if (route.currentIndex <= 1) //2 or less nodes means no shuffling.
        {
            judge.OverrideJudge(Judgement.Fail);
            changedDelivery = null;
            newIndexDelivery = null;
            removeTimeDelta = 0;
            addTimeDelta = 0;
            timeDelta = 0;
            return;
        }

        int index = route.getRandomIncluded(rng); //The node that will be inserted
        int newIndex = (index + rng.Next(0, route.currentIndex - 2)) % route.currentIndex + 1; //The position that 'index' will be put after

        changedDelivery = route.nodes[index].value;
        newIndexDelivery = route.nodes[newIndex].value;

        int currentID = route.nodes[index].value.address.matrixID;

        int removePrevID = route.nodes[index].prev.value.address.matrixID;
        int removeNextID = route.nodes[index].next.value.address.matrixID;

        removeTimeDelta =
            Input.GetTimeFromTo(removePrevID, removeNextID) - //New value
            (Input.GetTimeFromTo(removePrevID, currentID) + //Old values are substracted
            Input.GetTimeFromTo(currentID, removeNextID)); //-
            //changedDelivery.address.emptyingTime;

        int addPrevID = route.nodes[newIndex].prev.value.address.matrixID;
        int addID = route.nodes[newIndex].value.address.matrixID;
        int addNextID = route.nodes[newIndex].next.value.address.matrixID;

       if (route.nodes[newIndex].next.index == route.nodes[index].index)//addNextID == currentID) //This means the current node is swapped with its left neighbor
       {
            addNextID = removeNextID;
       }

        addTimeDelta =
            Input.GetTimeFromTo(addID, currentID) + //New values are added
            Input.GetTimeFromTo(currentID, addNextID) -
            Input.GetTimeFromTo(addID, addNextID);// +
            //changedDelivery.address.emptyingTime; //Old value is substracted

        timeDelta = removeTimeDelta + addTimeDelta;

        judge.Testify(timeDelta);
    }

    public void ShuffleRoute(Delivery changedDelivery, Delivery newIndexDelivery, int removeTimeDelta, int addTimeDelta)
    {
        RemoveStop(changedDelivery, removeTimeDelta);
        AddStop(changedDelivery, newIndexDelivery.routeNode.index, addTimeDelta);
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