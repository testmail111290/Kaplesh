using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Humana.Claims.CasNextGen.UI.Common;
using Humana.Claims.CasNextGen.UI.Common.Helpers;
using Humana.Claims.CasNextGen.UI.Common.Interfaces;
using Humana.Claims.CasNextGen.UI.Website.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Server.Interfaces;
using Website.ControlLines;

namespace Humana.Claims.CasNextGen.UI.Website.Controllers
{
    public class PMDIController : CasuiBaseController
    {
        public string BaseAPIURL { get; set; }
        public string ApiKeyName { get; set; }
        public string ApiKeyValue { get; set; }

        //private const string dateWatermark = "--/--/--";

        private IAPIServiceManager<PMDI> _serviceManager;
        public PMDIController(ILoggingHelper loggingHelper, IAPIServiceManager<PMDI> serviceManager) : base(loggingHelper)
        {
            BaseAPIURL = ConfigHelper.GetValue(Constants.NextGenPMDIAPIBaseURL);
            ApiKeyName = ConfigHelper.GetValue(Constants.NextGenPMDIAPIKeyName);
            ApiKeyValue = ConfigHelper.GetValue(Constants.NextGenPMDIAPIKeyValue);
            _serviceManager = serviceManager;
        }
        // GET: PMDI
        internal PMDI PMDIInfo(string controlValue, string userId, string uniqueId)
        {
            PMDI pmdiRes = new PMDI();
            try
            {
                pmdiRes = new PMDI()
                {
                    PMDIDto = new Common.PMDIDto()
                    {
                        ClientNumber = string.Empty,
                        ControlLine = controlValue,
                        UserId = userId,
                        UniqueKey = uniqueId,
                        Pf_key = " "
                    }
                };
                var pdiControlLine = new PMDIControlLine(controlValue);
                if (this.PreviousResponse != null && this.PreviousResponse is System.Object &&
                  ((PreviousResponse.GetType().Namespace.ToUpper().Substring(PreviousResponse.GetType().Namespace.Length - 4, 4) == "BCOP")
                  || (PreviousResponse.GetType().Namespace.ToUpper().Substring(PreviousResponse.GetType().Namespace.Length - 4, 4) == "PMDI")))
                {
                    //pmdiRes.PMDIDto.Response = PreviousResponse;
                }
                else
                {
                    //pmdiRes.PMDIDto.Response = this.BCOPResponse;
                }
                int PageNumber = 0;
                switch (pdiControlLine.PageName)
                {
                    case "PMDI":
                        PageNumber = 1;
                        break;
                    case "PMDN":
                        PageNumber = 2;
                        break;
                    case "PMD3":
                        PageNumber = 3;
                        break;
                    case "PMD4":
                        PageNumber = 4;
                        break;
                    default:
                        break;
                }
                pmdiRes.PMDIDto = PmdiServiceRequest(pmdiRes.PMDIDto,PageNumber, pdiControlLine);

               

                return pmdiRes;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public PMDIDto PmdiServiceRequest(PMDIDto objPMDIDto,int PageNumber,PMDIControlLine pdiControlLine)
        {
            try
            {
                string FileLocation = @"C:\Users\PIS8185\source\repos\CASNextGen.UI.Website\Website\Json\PDMI.json";
                StreamReader sr = new StreamReader(FileLocation);
                string jsonString = sr.ReadToEnd();
                objPMDIDto = JsonConvert.DeserializeObject<PMDIDto>(jsonString);
                int counts = PageNumber - 1;
                
                if (objPMDIDto.Iterations.Count>=PageNumber*3)
                {
                    objPMDIDto.Iterations=objPMDIDto.Iterations.Skip(counts * 3).Take(3).ToList();
                }

                switch (pdiControlLine.PageName)
                {
                    case "PMDI":
                        objPMDIDto.ControlLine = "PMDN";
                        PageNumber = 1;
                        break;
                    case "PMDN":
                        objPMDIDto.ControlLine = "PMD3";
                        PageNumber = 2;
                        break;
                    case "PMD3":
                        objPMDIDto.ControlLine = "PMD4";
                        PageNumber = 3;
                        break;
                    case "PMD4":
                        objPMDIDto.ControlLine = "PMDI";
                        PageNumber = 4;
                        break;
                    default:
                        break;
                }

                objPMDIDto.Headings = new List<Heading>();
                int count = objPMDIDto.Headings.Count;
                if (objPMDIDto.Headings.Count < 4)
                {
                    Heading objHeading = new Heading();
                    objHeading.Name = string.Empty;
                    while (objPMDIDto.Headings.Count != 4)
                    {
                        objPMDIDto.Headings.Add(objHeading);
                    }
                }
                if (objPMDIDto != null && objPMDIDto.Iterations.Count < 3)
                {
                    Iteration objIteration = new Iteration();
                    objIteration.IterationDetail = new IterationDetail();
                    
                    count = objPMDIDto.Iterations.Count;
                    while (objPMDIDto.Iterations.Count!=3)
                    {
                        objPMDIDto.Iterations.Add(objIteration);
                    }
                }

            }
            catch (Exception ex)
            {
            }
            return objPMDIDto;
        }
    }
}
