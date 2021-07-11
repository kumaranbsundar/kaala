using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;

namespace TimesheetApi
{
    public interface ITimesheetService
    {
        Task<Timesheet> Get(string userId, int weekId);
    }

    internal sealed class TimesheetService : ITimesheetService
    {
        private readonly IDynamoDBContext dynamoDBContext;

        public TimesheetService(IDynamoDBContext dynamoDBContext)
        {
            this.dynamoDBContext = dynamoDBContext;
        }

        public async Task<Timesheet> Get(string userId, int weekId)
        {
            var hashKey = $"{userId}_{weekId}";

            // return (await this.dynamoDBContext
            //         .QueryAsync<Timesheet>(hashKey)
            //         .GetRemainingAsync()
            //         ).ToList().FirstOrDefault();

            return new Timesheet {
                Id = "Test_1",
                WeekId = 1,
                WeekSheet = new List<TimesheetDay> {
                    {new TimesheetDay { Day = DateTime.Now, Hours = 10, Description = "Test Desc" }}
                }
            };
        }

        // public string Post() {

        // }

        // public string Put() {

        // }

        // public string Delete() {

        // }        
    }

}