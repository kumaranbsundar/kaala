using System;
using System.Collections.Generic;
using Amazon.CDK;

namespace ApiPipeline
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            var stackProps = new PipelineStackProps
            {
                //ApiName = app.Node.TryGetContext("api-name").ToString(),
                CodeStarConnectionId = "0f659edf-6b6f-4277-9f62-bdcb0ac08d99",
                KMSKeyId = "4ff28163-6358-46dd-be58-bcfc635bd2b8",
                RepoName = "kaala",
                DeployEnvs = new List<DeploymentEnvironment> {
                    new DeploymentEnvironment {AccountId = "324668897075", EnvironmentName = "dev"}
                }
            };

            //new PipelineStack(app, $"{stackProps.ApiName}ApiPipelineStack", stackProps);
            new PipelineStack(app, "ApiPipelineStack", stackProps);            

            app.Synth();
        }
    }
}
