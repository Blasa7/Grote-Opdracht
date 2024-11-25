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
    LinkedList<Delivery>[] deliveries = new LinkedList<Delivery>[5];

    /// <summary>
    /// Adds a one time delivery to the given route at random.
    /// </summary>
    void AddOneTimeDelivery(OneTimeDelivery delivery, Route route, Random rng, Judge judge)
    {
        int testimony = -delivery.address.emptyingTime * 3; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        route.AddRandomStop(delivery.address, rng, judge);

        if (judge.GetJudgement() == Judgement.Pass)
            deliveries[(int)route.weekDay].AddLast(delivery);
    }

    //Maybe add remove
    //Maybe add shuffle/swap
}

abstract class Delivery
{
    public Address address;
    public LinkedListNode<Delivery> node;

    public Delivery(Address address, LinkedListNode<Delivery> node)
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
    public IndexedLinkedList<Address> route;

    public WeekDay weekDay;
    
    int collectedGarbage = 0;
    int maximumGarbage;

    int duration;

    public Route(Address depot, int maximumSize)
    {
        route = new IndexedLinkedList<Address>(depot, maximumSize);
    }

    public void AddRandomStop(Address address, Random rng, Judge judge)
    {
        int index = route.getRandomIncluded(rng);

        int prevID = route.nodes[index].value.matrixID;
        int nextID = route.nodes[index].next.value.matrixID;

        int testimony = 
            Input.GetTimeFromTo(prevID, address.matrixID) + //New values are added
            Input.GetTimeFromTo(address.matrixID, nextID) - 
            Input.GetTimeFromTo(prevID, nextID); //Old value is substracted

        judge.Testify(testimony);

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
    public int emptyingTime;

    public Address(string s)
    {
        name = s;
    }

    public override string ToString()
    {
        return name;
    }
}

public enum WeekDay
{
    Monday = 0,
    Tuesday = 1,
    Wednesday = 2,
    Thursday = 3,
    Friday = 4
}