class Program
{
    static void Main(string[] args)
    {
        // Input input = Input.Parse();
        
        Route route = new Route(new Adress("start"), 1000);

        //insert d
        route.InsertAfter(new Adress("a"), 0);
        //insert e
        route.InsertAfter(new Adress("b"), 1);
        //a->e->d [a, d, e]
        route.InsertAfter(new Adress("c"),2);
        route.InsertAfter(new Adress("d"), 3);

        route.SwapNodes(2, 1);

        //[a, d, e, b, c]
        //a e d
        //d e a
        Console.WriteLine("\n" + "\n" + "\n");
        Console.WriteLine(route);
    }
}