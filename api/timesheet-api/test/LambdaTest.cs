using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.IO;

namespace TimesheetApi
{
    public class LambdaTest
    {
        [Fact]
        public async Task TetGetMethod()
        {
            var lambda = new Lambda();
            var request = new APIGatewayProxyRequest{
                HttpMethod = "Get"
            };
            
            //using FileStream openStream = File.OpenRead("event.json");
            //var request2 = await JsonSerializer.DeserializeAsync<APIGatewayProxyRequest>(openStream);

            var context = new TestLambdaContext();
            
            var response = await lambda.Handler(request, context);
            Assert.Equal(200, response.StatusCode);

            var timesheet = JsonSerializer.Deserialize<Timesheet>(response.Body);
            Assert.Equal(timesheet.Id, "Test_2");
        }
    }
}
