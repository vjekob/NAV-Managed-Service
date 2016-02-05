using ContosoDemoNAV.Process.Workflow;

namespace ContosoDemoNAV.Events
{
    public class WorkflowStatusChangeEventArgs
    {
        public WorkflowStatus OldStatus { get; set; }
        public WorkflowStatus NewStatus { get; set; }
    }
}