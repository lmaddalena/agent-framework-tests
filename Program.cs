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
            Console.WriteLine("3. Session");
            Console.WriteLine("4. PlugIn");
            Console.WriteLine("5. Memory");
            Console.WriteLine("6. RAG");
            Console.WriteLine("7. Exit");
            Console.Write("\nSelect an option (1-7): ");

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
                    await Session.RunAsync();
                    break;
                case "4":
                    await Plugin.RunAsync();
                    break;
                case "5":
                    await Memory.RunAsync();
                    break;
                case "6":
                    await Rag.RunAsync();
                    break;
                case "7":
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
