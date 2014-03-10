using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Timers;
using vgheri.SmashItEngine.core.Events;

namespace vgheri.SmashItEngine.core
{
    public class Engine : IEngine
    {
        #region Events
        public delegate void TestProgressEventHandler(object sender, TestProgressEventArgs args);
        public event TestProgressEventHandler TestProgressed;

        // Invoke the Changed event; called whenever list changes
        protected virtual void OnTestProgressed(TestProgressEventArgs e)
        {
            if (TestProgressed != null)
                TestProgressed(this, e);
        }

        public delegate void TestCompletedEventHandler(object sender, TestCompletedEventArgs args);
        public event TestCompletedEventHandler TestCompleted;

        // Invoke the Changed event; called whenever list changes
        protected virtual void OnTestCompleted(TestCompletedEventArgs e)
        {
            if (TestCompleted != null)
                TestCompleted(this, e);
        }

        #endregion

        private Scenario scenario;
        private int userSpawned;
        private int currentNumberOfConcurrentUsers;
        private Stopwatch testTimeElapsed;
        private int hits;
        private int errors;
        private int timeouts;
        private List<HttpActionResult> executionResults;
        private double progressUpdateFrequency;

        // Timers
        private Timer spawnTimer;
        private Timer testProgressTimer;
        private Timer testCompletedTimer;

        Func<HttpRequestMessage, HttpRequestMessage> Factory;

        public Engine(int totalUsers, string targetAddress, int testDuration,
            UserGrowthProgressionModel userGrowthProgressionModel, int pauseDuration, double timeout)
        {
            try
            {
                this.scenario = new Scenario(totalUsers, targetAddress, testDuration, userGrowthProgressionModel, timeout, pauseDuration);
                this.Factory = this.scenario.CreateStep;
            }
            catch (ArgumentException exception)
            {
                // TODO Log it
                throw;
            }

            // Init params
            this.testTimeElapsed = new Stopwatch();
            this.executionResults = new List<HttpActionResult>();
            this.progressUpdateFrequency = 5000; // Send an update out to the client every x seconds
            InitialiseTimers();            
        }

        private void InitialiseTimers()
        {   
            this.spawnTimer = new Timer(ComputeUserSpawnFrequency());
            spawnTimer.AutoReset = true;
            spawnTimer.Elapsed += new ElapsedEventHandler(HandleSpawnTimerElapsed);

            this.testProgressTimer = new Timer(this.progressUpdateFrequency);
            testProgressTimer.AutoReset = true;
            testProgressTimer.Elapsed += new ElapsedEventHandler(HandleTestProgressTimerElapsed);

            this.testCompletedTimer = new Timer(this.scenario.Duration * 1000);
            testCompletedTimer.AutoReset = false;
            testCompletedTimer.Elapsed += new ElapsedEventHandler(HandleTestCompletedTimerElapsed);
        }

        public IEngine AddStep(Dictionary<string, string> headers, string verb, string endpoint, HttpRequestContentType? contentType, 
            string bodyContent, string mimeType = "application/json")
        {
            this.scenario.AddStep(headers, verb, endpoint, contentType, bodyContent, mimeType);
            return this;
        }
                
        public void Run()
        {
            if (this.scenario == null)
            {
                throw new ApplicationException("Error: create a test scenario before running the test.");
            }
            // Start the stopwatch
            this.testTimeElapsed.Start();
            // Start the timers
            this.spawnTimer.Start();
            this.testProgressTimer.Start();
            this.testCompletedTimer.Start();
        }

        private void HandleTestProgressTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Create the TestProgressEventArgs based on the ExecutionResults
            var eventArgs = CreateTestProgressEventArgs(this.executionResults);            
            // Raise TestProgress event
            this.OnTestProgressed(eventArgs);
        }

        private void HandleTestCompletedTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Stop the stopwatch
            this.testTimeElapsed.Stop();
            // Stop the timers           
            this.spawnTimer.Stop();
            this.spawnTimer.Close();
            this.testProgressTimer.Stop();
            this.testProgressTimer.Close();
            var eventArgs = CreateTestCompletedEventArgs(this.executionResults);
            this.OnTestCompleted(eventArgs);
        }        

        async void HandleSpawnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            await ExecuteScenario();
        }

        private async Task ExecuteScenario()
        {
            IncreaseTotalUsersCounter();
            IncreaseConcurrentUsersCounter();
            // Execute the scenario
            foreach (var step in scenario.Steps)
            {
                var result = await ExecuteAction(Factory(step));
                this.executionResults.Add(result);
                var pauseDone = await Pause();
            }
            DecreaseConcurrentUsersCounter();
        }

        /// <summary>
        /// Returns the frequency in milliseconds
        /// </summary>
        /// <returns></returns>
        private double ComputeUserSpawnFrequency()
        {
            double frequency = 0;            
            if (this.scenario.UserGrowthProgressionModel == UserGrowthProgressionModel.Linear)
            {
                
                frequency = (((double)this.scenario.Duration) / ((double)this.scenario.Users)) * 1000;                
            }
            else
            {
                // TODO implement other strategies
                throw new NotImplementedException("Only linear growth is implemented at the moment.");
            }
            return frequency;
        }

        private async Task<HttpActionResult> ExecuteAction(HttpRequestMessage step)
        {
            HttpActionResult result = null;
            HttpResponseMessage response = null;
            HttpClient httpClient = new HttpClient();
            Stopwatch watch = new Stopwatch();
            try 
            {
                httpClient.BaseAddress = this.scenario.BaseAddress;
                httpClient.Timeout = TimeSpan.FromMilliseconds(this.scenario.Timeout);
                watch.Start();
                response = await httpClient.SendAsync(step, HttpCompletionOption.ResponseContentRead);                
                watch.Stop();
                if (!response.IsSuccessStatusCode)
                {
                    this.errors++;
                }
                // Log 
                result = new HttpActionResult()
                {
                    RequestTimedOut = false,
                    ResponseMessage = response,
                    ResponseTime = watch.ElapsedMilliseconds,
                    ConcurrentUsers = this.currentNumberOfConcurrentUsers
                };
            }
            catch (TaskCanceledException ex)
            {
                watch.Stop();
                this.timeouts++;
                result = new HttpActionResult()
                {
                    RequestTimedOut = true,
                    ResponseMessage = null,
                    ResponseTime = watch.ElapsedMilliseconds,
                    ConcurrentUsers = this.currentNumberOfConcurrentUsers
                };
            }
            finally 
            {         
                this.hits++;                
            }
            
            return result;
        }

        private async Task<bool> Pause()
        {
            Task<bool> t = Task.Factory.StartNew(() =>
            {
                System.Threading.Thread.Sleep(this.scenario.PauseDuration * 1000);
                return true;
            });            
            bool ended = await t;
            return ended;
        }

        private void IncreaseTotalUsersCounter()
        {
            this.userSpawned++;
        }

        private void IncreaseConcurrentUsersCounter()
        {
            this.currentNumberOfConcurrentUsers++;
        }

        private void DecreaseConcurrentUsersCounter()
        {
            this.currentNumberOfConcurrentUsers--;
        }

        private TestProgressEventArgs CreateTestProgressEventArgs(List<HttpActionResult> results)
        {            
            var timeElapsed = this.testTimeElapsed.Elapsed.TotalSeconds;
            var users = this.userSpawned;
            var averageConcurrentUsers = results.Select(r => r.ConcurrentUsers).Average();
            var hits = this.hits;
            var errors = this.errors;
            var timeouts = this.timeouts;
            var averageResponseTime = 0.0;
            if (results != null && results.Count > 0)
            {
                averageResponseTime = results.Where(r => r.RequestTimedOut == false).Select(r => r.ResponseTime).Average();
            }            
            return new TestProgressEventArgs(timeElapsed, users, averageConcurrentUsers, hits, errors, timeouts, averageResponseTime);
        }

        private TestCompletedEventArgs CreateTestCompletedEventArgs(List<HttpActionResult> results)
        {         
            var users = this.userSpawned;
            var actualTestDuration = this.testTimeElapsed.Elapsed.TotalSeconds;
            var maxConcurrentUsers = this.executionResults.Select(r => r.ConcurrentUsers).Max();
            var averageConcurrentUsers = results.Select(r => r.ConcurrentUsers).Average();
            var averageResponseTime = results.Where(r => r.RequestTimedOut == false).Select(r => r.ResponseTime).Average();
            return new TestCompletedEventArgs(users, actualTestDuration, maxConcurrentUsers,
                averageConcurrentUsers, averageResponseTime, this.hits, this.errors, this.timeouts);
        }
    }
}
