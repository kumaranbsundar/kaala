using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.S3;

namespace TimesheetApiInfra
{
    public class PipelineStack : Stack
    {
        internal PipelineStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var encryptionKey = Key.FromKeyArn(this, "encryptionKey"
                , "arn:aws:kms:us-east-1:055117415094:key/4ff28163-6358-46dd-be58-bcfc635bd2b8");

            var sourceArtifact = Bucket.FromBucketAttributes(this, "sourceArtifact", new BucketAttributes
            {
                BucketArn = "arn:aws:s3:::codepipeline-artifacts-055117415094-us-east-1",
                EncryptionKey = encryptionKey
            });

            var pipelineRole = Role.FromRoleArn(this, "pipelineRole",
                "arn:aws:iam::055117415094:role/CodePipelineMasterRole",
                new FromRoleArnOptions { Mutable = false }
            );

            var devCrossAccountRole = Role.FromRoleArn(this, "crossAccountRole",
                "arn:aws:iam::324668897075:role/CodePipelineCrossAccountRole",
                new FromRoleArnOptions { Mutable = false }
            );

            var deploymentRole = Role.FromRoleArn(this, "deploymentRole",
                    "arn:aws:iam::324668897075:role/CodePipelineCfnDeploymentRole",
                    new FromRoleArnOptions { Mutable = false }
            );

            var devAccountId = "324668897075";
            var ecrRepoName = "timesheetapi";
            var ecrImageId = $"{devAccountId}.dkr.ecr.us-east-1.amazonaws.com/{ecrRepoName}:$CODEBUILD_RESOLVED_SOURCE_VERSION";            

            var sourceOutputArtifact = new Artifact_();
            var cdkBuildOutput = new Artifact_("CdkBuildOutput");

            var cdkBuildProject = new PipelineProject(this, "CDKBuild", new PipelineProjectProps
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
                            ["commands"] = "cd api/timesheet-api/infra && npx cdk synth -o dist"
                        }
                    },
                    ["artifacts"] = new Dictionary<string, object>
                    {
                        ["base-directory"] = "api/timesheet-api/infra/dist",
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

            var containerBuildProject = new PipelineProject(this, "ContainerBuild", new PipelineProjectProps
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
                                "aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 055117415094.dkr.ecr.us-east-1.amazonaws.com"
                            }
                        },
                        ["build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[] {
                                "echo Build started on `date`",
                                "echo Building the Docker image...",
                                "cd api/timesheet-api/src",
                                $"docker build -t {ecrImageId} ."
                            }
                        },
                        ["post_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[] {
                                "echo Build completed on `date`",
                                "echo Pushing the Docker image...",
                                $"docker push {ecrImageId}"
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

            var pipeline = new Pipeline(this, "TimesheetApiPipeline", new PipelineProps
            {
                PipelineName = "TimesheetApiPipeline",
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
                                Repo = "kaala",
                                TriggerOnPush = true,
                                ConnectionArn = "arn:aws:codestar-connections:us-east-1:055117415094:connection/0f659edf-6b6f-4277-9f62-bdcb0ac08d99",
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
                                Project = cdkBuildProject,
                                Input = sourceOutputArtifact,
                                Outputs = new[] {cdkBuildOutput},
                                Role = pipelineRole
                            })
                        }
                    },
                    new Amazon.CDK.AWS.CodePipeline.StageProps {
                        StageName = "Pipeline_SelfMutate",
                        Actions = new []
                        {
                            new CloudFormationCreateUpdateStackAction(new CloudFormationCreateUpdateStackActionProps {
                                ActionName = "SelfMutate",
                                TemplatePath = cdkBuildOutput.AtPath("TimesheetApiPipeline.template.json"),
                                StackName = "TimesheetApiPipeline",
                                AdminPermissions = true,
                                Role = pipelineRole
                            })
                        }
                    },                    
                    new Amazon.CDK.AWS.CodePipeline.StageProps {
                        StageName = "Deploy_Dev",
                        Actions = new Amazon.CDK.AWS.CodePipeline.Action[]
                        {
                            new CloudFormationCreateUpdateStackAction(new CloudFormationCreateUpdateStackActionProps {
                                ActionName = "Create_ECR",
                                TemplatePath = cdkBuildOutput.AtPath("TimesheetApiImageRepo.template.json"),
                                StackName = "TimesheetApiECRRepo",
                                AdminPermissions = true,
                                Role = devCrossAccountRole,
                                DeploymentRole = deploymentRole,
                                CfnCapabilities = new[] { CfnCapabilities.ANONYMOUS_IAM},
                                RunOrder = 2
                            }),                            
                            new CodeBuildAction(new CodeBuildActionProps {
                                ActionName = "Lambda_Image_Build",
                                Project = containerBuildProject,
                                Input = sourceOutputArtifact,
                                Role = pipelineRole,
                                RunOrder = 3                                
                            }),
                            // new CloudFormationCreateUpdateStackAction(new CloudFormationCreateUpdateStackActionProps {
                            //     ActionName = "Deploy",
                            //     TemplatePath = cdkBuildOutput.AtPath("TimesheetApi.template.json"),
                            //     StackName = "TimesheetApiDeploymentStack",
                            //     AdminPermissions = true,
                            //     Role = devCrossAccountRole,
                            //     DeploymentRole = deploymentRole,
                            //     CfnCapabilities = new[] { CfnCapabilities.ANONYMOUS_IAM},
                            //     RunOrder = 3                                
                            // })
                        }
                    }
                }
            });

            // var pipeline = new CdkPipeline(this, "TimesheetApiPipeline", new CdkPipelineProps
            // {
            //     PipelineName = "TimesheetApiPipeline",
            //     CloudAssemblyArtifact = cloudAssemblyArtifact,
            //     SourceAction = new CodeStarConnectionsSourceAction(new CodeStarConnectionsSourceActionProps
            //     {
            //         ActionName = "Github",
            //         Branch = "main",
            //         Output = sourceArtifact,
            //         Owner = "kumaranbsundar",
            //         Repo = "kaala",
            //         TriggerOnPush = true,
            //         ConnectionArn = "arn:aws:codestar-connections:us-east-1:055117415094:connection/0f659edf-6b6f-4277-9f62-bdcb0ac08d99"
            //     }),
            //     SynthAction = new SimpleSynthAction(new SimpleSynthActionProps
            //     {
            //         Environment = new BuildEnvironment
            //         {
            //             BuildImage = LinuxBuildImage.STANDARD_5_0
            //         },
            //         Subdirectory = "api/timesheet-api/infra",
            //         SourceArtifact = sourceArtifact,
            //         CloudAssemblyArtifact = cloudAssemblyArtifact,
            //         InstallCommands = new[] { "npm install -g aws-cdk" },
            //         SynthCommand = "cdk synth"
            //     })
            // });

            // var devStage = pipeline.AddApplicationStage(new SolutionStage(this
            //     , "Development"
            //     , new Amazon.CDK.StageProps { Env = new Environment { Account = "", Region = "us-east-1" } })
            // );

            // devStage.AddActions(new ShellScriptAction(new ShellScriptActionProps {
            //     ActionName = "Test Lamabda Function",
            //     Commands = new[] {
            //         ""
            //     }
            // }))
        }
    }
}