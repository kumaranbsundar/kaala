using System.Collections.Generic;
using Amazon.CDK;

namespace ApiInfraStack
{
    public class ApiStack
    {
        public void Initialize(string apiName, IList<DeploymentEnvironment> environments, Stage app)
        {
            var stackProps = new ApiStackProps
            {
                OrganizationId = "o-u6ecwc10h7",
                ApiName = apiName,
                DeployEnvs = environments
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
        }

    }
}
