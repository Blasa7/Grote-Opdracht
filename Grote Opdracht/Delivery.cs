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
