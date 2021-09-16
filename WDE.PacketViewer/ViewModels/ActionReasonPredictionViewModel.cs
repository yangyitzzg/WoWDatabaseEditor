using WDE.PacketViewer.Processing.Processors.ActionReaction;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels
{
    public class ActionReasonPredictionViewModel
    {
        public int PacketNumber { get; }
        public int Chance { get; }
        public int Diff { get; }
        public string Description { get; }
        public string Explain { get; }
        
        public ActionReasonPredictionViewModel(PacketBase action, double probability, string explain, EventHappened reason)
        {
            PacketNumber = reason.PacketNumber;
            Description = reason.Description;
            Chance = (int)(probability * 100);
            Explain = explain;
            Diff = (int)(action.Time.ToDateTime() - reason.Time).TotalMilliseconds;
        }
    }

    public class PossibleActionViewModel
    {
        public int PacketNumber { get; }
        public int Chance { get; }
        public int Diff { get; }
        public string Description { get; }
        public string Explain { get; }
        
        public PossibleActionViewModel(PacketBase @event, double probability, string explain, ActionHappened action)
        {
            PacketNumber = action.PacketNumber;
            Description = action.Description;
            Chance = (int)(probability * 100);
            Explain = explain;
            Diff = (int)(action.Time - @event.Time.ToDateTime()).TotalMilliseconds;
        }
    }
    
}