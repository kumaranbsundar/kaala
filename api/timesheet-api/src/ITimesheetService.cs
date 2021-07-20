using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace TimesheetApi
{
    public interface ITimesheetService
    {
        Task<string> Get(string userId, string weekId);
        Task Post(string userId, string weekId, string httpBody);
        public ILambdaLogger Logger { get; set; }
    }

}