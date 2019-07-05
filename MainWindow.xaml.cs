using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Tiff2Pdf.Classes;
using Tiff2Pdf.Properties;

namespace Tiff2Pdf
{
    public partial class MainWindow:INotifyPropertyChanged
    {

        #region Notify Stuff

        /// <summary>
        /// NotifyPropertyChanged Implement
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifier
        /// </summary>
        /// <param name="prop">Notified Property Name</param>
        private void Notify([CallerMemberName]string prop="")=>PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        #endregion

        #region Private Stuff

        /// <summary>
        /// State for Iconize. true if iconized
        /// </summary>
        private bool _state = true;

        /// <summary>
        /// File List
        /// </summary>
        private ObservableCollection<ConvertFile> _fileList = new ObservableCollection<ConvertFile>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Window's State. Converted to Boolean in the XAML Window.
        /// </summary>
        public bool State { get => _state;
            set { _state = value;Notify(); } }

        /// <summary>
        /// Convert List's all files
        /// </summary>
        public ObservableCollection<ConvertFile> FileList { get => _fileList;
            set { _fileList = value; Notify(); } }

        /// <summary>
        /// Ekranda ortada yazılacak metin.
        /// </summary>
        public string MiddleText => string.Format("{0} Files are Ready", FileList.Count);

        #endregion

        /// <inheritdoc />
        /// <summary>
        /// START:Initialize
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            FileList.CollectionChanged += CheckAndIfConvert;
            Ni.Click += (s, e) => { Iconize(null, null); };
            Ni.Icon = new System.Drawing.Icon("Tiff2Pdf.ico");
        }

        #region IconizingStuff

        /// <summary>
        /// Program's Icon
        /// </summary>
        public System.Windows.Forms.NotifyIcon Ni = new System.Windows.Forms.NotifyIcon()
        {
            BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Warning,
            BalloonTipText = @"Waits Here",
            BalloonTipTitle = @"Tiff2Pdf",
            Visible = true
        };

        /// <summary>
        /// Iconize Click
        /// </summary>
        /// <param name="sender">_ button</param>
        /// <param name="e">Button.Click</param>
        private void Iconize(object sender, RoutedEventArgs e)
        {
            State = !State;
            Ni.BalloonTipText = @"Program " + ((State) ? "is on the screen" : "is hidden");
            Ni.ShowBalloonTip(1000);
        }

        #endregion

        #region Adding to List

        /// <summary>
        /// listbox Drop, Add File
        /// </summary>
        /// <param name="sender">ListBox</param>
        /// <param name="e">ListBox.Drop</param>
        private void AddFile(object sender, DragEventArgs e)
        {
            ShowandGetPath(e.Data.GetDataPresent(DataFormats.FileDrop)
                ? ((string[]) e.Data.GetData(DataFormats.FileDrop)).ToList()
                : new List<string>());
        }

        /// <summary>
        /// Opens a Dialog, and adds the TIFF's on the List
        /// </summary>
        /// <param name="p">Extra Item. Can be null</param>
        public void ShowandGetPath(List<string> p)
        {
            var paths = new List<string>();
            paths.AddRange(p);
            if (paths.Count == 0)
            {
                # region OpenFileDialog ofd
                OpenFileDialog ofd = new OpenFileDialog()
                {
                    AddExtension = false,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    DefaultExt = "tiff",
                    Multiselect = true,
                    Title = "tiff Dosyası seçiniz.",
                    Filter = "TIF|*.tif;*.TIFF;*tiff;*.TIF;*.TİF;*TİFF"
                };
                #endregion

                if (ofd.ShowDialog() == true)
                {
                    paths.AddRange(ofd.FileNames.ToList());
                }
                else { MessageBox.Show("You have to select something.", "Warning"); return; }
            }
            foreach (var item in paths)
            {
                var dos = new ConvertFile()
                {
                    FileName = item,
                    Value = 0
                };
                FileList.Add(dos);
                Notify(@"FileList");
                Notify(@"MiddleText");
            }
        }
        #endregion

        #region Listbox Context

        /// <summary>
        /// Context> Delete in the list. If Tag is True, then Delete with file
        /// </summary>
        /// <param name="sender">MenuItem in the Context</param>
        /// <param name="e">MenuItem.Click</param>
        private void Erase(object sender, RoutedEventArgs e)
        {
            var sea = new List<ConvertFile>();
            sea.AddRange(lbConverter.SelectedItems.OfType<ConvertFile>());
            foreach (var item in sea)
            {
                if ((sender as MenuItem)?.Tag.ToString().ToLower() == "true" &&
                    File.Exists(item.FileName)) File.Delete(item.FileName);
                FileList.Remove(item);
            }

        }

        /// <summary>
        /// Context>Open File
        /// </summary>
        /// <param name="sender">MenuItem</param>
        /// <param name="e">Click</param>
        private void Follow(object sender, RoutedEventArgs e)
        {
            Process.Start((lbConverter.SelectedItem as ConvertFile)?.FileName);
        }

        /// <summary>
        /// Context,Add File
        /// </summary>
        /// <param name="sender">MenuItem</param>
        /// <param name="e">Click</param>
        private void NewFile(object sender, RoutedEventArgs e)
        {
            ShowandGetPath(new List<string>());
        }

        #endregion
        
        /// <summary>
        /// Settings Save
        /// </summary>
        /// <param name="sender">CheckBox</param>
        /// <param name="e">Click</param>
        private void SettingsChanged(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
        }

        #region Conversion

        /// <summary>
        /// Works the Worker. How Ironic...
        /// </summary>
        /// <param name="sender">Convert Button</param>
        /// <param name="e"> Click</param>
        private void ConvertList(object sender, RoutedEventArgs e)
        {
            using(BackgroundWorker bw = GetBw())
            {
                bw.RunWorkerAsync(FileList);
            }
        }

        /// <summary>
        /// If UseButton is false, convert immediately
        /// </summary>
        /// <param name="sender">FileList</param>
        /// <param name="e">CollectionChanged</param>
        private void CheckAndIfConvert(object sender, NotifyCollectionChangedEventArgs e)
        {
            using(BackgroundWorker bw = GetBw())
            {
                if (e.NewItems != null && !Settings.Default.useButton && !bw.IsBusy)
                    bw.RunWorkerAsync(new List<ConvertFile>(e.NewItems.OfType<ConvertFile>()));
            }
            Notify("FileList");
            Notify("MiddleText");
            Notify("State");
        }

        /// <summary>
        ///a Worker can convert the TIFF Files into PDF's 
        /// </summary>
        /// <returns> İşçi</returns>
        private BackgroundWorker GetBw()
        {
            var bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                foreach (var item in (List<ConvertFile>) e.Argument)
                {
                    if (item.Status != Stat.READY)
                        continue;
                    item.Convert();
                    Application.Current.Dispatcher.Invoke((System.Threading.ThreadStart)delegate
                    {
                        ConvertFile.CheckPermissionAndFire(Settings.Default.Erase, () => { FileList.Remove(item); });
                    });
                    Notify("FileList");
                }
            };
            return bw;
        }

        #endregion

        }
    }
