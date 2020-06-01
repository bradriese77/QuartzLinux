using System;
using System.Net;
using System.Security.Permissions;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using Microsoft.VisualBasic.FileIO;
using log4net;
using log4net.Appender;

namespace AppUtilityLib
{

    public class AppUtility
    {

        private static readonly log4net.ILog logger = GetLogger();

        private static log4net.ILog GetLogger()
        {
            if(System.Reflection.Assembly.GetEntryAssembly()?.EntryPoint?.DeclaringType!=null)
          return log4net.LogManager.GetLogger(System.Reflection.Assembly.GetEntryAssembly().EntryPoint.DeclaringType);
            else return log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }
        public log4net.ILog Logger { get { return logger; } }
        public Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            Assembly MyAssembly, objExecutingAssemblies;
            string strTempAssmbPath = "";

            objExecutingAssemblies = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(",")))
                {
                    //Build the path of the assembly from where it has to be loaded.				
                    strTempAssmbPath = AssemblyDirectory + "\\" + args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                    break;
                }

            }
            //Load the assembly from the specified path. 					
            MyAssembly = Assembly.LoadFrom(strTempAssmbPath);

            //Return the loaded assembly.
            return MyAssembly;
        }

        public static int ReflectCmdArgs(string[] args)
        {
            Assembly LoadedAssembly = Assembly.GetEntryAssembly();
            Type ProgramType = LoadedAssembly.EntryPoint.DeclaringType;
            Type FindType = ProgramType;
            MethodInfo argMethod = null;
            while (FindType != null)
            {
                int i = 0;

                argMethod = FindType.GetMethod(args[i].Replace("-", ""));
                if (argMethod == null)
                {
                    FindType = FindType.BaseType;
                }
                if (argMethod != null)
                {
                    FindType = null;
                    ParameterInfo[] parminfo = argMethod.GetParameters();
                    int numberofparms = parminfo.Length;
                    if ((args.Length - 1 - i) < numberofparms) return ShowHelp();

                    List<object> parms = new List<object>();
                    int flagindex = i + 1;
                    for (int p = 0; p < numberofparms; p++)
                    {


                        parms.Add((object)Convert.ChangeType(args[p + flagindex], parminfo[p].ParameterType));
                        i++;

                    }
                    Object inst = LoadedAssembly.CreateInstance(ProgramType.ToString());

                    object returnvalue = argMethod.Invoke(inst, parms.ToArray());
                    if (returnvalue != null)
                    {
                        Type returnvalueType = returnvalue.GetType();
                        switch (returnvalueType.Name)
                        {
                            case "String[]":
                                foreach (string s in (String[])returnvalue)
                                {
                                    Console.WriteLine(s);
                                }

                                break;
                            case "Int32":
                                Console.WriteLine(returnvalue);
                                return (int)returnvalue;
                            default:
                                Console.WriteLine(returnvalue);
                                break;
                        }
                    }

                }

            }
            if (argMethod == null) return ShowHelp();
            return 0;
        }
        public static int ShowHelp()
        {

            string HelpStr = "Usage:\r\n";

            Assembly LoadedAssembly = Assembly.GetEntryAssembly();

            foreach (MethodInfo mthd in LoadedAssembly.EntryPoint.DeclaringType.GetMethods())
            {
                if (mthd.IsPublic && mthd.DeclaringType == LoadedAssembly.EntryPoint.DeclaringType)
                {

                    HelpStr += System.Diagnostics.Process.GetCurrentProcess().ProcessName + " -" + mthd.Name + " ";
                    foreach (ParameterInfo prm in mthd.GetParameters())
                    {
                        HelpStr += "<" + prm.Name + "> ";

                    }
                    HelpStr += "\r\n";
                }
            }

            Console.WriteLine(HelpStr);
            return 1;
        }

        public string AssemblyDirectory = "";
        public object CreateInstanceOfAssemblyType(string AssemblyPath, string TypeName)
        {
            return CreateInstanceOfAssemblyType(AssemblyPath, TypeName, null);
        }
        public Assembly LoadAssembly(string AssemblyPath)
        {

            System.IO.FileInfo info = new System.IO.FileInfo(AssemblyPath);
            this.AssemblyDirectory = info.DirectoryName;
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);
            Assembly LoadAssembly = Assembly.LoadFrom(AssemblyPath);
            return LoadAssembly;
        }
        public object CreateInstanceOfAssemblyType(string AssemblyPath, string TypeName, object[] Parameters)
        {
            System.IO.FileInfo info = new System.IO.FileInfo(AssemblyPath);
            this.AssemblyDirectory = info.DirectoryName;
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);

            Assembly LoadAssembly = Assembly.LoadFrom(AssemblyPath);
            foreach (string ReferencedAssemblyPath in System.IO.Directory.GetFiles(AssemblyDirectory, "*.dll"))
            {
                try
                {
                    currentDomain.Load(AssemblyName.GetAssemblyName(ReferencedAssemblyPath));
                    //currentDomain.Assembly.LoadFrom(ReferencedAssemblyPath));
                }
                catch { }
            }

            Type AssemblyType;
            if (TypeName == string.Empty)
            {
                AssemblyType = LoadAssembly.EntryPoint.DeclaringType;
            }
            else
            {

                AssemblyType = LoadAssembly.GetType(TypeName);
            }
            if (Parameters != null)
            {

                ConstructorInfo[] cis = AssemblyType.GetConstructors();
                ConstructorInfo myci = cis[0];
                foreach (ConstructorInfo ci in cis)
                {

                    ParameterInfo[] pis = ci.GetParameters();
                    if (Parameters.Count() == pis.Count())
                    {
                        myci = ci;

                    }
                }
                return myci.Invoke(Parameters);

            }
            else
            {
                return LoadAssembly.CreateInstance(AssemblyType.FullName);
            }
        }
        public void LogMoveFolder(string SourceFolder, string DestinationFolder,bool RemoveTopSourceFolder=true)
        {

            LogCreateDirectory(DestinationFolder);
            DirectoryInfo pdinfo = new DirectoryInfo(SourceFolder);
            foreach (FileInfo pinfo in pdinfo.GetFiles())
            {
                try
                {
                    string DestinationPath = Path.Combine(DestinationFolder, pinfo.Name);
                    LogMove(pinfo.FullName, DestinationPath, true);

                }
                catch { }
            }

            foreach (string Folder in Directory.GetDirectories(SourceFolder, "*.*", System.IO.SearchOption.TopDirectoryOnly))
            {
                try
                {
                    DirectoryInfo dinfo = new DirectoryInfo(Folder);
                    string ChildDestinationFolder = Path.Combine(DestinationFolder, dinfo.Name);
                    LogCreateDirectory(ChildDestinationFolder);
                    foreach (FileInfo info in dinfo.GetFiles())
                    {
                        string DestinationPath = Path.Combine(ChildDestinationFolder, info.Name);
                        LogMove(info.FullName, DestinationPath, true);

                    }
                    foreach (DirectoryInfo childDinfo in dinfo.GetDirectories())
                    {
                        LogMoveFolder(childDinfo.FullName, Path.Combine(ChildDestinationFolder, childDinfo.Name),true);

                    }
                }
                catch { }
                LogDeleteDirectory(Folder, false);
            }
            if(RemoveTopSourceFolder)
            LogDeleteDirectory(SourceFolder, false);
        }
        public void LogActionDesc(string Desc,Action action)
        {

            string LogStr = string.Format("{0}", Desc);
            Logger.Info(LogStr);
            Console.WriteLine(LogStr);
            try
            {
                action.Invoke();
            }
            catch(Exception ex)
            {
                LogStr=string.Format("Exception {0} {1}", Desc, ex.ToString());
                Logger.Error(LogStr);
                Console.WriteLine(LogStr);

                throw ex;
            }

        }
        public void LogMove(string SourcePath, string DestinationPath, bool Overwrite)
        {
            try
            {
                LogCopy(SourcePath, DestinationPath, Overwrite);
                LogDelete(SourcePath);
              
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Failed to copy {0} to {1}", SourcePath, DestinationPath), ex);

            }
        }
        public void LogDelete(string Path)
        {
            try
            {
                Logger.Info(string.Format("Deleting {0}", Path));
                File.Delete(Path);
                Logger.Info(string.Format("Successfully Deleted {0}", Path));
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Failed to delete {0}", Path), ex);
                throw ex;
            }
        }
        
        public void LogDeleteDirectory(string Path,bool Recurse)
        {
            try
            {
                Logger.Info(string.Format("Deleting Folder {0},{1}", Path,Recurse));
                Directory.Delete(Path,Recurse);
                Logger.Info(string.Format("Successfully Deleted Folder{0},{1}", Path,Recurse));
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Failed to delete {0}", Path), ex);
                throw ex;
            }
        }
        public void LogCreateDirectory(string Path)
        {
            try
            {
                Logger.Info(string.Format("Creating Folder {0}", Path));
                if (Directory.Exists(Path))
                {

                    Logger.Info(string.Format("Directory {0} already exists", Path));
                }
                else
                {
                    Directory.CreateDirectory(Path);
                    Logger.Info(string.Format("Successfully Created Folder {0}", Path));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Failed to delete {0}", Path), ex);
                throw ex;
            }
        }

        public void LogCopy(string SourcePath, string DestinationPath, bool Overwrite)
        {
            try
            {
               Logger.Info(string.Format("Copying {0} to {1}",SourcePath,DestinationPath));
               File.Copy(SourcePath, DestinationPath, Overwrite);
               Logger.Info(string.Format("Successfully copied {0} to {1}",SourcePath,DestinationPath));
            }
            catch(Exception ex)
            {
                Logger.Error(string.Format("Failed to copy {0} to {1}", SourcePath, DestinationPath), ex);
                throw ex;
            }
        }

        public virtual string GetSetting(string Name, string Default)
        {
            string ReturnValue;
            try
            {
                ReturnValue = System.Configuration.ConfigurationManager.AppSettings.Get(Name);
                if (ReturnValue == null || ReturnValue == string.Empty)
                {
                    ReturnValue = Default;
                }
            }
            catch { ReturnValue = Default; }

            return ReturnValue;

        }
        public object CallMethod(object obj, string MethodName)
        {
            return CallMethod(obj, MethodName, obj);
        }
        public object CallMethod(object obj, string MethodName, object target)
        {
            return CallMethod(obj, MethodName, target, null);
        }
        public object CallMethod(object obj, string MethodName, object target, object arg)
        {
            return CallMethod(obj, MethodName, target, new object[] { arg });
        }
        public object CallMethod(object obj, string MethodName, object target, object[] args)
        {
            return obj.GetType().InvokeMember(MethodName,
            BindingFlags.InvokeMethod, null, target, args);
            //return CallInvoke(obj, MethodName, target, args, BindingFlags.InvokeMethod);
        }
        public object CallInvoke(object obj, string MethodName, object target, object[] args, BindingFlags flags)
        {
            return obj.GetType().InvokeMember(MethodName,
            flags, null, target, args);
        }
        public static string GetLogFileName()
        {
            var rootAppender = LogManager.GetRepository()
                                         .GetAppenders()
                                         .OfType<RollingFileAppender>()
                                         .FirstOrDefault();

            return rootAppender != null ? rootAppender.File : string.Empty;
        }
                public int RunProcess(string Script, string Arguments, int? ScriptTimeOutInSeconds)
        {
            int PID = 0;
           return RunProcess(Script, Arguments, ScriptTimeOutInSeconds, out PID, Environment.CurrentDirectory);
        }

        public int RunProcess(string Script, string Arguments, int? ScriptTimeOutInSeconds, out int PID,string WorkingFolder,ProcessWindowStyle WindowStyle= ProcessWindowStyle.Normal)
        {
            Process process = new Process();
            Logger.InfoFormat("RunProcess {0} {1}",Script,Arguments);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory=WorkingFolder;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.FileName = Script;
            process.StartInfo.WindowStyle = WindowStyle;
            process.StartInfo.Arguments = Arguments;
            process.Start();
            PID = process.Id;
            if(ScriptTimeOutInSeconds.HasValue)
            {
                process.WaitForExit(ScriptTimeOutInSeconds.Value * 1000);
                return process.ExitCode;
            }
            return 0;

        }
        public void KillName(string Name, string TitleContains)
        {

            foreach (var Proc in Process.GetProcesses().Where(p => p.ProcessName.ToLower().StartsWith(Name)))
            {
                string ProcName = Proc.ProcessName.ToLower();
                if (string.IsNullOrEmpty(TitleContains) || Proc.MainWindowTitle.Contains(TitleContains))
                    try
                    {
                        Proc.Kill();
                    }
                    catch { }
            }

        }
        public List<IntPtr> GetProcessHandles(string Name, string TitleContains)
        {

            var procs = from p in Process.GetProcesses()
                        where p.ProcessName.ToLower().StartsWith(Name) && (string.IsNullOrEmpty(TitleContains) || p.MainWindowTitle.Contains(TitleContains))
                        select p.MainWindowHandle;

            return procs.ToList();

        }
    }

}
