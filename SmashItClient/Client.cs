using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vgheri.SmashItEngine.core;
using vgheri.SmashItEngine.core.Events;
namespace vgheri.SmashItClient
{
    public class Client
    {
        static void Main(string[] args)
        {
            var users = 500;
            var address = @"http://showtime-eurogiciel.azurewebsites.net/";
            var testDuration = 90;
            var model = UserGrowthProgressionModel.Linear;
            var timeout = 3000;
            var pauseDuration = 1;
            var engine = new Engine(users, address, testDuration, model, pauseDuration, timeout);
            engine.TestProgressed += new Engine.TestProgressEventHandler(HandleProgressEvent);
            engine.TestCompleted += new Engine.TestCompletedEventHandler(HandleCompletedEvent);

            engine.AddStep(null, "GET", "", null, null, null)
                .AddStep(null, "GET", "en-US/Product/17/wh2014sh1-super-wash", null, null, null)
                .Run();

            Console.Read();
        }

        private static void HandleCompletedEvent(object sender, TestCompletedEventArgs args)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Simulation completed: ");
            builder.AppendLine("Total number of user: " + args.UsersSpawned.ToString());
            builder.AppendLine("Test duration: " + args.ActualTestDuration.ToString());
            builder.AppendLine("Max number of concurrent users: " + args.MaxNumberConcurrentUsers.ToString());
            builder.AppendLine("Average number of concurrent users: " + args.AverageConcurrentUsers.ToString());
            builder.AppendLine("Average response time: " + args.AverageResponseTime + " milliseconds.");
            builder.AppendLine("Total hits: " + args.Hits.ToString());
            builder.AppendLine("Total errors: " + args.Errors.ToString());
            builder.AppendLine("Total timeouts: " + args.Timeouts.ToString());
            Console.WriteLine(builder.ToString());
        }

        private static void HandleProgressEvent(object sender, TestProgressEventArgs args)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Simulation progress: ");
            builder.AppendLine("Time elapsed: " + args.TimeElapsed.ToString() + " seconds.");
            builder.AppendLine("Total number of user: " + args.UsersSpawned.ToString());
            builder.AppendLine("Average number of concurrent users: " + args.AverageConcurrentUsers.ToString());
            builder.AppendLine("Average response time: " + args.AverageResponseTime);
            builder.AppendLine("Total hits: " + args.Hits.ToString());
            builder.AppendLine("Total errors: " + args.Errors.ToString());
            builder.AppendLine("Total timeouts: " + args.Timeouts.ToString());
            Console.WriteLine(builder.ToString());
        }
    }
}
