using PollQT.DataTypes;
using System.Threading.Tasks;

namespace PollQT.OutputSinks
{

    internal interface IOutputSink
    {
        public Task NewEvent(PollResult pollResults);
    }
}
