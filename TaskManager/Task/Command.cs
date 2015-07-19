using Front.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace TaskManager.Task
{
    static class Command
    {
        public static void LoadCommands()
        {
            Util.Commands.Add("ConsoleWriteLine", ConsoleWriteLine);
            Util.Commands.Add("CommandExecuter", CommandExecuter);
            Util.Commands.Add("ConnectDataBase", ConnectDataBase);
            Util.Commands.Add("CheckByProcess", CheckByProcess);
            Util.Commands.Add("CompareString", CompareString);
            Util.Commands.Add("CompareNumber", CompareNumber);
            Util.Commands.Add("ChildOnFalse", ChildOnFalse);
            Util.Commands.Add("ChildOnTrue", ChildOnTrue);
            Util.Commands.Add("CreateTable", CreateTable);
            Util.Commands.Add("AddIntoList", AddIntoList);
            Util.Commands.Add("SetResource", SetResource);
            Util.Commands.Add("ForEachItem", ForEachItem);
            Util.Commands.Add("ForEachRow", ForEachRow);
            Util.Commands.Add("CreateList", CreateList);
            Util.Commands.Add("SendEmail", SendEmail);
            Util.Commands.Add("FromFixed", FromFixed);
            Util.Commands.Add("FromItem", FromItem);
            Util.Commands.Add("QuerySql", QuerySql);
            Util.Commands.Add("FromRow", FromRow);
            Util.Commands.Add("Replace", Replace);
            Util.Commands.Add("Retask", Retask);
            Util.Commands.Add("Return", Return);
            Util.Commands.Add("Shell", Shell);
            Util.Commands.Add("Kill", Kill);
            Util.Commands.Add("Ping", Ping);
        }

        static internal TaskResult CommandExecuter(Task task, TaskResult result)
        {
            return ExecuteAllChild(task, result);
        }
        static internal TaskResult ConnectDataBase(Task task, TaskResult result)
        {
            var value = new DataBase(Get(task, 0, result).ToString());
            return Next(task, value, result);
        }
        static internal TaskResult CreateList(Task task, TaskResult result)
        {
            return Next(task, new List<object>(), result);
        }
        static internal TaskResult AddIntoList(Task task, TaskResult result)
        {
            ((List<object>)Get(task, 0, result)).Add(Get(task, 1, result));
            ExecuteAllChild(task, result);
            return result;
        }
        static internal TaskResult QuerySql(Task task, TaskResult result)
        {
            var value = ((DataBase)Get(task, 0, result)).CreateQuery(Get(task, 1, result).ToString());
            return Next(task, value, result);
        }
        static internal TaskResult CreateTable(Task task, TaskResult result)
        {
            var value = ((Query)Get(task, 0, result)).Execute<DataTable>();
            return Next(task, value, result);
        }
        static internal TaskResult ForEachRow(Task task, TaskResult result)
        {
            foreach (DataRow value in ((DataTable)Get(task, 0, result)).Rows)
            {
                result.Add(task.Name, value);
                ExecuteAllChild(task, result);
                result.Values.Remove(task.Name);
            }
            return result;
        }
        static internal TaskResult ForEachItem(Task task, TaskResult result)
        {
            foreach (var value in ((List<object>)Get(task, 0, result)))
            {
                result.Add(task.Name, value);
                ExecuteAllChild(task, result);
                result.Values.Remove(task.Name);
            }
            return result;
        }
        static internal TaskResult ConsoleWriteLine(Task task, TaskResult result)
        {
            Console.WriteLine(Get(task, 0, result).ToString());
            return result;
        }
        static internal TaskResult FromRow(Task task, TaskResult result)
        {
            var value = ((DataRow)Get(task, 0, result))[Get(task, 1, result).ToString()];
            return Next(task, value, result);
        }
        static internal TaskResult FromItem(Task task, TaskResult result)
        {
            return Next(task, Get(task, 0, result), result);
        }
        static internal TaskResult FromFixed(Task task, TaskResult result)
        {
            return Next(task, task.Value, result);
        }
        static internal TaskResult Replace(Task task, TaskResult result)
        {
            var value = Get(task, 0, result).ToString().Replace(Get(task, 1, result).ToString(), Get(task, 2, result).ToString());
            return Next(task, value, result);
        }
        static internal TaskResult Return(Task task, TaskResult result)
        {
            for (int i = 0; i < task.Params.Count; i++)
            {
                result.AddReturn(task.Name + i, Get(task, i, result));
            }
            ExecuteAllChild(task, result);
            return result;
        }
        static internal TaskResult Shell(Task task, TaskResult result)
        {
            if (task.Params.Count <= 1)
            {
                Process.Start(Get(task, 0, result).ToString());
            }
            else
            {
                Process.Start(Get(task, 0, result).ToString(), Get(task, 1, result).ToString());
            }

            ExecuteAllChild(task, result);
            return result;
        }
        static internal TaskResult Kill(Task task, TaskResult result)
        {
            Process.GetProcessesByName(Get(task, 0, result).ToString()).ToList().ForEach(f => f.Kill());
            ExecuteAllChild(task, result);
            return result;
        }
        static internal TaskResult Ping(Task task, TaskResult result)
        {
            var pingResult = new Ping().Send(Get(task, 0, result).ToString()).Status == IPStatus.Success;
            return Next(task, pingResult, result);
        }
        static internal TaskResult ChildOnTrue(Task task, TaskResult result)
        {
            if ((bool)Get(task, 0, result))
                ExecuteAllChild(task, result);

            return result;
        }
        static internal TaskResult ChildOnFalse(Task task, TaskResult result)
        {
            if (!(bool)Get(task, 0, result))
                ExecuteAllChild(task, result);

            return result;
        }
        static internal TaskResult CompareString(Task task, TaskResult result)
        {
            var value = false;
            switch (Get(task, 2, result).ToString())
            {
                case "==":
                    value = Get(task, 0, result).ToString() == Get(task, 1, result).ToString();
                    break;
                case "!=":
                    value = Get(task, 0, result).ToString() != Get(task, 1, result).ToString();
                    break;
                default:
                    break;
            }
            return Next(task, value, result);
        }
        static internal TaskResult CompareNumber(Task task, TaskResult result)
        {
            var value = false;
            switch (Get(task, 2, result).ToString())
            {
                case "==":
                    value = (float)Get(task, 0, result) == (float)Get(task, 1, result);
                    break;
                case "!=":
                    value = (float)Get(task, 0, result) != (float)Get(task, 1, result);
                    break;
                case ">=":
                    value = (float)Get(task, 0, result) >= (float)Get(task, 1, result);
                    break;
                case "<=":
                    value = (float)Get(task, 0, result) <= (float)Get(task, 1, result);
                    break;
                case ">":
                    value = (float)Get(task, 0, result) > (float)Get(task, 1, result);
                    break;
                case "<":
                    value = (float)Get(task, 0, result) < (float)Get(task, 1, result);
                    break;
                default:
                    break;
            }
            return Next(task, value, result);
        }
        static internal TaskResult CheckByProcess(Task task, TaskResult result)
        {
            var value = false;
            var process = Get(task, 0, result).ToString();
            var ipAddress = "localhost";

            if (task.Params.Count > 1)
            {
                ipAddress = Get(task, 1, result).ToString();
                ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Any(a => a.ToString().Equals(ipAddress)) ? "localhost" : ipAddress;
            }
            
            if (ipAddress.ToLower().Equals("localhost"))
            {
                value = Process.GetProcessesByName(process).Length > 0;
            }
            else
            {
                value = CheckProcessIntoRemoteMachine(process, ipAddress, Get(task, 2, result).ToString(), Get(task, 3, result).ToString());
            }
            return Next(task, value, result);
        }
        static internal TaskResult Retask(Task task, TaskResult result) //Tarefa para reagendamento da ExecuterTask
        {
            var seconds = 0;
            var minutes = 0;
            var hours = 0;
            var days = 0;

            if (task.Params.Count > 0) seconds = Convert.ToInt32(Get(task, 0, result));
            if (task.Params.Count > 1) minutes = Convert.ToInt32(Get(task, 1, result));
            if (task.Params.Count > 2) hours = Convert.ToInt32(Get(task, 2, result));
            if (task.Params.Count > 3) days = Convert.ToInt32(Get(task, 3, result));

            var timeSpan = new TimeSpan(days, hours, minutes, seconds);

            result.ExecuterTask.NextExecute = DateTime.Now.Add(timeSpan);

            var db = new FOrDB.FOrDB("TaskManager");
            db.Tables["Task"].Insert(result.ExecuterTask);

            ExecuteAllChild(task, result);
            return result;
        }
        static internal TaskResult SendEmail(Task task, TaskResult result)
        {
            var mail = new MailMessage(Get(task, 0, result).ToString(), Get(task, 1, result).ToString());
            var client = new SmtpClient();
            client.EnableSsl = Convert.ToBoolean(Get(task, 2, result));
            client.Port = Convert.ToInt32(Get(task, 3, result));
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            //client.UseDefaultCredentials = false;
            client.Host = Get(task, 4, result).ToString();
            client.Credentials = new NetworkCredential(Get(task, 5, result).ToString(), Get(task, 6, result).ToString());
            mail.Subject = Get(task, 7, result).ToString();
            mail.Body = Get(task, 8, result).ToString() + Environment.NewLine + Environment.NewLine + "---" + Environment.NewLine + "E-Mail enviado através do TaskManager (" +Environment.MachineName + ")";
            client.Send(mail);

            ExecuteAllChild(task, result);
            return result;
        }
        static internal TaskResult SetResource(Task task, TaskResult result)
        {
            var list = task.Params.ToList();
            list.RemoveAt(0);

            var db = new FOrDB.FOrDB("TaskManager");
            db.Tables["Resources"].Insert(Get(task, 0, result).ToString(), list);
            ExecuteAllChild(task, result);
            return result;
        }
        ///////>   Falta testar

        static internal TaskResult SetGlobal(Task task, TaskResult result)
        {
            var db = new FOrDB.FOrDB("TaskManager");
            db.Tables["Global"].Update(task.Name, Get(task, 0, result));
            ExecuteAllChild(task, result);
            return result;
        }
        static internal TaskResult GetGlobal(Task task, TaskResult result)
        {
            var db = new FOrDB.FOrDB("TaskManager");
            var value = db.Tables["Global"].Load().First(w => w.Key == task.Name).Value;
            return Next(task, value, result);
        }

        ///////>   Falta desenvolver

        static internal TaskResult SendMessage(Task task, TaskResult result)
        {
            throw new NotImplementedException();
        } //Envia mensagem para algum programa específico
        static internal TaskResult SetMessage(Task task, TaskResult result)
        {
            if (!Util.Global.ContainsKey(task.Name)) Util.Global.Add(task.Name, null);
            ((List<object>)Util.Global[task.Name]).Add(Get(task, 0, result));
            ExecuteAllChild(task, result);
            return result;
        }
        static internal TaskResult GetMessage(Task task, TaskResult result)
        {
            if (((List<object>)Util.Global[task.Name]).Count > 0)
            {
                var value = ((List<object>)Util.Global[task.Name])[0];
                ((List<object>)Util.Global[task.Name]).RemoveAt(0);
                return Next(task, value, result);
            }

            ExecuteAllChild(task, result);
            return result;

        }

        ///////>   Funções Não Comandos     /////////////////////////////////////////////

        static internal TaskResult ExecuteAllChild(Task task, TaskResult result)
        {
            foreach (var child in task.Childs)
            {
                child.Execute(result);
            }
            return result;
        }
        static internal TaskResult Next(Task task, object value, TaskResult result)
        {
            result.Add(task.Name, value);
            ExecuteAllChild(task, result);
            return result;
        }
        static internal bool CheckProcessIntoRemoteMachine(string process, string ipAddress, string username, string password)
        {
            var value = false;

            //Configurações do computador remoto
            var connectoptions = new ConnectionOptions();
            connectoptions.Username = username;
            connectoptions.Password = password;

            var scope = new ManagementScope(@"\\" + ipAddress + @"\root\cimv2");
            scope.Options = connectoptions;

            //Define query que consulta os processo na máquina remota
            var query = new SelectQuery("select * from Win32_Process where name = '" + process + "'");

            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                var collection = searcher.Get();
                foreach (ManagementObject item in collection)
                {
                    value = item["Name"].ToString() == process;
                    if (value) break;
                }
            }
            return value;
        }
        static internal object Get(Task task, int index, TaskResult result)
        {
            if (task.Params[index].StartsWith("\\"))
            {
                return result.Values[task.Params[index].TrimStart('\\')];
            }
            else if (task.Params[index].StartsWith("@"))
            {
                return Util.Global[task.Params[index].TrimStart('@')];
            }
            else if (task.Params[index].Equals("#"))
            {
                return result.Values[task.ParentName];
            }

            return task.Params[index];
        }
    }
}
