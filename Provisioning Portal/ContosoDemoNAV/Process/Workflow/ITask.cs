using System;
using ContosoDemoNAV.Events;

namespace ContosoDemoNAV.Process.Workflow
{
    public interface ITask
    {
        Guid TaskId { get; }
        int Ordinal { get; set; }
        bool HighLevelTask { get; set; }
        string Error { get; }
        WorkflowStatus Status { get; }
        bool ReadyToRun { get; }
        string Description { get; set; }

        event EventHandler Completed;
        event EventHandler<WorkflowStatusChangeEventArgs> StatusChanged;

        void Run(State state);
    }
}