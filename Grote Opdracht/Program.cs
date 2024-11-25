class Program
{
    static void Main(string[] args)
    {
        // Input input = Input.Parse();
        
        Route route = new Route();
        route.nodes = new LocationNode[5]
        {
            new LocationNode("a"),
            new LocationNode("b"),
            new LocationNode("c"),
            new LocationNode("d"),
            new LocationNode("e")
        };

        //first node point to itself
        route.currentIndex = 0;
        route.nodes[0].prev = route.nodes[0];
        route.nodes[0].next = route.nodes[0];

        //insert d
        route.InsertAfter(1, 0);
        //insert e
        route.InsertAfter(2, 1);
        //a->e->d [a, d, e]
        route.InsertAfter(3,2);
        route.InsertAfter(4, 3);

        route.SwapNodes(2, 1);

        //[a, d, e, b, c]
        //a e d
        //d e a
        Console.WriteLine("\n" + "\n" + "\n");
        Console.WriteLine(route);
    }
}