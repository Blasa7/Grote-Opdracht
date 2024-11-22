using System.IO;

class Input
{
    public Order[] orders = new Order[1177];
    public DistancesMatrix distancesMatrix;

    public static Input Parse()
    {
        Input input = new Input();

        input.orders = new Order[1177];

        string line;
        using (StreamReader sr = new StreamReader(@"..\\..\\..\\order.txt"))
        {
            sr.ReadLine();

            line = sr.ReadLine();
            int i = 0;

            while (line != null)
            {
                string[] split = line.Split(";");
                for (int j = 0; j < split.Length; j++)
                {
                    split[j] = split[j].Trim();
                }

                input.orders[i] = Order.Parse(split);
                i++;

                line = sr.ReadLine();
            }
        }

        input.distancesMatrix = DistancesMatrix.Parse(@"..\\..\\..\\AfstandenMatrix.txt");

        return input;
    }

    public override string ToString()
    {
        return orders.ToString() + distancesMatrix.ToString();
    }
}

class Order
{
    public int id;
    public string location;
    public int frequency;
    public int containerAmount;
    public int containerVolume;
    public float emptyingTime;
    public int matrixID;
    public int x, y;



    public static Order Parse(string[] input)
    {
        Order order = new Order();
        order.id = int.Parse(input[0]);
        order.location = input[1];
        order.frequency = int.Parse(input[2][0].ToString());
        order.containerAmount = int.Parse(input[3]);
        order.containerVolume = int.Parse(input[4]);
        order.emptyingTime = float.Parse(input[5]);
        order.matrixID = int.Parse(input[6]);
        order.x = int.Parse(input[7]);
        order.y = int.Parse(input[8]);

        return order;
    }

    public override string ToString()
    {
        return 
            id.ToString() + " " +
            location + " " +
            frequency.ToString() + " " +
            containerAmount.ToString() + " " +
            containerVolume.ToString() + " " +
            emptyingTime.ToString() + " " +
            matrixID.ToString() + " " +
            x.ToString() + " " +
            y.ToString();
    }
}

class DistancesMatrix
{
    public Distance[,] matrix;

    public static DistancesMatrix Parse(string path)
    {
        DistancesMatrix distancesMatrix = new DistancesMatrix();

        distancesMatrix.matrix = new Distance[1099,1099];

        using (StreamReader sr = new StreamReader(path))
        {
            sr.ReadLine();

            string line = sr.ReadLine();

            while (line != null)
            {
                string[] split = line.Split(';');

                distancesMatrix.matrix[int.Parse(split[0]), int.Parse(split[1])] = new Distance(int.Parse(split[2]), int.Parse(split[3])); 

                line = sr.ReadLine();
            }
        }

        return distancesMatrix;
    }
}

class Distance
{
    int distance;
    int time;

    public Distance(int distance, int time)
    {
        this.distance = distance;
        this.time = time;
    }

    public override string ToString()
    {
        return distance.ToString() + " " + time.ToString();
    }
}