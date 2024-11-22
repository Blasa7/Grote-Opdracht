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
        route.InsertNode(1, 0, 0);
        //insert e
        route.InsertNode(2, 1, 0);
        //a->e->d [a, d, e]
        route.SwapNodes(1,1);

        //[a, d, e, b, c]
        //a e d
        //d e a
        Console.WriteLine("\n" + "\n" + "\n");
        Console.WriteLine(route);
    }
}