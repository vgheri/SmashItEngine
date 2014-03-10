using System;
using System.Text;

namespace vgheri.SmashItEngine.core.Events
{
    public class TestProgressEventArgs : EventArgs
    {
        public double TimeElapsed { get; set; }
        public int UsersSpawned { get; set; }
        public double AverageConcurrentUsers { get; set; }
        public int Hits { get; set; }
        public int Errors { get; set; }
        public int Timeouts { get; set; }
        public double AverageResponseTime { get; private set; }

        public TestProgressEventArgs(double elapsed, int userSpawned, double averageConcurrentUsers,
            int hits, int errors, int timeouts, double averageResponseTime)
        {
            this.TimeElapsed = elapsed;
            this.UsersSpawned = userSpawned;
            this.AverageConcurrentUsers = averageConcurrentUsers;
            this.Hits = hits;
            this.Errors = errors;
            this.Timeouts = timeouts;
            this.AverageResponseTime = averageResponseTime;
        }
    }
}
