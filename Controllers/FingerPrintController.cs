using FingerPrintModule.DAO;
using FingerPrintModule.Facade;
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
            var data = fingerPrintFacade.Capture(fingerPosition);

            var db = new DataAccess();
            var previously = db.GetPatientBiometricinfo();
            
            var matchedPatientId = fingerPrintFacade.Verify(new FingerPrintMatchInputModel
            {
                FingerPrintTemplate = data.Template,
                FingerPrintTemplateListToMatch = new List<FingerPrintInfo>(previously)

            });
            if(matchedPatientId != 0)
            {
                data.ErrorMessage = "Previous record found for this finger print";
            }
            return data; 
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
        public ResponseModel SaveToDatabase(List<FingerPrintInfo> fingerPrintList)
        {
            try
            {
                var db = new DataAccess();
                foreach (var f in fingerPrintList)
                {
                    db.Save(f);
                }                
                return new ResponseModel
                {
                    ErrorMessage = "Saved successfully",
                    IsSuccessful = true
                };
            }
            catch(Exception ex)
            {
                return new ResponseModel
                {
                    ErrorMessage = ex.Message,
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
            db.ExecuteScalar(string.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{0}';",connectionString.DatabaseName));

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
