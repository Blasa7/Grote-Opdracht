using System.Collections;

class Solution
{

}

class Route //Dit is een linked list
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
    public Route(string[] locations)
    {
        nodes = new LocationNode[locations.Length];

        for (int i = 0; i < locations.Length; i++)
        {
            nodes[i] = new LocationNode(locations[i]);
        }

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
    /// Returns a random index from the nodes that are not part of the route.
    /// </summary>
    public int getRandomExcluded(Random rng)
    {
        return rng.Next(currentIndex + 1, nodes.Length);
    }

    /// <summary>
    /// Inserts the node at nodeIndex after the node at prevIndex.
    /// </summary>
    public void InsertAfter(int nodeIndex, int prevIndex)
    {
        LocationNode prev = nodes[prevIndex];
        LocationNode current = nodes[nodeIndex];
        LocationNode next = nodes[prevIndex].next;

        //Swap pointers from neighbors
        prev.next = nodes[nodeIndex];
        next.prev = nodes[nodeIndex];

        current.prev = prev;
        current.next = next;

        //Increase the current index before swapping the element at currentIndex
        currentIndex++;

        //Swap elements of the nodes array
        (nodes[nodeIndex], nodes[currentIndex]) = (nodes[currentIndex], nodes[nodeIndex]);
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
    public string address;

    public LocationNode(string address)
    {
        this.address = address;
    }

    public override string ToString(){
        return "Node { prev = " + prev.address + ", value = " + address + ", " + "next = " + next.address + " }";
    }
}