using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ECR;
using System.Collections.Generic;

namespace TimesheetApiInfra
{
    public class EcrStack : Stack
    {        
        internal EcrStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var ecrRepo = new Repository(this, "TimesheetApiRepo", new RepositoryProps
            {
                RepositoryName = "timesheetapi"
            });

            ecrRepo.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Principals = new[] { new AnyPrincipal() },
                Actions = new[] {
                    "ecr:BatchCheckLayerAvailability",
                    "ecr:BatchGetImage",
                    "ecr:DescribeImages",
                    "ecr:DescribeRepositories",
                    "ecr:GetDownloadUrlForLayer",
                    "ecr:InitiateLayerUpload",
                    "ecr:PutImage",
                    "ecr:UploadLayerPart",
                    "ecr:CompleteLayerUpload"
                },
                Conditions = new Dictionary<string, object>
                {
                    ["ForAnyValue:StringLike"] = new Dictionary<string, object>
                    {
                        ["aws:PrincipalOrgPaths"] = "o-u6ecwc10h7/*"
                    }
                }
            }));                        
        }
    }
}