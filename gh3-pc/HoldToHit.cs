using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;

// Applies only the version-checked, tested hook embedded at build time.
// No on-disk game modifications. All remote addresses belong to GH3.exe.
static class Native {
 [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
 [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool ReadProcessMemory(IntPtr process,IntPtr address,byte[] buffer,UIntPtr size,out UIntPtr read);
 [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool WriteProcessMemory(IntPtr process,IntPtr address,byte[] buffer,UIntPtr size,out UIntPtr written);
 [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr VirtualAllocEx(IntPtr process,IntPtr address,UIntPtr size,uint type,uint protect);
 [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool VirtualProtectEx(IntPtr process,IntPtr address,UIntPtr size,uint protect,out uint oldProtect);
 [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool VirtualFreeEx(IntPtr process,IntPtr address,UIntPtr size,uint type);
 [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool FlushInstructionCache(IntPtr process,IntPtr address,UIntPtr size);
 [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr OpenThread(uint access,bool inherit,int id);
 [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool Wow64GetThreadContext(IntPtr thread,[In,Out] byte[] context);
 [DllImport("ntdll.dll")] internal static extern int NtSuspendProcess(IntPtr handle);
 [DllImport("ntdll.dll")] internal static extern int NtResumeProcess(IntPtr handle);
 [DllImport("user32.dll",SetLastError=true)] internal static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
 [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window,int id);
 [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
}

sealed class Trainer : IDisposable {
 internal Process Game;
 internal bool Enabled;
 internal bool Attached { get { return handle!=IntPtr.Zero && installed; } }
 IntPtr handle,allocation;
 byte[] jump,missJump;
 bool installed,missInstalled;
 internal string LogPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"session.log");
 static IntPtr Ptr(long n) { return new IntPtr(n); }
 static Exception Error(string action) { return new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),action); }
 internal void Log(string message) { try { File.AppendAllText(LogPath,DateTime.Now.ToString("s")+" "+message+Environment.NewLine); } catch {} }
 static string Hash(byte[] bytes) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
 static bool Same(byte[] a,byte[] b) { if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true; }
 byte[] Read(long address,int length) {
  byte[] bytes=new byte[length];UIntPtr read;
  if(!Native.ReadProcessMemory(handle,Ptr(address),bytes,(UIntPtr)(uint)length,out read)||read.ToUInt64()!=(ulong)length)throw Error("Read GH3 memory");
  return bytes;
 }
 void Write(long address,byte[] bytes) {
  UIntPtr written;
  if(!Native.WriteProcessMemory(handle,Ptr(address),bytes,(UIntPtr)(uint)bytes.Length,out written)||written.ToUInt64()!=(ulong)bytes.Length)throw Error("Write GH3 patch");
  if(!Same(Read(address,bytes.Length),bytes))throw new Exception("GH3 patch verification failed.");
 }
 void ReplaceHook(long hook,byte[] expected,byte[] replacement) {
  int status=Native.NtSuspendProcess(handle);
  if(status!=0)throw new Exception("Could not briefly pause GH3 for patch installation ("+status.ToString("X8")+").");
  uint old=0;bool protectionChanged=false;
  try {
   if(!Same(Read(hook,expected.Length),expected))throw new Exception("Another modification changed the hook; replacement stopped.");
   // Do not replace bytes beneath a suspended thread's next instruction.
   // The original six-byte span has an instruction boundary at Hook+4.
   Game.Refresh();
   foreach(ProcessThread thread in Game.Threads) {
    IntPtr th=Native.OpenThread(0x8,false,thread.Id);
    if(th==IntPtr.Zero)throw Error("Inspect GH3 thread before patching");
    try {
     byte[] context=new byte[716];Array.Copy(BitConverter.GetBytes(0x10001),context,4);
     if(!Native.Wow64GetThreadContext(th,context))throw Error("Read GH3 instruction pointer");
     uint ip=BitConverter.ToUInt32(context,184);
     if(ip>hook&&ip<hook+replacement.Length)throw new Exception("GH3 is executing the hook. Pause the song and retry Attach.");
    } finally { Native.CloseHandle(th); }
   }
   if(!Native.VirtualProtectEx(handle,Ptr(hook),(UIntPtr)(uint)replacement.Length,0x40,out old))throw Error("Make hook writable");
   protectionChanged=true;
   try { Write(hook,replacement); }
   catch { Write(hook,expected);throw; }
   if(!Native.FlushInstructionCache(handle,Ptr(hook),(UIntPtr)(uint)replacement.Length))throw Error("Refresh instruction cache");
  } finally {
   if(protectionChanged){uint unused;Native.VirtualProtectEx(handle,Ptr(hook),(UIntPtr)(uint)replacement.Length,old,out unused);}
   int resume=Native.NtResumeProcess(handle);
   if(resume!=0)Log("ERROR: resume returned "+resume.ToString("X8"));
  }
 }
 internal void Attach() {
  if(Attached)return;
  var found=Process.GetProcessesByName("GH3");
  if(found.Length!=1)throw new Exception(found.Length==0?"Start GH3, enter a song and pause it, then click Attach.":"Please leave only one GH3 instance running.");
  Game=found[0];string path=Game.MainModule.FileName;
  if(Hash(File.ReadAllBytes(path))!=Payload.ExeHash)throw new Exception("This GH3.exe version is unsupported. No patch was applied.");
  if(Game.MainModule.BaseAddress.ToInt64()!=0x400000)throw new Exception("Unexpected game image base. No patch was applied.");
  handle=Native.OpenProcess(0xC38,false,Game.Id);
  if(handle==IntPtr.Zero)throw Error("Open GH3 for patching");
  bool hookAttempted=false;
  try {
   if(Hash(Read(Payload.RoutineStart,Payload.RoutineSize))!=Payload.RoutineHash)throw new Exception("GH3 input code differs from the tested build, or another mod is active. No patch was applied.");
   if(Hash(Read(Payload.MissHook,0x170))!=Payload.MissRoutineHash)throw new Exception("GH3 empty-press code differs from the tested build. No patch was applied.");
   byte[] code;
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("HoldHook")) {
    code=new byte[stream.Length];int read=stream.Read(code,0,code.Length);if(read!=code.Length)throw new Exception("Missing patch resource.");
   }
   allocation=Native.VirtualAllocEx(handle,IntPtr.Zero,(UIntPtr)0x2000,0x3000,0x04);
   if(allocation==IntPtr.Zero)throw Error("Allocate GH3 helper");
   long cave=allocation.ToInt64();
   if(cave<0x10000||cave+0x2000>0x80000000)throw new Exception("Unable to obtain a compatible 32-bit helper address.");
   foreach(int offset in Payload.FlagRelocations)Array.Copy(BitConverter.GetBytes((uint)(cave+0x1000)),0,code,offset,4);
   for(int i=0;i<Payload.ReturnRelocations.Length;i++){int offset=Payload.ReturnRelocations[i];Array.Copy(BitConverter.GetBytes(unchecked((int)(Payload.ReturnTargets[i]-(cave+offset+4)))),0,code,offset,4);}
   Write(cave,code);Write(cave+0x1000,BitConverter.GetBytes(0));
   uint unused;
   if(!Native.VirtualProtectEx(handle,allocation,(UIntPtr)0x1000,0x20,out unused))throw Error("Protect helper code");
   if(!Native.FlushInstructionCache(handle,allocation,(UIntPtr)(uint)code.Length))throw Error("Refresh helper instructions");
   jump=new byte[]{0xe9,0,0,0,0,0x90};Array.Copy(BitConverter.GetBytes(unchecked((int)(cave-Payload.Hook-5))),0,jump,1,4);
   missJump=new byte[]{0xe9,0,0,0,0,0x90};Array.Copy(BitConverter.GetBytes(unchecked((int)(cave+Payload.MissOffset-Payload.MissHook-5))),0,missJump,1,4);
   hookAttempted=true;ReplaceHook(Payload.MissHook,Payload.MissOriginal,missJump);missInstalled=true;
   ReplaceHook(Payload.Hook,Payload.Original,jump);installed=true;Enabled=false;
   Log("Controller Hold-to-Hit attached PID "+Game.Id+"; OFF; helper="+cave.ToString("X8")+"; both hooks verified.");
  } catch {
   if(missInstalled){try{ReplaceHook(Payload.MissHook,missJump,Payload.MissOriginal);missInstalled=false;}catch(Exception restore){Log("Miss-hook rollback failed: "+restore.Message+". Restart GH3 to clear the disabled helper.");}}
   // Keep allocated code if a hook write was attempted: an interrupted game
   // thread could already hold a helper instruction pointer.
   if(allocation!=IntPtr.Zero&&!hookAttempted)Native.VirtualFreeEx(handle,allocation,UIntPtr.Zero,0x8000);
   Native.CloseHandle(handle);handle=IntPtr.Zero;allocation=IntPtr.Zero;throw;
  }
 }
 internal void SetEnabled(bool enabled) {
  if(!Attached)throw new Exception("Attach to GH3 first.");
  if(!Same(Read(Payload.Hook,6),jump)||!Same(Read(Payload.MissHook,6),missJump))throw new Exception("GH3 hook was changed by another tool.");
  Write(allocation.ToInt64()+0x1000,BitConverter.GetBytes(enabled?1:0));Enabled=enabled;Log(enabled?"ON":"OFF");
 }
 internal void Detach() {
  if(handle==IntPtr.Zero)return;
  try {
   if(Game!=null&&!Game.HasExited&&installed) {
    SetEnabled(false);ReplaceHook(Payload.Hook,jump,Payload.Original);ReplaceHook(Payload.MissHook,missJump,Payload.MissOriginal);Log("Detached; both original hooks restored.");
   }
  } finally {
   // Retain two pages until GH3 exits so any in-flight helper can safely return.
   Native.CloseHandle(handle);handle=IntPtr.Zero;allocation=IntPtr.Zero;installed=false;missInstalled=false;Enabled=false;
  }
 }
 public void Dispose(){Detach();}
}

sealed class ControlWindow : Form {
 readonly Trainer trainer=new Trainer();
 readonly Label status=new Label();
 readonly Button toggle=new Button(),attach=new Button();
 readonly Timer timer=new Timer();
 bool hotkey,autoAttach;
 internal ControlWindow(bool launchGame) {
  autoAttach=launchGame;
  Text="Controller Hold-to-Hit — Guitar Hero III";ClientSize=new Size(510,240);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
  Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;
  status.SetBounds(20,18,470,46);status.Font=new Font(Font.FontFamily,13,FontStyle.Bold);Controls.Add(status);
  var help=new Label(){Text="F6: toggle while GH3 is focused\nHold matching frets to hit repeated notes.\nNew button presses keep normal hit/miss judgment.\nClose this tool to restore the original game code.",AutoSize=false};
  help.SetBounds(20,75,470,100);Controls.Add(help);
  attach.Text="Attach to GH3";attach.SetBounds(20,185,150,36);attach.Click+=(s,e)=>TryAttach();Controls.Add(attach);
  toggle.Text="Enable (F6)";toggle.SetBounds(185,185,150,36);toggle.Click+=(s,e)=>Toggle();Controls.Add(toggle);
  var close=new Button(){Text="Close / restore"};close.SetBounds(350,185,140,36);close.Click+=(s,e)=>Close();Controls.Add(close);
  Shown+=(s,e)=>{hotkey=Native.RegisterHotKey(Handle,1,0x4000,0x75);if(!launchGame)TryAttach();if(!hotkey)MessageBox.Show(this,"F6 is unavailable. Use the Enable button instead.",Text);};
  FormClosing+=(s,e)=>{try{trainer.Detach();}catch(Exception ex){MessageBox.Show(this,"Automatic restore failed: "+ex.Message+"\nClose and reopen GH3 to clear the runtime patch.",Text);}if(hotkey)Native.UnregisterHotKey(Handle,1);};
  timer.Interval=500;timer.Tick+=(s,e)=>{
   if(trainer.Attached&&trainer.Game.HasExited){trainer.Detach();autoAttach=true;UpdateStatus();}
   if(!trainer.Attached&&autoAttach){
    var games=Process.GetProcessesByName("GH3");bool ready=false;
    foreach(var game in games){try{ready|=game.MainWindowHandle!=IntPtr.Zero;}catch{}finally{game.Dispose();}}
    if(ready){autoAttach=false;TryAttach();}
   }
  };timer.Start();UpdateStatus();
 }
 void UpdateStatus(){status.Text=trainer.Attached?(trainer.Enabled?"ON — hold matching frets":"OFF — normal game controls"):"Waiting for GH3";status.ForeColor=trainer.Enabled?Color.DarkGreen:Color.Black;toggle.Enabled=trainer.Attached;toggle.Text=trainer.Enabled?"Disable (F6)":"Enable (F6)";attach.Enabled=!trainer.Attached;}
 void TryAttach(){autoAttach=false;var games=Process.GetProcessesByName("GH3");bool exists=games.Length>0;foreach(var game in games)game.Dispose();if(!exists){autoAttach=true;UpdateStatus();return;}try{trainer.Attach();}catch(Exception ex){trainer.Log("ERROR: "+ex.Message);MessageBox.Show(this,ex.Message,Text);}UpdateStatus();}
 void Toggle(){try{if(trainer.Attached)trainer.SetEnabled(!trainer.Enabled);}catch(Exception ex){trainer.Log("ERROR: "+ex.Message);MessageBox.Show(this,ex.Message,Text);}UpdateStatus();}
 protected override void WndProc(ref Message message){if(message.Msg==0x312&&message.WParam.ToInt32()==1){uint pid;Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out pid);if((trainer.Game!=null&&pid==(uint)trainer.Game.Id)||pid==(uint)Process.GetCurrentProcess().Id)Toggle();}base.WndProc(ref message);}
}

static class Program {
 internal const string GamePath=@"C:\Program Files (x86)\Aspyr\Guitar Hero III\GH3.exe";
 static void LaunchGame(){
  if(!File.Exists(GamePath))throw new Exception("GH3.exe was not found at "+GamePath);
  var running=Process.GetProcessesByName("GH3");bool exists=running.Length>0;
  foreach(var process in running)process.Dispose();
  if(!exists)Process.Start(new ProcessStartInfo(GamePath){WorkingDirectory=Path.GetDirectoryName(GamePath),UseShellExecute=true});
 }
 [STAThread] static void Main(string[] args){
  bool launch=args.Length==1&&args[0]=="--launch-game";
  if(launch){try{LaunchGame();}catch(Exception ex){MessageBox.Show(ex.Message,"GH3 launcher");Environment.ExitCode=1;return;}}
  bool first;using(var mutex=new System.Threading.Mutex(true,"Local\\PabloGH3HoldToHitV1",out first)){
   if(!first){if(!launch)MessageBox.Show("The GH3 hold-to-hit tool is already running.");return;}
   if(args.Length==1&&args[0]=="--self-test") {
    var test=new Trainer();
    try {test.Attach();test.SetEnabled(true);test.SetEnabled(false);test.Detach();test.Log("SELF-TEST PASSED: attach, ON, OFF, restore, byte verification.");}
    catch(Exception ex){test.Log("SELF-TEST FAILED: "+ex);try{test.Detach();}catch(Exception restore){test.Log("RESTORE FAILED: "+restore);}Environment.ExitCode=1;}
    return;
   }
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new ControlWindow(launch));
  }
 }
}
