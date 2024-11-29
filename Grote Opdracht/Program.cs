class Program
{
    static void Main(string[] args)
    {
        // Input input = Input.Parse();
        
        WorkDay workDay = new WorkDay(new Address("start"), 1000);

        //insert d
        workDay.route.InsertAfter(new Address("a"), 0);
        //insert e
        workDay.route.InsertAfter(new Address("b"), 1);
        //a->e->d [a, d, e]
        workDay.route.InsertAfter(new Address("c"),2);
        workDay.route.InsertAfter(new Address("d"), 3);

        workDay.route.SwapNodes(2, 1);

        //[a, d, e, b, c]
        //a e d
        //d e a
        Console.WriteLine("\n" + "\n" + "\n");
        Console.WriteLine(workDay);
    }
}