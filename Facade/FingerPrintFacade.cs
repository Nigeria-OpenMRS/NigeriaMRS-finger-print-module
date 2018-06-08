using FingerPrintModule.DAO;
using SecuGen.FDxSDKPro.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace FingerPrintModule.Facade
{
    public class FingerPrintFacade
    {
        private SGFingerPrintManager m_FPM;
        private SGFPMSecurityLevel m_SecurityLevel;
        private Int32 m_ImageWidth;
        private Int32 m_ImageHeight;
        private Int32 m_Dpi;
        private int serialNo;

        private bool m_DeviceOpened;
        Int32 max_template_size = 0;



        public FingerPrintInfo Capture(int fingerPosition)
        {
            InitializeDevice();

            Byte[] fp_image = new Byte[m_ImageWidth * m_ImageHeight];
            Byte[] m_fingerprinttemplate = new Byte[max_template_size];

            Int32 error = (Int32)SGFPMError.ERROR_NONE;
            Int32 img_qlty = 0;

            if (m_DeviceOpened)
            {
                error = m_FPM.GetImage(fp_image);
            }

            if (error == (Int32)SGFPMError.ERROR_NONE)
            {
                m_FPM.GetImageQuality(m_ImageWidth, m_ImageHeight, fp_image, ref img_qlty);


                SGFPMFingerInfo finger_info = new SGFPMFingerInfo();
                finger_info.FingerNumber = (SGFPMFingerPosition)fingerPosition;
                finger_info.ImageQuality = (Int16)img_qlty;
                finger_info.ImpressionType = (Int16)SGFPMImpressionType.IMPTYPE_LP;
                finger_info.ViewNumber = 1;

                // CreateTemplate
                error = m_FPM.CreateTemplate(finger_info, fp_image, m_fingerprinttemplate);

                if (error == (Int32)SGFPMError.ERROR_NONE)
                {
                    return new FingerPrintInfo
                    {
                        Manufacturer = "",
                        Model = "",
                        SerialNumber = "",
                        ImageWidth = m_ImageWidth,
                        ImageHeight = m_ImageHeight,
                        ImageDPI = m_Dpi,
                        ImageQuality = img_qlty,
                        Image = ToBase64String(fp_image, ImageFormat.Bmp),
                        Template = Convert.ToBase64String(m_fingerprinttemplate),
                        FingerPositions = (FingerPositions)fingerPosition
                    };
                }
            }
            throw new ApplicationException("Error : " + ((SGFPMError)error).ToString());
        }

        public int Verify(FingerPrintMatchInputModel input)
        {
            InitializeDevice();

            int matchedRecord = 0;
            Int32 err = 0;
            Byte[] fingerprint = Convert.FromBase64String(input.FingerPrintTemplate);

            foreach (var data in input.FingerPrintTemplateListToMatch)
            {
                SGFPMISOTemplateInfo sample_info = new SGFPMISOTemplateInfo();

                byte[] byteTemplate = Convert.FromBase64String(data.Template);
                err = m_FPM.GetIsoTemplateInfo(byteTemplate, sample_info);

                for (int i = 0; i < sample_info.TotalSamples; i++)
                {
                    bool matched = false;
                    err = m_FPM.MatchIsoTemplate(byteTemplate, i, fingerprint, 0, m_SecurityLevel, ref matched);
                    if (matched)
                    {
                        matchedRecord = data.PatienId;
                        break;
                    }
                }
            }

            return matchedRecord;
        }

        private void InitializeDevice()
        {
            m_FPM = new SGFingerPrintManager();

            Int32 error;
            SGFPMDeviceName device_name = SGFPMDeviceName.DEV_AUTO;
            Int32 device_id = (Int32)SGFPMPortAddr.USB_AUTO_DETECT;
            m_SecurityLevel = SGFPMSecurityLevel.NORMAL;

            m_DeviceOpened = false;


            error = m_FPM.Init(device_name);
            if(error != (Int32)SGFPMError.ERROR_NONE)
            {
                error = m_FPM.InitEx(m_ImageWidth, m_ImageHeight, m_Dpi);
            }
            
            if (error == (Int32)SGFPMError.ERROR_NONE)
            {
                m_FPM.CloseDevice();
                error = m_FPM.OpenDevice(device_id);
            }
            else
            {
                throw new ApplicationException("Unable to initialize scanner. Kindly check the connection");
            }

            if (error == (Int32)SGFPMError.ERROR_NONE)
            {
                SGFPMDeviceInfoParam pInfo = new SGFPMDeviceInfoParam();
                m_FPM.GetDeviceInfo(pInfo);
                m_ImageWidth = pInfo.ImageWidth;
                m_ImageHeight = pInfo.ImageHeight;
                m_Dpi = pInfo.ImageDPI;
                serialNo = pInfo.DeviceID;
            }

            error = m_FPM.SetTemplateFormat(SGFPMTemplateFormat.ISO19794);
            error = m_FPM.GetMaxTemplateSize(ref max_template_size);

            if (device_name != SGFPMDeviceName.DEV_UNKNOWN)
            {
                error = m_FPM.OpenDevice(device_id);
                if (error == (Int32)SGFPMError.ERROR_NONE)
                {
                    m_DeviceOpened = true;
                }
            }
        }
        

        public string ToBase64String(Byte[] imgData, ImageFormat imageFormat)
        {
            int colorval;
            Bitmap bmp = new Bitmap(m_ImageWidth, m_ImageHeight);

            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    colorval = (int)imgData[(j * m_ImageWidth) + i];
                    bmp.SetPixel(i, j, Color.FromArgb(colorval, colorval, colorval));
                }
            }

            string base64String = string.Empty;

            MemoryStream memoryStream = new MemoryStream();
            bmp.Save(memoryStream, imageFormat);

            memoryStream.Position = 0;
            byte[] byteBuffer = memoryStream.ToArray();

            memoryStream.Close();

            base64String = Convert.ToBase64String(byteBuffer);
            byteBuffer = null;

            return base64String;
        }

        public byte[] Base64StringByteArray(string base64String)
        { 
            byte[] byteBuffer = Convert.FromBase64String(base64String);
            return byteBuffer;
            
        }

         
    }
}
