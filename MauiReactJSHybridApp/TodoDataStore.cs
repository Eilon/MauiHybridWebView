using System.Diagnostics;

namespace MauiReactJSHybridApp
{
    public class TodoDataStore
    {
        private readonly List<TodoTask> _taskData = new List<TodoTask>()
        {
            new(){ id="todo-0", name="Eat", completed=true  },
            new(){ id="todo-1", name="Sleep", completed=false  },
            new(){ id="todo-2", name="Repeat", completed=false  },
        };

        public event EventHandler TaskDataChanged;

        private void OnTaskDataChanged()
        {
            TaskDataChanged?.Invoke(this, EventArgs.Empty);
        }

        public List<TodoTask> GetData() => _taskData;

        public void AddTask(TodoTask newTask)
        {
            Debug.WriteLine($"AddTask: {newTask.id}: {newTask.name} ({(newTask.completed ? "" : "not ")}completed)");
            _taskData.Add(newTask);
            OnTaskDataChanged();
        }

        public void EditTask(string id, string newName)
        {
            Debug.WriteLine($"EditTask: {id}: {newName}");
            _taskData.Single(t => t.id == id).name = newName;
            OnTaskDataChanged();
        }

        public void DeleteTask(string id)
        {
            Debug.WriteLine($"DeleteTask: {id}");
            _taskData.Remove(_taskData.Single(t => t.id == id));
            OnTaskDataChanged();
        }

        public void ToggleCompletedTask(string id)
        {
            Debug.WriteLine($"ToggleCompletedTask: {id}");
            var task = _taskData.Single(t => t.id == id);
            task.completed = !task.completed;
            OnTaskDataChanged();
        }
    }
}
