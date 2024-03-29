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

            var apiName = new CfnParameter(this, "ApiName", new CfnParameterProps
            {                
                Type = "String",
                Description = "The name of the API"
            });

            var apiNameLower = new CfnParameter(this, "ApiNameLower", new CfnParameterProps
            {                
                Type = "String",
                Description = "The name of the API in Lowercase"
            });            

            //
            // Create the pipeline
            //
            var pipeline = new Pipeline(this, $"ApiPipeline", new PipelineProps            
            {
                PipelineName = $"{apiName.ValueAsString}ApiPipeline",                
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
                                Role = pipelineRole,
                                VariablesNamespace = "SourceVariables"
                            })
                        }
                    },
                    new Amazon.CDK.AWS.CodePipeline.StageProps {
                        StageName = "Build",
                        Actions = new []
                        {
                            new CodeBuildAction(new CodeBuildActionProps {
                                ActionName = "CDK_Synth",
                                Project = GetCdkBuildProject(encryptionKey, pipelineRole),
                                Input = sourceOutputArtifact,
                                Outputs = new[] {cdkBuildOutput},
                                Role = pipelineRole,
                                EnvironmentVariables = new Dictionary<string, IBuildEnvironmentVariable> {{
                                    "API_NAME", new BuildEnvironmentVariable {
                                        Type = BuildEnvironmentVariableType.PLAINTEXT,
                                        Value = apiNameLower.ValueAsString }}
                                }
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
                    apiNameLower.ValueAsString,
                    cdkBuildOutput,
                    crossAccountRole,
                    deploymentRole,
                    pipelineRole,
                    sourceOutputArtifact,
                    GetContainerBuildProject(encryptionKey, pipelineRole)
                ));
            });
        }

        private PipelineProject GetCdkBuildProject(IKey encryptionKey, IRole pipelineRole)
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
                            ["commands"] = new string[] {
                                "npm install -g aws-cdk",
                                "export PATH=\"$PATH:/root/.dotnet/tools\"",
                                "dotnet tool install -g AWS.CodeArtifact.NuGet.CredentialProvider",
                                "dotnet codeartifact-creds install"
                            }
                        },
                        ["pre_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new string[] {
                                "dotnet nuget add source -n codeartifact \"$(aws codeartifact get-repository-endpoint --domain eonsos --domain-owner 055117415094 --repository kaala --format nuget --query repositoryEndpoint --output text)\"v3/index.json"
                            }
                        },                        
                        ["build"] = new Dictionary<string, object>
                        {
                            ["commands"] = "cd api/$API_NAME-api/infra && npx cdk synth -o dist"
                        }
                    },
                    ["artifacts"] = new Dictionary<string, object>
                    {
                        ["base-directory"] = $"api/$API_NAME-api/infra/dist",
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

        private PipelineProject GetContainerBuildProject(IKey encryptionKey, IRole pipelineRole)
        {
            return new PipelineProject(this, "ContainerBuild", new PipelineProjectProps
            {
                BuildSpec = BuildSpec.FromObject(new Dictionary<string, object>
                {
                    ["version"] = "0.2",
                    ["phases"] = new Dictionary<string, object>
                    {
                        ["pre_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[] {
                                "echo Logging in to Amazon ECR...",
                                "aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin $ECR_REGISTRY"
                            }
                        },
                        ["build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[] {
                                "echo Build started on `date`",
                                "echo Building the Docker image...",
                                "cd api/timesheet-api/src",
                                "docker build -t $ECR_IMAGE:$CODEBUILD_RESOLVED_SOURCE_VERSION -t $ECR_IMAGE:latest ."
                            }
                        },
                        ["post_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[] {
                                "echo Build completed on `date`",
                                "echo Pushing the Docker image...",
                                $"docker push $ECR_IMAGE:$CODEBUILD_RESOLVED_SOURCE_VERSION",
                                $"docker push $ECR_IMAGE:latest"
                            }
                        }
                    }
                }),
                Environment = new BuildEnvironment
                {
                    BuildImage = LinuxBuildImage.STANDARD_5_0,
                    Privileged = true
                },
                EncryptionKey = encryptionKey,
                Role = pipelineRole
            });
        }

        private Amazon.CDK.AWS.CodePipeline.IStageOptions GetPipelineStage(
            DeploymentEnvironment deployEnv,
            string apiName,
            Artifact_ cdkBuildOutput,
            IRole crossAccountRole,
            IRole deploymentRole,
            IRole pipelineRole,
            Artifact_ sourceOutputArtifact,
            PipelineProject containerBuildProject)
        {
            var ecrStackName = $"{apiName}api-{deployEnv.EnvironmentName}-ecrrepo";            
            var ecrRepoName = $"{apiName}api-{deployEnv.EnvironmentName}-repo";
            var ecrRegistry = $"{deployEnv.AccountId}.dkr.ecr.us-east-1.amazonaws.com";
            var ecrImageId = $"{ecrRegistry}/{ecrRepoName}";
            var infraStackName = $"{apiName}api-{deployEnv.EnvironmentName}-infra";

            return new Amazon.CDK.AWS.CodePipeline.StageOptions
            {
                StageName = $"Deploy_{deployEnv.EnvironmentName}",
                Actions = new Amazon.CDK.AWS.CodePipeline.Action[]
                {
                    new CloudFormationCreateUpdateStackAction(new CloudFormationCreateUpdateStackActionProps {
                        ActionName = "Create_ECR",
                        TemplatePath = cdkBuildOutput.AtPath($"{ecrStackName}.template.json"),
                        StackName = ecrStackName,
                        AdminPermissions = true,
                        Role = crossAccountRole,
                        DeploymentRole = deploymentRole,
                        CfnCapabilities = new[] { CfnCapabilities.ANONYMOUS_IAM},
                        RunOrder = 1
                    }),
                    new CodeBuildAction(new CodeBuildActionProps {
                        ActionName = "Lambda_Image_Build",
                        Project = containerBuildProject,
                        Input = sourceOutputArtifact,
                        Role = pipelineRole,
                        EnvironmentVariables = new Dictionary<string, IBuildEnvironmentVariable> {{
                            "ECR_REGISTRY", new BuildEnvironmentVariable {
                                Type = BuildEnvironmentVariableType.PLAINTEXT,
                                Value = ecrRegistry }},{
                            "ECR_IMAGE", new BuildEnvironmentVariable {
                                Type = BuildEnvironmentVariableType.PLAINTEXT,
                                Value = ecrImageId }}
                        },
                        RunOrder = 2
                    }),
                    new CloudFormationCreateUpdateStackAction(new CloudFormationCreateUpdateStackActionProps {
                        ActionName = "Deploy",
                        TemplatePath = cdkBuildOutput.AtPath($"{infraStackName}.template.json"),
                        StackName = infraStackName,
                        AdminPermissions = true,
                        Role = crossAccountRole,
                        DeploymentRole = deploymentRole,
                        CfnCapabilities = new[] { CfnCapabilities.ANONYMOUS_IAM },
                        ParameterOverrides = new Dictionary<string, object> {
                            { "ImageTag", "#{SourceVariables.CommitId}" }
                        },
                        RunOrder = 3
                    })                    
                }
            };
        }
    }
}