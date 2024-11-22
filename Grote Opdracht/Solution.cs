using System.Collections;

class Solution
{

}

class Route //Dit is een linked list
{   //                .
    //[a, b, c, d, e, h, g, f, i, ..]
    //currentIndex = 3
    // b<-a->c
    private LocationNode[] nodes;
    int currentIndex;

    Random rng = new Random();
    public int getRandom(Random rng)
    {
        //WIP lol
        return rng.Next(1, nodes.Length);
    }

    public void InsertNode(int nodeIndex, int prevIndex, int nextIndex)
    {
        //Insert nodeIndex
        //Swap pointers from neighbors
        nodes[prevIndex].next = nodes[nodeIndex];
        nodes[nextIndex].prev = nodes[nodeIndex];
        
        nodes[nodeIndex].prev = nodes[prevIndex];
        nodes[nodeIndex].next = nodes[nextIndex];
        
        //Increase the current index
        currentIndex++;

        //Swap elements of the nodes array
        (nodes[nodeIndex], nodes[currentIndex]) = (nodes[currentIndex], nodes[nodeIndex]);
    }

    public void RemoveNode(int nodeIndex)
    {
        //Removing the node from the linked list
        nodes[nodeIndex].prev.next = nodes[nodeIndex].next;
        nodes[nodeIndex].next.prev = nodes[nodeIndex].prev;

        //Swap the to be deleted node with the last node in the used nodes
        (nodes[nodeIndex], nodes[currentIndex]) = (nodes[currentIndex], nodes[nodeIndex]);

        //Lower the current index to discard the last element
        currentIndex--;
    }

    public void SwapNodes(int leftIndex, int rightIndex)
    {
        LocationNode prevLeftIndex = nodes[leftIndex].prev;
        LocationNode nextLeftIndex = nodes[leftIndex].next;
        
        //left points to right's neighbors
        nodes[leftIndex].prev = nodes[rightIndex].prev;
        nodes[leftIndex].next = nodes[rightIndex].next;

        //right's neighbors point to left
        nodes[leftIndex].prev.next = nodes[leftIndex];
        nodes[leftIndex].next.prev = nodes[leftIndex];

        //right points to left's neighbors
        nodes[rightIndex].prev = prevLeftIndex;
        nodes[rightIndex].next = nextLeftIndex;

        //left's neighbors point to right
        nodes[rightIndex].prev.next = nodes[rightIndex];
        nodes[rightIndex].next.prev = nodes[rightIndex];
    }

}

class LocationNode
{
    public LocationNode prev;
    public LocationNode next;
}