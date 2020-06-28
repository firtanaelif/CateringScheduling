using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace lastone
{
    class Program
    {
        public static int[] salvers = new int[3] { 0, 0, 0 };
        public static int[] inventory = new int[3] { 30, 15, 30 };
        public static int[] lowEat = new int[3] { 10, 10, 10 };
        public static readonly string[] salverNames = new string[3] { "Borek", "Cake", "Drink" };
        public static int allItems = 75;
        public static Dictionary<int, int[]> Visitors = new Dictionary<int, int[]>();
        public static bool ready = false;
        public static int allDone = 0;
        static readonly object[] locker = new object[3] { false, false, false };
        static readonly object lockAddList = new object();
        private static Random lockSleeper = new Random();
        private static Random lockChoicer = new Random();
        private static int[] _lockFlag = new int[3] { 0, 0, 0 };
        [ThreadStatic]
        static int visitorChoice;
        [ThreadStatic]
        static int[] ateItems;
        [ThreadStatic]
        static int[] maximumEat;
        [ThreadStatic]
        static int[] firstEat;
        [ThreadStatic]
        static int complete;

        static void Main(string[] args)
        {
            for (int i = 1; i <= 10; i++)
            {
                Visitors.Add(i, new int[3] { 0, 0, 0 });
                Thread t = new Thread(Program.Guest);
                t.Start(i);
            }
            for (int i = 0; i <= 2; i++)
            {
                salvers[i] += 5;
                inventory[i] -= 5;
            }
            Console.WriteLine("Waiter put the item on the salvers, program is ready...");
            Console.WriteLine("Program started");
            ready = true;
            int foodController = 0;
            int k = 0;
            while (k == 0)
            {
                if (salvers[foodController] <= 1)
                {
                    if (Interlocked.CompareExchange(ref _lockFlag[foodController], 1, 0) == 0)
                    {
                        if (inventory[foodController] > 0)
                        {
                            Monitor.Enter(locker[foodController]);
                            while (salvers[foodController] != 5 && inventory[foodController] > 0)
                            {
                                Interlocked.Increment(ref salvers[foodController]);
                                Interlocked.Decrement(ref inventory[foodController]);
                            }

                            Console.WriteLine("Waiter is putting the item on the " + salverNames[foodController] + " salver!");
                            Thread.Sleep(Sleep());
                            Console.WriteLine("Waiter put the item on the " + salverNames[foodController] + " inventory : " + inventory[foodController]);
                            Monitor.Exit(locker[foodController]);
                        }
                        if (inventory[0] + inventory[1] + inventory[2] == 0)
                        {
                            Console.WriteLine("Waiter completed his duty!");
                            k = 1;
                        }
                        Interlocked.Decrement(ref _lockFlag[foodController]);
                        Thread.Sleep(Sleep());
                    }
                }
                foodController++;
                if (foodController == 3)
                {
                    foodController = 0;
                }
            }
            while (true)
            {
                if (allDone == 10)
                {
                    foreach (var g in Visitors)
                    {
                        Console.WriteLine(g.Key + ". Guest => Borek : " + g.Value[0] + " Cake: " + g.Value[1] + " Drink: " + g.Value[2]);
                    }
                    Console.ReadKey();
                }
            }
        }
        public static void Guest(object j)
        {
            maximumEat = new int[3] { 5, 2, 5 };
            ateItems = new int[3] { 0, 0, 0 };
            firstEat = new int[3] { 1, 1, 1 };
            complete = 0;
            while (complete == 0)
            {
                if (ready)
                {
                    visitorChoice = RandomChoice();
                    if (allItems == 0)
                    {
                        Console.WriteLine(j + ".Guest has gone. All stuffs are eaten");
                        Interlocked.Increment(ref complete);
                    }
                    if (maximumEat[visitorChoice] > 0 && salvers[visitorChoice] + inventory[visitorChoice] + firstEat[visitorChoice] > lowEat[visitorChoice])
                    {
                        if (Interlocked.CompareExchange(ref _lockFlag[visitorChoice], 1, 0) == 0)
                        {
                            if (salvers[visitorChoice] > 0)
                            {
                                if (Interlocked.CompareExchange(ref firstEat[visitorChoice], 0, 1) == 1)
                                {
                                    Monitor.Enter(locker[visitorChoice]);

                                    Interlocked.Decrement(ref salvers[visitorChoice]);
                                    Interlocked.Decrement(ref maximumEat[visitorChoice]);
                                    Interlocked.Decrement(ref lowEat[visitorChoice]);
                                    Interlocked.Decrement(ref allItems);
                                    Interlocked.Increment(ref ateItems[visitorChoice]);
                                    Console.WriteLine(j + ". Guest took " + salverNames[visitorChoice] + " first time, ON SALVER : " + salvers[visitorChoice]);

                                    Interlocked.Decrement(ref _lockFlag[visitorChoice]);
                                    Thread.Sleep(Sleep());
                                    Monitor.Exit(locker[visitorChoice]);
                                    Thread.Sleep(Sleep());
                                }
                                else if (inventory[visitorChoice] + salvers[visitorChoice] > lowEat[visitorChoice])
                                {
                                    Monitor.Enter(locker[visitorChoice]);
                                    Interlocked.Decrement(ref salvers[visitorChoice]);
                                    Interlocked.Decrement(ref maximumEat[visitorChoice]);
                                    Interlocked.Decrement(ref allItems);
                                    Interlocked.Increment(ref ateItems[visitorChoice]);
                                    Console.WriteLine(j + ". Guest took " + salverNames[visitorChoice] + " ON SALVER : " + salvers[visitorChoice]);

                                    Interlocked.Decrement(ref _lockFlag[visitorChoice]);
                                    Thread.Sleep(Sleep());
                                    Monitor.Exit(locker[visitorChoice]);
                                    Thread.Sleep(Sleep());
                                }
                                Thread.Sleep(Sleep());
                            }
                            else
                            {

                                Interlocked.Decrement(ref _lockFlag[visitorChoice]);

                            }
                        }
                        else
                        {
                            Console.WriteLine(j + ". Guest try to take " + salverNames[visitorChoice] + " but could not");
                            Thread.Sleep(Sleep());
                        }
                    }
                    else if (maximumEat[0] + maximumEat[1] + maximumEat[2] == 0 || ((inventory[0] + salvers[0] <= lowEat[0]) && (inventory[1] + salvers[1] <= lowEat[1]) && (inventory[2] + salvers[2] <= lowEat[2]) && firstEat[0] == 0 && firstEat[1] == 0 && firstEat[2] == 0))
                    {
                        Console.WriteLine(j + ". Guest has no right to eat stuff");
                        Interlocked.Increment(ref complete);
                    }
                    else
                    {
                        Console.WriteLine(j + ". Guest has no right to take " + salverNames[visitorChoice]);
                    }
                }
            }
            lock (lockAddList)
            {
                Visitors[(int)j] = ateItems;
                allDone++;
            }
        }
        public static int Sleep()
        {
            lock (lockSleeper) return lockSleeper.Next(1000, 2000);
        }
        public static int RandomChoice()
        {
            lock (lockChoicer) return lockChoicer.Next(0, 3);
        }
    }
}
