using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;

namespace TimesheetApi
{
    public interface ITimesheetService
    {
        Task<Timesheet> Get(string userId, int weekId);

        public ILambdaLogger Logger { get; set; }
    }

    internal sealed class TimesheetService : ITimesheetService
    {
        private readonly IDynamoDBContext dynamoDBContext;

        public TimesheetService(IDynamoDBContext dynamoDBContext)
        {
            this.dynamoDBContext = dynamoDBContext;
        }

        public ILambdaLogger Logger { get; set; }

        public async Task<Timesheet> Get(string userId, int weekId)
        {
            return await this.dynamoDBContext.LoadAsync<Timesheet>($"{userId}_{weekId}");

            // return (await this.dynamoDBContext
            //         .QueryAsync<Timesheet>(hashKey)
            //         .GetRemainingAsync()
            //         ).ToList().FirstOrDefault();

            // return new Timesheet
            // {
            //     Id = "Test_1",
            //     WeekId = 1
            //     // WeekSheet = new List<TimesheetDay> {
            //     //     {new TimesheetDay { Day = DateTime.Now, Hours = 10, Description = "Test Desc" }}
            //     // }
            // };
        }
    }

}