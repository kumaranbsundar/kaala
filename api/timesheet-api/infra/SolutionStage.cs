using Amazon.CDK;

namespace TimesheetApiInfra
{
    public class SolutionStage : Stage
    {
        internal SolutionStage(Construct scope, string id, IStageProps props = null) : base(scope, id, props)
        {
            new TimesheetApiInfraStack(this, "TimesheetApi");
        }
    }
}