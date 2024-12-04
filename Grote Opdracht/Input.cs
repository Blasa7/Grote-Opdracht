using System.IO;
using System.Numerics;
using System.Text;

class Input
{
    public static Order[] orders = new Order[1177];
    public static DistancesMatrix distancesMatrix;
    public static int orderCount = 1177;

    public static float GetTimeFromTo(int matrixID1, int matrixID2)
    {
        return distancesMatrix.matrix[matrixID1, matrixID2].time / 60f; //Converted to minutes for consistency
    }

    /// <summary>
    /// Parses and builds and Input instance from DistancesMatrix.txt and Orders.txt
    /// </summary>
    public static void Parse() //Changed all inputs to static because its probarly easiest
    {
        //Input input = new Input();

        Input.orders = new Order[1177];

        string line;
        using (StreamReader sr = new StreamReader(@"..\\..\\..\\Orders.txt"))
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

                Input.orders[i] = Order.Parse(split);

                line = sr.ReadLine();
                i++;
            }
        }

        Input.distancesMatrix = DistancesMatrix.Parse(@"..\\..\\..\\DistancesMatrix.txt");

        //return input;
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


    /// <summary>
    /// Parses a Order from the given string array. Expected format is the same as used in Orders.txt
    /// </summary>
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
            "Order { " +
            id.ToString() + ", " +
            location + ", " +
            frequency.ToString() + ", " +
            containerAmount.ToString() + ", " +
            containerVolume.ToString() + ", " +
            emptyingTime.ToString() + ", " +
            matrixID.ToString() + ", " +
            x.ToString() + ", " +
            y.ToString() +
            " }";       
    }
}

class DistancesMatrix
{
    /// <summary>
    /// Matrix where the first index corresponds to MatrixID1 and the second index to MatrixID2.
    /// </summary>
    public Distance[,] matrix;

    /// <summary>
    /// Parses a DistancesMatrix from the given path
    /// </summary>
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
    public int distance;
    public int time;

    public Distance(int distance, int time)
    {
        this.distance = distance;
        this.time = time;
    }

    public override string ToString()
    {
        return "Distance { " + distance.ToString() + ", " + time.ToString() + " }";
    }
}