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


    }
}
