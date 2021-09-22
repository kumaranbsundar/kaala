using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.ECR;
using TimesheetApiInfra.Props;

namespace TimesheetApiInfra
{

    public class InfraStack : Stack
    {
        internal InfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // var assets = new DockerImageAsset(this, "LambdaImage", new DockerImageAssetProps {
            //     Directory = "../src"
            // });

            // Deploy Lambda
            //var dockerImageCode = DockerImageCode.FromImageAsset("../src");
            // var dockerImageCode = DockerImageCode.FromEcr(Repository.FromRepositoryName(this
            //     , "ecr-image-repository", "timesheetapi"));

            var stackProps = props as InfraStackProps;

            var dockerImageCode = DockerImageCode.FromEcr(Repository.FromRepositoryName(
                this,
                stackProps.EcrRepoName,
                stackProps.EcrRepoName
            ), new EcrImageCodeProps { Tag = "latest" });

            //  $"arn:aws:ecr:{this.Region}:{stackProps.AccountId}:repository/{stackProps.EcrRepoName}"),
            // new EcrImageCodeProps { Tag = "latest" });

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