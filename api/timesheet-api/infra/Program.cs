using System.Collections.Generic;
using Amazon.CDK;
using ApiInfraStack;

namespace TimesheetApiInfra
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            var apiStack = new ApiStack();
            apiStack.Initialize("Timesheet", new List<DeploymentEnvironment> {
                    new DeploymentEnvironment {AccountId = "324668897075", EnvironmentName = "dev"}
                }, app);

            app.Synth();
        }
    }
}