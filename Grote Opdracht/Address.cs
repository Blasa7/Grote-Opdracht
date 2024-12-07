class Address : IClonable<Address>
{
    public string name;
    public int orderID;
    public int matrixID;
    public int garbageAmount; //Total amount of garbage to be picked up at this location
    public int emptyingTime;
    public int containerAmount;
    public int frequency;

    public Address()
    {

    }

    public Address(string s)
    {
        name = s;
        orderID = 0;
        matrixID = 287;
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