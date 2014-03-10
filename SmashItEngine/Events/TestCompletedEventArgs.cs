using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace vgheri.SmashItEngine.core.Events
{
    public class TestCompletedEventArgs : EventArgs
    {
        public int UsersSpawned { get; set; }
        public double ActualTestDuration { get; set; }
        public int MaxNumberConcurrentUsers { get; set; }
        public double AverageConcurrentUsers { get; set; }        
        //public Uri BaseAddress { get; private set; }
        //public int Duration { get; private set; }
        //public int PauseDuration { get; private set; }
        //public UserGrowthProgressionModel UserGrowthProgressionModel { get; private set; }
        //public List<HttpRequestMessage> Steps { get; private set; }
        public double AverageResponseTime { get; private set; }
        public int Hits { get; set; }
        public int Errors { get; set; }
        public int Timeouts { get; set; }

        public TestCompletedEventArgs(int usersSpawned, double actualTestDuration, int maxConcurrentUsers, double averageConcurrentUsers,
            double averageResponseTime, int hits, int errors, int timeouts)
        {
            this.UsersSpawned = usersSpawned;
            this.ActualTestDuration = actualTestDuration;
            this.MaxNumberConcurrentUsers = maxConcurrentUsers;
            this.AverageConcurrentUsers = averageConcurrentUsers;
            //this.BaseAddress = address;
            //this.Duration = duration;
            //this.PauseDuration = pauseDuration;
            //this.UserGrowthProgressionModel = model;
            //this.Steps = steps;
            this.AverageResponseTime = averageResponseTime;
            this.Hits = hits;
            this.Errors = errors;
            this.Timeouts = timeouts;
        }
    }
}
