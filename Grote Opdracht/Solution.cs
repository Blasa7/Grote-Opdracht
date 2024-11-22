using System.Collections;

class Solution
{

}

class Route //Dit is een linked list
{   //                .
    //[a, b, c, d, e, h, g, f, i, ..]
    //currentIndex = 3
    // b<-a->c
    public LocationNode[] nodes;
    //index of the last location that IS part of the route
    public int currentIndex;

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
        //cannot swap with itself (or rather, it wouldn't change anything)
        if (leftIndex == rightIndex)
            throw new System.Exception("Cannot swap a node with itself!");

        LocationNode prevLeftNode = nodes[leftIndex].prev;
        LocationNode nextLeftNode = nodes[leftIndex].next;

        //If leftIndex is the left neighbor of the rightIndex
        if (nodes[leftIndex].next == nodes[rightIndex])
        {
            
            nodes[leftIndex].prev = nodes[rightIndex];
            nodes[leftIndex].next = nodes[rightIndex].next;

            nodes[rightIndex].next = nodes[leftIndex];
            nodes[rightIndex].prev = prevLeftNode;

            //Update the neighbors
            nodes[rightIndex].prev.next = nodes[rightIndex];
            nodes[leftIndex].next.prev = nodes[leftIndex];

            return;
        }

        //If the leftIndex is the right neighbor of the rightIndex
        if (nodes[leftIndex].prev == nodes[rightIndex])
        {
            
            nodes[leftIndex].next = nodes[rightIndex];
            nodes[leftIndex].prev = nodes[rightIndex].prev;

            nodes[rightIndex].prev = nodes[leftIndex];
            nodes[rightIndex].next = nextLeftNode;

            //Update the neighbors
            nodes[leftIndex].prev.next = nodes[leftIndex];
            nodes[rightIndex].next.prev = nodes[rightIndex];
            
            return;
        }

        
        //left points to right's neighbors
        nodes[leftIndex].prev = nodes[rightIndex].prev;
        nodes[leftIndex].next = nodes[rightIndex].next;

        //right's neighbors point to left
        nodes[leftIndex].prev.next = nodes[leftIndex];
        nodes[leftIndex].next.prev = nodes[leftIndex];

        //right points to left's neighbors
        nodes[rightIndex].prev = prevLeftNode;
        nodes[rightIndex].next = nextLeftNode;

        //left's neighbors point to right
        nodes[rightIndex].prev.next = nodes[rightIndex];
        nodes[rightIndex].next.prev = nodes[rightIndex];
    }

    public override string ToString(){
        string r = "";
        for (int i = 0; i < currentIndex + 1 ; i++)
        {
            r += nodes[i].ToString() + "\n";
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
        return "prev = " + prev.address + "\n" + address + "\n" + "next = " + next.address;
    }
}