﻿using System.Net;

class Route : IClonable<Route>
{
    public IndexedLinkedList<Delivery> route;

    int collectedGarbage = 0;
    int maximumGarbage = 100000; //Before compression we do not need to calculate the compression

    public float duration; //Time to empty at depot is 30 min

    public Route()
    {
        route = new IndexedLinkedList<Delivery>(new Delivery(Address.Depot()), Input.orderCount + 1);
        route.startIndex = 1;
    }

    public void StageRandomStop(Delivery delivery, Random rng, Judge judge, out int routeIndex, out float timeDelta)
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

        if (newGarbageAmount > maximumGarbage) //Hard limits
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

    public void ShuffleRoute(Random rng, Judge judge, out float timeDelta)
    {
        if (route.currentIndex <= 1) //2 or less nodes means no shuffling.
        {
            judge.OverrideJudge(Judgement.Fail);
            timeDelta = 0;
            return;
        }

        int index = route.getRandomIncluded(rng); //1 - currentindex
        int newIndex = (index + rng.Next(1, route.currentIndex)) % route.currentIndex + 1; // 1 - currentindex idk fix this
            
            //(index % route.currentIndex) + rng.Next(1, route.currentIndex);//index + (rng.Next(1, route.currentIndex) % route.currentIndex) + 1;//1 or 2 -> 2 or 1 .... index + 1 % currentindex = 0 or 1
            
            //((index + (rng.Next(1, route.currentIndex))) % (route.currentIndex)) + 1; //Test this a bit
        
        Delivery delivery = route.nodes[index].value;

        int currentID = route.nodes[index].value.address.matrixID;

        int removePrevID = route.nodes[index].prev.value.address.matrixID;
        int removeNextID = route.nodes[index].next.value.address.matrixID;

        float removeTimeDelta =
            Input.GetTimeFromTo(removePrevID, removeNextID) - //New value
            (Input.GetTimeFromTo(removePrevID, currentID) + //Old values are substracted
            Input.GetTimeFromTo(currentID, removeNextID)) -
            delivery.address.emptyingTime;

        int addPrevID = route.nodes[newIndex].prev.value.address.matrixID;
        int addNextID = route.nodes[newIndex].next.value.address.matrixID;

        if (addPrevID == currentID) //This means the current node is swapped with its right neighbor
        {
            addPrevID = removeNextID;
        }
        else if (addNextID == currentID) //This means the current node is swapped with its left neighbor
        {
            addNextID = removePrevID;
        }

        float addTimeDelta =
            Input.GetTimeFromTo(addPrevID, currentID) + //New values are added
            Input.GetTimeFromTo(currentID, addNextID) -
            Input.GetTimeFromTo(addPrevID, addNextID) +
            delivery.address.emptyingTime; //Old value is substracted

        timeDelta = removeTimeDelta + addTimeDelta;

        judge.Testify(timeDelta);

        if (judge.GetJudgement() == Judgement.Pass)
        {
            route.SwapNodes(index, newIndex);
        }
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