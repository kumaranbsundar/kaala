using System.Collections.Generic;
using Amazon.CDK;
using Amazon.JSII.Runtime.Deputy;

namespace TimesheetApiInfra.Props
{
    internal class ApiStackProps : DeputyBase, IStackProps
    {
        public string OrganizationId { get; set; }
        public string KMSKeyId { get; set; }
        public string CodeStarConnectionId { get; set; }
        public string ApiName { get; set; }
        public string RepoName { get; set; }
        public IList<DeploymentEnvironment> DeployEnvs { get; set; }
    }
}