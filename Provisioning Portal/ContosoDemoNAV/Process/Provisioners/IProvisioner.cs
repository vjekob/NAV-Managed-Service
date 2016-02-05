using System;
using ContosoDemoNAV.Events;
using ContosoDemoNAV.Process.Workflow;

namespace ContosoDemoNAV.Process.Provisioners
{
    [Obsolete]
    public interface IProvisioner
    {
        WorkflowStatus Status { get; }
        Exception Error { get; }

        event EventHandler Completed;
        event EventHandler<WorkflowStatusChangeEventArgs> StatusChanged;

        void Provision(State state);
    }
}