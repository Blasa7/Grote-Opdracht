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
        if (address.orderID == 0)
            return new Delivery(Address.Depot());

        Delivery copy = new Delivery();

        copy.address = address.Clone();
        copy.truck = truck;
        copy.day = day;

        return copy;
    }

    public override string ToString()
    {
        return address.ToString();
    }
}
