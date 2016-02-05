using System;
using ContosoDemoNAV.Events;

namespace ContosoDemoNAV.Process.Workflow
{
    public class BaseWorkflowTask : WorkflowTask, ITask
    {
        private readonly Action<State> _action;
        private WorkflowStatus _status;

        public WorkflowStatus Status
        {
            get { return _status; }
            protected set
            {
                if (_status == value)
                    return;

                var oldStatus = _status;
                _status = value;
                StatusChanged?.Invoke(this,
                    new WorkflowStatusChangeEventArgs {OldStatus = oldStatus, NewStatus = value});

                if (value == WorkflowStatus.Completed || value == WorkflowStatus.Error)
                    Completed?.Invoke(this, new EventArgs());
            }
        }

        public Guid TaskId { get; } = Guid.NewGuid();
        public int Ordinal { get; set;  }
        public bool HighLevelTask { get; set; }
        public string Error { get; protected set; }
        public bool ReadyToRun => Status == WorkflowStatus.None && !HasPredecessors();

        public string Description { get; set; }
        public event EventHandler Completed;
        public event EventHandler<WorkflowStatusChangeEventArgs> StatusChanged;

        protected BaseWorkflowTask() { }

        public BaseWorkflowTask(string desc, Action<State> action)
        {
            Description = desc;
            _action = action;
        }

        public virtual void Run(State state)
        {
            SafeInvoke<IWorkflowTask>((s, a) => { _action?.Invoke(s); }, state);
            Complete();
        }

        public virtual void Complete()
        {
            Status = WorkflowStatus.Completed;
        }

        public virtual void Fail(Exception e)
        {
            Error = e.Message;
            Status = WorkflowStatus.Error;
        }

        protected void SafeInvoke<T>(Action<State, T> action, State state) where T : class, IWorkflowTask
        {
            try
            {
                action?.Invoke(state, this as T);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}