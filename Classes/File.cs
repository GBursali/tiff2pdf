using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Tiff2Pdf.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Office.Interop.Word;
using Task = System.Threading.Tasks.Task;
#pragma warning disable 1998

namespace Tiff2Pdf.Classes
{ 

    /// <summary>
    /// Window States
    /// </summary
    public enum Stat
    {
        READY,CONVERTED
    }

    /// <summary>
    /// File class for converting.
    /// </summary>
    public sealed class ConvertFile : INotifyPropertyChanged
    {
        #region Privates

        private string _filename = string.Empty;
        private Stat _status = Stat.READY;
        private double _value = 0.0;

        #endregion

        #region INotfiy

        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify([CallerMemberName]string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        #endregion

        /// <summary>
        /// FullName of File
        /// </summary>
        public string FileName
        {
            get { return _filename; }
            set
            {
                if (value == _filename)
                    return;

                _filename = value;
                Notify();
            }
        }

        /// <summary>
        /// Percentage
        /// </summary>
        public double Value
        {
            get { return _value; }
            set
            {
                if (value == _value)
                    return;

                _value = value;
                Notify();
            }
        }

        /// <summary>
        /// File Status
        /// </summary>
        public Stat Status
        {
            get { return _status; }
            private set
            {
                _status = value;
                Notify();
                //Notify("color");
            }
        }

        /// <summary>
        /// File's name
        /// </summary>
        public string FileShort => global::System.IO.Path.GetFileName(FileName);

        /// <summary>
        /// TIFF to PDF
        /// </summary>
        public async void Convert()
        {
            try
            {
                using (var pd = new PdfDocument())
                {
                    var bpm = Image.FromFile(FileName);
                    using (bpm)
                    {
                        for (var i = 0; i < bpm.GetFrameCount(FrameDimension.Page); i++)
                            using (var byteStream = new MemoryStream())
                            {
                                bpm.SelectActiveFrame(FrameDimension.Page, i);
                                bpm.Save(byteStream, ImageFormat.Jpeg);
                                var ppage = pd.AddPage();
                                using (var gfx = XGraphics.FromPdfPage(ppage))
                                {
                                    gfx.DrawImage(XImage.FromStream(byteStream), 0, 0);
                                }
                            }
                    }

                    pd.Save(Path());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception Occured:" + e.Message);
                return;
            }

            Status = Stat.CONVERTED;
            CheckPermissionAndFire(Settings.Default.openAfterCon,
                                   () => { global::System.Diagnostics.Process.Start(Path()); });
            Notify("fileShort");
        }

        /// <summary>
        /// Gives the Path for this file according to settings.
        /// </summary>
        /// <returns>Path</returns>
        private string Path()
        {
            var path = "";
            CheckPermissionAndFire(Settings.Default.useSamePath, () => { path = global::System.IO.Path.Combine(global::System.IO.Path.GetFullPath(FileName).Substring(0, FileName.LastIndexOf(".", StringComparison.Ordinal)) + ".pdf"); });

            if (!string.IsNullOrEmpty(path))
                return path;

            var sfd = new SaveFileDialog
            {
                Title = "Choose a Path to save",
                CheckPathExists = true,
                AddExtension = false,
                Filter = "PDF Files|*.pdf",
                FileName = global::System.IO.Path.GetFileNameWithoutExtension(FileName) + ".pdf",
                OverwritePrompt = true
            };

            if (sfd.ShowDialog() == true)
                path= sfd.FileName;
            else
                MessageBox.Show("You have to select a Path");

            return path;
        }

        /// <summary>
        /// If you have the 'perm'ission, 'Fire'
        /// </summary>
        /// <param name="perm">Permission</param>
        /// <param name="fire">Event</param>
        public static void CheckPermissionAndFire(bool perm, Action fire)
        {
            if (perm) fire();
        }
    }
}

