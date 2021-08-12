using Amazon.CDK;

namespace TimesheetApiInfra
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            
            new EcrStack(app, "TimesheetApiImageRepo");
            new TimesheetApiInfraStack(app, "TimesheetApi");
            new PipelineStack(app, "TimesheetApiPipeline");

            app.Synth();
        }
    }
}
