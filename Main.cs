using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using ConnectivityMonitor.Properties;
using static System.Windows.Forms.AxHost;
using System.Reflection;

namespace ConnectivityMonitor
{
    public partial class Main : Form
    {
        public int VisibleStatus = 0;
        public string DowntimeStart;
        public string DowntimeStop;
        public int DowntimeDuration = 0;
        public string Shift;
        public static IPAddress LocalIpAddress() => Dns.GetHostEntry(Dns.GetHostName()).AddressList.LastOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString().StartsWith("172."));
        public Main()
        {
            InitializeComponent();
            timer1 = new Timer(); timer1.Tick += new EventHandler(CheckIfIamOnline); timer1.Interval = 3000; timer1.Start();
            timer2 = new Timer(); timer2.Tick += new EventHandler(CheckHowLongIamBeingOffline); timer2.Interval = 3000;
        }
        private void CheckIfIamOnline(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection sqlCon = new SqlConnection(Connection.ConnectionString))
                {
                    sqlCon.Open(); SqlCommand sqlCmd = new SqlCommand("PingMe", sqlCon);
                    sqlCmd.CommandType = CommandType.StoredProcedure; sqlCmd.CommandTimeout = 2; // 2X timer + timeout = time takes to trigger state change
                    sqlCmd.ExecuteNonQuery();
                    label2.Text = "IP: " + LocalIpAddress().ToString(); label3.Text = "PC: " + Environment.MachineName;
                    button1.BackColor = Color.Green; button1.Text = "Online"; label1.Text = "Status: Connected";
                }
            }
            catch (Exception)
            {
                button1.BackColor = Color.Red; button1.Text = "Offline"; label1.Text = "Status: Disconnected"; timer1.Stop();
                DowntimeStart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); ; timer2.Start();
            }
        }
        private void CheckHowLongIamBeingOffline(object sender, EventArgs e)
        {
            DowntimeStop = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); ;
            if (DateTime.Now >= Convert.ToDateTime("06:00:00") && DateTime.Now < Convert.ToDateTime("14:00:00")) { Shift = "Morning"; }
            else if (DateTime.Now >= Convert.ToDateTime("14:00:00") && DateTime.Now < Convert.ToDateTime("22:00:00")) { Shift = "Afternoon"; }
            else if (DateTime.Now >= Convert.ToDateTime("22:00:00") || DateTime.Now < Convert.ToDateTime("06:00:00")) { Shift = "Night"; }

            try
            {
                using (SqlConnection sqlCon = new SqlConnection(Connection.ConnectionString))
                {
                    sqlCon.Open(); SqlCommand sqlCmd = new SqlCommand("PingMe", sqlCon);
                    sqlCmd.CommandType = CommandType.StoredProcedure; sqlCmd.CommandTimeout = 2; // 2X timer + timeout = time takes to trigger state change
                    sqlCmd.ExecuteNonQuery();

                    button1.BackColor = Color.Green; button1.Text = "Online"; timer2.Stop();

                    DateTime Stop = Convert.ToDateTime(DowntimeStop); DateTime Start = Convert.ToDateTime(DowntimeStart);
                    TimeSpan Duration = Convert.ToDateTime(DowntimeStop) - Convert.ToDateTime(DowntimeStart);
                    string Seconds = Duration.TotalSeconds.ToString(); DowntimeDuration = Convert.ToInt32(Seconds);

                    string query = "insert into Connectivity_monitor (PC,IP,DowntimeDuration,DowntimeStart,DowntimeStop,Shift)values('" + Environment.MachineName + "','" + LocalIpAddress().ToString() + "','" + DowntimeDuration + "','" + Start.ToString("yyyy-MM-dd HH:mm:ss") + "','" + Stop.ToString("yyyy-MM-dd HH:mm:ss") + "','" + Shift.ToString() + "')";
                    SqlCommand cmd = new SqlCommand(query, sqlCon); cmd.ExecuteNonQuery();

                    label1.Text = "Status: Reconnected, connection downtime recorded";
                    timer1.Start();
                }
            }
            catch (Exception)
            {
                button1.BackColor = Color.Red; button1.Text = "Offline"; label1.Text = "Status: Disconnected, measuring downtime duration";
            }
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            Hide();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (VisibleStatus == 0)
                { Show(); WindowState = FormWindowState.Normal; VisibleStatus = 1; }
                else { Hide(); VisibleStatus = 0; }
            }
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide(); VisibleStatus = 0;
            }
        }
    }
}
