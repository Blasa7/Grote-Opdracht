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
            Truck1[i] = new Route(new Address("depot"), 1000);
        }
        for (int i = 0; i < Truck2.Length; i++)
        {
            Truck2[i] = new Route(new Address("depot"), 1000);
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
    IndexedLinkedList<Delivery>[] deliveries = new IndexedLinkedList<Delivery>[5];

    Route[][] routes = new Route[2][] { new Route[5], new Route[5] };
    
    /// <summary>
    /// Adds a one time delivery to the given route at random.
    /// </summary>
    void AddRandomOneTimeDelivery(OneTimeDelivery delivery, Random rng, Judge judge)
    {
        //First calculate variables
        int weekDay = rng.Next(0, 5);

        int truck = rng.Next(0, 2);

        //Second testify
        int testimony = -delivery.address.emptyingTime * 3; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        routes[truck][weekDay].AddRandomStop(delivery.address, rng, judge);

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
            deliveries[routes[truck][weekDay].weekDay].InsertLast(delivery);
    }

    //Maybe add remove

    //Maybe add shuffle/swap
}

abstract class Delivery
{
    public Address address;
    public IndexedLinkedListNode<Delivery> node;

    public Delivery(Address address, IndexedLinkedListNode<Delivery> node)
    {
        this.address = address;
        this.node = node;
    }
}

class OneTimeDelivery(Address address, LinkedListNode<Delivery> node) : Delivery(address, node) { }

class TwoTimeDeliery(Address address, LinkedListNode<Delivery> node) : Delivery(address, node)
{ 
    TwoTimeDeliery other;
}

class ThreeTimeDelivery(Address address, LinkedListNode<Delivery> node) : Delivery(address, node)
{
    ThreeTimeDelivery[] others = new ThreeTimeDelivery[2];
}

class FourTimeDelivery(Address address, LinkedListNode<Delivery> node) : Delivery (address, node) 
{
    FourTimeDelivery[] others = new FourTimeDelivery[3];
}

class Route // This is a linked list
{
    //In the future route will need to be a day schedule of a single truck.
    //So it will probarly either be a list of indexed linked lists or a linked list of indexed linked lists
    public IndexedLinkedList<Address> route; 

    public int weekDay; // 0, 1, 2, 3, 4
    
    int collectedGarbage = 0;
    int maximumGarbage;

    int duration;

    public Route(Address depot, int maximumSize)
    {
        route = new IndexedLinkedList<Address>(depot, maximumSize);
    }

    public void AddRandomStop(Address address, Random rng, Judge judge)
    {
        //First calculate variables
        int index = route.getRandomIncluded(rng);

        int prevID = route.nodes[index].value.matrixID;
        int nextID = route.nodes[index].next.value.matrixID;

        //Second testify
        int testimony = 
            Input.GetTimeFromTo(prevID, address.matrixID) + //New values are added
            Input.GetTimeFromTo(address.matrixID, nextID) - 
            Input.GetTimeFromTo(prevID, nextID); //Old value is substracted

        judge.Testify(testimony);

        //Third call other functions that need to testify

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
            route.InsertAfter(address, index);
    }

    public override string ToString()
    {
        return route.ToString();
    }
}

class Address
{
    public string name;
    public int matrixID;
    public int volumePerContainer;
    public int containerAmount;
    public int emptyingTime;

    public Address(string s)
    {
        name = s;
    }

    public static bool operator == (Address a, Address b)
    {
        return a.matrixID == b.matrixID;
    }

    public static bool operator != (Address a, Address b)
    {
        return a.matrixID != b.matrixID;
    }

    public override string ToString()
    {
        return name;
    }
}