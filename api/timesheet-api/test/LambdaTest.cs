using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace TimesheetApi
{
    public class LambdaTest
    {
        [Fact]
        public async Task TestGetMethod()
        {
            var lambda = new Lambda();
            var request = new APIGatewayProxyRequest{
                HttpMethod = "Get",
                PathParameters = new Dictionary<string, string> { 
                    { "UserId", "Test" },
                    { "Id", "1" }  
                } 
            };
            
            //using FileStream openStream = File.OpenRead("event.json");
            //var request2 = await JsonSerializer.DeserializeAsync<APIGatewayProxyRequest>(openStream);

            var context = new TestLambdaContext();
            
            var response = await lambda.Handler(request, context);
            Assert.Equal(200, response.StatusCode);

            var timesheets = JsonSerializer.Deserialize<IEnumerable<Timesheet>>(response.Body);
            Assert.Equal(timesheets.ToList().Count, 2);
        }

        [Fact]
        public async Task TestPostMethod()
        {
            var lambda = new Lambda();
            var request = new APIGatewayProxyRequest{
                HttpMethod = "Post",
                PathParameters = new Dictionary<string, string> { 
                    { "UserId", "Test" },
                    { "Id", "1" }  
                },
                Body = "[{\"Day\": \"2021-07-19\",\"Hours\": 8,\"Description\": \"worked on timesheet app\"}" +
                    ",{\"Day\": \"2021-07-20\",\"Hours\": 9,\"Description\": \"continued working worked on timesheet app\"}]"
            };
            
            var context = new TestLambdaContext();
            
            var response = await lambda.Handler(request, context);
            Assert.Equal(200, response.StatusCode);
        }
    }
}
