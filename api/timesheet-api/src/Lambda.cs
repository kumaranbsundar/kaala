using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TimesheetApi
{
    public class Lambda
    {
        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Lambda()
        {
        }


        /// <summary>
        /// A Lambda function to respond to HTTP methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The API Gateway response.</returns>
        public APIGatewayProxyResponse Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Get Request\n");

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "Hello from the Kumarans",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };

            return response;
        }
    }
}
