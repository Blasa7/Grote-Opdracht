using System.CodeDom.Compiler;
using System.Collections;
using System.Xml.Linq;

class Solution
{
    //Each truck has 5 routes, one per day.
    private Route[] Truck1 = new Route[5];
    private Route[] Truck2 = new Route[5];

    public int score; //The score in seconds

    public Solution()
    {
        GenerateInitialSolution();
    }

    public void GenerateInitialSolution()
    {
        //WIP
        for (int i = 0; i < Truck1.Length; i++)
        {
            Truck1[i] = new Route(new Adress("depot"), 1000);
        }
        for (int i = 0; i < Truck2.Length; i++)
        {
            Truck2[i] = new Route(new Adress("depot"), 1000);
        }
    }
    public int CalculateDifference(Route route)
    {
        return 0;
    }

    public int CalculateScore(Route route, int oldScore)
    {
        int difference = CalculateDifference(route);
        return oldScore + difference;
    }
}

class Schedule
{
    LinkedList<Delivery>[] deliveries = new LinkedList<Delivery>[5];

    /// <summary>
    /// Adds a one time delivery to the given route at random.
    /// </summary>
    void AddOneTimeDelivery(OneTimeDelivery delivery, Route route, Random rng)
    {
        route.AddRandomStop(delivery.adress, rng);
    }

    void RemoveOneTimeDelivery(OneTimeDelivery delivery, Route route, Random rng)
    {
        route.RemoveNode(delivery.adress.routeIndex);
    }

    void ShuffleOneTimeDelivery(OneTimeDelivery delivery, Random rng)
    {
        delivery.node.List.Remove(delivery.node);

        deliveries[rng.Next(0, 5)].AddLast(delivery.node);
    }

    void ShuffleTwoTimeDelivery(TwoTimeDeliery delivery, Random rng)
    {
        
    }
}

abstract class Delivery
{
    public Adress adress;
    public LinkedListNode<Delivery> node;

    public Delivery(Adress adress, LinkedListNode<Delivery> node)
    {
        this.adress = adress;
        this.node = node;
    }
}

class OneTimeDelivery(Adress adress, LinkedListNode<Delivery> node) : Delivery(adress, node) { }

class TwoTimeDeliery(Adress adress, LinkedListNode<Delivery> node) : Delivery(adress, node)
{ 
    TwoTimeDeliery other;
}

class ThreeTimeDelivery(Adress adress, LinkedListNode<Delivery> node) : Delivery(adress, node)
{
    ThreeTimeDelivery[] others = new ThreeTimeDelivery[2];
}

class FourTimeDelivery(Adress adress, LinkedListNode<Delivery> node) : Delivery (adress, node) 
{
    FourTimeDelivery[] others = new FourTimeDelivery[3];
}

class Route // This is a linked list
{     
    /// <summary>
    /// Array of all locations that are either included or excluded from the route.
    /// </summary>
    public LocationNode[] nodes;

    /// <summary>
    /// Index of the last node that is part of the route.
    /// </summary>
    public int currentIndex;

    /// <summary>
    /// Empty constructor only use for testing.
    /// </summary>
    public Route() { }

    /// <summary>
    /// Constructor of a route must recieve an array with all locations 
    /// with the location at position 0 being the start and end.
    /// </summary>
    public Route(Adress depot, int locationCount)
    {
        nodes = new LocationNode[locationCount];

        nodes[0] = new LocationNode(depot);
        nodes[0].prev = nodes[0];
        nodes[0].next = nodes[0];

        currentIndex = 0;
    }

    /// <summary>
    /// Returns a random index from the nodes that are part of the route excluding the start node.
    /// Do not use on a route with only a single node.
    /// </summary>
    public int getRandomIncluded(Random rng)
    {
        return rng.Next(1, currentIndex + 1);
    }

    /// <summary>
    /// Inserts the node at nodeIndex after the node at prevIndex.
    /// </summary>
    public void InsertAfter(Adress address, int prevIndex)
    {
        LocationNode current = new LocationNode(address);

        LocationNode prev = nodes[prevIndex];
        LocationNode next = nodes[prevIndex].next;

        //Swap pointers from neighbors
        prev.next = current;
        next.prev = current;

        current.prev = prev;
        current.next = next;

        //Increase the current index before swapping the element at currentIndex
        currentIndex++;
        current.address.routeIndex = currentIndex;
        nodes[currentIndex] = current;
    }

    /// <summary>
    /// Removes the node at nodeIndex.
    /// </summary>
    public void RemoveNode(int nodeIndex)
    {
        //Removing the node from the linked list.
        nodes[nodeIndex].prev.next = nodes[nodeIndex].next;
        nodes[nodeIndex].next.prev = nodes[nodeIndex].prev;

        //Swap the to be deleted node with the last node in the used nodes.
        (nodes[nodeIndex], nodes[currentIndex]) = (nodes[currentIndex], nodes[nodeIndex]);

        //Lower the current index to discard the last element.
        currentIndex--;
    }

    /// <summary>
    /// Swaps two nodes. A node is not allowed to swap with itself.
    /// </summary>
    public void SwapNodes(int leftIndex, int rightIndex)
    {
        //A node is not allowed to swap with itself.
        if (leftIndex == rightIndex)
            throw new System.Exception("Cannot swap a node with itself!");

        LocationNode prevLeftNode = nodes[leftIndex].prev;
        LocationNode leftNode = nodes[leftIndex];
        LocationNode nextLeftNode = nodes[leftIndex].next;

        LocationNode rightNode = nodes[rightIndex];

        leftNode.address.routeIndex = rightIndex;
        rightNode.address.routeIndex = leftIndex;

        //If the left node is the right neighbor of the right node
        //swap them to ensure the leftNode is to the left of the rightNode.
        if (prevLeftNode == rightNode)
        {
            prevLeftNode = nodes[rightIndex].prev;
            leftNode = nodes[rightIndex];
            nextLeftNode = nodes[rightIndex].next;

            rightNode = nodes[leftIndex];
        }

        //Unique case when leftIndex is the direct neighbor of the rightIndex.
        if (nextLeftNode == rightNode)
        {
            
            leftNode.prev = rightNode;
            leftNode.next = rightNode.next;

            rightNode.next = leftNode;
            rightNode.prev = prevLeftNode;

            //Update the neighbors
            rightNode.prev.next = rightNode;
            leftNode.next.prev = leftNode;

            return;
        }
        
        //Default case where two nodes are not neighboring eachother.

        //Left points to right's neighbors.
        leftNode.prev = rightNode.prev;
        leftNode.next =rightNode.next;

        //Right's neighbors point to left.
        leftNode.prev.next = leftNode;
        leftNode.next.prev = leftNode;

        //Right points to left's neighbors.
        rightNode.prev = prevLeftNode;
        rightNode.next = nextLeftNode;

        //Left's neighbors point to right.
        rightNode.prev.next = rightNode;
        rightNode.next.prev = rightNode;
    }

    public void AddRandomStop(Adress adress, Random rng)
    {
        InsertAfter(adress, getRandomIncluded(rng));
    }

    /// <summary>
    /// Converts the route to a string where nodes are added in order of route traversal.
    /// </summary>
    public override string ToString()
    {
        string r = "";
        LocationNode currentNode = nodes[0];

        for (int i = 0; i < currentIndex + 1 ; i++)
        {
            r += currentNode.ToString() + "\n";
            currentNode = currentNode.next;
        }
        return r;
    }
}

class LocationNode
{
    public LocationNode prev;
    public LocationNode next;
    public Adress address;

    public LocationNode(Adress address)
    {
        this.address = address;
    }

    public override string ToString(){
        return "Node { prev = " + prev.address + ", value = " + address + ", " + "next = " + next.address + " }";
    }
}

class Adress
{
    public int routeIndex;
    public string name;

    public Adress(string s)
    {
        name = s;
    }

    public override string ToString()
    {
        return name;
    }
}