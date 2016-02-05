using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebGrease.Css.Extensions;

namespace ContosoDemoNAV.Process.Workflow
{
    public abstract class WorkflowTaskGroup : BaseWorkflowTask
    {
        private const int MaxThreads = 5;

        private readonly object _lockAddRemove = new object();
        private readonly object _lockThreadCounter = new object();
        private readonly object _lockProducer = new object();

        private readonly ConcurrentBag<ITask> _tasks = new ConcurrentBag<ITask>();
        private readonly List<ITask> _remaining = new List<ITask>();
        private readonly BlockingCollection<ITask> _queue = new BlockingCollection<ITask>();

        private int _threadCount;
        private State _state;

        private void TaskOnCompleted(object sender, EventArgs eventArgs)
        {
                var task = sender as ITask;
                if (task?.Error != null)
                {
                    Error = task.Error;
                    Status = WorkflowStatus.Error;
                }
                else
                {
                    _tasks.ToList().ForEach(t => (t as IWorkflowTask)?.CompletePredecessor(task));
                }
            ProduceQueue();
        }

        private void PrepareSuccessorsAndPredecessors(ITask task)
        {
            if (!_tasks.Contains(task))
                _tasks.Add(task);

            var tasks = new List<ITask>();
            (task as IWorkflowTask)?.ReportTasks(tasks);
            tasks.Where(t => !_tasks.Contains(t)).ForEach(PrepareSuccessorsAndPredecessors);
            tasks.Where(t => !_tasks.Contains(t)).ForEach(_tasks.Add);
        }

        private void OrderTasks(ITask thisTask, int nextOrdinal)
        {
            _tasks.Where(t => (thisTask as IWorkflowTask)?.IsPredecessorOf(t) ?? !(t as IWorkflowTask)?.HasPredecessors() ?? t.Ordinal == 0).ForEach(t =>
            {
                if (t.Ordinal == 0)
                {
                    t.Ordinal = ++nextOrdinal;
                }
                OrderTasks(t, nextOrdinal);
            });
        }

        private void PrepareExecution(State state)
        {
            _state = state;
            RequestTasks();
            _tasks.ForEach(PrepareSuccessorsAndPredecessors);

            _state.GetOrCreate<List<ITask>>().AddRange(_tasks.Where(t => !_state.Get<List<ITask>>().Contains(t)));

            lock (_lockAddRemove)
                _tasks.ForEach(t =>
                {
                    _remaining.Add(t);
                    t.Completed += TaskOnCompleted;
                });

            OrderTasks(null, Ordinal);
        }

        private void ProduceQueue()
        {
            List<ITask> ready;
            lock (_lockProducer)
            {
                ready = _remaining.Where(t => t.ReadyToRun).ToList();
                _remaining.RemoveAll(t => ready.Contains(t));
            }

            foreach (var t in ready.Where(t => t.ReadyToRun))
            {
                _queue.Add(t);
            }
        }

        private void ProcessTasks()
        {
            Status = WorkflowStatus.Running;

            ProduceQueue();

            var sleepTime = 1;
            while (Status != WorkflowStatus.Completed && Status != WorkflowStatus.Error)
            {
                bool threadStart;
                int threadCount;
                lock (_lockThreadCounter)
                {
                    threadCount = _threadCount;
                    threadStart = _threadCount < MaxThreads && _queue.Any();
                    if (threadStart)
                    {
                        _threadCount++;
                    }
                }

                if (threadStart)
                {
                    Task.Factory.StartNew(() =>
                    {
                        var task = _queue.GetConsumingEnumerable().First();
                        task.Run(_state);
                        while (task.Status != WorkflowStatus.Completed && task.Status != WorkflowStatus.Error)
                            Thread.Sleep(50);
                        lock (_lockThreadCounter)
                            _threadCount--;
                    });
                }

                if (_tasks.All(t => t.Status == WorkflowStatus.Completed))
                {
                    Status = WorkflowStatus.Completed;
                }

                lock (_lockThreadCounter)
                    sleepTime = threadCount == _threadCount ? sleepTime + 1 : 1;

                Thread.Sleep(sleepTime);
            }
        }

        protected abstract void RequestTasks();

        protected IWorkflowTask RegisterTask(ITask task)
        {
            if (task == null) return null;

            if (!_tasks.Contains(task))
            {
                _tasks.Add(task);
            }
            return task as IWorkflowTask;
        }

        protected IWorkflowTask RegisterTask(List<ITask> tasks)
        {
            if (tasks == null) return null;

            tasks.ForEach(t => RegisterTask(t));
            return tasks.Last() as IWorkflowTask;
        }

        public override void Run(State state)
        {
            PrepareExecution(state);
            ProcessTasks();
        }

        public void RunAsync(State state, Action complete)
        {
            PrepareExecution(state);
            Task.Factory.StartNew(() =>
            {
                ProcessTasks();
                complete?.Invoke();
            });
        }

        public IEnumerable<ITask> GetTasks() => _tasks;
    }
}