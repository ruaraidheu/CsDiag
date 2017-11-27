using Microsoft.CSharp;
using Microsoft.Win32;
using Ruaraidheulib;
using Ruaraidheulib.Console;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Ruaraidheulib.List;

namespace CsDiag
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Compile
            string filename = "generic";
            TimeSpan maxtargettime = new TimeSpan(0, 0, 0, 0, 10);
            string loadpath = "reference.ovr";
            if (args.Length > 0)
            {
                loadpath = args[0];
            }
            bool foundcode = false;
            int depth = 10;
            string loadname = "reference";
            string exten = ".ovr";
            string pth = Environment.CurrentDirectory;
            while (!foundcode)
            {
                if (loadpath.IsStartAndRemove(out loadpath, "::"))
                {
                    loadpath = loadpath.Replace("/", "\\");
                    string[] fp = loadpath.Split(".");
                    if (fp.Length >= 2)
                    {
                        exten = "." + fp.Last();
                        loadpath = loadpath.IfEndAndRemove(exten);
                        fp = new string[0];
                        fp = loadpath.Split("\\");
                        if (fp.Length >= 2)
                        {
                            loadname = fp.Last();
                            loadpath = loadpath.IfEndAndRemove("\\" + loadname);
                            pth += "\\"+loadpath;
                        }
                        else
                        {
                            loadname = loadpath;
                        }
                    }
                    else
                    {
                        fp = new string[0];
                        fp = loadpath.Split("\\");
                        if (fp.Length >= 2)
                        {
                            loadname = fp.Last();
                            loadpath = loadpath.IfEndAndRemove("\\" + loadname);
                            pth += "\\"+loadpath;
                        }
                        else
                        {
                            loadname = loadpath;
                        }
                    }
                }
                else
                {
                    loadpath = loadpath.Replace("/", "\\");
                    string[] fp = loadpath.Split(".");
                    if (fp.Length >= 2)
                    {
                        exten = "." + fp.Last();
                        loadpath = loadpath.IfEndAndRemove(exten);
                        fp = new string[0];
                        fp = loadpath.Split("\\");
                        if (fp.Length >= 2)
                        {
                            loadname = fp.Last();
                            loadpath = loadpath.IfEndAndRemove("\\" + loadname);
                            pth = loadpath;
                        }
                        else
                        {
                            loadname = loadpath;
                        }
                    }
                    else
                    {
                        fp = new string[0];
                        fp = loadpath.Split("\\");
                        if (fp.Length >= 2)
                        {
                            loadname = fp.Last();
                            loadpath = loadpath.IfEndAndRemove("\\" + loadname);
                            pth = loadpath;
                        }
                        else
                        {
                            loadname = loadpath;
                        }
                    }
                }

                if (exten != ".ovr"&&exten != ".csdref")
                {
                    foundcode = true;
                }
                else
                {
                    string[] tmpl = File.ReadAllLines(pth + "\\" + loadname + exten);
                    if (tmpl.Length >= 1)
                    {
                        loadpath = tmpl[0];
                    }
                }
                if(depth < 1)
                {
                    throw new LoopLengthException();
                }
                depth--;
            }
            string code = File.ReadAllText(pth+"\\"+loadname+exten);
            List<string> refassemblies = new List<string>();
            if (TagDecoder.IsTag(code, "lib"))
            {
                string[] data = TagDecoder.WTags(code, "lib", true).Split('|');
                foreach(string strd in data)
                {
                    refassemblies.Add(strd);
                }
                code = TagDecoder.StripTags(code, "lib");
            }
            string namesp = "CSCT";
            string cla = "Generic";
            string method = "CSD";
            if (TagDecoder.IsTag(code, "detail"))
            {
                string[] data = TagDecoder.WTags(code, "detail", true).Split('|');
                if (data.Length == 3)
                {
                    namesp = data[0];
                    cla = data[1];
                    method = data[2];
                }
                code = TagDecoder.StripTags(code, "detail");
            }
            filename = cla;
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("Ruaraidheulib.dll");
            parameters.ReferencedAssemblies.Add("CsDiag.exe");
            foreach(string sref in refassemblies)
            {
                parameters.ReferencedAssemblies.Add(sref);
            }

            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);
            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(System.String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }

                throw new InvalidOperationException(sb.ToString());
            }
            Assembly assembly = results.CompiledAssembly;
            Type program = assembly.GetType(namesp+"."+cla);
            MethodInfo main = program.GetMethod(method);
            #endregion
            #region Init
            Save save = new Save();
            Exception kill = null;
            string dat = "";
            TimeSpan usertime = new TimeSpan();
            TimeSpan ignoredtime = new TimeSpan();
            StopLap sl = new StopLap();
            Errorhandling eh = new Errorhandling(save, sl);
            sl.ResetS();
            save.SaveLine("--------START_OF_TEST--------");
            sl.StartS();
            try
            {
            #endregion
            dat = (string)main.Invoke(null, new object[] { save, sl, eh });
            #region Post
                sl.IgnoreTimeClick("Return");
            }
            catch (Exception e)
            {
                kill = e;
                save.FastLine("PROGRAM_FORCIBLY_TERMINATED_DUE_TO_FATAL_EXCEPTION");
            }
            sl.StopS();
            if (kill == null)
            {
                save.SaveLine("---------END_OF_TEST---------");
            }
            else
            {
                save.SaveLine("-------TEST_TERMINATED-------");
            }
            save.SaveLine("");
            save.SaveLine("COMPILING_DIAGNOSTIC");
            dat += eh.GetTags();
            System.Threading.Thread.Sleep(2000);
            save.SaveLine("");
            save.SaveLine("");
            save.SaveLine("");
            #endregion
            #region Diag
            save.SaveLine("------DIAGNOSTIC_REPORT------");
            save.SaveLine("");
            save.SaveLine("");

            //Report

            #region ID
            save.SaveLine("--ID_INFO--");
            save.SaveLine("");
            save.SaveLine("DATE: {0}", DateTime.Now);
            if (TagDecoder.IsTag(dat, "NN"))
                filename = TagDecoder.WTags(dat, "NN");
            if (args.Length > 1)
            {
                filename = args[1];
            }
            save.SaveLine("TEST: " + filename.ToUpper());

            dat = dat.ToUpper();
            if (TagDecoder.IsTag(dat, "DAT"))
            {
                string[] data = TagDecoder.WTags(dat, "DAT", true).Split('|');
                if (data.Length >= 4)
                {
                    save.SaveLine("NAME: " + data[0]);
                    save.SaveLine("REPO: " + data[1]);
                    save.SaveLine("FILE: " + data[2]);
                    save.SaveLine("CLASS: " + data[3]);
                }
            }

            if (TagDecoder.IsTag(dat, "TT"))
                maxtargettime = new TimeSpan(0, 0, 0, 0, TagDecoder.WTags(dat, "TT").ToInt32());

            string[] libs = Directory.GetFiles(Environment.CurrentDirectory, "*.dll");
            Loop.For((i) =>
            {
                string[] p = libs[i].Split('\\');
                string ps = p.Last().ToUpper();
                if (i == 0)
                {
                    save.SaveLine("LIBRARY: " + ps);
                }
                else
                {
                    save.SaveLine("         " + ps);
                }
            }
            , libs.Length);
            if (sl.userclicks > 0)
            {
                save.SaveLine("USER_INPUT: TRUE");
            }
            else
            {
                save.SaveLine("USER_INPUT: FALSE");
            }
            save.SaveLine("TARGET_TIME: {0}", maxtargettime);
            save.SaveLine("");
            save.SaveLine("");
            #endregion
            #region System
            save.SaveLine("--SYSTEM_INFO--");
            save.SaveLine("");
            save.SaveLine("DIRECTORY: " + System.Environment.CurrentDirectory);
            save.SaveLine("OS: " + System.Environment.OSVersion.ToString());
            RegistryKey processor_name = Registry.LocalMachine.OpenSubKey(@"Hardware\Description\System\CentralProcessor\0", RegistryKeyPermissionCheck.ReadSubTree);
            if (processor_name != null)
            {
                if (processor_name.GetValue("ProcessorNameString") != null)
                {
                    save.SaveLine("PROCESSOR: " + processor_name.GetValue("ProcessorNameString").ToString());
                }
            }
            save.SaveLine("PROCESSOR_COUNT: " + System.Environment.ProcessorCount);
            save.SaveLine("MACHINE_NAME: " + System.Environment.MachineName);
            save.SaveLine("IS_64BIT_OS: " + System.Environment.Is64BitOperatingSystem);
            save.SaveLine("IS_64BIT_PROCESS: " + System.Environment.Is64BitProcess);
            save.SaveLine("PAGE_SIZE: " + System.Environment.SystemPageSize);
            save.SaveLine("CLR_VERSION: " + System.Environment.Version);
            save.SaveLine("WORKING_SET: " + System.Environment.WorkingSet);
            save.SaveLine("");
            save.SaveLine("");
            #endregion
            #region Profiling
            save.SaveLine("--PROFILING--");
            save.SaveLine("");
            save.SaveLine("TIME: {0}", sl.s.Elapsed);
            save.SaveLine("TARGET_TIME: {0}", maxtargettime);
            save.SaveLine("MISSING_TIME: {0}", sl.MissingTime());
            for (int i = 0; i < sl.dt.Count; i++)
            {
                if (sl.ds[i].StartsWith("ut:ex"))
                {
                    usertime += sl.dt[i];
                }
            }
            save.SaveLine("USERTIME: {0}", usertime);
            save.SaveLine("USER_ADJUSTED_TOTAL: {0}", sl.s.Elapsed - usertime);
            for (int i = 0; i < sl.dt.Count; i++)
            {
                if (sl.ds[i].StartsWith("ig:ex"))
                {
                    ignoredtime += sl.dt[i];
                }
            }
            save.SaveLine("IGNORED_TIME: {0}", ignoredtime);
            save.SaveLine("IGNORED_ADJUSTED: {0}", sl.s.Elapsed - ignoredtime);
            save.SaveLine("IGNORED_ADJUSTED_TOTAL: {0}", (sl.s.Elapsed - ignoredtime) - usertime);
            save.SaveLine("---");
            List<string> lstr = new List<string>();
            List<Twoint> lint = new List<Twoint>();
            if (TagDecoder.IsTag(dat, "FORCEEDIT"))
            {
                string[] data = TagDecoder.WTags(dat, "FORCEEDIT", true).Split('|');
                for (int i = 0; i < data.Length; i++)
                {
                    string[] d2 = data[i].Split(";+;");
                    string[] d3 = data[i].Split(";-;");
                    if (d2.Length == 2)
                    {
                        Twoint t = new Twoint(-1, -1);
                        Loop.For((j) =>
                        {
                            if (sl.ds[j] == d2[0])
                            {
                                t.X = j;
                            }
                            if (sl.ds[j] == d2[1])
                            {
                                t.Y = j;
                            }
                        }
                        , sl.ds.Count);
                        if (t.X != -1 && t.Y != -1)
                        {
                            lstr.Add("FORCECLICKJOIN-FE:ED(" + sl.ds[t.X].ToUpper() + " + " + sl.ds[t.Y].ToUpper() + "): " + (sl.dt[t.X] + sl.dt[t.Y]).ToString().ToUpper());
                            lint.Add(t);
                        }
                    }
                    else if (d3.Length == 2)
                    {
                        Twoint t = new Twoint(-1, -1);
                        Loop.For((j) =>
                        {
                            if (sl.ds[j] == d3[0])
                            {
                                t.X = j;
                            }
                            if (sl.ds[j] == d3[1])
                            {
                                t.Y = j;
                            }
                        }
                        , sl.ds.Count);
                        if (t.X != -1 && t.Y != -1)
                        {
                            lstr.Add("FORCECLICKSUB-FE:ED(" + sl.ds[t.X].ToUpper() + " - " + sl.ds[t.Y].ToUpper() + "): " + (sl.dt[t.X] - sl.dt[t.Y]).ToString().ToUpper());
                            lint.Add(t);
                        }
                    }
                }
            }
            for (int i = 0; i < sl.dt.Count; i++)
            {
                bool rement = false;
                for (int j = 0; j < lint.Count; j++)
                {
                    if (lint[j].X == i)
                    {
                        rement = true;
                        if (lint[j].X > lint[j].Y)
                        {
                            save.SaveLine(lstr[j]);
                        }
                    }
                    if (lint[j].Y == i)
                    {
                        rement = true;
                        if (lint[j].Y > lint[j].X)
                        {
                            save.SaveLine(lstr[j]);
                        }
                    }
                }
                if (!rement)
                {
                    save.SaveLine("TIMECLICK-{0}: {1}", sl.ds[i].ToUpper(), sl.dt[i]);
                }
            }
            save.SaveLine("---");
            if (TagDecoder.IsTag(dat, "CLICKEDIT"))
            {
                string[] data = TagDecoder.WTags(dat, "CLICKEDIT", true).Split('|');
                for (int i = 0; i < data.Length; i++)
                {
                    string[] d2 = data[i].Split(":+:");
                    string[] d3 = data[i].Split(":-:");
                    if (d2.Length == 2)
                    {
                        Twoint t = new Twoint(-1, -1);
                        Loop.For((j) =>
                        {
                            if (sl.ds[j] == d2[0])
                            {
                                t.X = j;
                            }
                            if (sl.ds[j] == d2[1])
                            {
                                t.Y = j;
                            }
                        }
                        , sl.ds.Count);
                        if (t.X != -1 && t.Y != -1)
                        {
                            save.SaveLine("TIMECLICKJOIN-{0}: {1}", "CE:ED(" + sl.ds[t.X].ToUpper() + " + " + sl.ds[t.Y].ToUpper() + ")", (sl.dt[t.X] + sl.dt[t.Y]).ToString().ToUpper());
                        }
                    }
                    else if (d3.Length == 2)
                    {
                        Twoint t = new Twoint(-1, -1);
                        Loop.For((j) =>
                        {
                            if (sl.ds[j] == d3[0])
                            {
                                t.X = j;
                            }
                            if (sl.ds[j] == d3[1])
                            {
                                t.Y = j;
                            }
                        }
                        , sl.ds.Count);
                        if (t.X != -1 && t.Y != -1)
                        {
                            save.SaveLine("TIMECLICKSUB-{0}: {1}", "CE:ED(" + sl.ds[t.X].ToUpper() + " - " + sl.ds[t.Y].ToUpper() + ")", (sl.dt[t.X] - sl.dt[t.Y]).ToString().ToUpper());
                        }
                    }
                }
            }
            save.SaveLine("");
            save.SaveLine("");
            #endregion
            #region Errors
            save.SaveLine("--ERRORS--");
            save.SaveLine("");

            if (kill != null)
            {
                save.SaveLine("TERMINATION_EXCEPTION: ");
                save.SaveLine("      EXCEPTION: {0}", kill.GetType().ToString().ToUpper());
                save.SaveLine("        MESSAGE: {0}", kill.Message.ToUpper());
                save.SaveLine("    STACK_TRACE: {0}", kill.StackTrace.ToUpper());
                save.SaveLine("        HRESULT: {0}", kill.HResult.ToString().ToUpper());
                if (kill.HelpLink != null)
                    save.SaveLine("       HELPLINK: {0}", kill.HelpLink.ToUpper());
                save.SaveLine("");
            }
            if (TagDecoder.IsTag(dat, "ED"))
            {
                string[] edsec = TagDecoder.WTags(dat, "ED", true).Split('|');
                Loop.For((i) =>
                {
                    string[] eind = edsec[i].Split(":+:");
                    if (eind.Length == 4)
                    {
                        save.SaveLine("HANDLED_EXCEPTION: ");
                        save.SaveLine("      EXCEPTION: {0}", eind[0]);
                        save.SaveLine("        MESSAGE: {0}", eind[1]);
                        save.SaveLine("    STACK_TRACE: {0}", eind[2]);
                        save.SaveLine("        HRESULT: {0}", eind[3]);
                    }
                    else if (eind.Length == 5)
                    {
                        save.SaveLine("HANDLED_EXCEPTION: ");
                        save.SaveLine("      EXCEPTION: {0}", eind[0]);
                        save.SaveLine("        MESSAGE: {0}", eind[1]);
                        save.SaveLine("    STACK_TRACE: {0}", eind[2]);
                        save.SaveLine("        HRESULT: {0}", eind[3]);
                        save.SaveLine("       HELPLINK: {0}", eind[4]);
                    }
                }
                , edsec.Length);
            }

            save.SaveLine("");
            save.SaveLine("");
            #endregion
            #region Results
            save.SaveLine("--RESULTS--");
            save.SaveLine("");
            save.DirectWrite("");

            save.SaveWrite("EXCEPTIONS: ");
            System.Threading.Thread.Sleep(300);
            if (kill == null)
            {
                save.SaveWrite("PASS");
                System.Threading.Thread.Sleep(800);
                save.SaveLine("");
            }
            else
            {
                save.SaveWrite("FAIL");
                System.Threading.Thread.Sleep(800);
                save.SaveLine("");
                save.SaveLine("EXCEPTION={0}", kill.GetType().ToString().ToUpper());
                save.SaveLine("MESSAGE={0}", kill.Message.ToUpper());
                save.SaveLine("STACK_TRACE={0}", kill.StackTrace.ToUpper());
                save.SaveLine("HRESULT={0}", kill.HResult.ToString().ToUpper());
                if (kill.HelpLink != null)
                    save.SaveLine("HELPLINK={0}", kill.HelpLink.ToUpper());
                System.Threading.Thread.Sleep(800);
                save.SaveLine("");
            }

            save.SaveWrite("TIME: ");
            System.Threading.Thread.Sleep(300);
            if (((sl.s.Elapsed - ignoredtime) - usertime) < maxtargettime)
            {
                save.SaveWrite("PASS");
                System.Threading.Thread.Sleep(800);
                save.SaveLine("");
            }
            else
            {
                save.SaveWrite("FAIL");
                System.Threading.Thread.Sleep(800);
                save.SaveLine("");
                save.SaveLine("TARGET_TIME={0}", maxtargettime);
                save.SaveLine("TOTAL_TIME_={0}", sl.s.Elapsed);
                save.SaveLine("USER_ADJUST={0}", sl.s.Elapsed - usertime);
                save.SaveLine("IGNRED_TIME={0}", ignoredtime);
                save.SaveLine("IGNR_ADJUST={0}", sl.s.Elapsed - ignoredtime);
                save.SaveLine("IGNR_TOTAL_={0}", (sl.s.Elapsed - ignoredtime) - usertime);
                System.Threading.Thread.Sleep(800);
                save.SaveLine("");
            }

            //Report End

            save.ExclusiveWrite("");
            save.SaveLine("");
            #endregion
            save.SaveLine("--------END_OF_REPORT--------");

            save.SaveLine("");
            save.SaveLine("");
            save.SaveLine("");

            save.SaveAll(pth + "\\DiagReport-" + filename + ".log");

            save.SaveLine("");
            save.SaveLine("----PRESS_ANY_KEY_TO_EXIT----");
            Console.ReadKey();
            #endregion
        }
    }
    #region Supplementary
    public class Errorhandling
    {
        List<Exception> exc = new List<Exception>();
        bool suppress;
        SuppressionType suppressc;
        Save _s;
        StopLap _sl;
        public Errorhandling(Save s, StopLap sl, bool suppressConsole, SuppressionType suppressClicks)
        {
            suppress = suppressConsole;
            suppressc = suppressClicks;
            _s = s;
            _sl = sl;
        }
        public Errorhandling(Save s, StopLap sl)
        {
            suppress = false;
            suppressc = SuppressionType.StandardClick;
            _s = s;
            _sl = sl;
        }
        public enum SuppressionType
        {
            Suppress, IgnoreTime, StandardClick
        }
        public bool SuppressConsole
        {
            get { return suppress; }
            set { suppress = value; }
        }
        public SuppressionType SuppressClicks
        {
            get { return suppressc; }
            set { suppressc = value; }
        }
        public static string StitchError(Exception ex)
        {
            string ret = "";
            ret += ex.GetType().ToString() + ":+:";
            ret += ex.Message + ":+:";
            ret += ex.StackTrace + ":+:";
            ret += ex.HResult.ToString();
            if (ex.HelpLink != null)
            {
                ret += ":+:" + ex.HelpLink;
            }
            return ret;
        }
        public static string StitchED(params string[] s)
        {
            string ret = "";
            Loop.Foreach(s, (t, i) =>
            {
                if (i == 0)
                {
                    ret += t;
                }
                else
                {
                    ret += "|" + t;
                }
            });
            return "<ED>" + ret + "</ED>";
        }
        public static string StitchED(params Exception[] s)
        {
            string[] ret = new string[s.Length];
            Loop.For((i) =>
            {
                ret[i] = StitchError(s[i]);
            }, s.Length);
            return StitchED(ret);
        }
        public static string StitchED(List<Exception> s)
        {
            string[] ret = new string[s.Count];
            Loop.For((i) =>
            {
                ret[i] = StitchError(s[i]);
            }, s.Count);
            return StitchED(ret);
        }
        public void Add(Exception ex)
        {
            if (!suppress)
            {
                _s.FastLine(ex.GetType().ToString());
            }
            if (suppressc == SuppressionType.StandardClick)
            {
                _sl.Click("Exception(" + exc.Count.ToString() + ")");
            }
            else if (suppressc == SuppressionType.IgnoreTime)
            {
                _sl.IgnoreTimeClick("Exception(" + exc.Count.ToString() + ")");
            }
            exc.Add(ex);
        }
        public string GetTags()
        {
            return StitchED(exc);
        }
    }
    public class InvalidTagConfigurationException : Exception
    {
        public InvalidTagConfigurationException() : base("Tags don't match.")
        {
        }
    }
    public class Save
    {
        List<string> l;
        public Save()
        {
            l = new List<string>();
        }
        public void SaveLine(string s, params object[] o)
        {
            string _s = System.String.Format(s, o);
            l.Add(_s);
            Console.WriteLine(_s);
            System.Threading.Thread.Sleep(200);
        }
        public void SaveWrite(string s, params object[] o)
        {
            string _s = System.String.Format(s, o);
            l[l.Count - 1] = l.Last() + _s;
            Console.Write(_s);
            System.Threading.Thread.Sleep(200);
        }
        public void FastLine(string s, params object[] o)
        {
            string _s = System.String.Format(s, o);
            l.Add(_s);
            Console.WriteLine(_s);
        }
        public void FastWrite(string s, params object[] o)
        {
            string _s = System.String.Format(s, o);
            l[l.Count - 1] = l.Last() + _s;
            Console.Write(_s);
        }
        public void DirectWrite(string s, params object[] o)
        {
            string _s = System.String.Format(s, o);
            l.Add(_s);
        }
        public void ExclusiveWrite(string s, params object[] o)
        {
            string _s = System.String.Format(s, o);
            Console.WriteLine(_s);
        }

        public string SaveRead(StopLap sl)
        {
            sl.StartUserClick();
            string s = Console.ReadLine();
            DirectWrite(s);
            sl.UserClick();
            return s;
        }

        public void SaveAll(string path)
        {
            SaveLine("------------SAVING-----------");
            SaveLine("");
            SaveLine("FILE_LOCATION: " + path.ToUpper());
            SaveLine("");
            if (!File.Exists(path))
            {
                SaveLine("____________________________________________________________________________________________________");
                SaveLine("");
                SaveLine("");
                SaveLine("");
                File.WriteAllLines(path, l);
                Console.WriteLine("---FILE_SAVED_SUCCESSFULLY---");
                return;
            }
            else
            {
                SaveLine("FILENAME_HAS_BEEN_TAKEN");
                SaveLine("");
                SaveLine("APPENDING");
                SaveLine("");
                SaveLine("____________________________________________________________________________________________________");
                SaveLine("");
                SaveLine("");
                SaveLine("");
                File.AppendAllLines(path, l);
                SaveLine("---FILE_SAVED_SUCCESSFULLY---");
            }
        }
    }
    public class StopLap : Stopwatch
    {
        public List<TimeSpan> dt;
        public List<string> ds;
        public Stopwatch s;
        public int userclicks = 0;
        public int ignoreclicks = 0;
        public StopLap()
        {
            dt = new List<TimeSpan>();
            ds = new List<string>();
            s = new Stopwatch();
        }
        public void StartS()
        {
            s.Start();
            Start();
        }
        public void Click()
        {
            ds.Add(dt.Count.PadToString(3, 0));
            dt.Add(Elapsed);
            Reset();
            Start();
        }
        public void Click(string title)
        {
            ds.Add(title);
            dt.Add(Elapsed);
            Reset();
            Start();
        }
        public void UserClick()
        {
            Click("ut:ex(" + userclicks + ")");
            userclicks++;
        }
        public void StartUserClick()
        {
            Click("ut:bg(" + userclicks + ")");
        }
        public void IgnoreTimeClick()
        {
            Click("ig:ex(" + ignoreclicks + ")");
            ignoreclicks++;
        }
        public void IgnoreTimeClick(string post)
        {
            Click("ig:ex(" + ignoreclicks + ")-" + post);
            ignoreclicks++;
        }
        public void StopS()
        {
            Stop();
            s.Stop();
            ds.Add("ig:ex-StopS()");
            dt.Add(Elapsed);
        }
        public void ResetS()
        {
            Reset();
            s.Reset();
            dt.Clear();
            ds.Clear();
        }
        public void Lap()
        {
            Click();
        }
        public TimeSpan MissingTime()
        {
            TimeSpan tmp = new TimeSpan();
            foreach (TimeSpan t in dt)
            {
                tmp += t;
            }
            return (s.Elapsed - tmp);
        }
    }
    #endregion
}
