using Amazon.CDK;

namespace TimesheetApiInfra
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new TimesheetApiInfraStack(app, "TimesheetApi");

            app.Synth();
        }
    }
}
