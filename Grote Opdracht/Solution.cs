
class Solution
{
    //Each truck has 5 routes, one per day.
    private WorkDay[][] solution = new WorkDay[2][] { new WorkDay[5], new WorkDay[5] };

    public int score; //The score in seconds

    public Solution()
    {
        for (int i = 0; i < Input.orderCount; i++)
        {
            score += Input.orders[i].emptyingTime * Input.orders[i].frequency * 3;
        }
    }

    public void UpdateSolution(Schedule schedule, int score)
    {
        this.score = score;

        WorkDay[][] copy = new WorkDay[2][] { new WorkDay[5], new WorkDay[5] };
        for (int i = 0; i < copy.Length; i++) // foreach truck
        {
            for (int j = 0; j < copy[i].Length; j++) // foreach workday
                copy[i][j] = schedule.workDays[i][j].Clone();
        }

        solution = copy;
    }

    /// <summary>
    /// Prints the solution as specified in 'Format-invoer-checker.docx' 
    /// (Truck;Day;Address.no;AddressID)
    /// </summary>
    public string PrintSolution(bool write)
    {
        using (StreamWriter sw = new StreamWriter(@"..\\..\\..\\solution.txt"))
        {
            int truck, day;
            Tuple<string, string>[] addresses;
            int startAddressNumber = 1;

            for (int i = 0; i < solution.Length; i++) // foreach truck
            {
                truck = i + 1; // 0,1 -> 1,2
                for (int j = 0; j < solution[i].Length; j++) // foreach workday
                {
                    startAddressNumber = 1;

                    WorkDay w = solution[i][j];
                    day = w.weekDay + 1;

                    int currentIndex = w.workDay.currentIndex;
                    IndexedLinkedListNode<Route> currentNode = w.workDay.nodes[0];
                    for (int k = 0; k < currentIndex + 1; k++) // foreach route
                    {
                        if (currentNode.value.route.currentIndex == 0)
                        {
                            currentNode = currentNode.next;
                            continue;
                        }

                        addresses = currentNode.value.GetAddresses(startAddressNumber);
                        currentNode = currentNode.next;

                        foreach (Tuple<string, string> address in addresses)
                        {
                            if (write)
                            {
                                sw.WriteLine($"{truck};{day};{address.Item1};{address.Item2}");
                            }
                            else
                            {
                                Console.WriteLine($"{truck};{day};{address.Item1};{address.Item2}");
                            }
                            startAddressNumber++;
                        }

                    }

                }
            }
        }
        return "";
    }
}