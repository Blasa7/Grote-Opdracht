using System.Security.Cryptography.X509Certificates;

class Solution
{
    //Each truck has 5 routes, one per day.
    private WorkDay[] Truck1 = new WorkDay[5];
    private WorkDay[] Truck2 = new WorkDay[5];

    public int score; //The score in seconds

    public Solution()
    {
        
    }

    public static Solution Copy(Solution current)
    {
        Solution copy = new Solution();
        copy.score = current.score;
        copy.Truck1 = current.Truck1;
        copy.Truck2 = current.Truck2;
        
        return copy;
    }

    public void GenerateInitialSolution()
    {
        //WIP
        Address depot = new Address("depot");
        for (int i = 0; i < Truck1.Length; i++)
        {
            Truck1[i] = new WorkDay(new Delivery(depot), 1000);
        }
        for (int i = 0; i < Truck2.Length; i++)
        {
            Truck2[i] = new WorkDay(new Delivery(depot), 1000);
        }
    }
    public int CalculateDifference(WorkDay workDay)
    {
        return 0;
    }

    public int CalculateScore(WorkDay workDay, int oldScore)
    {
        int difference = CalculateDifference(workDay);
        return oldScore + difference;
    }
}

class Schedule
{
    IndexedLinkedList<Delivery>[] deliveries = new IndexedLinkedList<Delivery>[5];

    //5 work days for 2 trucks
    WorkDay[][] workDays = new WorkDay[2][] { new WorkDay[5], new WorkDay[5] };
    
    /// <summary>
    /// Adds a one time delivery to the given route at random.
    /// </summary>
    void AddRandomOneTimeDelivery(Address address, Random rng, Judge judge)
    {
        //First calculate variables
        Delivery delivery = new Delivery(address);

        int weekDay = rng.Next(0, 5);
        int truck = rng.Next(0, 2);

        //Second testify
        float testimony = -delivery.address.emptyingTime * 3; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[truck][weekDay].AddRandomStop(delivery, rng, judge);
        
        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            delivery.truck = truck;
            delivery.day = weekDay;
            deliveries[workDays[truck][weekDay].weekDay].InsertLast(delivery);
        }
    }

    void AddRandomTwoTimeDelivery(Address address, Random rng, Judge judge)
    {
        Delivery delivery = new Delivery(address);
        Delivery otherDelivery = new Delivery(address);

        delivery.others[0] = otherDelivery;
        otherDelivery.others[0] = delivery;
        
        int timeSlot = rng.Next(0, 2); //Monday-Thursday or Tuesday-Friday
        int truck = rng.Next(0, 2);

        //Second testify
        float testimony = -delivery.address.emptyingTime * 3 * 3; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        if (timeSlot == 0)
        {
            workDays[truck][0].AddRandomStop(delivery, rng, judge);
            workDays[truck][3].AddRandomStop(delivery, rng, judge);
        }
        else
        {
            workDays[truck][1].AddRandomStop(delivery, rng, judge);
            workDays[truck][4].AddRandomStop(delivery, rng, judge);
        }       

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            delivery.truck = truck;
            otherDelivery.truck = truck;
            
            if (timeSlot == 0)
            {   
                //NOTE:
                //2+ Frequency Deliveries are put on the same truck here, but in therory could be on different ones 
                
                delivery.day = 0;
                otherDelivery.day = 3;

                deliveries[0].InsertLast(delivery);
                deliveries[3].InsertLast(delivery);
            }
            else
            {
                delivery.day = 1;
                otherDelivery.day = 4;
                
                deliveries[1].InsertLast(delivery);
                deliveries[4].InsertLast(delivery);
            }
        } 
    }

    //Maybe add remove
    void RemoveRandomDelivery(Random rng, Judge judge)
    {
        //First calculate variables
        int weekDay = rng.Next(0, 5);
        int index = deliveries[weekDay].getRandomIncluded(rng);
        
        Delivery delivery = deliveries[weekDay].nodes[index].value; //Get a random delivery

        //We need to remove all the stops in all the routes in all the workdays foreach truck as well
        workDays[delivery.truck][delivery.day].RemoveStop(delivery, rng , judge);

        for (int i = 0; i < delivery.others.Length; i++)
        {
            workDays[delivery.others[i].truck][delivery.others[i].day].RemoveStop(delivery.others[i], rng , judge);
        }
        

        //Third call other functions that need to testify
        //workDays[truck][weekDay].RemoveStop(delivery.address, rng, judge);
    }

    //Maybe add shuffle/swap
}

class Delivery
{
    public Address address;
    public int truck;
    public int day;
    public IndexedLinkedListNode<Route> workDayNode;
    public IndexedLinkedListNode<Delivery> routeNode;
    public Delivery[] others;

    public Delivery(Address address)
    {
        this.address = address;
        this.others = new Delivery[address.frequency-1];
    }
} 

class WorkDay // This is a linked list
{
    public IndexedLinkedList<Route> workDay; 

    public int weekDay; // 0, 1, 2, 3, 4

    public WorkDay(Delivery depot, int maximumSize)
    {
        Route start = new Route(depot, maximumSize);
        workDay = new IndexedLinkedList<Route>(start, 10); //Hard coded size maybe need to be larger
    }

    public void AddRandomStop(Delivery delivery, Random rng, Judge judge)
    {
        int index = workDay.getRandomIncluded(rng);

        delivery.workDayNode = workDay.nodes[index];
        workDay.nodes[index].value.AddRandomStop(delivery, rng, judge);
    }
    
    public void RemoveStop(Delivery delivery, Random rng, Judge judge)
    {
        int index = delivery.workDayNode.index;

        workDay.nodes[index].value.RemoveStop(delivery, judge);
    }

    public override string ToString()
    {
        return workDay.ToString();
    }
}

class Route
{
    public IndexedLinkedList<Delivery> route;
    
    int collectedGarbage = 0;
    int maximumGarbage;

    int duration;

    public Route(Delivery depot, int maximumSize)
    {
        route = new IndexedLinkedList<Delivery>(depot, maximumSize);
    }

    public void AddRandomStop(Delivery delivery, Random rng, Judge judge)
    {
        //First calculate variables
        int index = route.getRandomIncluded(rng);

        Address address = delivery.address;
        int prevID = route.nodes[index].value.address.matrixID;
        int nextID = route.nodes[index].next.value.address.matrixID;

        //Second testify
        int testimony = 
            Input.GetTimeFromTo(prevID, address.matrixID) + //New values are added
            Input.GetTimeFromTo(address.matrixID, nextID) - 
            Input.GetTimeFromTo(prevID, nextID); //Old value is substracted

        judge.Testify(testimony);

        //Third call other functions that need to testify

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            route.InsertAfter(delivery, index);
            delivery.routeNode = route.nodes[route.currentIndex];
        }
    }

    public void RemoveStop(Delivery delivery, Judge judge)
    {
        //First calculate variables
        int index = delivery.routeNode.index;

        Address address = delivery.address;
        int prevID = route.nodes[index].value.address.matrixID;
        int nextID = route.nodes[index].next.value.address.matrixID;

        //Second testify
        int testimony =
            Input.GetTimeFromTo(prevID, nextID) - //New value
            (Input.GetTimeFromTo(prevID, address.matrixID) + //Old values are substracted
            Input.GetTimeFromTo(address.matrixID, nextID)); 

        judge.Testify(testimony);

        //Third call other functions that need to testify

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
            route.RemoveNode(index);
    }
}

class Address
{
    public string name;
    public int matrixID;
    public int volumePerContainer;
    public int containerAmount;
    public float emptyingTime;
    public int frequency;

    public Address(string s)
    {
        name = s;
        matrixID = 0;
        volumePerContainer = 0;
        containerAmount = 0;
        emptyingTime = 0;
    }

    public Address(Order order)
    {
        name = order.location;
        matrixID = order.matrixID;
        volumePerContainer = order.containerVolume;
        containerAmount = order.containerAmount;
        emptyingTime = order.emptyingTime;
        frequency = order.frequency;
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