using CommonLib.DAO;
using CommonLib.Facade;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace FingerPrintModule.Controllers
{
    public class FingerPrintController : ApiController
    {

        [HttpGet]
        public FingerPrintInfo CapturePrint(int fingerPosition)
        {
            FingerPrintFacade fingerPrintFacade = new FingerPrintFacade();
            var data = fingerPrintFacade.Capture(fingerPosition, out string err, false);

            if (string.IsNullOrEmpty(err))
            {
                var db = new DataAccess();
                var previously = db.GetPatientBiometricinfo();

                var matchedPatientId = fingerPrintFacade.Verify(new FingerPrintMatchInputModel
                {
                    FingerPrintTemplate = data.Template,
                    FingerPrintTemplateListToMatch = new List<FingerPrintInfo>(previously)

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

        [HttpGet]
        public List<FingerPrintInfo> CheckForPreviousCapture(string PatientUUID)
        {
            var db = new DataAccess();
            var patientInfo = db.RetrievePatientIdByUUID(PatientUUID);

            if (patientInfo != null && Int32.TryParse(patientInfo.Split('|')[1], out int pid))
            {
                var previously = db.GetPatientBiometricinfo(pid);
                return previously;
            }
            else
            {
                return null;
            }            
        }


        [HttpPost]
        public string MatchFingerPrint(FingerPrintMatchInputModel input)
        {

            FingerPrintFacade fingerPrintFacade = new FingerPrintFacade();
            var matchedPatientId = fingerPrintFacade.Verify(input);


            return JsonConvert.SerializeObject(new
            {
                PatientId = matchedPatientId,
                Matched = matchedPatientId != 0
            });
        }


        [HttpPost]
        public ResponseModel SaveToDatabase(SaveModel model)
        {
            var db = new DataAccess();
            string patientUUID = model.PatientUUID;
            var patientInfo = db.RetrievePatientIdByUUID(patientUUID);

            if (patientInfo != null && Int32.TryParse(patientInfo.Split('|')[1], out int pid))
            {
                model.FingerPrintList.ForEach(x => x.PatienId = pid);
                return db.SaveToDatabase(model.FingerPrintList);
            }
            else
            {
                return new ResponseModel
                {
                    ErrorMessage = "Invalid patientId supplied",
                    IsSuccessful = false
                };
            }            
        }


        [HttpPost]
        public ConnectionString SaveConnectionString(ConnectionString connectionString)
        {
            var db = new DataAccess(connectionString.FullConnectionString);
            //check if connection is valid
            //if connection string is wrong an error would occur here and will return error
            db.ExecuteScalar(string.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{0}';", connectionString.DatabaseName));

            db.ExecuteQuery(
               @"CREATE TABLE IF NOT EXISTS `biometricInfo` (
                        `biometricInfo_Id` INT(11) NOT NULL AUTO_INCREMENT,
                          `patient_Id` INT(11) NOT NULL,
                          `template` TEXT NOT NULL,
                          `imageWidth` INT(11) DEFAULT NULL,
                          `imageHeight` INT(11) DEFAULT NULL,
                          `imageDPI` INT(11) DEFAULT NULL,
                          `imageQuality` INT(11) DEFAULT NULL,
                          `fingerPosition` VARCHAR(50) DEFAULT NULL,
                          `serialNumber` VARCHAR(255) DEFAULT NULL,
                          `model` VARCHAR(255) DEFAULT NULL,
                          `manufacturer` VARCHAR(255) DEFAULT NULL,
                          `creator` INT(11) DEFAULT NULL,
                          `date_created` DATETIME DEFAULT NULL,
                          PRIMARY KEY(`biometricInfo_Id`),
                          FOREIGN KEY(patient_Id) REFERENCES patient(patient_Id),
                          FOREIGN KEY(creator) REFERENCES patient(creator)
                        ) ENGINE = MYISAM AUTO_INCREMENT = 2 DEFAULT CHARSET = utf8; "
                );

            db.WriteConnectionToFile(connectionString);
            return connectionString;
        }

        [HttpPost]
        public ConnectionString GetConnectionString()
        {
            var db = new DataAccess();
            return db.GetConnectionStringFromFile();
        }
    }
}
