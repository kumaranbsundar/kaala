using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ECR;
using Amazon.DynamoDBv2;

namespace TimesheetApiInfra
{
    public class TimesheetApiInfraStack : Stack
    {
        internal TimesheetApiInfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // var assets = new DockerImageAsset(this, "LambdaImage", new DockerImageAssetProps {
            //     Directory = "../src"
            // });

            // Deploy Lambda
            //var dockerImageCode = DockerImageCode.FromImageAsset("../src");
            // var dockerImageCode = DockerImageCode.FromEcr(Repository.FromRepositoryName(this
            //     , "ecr-image-repository", "timesheetapi"));
            var dockerImageCode = DockerImageCode.FromEcr(Repository.FromRepositoryArn(this
                , "timesheetapi-repo", "arn:aws:ecr:us-east-1:324668897075:repository/timesheetapi:latest"));            

            var lambda = new DockerImageFunction(this, "TimesheetApi", new DockerImageFunctionProps
            {
                FunctionName = "TimesheetApi",
                Code = dockerImageCode,
                Description = "Timesheet API",
                Timeout = Duration.Seconds(10),
            });        

            // DynamoDb Table
            var table = new Table(this, "Timesheet", new TableProps
            {
                TableName = "Timesheet",
                PartitionKey = new Attribute
                {
                    Name = "Id",
                    Type = AttributeType.STRING
                },
                SortKey = new Attribute
                {
                    Name = "Day",
                    Type = AttributeType.STRING
                }
            });

            // Assign permission to lambda to access the Timesheet Table
            lambda.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Actions = new[] { "dynamodb:*" },
                Resources = new[] { table.TableArn }
            }));
        }
    }
}