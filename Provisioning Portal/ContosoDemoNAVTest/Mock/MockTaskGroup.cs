using System.Collections.Generic;
using System.Linq;
using ContosoDemoNAV.Process.Workflow;

namespace ContosoDemoNAVTest.Mock
{
    public class MockTaskGroup : WorkflowTaskGroup
    {
        private ITask[] _tasks;

        public MockTaskGroup(params ITask[] tasks)
        {
            _tasks = tasks;
        }

        public MockTaskGroup(List<ITask> tasks)
        {
            _tasks = tasks.ToArray();
        }

        protected override void RequestTasks()
        {
            _tasks.ToList().ForEach(t => RegisterTask(t));
        }

        public void Add(ITask task)
        {
            var tasks = _tasks.ToList();
            tasks.Add(task);
            _tasks = tasks.ToArray();
        }

        public void Add(List<ITask> tasks)
        {
            _tasks = _tasks.ToList().Concat(tasks).ToArray();
        }
    }
}
