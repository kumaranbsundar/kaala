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

            app.Synth();
        }
    }
}