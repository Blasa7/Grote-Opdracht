
class Delivery : IClonable<Delivery>
{
    public Address address;
    public int truck;
    public int day;
    public IndexedLinkedListNode<Delivery> scheduleNode;
    public IndexedLinkedListNode<Route> workDayNode;
    public IndexedLinkedListNode<Delivery> routeNode;
    public Delivery[] others;

    public Delivery()
    {

    }

    public Delivery(Address address)
    {
        this.address = address;
        this.others = new Delivery[address.frequency - 1];
    }

    public Delivery Clone()
    {
        Delivery copy = new Delivery();

        copy.address = address.Clone();

        return copy;
        // throw new NotImplementedException("This shouldn't be called");
    }

    public override string ToString()
    {
        return address.ToString();
    }
}

class Address : IClonable<Address>
{
    public string name;
    public int orderID;
    public int matrixID;
    public int garbageAmount; //Total amount of garbage to be picked up at this location
    public float emptyingTime;
    public int containerAmount;
    public int frequency;

    public Address()
    {

    }

    public Address(string s)
    {
        name = s;
        orderID = 0;
        matrixID = 0;
        garbageAmount = 0;
        emptyingTime = 0;
        containerAmount = 0;
        frequency = 1;
    }

    public Address(Order order)
    {
        name = order.location;
        orderID = order.id;
        matrixID = order.matrixID;
        garbageAmount = order.containerVolume * order.containerAmount;
        emptyingTime = order.emptyingTime;
        containerAmount = order.containerAmount;
        frequency = order.frequency;
    }

    public static Address Depot()
    {
        return new Address("Depot");
    }

    public static bool operator ==(Address a, Address b)
    {
        return a.matrixID == b.matrixID;
    }

    public static bool operator !=(Address a, Address b)
    {
        return a.matrixID != b.matrixID;
    }

    public Address Clone()
    {
        Address clone = new Address();

        clone.name = name;
        clone.orderID = orderID;
        clone.matrixID = matrixID;
        clone.garbageAmount = garbageAmount;
        clone.emptyingTime = emptyingTime;
        clone.frequency = frequency;

        return clone;
    }

    public override string ToString()
    {
        return name;
    }
}
