using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager.Task
{
    public class TaskResult
    {
        public TaskResult(ExecuterTask task)
        {
            ExecuterTask = task;
        }

        public ExecuterTask ExecuterTask { get; set; }
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
        internal Dictionary<string, object> RertunValues { get; set; } = new Dictionary<string, object>();
        internal TaskResult Add(string key, object value)
        {
            if (Values.ContainsKey(key))
            {
                Values[key] = value;
            }
            else
            {
                Values.Add(key, value);
            }
            return this;
        }
        internal TaskResult AddReturn(string key, object value)
        {
            if (RertunValues.ContainsKey(key))
            {
                RertunValues[key] = value;
            }
            else
            {
                RertunValues.Add(key, value);
            }
            return this;
        }
        internal TaskResult AddReturnRange(TaskResult task)
        {
            foreach (var item in task.RertunValues)
            {
                AddReturn(item.Key, item.Value);
            }
            return this;
        }
    }

    public static class Util
    {
        public static List<ExecuterTask> CurrentTask = new List<ExecuterTask>();
        public static Dictionary<string, List<object>> Messages = new Dictionary<string, List<object>>();
        public static Dictionary<string, object> Global = new Dictionary<string, object>();
        public static Dictionary<string, Func<Task, TaskResult, TaskResult>> Commands = new Dictionary<string, Func<Task, TaskResult, TaskResult>>();
    }

    public class Task
    {
        public Task()
        {
            Id = GetHashCode();
            LoadParams();
        }
        public Task(string name)
        {
            Id = GetHashCode();
            Name = name;
            LoadParams();
        }
        public Task(string name, string command)
        {
            Id = GetHashCode();
            Name = name;
            Command = command;
            LoadParams();
        }
        public Task(string name, string command, params string[] args)
        {
            Id = GetHashCode();
            Name = name;
            Command = command;
            Params = args.ToList();
            LoadParams();
        }
        public int Id { get; private set; }
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    var random = new Random(Id);
                    return (random.Next(0, 99999999) + random.GetHashCode() + Id).ToString();
                }

                return _name;
            }
            set { _name = value; }
        }
        public string ParentName { get; set; }
        public string Value { get; set; }
        public List<string> Params { get {
                LoadParams();
                return _params;
            } set {
                _params = value;
                LoadParams();
            }
        }
        internal Func<Task, TaskResult, TaskResult> Func { get { return Util.Commands[Command]; } }
        public string Command { get; set; }
        private List<Task> _childs { get; set; } = new List<Task>();
        public void Add(Task task)
        {
            var addtask = new Task
            {
                Name = task.Name,
                Value = task.Value,
                Command = task.Command,
                Params = task.Params.ToArray().ToList(), //Tira a referência
                _childs = task.Childs,
                ParentName = Name
            };

            addtask.Id = addtask.GetHashCode();
        
            if (addtask.Params.Count <= 0) addtask.Params.Add("\\"+Name);
            _childs.Add(addtask);
        }
        public List<Task> Childs { get { return _childs; } }
        internal TaskResult Execute(TaskResult result)
        {
            return Func(this, result);
        }
        private void LoadParams()
        {
            var list = new List<string>();
            var db = new FOrDB.FOrDB("TaskManager");

            foreach (var item in _params)
            {
                if (item.StartsWith("%"))
                {
                    var teste = db.Tables["Resources"].Load().First(f => f.Key == item.TrimStart('%')).Value;
                    var subList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(teste.ToString());

                    foreach (var subitem in subList)
                    {
                        list.Add(subitem);
                    }
                }
                else
                {
                    list.Add(item);
                }
            }
            _params = list;
        }

        private string _name;
        private List<string> _params  = new List<string>();
    }

    public class SimpleTask : Task
    {
        public SimpleTask(string name, string command, string value)
        {
            Name = name;
            Command = command;
            Value = value;
        }
    }

    public class ExecuterTask : Task
    {
        public DateTime NextExecute { get; set; }
        public ExecuterTask()
        {
            Command = "CommandExecuter";
        }
        public ExecuterTask(string name)
        {
            Name = name;
            Command = "CommandExecuter";
        }
        public TaskResult Execute()
        {
            var result = Func(this, new TaskResult(this));
            var returnResult = new TaskResult(this)
            {
                Values = result.RertunValues
            };

            return returnResult;
        }
    }
}
