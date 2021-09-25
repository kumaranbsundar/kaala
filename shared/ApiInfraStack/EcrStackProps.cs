using Amazon.CDK;
using Amazon.JSII.Runtime.Deputy;

namespace ApiInfraStack
{
    internal class EcrStackProps : DeputyBase, IStackProps
    {
        public string OrganizationId { get; set; }
        public string EcrRepoName { get; set; }
    }
}