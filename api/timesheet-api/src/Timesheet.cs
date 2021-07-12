using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;

namespace TimesheetApi
{
    [DynamoDBTable("Timesheet")]
    public class Timesheet
    {
        [DynamoDBHashKey]
        public string Id { get; set; }
        public int WeekId { get; set; }
        //public IEnumerable<TimesheetDay> WeekSheet { get; set; }
    }

    public class TimesheetDay
    {
        public DateTime Day { get; set; }
        public float Hours { get; set; }
        public string Description { get; set; }
    }

}