using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.DynamoDB;

namespace TimesheetApiInfra
{
    public class TimesheetApiInfraStack : Stack
    {
        internal TimesheetApiInfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Deploy Lambda
            var dockerImageCode = DockerImageCode.FromImageAsset("../src");
            var lambda = new DockerImageFunction(this, "TimesheetApi", new DockerImageFunctionProps
            {
                FunctionName = "TimesheetApi",
                Code = dockerImageCode,
                Description = "Timesheet API",
                Timeout = Duration.Seconds(10)
            });

            // DynamoDb Table
            var table = new Table(this, "Timesheet", new TableProps
            {
                TableName = "Timesheet",
                PartitionKey = new Attribute
                {
                    Name = "Id",
                    Type = AttributeType.STRING
                }
            });
        }
    }
}
