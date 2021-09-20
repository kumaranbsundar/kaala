using System.Collections.Generic;
using System.Linq;
using Amazon.CDK;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.S3;

namespace ApiPipeline
{
    public class PipelineStack : Stack
    {
        internal PipelineStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var stackProps = props as PipelineStackProps;
            var codestarArn = $"arn:aws:codestar-connections:us-east-1:{this.Account}:connection/{stackProps.CodeStarConnectionId}";
            var encryptionKey = Key.FromKeyArn(this, "encryptionKey", $"arn:aws:kms:{this.Region}:{this.Account}:key/{stackProps.KMSKeyId}");

            var sourceArtifact = Bucket.FromBucketAttributes(this, "sourceArtifact", new BucketAttributes
            {
                BucketArn = $"arn:aws:s3:::codepipeline-artifacts-{this.Account}-{this.Region}",
                EncryptionKey = encryptionKey
            });

            var pipelineRole = Role.FromRoleArn(this, "pipelineRole",
                $"arn:aws:iam::{this.Account}:role/CodePipelineMasterRole",
                new FromRoleArnOptions { Mutable = false }
            );

            var sourceOutputArtifact = new Artifact_();
            var cdkBuildOutput = new Artifact_("CdkBuildOutput");

            //
            // Create the pipeline
            //
            var pipeline = new Pipeline(this, $"{stackProps.ApiName}ApiPipeline", new PipelineProps
            {
                PipelineName = $"{stackProps.ApiName}ApiPipeline",
                ArtifactBucket = sourceArtifact,
                Role = pipelineRole,
                Stages = new[]
                {
                    new Amazon.CDK.AWS.CodePipeline.StageProps {
                        StageName = "Source",
                        Actions = new []
                        {
                            new CodeStarConnectionsSourceAction(new CodeStarConnectionsSourceActionProps
                            {
                                ActionName = "Github",
                                Branch = "main",
                                Output = sourceOutputArtifact,
                                Owner = "kumaranbsundar",
                                Repo = stackProps.RepoName,
                                TriggerOnPush = true,
                                ConnectionArn = codestarArn,
                                Role = pipelineRole
                            })
                        }
                    },
                    new Amazon.CDK.AWS.CodePipeline.StageProps {
                        StageName = "Build",
                        Actions = new []
                        {
                            new CodeBuildAction(new CodeBuildActionProps {
                                ActionName = "CDK_Synth",
                                Project = GetCdkBuildProject(encryptionKey, pipelineRole, stackProps),
                                Input = sourceOutputArtifact,
                                Outputs = new[] {cdkBuildOutput},
                                Role = pipelineRole
                            })
                        }
                    }
                }
            });

            stackProps.DeployEnvs.ToList().ForEach(de =>
            {
                var crossAccountRole = Role.FromRoleArn(this, "crossAccountRole",
                    $"arn:aws:iam::{de.AccountId}:role/CodePipelineCrossAccountRole",
                    new FromRoleArnOptions { Mutable = false }
                );

                var deploymentRole = Role.FromRoleArn(this, "deploymentRole",
                    $"arn:aws:iam::{de.AccountId}:role/CodePipelineCfnDeploymentRole",
                    new FromRoleArnOptions { Mutable = false }
                );

                pipeline.AddStage(GetPipelineStage(
                    de,
                    stackProps,
                    cdkBuildOutput,
                    crossAccountRole,
                    deploymentRole
                ));
            });
        }

        private PipelineProject GetCdkBuildProject(IKey encryptionKey, IRole pipelineRole, PipelineStackProps stackProps)
        {
            return new PipelineProject(this, "CDKBuild", new PipelineProjectProps
            {
                BuildSpec = BuildSpec.FromObject(new Dictionary<string, object>
                {
                    ["version"] = "0.2",
                    ["phases"] = new Dictionary<string, object>
                    {
                        ["install"] = new Dictionary<string, object>
                        {
                            ["commands"] = "npm install -g aws-cdk"
                        },
                        ["build"] = new Dictionary<string, object>
                        {
                            ["commands"] = $"cd api/{stackProps.ApiName.ToLower()}-api/infra && npx cdk synth -o dist"
                        }
                    },
                    ["artifacts"] = new Dictionary<string, object>
                    {
                        ["base-directory"] = $"api/{stackProps.ApiName.ToLower()}-api/infra/dist",
                        ["files"] = new string[]
                        {
                            "*.template.json"
                        }
                    }
                }),
                Environment = new BuildEnvironment
                {
                    BuildImage = LinuxBuildImage.STANDARD_5_0
                },
                EncryptionKey = encryptionKey,
                Role = pipelineRole
            });
        }

        private Amazon.CDK.AWS.CodePipeline.IStageOptions GetPipelineStage(
            DeploymentEnvironment deployEnv,
            PipelineStackProps stackProps,
            Artifact_ cdkBuildOutput,
            IRole crossAccountRole,
            IRole deploymentRole)
        {
            var ecrRepoName = $"{stackProps.ApiName}api-ecrrepo-{deployEnv.EnvironmentName}";

            return new Amazon.CDK.AWS.CodePipeline.StageOptions
            {
                StageName = $"Deploy_{deployEnv.EnvironmentName}",
                Actions = new Amazon.CDK.AWS.CodePipeline.Action[]
                {
                    new CloudFormationCreateUpdateStackAction(new CloudFormationCreateUpdateStackActionProps {
                        ActionName = "Create_ECR",
                        TemplatePath = cdkBuildOutput.AtPath($"{ecrRepoName}.template.json"),
                        StackName = ecrRepoName,
                        AdminPermissions = true,
                        Role = crossAccountRole,
                        DeploymentRole = deploymentRole,
                        CfnCapabilities = new[] { CfnCapabilities.ANONYMOUS_IAM},
                        RunOrder = 1
                    })
                }
            };
        }
    }
}