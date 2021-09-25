using Amazon.CDK;
using Amazon.JSII.Runtime.Deputy;

namespace ApiInfraStack
{
    internal class InfraStackProps : DeputyBase, IStackProps
    {
        public string AccountId { get; set; }
        public string EcrRepoName { get; set; }
    }
}