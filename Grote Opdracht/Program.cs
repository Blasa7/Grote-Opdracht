class Program
{
    static void Main(string[] args)
    {
        // Input input = Input.Parse();
        
        Route route = new Route(new Address("start"), 1000);

        //insert d
        route.route.InsertAfter(new Address("a"), 0);
        //insert e
        route.route.InsertAfter(new Address("b"), 1);
        //a->e->d [a, d, e]
        route.route.InsertAfter(new Address("c"),2);
        route.route.InsertAfter(new Address("d"), 3);

        route.route.SwapNodes(2, 1);

        //[a, d, e, b, c]
        //a e d
        //d e a
        Console.WriteLine("\n" + "\n" + "\n");
        Console.WriteLine(route);
    }
}