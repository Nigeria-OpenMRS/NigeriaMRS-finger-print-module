using CommonLib.DAO;
using CommonLib.Facade;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FingerPrintDesktop
{
    public partial class MainWindow : Window
    {
        string[] fingerPosition;
        List<FingerPrintInfo> fingerPrintList;
        List<FingerPrintInfo> previousBiometricInfo;
        DataAccess db;
        public MainWindow()
        {
            InitializeComponent();
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
            fingerPrintList = new List<FingerPrintInfo>();
            fingerPosition = new string[] {"Unknown","RIGHT_THUMB", "RIGHT_INDEX", "RIGHT_MIDDLE", "RIGHT_RING", "RIGHT_LITTLE", "LEFT_THUMB", "LEFT_INDEX", "LEFT_MIDDLE", "LEFT_RING",
            "LEFT_LITTLE" };

            db = new DataAccess();
            var connectionInfo = db.GetConnectionStringFromFile();
            if (connectionInfo == null)
            {
                DatabaseSetting dbpage = new DatabaseSetting();
                dbpage.Show();
                this.Close();
            }
            else
            {
                previousBiometricInfo = db.GetPatientBiometricinfo();
            }
        }

        string patientId = "", patient_name = "";

        private void BtnCapturePrint_Click(object sender, RoutedEventArgs e)
        {
            if (patientId != txtPatientId.Text)
            {
                patientId = txtPatientId.Text;
                var db = new DataAccess();
                patient_name = db.RetrievePatientNameByUniqueId(patientId);
                lblPatientName.Content = patient_name.Split('|')[0];
                var previously = db.GetPatientBiometricinfo();
            }

            if (string.IsNullOrEmpty(patient_name))
            {
                MessageBox.Show("No patient information found for Id " + patientId);
                return;
            }

            string button_name = ((Button)sender).Name;
            int position = Array.FindIndex(fingerPosition, x => x.ToUpper() == button_name.ToUpper());

            var pictureBox = (System.Windows.Controls.Image)FindName("pictureBoxR" + position);

            FingerPrintInfo fingerPrintInfo = CapturePrint(position);
            if (fingerPrintInfo != null && string.IsNullOrEmpty(fingerPrintInfo.ErrorMessage))
            {
                DrawImage(fingerPrintInfo.ImageByte, pictureBox, fingerPrintInfo.ImageWidth, fingerPrintInfo.ImageHeight);

                Int32.TryParse(patient_name.Split('|')[1], out int pid);
                fingerPrintInfo.PatienId = pid;
                fingerPrintList.Add(fingerPrintInfo);

                if(fingerPrintList.Count > 5)
                {
                    btnSave.IsEnabled = true;
                }
            }
            else
            {
                MessageBox.Show(fingerPrintInfo.ErrorMessage, "Attention!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        FingerPrintInfo CapturePrint(int fingerPosition)
        {
            FingerPrintFacade fingerPrintFacade = new FingerPrintFacade();
            var data = fingerPrintFacade.Capture(fingerPosition, out string err, true);

            if (string.IsNullOrEmpty(err))
            {
                var matchedPatientId = fingerPrintFacade.Verify(new FingerPrintMatchInputModel
                {
                    FingerPrintTemplate = data.Template,
                    FingerPrintTemplateListToMatch = new List<FingerPrintInfo>(previousBiometricInfo)
                });

                if (matchedPatientId != 0)
                {
                    string info = db.RetrievePatientNameByPatientId(matchedPatientId);
                    string name = info.Split('|')[0];
                    string UniqueId = info.Split('|')[1];
                    data.ErrorMessage = string.Format("Finger print record already exist for this patient {0} Name : {1} {2} Patient Identifier : {3}",
                        Environment.NewLine, name, Environment.NewLine, UniqueId);
                }
            }
            else
            {
                data = new FingerPrintInfo();
                data.ErrorMessage = err;
            }
            return data;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var db = new DataAccess();
            var response = db.SaveToDatabase(fingerPrintList);
            MessageBox.Show(response.ErrorMessage);

            //update this list
            if (response.IsSuccessful)
            {
                previousBiometricInfo = db.GetPatientBiometricinfo();
                fingerPrintList = new List<FingerPrintInfo>();
            }
            ResetAllControl();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetAllControl();
        }

        void ResetAllControl()
        {
            fingerPrintList = new List<FingerPrintInfo>();
            pictureBoxR1.Source = null;
            pictureBoxR2.Source = null;
            pictureBoxR3.Source = null;
            pictureBoxR4.Source = null;
            pictureBoxR5.Source = null;
            pictureBoxR6.Source = null;
            pictureBoxR7.Source = null;
            pictureBoxR8.Source = null;
            pictureBoxR9.Source = null;
            pictureBoxR10.Source = null;
            txtPatientId.Text = "";
            lblPatientName.Content = "";
            btnSave.IsEnabled = false;
        }

        private void BtnDBSettings_Click(object sender, RoutedEventArgs e)
        {
            DatabaseSetting dbpage = new DatabaseSetting();
            dbpage.Show();
            this.Close();
        }


        private void DrawImage(Byte[] imgData, System.Windows.Controls.Image pictureBox, int m_ImageWidth, int m_ImageHeight)
        {
            int colorval;
            Bitmap bmp = new Bitmap(m_ImageWidth, m_ImageHeight);

            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    colorval = (int)imgData[(j * m_ImageWidth) + i];
                    bmp.SetPixel(i, j, System.Drawing.Color.FromArgb(colorval, colorval, colorval));
                }
            }

            pictureBox.Source = BitmapToImageSource(bmp);
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
    }
}
