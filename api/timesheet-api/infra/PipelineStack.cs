using Amazon.CDK;
using Amazon.CDK.Pipelines;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.CodeBuild;

namespace TimesheetApiInfra
{
    public class PipelineStack : Stack
    {
        internal PipelineStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var sourceArtifact = new Artifact_();
            var cloudAssemblyArtifact = new Artifact_();
            
            var pipeline = new CdkPipeline(this, "TimesheetApiPipeline", new CdkPipelineProps {
                PipelineName = "TimesheetApiPipeline",
                CloudAssemblyArtifact = cloudAssemblyArtifact,
                SourceAction = new CodeStarConnectionsSourceAction(new CodeStarConnectionsSourceActionProps {
                    ActionName = "Github",
                    Branch = "main",
                    Output = sourceArtifact,
                    Owner = "kumaranbsundar",
                    Repo = "kaala",
                    TriggerOnPush = true,
                    ConnectionArn = "arn:aws:codestar-connections:us-east-1:324668897075:connection/fb96c903-1567-4ea4-8b9f-a0b9b17f80cc"
                }),                
                // SourceAction = new GitHubSourceAction(new GitHubSourceActionProps {
                //     ActionName = "Github",
                //     Branch = "main",
                //     Output = sourceArtifact,
                //     OauthToken = SecretValue.SecretsManager("GITHUB_TOKEN_NAME"),
                //     Trigger = GitHubTrigger.POLL,
                //     Owner = "kumaranbsundar",
                //     Repo = "kaala"
                // }),
                SynthAction = new SimpleSynthAction(new SimpleSynthActionProps {
                    Environment = new BuildEnvironment {
                        BuildImage = LinuxBuildImage.STANDARD_5_0
                    },
                    Subdirectory = "api/timesheet-api/infra",
                    SourceArtifact = sourceArtifact,
                    CloudAssemblyArtifact = cloudAssemblyArtifact,
                    InstallCommands = new [] {"npm install -g aws-cdk"},
                    SynthCommand = "cdk synth"
                })
            });

            var devStage = pipeline.AddApplicationStage(new SolutionStage(this, "Development"));
            // devStage.AddActions(new ShellScriptAction(new ShellScriptActionProps {
            //     ActionName = "Test Lamabda Function",
            //     Commands = new[] {
            //         ""
            //     }
            // }))
        }
    }
}