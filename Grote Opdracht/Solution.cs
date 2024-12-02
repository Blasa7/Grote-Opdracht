﻿using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

class Solution
{
    //Each truck has 5 routes, one per day.
    private WorkDay[][] solution = new WorkDay[2][] { new WorkDay[5], new WorkDay[5] };

    public float score; //The score in seconds

    public Solution()
    {
        
    }

    public Solution Clone()
    {
        Solution copy = new Solution();
        
        //TODO, keep in mind you need to clone all reference types (classes) to not use the same pointers

        return copy;
    }

    public void GenerateInitialSolution()
    {
        //WIP
        Address depot = new Address("depot");
        for (int i = 0; i < solution.Length; i++)
        {
            for (int j = 0; j < solution[i].Length; j++)
                solution[i][j] = new WorkDay(new Delivery(depot), 1000);
        }
    }
}

class Schedule
{
    /// <summary>
    /// 5 indexed linked lists track which deliveries are being made on each day.
    /// </summary>
    public IndexedLinkedList<Delivery>[] deliveries = new IndexedLinkedList<Delivery>[5];

    //2 trucks each 5 workdays
    public WorkDay[][] workDays = new WorkDay[2][] { new WorkDay[5], new WorkDay[5] };

    public IndexedLinkedList<Address> unfulfilledAddresses;

    public Schedule(Order[] orders)
    {
        unfulfilledAddresses = new IndexedLinkedList<Address>(new Address("Depot"), orders.Length + 1);
    }

    public void AddRandomDelivery(Random rng, Judge judge)
    {
        int index = unfulfilledAddresses.getRandomIncluded(rng);
        IndexedLinkedListNode<Address> node = unfulfilledAddresses.nodes[index];
        Address address = node.value;

        switch (address.frequency)
        {
            case 0:
                AddRandomOneTimeDelivery(address, rng, judge);
                break;
            case 1:
                AddRandomTwoTimeDelivery(address, rng, judge);
                break;
            case 2:
                AddRandomThreeTimeDelivery(address, rng, judge);
                break;
            case 3:
                AddRandomFourTimeDelivery(address, rng, judge);
                break;
        }

        if (judge.GetJudgement() == Judgement.Pass)
        {
            unfulfilledAddresses.RemoveNode(index);
        }
    }
    
    /// <summary>
    /// Adds a one time delivery to the a random truck at random.
    /// </summary>
    void AddRandomOneTimeDelivery(Address address, Random rng, Judge judge)
    {
        //First calculate variables
        Delivery delivery = new Delivery(address);

        int weekDay = rng.Next(0, 5);
        int truck = rng.Next(0, 2);

        //Second testify
        float testimony = -address.emptyingTime * 3; //Assumption is that a previously unfulfilled order is added

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
        //First calculate variables
        Delivery delivery1 = new Delivery(address);
        Delivery delivery2 = new Delivery(address);

        delivery1.others[0] = delivery2;
        delivery2.others[0] = delivery1;
        
        int timeSlot = rng.Next(0, 2); //Monday-Thursday or Tuesday-Friday
        int truck = rng.Next(0, 2);

        int day1 = 0 + timeSlot; //0 or 1
        int day2 = 3 + timeSlot; //3 or 4

        //Second testify
        float testimony = -address.emptyingTime * 3 * 2; //Assumption is that a previously unfulfilled order is added

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[truck][day1].AddRandomStop(delivery1, rng, judge);
        workDays[truck][day2].AddRandomStop(delivery2, rng, judge);    

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            delivery1.truck = truck;
            delivery2.truck = truck;

            delivery1.day = day1;
            delivery2.day = day2;

            deliveries[day1].InsertLast(delivery1);
            deliveries[day2].InsertLast(delivery2);
            //NOTE:
            //2+ Frequency Deliveries are put on the same truck here, but in therory could be on different ones 
        } 
    }

    void AddRandomThreeTimeDelivery(Address address, Random rng, Judge judge)
    {
        //First calculate variables
        Delivery delivery1 = new Delivery(address);
        Delivery delivery2 = new Delivery(address);
        Delivery delivery3 = new Delivery(address);

        delivery1.others[0] = delivery2;
        delivery1.others[1] = delivery3;

        delivery2.others[0] = delivery1;
        delivery2.others[1] = delivery3;

        delivery3.others[0] = delivery1;
        delivery3.others[1] = delivery2;

        int truck = rng.Next(0, 2);

        //Second testify
        float testimony = -address.emptyingTime * 3 * 3;

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[truck][0].AddRandomStop(delivery1, rng, judge);
        workDays[truck][2].AddRandomStop(delivery2, rng, judge);
        workDays[truck][4].AddRandomStop(delivery3, rng, judge);

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            delivery1.truck = truck;
            delivery2.truck = truck;
            delivery3.truck = truck;

            delivery1.day = 0;
            delivery2.day = 2;
            delivery3.day = 4;

            deliveries[0].InsertLast(delivery1);
            deliveries[2].InsertLast(delivery2);
            deliveries[4].InsertLast(delivery3);
        }
    }

    void AddRandomFourTimeDelivery(Address address, Random rng, Judge judge)
    {
        //First calculate variables
        Delivery delivery1 = new Delivery(address);
        Delivery delivery2 = new Delivery(address);
        Delivery delivery3 = new Delivery(address);
        Delivery delivery4 = new Delivery(address);

        delivery1.others[0] = delivery2;
        delivery1.others[1] = delivery3;
        delivery1.others[2] = delivery4;

        delivery2.others[0] = delivery1;
        delivery2.others[1] = delivery3;
        delivery2.others[2] = delivery4;

        delivery3.others[0] = delivery1;
        delivery3.others[1] = delivery2;
        delivery3.others[2] = delivery4;

        delivery4.others[0] = delivery1;
        delivery4.others[1] = delivery2;
        delivery4.others[2] = delivery3;

        int truck = rng.Next(0, 2);

        int skipDay = rng.Next(0, 5);
        int[] days = new int[4];

        for (int i = 0, day = 0; i < 4; i++, day++)
        {
            if (day == skipDay)
                day++;

            days[i] = day;
        }

        //Second testify
        float testimony = -address.emptyingTime * 3 * 4;

        judge.Testify(testimony);

        //Third call other functions that need to testify
        workDays[truck][days[0]].AddRandomStop(delivery1, rng, judge);
        workDays[truck][days[1]].AddRandomStop(delivery2, rng, judge);
        workDays[truck][days[2]].AddRandomStop(delivery3, rng, judge);
        workDays[truck][days[3]].AddRandomStop(delivery4 , rng, judge);

        //Fourth check judgement
        if (judge.GetJudgement() == Judgement.Pass)
        {
            delivery1.truck = truck;
            delivery2.truck = truck;
            delivery3.truck = truck;
            delivery4.truck = truck;

            delivery1.day = days[0];
            delivery2.day = days[1];
            delivery3.day = days[2];
            delivery4.day = days[3];

            deliveries[days[0]].InsertLast(delivery1);
            deliveries[days[1]].InsertLast(delivery2);
            deliveries[days[2]].InsertLast(delivery3);
            deliveries[days[3]].InsertLast(delivery4); 
        }
    }

    //Maybe add remove
    public void RemoveRandomDelivery(Random rng, Judge judge)
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

    public Schedule Clone()
    {
        throw new NotImplementedException();
    }
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

    public Delivery Clone()
    {
        throw new NotImplementedException();
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

    public WorkDay Clone()
    {
        throw new NotImplementedException();
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

    public Route Clone()
    {
        throw new NotImplementedException();
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

    public Address Clone()
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return name;
    }
}