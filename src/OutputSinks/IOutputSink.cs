using System.Collections.Generic;
using System.Threading.Tasks;
using PollQT.DataTypes;
namespace PollQT.OutputSinks
{
    internal interface IOutputSink
    {
        public Task NewEvent(List<PollResult> pollResults);
    }
}
