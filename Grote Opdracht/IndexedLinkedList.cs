class IndexedLinkedList<Type> where Type: IClonable<Type>
{
    /// <summary>
    /// Array of all potential nodes that are either included or excluded from the route.
    /// </summary>
    public IndexedLinkedListNode<Type>[] nodes;

    public int startIndex = 0;

    /// <summary>
    /// Index of the last node that is part of the linked list.
    /// </summary>
    public int currentIndex;


    /// <summary>
    /// Constructor of a route must recieve an array with all locations 
    /// with the location at position 0 being the start and end.
    /// </summary>
    public IndexedLinkedList(Type head, int maximumSize)
    {
        nodes = new IndexedLinkedListNode<Type>[maximumSize];

        nodes[0] = new IndexedLinkedListNode<Type>(head);
        nodes[0].prev = nodes[0];
        nodes[0].next = nodes[0];

        startIndex = 0;
        currentIndex = 0;
    }

    public IndexedLinkedList(int maximumSize)
    {
        nodes = new IndexedLinkedListNode<Type>[maximumSize];

        startIndex = 0;
        currentIndex = -1;
    }

    /// <summary>
    /// Returns a random index from the nodes that are part of the linked list excluding the start node.
    /// Do not use on a linked list with only a single node.
    /// </summary>
    public int getRandomIncluded(Random rng)
    {
        return rng.Next(startIndex, currentIndex + 1);
    }

    /// <summary>
    /// Inserts the node at nodeIndex after the node at prevIndex.
    /// </summary>
    public IndexedLinkedListNode<Type> InsertAfter(Type value, int prevIndex)
    {
        //If the list is empty
        if (currentIndex == -1) 
        {
            nodes[0] = new IndexedLinkedListNode<Type>(value);
            nodes[0].prev = nodes[0];
            nodes[0].next = nodes[0];

            currentIndex++;

            return nodes[0];
        }

        IndexedLinkedListNode<Type> current = new IndexedLinkedListNode<Type>(value);

        IndexedLinkedListNode<Type> prev = nodes[prevIndex];
        IndexedLinkedListNode<Type> next = nodes[prevIndex].next;

        //Swap pointers from neighbors
        prev.next = current;
        next.prev = current;

        current.prev = prev;
        current.next = next;

        //Increase the current index before swapping the element at currentIndex
        currentIndex++;
        current.index = currentIndex;
        nodes[currentIndex] = current;

        return current;
    }

    /// <summary>
    /// Removes the node at nodeIndex.
    /// </summary>
    public void RemoveNode(int nodeIndex)
    {
        //Removing the node from the linked list.
        nodes[nodeIndex].prev.next = nodes[nodeIndex].next;
        nodes[nodeIndex].next.prev = nodes[nodeIndex].prev;

        //Swap the indexes
        nodes[currentIndex].index = nodes[nodeIndex].index;

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

        IndexedLinkedListNode<Type> prevLeftNode = nodes[leftIndex].prev;
        IndexedLinkedListNode<Type> leftNode = nodes[leftIndex];
        IndexedLinkedListNode<Type> nextLeftNode = nodes[leftIndex].next;

        IndexedLinkedListNode<Type> rightNode = nodes[rightIndex];

        leftNode.index = rightIndex;
        rightNode.index = leftIndex;

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

        //Default case where two nodes are not neighboring each other.

        //Left points to right's neighbors.
        leftNode.prev = rightNode.prev;
        leftNode.next = rightNode.next;

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
    /// Inserts a value node at the end of the linked list. 
    /// Note it is a circular linked list so this also means before the first node.
    /// </summary>
    public IndexedLinkedListNode<Type> InsertLast(Type value)
    {
        return InsertAfter(value, currentIndex);
    }

    public IndexedLinkedList<Type> Clone()
    {
        IndexedLinkedList<Type> copy = new IndexedLinkedList<Type>(nodes.Length);
        copy.startIndex = startIndex;

        IndexedLinkedListNode<Type> currentNode = nodes[0];

        for (int i = 0; i < currentIndex + 1; i++)
        {
            copy.InsertAfter(currentNode.value.Clone(), currentNode.prev.index);
            currentNode = currentNode.next;
        }

        return copy;
    }

    /// <summary>
    /// Converts the linked list to a string where nodes are added in order of linked list traversal.
    /// </summary>
    public override string ToString()
    {
        string r = "";
        IndexedLinkedListNode<Type> currentNode = nodes[0];

        for (int i = 0; i < currentIndex + 1; i++)
        {
            r += currentNode.ToString() + "\n";
            currentNode = currentNode.next;
        }
        return r;
    }
}

class IndexedLinkedListNode<Type>
{
    public Type value;
    public int index;
    public IndexedLinkedListNode<Type> prev;
    public IndexedLinkedListNode<Type> next;

    public IndexedLinkedListNode(Type value)
    {
        this.value = value;
    }

    public IndexedLinkedListNode<Type> Clone()
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return "Node { prev = " + prev.value + ", value = " + value + ", " + "next = " + next.value + " }";
    }
}
