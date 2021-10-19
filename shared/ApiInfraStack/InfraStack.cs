using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.ECR;

namespace ApiInfraStack
{
    internal class InfraStack : Stack
    {
        internal InfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var stackProps = props as InfraStackProps;

            var imageTag = new CfnParameter(this, "ImageTag", new CfnParameterProps
            {                
                Type = "String",
                Default = "latest",
                Description = "The tag of the image that needs to be deployed to the lambda"
            });            

            var dockerImageCode = DockerImageCode.FromEcr(Repository.FromRepositoryName(
                this,
                stackProps.EcrRepoName,
                stackProps.EcrRepoName
            ), new EcrImageCodeProps { Tag = imageTag.ValueAsString });

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