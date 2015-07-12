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

            _client.Start(false, true);
            
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

            InitialToTask();

            //var tkExe = new ExecuterTask("TesteEmail");
            //var tkMail = new Task.Task(null, "SendEmail", "%Email");
            //tkExe.Add(tkMail);

            //db.Tables["Task_On_Open"].Insert(tkExe);

            _trdExecuter = new Thread(Executer);
            _trdExecuter.Start();

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

            _status = status;

            switch (status)
            {
                case Status.Starting:
                    _running = true;
                    lblStatus.BackColor = Color.FromArgb(64, 64, 64);
                    lblStatus.ForeColor = Color.White;
                    lblStatus.Text = "Starting";
                    btnRun.Text = "Stop";
                    break;
                case Status.Running:
                    _running = true;
                    lblStatus.BackColor = Color.Lime;
                    lblStatus.ForeColor = Color.White;
                    lblStatus.Text = "Running";
                    btnRun.Text = "Stop";
                    break;
                case Status.Stopped:
                    _running = false;
                    lblStatus.BackColor = Color.FromArgb(64, 64, 64);
                    lblStatus.ForeColor = Color.White;
                    lblStatus.Text = "Stopped";
                    btnRun.Text = "Run";
                    break;
                case Status.Error:
                    SetStatus(Status.Stopped);
                    lblStatus.BackColor = Color.Red;
                    lblStatus.ForeColor = Color.White;
                    lblStatus.Text = "Error";
                    btnRun.Text = "Run";
                    break;
                default:
                    break;
            }
        }

        private void trmRefresh_Tick(object sender, EventArgs e)
        {
            
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            SetStatus(_status == Status.Stopped ? Status.Running : Status.Stopped);
        }
    }
}
