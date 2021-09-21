using System.Collections.Generic;
using Amazon.CDK;
using TimesheetApiInfra.Props;

namespace TimesheetApiInfra
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            var stackProps = new ApiStackProps
            {
                OrganizationId = "o-u6ecwc10h7",
                ApiName = "Timesheet",
                DeployEnvs = new List<DeploymentEnvironment> {
                    new DeploymentEnvironment {AccountId = "324668897075", EnvironmentName = "dev"}
                }
            };

            foreach (var env in stackProps.DeployEnvs)
            {
                var stackName = $"{stackProps.ApiName.ToLower()}api-{env.EnvironmentName}";
                var ecrRepoName = $"{stackName}-repo";

                new EcrStack(app, $"{stackName}-ecrrepo", new EcrStackProps
                {
                    EcrRepoName = ecrRepoName,
                    OrganizationId = stackProps.OrganizationId
                });

                new InfraStack(app, $"{stackName}-infra", new InfraStackProps
                {
                    AccountId = env.AccountId,
                    EcrRepoName = ecrRepoName
                });
            };

            // new EcrStack(app, $"{stackProps.EcrRepoName}-ecr-repo", stackProps);
            //new TimesheetApiInfraStack(app, $"{stackProps.EcrRepoName}-", stackProps);
            //new PipelineStack(app, $"{stackProps.EcrRepoName}-pipeline", stackProps);

            app.Synth();
        }
    }

    // internal interface IAccountStage
    // {
    //     string AccountId { get; set; }
    //     string EnvName { get; set; }
    // }

    // internal class AccountStage : IAccountStage
    // {
    //     public string AccountId { get; set; }
    //     public string EnvName { get; set; }
    // }

    // internal class CrossStackProps : IStackProps
    // {
    //     public string OrganizationId { get; set; }
    //     public string KMSKeyId { get; set; }
    //     public string EcrRepoName { get; set; }
    //     public string CodeStarConnectionId { get; set; }
    //     public IAccountStage[] AllStages { get; set; }
    //     public IAccountStage Stage { get; set; }
    // }

    // internal class InputProps
    // {
    //     public string OrganizationId { get; set; }
    //     public string KMSKeyId { get; set; }
    //     public string EcrRepoName { get; set; }
    //     public string CodeStarConnectionId { get; set; }
    //     public IDictionary<string, string> Stages { get; set; }
    // }

}
