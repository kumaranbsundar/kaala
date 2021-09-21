using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;

namespace TimesheetApi
{
    internal sealed class TimesheetService : ITimesheetService
    {
        private readonly IDynamoDBContext dynamoDBContext;

        public TimesheetService(IDynamoDBContext dynamoDBContext)
        {
            this.dynamoDBContext = dynamoDBContext;
        }

        public ILambdaLogger Logger { get; set; }

        public async Task<string> Get(string userId, string weekId)
        {
            var dailySheets = await this.dynamoDBContext
                .QueryAsync<Timesheet>($"{userId}_{weekId}")
                .GetRemainingAsync();

            return dailySheets == null ? null : JsonSerializer.Serialize(dailySheets);
        }

        public async Task Post(string userId, string weekId, string httpBody)
        {
            var dailySheets = JsonSerializer.Deserialize<IEnumerable<Timesheet>>(httpBody);

            var batch = this.dynamoDBContext.CreateBatchWrite<Timesheet>();
            batch.AddPutItems(dailySheets.Select(ds => new Timesheet
            {
                Day = ds.Day,
                Hours = ds.Hours,
                Description = "this is the desc " + ds.Description,
                Id = $"{userId}_{weekId}"
            }));
            
            await batch.ExecuteAsync();
        }
    }

}