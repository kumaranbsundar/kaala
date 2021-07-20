using System;
using Amazon.DynamoDBv2.DataModel;

namespace TimesheetApi
{
    [DynamoDBTable("Timesheet")]
    public class Timesheet
    {
        [DynamoDBHashKey]
        public string Id { get; set; }

        [DynamoDBRangeKey]
        public DateTime Day { get; set; }

        public float Hours { get; set; }

        public string Description { get; set; }
    }
}