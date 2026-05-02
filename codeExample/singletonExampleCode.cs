using System;
using System.Collections.Generic;

namespace DoFactory.GangOfFour.Singleton.NETOptimized
{
    /// <summary>
    /// MainApp startup class for .NET optimized 
    /// Singleton Design Pattern.
    /// </summary>
    class MainApp
    {
        /// <summary>
        /// Entry point into console application.
        /// </summary>
        static void Main()
        {
            LoadBalancer b1 = LoadBalancer.GetLoadBalancer();
            LoadBalancer b2 = LoadBalancer.GetLoadBalancer();
            LoadBalancer b3 = LoadBalancer.GetLoadBalancer();
            LoadBalancer b4 = LoadBalancer.GetLoadBalancer();

            // Confirm these are the same instance
            if (b1 == b2 && b2 == b3 && b3 == b4)
            {
                Console.WriteLine("Same instance\n");
            }

            // Load balance 15 requests for a server
            LoadBalancer lb = LoadBalancer.GetLoadBalancer();
            for (int i = 0; i < 15; i++)
            {
                Console.WriteLine(lb.Server.Name);
            }

            // Wait for user
            Console.Read();        
        }
    }
    
    // Singleton

    sealed class LoadBalancer
    { 
        // Static members are 'eagerly initialized', that is, 
        // immediately when class is loaded for the first time.
        // .NET guarantees thread safety for static initialization
        private static readonly LoadBalancer instance = 
            new LoadBalancer(); 

        // Type-safe generic list of servers
        private List<Server> servers = new List<Server>();
        private Random random = new Random();

        // Note: constructor is 'private'
        private LoadBalancer() 
        {
            // List of available servers
            servers.Add(new Server("ServerI","120.14.220.18"));
            servers.Add(new Server("ServerII","120.14.220.19"));
            servers.Add(new Server("ServerIII","120.14.220.20"));
            servers.Add(new Server("ServerIV","120.14.220.21"));
            servers.Add(new Server("ServerV","120.14.220.22"));
        } 

        public static LoadBalancer GetLoadBalancer()
        {
            return instance;
        }
        
        // Simple, but effective load balancer

        public Server Server
        {
            get
            {
                int r = random.Next(servers.Count);
                return servers[r];
            }
        }
    }

    // Simple server machine
    class Server
    {
        private string name;
        private string ip;
        public Server(string name, string ip)
        {
            this.name = name;
            this.ip = ip;
        }
        public string Name
        {
            get { return name; }
        }
        public string IP
        {
            get { return ip; }
        }
    }
}
