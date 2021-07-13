using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using System.Text.Json;
using Amazon;
using System;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace TimesheetApi
{
    public class Lambda
    {
        ITimesheetService timesheetService;

        public Lambda() : this(null) { }

        public Lambda(ITimesheetService timesheetService)
        {
            this.timesheetService = timesheetService ?? new TimesheetService
            (
                new DynamoDBContext(new AmazonDynamoDBClient(RegionEndpoint.USEast1))
            );
        }


        /// <summary>
        /// A Lambda function to respond to HTTP methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The API Gateway response.</returns>
        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            timesheetService.Logger = context.Logger;

            Timesheet timesheet = null;
            switch (request.HttpMethod.ToUpper())
            {
                case "GET":
                    timesheet = await timesheetService.Get(
                        request.PathParameters["UserId"],
                        Int32.Parse(request.PathParameters["Id"]));
                    break;
                default:
                    break;
            }

            if (timesheet == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound
                };
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonSerializer.Serialize<Timesheet>(timesheet),
                Headers = new Dictionary<string, string> { { "Content-Type", "text/json" } }
            };
        }
    }
}
