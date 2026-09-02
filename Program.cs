namespace agent_framework_tests;

class Program
{
    static async Task Main(string[] args)
    {
        bool exit = false;

        while (!exit)
        {
            Console.Clear();
            Console.WriteLine("=== MAIN MENU ===");
            Console.WriteLine("1. First Agent");
            Console.WriteLine("2. Agent with tools");
            Console.WriteLine("3. TO DO...");
            Console.WriteLine("4. Exit");
            Console.Write("\nSelect an option (1-4): ");

            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await FirstAgent.RunAsync();
                    break;
                case "2":
                    await AgentWithTools.RunAsync();
                    break;
                case "3":
                    Console.WriteLine("\nto do...");
                    break;
                case "4":
                    exit = true;
                    Console.WriteLine("\nExiting...");
                    break;
                default:
                    Console.WriteLine("\nInvalid choice. Press any key to continue.");
                    break;
            }

            if (!exit)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.Read();
            }

        }
    }
}
