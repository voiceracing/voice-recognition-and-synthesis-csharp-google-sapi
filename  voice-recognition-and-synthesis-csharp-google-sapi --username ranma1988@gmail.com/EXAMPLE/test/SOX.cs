using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Diagnostics;

namespace test
{
    class SOX
    {
        string SOX_PATH = "";

        public SOX(string install_dir)
        {
            if (!System.IO.Directory.Exists(install_dir))
            {
                throw new Exception(install_dir+" is not a directory");
            }

            if (!install_dir.EndsWith(@"\")) install_dir += @"\";

            SOX_PATH = install_dir;


            if (!System.IO.File.Exists(SOX_PATH + "sox.exe"))
            {
                throw new Exception("sox path incorrect: '"+SOX_PATH + "sox.exe' is missing");
            }

                //jeżeli nie było rec.exe to go zrób
                if (!System.IO.File.Exists(SOX_PATH + "rec.exe"))
                    System.IO.File.Copy(SOX_PATH + "sox.exe", SOX_PATH + "rec.exe");
        }


        public void Record(string WAV_fileName,int TimeOutMS)
        {
            Execute(SOX_PATH + "rec.exe",
                    "-r 16000 -b 16 -c 1 " + WAV_fileName, 
                    TimeOutMS);   
        }

        public void Record_smart(string WAV_fileName)
        {
            string s = 
                    WAV_fileName+
                    " silence 1 0.25 0.5% 1 0.5 1.0%"+
                    " -r 16000 -b 16 -c 1 ";
            Execute(SOX_PATH + "rec.exe",
                s,
                    10*1000);
        }

        public void Convert(string WAV_fileName,string Flac_FileName)
        {
            Execute(SOX_PATH + "sox.exe",
                    WAV_fileName+" "+Flac_FileName+" gain -n -5 silence 1 5 2%",
                    10*1000);
        }

        /* don't play mp3:/
        public void Play(string FileName)
        {
            Execute(SOX_PATH + "sox.exe",
                    FileName,
                    10 * 1000);
        }
         */

        /// <summary>
        /// Executes a shell command synchronously.
        /// </summary>
        /// <param name="command">string command</param>
        /// <returns>string, as output of the command.</returns>
        public static TimeSpan Execute(string FileName,string args,int TimeOutMS)
        {
            DateTime Timestart = DateTime.MinValue;

            try
            {
                int ProcessIDmem = 0;

                using (Process p = new Process())
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;  //Redirect the output stream of the child process.
                    p.StartInfo.CreateNoWindow = true;          //no console window
                    p.StartInfo.FileName = FileName;            //exe file
                    p.StartInfo.Arguments = args;               //program start arguments
                    p.Start();// Start the child process.
                    Timestart = DateTime.Now;
                    ProcessIDmem = p.Id;
          
                    p.WaitForExit(TimeOutMS);//wait max time
                    
                    p.Kill();//and done

                }//force done ;) no more problems
            }
            catch (Exception)
            {
                // Log the exception
            }

            if (Timestart == DateTime.MinValue)
                return TimeSpan.MinValue;

            return DateTime.Now - Timestart;
        }
    }
}
