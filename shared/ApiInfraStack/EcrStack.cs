using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.ECR;
using System.Collections.Generic;

namespace ApiInfraStack
{
    internal class EcrStack : Stack
    {
        internal EcrStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var stackProps = props as EcrStackProps;

            var ecrRepo = new Repository(this, stackProps.EcrRepoName, new RepositoryProps
            {
                RepositoryName = stackProps.EcrRepoName
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
                        ["aws:PrincipalOrgPaths"] = $"{stackProps.OrganizationId}/*"
                    }
                }
            }));
        }
    }
}