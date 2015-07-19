using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskManager.Task;
using TaskManager.Task.FOrDB;
using TCPCom;

namespace TaskManager
{
    public partial class frmMain : Form
    {

        TCPServer _server = new TCPServer(9999);
        TCPClient _client = new TCPClient("192.168.25.2", 9999);
        FOrDB db = new FOrDB("TaskManager");
        Thread _trdExecuter;
        private bool _closing;
        private bool _running;
        private Status _status;

        private List<string> InputList = new List<string>();
        private int indexInput;

        private enum Status
        {
            Starting,
            Running,
            Stopped,
            Error,
            ErrorOk
        }

        public frmMain()
        {
            InitializeComponent();

            _server.Accepting = true;
            _server.MaxClient = 100;
            _server.DataReceived_Handler += OnReceiveData;
            _server.ClientAccepted_Handler += OnAccept;
            _server.Start();

            //_client.Start(false, true);
            CheckForIllegalCrossThreadCalls = false;
            
            Initial();
            
        }

        private void OnAccept(Client e)
        {
            Console.WriteLine(e);
        }

        private void OnReceiveData(Client e, string Data, bool Succesfull)
        {
            var task = Newtonsoft.Json.JsonConvert.DeserializeObject<ExecuterTask>(Data);
            db.Tables["Task"].Insert(task);
        }

        private void Initial()
        {
            SetStatus(Status.Starting);

            Command.LoadCommands();

            db.CreateTable("Task_On_Open");
            db.CreateTable("Task");
            db.CreateTable("Task_Reg");
            db.CreateTable("Internal_Task");
            db.CreateTable("Resources");
            db.Tables["Task"].Clear();
            db.Tables["Global"].Clear();

            InitialToTask();

            ReadLine();

            _trdExecuter = new Thread(Executer);
            _trdExecuter.Start();

            tbxInput.Focus();
            tbxInput.Select();
        }

        private void InitialToTask()
        {
            var list = db.Tables["Task_On_Open"].Load<ExecuterTask>();
            foreach (var item in list)
            {
                db.Tables["Task"].Insert(item.Value);
            }
        }

        private void Executer()
        {
            while (!_closing)
            {
                while (_running)
                {
                    SetStatus(Status.Running);
                    var list = db.Tables["Task"].Load<ExecuterTask>().Where(w => w.Value.NextExecute <= DateTime.Now).ToArray();

                    foreach (var item in list)
                    {
                        item.Value.Execute();
                        //db.Tables["Task_Reg"].Insert(item.Value);
                        db.Tables["Task"].Delete(item);
                    }

                    Thread.Sleep(400);
                }
                Thread.Sleep(1000);
            }
        }

        private void SetStatus(Status status, Status statusBeforeError = Status.Running)
        {
            if (_status == Status.Error && status != Status.ErrorOk) return;
            if (status == Status.ErrorOk) status = statusBeforeError;

            var showOutput = _status != status;

            _status = status;

            switch (status)
            {
                case Status.Starting:
                    _running = true;
                    lblStatus.BackColor = Color.FromArgb(64, 64, 64);
                    lblStatus.ForeColor = Color.White;
                    lblStatus.Text = "Starting";
                    btnRun.Text = "Stop";
                    if(showOutput) WriteOutput("Iniciando");
                    break;
                case Status.Running:
                    _running = true;
                    lblStatus.BackColor = Color.Lime;
                    lblStatus.ForeColor = Color.White;
                    lblStatus.Text = "Running";
                    btnRun.Text = "Stop";
                    if (showOutput) WriteOutput("Executando");
                    break;
                case Status.Stopped:
                    _running = false;
                    lblStatus.BackColor = Color.FromArgb(64, 64, 64);
                    lblStatus.ForeColor = Color.White;
                    lblStatus.Text = "Stopped";
                    btnRun.Text = "Run";
                    if (showOutput) WriteOutput("Parado");
                    break;
                case Status.Error:
                    SetStatus(Status.Stopped);
                    lblStatus.BackColor = Color.Red;
                    lblStatus.ForeColor = Color.White;
                    lblStatus.Text = "Error";
                    btnRun.Text = "Run";
                    if (showOutput) WriteOutput("Erro");
                    break;
                default:
                    break;
            }

            if (showOutput) TaskGenerator("SetGlobal TaskStatus " + _status.ToString());
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetStatus(Status.Stopped);
            _closing = true;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            SetStatus(_status == Status.Stopped ? Status.Running : Status.Stopped);
        }

        private void ReadLine()
        {
            tbxInput.Text = "";
        }
        private void WriteOutput(string message)
        {
            tbxOutput.Text += message;
            tbxOutput.Text += Environment.NewLine;

            tbxOutput.SelectionStart = tbxOutput.Text.Length;
            tbxOutput.ScrollToCaret();
        }
        private void Cmd(string cmd)
        {
            WriteOutput("Comando: " + cmd);

            switch (cmd)
            {
                case "Run":
                    SetStatus(Status.Running);
                    break;
                case "Stop":
                    SetStatus(Status.Stopped);
                    break;
                default:
                    TaskGenerator(cmd);
                    break;
            }
            if(!InputList.Contains(cmd)) InputList.Add(cmd);
            ReadLine();
        }
        private void TaskGenerator(string cmdString)
        {
            var tkExe = new ExecuterTask("Executer");

            var cmds = cmdString.Split((":").ToArray(), StringSplitOptions.RemoveEmptyEntries);

            Task.Task tk;
            Task.Task tkFather = tkExe;
            Task.Task tkFatherF = tkExe;

            var isBrother = false;

            foreach (var cmd in cmds)
            {
                var args = cmd.Split((" ").ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                var exe = args[0];
                isBrother = exe.StartsWith(">");
                exe = exe.TrimStart('>');
                args.RemoveAt(0);
                var name = DateTime.Now.Ticks.ToString();
                if (args.Count > 0)
                {
                    name = args[0];
                    if (args.Count > 1) { args.RemoveAt(0); }
                }
                tk = new Task.Task(name,exe, args.ToArray());
                if(isBrother)
                    tkFatherF.Add(tk);
                else
                    tkFather.Add(tk);
                tkFatherF = tkFather;
                tkFather = tk;
            }

            try
            {
                var result = tkExe.Execute();
                foreach (var item in result.Values)
                {
                    WriteOutput(": " + item.Value.ToString());
                }
                
            }
            catch (Exception ex)
            {
                WriteOutput("Não foi possível realizar comando: " + ex.Message);
            }
        }

        private void tbxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                if (string.IsNullOrWhiteSpace(tbxInput.Text)) return;
                Cmd(tbxInput.Text.TrimEnd('\n'));
                e.Handled = true;
            }
            else if (e.KeyData == Keys.Up)
            {
                indexInput--;
                indexInput = indexInput < 0 ? 0 : indexInput;
                if (InputList.Count - 1 >= indexInput)
                    tbxInput.Text = InputList[indexInput];
                e.Handled = true;
            }
            else if (e.KeyData == Keys.Down)
            {
                indexInput++;
                indexInput = indexInput > InputList.Count ? InputList.Count : indexInput;
                if (InputList.Count - 1 >= indexInput)
                    tbxInput.Text = InputList[indexInput];
                else
                    tbxInput.Text = "";
                e.Handled = true;
            }
        }
    }
}
