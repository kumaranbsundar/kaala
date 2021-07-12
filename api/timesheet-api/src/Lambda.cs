using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using System.Text.Json;
using Amazon.Runtime;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.DynamoDBv2.DocumentModel;
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
                new DynamoDBContext(new AmazonDynamoDBClient())
            );

            //s3Client = options.CreateServiceClient<IAmazonS3>();            

            // var chain = new CredentialProfileStoreChain();

            // AWSCredentials awsCredentials;
            // if (chain.TryGetAWSCredentials("kaaladev", out awsCredentials))
            // //if (chain.TryGetProfile("kaaladevtest", out awsCredentials))            
            // {
            //     //var dynamoClient = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
            //     var dynamoClient = new AmazonDynamoDBClient(awsCredentials);

            //     this.timesheetService = timesheetService ?? new TimesheetService(
            //         new DynamoDBContext(dynamoClient), dynamoClient
            //     );

            //     // // Use awsCredentials to create an Amazon S3 service client
            //     // using (var client = new AmazonS3Client(awsCredentials))
            //     // {
            //     //     var response = await client.ListBucketsAsync();
            //     //     Console.WriteLine($"Number of buckets: {response.Buckets.Count}");
            //     // }
            // }

            // // var dynamoClient = new AmazonDynamoDBClient();

            // // this.timesheetService = timesheetService ?? new TimesheetService(
            // //     new DynamoDBContext(dynamoClient)
            // // );
        }


        /// <summary>
        /// A Lambda function to respond to HTTP methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The API Gateway response.</returns>
        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            timesheetService.Logger = context.Logger;

            // ListBucketsRequest request2 = new ListBucketsRequest();
            // ListBucketsResponse response2 = await this.s3Client.ListBucketsAsync(request2);
            // // Process response.
            // foreach (S3Bucket bucket in response2.Buckets)
            // {
            //     context.Logger.LogLine($"{bucket.BucketName}");
            // }

            // var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            // var DDBContext = new DynamoDBContext(this.dynamoClient, config);
            // var timesheet = await DDBContext.LoadAsync<Timesheet>("Test_1");
            // // var timesheetTable = Table.LoadTable(this.dynamoClient, "Timesheet");
            // // var timesheetDoc = await timesheetTable.GetItemAsync("Test_1");
            // context.Logger.LogLine(JsonSerializer.Serialize<Timesheet>(timesheet));

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
                return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.NotFound };
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
