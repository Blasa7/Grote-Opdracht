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
    // total driving time = ab + bc + cd + de + ef + fa
    // relevant driving time = bc + cd + de + ef

    // remove c
    // ...a - b - d - e - f - a...
    // total driving time = ab + bd + de + ef + fa  ( -bc  -cd  +bd )   
    // relevant driving time = bd + de + ef         ( -bc  -cd  +bd )   all same

    // remove b
    //...a - d - e - f - a...
    // total driving time = ad + de + ef + fa   ( -ab  -bd  +ad )       
    // relevant driving time = de + ef          ( -bd )                 only the middle

    // add g after f
    //...a - d - e - f - g - a...
    // total driving time = ad + de + ef + fg   ( -fa  +fg  +ga )       
    // relevant driving time = de + ef + fg     ( +fg )                 only the middle

    public Route()
    {
        route = new IndexedLinkedList<Delivery>(new Delivery(Address.Depot()), Input.orderCount + 1);
        route.startIndex = 1;
    }

    public void StageRandomStop(Delivery delivery, int routeNum, Random rng, Judge judge, out int routeIndex, out int timeDelta, out int routeNumDelta)
    {
        routeNumDelta = 0;

        //First calculate variables
        routeIndex = route.getRandomIncluded(rng, 0);

        Address address = delivery.address;
        int prevID = route.nodes[routeIndex].value.address.matrixID;
        int nextID = route.nodes[routeIndex].next.value.address.matrixID;

        int newGarbageAmount = collectedGarbage + delivery.address.garbageAmount;

        //Second testify
        timeDelta =
            Input.GetTimeFromTo(prevID, address.matrixID) + //New values are added
            Input.GetTimeFromTo(address.matrixID, nextID) -
            Input.GetTimeFromTo(prevID, nextID) +
            delivery.address.emptyingTime; //Old value is substracted


        // Check that max num of filled routes <= GlobalMaxOfRoutes
        if (route.currentIndex == 0)
        {
            //if (Schedule.StagedNumOfRoutes >= Schedule.GlobalMaxOfRoutes) //cannot add past the maximum number of routes
            if (routeNum >= judge.maxRoutes)
            {
                judge.OverrideJudge(Judgement.Fail);
            }
            else
            {
                timeDelta += 1800000; //Because the emptying time at depot is 30 min
                routeNumDelta = 1;//judge.stagedNumRoutes++;
            }
        }

        int penalty = 0;

        //Soft constraint:
        //If the maximum garbage is exceeded, add a penalty
        if (newGarbageAmount > maximumGarbage)
        {
            if (collectedGarbage < maximumGarbage) //with this stop, it firsts exceeds the limit
            {
                penalty = newGarbageAmount - maximumGarbage; //only add the diffrence from the maximum
            }
            else //there was already too much garbage
            {
                penalty = newGarbageAmount - collectedGarbage;
            }
        }

        // Soft constraint:
        // If there was a penalty, lower it after removing a stop

        /*int newTotalGarbage = collectedGarbage + delivery.address.garbageAmount;

        if (newTotalGarbage > maximumGarbage)
        {
            //120 > 100

            int overLimit = newTotalGarbage - maximumGarbage; //120 - 100
            int delta = delivery.address.garbageAmount; // 5
            //int newGarbageAmount = collectedGarbage - delta;

            penalty = Math.Min(overLimit, delta);

        }*/

        judge.Testify(timeDelta, 0, penalty);
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
        if (routeIndex == 0) //Add after depot
        {
            relevantDelta = Input.GetTimeFromTo(thisID, nextID);
            //if (route.currentIndex == 0) // The route is empty
            //{
            //    judge.numRoutes++;
            //}
        }
        else if (routeIndex == route.currentIndex) //Add before depot
        {
            relevantDelta = Input.GetTimeFromTo(prevID, thisID);
        }

        relevantDrivingTime += relevantDelta;

        delivery.routeNode = route.InsertAfter(delivery, routeIndex);
    }

    /// <summary>
    /// Call this before RemoveStop and pass the corresponding arguments
    /// </summary>
    public void StageRemoveStop(Delivery delivery, int routeNum, Judge judge, out int timeDelta, out int routeNumDelta)
    {
        routeNumDelta = 0;

        //First calculate variables
        int index = delivery.routeNode.index;

        Address address = delivery.address;
        int prevID = route.nodes[index].prev.value.address.matrixID;
        int nextID = route.nodes[index].next.value.address.matrixID;

        //Second testify
        timeDelta =
            Input.GetTimeFromTo(prevID, nextID) - //New value
            (Input.GetTimeFromTo(prevID, address.matrixID) + //Old values are substracted.
            Input.GetTimeFromTo(address.matrixID, nextID)) -
            delivery.address.emptyingTime;

        if (route.currentIndex == 1) //There are two nodes
        {
            if (routeNum <= judge.minRoutes) //cannot remove past the minimum number of routes
                judge.OverrideJudge(Judgement.Fail);
            else
            {
                timeDelta -= 1800000; //Minus 30 minutes because you no longer have the 30 min emptying time.
                routeNumDelta = -1;
            }
        }

        int penalty = 0;

        // Soft constraint:
        // If there was a penalty, lower it after removing a stop
        if (collectedGarbage > maximumGarbage)
        {
            //120 > 100

            int overLimit = collectedGarbage - maximumGarbage; //102 - 100 = 2 // 120 - 5 = 15 
            int delta = delivery.address.garbageAmount; // 5
            //int newGarbageAmount = collectedGarbage - delta;

            penalty = Math.Min(overLimit, delta);
            
        }

        judge.Testify(timeDelta, 0, -penalty);
    }

    /// <summary>
    /// Call this after calling StageRemoveStop and use the correspoding returns.
    /// The judgement is assumed to have passed (check before calling).
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
        if (removedIndex == 1) //Remove node after depot
        {
            relevantDelta = -Input.GetTimeFromTo(thisID, nextID);
            //if (route.currentIndex == 1) // gloabal number of used routes now decreases
            //{
            //    Schedule.GlobalNumOfRoutes--;
            //}
        }
        else if (removedIndex == route.currentIndex) //Remove node before depot
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
            Input.GetTimeFromTo(currentID, removeNextID));

        int addPrevID = route.nodes[newIndex].prev.value.address.matrixID;
        int addID = route.nodes[newIndex].value.address.matrixID;
        int addNextID = route.nodes[newIndex].next.value.address.matrixID;

       if (route.nodes[newIndex].next.index == route.nodes[index].index) //This means the current node is swapped with its left neighbor
       {
            addNextID = removeNextID;
       }

        addTimeDelta =
            Input.GetTimeFromTo(addID, currentID) + //New values are added
            Input.GetTimeFromTo(currentID, addNextID) -
            Input.GetTimeFromTo(addID, addNextID); //Old value is substracted

        timeDelta = removeTimeDelta + addTimeDelta;

        // Garbageamount stays the same
        judge.Testify(timeDelta, 0, 0);
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