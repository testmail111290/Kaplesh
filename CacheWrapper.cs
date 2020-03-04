using Humana.WebClaim.CASUI.Common;
using Humana.WebClaim.CASUI.Common.Enums;
using Humana.WebClaim.CASUI.Web.Filters;
using Humana.WebClaim.CASUI.Web.Handlers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Reflection;
using Humana.WebClaim.CASUI.Common.DataTransferObjects;
using Humana.WebClaim.CASUI.Server;
using System.Web;
using System.Diagnostics;
using Newtonsoft.Json;
namespace Humana.WebClaim.CASUI.Web
{
    public class ControlLineController : CASUIBaseController
    {
        public static bool bybyFlag = false;
        public static bool isbybyFlag = false;


        // GET: ControlLine
        [HttpGet]
        [IpBanFilterAttribute]
        [SessiontimeoutFiltercsAttribute]
        [RefreshDetectFilterAttribute]
        public ActionResult ControlLine(string clientNumber, string userId = null)
        {
            CASClient client;

// Additional field specific validation to ensure against trust boundary violations
            bool idMatch = Utilities.RegexMatchNullIsValid(userId, Constants.userIdPattern);
            if (!idMatch)
            {
                    userId = null;
            }

            if (!System.Enum.IsDefined(typeof(CASClient), Convert.ToInt32(clientNumber)))
            {
                TempData["RedirectMessage"] = "Not a valid Client number.";
                return RedirectToAction("Menu", "Menu", new { userId = userId });
            }
            else
                client = (CASClient)Convert.ToInt32(clientNumber);

            if (client == CASClient.CASFileMaintenance)
            {
                clientNumber = Common.Constants.FileMaintenanceClientNumber;
            }


            bool IsRefreshed = false;
            bool IsClientExist = false;
            bybyFlag = true;

            ViewBag.SessionIdelWindowSize = ConfigHelper.SessionTimeout;

            /* Technically this block of code is not needed here because we DO NOT pull the
            * userid from the URL at this point, even if present. We get it from ActionResult
            * method argument. However, if we don't make this page behave the same as the other
            * pages, it causes confusion during testing.
            * It's confusing because the manipulated userId is not cleared from the URL; yet
            * CASUI continues to work with the previously validated userId.
            * All that being said....
            * This is testing comments with Git 19.09
            * All that being said....           * 
            * This is master branch change commit
            */
            string userIdUrl = string.Empty;
            string userIdPrevUrl = string.Empty;
            userIdUrl = GetValueFromURL("userId", false);


            //userIdPrevUrl = getValueFromURL("userId", true);
            if (!string.IsNullOrWhiteSpace(userIdUrl)                                   // if current userId not null
                    && !AuthenticatedUserList.Exists(s => s == userIdUrl.ToUpper())     //  and not authenticated
                                                                                        //&& !string.IsNullOrWhiteSpace(userIdPrevUrl)                        //  and previous userId not null
                                                                                        //&& userIdUrl != userIdPrevUrl
                    )                                      //  and previous userId not the same
            {
                ClientNumberList.Remove(userId.ToUpper() + ":" + clientNumber);

                // If this is an unauthenticated user, send them back to the menu.
                // This should only be the case if someone manually manipulates
                // the value in the URL.               
                TempData["RedirectMessage"] = "Userid not authenticated.";
                return RedirectToAction("Menu", "Menu");
            }
            /* End of technically unnecessary block of code. LOL */
            else if (!string.IsNullOrWhiteSpace(userIdUrl)                                   // if current userId not null
                    && AuthenticatedUserList.Exists(s => s == userIdUrl.ToUpper())     //  and already authenticated
                    )
            {
                this.UserId = userId==null? userIdUrl.ToUpper():userId;
            }
            else if (!String.IsNullOrEmpty(this.UserId.ToString()) && this.UserId.ToString() != Common.Constants.defaultObjectType)
            {
                userId = this.UserId.ToString();
            }
            else if (String.IsNullOrWhiteSpace(userId))
            {
                userId = ExtractUserId(User.Identity.Name).ToUpper();
            }


            if (RouteData.Values["IsRefreshed"] != null)
            {
                if (!string.IsNullOrEmpty(RouteData.Values["IsRefreshed"].ToString()) && Convert.ToBoolean(RouteData.Values["IsRefreshed"]))
                {
                    // page has been refreshed.
                    IsRefreshed = true;
                }
                else
                {
                    IsRefreshed = false;
                }
            }

            string uidClientNumber = userId + ":" + clientNumber;
            var match = ClientNumberList.FirstOrDefault(stringToCheck => stringToCheck.Contains(uidClientNumber));
            if (match != null)
            {
                IsClientExist = true;
            }
            else
            {
                List<string> clientlist = this.ClientNumberList == null ? new List<string>() : this.ClientNumberList;
                clientlist.Add(uidClientNumber);
                this.ClientNumberList = clientlist;
            }

            //bool res = DataValidationHelper.IsValidCasClient(this, clientNumber, out client);
            if (IsClientExist)
            {
                TempData["Clientlogin"] = clientNumber;
                //Hard coded values need to be moved to either const or common static named list
                return RedirectToAction("Menu", "Menu", new { userId = userId });

            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                this.UserId = userId.ToUpper();
            }

            if (client == CASClient.CASFileMaintenance)
            {
                this.ClientNumber = Common.Constants.FileMaintenanceClientNumber;
            }
            else
            {
                this.ClientNumber = (int)client; // client.ToString();
            }
            this.ActualClientNumber = (int)client;

            TempData["ClientNumber"] = DataValidationHelper.GetEnumDescription(client).Replace(Common.Constants.CasClientPrefix, string.Empty);
            TempData["UserId"] = this.UserId; // CR021

            var model = new ControlLineVM();
            model.ListModifier = ModifierList();
            SessionManipulation(this, clientNumber, false);
            ViewBag.clientno = (int)client;
            return View(model);
        }

        [HttpGet]
        [IpBanFilterAttribute]
        public ActionResult LoadPartialView(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;

            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");

            try
            {

                ////////////////////////////////////////////////
                LoggingHelper.Log(ElasticType.ControlLine, controlValue);
               ////////////////////////////////////////////////
                string[] controlList = controlValue.Split(',');
                if (!string.IsNullOrEmpty(controlValue) && controlValue.Contains("Prev"))
                {
                    var controllist = controlValue.Split(',');
                    if (controllist[controllist.Length - 1] == "Prev")
                    {
                        controlValue = CacheControlLine.ToString();
                    }
                }

                controlList = controlValue.Split(',');
                //var userid = CheckAndReturnUserID(Queryclientvalue);
                //this.UserId = (userid == "" ? User.Identity.Name.ToString().Replace(Constants.HumandStr, "") : userid);

                if (!string.IsNullOrWhiteSpace(this.UserId.ToString()))
                {
                    if (Array.IndexOf(Constants.validUMIDscreens, controlList[0]) > -1)
                    {
                        if (controlList[1].Contains("H"))
                        {
                            using (UMIDController UMID = new UMIDController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Umid umid = UMID.UMIDInfo(controlValue, this.UserId.ToString());

                                if (umid.UMIDDto.NextOperation == "Yes")
                                {
                                    using (MSSController MSS = new MSSController())
                                    {
                                        //this.UniqueId = NewGuid().ToString();
                                        Mss mss = MSS.MSSInfo(umid.UMIDDto.ControlLine, this.UserId.ToString());

                                        string indictr = string.Empty;
                                        var cntlline = checkForMRI(mss.MSSDto.Response, out indictr);
                                        if (!string.IsNullOrWhiteSpace(cntlline))
                                        {
                                            return LoadPartialViewMRIRedirection(cntlline, indictr, false);
                                        }

                                        if (mss.MSSDto.NextOperation == "Yes")
                                        {
                                            return LoadPartialViewUMIDMSS(umid.UMIDDto.ControlLine);
                                        }
                                        else
                                        {

                                            return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                        }
                                    }
                                }
                                else
                                {
                                    return PartialView("~/Views/Shared/UMID/UMID.cshtml", umid);
                                }
                            }
                        }
                    }

                    switch (controlList[0])
                    {
                        case "BYBY":
                        case "EXIT":
                            {
                                //var clientno = getValueFromURL("clientNumber");
                                string uidClientNo = userId + ":" + clientNumber;

                                //SessionManipulation(this, clientNumber, true, true, true);
                                // ClearSessionDefaultClient(this, typeof(CASUIBaseController), clientno); // CR021 MJS
                                TempData["Clientlogin"] = "";
                                isbybyFlag = true;
                                this.ClientNumberList.Remove(uidClientNo);
                                BybyFuction(clientNumber, "BYCL", true);
                                SessionManipulation(this, clientNumber, true, true, true); //VXP-moved this statement after byby call from above.
                                return loadPartialViewDefault("", "redirect");

                            }
                        #region---EDI,ELI,ETI,EOI,ESI,CSI,CPI,CFI

                        case "EDI":
                        case "ESD":
                            using (EDIController EDI = new EDIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Edi edi = EDI.EDIInfo(controlValue, this.UserId.ToString());
                                //E indicator Required
                                Default EdefaultModel = new Default();
                                if (ErrorHandlerForE(edi.EDIDto.Response, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }
                                if (edi.EDIDto.EdiFlag == "P" || (edi.EDIDto.ResMsgs != null && edi.EDIDto.EdiFlag == "P"))
                                    return PartialView("~/Views/Shared/EDI/EDIPhysician.cshtml", edi);
                                else if (edi.EDIDto.EdiFlag == "H" || (edi.EDIDto.ResMsgs != null && edi.EDIDto.EdiFlag == "H"))
                                    return PartialView("~/Views/Shared/EDI/EDIHospital.cshtml", edi);
                                return PartialView("~/Views/Shared/EDI/EDIHospital.cshtml", edi);
                            }
                        case "ELI":
                        case "ELN":
                            using (ELIController ELI = new ELIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Eli eli = ELI.ELIInfo(controlValue, this.UserId.ToString());
                                ////E indicator Required
                                Default EdefaultModel = new Default();
                                if (ErrorHandlerForE(eli.ELIDto.Response, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }
                                if (eli.ELIDto.EliFlag == "P" || (eli.ELIDto.ResMsgs != null && eli.ELIDto.EliFlag == "P"))
                                    return PartialView("~/Views/Shared/ELI/ELIPhysician.cshtml", eli);
                                else if (eli.ELIDto.EliFlag == "H" || (eli.ELIDto.ResMsgs != null && eli.ELIDto.EliFlag == "H"))
                                    return PartialView("~/Views/Shared/ELI/ELIHospital.cshtml", eli);
                                return PartialView("~/Views/Shared/ELI/ELIPhysician.cshtml", eli);
                            }


                        case "ELP":
                            using (ELIController ELI = new ELIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Eli eli = ELI.ELIInfo(controlValue, this.UserId.ToString());
                                //E indicator Required
                                Default EdefaultModel = new Default();
                                if (ErrorHandlerForE(eli.ELIDto.Response, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }
                                if (eli.ELIDto.EliFlag == "P")
                                    return PartialView("~/Views/Shared/ELI/ELIPhysician.cshtml", eli);
                                else if (eli.ELIDto.EliFlag == "H")
                                    return PartialView("~/Views/Shared/ELI/ELIHospital.cshtml", eli);
                                return PartialView("~/Views/Shared/ELI/ELIPhysician.cshtml", eli);
                            }

                        case "ETI":
                        case "ETN":
                        case "ETP":
                            using (ETIController ETI = new ETIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Eti eti = ETI.ETIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/ETI/ETI.cshtml", eti);
                            }

                        case "EOI":
                            using (EOIController EOI = new EOIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Eoi eoi = EOI.EOIInfo(controlValue, this.UserId.ToString());
                                string a = ViewBag.PreviousScreen;
                                return PartialView("~/Views/Shared/EOI/EOI.cshtml", eoi);
                            }

                        case "ESI":
                        case "ESN":
                        case "ESP":
                            using (ESIController ESI = new ESIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Esi esi = ESI.ESIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/ESI/ESI.cshtml", esi);
                            }

                        case "CSI":
                        case "CSN":

                            using (CSIController CSI = new CSIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Csi csi = CSI.CSIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/CSI/CSI.cshtml", csi);
                            }
                        case "CPI":
                        case "CPN":
                            using (CPIController CPI = new CPIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Cpi cpi = CPI.CPIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/CPI/CPI.cshtml", cpi);
                            }
                        case "CFI":
                        case "CFN":
                            using (CFIController CFI = new CFIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Cfi cfi = CFI.CFIInfo(controlValue, this.UserId.ToString());
                                //E indicator Required
                                Default EdefaultModel = new Default();
                                if (ErrorHandlerForE(cfi.CFIDto.Response, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }
                                if (cfi.CFIDto.CfiFlag == "P" || (cfi.CFIDto.ResMsgs != null && cfi.CFIDto.CfiFlag == "P"))
                                    return PartialView("~/Views/Shared/CFI/CFIP.cshtml", cfi);
                                else if (cfi.CFIDto.CfiFlag == "H" || (cfi.CFIDto.ResMsgs != null && cfi.CFIDto.CfiFlag == "H"))
                                    return PartialView("~/Views/Shared/CFI/CFIH.cshtml", cfi);

                                return PartialView("~/Views/Shared/CFI/CFIP.cshtml", cfi);
                            }

                        #endregion

                        #region---PDI,MHI,MRI,DMI,DGI,CRI,CWI,MAI, MGI,MSS

                        case "PDI":
                        case "PDU":
                        case "PDN":
                            using (PDIController PDI = new PDIController())
                            {

                                Pdi pdi = new Pdi();
                                this.UniqueId = NewGuid().ToString();

                                if ((controlList[0] == "PDN" || controlList[0] == "PDU") && this.PreviousResponse.ToString().Equals(Common.Constants.defaultObjectType))
                                {
                                    pdi.PDIDto = new PDIDto();
                                    pdi.PDIDto.ResMsgs = "Control line changed since initial";
                                    pdi.PDIDto.ResInnerMsgs = "";
                                    pdi.PDIDto.ControlLine = controlValue.ToString();
                                    pdi.PDIDto.PdiFlag = "";
                                }
                                else
                                    pdi = PDI.PDIInfo(controlValue, this.UserId.ToString(), null);
                                //E indicator Required
                                Default EdefaultModel = new Default();
                                if (ErrorHandlerForE(pdi.PDIDto.Response, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }
                                if (pdi.PDIDto.PdiFlag == "P")
                                    return PartialView("~/Views/Shared/PDI/PDIP.cshtml", pdi);
                                else if (pdi.PDIDto.PdiFlag == "H")
                                    return PartialView("~/Views/Shared/PDI/PDIH.cshtml", pdi);
                                return PartialView("~/Views/Shared/PDI/PDIH.cshtml", pdi);
                            }

                        case "MRI":
                        case "MAI":
                        case "MGI":
                        case "MTI":
                        case "MPI":
                        case "MXI":
                            string[] arrMPC = new string[] { "MPCR", "MPI", "MXI" };
                            string[] arrMaiMgiMti = new string[] { "MAI", "MGI", "MTI" };

                            if (arrMaiMgiMti.Any(s => s.Contains(controlList[0])) && PreviousResponse.ToString() != Common.Constants.defaultObjectType &&
                                    arrMaiMgiMti.Any(s => s.Contains(PreviousResponse.GetType().Namespace.ToUpper().Substring(PreviousResponse.GetType().Namespace.Length - 3, 3))))
                            {
                                return MaiMGIMTI(controlValue);

                            }

                            using (MSSController MSS = new MSSController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                                var MSSControlLine = mss.MSSDto.ControlLine.Split(',');
                                Default EdefaultModel = new Default();
                                if (ErrorHandlerForE(mss.MSSDto.Response, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }

                                //Commented the code to skip MSS page if user enters same control line again
                                //SIT Defect#354495
                                //if (this.CacheControlLine != null && this.CacheControlLine.ToString() != Constants.defaultObjectType)
                                //{
                                //    string preContLn = Convert.ToString(CacheControlLine);
                                //    if (preContLn == controlValue)
                                //    {
                                //        mss.MSSDto.NextOperation = "Yes";
                                //    }
                                //}
                                var isdirect = true;

                                if (mss.MSSDto.NextOperation == "Yes" || MssSkipCheck(MSSControlLine[0], controlValue))
                                {
                                    isdirect = false;
                                    if ((MSSControlLine[0] == "MRI" || MSSControlLine[0] == "MRU") && MSSControlLine[3] == "PTD")
                                    {
                                        return LoadPartialViewMRIPTD(controlValue);
                                    }
                                    else if ((MSSControlLine[0] == "MRI" || MSSControlLine[0] == "MRU") && MSSControlLine[3] == "CSP")
                                    {
                                        string indictr = string.Empty;
                                        var cntlline = checkForMRI(mss.MSSDto.Response, out indictr);
                                        if (!string.IsNullOrWhiteSpace(cntlline))
                                        {
                                            cntlline = cntlline.Replace("CSP", "").Trim();
                                            return LoadPartialViewMRIRedirection(cntlline, indictr, false);
                                        }
                                        return LoadPartialViewMRICSP(controlValue);
                                    }
                                    else if ((MSSControlLine[0] == "MRI" || MSSControlLine[0] == "MRU") )
                                    {
                                        if (MSSControlLine[0] == "MRU")
                                        {
                                            var cntrLine = mss.MSSDto.ControlLine.Replace("MRU", "MRI").Trim();
                                            return LoadPartialViewMRI(cntrLine, false);
                                        }
                                        else
                                        {
                                            return LoadPartialViewMRI(mss.MSSDto.ControlLine, false);
                                        }
                                    }

                                    else if (MSSControlLine[0] == "MAI")
                                    {
                                        return LoadPartialViewMAI(controlValue, "10", isdirect);
                                    }
                                    else if (MSSControlLine[0] == "MGI")
                                    {
                                        return LoadPartialViewMGI(controlValue, "10", isdirect);
                                    }
                                    else if (MSSControlLine[0] == "MTI")
                                    {
                                        return LoadPartialViewMTI(controlValue, "10", isdirect);
                                    }
                                    else if (arrMPC.Any(s => s.Contains(controlList[0])))
                                    {
                                        return LoadPartialViewMPI(controlValue, "", null, null, false);
                                    }
                                }
                                else
                                {

                                    if ((MSSControlLine[0] == "MRI" || MSSControlLine[0] == "MRU"))
                                        this.ISMSSHit = false;
                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }

                                return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                            }
                        case "MHN":
                        case "MHP":
                            Mhi mhiNew = new Mhi();

                            if (!this.PreviousResponse.ToString().Equals(Common.Constants.defaultObjectType))//&& !this.PreviousResponseModifier.ToString().Equals(Constants.defaultObjectType))
                            {
                                string strModifier = Utilities.GetModifierFromControlLine(controlValue);

                                if (!IsSuffChangeMSSRequird(this.PreviousResponse, controlValue))
                                {
                                    using (MSSController MSS = new MSSController())
                                    {
                                        Mss mss = new Mss();
                                        this.UniqueId = NewGuid().ToString();

                                        mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                                        this.CacheControlLine = controlValue;
                                        mss.MSSDto.ControlLine = controlValue;
                                        if (mss.MSSDto.NextOperation == "Yes")
                                        {
                                            //controlValue = "MHI";
                                            return LoadPartialViewMHI(controlValue, true);
                                        }
                                        else
                                        {
                                            return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                        }
                                    }
                                }

                                if (!string.IsNullOrEmpty(strModifier)
                                    && this.PreviousResponseModifier.ToString().Equals(Common.Constants.defaultObjectType)
                                    && strModifier != "F"
                                    )
                                {

                                    using (MSSController MSS = new MSSController())
                                    {
                                        Mss mss = new Mss();
                                        this.UniqueId = NewGuid().ToString();

                                        mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                                        this.CacheControlLine = controlValue;
                                        mss.MSSDto.ControlLine = controlValue;
                                        this.PreviousResponse = null;
                                        return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                    }

                                }
                                else
                                {
                                    using (MHIController mhi = new MHIController())
                                    {

                                        mhiNew = mhi.MHIInfo(controlValue, this.UserId.ToString(), false);
                                        if (mhiNew.ModifierDto.ResInnerMsgs.Contains("HISTORY MODIFIER INVALID") || mhiNew.ModifierDto.ResInnerMsgs.Contains("CONTROL LINE CHANGED SINCE INITIAL"))
                                        {
                                            Mss mss = new Mss();
                                            mss.MSSDto = new MSSDto();
                                            mss.MSSDto.ResMsgs = mhiNew.ModifierDto.ResInnerMsgs;
                                            mss.MSSDto.ControlLine = controlValue;


                                            return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                        }
                                        else
                                        {
                                            return PartialView("~/Views/Shared/MHI/MHI.cshtml", mhiNew);
                                        }

                                    }
                                }

                            }
                            else
                            {
                                using (MSSController MSS = new MSSController())
                                {
                                    Mss mss = new Mss();
                                    this.UniqueId = NewGuid().ToString();

                                    mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                                    this.CacheControlLine = controlValue;
                                    mss.MSSDto.ControlLine = controlValue;
                                    this.PreviousResponse = null;
                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }
                            }
                        case "MHI":

                            using (MSSController MSS = new MSSController())
                            {
                                Mss mss = new Mss();
                                this.UniqueId = NewGuid().ToString();

                                mss = MSS.MSSInfo(controlValue, this.UserId.ToString());

                                if (mss.MSSDto.NextOperation == "Yes" || (Utilities.IsMSSSkip(controlValue, this.ISMSSHit, this.CacheControlLine)))
                                {
                                    return LoadPartialViewMHI(controlValue, true);
                                }
                                else
                                {
                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);

                                }
                            }

                        case "MRN":
                            using (MRIController MRI = new MRIController())
                            {
                                if (controlList.Length > 3 && controlList[3] == "CSP")
                                {
                                    return LoadPartialViewMRICSP(controlValue);
                                }

                                Mri mri = new Mri();
                                mri = MRI.MRIInfo(controlValue, this.UserId.ToString(), null);
                                return PartialView("~/Views/Shared/MRI/MRI.cshtml", mri);
                            }

                        case "DMI":
                        case "DMN":
                            using (DMIController DMI = new DMIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Dmi dmi = DMI.DMIInfo(controlValue, this.UserId.ToString(), null, null, false);
                                //E indicator Required
                                Default EdefaultModel = new Default();
                                if (ErrorHandlerForE(dmi.DMIDto.Response, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }
                                if (dmi.DMIDto.F6 == "1")
                                    return PartialView("~/Views/Shared/DMI/DMISummary.cshtml", dmi);
                                else if (dmi.DMIDto.F6 == "2")
                                    return PartialView("~/Views/Shared/DMI/DMIDetail.cshtml", dmi);
                                else
                                {
                                    LoggingHelper.Log(LogType.ERROR, "DMI-" + this.UserId.ToString() + " DMN Default", "IncorrectValue");
                                    return PartialView("", dmi);
                                }
                            }

                        case "DGI":
                        case "DGN":
                            using (DGIController DGI = new DGIController())
                            {
                                Dgi Dgi;
                                this.UniqueId = NewGuid().ToString();
                                if (controlList.Length >= 3)
                                    Dgi = DGI.DGIInfo(controlValue, this.UserId.ToString(), controlList[2]);
                                else
                                    Dgi = DGI.DGIInfo(controlValue, this.UserId.ToString(), null);
                                if (Dgi.DGIDto != null && string.IsNullOrEmpty(Dgi.DGIDto.ResMsgs))
                                {
                                    if (controlList[2].Contains("9"))
                                        return PartialView("~/Views/Shared/DGI/DGI1.cshtml", Dgi);
                                    else
                                        return PartialView("~/Views/Shared/DGI/DGI.cshtml", Dgi);
                                }
                                return PartialView("~/Views/Shared/DGI/DGI.cshtml", Dgi);
                            }

                        case "CRI":
                            using (MSSController MSS = new MSSController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());

                                if (mss.MSSDto.NextOperation == "Yes" || (Utilities.IsMSSSkip(controlValue, this.ISMSSHit, this.CacheControlLine)))
                                {
                                    return LoadPartialViewCRI(controlValue, false);
                                }
                                else
                                {

                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }
                            }

                        case "CRN":
                        case "CRP":
                        case "CRU":
                        case "CRA":
                            return LoadPartialViewCRI(controlValue, false);
                        case "CWI":
                            using (MSSController MSS = new MSSController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());

                                if (mss.MSSDto.NextOperation == "Yes" || (Utilities.IsMSSSkip(controlValue, this.ISMSSHit, this.CacheControlLine)))
                                {
                                    return LoadPartialViewCWI(controlValue, false);
                                }
                                else
                                {
                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }
                            }

                        case "CWN":
                        case "CWP":
                        case "CWU":
                        case "CWA":
                            return LoadPartialViewCWI(controlValue, false);

                        case "MAN":
                        case "MAP":
                            using (MAIController MAI = new MAIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mai mai = MAI.MAIInfo(controlValue, this.UserId.ToString(), "");
                                string indictr = string.Empty;
                                var cntlline = checkForMRI(mai.MAIDto.Response, out indictr);
                                if (!string.IsNullOrWhiteSpace(cntlline))
                                {
                                    return LoadPartialViewMRIRedirection(cntlline, indictr);
                                }
                                return PartialView("~/Views/Shared/MAI/MAIDetail.cshtml", mai);
                            }
                        case "MGN":
                        case "MGP":
                            using (MGIController MGI = new MGIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mgi mgi = MGI.MGIInfo(controlValue, this.UserId.ToString(), "");
                                string indictr = string.Empty;
                                var cntlline = checkForMRI(mgi.MGIDto.Response, out indictr);
                                if (!string.IsNullOrWhiteSpace(cntlline))
                                {
                                    return LoadPartialViewMRIRedirection(cntlline, indictr);
                                }
                                return PartialView("~/Views/Shared/MGI/MGIDetail.cshtml", mgi);
                            }
                        case "MTN":
                        case "MTP":
                            using (MTIController MTI = new MTIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mti mti = MTI.MTIInfo(controlValue, this.UserId.ToString(), "");
                                string indictr = string.Empty;
                                var cntlline = checkForMRI(mti.MTIDto.Response, out indictr);
                                if (!string.IsNullOrWhiteSpace(cntlline))
                                {
                                    return LoadPartialViewMRIRedirection(cntlline, indictr);
                                }
                                return PartialView("~/Views/Shared/MTI/MTIDetail.cshtml", mti);
                            }
                        #endregion

                        #region --- ASI,XCI,SDI,CNI,MDI,MTI,RFI,PCI

                        case "ASI":
                        case "ASN":
                            using (ASIController ASI = new ASIController())
                            {

                                this.UniqueId = NewGuid().ToString();
                                Asi asi = ASI.ASIInfo(controlValue, this.UserId.ToString(), false, "-1");
                                return PartialView("~/Views/Shared/ASI/ASI.cshtml", asi);
                            }

                        case "XCI":
                        case "XCN":
                        case "XCP":
                        case "XRI":
                        case "XRN":
                        case "XRP":
                        case "XHI":
                        case "XHN":
                        case "XHP":
                            using (XciXriXhiController XciXriXhiController = new XciXriXhiController())
                            {
                                //Removing this custom logic as fix for UAT Defect#330868, verified with the control line with 9 digit claim number
                                //as per the SIT Defect#324224
                                //controlValue = controlValue.Replace(" ", string.Empty);

                                XciXriXhi xcixrixhi;
                                this.UniqueId = NewGuid().ToString();
                                xcixrixhi = XciXriXhiController.XciXriXhiInfo(controlValue, this.UserId.ToString(), null, null, null, null, false);
                                //E indicator Required
                                Default EdefaultModel = new Default();
                                if (ErrorHandlerForE(xcixrixhi.XriXciXhiDto.Response, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }

                                if (!string.IsNullOrEmpty(xcixrixhi.XriXciXhiDto.PageIndicator) && xcixrixhi.XriXciXhiDto.PageIndicator == "1")
                                {
                                    return PartialView("~/Views/Shared/XCI/XCISummary.cshtml", xcixrixhi);
                                }
                                else if (!string.IsNullOrEmpty(xcixrixhi.XriXciXhiDto.PageIndicator) && xcixrixhi.XriXciXhiDto.PageIndicator == "2")
                                {
                                    return PartialView("~/Views/Shared/XCI/XCIDetails.cshtml", xcixrixhi);
                                }
                                else if (string.IsNullOrEmpty(xcixrixhi.XriXciXhiDto.PageIndicator) || xcixrixhi.XriXciXhiDto.PageIndicator == "3")
                                {
                                    return PartialView("~/Views/Shared/XCI/XCISummary.cshtml", xcixrixhi);
                                }
                                else if (!string.IsNullOrEmpty(xcixrixhi.XriXciXhiDto.PageIndicator) && xcixrixhi.XriXciXhiDto.PageIndicator == "Z")
                                {
                                    controlValue = xcixrixhi.XriXciXhiDto.ControlLine;
                                    return LoadPartialViewMPI(controlValue, "", null, null);
                                }

                                LoggingHelper.Log(LogType.ERROR, "XCI-" + this.UserId.ToString() + " LoadPartialView", "IncorrectValue");
                                return PartialView("", xcixrixhi);
                            }

                        case "CNI":
                        case "CNN":
                        case "CNP":
                            using (CNIController CNI = new CNIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Cni Cni = CNI.CNIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/CNI/CNI.cshtml", Cni);
                            }

                        case "MDI":
                        case "MDN":
                        case "MDP":
                            using (MDIController MDI = new MDIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mdi Mdi = MDI.MdiInfo(controlValue, this.UserId.ToString(), null, true);
                                var MdiControlLine = Mdi.MdiDto.ControlLine.Split(',');
                                //E indicator Required
                                Default EdefaultModel = new Default();
                                if (ErrorHandlerForE(Mdi.MdiDto.Response, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }
                                string indictr = string.Empty;
                                var cntlline = checkForMRI(Mdi.MdiDto.Response, out indictr);
                                if (!string.IsNullOrWhiteSpace(cntlline))
                                {
                                    return LoadPartialViewMRIRedirection(cntlline, indictr);
                                }
                                else if (Mdi.MdiDto.IsSummaryPage)
                                {
                                    return PartialView("~/Views/Shared/MDI/MDIFirst.cshtml", Mdi);
                                }
                                else
                                {
                                    return PartialView("~/Views/Shared/MDI/MDISecond.cshtml", Mdi);
                                }
                            }

                        case "RFI":

                            using (MSSController MSS = new MSSController())
                            {
                                this.UniqueId = NewGuid().ToString();

                                Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());

                                if (mss.MSSDto.NextOperation == "Yes" || (Utilities.IsMSSSkip(controlValue, this.ISMSSHit, this.CacheControlLine)))
                                {
                                    return LoadPartialViewRFI(controlValue, null, false);
                                }
                                else
                                {

                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }
                            }

                        case "RFN":
                        case "RFP":
                        case "RFU":
                        case "RFA":
                            return LoadPartialViewRFI(controlValue, null, false);

                        case "PCI":
                            using (MSSController MSS = new MSSController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                                if (mss.MSSDto.NextOperation == "Yes" || (Utilities.IsMSSSkip(controlValue, this.ISMSSHit, this.CacheControlLine)))
                                {
                                    return LoadPartialViewPCI(controlValue, false);
                                }
                                else
                                {
                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }
                            }

                        case "PCN":
                        case "PCU":
                        case "PCA":
                            return LoadPartialViewPCI(controlValue, false);

                        #endregion

                        #region---RGI,RSI,API,MSI,PRI,ISI,LTI,GNI
                        case "RGI":
                            if (controlValue == "RGI,")
                            {
                                using (RGMController RGM = new RGMController())
                                {
                                    this.UniqueId = NewGuid().ToString();
                                    RgmRgiRgeDto rgm = RGM.RGMInfo(controlValue, this.UserId.ToString(), null);
                                    return PartialView("~/Views/Shared/RGI/RGM.cshtml", rgm);
                                }
                            }
                            else
                            {
                                using (RGIController RGI = new RGIController())
                                {
                                    Rgm modelRgm = new Rgm();
                                    this.UniqueId = NewGuid().ToString();
                                    RgmRgiRgeDto rgi = RGI.RGIInfo(controlValue, "", this.UserId.ToString(), null);
                                    //E indicator Required
                                    Default EdefaultModel = new Default();
                                    object objresponse = rgi.RGIDto != null ? rgi.RGIDto.Response : (rgi.RGMDto != null ? rgi.RGMDto.Response : rgi.RGEDto.Response);

                                    if (ErrorHandlerForE(objresponse, out  EdefaultModel))
                                    {
                                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                    }
                                    if (rgi.PageIndicator == "1" || rgi.PageIndicator == "3")
                                    {
                                        Rgi rgimodel = new Rgi();
                                        rgimodel.RGIDto = rgi.RGIDto;
                                        return PartialView("~/Views/Shared/RGI/RGI.cshtml", rgimodel);
                                    }
                                    else if (rgi.PageIndicator == "2")
                                    {
                                        modelRgm.RGMDto = rgi.RGMDto;
                                        return PartialView("~/Views/Shared/RGI/RGM.cshtml", modelRgm);
                                    }
                                    return PartialView("~/Views/Shared/RGI/RGI.cshtml", rgi);

                                }
                            }

                        case "RGN":
                        case "RGP":
                        case "RGU":
                        case "RGA":
                            using (RGIController RGI = new RGIController())
                            {
                                Rgi modelRgi = new Rgi();
                                Rgm modelRgm = new Rgm();
                                Rge modelRge = new Rge();

                                this.UniqueId = NewGuid().ToString();
                                RgmRgiRgeDto rgi = new RgmRgiRgeDto();
                                rgi = RGI.RGIInfo(controlValue, "", this.UserId.ToString(), null);
                                //E indicator Required
                                Default EdefaultModel = new Default();
                                object objresponse = rgi.RGIDto != null ? rgi.RGIDto.Response : (rgi.RGMDto != null ? rgi.RGMDto.Response : rgi.RGEDto.Response);

                                if (ErrorHandlerForE(objresponse, out  EdefaultModel))
                                {
                                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                                }
                                if (rgi.PageIndicator == "1" || rgi.PageIndicator == "3")
                                {
                                    modelRgi.RGIDto = rgi.RGIDto;
                                    return PartialView("~/Views/Shared/RGI/RGI.cshtml", modelRgi);
                                }
                                else if (rgi.PageIndicator == "2")
                                {
                                    modelRgm.RGMDto = rgi.RGMDto;
                                    return PartialView("~/Views/Shared/RGI/RGM.cshtml", modelRgm);
                                }

                                else if (rgi.PageIndicator == "4")
                                {
                                    modelRge.RGEDto = rgi.RGEDto;
                                    return PartialView("~/Views/Shared/RGI/RGE.cshtml", modelRge);
                                }
                                return PartialView("~/Views/Shared/RGI/RGI.cshtml", modelRgi);
                            }
                        case "RSI":

                            using (MSSController MSS = new MSSController())
                            {
                                Mss mss = new Mss();
                                this.UniqueId = NewGuid().ToString();
                                mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                                if (mss.MSSDto.NextOperation == "Yes" || (Utilities.IsMSSSkip(controlValue, this.ISMSSHit, this.CacheControlLine)))
                                {
                                    return LoadPartialViewRSI(controlValue, null, false);
                                }
                                else
                                {

                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }
                            }

                        case "RSN":
                        case "RSP":
                            return LoadPartialViewRSI(controlValue, null, false);

                        case "API":
                        case "API1":
                            using (APIController API = new APIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Api api = API.APIInfo(controlValue, this.UserId.ToString(), null);
                                if (controlList[1] == "1")
                                {
                                    return PartialView("~/Views/Shared/API/API1.cshtml", api);
                                }
                                else
                                {
                                    return PartialView("~/Views/Shared/API/API.cshtml", api);
                                }
                            }
                        case "MSI":
                        case "MSN":

                            using (MSIController MSI = new MSIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Msi msi = MSI.MSIInfo(controlValue, this.UserId.ToString());
                                var MsiControlLine = msi.MSIDto.ControlLine.Split(',');
                                if (MsiControlLine[0] == "MRI")
                                {
                                    return LoadPartialViewMSS(msi.MSIDto.ControlLine);
                                }
                                else
                                {
                                    return PartialView("~/Views/Shared/MSI/MSI.cshtml", msi);
                                }
                            }
                        case "PRI":

                            using (MSSController MSS = new MSSController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                                if (mss.MSSDto.NextOperation == "Yes" || (Utilities.IsMSSSkip(controlValue, this.ISMSSHit, this.CacheControlLine)))
                                {
                                    return LoadPartialViewPRI(controlValue, false);
                                }
                                else
                                {

                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }
                            }

                        case "PRN":
                        case "PRP":
                        case "PRU":
                            //case "PRA":
                            return LoadPartialViewPRI(controlValue, false);
                        case "ISI":
                        case "ISN":
                        case "ISP":
                            using (ISIController ISI = new ISIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Isi Isi = ISI.ISIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/ISI/ISI.cshtml", Isi);
                            }
                        case "GNI":
                        case "GNN":
                        case "GNP":
                            using (GNIController GNI = new GNIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Gni gni = GNI.GniInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/GNI/GNI.cshtml", gni);
                            }

                        #endregion

                        #region---PSI,CTI,PXI,OCI,CLI,BCI,SNI,RDI
                        case "PSI":
                            using (PSIController PSI = new PSIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Psi psi = PSI.PSIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/PSI/PSI.cshtml", psi);
                            }
                        case "PXI":
                            using (PXIController PXI = new PXIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Pxi pxi = PXI.PXIInfo(controlValue, this.UserId.ToString(), null);
                                return PartialView("~/Views/Shared/PXI/PXI.cshtml", pxi);
                            }
                        case "OCI":
                            using (MSSController MSS = new MSSController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                                if (mss.MSSDto.NextOperation == "Yes")
                                {
                                    return LoadPartialViewOCI(controlValue, false);
                                }
                                else
                                {
                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }
                            }
                        case "OCN":
                        case "OCU":
                        case "OCA":
                            using (OCIController OCI = new OCIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Oci Oci = OCI.OCIInfo(controlValue, this.UserId.ToString(), null);
                                return PartialView("~/Views/Shared/OCI/OCI.cshtml", Oci);
                            }

                        case "SNI":
                        case "SNN":
                        case "SNP":
                        case "SNU":
                        case "SNA":
                            using (SNIController SNI = new SNIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Sni Sni = SNI.SNIInfo(controlValue, this.UserId.ToString(), null);
                                return PartialView("~/Views/Shared/SNI/SNI.cshtml", Sni);
                            }

                        #endregion

                        #region---PAI,XHI,XRI,

                        case "PAI":
                        case "PAN":
                            using (PAIController PAI = new PAIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Pai Pai = PAI.PaiInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/PAI/PAI.cshtml", Pai);
                            }

                        #endregion

                        #region---BPI,MGI,IPI,EBI,
                        case "ESC":
                            using (ESCController ESC = new ESCController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Esc Esc = ESC.ESCInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/ESC/ESC.cshtml", Esc);

                            }


                        case "IPI":
                        case "IPN":
                            using (IPIController IPI = new IPIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Ipi ipi = IPI.IPIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/IPI/IPI.cshtml", ipi);
                            }
                        #endregion

                        #region---Planload
                        case "BCOP":
                            using (BCOPController BCOP = new BCOPController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Bcop bcop = BCOP.BCOPInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/BCOP/BCOP.cshtml", bcop);
                            }
                        case "PMDI":
                        case "PMDN":
                        case "PMD3":
                        case "PMD4":
                            using (PMDIController PMDI = new PMDIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                PMDI pmdi = PMDI.PMDIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/PMDI/PMDI.cshtml", pmdi);
                            }
                        case "PMCI":
                        case "PMCN":
                        case "PMC3":
                        case "PMC4":
                        case "PMCU":
                            using (PMCIController Pmci = new PMCIController())
                            {
                                Pmci pmci = new Pmci();
                                this.UniqueId = NewGuid().ToString();
                                pmci = Pmci.PMCIInfo(controlValue, this.UserId.ToString(), pmci.PMCIDto);  //ToDo: Is this really supposed to use the pmci object, which has not been used yet.
                                return PartialView("~/Views/Shared/Pmci/Pmci.cshtml", pmci);
                            }

                        case "PMEI":
                        case "PMEN":
                        case "PME3":
                        case "PME4":
                        case "PME5":
                            using (PMEIController PMEI = new PMEIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Pmei pmei = PMEI.PMEIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/PMEI/PMEI.cshtml", pmei);
                            }
                        case "PMFI":
                        case "PMFN":
                        case "PMF3":
                        case "PMF4":
                            using (PMFIController PMFI = new PMFIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Pmfi pmfi = PMFI.PMFIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/PMFI/PMFI.cshtml", pmfi);
                            }
                        case "S5PI":
                            return LoadPartialViewS5PI(controlValue, null, false);

                        case "UF7I":
                            using (UF7IController uf7iController = new UF7IController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                UF7I uf7imodel = uf7iController.UF7IInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/UF7I/UF7I.cshtml", uf7imodel);
                            }
                        case "ZIPI":
                            return LoadPartialViewZIPI(controlValue);

                        case "S6PI":
                            return LoadPartialViewS6PI(controlValue, null, false);

                        case "UF6I":
                            return LoadPartialViewUF6I(controlValue, null, false);

                        #endregion

                        #region---Demo6

                        case "MCI":
                            using (MCIController MCI = new MCIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Mci mci = MCI.MCIInfo(controlValue, this.UserId.ToString(), null);
                                return PartialView("~/Views/Shared/MCI/MCI.cshtml", mci);
                            }
                        #endregion

                        case "MPCR":
                            return LoadPartialViewMPI(controlValue, "", null, null);
                        case "OSI":
                            using (OSIController OSI = new OSIController())
                            {
                                this.UniqueId = NewGuid().ToString();
                                Osi osi = OSI.OSIInfo(controlValue, this.UserId.ToString());
                                return PartialView("~/Views/Shared/OSI/OSIDetail.cshtml", osi);
                            }

                        case "CMI":
                            using (MSSController MSS = new MSSController())
                            {
                                this.UniqueId = NewGuid().ToString();

                                Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                                if (mss.MSSDto.NextOperation == "Yes" || (Utilities.IsMSSSkip(controlValue, this.ISMSSHit, this.CacheControlLine)))
                                {
                                    return LoadPartialViewCMI(controlValue, null, false);
                                }
                                else
                                {
                                    this.ISMSSHit = true;
                                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                                }
                            }
                        case "CMN":
                        case "CMP":
                            return LoadPartialViewCMI(controlValue, null, false);

                        default:
                            {
                                LoggingHelper.Log(LogType.ERROR, "Default-" + this.UserId.ToString() + " LoadPartialView", "IncorrectValue");
                                return PartialView("");
                            }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(clientNumber))
                    SessionManipulation(this, clientNumber, false, true);
                throw;
            }
            finally
            {
                if (controlValue != "BYBY" && controlValue != "EXIT")
                {
                    if (!string.IsNullOrWhiteSpace(clientNumber))
                        SessionManipulation(this, clientNumber, false);
                }
            }

        }

        #region---Method Call from JQuery/Ajax to Update

        [HttpPost]
        public PartialViewResult loadPartialViewDMI(string controlLine, string strsummaryDTO, string strdetailDto, bool F6)
        {
            Common.DMIDatailDto detailDto = new Common.DMIDatailDto();
            Common.DMISummaryDto summaryDTO = new Common.DMISummaryDto();
            if (!string.IsNullOrEmpty(strsummaryDTO) && !string.IsNullOrEmpty(strdetailDto))
            {
                summaryDTO = JsonConvert.DeserializeObject<Common.DMISummaryDto>(HttpUtility.UrlDecode(strsummaryDTO), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
                detailDto = JsonConvert.DeserializeObject<Common.DMIDatailDto>(HttpUtility.UrlDecode(strdetailDto), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlLine);

                using (DMIController DMI = new DMIController())
                {

                    this.UniqueId = NewGuid().ToString();

                    Dmi Dmi = DMI.DMIInfo(controlLine, this.UserId.ToString(), summaryDTO, detailDto, F6);
                    //E indicator Required
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(Dmi.DMIDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }
                    if (Dmi.DMIDto.F6 == "1")
                        return PartialView("~/Views/Shared/DMI/DMISummary.cshtml", Dmi);
                    else if (Dmi.DMIDto.F6 == "2")
                        return PartialView("~/Views/Shared/DMI/DMIDetail.cshtml", Dmi);
                    else
                    {
                        LoggingHelper.Log(LogType.ERROR, "DMI-" + this.UserId.ToString() + " else statement", "IncorrectValue");
                        return PartialView("", Dmi);
                    }
                }

            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public PartialViewResult LoadPartialViewUpdate_PDI(string controlLine, string strpdiDTO)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlLine, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlLine, "Control line contains invalid characters");
            }

            Pdi Pdi = new Pdi();
            Common.PDIDto pdiDTO = new Common.PDIDto();

            if (!string.IsNullOrEmpty(strpdiDTO))
            {
                pdiDTO = JsonConvert.DeserializeObject<Common.PDIDto>(HttpUtility.UrlDecode(strpdiDTO), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (PDIController PDI = new PDIController())
                {
                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();
                    Pdi.PDIDto = pdiDTO;

                    Pdi = PDI.PDIInfo(controlLine, this.UserId.ToString(), pdiDTO);
                    if (Pdi.PDIDto.PdiFlag == "P")
                        return PartialView("~/Views/Shared/PDI/PDIP.cshtml", Pdi);
                    else if (Pdi.PDIDto.PdiFlag == "H")
                        return PartialView("~/Views/Shared/PDI/PDIH.cshtml", Pdi);
                    return PartialView("~/Views/Shared/PDI/PDIH.cshtml", Pdi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpPost]
        public PartialViewResult LoadPartialViewUpdate_CWI(string controlLine, string strcwiDTO)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlLine, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlLine, "Control line contains invalid characters");
            }

            Cwi cwi = new Cwi();
            Common.CWIDto cwiDTO = new Common.CWIDto();
            if (!string.IsNullOrEmpty(strcwiDTO))
            {
                cwiDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.CWIDto>(HttpUtility.UrlDecode(strcwiDTO), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (CWIController CWI = new CWIController())
                {
                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();
                    cwi.CWIDto = cwiDTO;

                    cwi = CWI.CWIInfo(controlLine, this.UserId.ToString(), cwiDTO);
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(cwi.CWIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }
                    return PartialView("~/Views/Shared/CWI/CWI.cshtml", cwi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpPost]
        public PartialViewResult LoadPartialViewUpdate_PCI(string controlLine, string strpciDTO)
        {
            Pci pci = new Pci();
            Common.PCIDto pciDTO = new Common.PCIDto();
            if (!string.IsNullOrEmpty(strpciDTO))
            {
                pciDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.PCIDto>(HttpUtility.UrlDecode(strpciDTO), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (PCIController PCI = new PCIController())
                {

                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();
                    pci.PCIDto = pciDTO;

                    pci = PCI.PCIInfo(controlLine, this.UserId.ToString(), pciDTO);

                    return PartialView("~/Views/Shared/PCI/PCI.cshtml", pci);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpPost]
        public PartialViewResult LoadPartialViewUpdate_CRI(string controlLine, string strcriDTO)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlLine, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlLine, "Control line contains invalid characters");
            }

            Cri cri = new Cri();
            Common.CRIDto criDTO = new Common.CRIDto();
            if (!string.IsNullOrEmpty(strcriDTO))
            {
                criDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.CRIDto>(HttpUtility.UrlDecode(strcriDTO), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }

            //VXP-investigate - why no get client number earlier
            string userId = string.Empty;
            string clientvalue = string.Empty;
            ValidateUserClient(out userId, out clientvalue); //improvement opurtunity- what to do if userid is not authenticated?

            //var clientvalue = ClientNumber.ToString();

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (CRIController CRI = new CRIController())
                {

                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();
                    cri.CRIDto = criDTO;

                    cri = CRI.CRIInfo(controlLine, this.UserId.ToString(), criDTO);
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(cri.CRIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }
                    return PartialView("~/Views/Shared/CRI/CRI.cshtml", cri);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

            return PartialView("", cri);

        }
        [HttpPost]
        public PartialViewResult LoadPartialViewS5PI(string controlLine, Common.S5PIDto S5piDto, bool isdirect = true)
        {
            S5PI s5pi = new S5PI();
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                using (S5PIController S5PI = new S5PIController())
                {

                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();
                    s5pi.S5PIDto = S5piDto;

                    s5pi = S5PI.S5PIInfo(controlLine, this.UserId.ToString(), s5pi.S5PIDto);
                    return PartialView("~/Views/Shared/S5PI/S5PI.cshtml", s5pi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }


        }
        [HttpPost]
        public PartialViewResult LoadPartialViewS6PI(string controlLine, Common.S6piDto S6piDto, bool isdirect = true)
        {
            S6PI s6pi = new S6PI();
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                using (S6PIController S6PI = new S6PIController())
                {

                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();
                    s6pi.S6PIDto = S6piDto;

                    s6pi = S6PI.S6PIInfo(controlLine, this.UserId.ToString(), s6pi.S6PIDto);
                    return PartialView("~/Views/Shared/S6PI/S6PI.cshtml", s6pi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public PartialViewResult LoadPartialViewZIPI(string controlLine, Common.ZIPIDto zipiDTO = null)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            using (ZIPIController ZIPI = new ZIPIController())
            {
                Zipi zipi = new Zipi();
                this.UniqueId = NewGuid().ToString();
                zipi.ZIPIDto = zipiDTO;

                zipi = ZIPI.ZIPIInfo(controlLine, this.UserId.ToString(), zipiDTO);

                return PartialView("~/Views/Shared/ZIPI/ZIPI.cshtml", zipi);
            }
        }

        [HttpPost]
        public PartialViewResult LoadPartialViewUpdateRFI(string controlLine, string strrfiDTO)

        {
            var RFIControlLine = controlLine.Split(',');

            Common.RFIDto rfiDTO = new Common.RFIDto();
            if (!string.IsNullOrEmpty(strrfiDTO))
            {
                rfiDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.RFIDto>(HttpUtility.UrlDecode(strrfiDTO), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }

            //VXP-investigate - why no get client number earlier
            string userId = string.Empty;
            string clientvalue = string.Empty;
            ValidateUserClient(out userId, out clientvalue); //improvement opurtunity- what to do if userid is not authenticated?

            //var clientvalue = ClientNumber.ToString();

            try
            {
                SessionManipulation(this, clientvalue, true);
                if ((RFIControlLine[0] == "RFA" || RFIControlLine[0] == "RFU" || RFIControlLine[0] == "RFN" || RFIControlLine[0] == "RFP") && this.PreviousResponse.ToString().Equals(Common.Constants.defaultObjectType)) //RFI changes
                {
                    using (MSSController MSS = new MSSController())
                    {
                        this.UniqueId = NewGuid().ToString();
                        Mss mss = MSS.MSSInfo(controlLine, this.UserId.ToString());
                        if (mss.MSSDto.NextOperation == "Yes")
                            return LoadPartialViewRFI(controlLine, null, false);
                        else
                        {
                            mss.MSSDto.ControlLine = controlLine;
                            return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                        }
                    }
                }

                using (RFIController RFI = new RFIController())
                {
                    Rfi rfi = new Rfi();
                    RsiRfiDto rfiDto = new RsiRfiDto();
                    this.UniqueId = NewGuid().ToString();
                    rfiDto.RFIDto = rfiDTO;
                    ModelState.Clear();

                    rfiDto = RFI.RFIInfo(controlLine, this.UserId.ToString(), rfiDto.RFIDto, null);
                    rfi.RFIDto = rfiDto.RFIDto;
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(rfi.RFIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }
                    return PartialView("~/Views/Shared/RFI/RFI.cshtml", rfi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public ActionResult LoadPartialViewUpdateXCI(string controlValue, string strclaimsDetailDto = null, string strclaimsDetailGridData = "", bool F9 = false)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    this.UserId = userId.ToUpper();
                }
                else
                {
                    this.UserId = ExtractUserId(User.Identity.Name).ToUpper();
                }

                SessionManipulation(this, clientvalue, true);
                Common.XciXriXhiDetailDto claimsDetailDto = new Common.XciXriXhiDetailDto();
                using (XciXriXhiController XciXriXhiController = new XciXriXhiController())
                {
                    XriXciXhiDto xciDto = new XriXciXhiDto();
                    this.UniqueId = NewGuid().ToString();
                    xciDto.XciDetailDto = claimsDetailDto;

                    if (!string.IsNullOrEmpty(strclaimsDetailGridData) && !string.IsNullOrEmpty(strclaimsDetailDto))
                    {
                        claimsDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.XciXriXhiDetailDto>(HttpUtility.UrlDecode(strclaimsDetailDto), new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        });
                        List<Common.XciXriXhiDetailGridDto> XCIDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.XciXriXhiDetailGridDto>>(HttpUtility.UrlDecode(strclaimsDetailGridData), new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        });
                        XCIDetailDto.ForEach(xci => xci.dtl_plc_trtmo = xci.dtl_plc_trtmo.ToUpper());
                        XCIDetailDto.ForEach(xci => xci.dtl_ctr_deco = xci.dtl_ctr_deco.ToUpper());
                        XCIDetailDto.ForEach(xci => xci.dtl_adj_deco = xci.dtl_adj_deco.ToUpper());
                        claimsDetailDto.XciXriXhiDetailsGrid = XCIDetailDto;
                    }

                    XciXriXhi xcixrixhi = XciXriXhiController.XciXriXhiInfo(controlValue, this.UserId.ToString(), null, null, null, claimsDetailDto, F9);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(xcixrixhi.XriXciXhiDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (xcixrixhi.XriXciXhiDto.PageIndicator == "1")
                        return PartialView("~/Views/Shared/XCI/XCISummary.cshtml", xcixrixhi);
                    else if (xcixrixhi.XriXciXhiDto.PageIndicator == "2")
                        return PartialView("~/Views/Shared/XCI/XCIDetails.cshtml", xcixrixhi);
                    else if (xcixrixhi.XriXciXhiDto.PageIndicator == "3")
                        return PartialView("~/Views/Shared/XCI/XCISummary.cshtml", xcixrixhi);
                    else if (xcixrixhi.XriXciXhiDto.PageIndicator == "H")
                        return LoadPartialViewMHI(xcixrixhi.XriXciXhiDto.ControlLine);
                    else if (!string.IsNullOrEmpty(xcixrixhi.XriXciXhiDto.PageIndicator) && xcixrixhi.XriXciXhiDto.PageIndicator == "Z")
                    {
                        controlValue = xcixrixhi.XriXciXhiDto.ControlLine;
                        return LoadPartialViewMPI(controlValue, "", null, null);
                    }
                    else
                    {
                        LoggingHelper.Log(LogType.ERROR, "XCI-" + this.UserId.ToString() + " LoadPartialViewUpdateXCI", "IncorrectValue");
                        return PartialView("", xcixrixhi);
                    }
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpPost]
        public PartialViewResult LoadPartialViewUpdate_PRI(string controlLine, string strpriDTO)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            Pri pri = new Pri();
            Common.PRIDto priDTO = new Common.PRIDto();
            if (!string.IsNullOrEmpty(strpriDTO))
            {
                priDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.PRIDto>(HttpUtility.UrlDecode(strpriDTO), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }
            try
            {
                SessionManipulation(this, clientvalue, true);
                using (PRIController PRI = new PRIController())
                {
                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();
                    pri.PRIDto = priDTO;

                    pri = PRI.PRIInfo(controlLine, this.UserId.ToString(), priDTO);
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(pri.PRIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }
                    return PartialView("~/Views/Shared/PRI/PRI.cshtml", pri);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpGet]
        public ActionResult LoadPartialViewMHI(string controlValue, bool Flag = false, string isMSSRequired = "False",
                                             bool IsRedirectedFromDMI = false, string baag_GrpCode = "", string baag_clasCd = "")
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                LoggingHelper.Log(ElasticType.ControlLine, controlValue);
                if (!Flag || isbybyFlag)
                {
                    SessionManipulation(this, clientvalue, true);
                    isbybyFlag = false;
                }

                //BAAG Logic
                if (!string.IsNullOrEmpty(baag_GrpCode))
                {   
                    // Additonal validation to ensure against Trust Boundary Violations
                    Match grpCodeMatch = Regex.Match(baag_GrpCode, Constants.baagGrpCdPattern);
                    Match clsCodeMatch = Regex.Match(baag_clasCd, Constants.baagClsCdPattern);
                    if (grpCodeMatch.Success && clsCodeMatch.Success
                        && (Session["classCD"] == null || baag_GrpCode != Convert.ToString(Session["grp"])))
                    {
                            Session["classCD"] = Server.HtmlEncode(baag_clasCd);
                            Session["grp"] = Server.HtmlEncode(baag_GrpCode);
                            Session["cntrl"] = Server.HtmlEncode(controlValue);
                    }
                }
                if (Session["cntrl"] != null)
                {
                    var cntrlValue = controlValue.Split(',');
                    var oldCntrlVal = Session["cntrl"].ToString().Split(',');
                    if (cntrlValue[1] != oldCntrlVal[1])
                    {
                        Session["classCD"] = "";
                        Session["grp"] = "";
                    }
                }

                if (Convert.ToBoolean(isMSSRequired) && controlValue.Split(',')[0]=="MHI")//// added for SIT defect#402014
                {
                    using (MSSController MSS = new MSSController())
                    {
                        this.UniqueId = NewGuid().ToString();

                        Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                        if (mss.MSSDto.NextOperation == "Yes")
                        {
                            //controlValue = "MHI";
                            return LoadPartialViewMHI(controlValue, true);
                        }
                        else
                        {
                            return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                        }
                    }
                }

                if (!(this.PreviousResponse.ToString().Contains("MHI") || this.PreviousResponse.ToString().Contains("MSS") || IsRedirectedFromDMI))
                {
                    this.PreviousResponse = null;

                    using (MSSController MSS = new MSSController())
                    {
                        this.UniqueId = NewGuid().ToString();
                        Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                        if (mss.MSSDto.NextOperation == "Yes" || (Utilities.IsMSSSkip(controlValue, this.ISMSSHit, this.CacheControlLine)))
                        {
                            //controlValue = "MHI";
                            return LoadPartialViewMHI(controlValue, true);
                        }
                        else
                        {
                            return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                        }
                    }
                }
                else
                {

                    using (MHIController MHI = new MHIController())
                    {
                        #region Added DMI Navigation logic

                        if (IsRedirectedFromDMI)
                            this.PreviousResponse = this.MSSResponse;

                        #endregion
                        ModelState.Clear();

                        Mhi mhi = MHI.MHIInfo(controlValue, this.UserId.ToString(), Flag);

                        #region Added MRI Navigation logic 

                        if (!string.IsNullOrEmpty(mhi.ModifierDto.ind) && mhi.ModifierDto.ind.Equals("MRI"))
                        {
                            using (MRIController MRI = new MRIController())
                            {
                                Mri mri = MRI.MRIInfo(mhi.ModifierDto.ControlLine, this.UserId.ToString(), null);
                                return PartialView("~/Views/Shared/MRI/MRI.cshtml", mri);
                            }
                        }

                        #endregion
                        if (!string.IsNullOrEmpty(mhi.ModifierDto.ResMsgs))
                        {
                            Default defaultModel = new Default();
                            defaultModel.ControlLine = controlValue;
                            defaultModel.UserId = this.UserId.ToString();
                            defaultModel.ResMsgs = mhi.ModifierDto.ResMsgs;
                            return PartialView("~/Views/Shared/Default/Default.cshtml", defaultModel);
                        }
                        else if (mhi.ModifierDto.ResInnerMsgs.Equals("HISTORY MODIFIER INVALID"))
                        {
                            Default defaultModel = new Default();
                            defaultModel.ControlLine = controlValue;
                            defaultModel.UserId = this.UserId.ToString();
                            defaultModel.ResMsgs = "HISTORY MODIFIER INVALID";
                            return PartialView("~/Views/Shared/Default/Default.cshtml", defaultModel);
                        }
                        else
                        {
                            mhi.ListModifier = ModifierList();
                            return PartialView("~/Views/Shared/MHI/MHI.cshtml", mhi);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }


        }


        public ActionResult LoadPartialViewMRIPTD(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    this.UserId = userId.ToUpper();
                }
                else
                {
                    this.UserId = ExtractUserId(User.Identity.Name).ToUpper();
                }

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MRIController MRI = new MRIController())
                {
                    Mri mri = MRI.MRIInfo(controlValue, this.UserId.ToString(), null);
                    if (!string.IsNullOrEmpty(mri.MRIDto.ind) && mri.MRIDto.ind.Equals("MRI"))
                    {
                        controlValue = mri.MRIDto.ControlLine.Replace("PTD", "").Trim();
                        controlValue = controlValue.Replace("MRU", "MRI").Trim();
                        return LoadPartialView(controlValue);

                    }
                    var controlline = "";
                    foreach (var item in mri.MRIDto.MRIDetails)
                    {
                        if (item.Group == null)
                        {
                            controlline = mri.MRIDto.ControlLine;
                            break;
                            ////return LoadPartialView(controlline);
                        }
                    }
                    if (controlline != "")
                        return LoadPartialView(controlline);

                    ////foreach (var item in mri.MRIDto.MRIDetails)
                    ////{
                    ////    if (item.Group == null)
                    ////    {
                    ////        var controlline = mri.MRIDto.ControlLine;
                    ////        return LoadPartialView(controlline);
                    ////    }
                    ////}
                    return PartialView("~/Views/Shared/PTD/MRIPTD.cshtml", mri);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }


        }

        public PartialViewResult LoadPartialViewMRI(string controlValue, bool isdirect = true)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MRIController MRI = new MRIController())
                {
                    Mri mri = MRI.MRIInfo(controlValue, this.UserId.ToString(), null);
                    return PartialView("~/Views/Shared/MRI/MRI.cshtml", mri);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        public PartialViewResult LoadPartialViewMDI(string controlValue, string clmInfoClmSmmrySel,
            string ClaimSummary, bool IsSummaryScreen = true, bool Isredirect = true)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (Isredirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MDIController MDI = new MDIController())
                {
                    Mdi model = new Mdi(new MDIDto());
                    if (!string.IsNullOrWhiteSpace(ClaimSummary))
                    {
                        model.MdiDto.MdiClmSummaryDtls = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.MdiSummaryDtl>>(HttpUtility.UrlDecode(ClaimSummary), new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        });
                    }

                    if (!string.IsNullOrEmpty(clmInfoClmSmmrySel))
                    {
                        model.MdiDto.MdiClmInfo.SelClaimSummary = clmInfoClmSmmrySel;
                    }

                    model = MDI.MdiInfo(controlValue, this.UserId.ToString(), model, IsSummaryScreen);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(model.MdiDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (model.MdiDto.IsSummaryPage)
                    {
                        return PartialView("~/Views/Shared/MDI/MDIFirst.cshtml", model);
                    }
                    else
                    {
                        return PartialView("~/Views/Shared/MDI/MDISecond.cshtml", model);
                    }
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }


        }

        [HttpPost]
        public PartialViewResult LoadPartialViewUpdate_CFC(string controlLine, string strcfiDTO, string strmodel)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlLine, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlLine, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (CFIController CFI = new CFIController())
                {
                    this.UniqueId = NewGuid().ToString();
                    Common.CFIDto model = new Common.CFIDto();
                    if (!String.IsNullOrEmpty(strmodel) && !String.IsNullOrEmpty(strcfiDTO))
                    {
                        strcfiDTO = HttpUtility.UrlDecode(strcfiDTO);
                        model = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.CFIDto>(HttpUtility.UrlDecode(strmodel), new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        });
                    }

                    Cfi cfi = CFI.CFIInfo(controlLine, this.UserId.ToString(), strcfiDTO, model);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(cfi.CFIDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (!string.IsNullOrEmpty(cfi.CFIDto.CfiFlag) && cfi.CFIDto.CfiFlag == "P" || (cfi.CFIDto.ResMsgs != null && cfi.CFIDto.CfiFlag == "P"))
                        return PartialView("~/Views/Shared/CFI/CFIP.cshtml", cfi);
                    else if (!string.IsNullOrEmpty(cfi.CFIDto.CfiFlag) && cfi.CFIDto.CfiFlag == "H" || (cfi.CFIDto.ResMsgs != null && cfi.CFIDto.CfiFlag == "H"))
                        return PartialView("~/Views/Shared/CFI/CFIH.cshtml", cfi);

                    return PartialView("~/Views/Shared/CFI/CFIH.cshtml", cfi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        public PartialViewResult LoadPartialViewMAI(string controlValue, string firstMai, bool isdirect = true)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            // Additonal validation to ensure against Trust Boundary Violations
            Match pageMatch = Regex.Match(firstMai, Constants.pagePattern);
            if (!pageMatch.Success)
            {
                return loadPartialViewDefault("", "redirect");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MAIController MAI = new MAIController())
                {
                    Mai mai = MAI.MAIInfo(controlValue, this.UserId.ToString(), Server.HtmlEncode(firstMai));
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(mai.MAIDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(mai.MAIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }

                    if (firstMai == "10" || firstMai == "")
                        return PartialView("~/Views/Shared/MAI/MAI.cshtml", mai);
                    else if (firstMai != "10")
                        return PartialView("~/Views/Shared/MAI/MAIDetail.cshtml", mai);

                    return PartialView("~/Views/Shared/MAI/MAI.cshtml", mai);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        public PartialViewResult LoadPartialViewMSI(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MSIController MSI = new MSIController())
                {
                    Msi msi = MSI.MSIInfo(controlValue, this.UserId.ToString());
                    return PartialView("~/Views/Shared/MSI/MSI.cshtml", msi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }


        }
        public PartialViewResult LoadPartialViewMGI(string controlValue, string firstMgi, bool isdirect = true)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            // Additonal validation to ensure against Trust Boundary Violations
            Match pageMatch = Regex.Match(firstMgi, Constants.pagePattern);
            if (!pageMatch.Success)
            {
                return loadPartialViewDefault("", "redirect");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MGIController MGI = new MGIController())
                {
                    Mgi mgi = MGI.MGIInfo(controlValue, this.UserId.ToString(), firstMgi);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(mgi.MGIDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    string indictr = string.Empty;
                    var cntlline = checkForMRI(mgi.MGIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }
                    if (firstMgi == "10")
                        return PartialView("~/Views/Shared/MGI/MGISummary.cshtml", mgi);
                    else if (firstMgi != "10")
                        return PartialView("~/Views/Shared/MGI/MGIDetail.cshtml", mgi);

                    return PartialView("~/Views/Shared/MGI/MGISummary.cshtml", mgi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }


        }

        public PartialViewResult LoadPartialViewMTI(string controlValue, string firstMti, bool isdirect = true)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            // Additonal validation to ensure against Trust Boundary Violations
            Match pageMatch = Regex.Match(firstMti, Constants.pagePattern);
            if (!pageMatch.Success)
            {
                return loadPartialViewDefault("", "redirect");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MTIController MTI = new MTIController())
                {
                    Mti mti = MTI.MTIInfo(controlValue, this.UserId.ToString(), firstMti);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(mti.MTIDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    string indictr = string.Empty;
                    var cntlline = checkForMRI(mti.MTIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }


                    if (firstMti == "10")
                        return PartialView("~/Views/Shared/MTI/MTI.cshtml", mti);
                    else if (firstMti != "10")
                        return PartialView("~/Views/Shared/MTI/MTIDetail.cshtml", mti);

                    return PartialView("~/Views/Shared/MTI/MTI.cshtml", mti);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        #endregion


        [HttpPost]
        public PartialViewResult LoadPartialViewUpdatePXI(string controlLine, string strPXIDto, string strPXIDetailDto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlLine, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlLine, "Control line contains invalid characters");
            }

            Common.PXIDto PXIDto = new Common.PXIDto();
            List<Common.PXIDetailDto> pxiDetailDto = new List<Common.PXIDetailDto>();
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            if (!(string.IsNullOrEmpty(strPXIDto) && string.IsNullOrEmpty(strPXIDetailDto)))
            {
                PXIDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.PXIDto>(HttpUtility.UrlDecode(strPXIDto), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
                pxiDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.PXIDetailDto>>(HttpUtility.UrlDecode(strPXIDetailDto), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });

            }
            try
            {
                SessionManipulation(this, clientvalue, true);
                PXIDto.PxiDetails = pxiDetailDto;
                PXIDto.ControlLine = controlLine;
                using (PXIController PXI = new PXIController())
                {
                    this.UniqueId = NewGuid().ToString();
                    Pxi pxi = PXI.PXIInfo(controlLine, this.UserId.ToString(), PXIDto);
                    return PartialView("~/Views/Shared/PXI/PXI.cshtml", pxi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public PartialViewResult LoadPartialViewUpdateSNI(string controlLine, string strSNIDto, string strSNIDetailDto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlLine, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlLine, "Control line contains invalid characters");
            }

            List<Common.SNIDetailDto> sniDetailDto = new List<Common.SNIDetailDto>();
            Common.SNIDto SNIDto = new Common.SNIDto();

            if (!(string.IsNullOrEmpty(strSNIDto) && string.IsNullOrEmpty(strSNIDetailDto)))
            {
                sniDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.SNIDetailDto>>(HttpUtility.UrlDecode(strSNIDetailDto), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });

                SNIDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.SNIDto>(HttpUtility.UrlDecode(strSNIDto), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);
                SNIDto.SNIDetails = sniDetailDto;
                SNIDto.ControlLine = controlLine;
                using (SNIController SNI = new SNIController())
                {
                    this.UniqueId = NewGuid().ToString();
                    Sni sni = SNI.SNIInfo(controlLine, this.UserId.ToString(), SNIDto);
                    return PartialView("~/Views/Shared/SNI/SNI.cshtml", sni);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }
        }

        [HttpPost]
        public PartialViewResult LoadPartialViewUpdateMRI(string controlLine, string strMRIDto, string strMRIDetailDto)
        {
            Common.MRIDto MRIDto = new Common.MRIDto();

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (!string.IsNullOrEmpty(strMRIDto) && !string.IsNullOrEmpty(strMRIDto))
                {
                    MRIDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.MRIDto>(HttpUtility.UrlDecode(strMRIDto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    List<Common.MRIDetailDto> mriDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.MRIDetailDto>>(HttpUtility.UrlDecode(strMRIDetailDto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    SessionManipulation(this, clientvalue, true);
                    MRIDto.MRIDetails = mriDetailDto;
                    MRIDto.ControlLine = controlLine;
                }
                using (MRIController MRI = new MRIController())
                {
                    this.UniqueId = NewGuid().ToString();
                    Mri mri = MRI.MRIInfo(controlLine, this.UserId.ToString(), MRIDto);
                    return PartialView("~/Views/Shared/MRI/MRI.cshtml", mri);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        #region <<PrivateMethod>>
        private string NewGuid()
        {
            string uniqueid = String.Empty;
            uniqueid = Utilities.getGUIDValue();
            return uniqueid;
        }

        private string MPCNewGuid(string PrevId)
        {
            string uniqueid = String.Empty;
            uniqueid = Utilities.getGUIDValue();
            if (PrevId == Common.Constants.defaultObjectType)
            {
                return uniqueid;
            }
            else
            {
                uniqueid = string.Concat(uniqueid.Substring(0, Common.Constants.GUIDFirstLen), PrevId.Substring(Common.Constants.GUIDFirstLen, Common.Constants.GUIDSecondLen));
            }

            return uniqueid;

            #region---Not is use---
            //string Id = string.Empty;
            //Random rn = new Random();
            //string charsToUse = "ABCDEFGHIJKLMNOPRSTUVWXYZ";

            //MatchEvaluator RandomChar = delegate (Match m)
            //{
            //    return charsToUse[rn.Next(charsToUse.Length)].ToString();
            //};

            //Id = (Regex.Replace("XXXXXXXXXX", "X", RandomChar));

            //Id = string.Concat(Id.Substring(0, 6), PrevId.Substring(6, 4));

            //return Id;
            #endregion
        }
        public List<SelectListItem> ModifierList()
        {
            List<SelectListItem> lstddlModifier = new List<SelectListItem>();
            string input = ConfigurationManager.AppSettings["Modifier"];
            string[] result = input.Split(new[] { "," }, StringSplitOptions.None);
            lstddlModifier.Add(new SelectListItem { Value = "--Select--", Text = "" });
            foreach (string mod in result)
            {
                lstddlModifier.Add(new SelectListItem { Text = mod, Value = mod });
            }
            return lstddlModifier;
        }
        #endregion
        public PartialViewResult LoadPartialViewMSS(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MSSController MSS = new MSSController())
                {
                    this.UniqueId = NewGuid().ToString();
                    Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                    this.ISMSSHit = true;
                    if (mss.MSSDto.NextOperation == "Yes")
                    {
                        return LoadPartialViewMRI(controlValue, false);
                    }
                    else
                    {
                        return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                    }
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpPost]
        public PartialViewResult LoadPartialViewMI(string controlValue, string RowId)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (ASIController ASI = new ASIController())
                {
                    this.UniqueId = NewGuid().ToString();
                    Asi asi = ASI.ASIInfo(controlValue, this.UserId.ToString(), true, RowId);
                    return PartialView("~/Views/Shared/ASI/ASI-MI.cshtml", asi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }
        }

        [HttpGet]

        public ActionResult LoadPartialViewMHIAPI(string controlValue, string lineval="")
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match linevalMatch1 = Regex.Match(lineval, Constants.lineValPattern1);
            Match linevalMatch2 = Regex.Match(lineval, Constants.lineValPattern2);
            // If value contains HTML then return error message
            if (linevalMatch1.Success || linevalMatch2.Success)
            {
                return loadPartialViewDefault(lineval, "Invalid selection");
            }

            string[] controlList = controlValue.ToUpper().Split(',');
            string ModifierVal = string.Empty;
            ModifierVal = Utilities.GetModifierFromControlLine(controlValue).Trim();

            string strCon = Utilities.GetModifierFromControlLine(controlValue).Trim();
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    this.UserId = userId.ToUpper();
                }
                else
                {
                    this.UserId = ExtractUserId(User.Identity.Name).ToUpper();
                }

                if (controlList[0] != "OVER")
                    SessionManipulation(this, clientvalue, true);

                if (ModifierVal == "CISH")
                {
                    using (APIController API = new APIController())
                    {

                        Api api = API.APIInfo(controlValue, this.UserId.ToString(), lineval);
                        return PartialView("~/Views/Shared/MHI/MHICISHAPI.cshtml", api);
                    }
                }
                else if (ModifierVal == "CISC")
                {
                    using (MHI_CISCController CISC = new MHI_CISCController())
                    {
                        Cisc Cisc = CISC.CISCInfo(controlValue, this.UserId.ToString(), lineval);
                        return PartialView("~/Views/Shared/MHI/MHI_CISC.cshtml", Cisc);
                    }
                }
                else if (ModifierVal == "CDE1" || ModifierVal == "CDE2")
                {
                    using (CDEController I = new CDEController())
                    {
                        Cde cde = I.CDEInfo(controlValue, this.UserId.ToString(), lineval);
                        return PartialView("~/Views/Shared/MHI/MHICDE.cshtml", cde);
                    }
                }
                else if (ModifierVal == "LCDF")
                {
                    using (LCDFController LCDF = new LCDFController())
                    {
                        LCDFMhi lcdfMHI = LCDF.LCDFInfo(controlValue, this.UserId.ToString(), lineval);
                        return PartialView("~/Views/Shared/MHI/MHILCDF.cshtml", lcdfMHI);
                    }
                }
                else if (ModifierVal == "BYP")
                {
                    using (MHI_BYPController BYP = new MHI_BYPController())
                    {
                        BYPMhi BYPMhi = BYP.BYPMHIInfo(controlValue, this.UserId.ToString(), lineval);
                        return PartialView("~/Views/Shared/MHI/MHIBYP.cshtml", BYPMhi);
                    }
                }
                else if (controlList[0] == "CMI" || controlList[0] == "CMN" || controlList[0] == "CMP")
                {
                    if (controlList[0] == "CMI")
                    {
                        using (MSSController MSS = new MSSController())
                        {
                            this.UniqueId = NewGuid().ToString();
                            Mss mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                        }
                    }
                    using (CMIController CMI = new CMIController())
                    {
                        this.UniqueId = NewGuid().ToString();
                        Cmi cmi = CMI.CmiInfo(controlValue, this.UserId.ToString());

                        string indictr = string.Empty;
                        var cntlline = checkForMRI(cmi.CMIDto.Response, out indictr);
                        return PartialView("~/Views/Shared/MHI/MHIPP3.cshtml", cmi);
                    }
                }
                else if (ModifierVal == "I")
                {
                    using (IController I = new IController())
                    {
                        IMHI Imhi = I.IMHIInfo(controlValue, this.UserId.ToString(), lineval);
                        return PartialView("~/Views/Shared/MHI/MHII.cshtml", Imhi);
                    }
                }
                else if (controlList[0] == "OVER")
                {
                    Over overModel = new Over();
                    bool yes = true;
                    //commented below line for CR21 and replaced with new function
                    //res = ChekClientWiseSession(this, typeof(CASUIBaseController), Common.Constants.varActualClientNumber); 
                    yes = AnyOtherOverClientExists();
                    if (yes)
                    {
                        overModel.overMsg = Common.Constants.overmsg;
                    }
                    else
                        overModel.overMsg = "";

                    overModel.UserId = CheckAndReturnUserID(clientvalue);

                    return PartialView("~/Views/Shared/Over/Over.cshtml", overModel);
                }

                LoggingHelper.Log(LogType.ERROR, "API-" + this.UserId.ToString() + " LoadPartialViewMHIAPI", "IncorrectValue");
                return PartialView("");
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                if (controlList[0] != "OVER")
                    SessionManipulation(this, clientvalue, false);
            }

        }

        #region---Payment MPC, ---
        public ActionResult LoadPartialViewMPI(string controlValue, string hotkey, string strMPCDto, string strMPCDetailDto, bool isdirect = true)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            var MPCmodeltype1 = "";
            var MPCmodemp1typ12o = "";
            var MPCmodelclaimno = "";

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    this.UserId = userId.ToUpper();
                }
                else
                {
                    this.UserId = ExtractUserId(User.Identity.Name).ToUpper();
                }

                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                Common.MPCDto MPCDto = new Common.MPCDto();
                if (!String.IsNullOrEmpty(strMPCDto))
                {
                    MPCDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.MPCDto>(HttpUtility.UrlDecode(strMPCDto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                }

                if (!String.IsNullOrEmpty(strMPCDetailDto) && strMPCDetailDto != "[]")
                {
                    List<Common.MPCDetailDto> MPCDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.MPCDetailDto>>(HttpUtility.UrlDecode(strMPCDetailDto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    MPCDetailDto.Where(w => ((!string.IsNullOrEmpty(w.mp1fdto)) && (w.mp1fdto.Contains("/")))).ToList().ForEach(s => s.mp1fdto = s.mp1fdto.Replace("/", ""));
                    MPCDetailDto.Where(w => (!string.IsNullOrEmpty(w.mp1ldto)) && (w.mp1ldto.Contains("/"))).ToList().ForEach(s => s.mp1ldto = s.mp1ldto.Replace("/", ""));
                    MPCDto.MpcDetails = MPCDetailDto;
                    MPCmodeltype1 = MPCDetailDto.Any() ? MPCDetailDto.FirstOrDefault().mp1typeo : string.Empty;
                    MPCmodemp1typ12o = MPCDetailDto.Any() ? MPCDetailDto.FirstOrDefault().mp1typ12o : string.Empty;
                    MPCmodeltype1 = string.IsNullOrEmpty(MPCmodeltype1) ? (string.IsNullOrEmpty(MPCmodemp1typ12o) ? "" : MPCmodemp1typ12o) : MPCmodeltype1;
                }

                using (MPCController MPC = new MPCController())
                {
                    Mpc mpc = new Mpc();

                    //if (this.UniqueId.ToString() == Constants.defaultObjectType)
                    //    this.UniqueId = NewGuid().ToString();
                    //else
                    this.UniqueId = MPCNewGuid(this.UniqueId.ToString());

                    if (MPCDto != null)
                    {
                        MPCmodelclaimno = string.IsNullOrEmpty(MPCDto.mpclnoo) ? (string.IsNullOrEmpty(MPCDto.mpclno2o) ? "" : MPCDto.mpclno2o)
                                                                                : MPCDto.mpclnoo;
                    }
                    ModelState.Clear();

                    MPCCRICWIDto MPCmodel = MPC.MPCInfo(controlValue, this.UserId.ToString(), hotkey, MPCDto);
                    if ((hotkey == "PF7" || hotkey == "PF22") && !String.IsNullOrEmpty(strMPCDetailDto) && String.IsNullOrEmpty(MPCmodel.ResMsgs))
                    {
                        string[] controlMpcList = controlValue.Split(',');

                        string NewcontrolLine = "";
                        if (hotkey == "PF7")//EDI
                        {
                            MPCmodelclaimno = string.IsNullOrEmpty(MPCmodelclaimno) ? "404040404" : MPCmodelclaimno;
                            NewcontrolLine = "EDI,";
                            NewcontrolLine += ((string.IsNullOrEmpty(MPCmodel.claimno) ? MPCmodelclaimno : MPCmodel.claimno) + "," + controlMpcList[1] + "," + controlMpcList[2]);
                            return LoadPartialView(NewcontrolLine);
                        }

                        else if (hotkey == "PF22")//DMI
                        {
                            NewcontrolLine = "DMI";
                            for (var ii = 1; ii < controlMpcList.Length; ii++)
                            {
                                if (ii != 2 && controlMpcList[ii].Length >= 1)
                                    NewcontrolLine = NewcontrolLine + "," + controlMpcList[ii];
                            }
                            // NewcontrolLine = NewcontrolLine + "," + Regex.Replace(MPCmodeltype1, "[^0-9a-zA-Z]+", "");
                            return loadPartialViewDMIMPC(NewcontrolLine, hotkey);
                        }
                    }

                    if (MPCmodel != null && (MPCmodel.indicator == "1" || MPCmodel.indicator == "2" || MPCmodel.indicator == "5" || MPCmodel.indicator == "6"))
                    {
                        mpc.MPCDto = MPCmodel.MPCDto;
                        this.PreviousResponse = mpc.MPCDto.Response;
                        mpc.MPCDto.isMJI = "";
                        if (this.MJIControlLine.ToString() != Common.Constants.defaultObjectType
                            && this.MJIControlLine.ToString().Contains("MJI"))
                        {
                            mpc.MPCDto.isMJI = "YES";
                        }
                        if (MPCmodel.indicator == "1" || MPCmodel.indicator == "5")
                            return PartialView("~/Views/Shared/MPC/MPCFirst.cshtml", mpc);
                        else if (MPCmodel.indicator == "2" || MPCmodel.indicator == "6")
                            return PartialView("~/Views/Shared/MPC/MPCSecond.cshtml", mpc);
                        else
                        {
                            LoggingHelper.Log(LogType.ERROR, "MPC-" + this.UserId.ToString() + " LoadPartialViewMPI else", "IncorrectValue");
                            return PartialView("");
                        }

                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "W"))
                    {
                        Cwi Cwi = new Cwi();
                        this.PreviousResponse = MPCmodel.CWIDto.Response;
                        ModelState.Clear();
                        Cwi.CWIDto = MPCmodel.CWIDto;
                        return PartialView("~/Views/Shared/CWI/CWI.cshtml", Cwi);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "C"))
                    {
                        Cri Cri = new Cri();
                        this.PreviousResponse = MPCmodel.CRIDto.Response;

                        Cri.CRIDto = MPCmodel.CRIDto;
                        return PartialView("~/Views/Shared/CRI/CRI.cshtml", Cri);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "S"))
                    {
                        Sdi Sdi = new Sdi();
                        this.PreviousResponse = MPCmodel.SDIDto.Response;

                        Sdi.SDIDto = MPCmodel.SDIDto;
                        return PartialView("~/Views/Shared/SDI/SDI.cshtml", Sdi);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "B"))
                    {
                        Ebi Ebi = new Ebi();
                        this.PreviousResponse = MPCmodel.EBIDto.Response;

                        Ebi.EBIDto = MPCmodel.EBIDto;
                        return PartialView("~/Views/Shared/EBI/EBI.cshtml", Ebi);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "A"))
                    {
                        ApiMPC apiMpc = new ApiMPC();
                        this.PreviousResponse = MPCmodel.APIMPCDto.Response;
                        apiMpc.APIMPCDto = MPCmodel.APIMPCDto;
                        return PartialView("~/Views/Shared/API/API.cshtml", apiMpc);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "L"))
                    {
                        Lti Lti = new Lti();
                        this.PreviousResponse = MPCmodel.LTIDto.Response;
                        Lti.LTIDto = MPCmodel.LTIDto;
                        return PartialView("~/Views/Shared/LTI/LTI.cshtml", Lti);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "J"))
                    {
                        Jji Jji = new Jji();
                        this.PreviousResponse = MPCmodel.JJIDto.Response;

                        Jji.JJIDto = MPCmodel.JJIDto;
                        return PartialView("~/Views/Shared/JJI/JJI.cshtml", Jji);

                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "D"))
                    {
                        Rdi Rdi = new Rdi();
                        this.PreviousResponse = MPCmodel.RDIDto.Response;
                        ModelState.Clear();
                        Rdi.RDIDto = MPCmodel.RDIDto;
                        return PartialView("~/Views/Shared/RDI/RDI.cshtml", Rdi);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == Common.Constants.mriIndicator ||
                                                  MPCmodel.indicator == Common.Constants.mssIndicator))

                    {
                        return LoadPartialViewMZItoMRI(MPCmodel.ControlLine);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "I" || MPCmodel.indicator == "P"))
                    {
                        Isi isi = new Isi();
                        this.PreviousResponse = MPCmodel.ISIDto.Response;
                        isi.ISIDto = MPCmodel.ISIDto;
                        return PartialView("~/Views/Shared/ISI/MPCISI.cshtml", isi);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "H"))
                    {
                        Ahi ahi = new Ahi();
                        this.PreviousResponse = MPCmodel.AHIDto.Response;
                        ahi.AHIDto = MPCmodel.AHIDto;
                        return PartialView("~/Views/Shared/AHI/AHI.cshtml", ahi);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "E"))
                    {
                        Default EdefaultModel = new Default();
                        if (ErrorHandlerForE(MPCmodel.Response, out  EdefaultModel))
                        {
                            return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                        }
                        LoggingHelper.Log(LogType.ERROR, "MPC-" + this.UserId.ToString() + " LoadPartialViewMPI E indicator", "IncorrectValue");
                        return PartialView("");
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "G" || MPCmodel.indicator == "F"))
                    {
                        Cbi Cbi = new Cbi();
                        this.PreviousResponse = MPCmodel.CBIDto.Response;

                        Cbi.CBIDto = MPCmodel.CBIDto;
                        if(MPCmodel.indicator == "G")
                            return PartialView("~/Views/Shared/CBI/CBIPhysician.cshtml", Cbi);
                        else
                            return PartialView("~/Views/Shared/CBI/CBIFacility.cshtml", Cbi);
                    }

                    LoggingHelper.Log(LogType.ERROR, "MPC-" + this.UserId.ToString() + " LoadPartialViewMPI no indicator", "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {

                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        #endregion

        [HttpGet]
        public void ClearCache()
        {
            string userId = string.Empty;
            string clientno = string.Empty;
            ValidateUserClient(out userId, out clientno); //improvement opurtunity- what to do if userid is not authenticated?

            this.PreviousResponse = null;
            this.PreviousResponseModifier = null;
            this.ISMSSHit = null;
            this.CacheControlLine = null;
            //var clientno = getValueFromURL("clientNumber", true);
            //var userId = getValueFromURL("userId", true);
            string uidClientNo = userId + ":" + clientno;
            this.ClientNumberList.Remove(uidClientNo);
            //var clientno = getValueFromURL("clientNumber");
            //if (!string.IsNullOrWhiteSpace(clientno))
            //    SessionManipulation(this, clientno, true, true);
        }
        [HttpGet]
        public void ClearCacheMenu(string clientno)
        {

            string userId = string.Empty;
            string clientnumber = string.Empty;
            ValidateUserClient(out userId, out clientnumber); //improvement opurtunity- what to do if userid is not authenticated?


            this.PreviousResponse = null;
            this.PreviousResponseModifier = null;
            this.ISMSSHit = null;
            this.CacheControlLine = null;
            //CASClient client;
            //if (DataValidationHelper.IsValidCasClient(this, clientno, out client))
            //{
            //    SessionManipulation(this, clientno, true, true, true);
            //}
            //if (!string.IsNullOrWhiteSpace(clientno))
            //    SessionManipulation(this, clientno, true, true, true);

        }

        [HttpPost]
        public JsonResult CheckXCBSCache()
        {
            XriXciXhiServiceManager XriXciXhiService = new XriXciXhiServiceManager();
            return Json(XriXciXhiService.GetControlline(this.XCBSResponse), JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public void ClearCachePDI()
        {
            string userId = string.Empty;
            string clientno = string.Empty;
            ValidateUserClient(out userId, out clientno); //improvement opurtunity- what to do if userid is not authenticated?

            if (this.PreviousResponse.ToString().Contains("PDI") || this.PreviousResponse.ToString().Contains("PDN") || this.PreviousResponse.ToString().Contains("PDU"))
                this.PreviousResponse = null;
        }
        [HttpGet]
        public PartialViewResult LoadPartialViewCRI(string controlValue, bool isdirect = true)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (CRIController CRI = new CRIController())
                {

                    if (this.UniqueId.ToString() == Constants.defaultObjectType)
                    {
                        this.UniqueId = NewGuid().ToString();
                    }

                    Cri cri = CRI.CRIInfo(controlValue, this.UserId.ToString(), null);

                    string indictr = string.Empty;
                    var cntlline = checkForMRI(cri.CRIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }

                    return PartialView("~/Views/Shared/CRI/CRI.cshtml", cri);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public ActionResult LoadPartialViewXCI(string controlValue, string rdoselVal = "", bool F9 = false)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    this.UserId = userId.ToUpper();
                }
                else
                {
                    this.UserId = ExtractUserId(User.Identity.Name).ToUpper();
                }

                SessionManipulation(this, clientvalue, true);
                using (XciXriXhiController XciXriXhiController = new XciXriXhiController())
                {
                    this.UniqueId = NewGuid().ToString();
                    XciXriXhi xcixrixhi = XciXriXhiController.XciXriXhiInfo(controlValue, this.UserId.ToString(), rdoselVal, null, null, null, F9);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(xcixrixhi.XriXciXhiDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (xcixrixhi.XriXciXhiDto.PageIndicator == "1")
                        return PartialView("~/Views/Shared/XCI/XCISummary.cshtml", xcixrixhi);
                    else if (xcixrixhi.XriXciXhiDto.PageIndicator == "2")
                        return PartialView("~/Views/Shared/XCI/XCIDetails.cshtml", xcixrixhi);
                    else if (xcixrixhi.XriXciXhiDto.PageIndicator == "3")
                        return PartialView("~/Views/Shared/XCI/XCIDetails.cshtml", xcixrixhi);
                    else if (!string.IsNullOrEmpty(xcixrixhi.XriXciXhiDto.PageIndicator) && xcixrixhi.XriXciXhiDto.PageIndicator == "Z")
                    {
                        controlValue = xcixrixhi.XriXciXhiDto.ControlLine;
                        return LoadPartialViewMPI(controlValue, "", null, null);
                    }
                    else
                    {
                        LoggingHelper.Log(LogType.ERROR, "XCI-" + this.UserId.ToString() + " LoadPartialViewXCI", "IncorrectValue");
                        return PartialView("~/Views/Shared/XCI/XCISummary.cshtml", xcixrixhi);
                    }
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpGet]
        public PartialViewResult LoadPartialViewPRI(string controlValue, bool isdirect = true)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                using (PRIController PRI = new PRIController())
                {
                    if (this.UniqueId.ToString() == Constants.defaultObjectType)
                    {
                        this.UniqueId = NewGuid().ToString();
                    }

                    Pri pri = PRI.PRIInfo(controlValue, this.UserId.ToString(), null);
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(pri.PRIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }
                    return PartialView("~/Views/Shared/PRI/PRI.cshtml", pri);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpGet]
        public PartialViewResult LoadPartialViewRSI(string controlValue, string rdoselVal, bool isdirect = true)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                using (RSIController RSI = new RSIController())
                {
                    string[] indicatorArr = new string[] { "1", "E", "R" };
                    Rfi model = new Rfi();
                    Rsi rsi = new Rsi();
                    if (this.UniqueId.ToString() == Constants.defaultObjectType)
                    {
                        this.UniqueId = NewGuid().ToString();
                    }

                    RsiRfiDto Rsi = RSI.RSIInfo(controlValue, this.UserId.ToString(), rdoselVal);
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(indicatorArr.Contains(Rsi.indicator) ? Rsi.RFIDto.Response :
                                    (Rsi.indicator == "2" ? Rsi.RSIDto.Response : Rsi.RSIDto.Response), out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }
                    model.RFIDto = Rsi.RFIDto;
                    rsi.RSIDto = Rsi.RSIDto;
                    var resobj = Rsi.RSIDto == null ? Rsi.RFIDto.Response : Rsi.RSIDto.Response;
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(resobj, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }
                    if (Rsi.indicator == "1" || Rsi.indicator == "E")
                    {
                        if (Rsi.indicator == "E")
                        {
                            LoggingHelper.Log(LogType.ERROR, "RSI-" + this.UserId.ToString() + " LoadPartialViewRSI indicator - " + Rsi.indicator, "IncorrectValue");
                        }
                        return PartialView("~/Views/Shared/RFI/RFI.cshtml", model);
                    }
                    else if (Rsi.indicator == "2")
                        return PartialView("~/Views/Shared/RSI/RSI.cshtml", rsi);


                    LoggingHelper.Log(LogType.ERROR, "RSI-" + this.UserId.ToString() + " LoadPartialViewRSI indicator - " + Rsi.indicator, "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpGet]
        public PartialViewResult LoadPartialViewRFI(string controlValue, string rdoselVal, bool isdirect = true)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                using (RFIController RFI = new RFIController())
                {
                    string[] indicatorArr = new string[] { "1", "E", "R" };
                    Rfi model = new Rfi();
                    Rsi rsi = new Rsi();
                    if (this.UniqueId.ToString() == Constants.defaultObjectType)
                    {
                        this.UniqueId = NewGuid().ToString();
                    }

                    RsiRfiDto rfi = RFI.RFIInfo(controlValue, this.UserId.ToString(), null, rdoselVal);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(rfi.RFIDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    string indictr = string.Empty;
                    var cntlline = checkForMRI(indicatorArr.Contains(rfi.indicator) ?
                        rfi.RFIDto.Response : rfi.RSIDto.Response, out indictr);

                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }

                    model.RFIDto = rfi.RFIDto;
                    rsi.RSIDto = rfi.RSIDto;

                    if (rfi.indicator == "1")
                        return PartialView("~/Views/Shared/RFI/RFI.cshtml", model);
                    else if (rfi.indicator == "2")
                        return PartialView("~/Views/Shared/RSI/RSI.cshtml", rsi);

                    LoggingHelper.Log(LogType.ERROR, "RFI-" + this.UserId.ToString() + " LoadPartialViewRFI indicator - " + rfi.indicator, "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpGet]
        public PartialViewResult LoadPartialViewUF6I(string controlValue, string rdoselVal, bool isdirect = true)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                using (UF6IController UF6I = new UF6IController())
                {
                    this.UniqueId = NewGuid().ToString();
                    UF6I uf6i = UF6I.UF6IInfo(controlValue, this.UserId.ToString());
                    return PartialView("~/Views/Shared/UF6I/UF6I.cshtml", uf6i);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }
        }

        public PartialViewResult LoadPartialViewEBI(string controlValue, string strEBIDetaildto, string hotkey)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            EBIDto ebiDto = new EBIDto();
            MPCCRICWIDto MPCAllDto = new MPCCRICWIDto();
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                if (!String.IsNullOrEmpty(strEBIDetaildto))
                {
                    List<Common.EBIDetailDto> EBIDetaildto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.EBIDetailDto>>(HttpUtility.UrlDecode(strEBIDetaildto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    ebiDto.ebiDetails = EBIDetaildto;
                    ebiDto.ebiDetails.ForEach(ebi => { ebi.edsel1o = ebi.edsel1o.ToUpper(); }); // convert value to upper-case
                    ebiDto.ebiDetails.Where(w => w.edsel1o == "-").ToList().ForEach(ebi => ebi.edsel1o = ""); // remove default place holder                    
                    ebiDto.ebiDetails.ForEach(ebi => { ebi.edsel2o = ebi.edsel2o.ToUpper(); }); // convert value to upper-case
                    ebiDto.ebiDetails.Where(w => w.edsel2o == "-").ToList().ForEach(ebi => ebi.edsel2o = ""); // remove default place holder   
                    MPCAllDto.EBIDto = ebiDto;
                }

                using (MPCController MPC = new MPCController())
                {
                    Mpc mpc = new Mpc();
                    this.UniqueId = MPCNewGuid(this.UniqueId.ToString());

                    MPCCRICWIDto MPCmodel = MPC.MPCInfo(controlValue, this.UserId.ToString(), hotkey, MPCAllDto);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(MPCmodel.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (MPCmodel != null && (MPCmodel.indicator == "1" || MPCmodel.indicator == "2" || MPCmodel.indicator == "5" || MPCmodel.indicator == "6"))
                    {
                        mpc.MPCDto = MPCmodel.MPCDto;
                        this.PreviousResponse = mpc.MPCDto.Response;
                        if (MPCmodel.indicator == "1" || MPCmodel.indicator == "5")
                            return PartialView("~/Views/Shared/MPC/MPCFirst.cshtml", mpc);
                        else if (MPCmodel.indicator == "2" || MPCmodel.indicator == "6")
                            return PartialView("~/Views/Shared/MPC/MPCSecond.cshtml", mpc);
                        else
                        {
                            LoggingHelper.Log(LogType.ERROR, "EBI-" + this.UserId.ToString() + " LoadPartialViewEBI", "IncorrectValue");
                            return PartialView("");
                        }
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "B"))
                    {
                        Ebi ebi = new Ebi();
                        ebi.EBIDto = MPCmodel.EBIDto;
                        return PartialView("~/Views/Shared/EBI/EBI.cshtml", ebi);
                    }

                    LoggingHelper.Log(LogType.ERROR, "EBI" + this.UserId.ToString() + " LoadPartialViewEBI no indicator", "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }
        }

        [HttpGet]
        public PartialViewResult loadPartialViewDMIMPC(string controlLine, string hotkey)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlLine);

                using (DMIController DMI = new DMIController())
                {
                    this.UniqueId = NewGuid().ToString();
                    Dmi dmi = DMI.DMIInfo(controlLine, this.UserId.ToString(), hotkey);
                    return PartialView("~/Views/Shared/DMI/DMISummary.cshtml", dmi);

                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        public PartialViewResult LoadPartialViewJJI(string controlValue, string hotkey, string strJJIDto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MPCController MPC = new MPCController())
                {
                    this.UniqueId = MPCNewGuid(this.UniqueId.ToString());

                    ModelState.Clear();
                    MPCCRICWIDto MPCmodel = new MPCCRICWIDto();
                    Common.JJIDto JJIDto = new Common.JJIDto();
                    if (!string.IsNullOrEmpty(strJJIDto))
                    {
                        MPCmodel.JJIDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.JJIDto>(HttpUtility.UrlDecode(strJJIDto), new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        });
                    }

                    MPCmodel = MPC.MPCInfo(controlValue, this.UserId.ToString(), hotkey, MPCmodel);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(MPCmodel.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (MPCmodel != null && (MPCmodel.indicator == "1" || MPCmodel.indicator == "2" || MPCmodel.indicator == "5" || MPCmodel.indicator == "6"))
                    {
                        Mpc mpc = new Mpc();
                        mpc.MPCDto = MPCmodel.MPCDto;
                        this.PreviousResponse = mpc.MPCDto.Response;
                        if (MPCmodel.indicator == "1" || MPCmodel.indicator == "5")
                            return PartialView("~/Views/Shared/MPC/MPCFirst.cshtml", mpc);
                        else if (MPCmodel.indicator == "2" || MPCmodel.indicator == "6")
                            return PartialView("~/Views/Shared/MPC/MPCSecond.cshtml", mpc);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "J"))
                    {
                        Jji jji = new Jji();
                        jji.JJIDto = MPCmodel.JJIDto;
                        return PartialView("~/Views/Shared/JJI/JJI.cshtml", jji);
                    }

                    LoggingHelper.Log(LogType.ERROR, "JJI-" + this.UserId.ToString() + " LoadPartialViewJJI", "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }
        }

        public PartialViewResult LoadPartialViewLTI(string controlValue, string hotkey, string strLTIDto, string strLTIDetaildto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            MPCCRICWIDto MPCAllDto = new MPCCRICWIDto();
            Common.LTIDto LTIDto = new Common.LTIDto();
            if (!string.IsNullOrEmpty(strLTIDto))
            {
                MPCAllDto.LTIDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.LTIDto>(HttpUtility.UrlDecode(strLTIDto), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                if (!String.IsNullOrEmpty(strLTIDetaildto))
                {
                    List<Common.LTIDetailDto> LtiDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.LTIDetailDto>>(HttpUtility.UrlDecode(strLTIDetaildto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    LtiDetailDto.Where(w => ((!string.IsNullOrEmpty(w.ltfrsto)) && (w.ltfrsto.Contains("/")))).ToList().ForEach(s => s.ltfrsto = s.ltfrsto.Replace("/", ""));
                    LtiDetailDto.Where(w => (!string.IsNullOrEmpty(w.ltlsto)) && (w.ltlsto.Contains("/"))).ToList().ForEach(s => s.ltlsto = s.ltlsto.Replace("/", ""));
                    MPCAllDto.LTIDto.LtiDetails = LtiDetailDto;
                }
                using (MPCController MPC = new MPCController())
                {
                    Mpc mpc = new Mpc();
                    this.UniqueId = MPCNewGuid(this.UniqueId.ToString());

                    MPCCRICWIDto MPCmodel = new MPCCRICWIDto();

                    MPCmodel = MPC.MPCInfo(controlValue, this.UserId.ToString(), hotkey, MPCAllDto);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(MPCmodel.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (MPCmodel != null && (MPCmodel.indicator == "1" || MPCmodel.indicator == "2" || MPCmodel.indicator == "5" || MPCmodel.indicator == "6"))
                    {
                        mpc.MPCDto = MPCmodel.MPCDto;
                        this.PreviousResponse = mpc.MPCDto.Response;
                        if (MPCmodel.indicator == "1" || MPCmodel.indicator == "5")
                            return PartialView("~/Views/Shared/MPC/MPCFirst.cshtml", mpc);
                        else if (MPCmodel.indicator == "2" || MPCmodel.indicator == "6")
                            return PartialView("~/Views/Shared/MPC/MPCSecond.cshtml", mpc);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "L"))
                    {
                        Lti lti = new Lti();
                        lti.LTIDto = MPCmodel.LTIDto;
                        return PartialView("~/Views/Shared/LTI/LTI.cshtml", lti);
                    }

                    LoggingHelper.Log(LogType.ERROR, "LTI-" + this.UserId.ToString() + " LoadPartialViewLTI", "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }



        }

        public PartialViewResult LoadPartialViewUpdateRDI(string controlValue, string hotKey, string strRDIDto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);
                MPCCRICWIDto MPCAllDto = new MPCCRICWIDto();
                Common.RDIDto RDIDto = new Common.RDIDto();
                if (!string.IsNullOrEmpty(strRDIDto))
                {
                    MPCAllDto.RDIDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.RDIDto>(HttpUtility.UrlDecode(strRDIDto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                }

                using (MPCController MPC = new MPCController())
                {
                    Mpc mpc = new Mpc();
                    this.UniqueId = MPCNewGuid(this.UniqueId.ToString());

                    MPCCRICWIDto MPCmodel = new MPCCRICWIDto();

                    MPCmodel = MPC.MPCInfo(controlValue, this.UserId.ToString(), hotKey, MPCAllDto);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(MPCmodel.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (MPCmodel != null && (MPCmodel.indicator == "1" || MPCmodel.indicator == "2" || MPCmodel.indicator == "5" || MPCmodel.indicator == "6"))
                    {
                        mpc.MPCDto = MPCmodel.MPCDto;
                        this.PreviousResponse = mpc.MPCDto.Response;
                        if (MPCmodel.indicator == "1" || MPCmodel.indicator == "5")
                            return PartialView("~/Views/Shared/MPC/MPCFirst.cshtml", mpc);
                        else if (MPCmodel.indicator == "2" || MPCmodel.indicator == "6")
                            return PartialView("~/Views/Shared/MPC/MPCSecond.cshtml", mpc);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "D"))
                    {
                        Rdi rdi = new Rdi();
                        rdi.RDIDto = MPCmodel.RDIDto;
                        ModelState.Clear();
                        return PartialView("~/Views/Shared/RDI/RDI.cshtml", rdi);
                    }

                    LoggingHelper.Log(LogType.ERROR, "RDI-" + this.UserId.ToString() + " LoadPartialViewUpdateRDI", "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }



        }

        public PartialViewResult LoadPartialViewSDI(string controlValue, string hotkey, string strSDIDetaildto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            SDIDto sdiDto = new SDIDto();
            MPCCRICWIDto MPCAllDto = new MPCCRICWIDto();
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);
                if (!String.IsNullOrEmpty(strSDIDetaildto))
                {
                    sdiDto.SDIDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.SDIDetailDto>>(HttpUtility.UrlDecode(strSDIDetaildto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    MPCAllDto.SDIDto = sdiDto;
                }
                using (MPCController MPC = new MPCController())
                {
                    Mpc mpc = new Mpc();
                    this.UniqueId = MPCNewGuid(this.UniqueId.ToString());

                    MPCCRICWIDto MPCmodel = new MPCCRICWIDto();

                    MPCmodel = MPC.MPCInfo(controlValue, this.UserId.ToString(), hotkey, MPCAllDto);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(MPCmodel.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (MPCmodel != null && (MPCmodel.indicator == "1" || MPCmodel.indicator == "2" || MPCmodel.indicator == "5" || MPCmodel.indicator == "6"))
                    {
                        mpc.MPCDto = MPCmodel.MPCDto;
                        this.PreviousResponse = mpc.MPCDto.Response;
                        if (MPCmodel.indicator == "1" || MPCmodel.indicator == "5")
                            return PartialView("~/Views/Shared/MPC/MPCFirst.cshtml", mpc);
                        else if (MPCmodel.indicator == "2" || MPCmodel.indicator == "6")
                            return PartialView("~/Views/Shared/MPC/MPCSecond.cshtml", mpc);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "S"))
                    {
                        Sdi sdi = new Sdi();
                        sdi.SDIDto = MPCmodel.SDIDto;
                        return PartialView("~/Views/Shared/SDI/SDI.cshtml", sdi);
                    }

                    LoggingHelper.Log(LogType.ERROR, "SDI-" + this.UserId.ToString() + " LoadPartialViewSDI", "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        public PartialViewResult LoadPartialViewAHI(string controlValue, string hotkey, string strAHIDto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                

                Common.AHIDto AHIDto = new Common.AHIDto();
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MPCController MPC = new MPCController())
                {
                    this.UniqueId = MPCNewGuid(this.UniqueId.ToString());

                    ModelState.Clear();
                    MPCCRICWIDto MPCmodel = new MPCCRICWIDto();
                    if (!string.IsNullOrEmpty(strAHIDto))
                    {
                        MPCmodel.AHIDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.AHIDto>(HttpUtility.UrlDecode(strAHIDto), new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        });
                    }

                    MPCmodel = MPC.MPCInfo(controlValue, this.UserId.ToString(), hotkey, MPCmodel);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(MPCmodel.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (MPCmodel != null && (MPCmodel.indicator == "1" || MPCmodel.indicator == "2"))
                    {
                        Mpc mpc = new Mpc();
                        mpc.MPCDto = MPCmodel.MPCDto;
                        this.PreviousResponse = mpc.MPCDto.Response;
                        if (MPCmodel.indicator == "1")
                            return PartialView("~/Views/Shared/MPC/MPCFirst.cshtml", mpc);
                        else if (MPCmodel.indicator == "2")
                            return PartialView("~/Views/Shared/MPC/MPCSecond.cshtml", mpc);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "H"))
                    {
                        Ahi ahi = new Ahi();
                        ahi.AHIDto = MPCmodel.AHIDto;
                        return PartialView("~/Views/Shared/AHI/AHI.cshtml", ahi);
                    }

                    LoggingHelper.Log(LogType.ERROR, "AHI-" + this.UserId.ToString() + " LoadPartialViewAHI", "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }


        }

        [HttpGet]
        public PartialViewResult LoadPartialViewCWI(string controlValue, bool isdirect = true)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (CWIController CWI = new CWIController())
                {
                    Cwi Cwi = new Cwi();
                    if (this.UniqueId.ToString() == Constants.defaultObjectType)
                    {
                        this.UniqueId = NewGuid().ToString();
                    }

                    Cwi = CWI.CWIInfo(controlValue, this.UserId.ToString(), null);
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(Cwi.CWIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }

                    return PartialView("~/Views/Shared/CWI/CWI.cshtml", Cwi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        //here
        [HttpGet]
        public PartialViewResult LoadPartialViewPCI(string controlValue, bool isdirect = true)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                using (PCIController PCI = new PCIController())
                {
                    Pci Pci = new Pci();
                    if (this.UniqueId.ToString() == Constants.defaultObjectType)
                    {
                        this.UniqueId = NewGuid().ToString();
                    }

                    Pci = PCI.PCIInfo(controlValue, this.UserId.ToString(), null);
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(Pci.PCIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }
                    return PartialView("~/Views/Shared/PCI/PCI.cshtml", Pci);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpGet]
        public PartialViewResult LoadPartialViewOCI(string controlValue, bool isdirect = true)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                using (OCIController OCI = new OCIController())
                {
                    Oci Oci = new Oci();
                    this.UniqueId = NewGuid().ToString();

                    Oci = OCI.OCIInfo(controlValue, this.UserId.ToString(), null);
                    return PartialView("~/Views/Shared/OCI/OCI.cshtml", Oci);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public PartialViewResult LoadPartialViewUpdate_OCI(string controlLine, string strociDTO, string strociDetailDto)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            Common.OCIDto ociDTO = new Common.OCIDto();
            try
            {
                SessionManipulation(this, clientvalue, true);

                if (!String.IsNullOrEmpty(strociDetailDto) && !String.IsNullOrEmpty(strociDTO))
                {
                    ociDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.OCIDto>(HttpUtility.UrlDecode(strociDTO), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    List<Common.OCIDetailDto> OCIDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.OCIDetailDto>>(HttpUtility.UrlDecode(strociDetailDto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    ociDTO.OciDetails = OCIDetailDto;
                }
                using (OCIController OCI = new OCIController())
                {
                    Oci oci = new Oci();
                    this.UniqueId = NewGuid().ToString();
                    oci.OCIDto = ociDTO;

                    oci = OCI.OCIInfo(controlLine, this.UserId.ToString(), ociDTO);
                    return PartialView("~/Views/Shared/OCI/OCI.cshtml", oci);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpGet]
        public PartialViewResult LoadPartialViewRGI(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (RGIController RGI = new RGIController())
                {
                    Rgi model = new Rgi();
                    Rgm modelRgm = new Rgm();
                    Rge modelRge = new Rge();
                    RgmRgiRgeDto Rgi = new RgmRgiRgeDto();
                    this.UniqueId = NewGuid().ToString();

                    Rgi = RGI.RGIInfo(controlValue, "", this.UserId.ToString(), null);
                    Default EdefaultModel = new Default();
                    object objresponse = Rgi.RGIDto != null ? Rgi.RGIDto.Response : (Rgi.RGMDto != null ? Rgi.RGMDto.Response : Rgi.RGEDto.Response);

                    if (ErrorHandlerForE(objresponse, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (Rgi.PageIndicator == "1" || Rgi.PageIndicator == "3")
                    {
                        model.RGIDto = Rgi.RGIDto;
                        return PartialView("~/Views/Shared/RGI/RGI.cshtml", model);
                    }
                    else if (Rgi.PageIndicator == "2")
                    {
                        modelRgm.RGMDto = Rgi.RGMDto;
                        return PartialView("~/Views/Shared/RGI/RGM.cshtml", modelRgm);
                    }

                    else if (Rgi.PageIndicator == "4" || Rgi.PageIndicator == "9")
                    {
                        modelRge.RGEDto = Rgi.RGEDto;
                        return PartialView("~/Views/Shared/RGI/RGE.cshtml", modelRge);
                    }
                    return PartialView("~/Views/Shared/RGI/RGI.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                ViewBag.exception = "Exception";
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpGet]
        public PartialViewResult LoadPartialViewRGM(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (RGMController RGM = new RGMController())
                {
                    Rgm model = new Rgm();
                    Rgi modelRgi = new Rgi();
                    Rge modelrge = new Rge();
                    RgmRgiRgeDto Rgi = new RgmRgiRgeDto();
                    this.UniqueId = NewGuid().ToString();

                    Rgi = RGM.RGMInfo(controlValue, this.UserId.ToString(), null);
                    Default EdefaultModel = new Default();
                    object objresponse = Rgi.RGIDto != null ? Rgi.RGIDto.Response : (Rgi.RGMDto != null ? Rgi.RGMDto.Response : Rgi.RGEDto.Response);
                    if (ErrorHandlerForE(objresponse, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (Rgi.PageIndicator == "1" || Rgi.PageIndicator == "3")
                    {
                        modelRgi.RGIDto = Rgi.RGIDto;
                        return PartialView("~/Views/Shared/RGI/RGI.cshtml", modelRgi);
                    }
                    else if (Rgi.PageIndicator == "2")
                    {
                        model.RGMDto = Rgi.RGMDto;
                        return PartialView("~/Views/Shared/RGI/RGM.cshtml", model);
                    }
                    else if (Rgi.PageIndicator == "4" || Rgi.PageIndicator == "9")
                    {
                        modelrge.RGEDto = Rgi.RGEDto;
                        return PartialView("~/Views/Shared/RGI/RGE.cshtml", modelrge);
                    }
                    return PartialView("~/Views/Shared/RGI/RGM.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpPost]
        public PartialViewResult LoadPartialViewUpdate_RGM(string controlLine, string strrgmDTO)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            Common.RGMDto rgmDTO = new RGMDto();
            if (!string.IsNullOrEmpty(strrgmDTO))
            {
                rgmDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.RGMDto>(HttpUtility.UrlDecode(strrgmDTO), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }
            try
            {
                SessionManipulation(this, clientvalue, true);
                using (RGMController RGM = new RGMController())
                {
                    Rgi model = new Rgi();
                    Rgm modelRgm = new Rgm();
                    Rge modelRge = new Rge();
                    RgmRgiRgeDto Rgi = new RgmRgiRgeDto();
                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();
                    Rgi.RGMDto = rgmDTO;
                    Rgi.RGMDto.ClientNumber = this.ClientNumber.ToString();

                    Rgi = RGM.RGMInfo(controlLine, this.UserId.ToString(), Rgi.RGMDto);
                    Default EdefaultModel = new Default();
                    object objresponse = Rgi.RGIDto != null ? Rgi.RGIDto.Response : (Rgi.RGMDto != null ? Rgi.RGMDto.Response : Rgi.RGEDto.Response);
                    if (ErrorHandlerForE(objresponse, out EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (Rgi.PageIndicator == "1" || Rgi.PageIndicator == "3")
                    {
                        model.RGIDto = Rgi.RGIDto;
                        return PartialView("~/Views/Shared/RGI/RGI.cshtml", model);
                    }
                    else if (Rgi.PageIndicator == "2")
                    {
                        modelRgm.RGMDto = Rgi.RGMDto;
                        return PartialView("~/Views/Shared/RGI/RGM.cshtml", modelRgm);
                    }

                    else if (Rgi.PageIndicator == "4" || Rgi.PageIndicator == "9")
                    {
                        modelRge.RGEDto = Rgi.RGEDto;
                        return PartialView("~/Views/Shared/RGI/RGE.cshtml", modelRge);
                    }
                    return PartialView("~/Views/Shared/RGI/RGI.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpGet]
        public PartialViewResult LoadPartialViewRGE(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (RGEController RGE = new RGEController())
                {
                    Rgm modelRgm = new Rgm();
                    Rgi modelRgi = new Rgi();
                    Rge model = new Rge();
                    RgmRgiRgeDto Rgi = new RgmRgiRgeDto();
                    this.UniqueId = NewGuid().ToString();

                    Rgi = RGE.RGEInfo(controlValue, this.UserId.ToString(), null);
                    Default EdefaultModel = new Default();
                    object objresponse = Rgi.RGIDto != null ? Rgi.RGIDto.Response : (Rgi.RGMDto != null ? Rgi.RGMDto.Response : Rgi.RGEDto.Response);
                    if (ErrorHandlerForE(objresponse, out EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (Rgi.PageIndicator == "1" || Rgi.PageIndicator == "3")
                    {
                        modelRgi.RGIDto = Rgi.RGIDto;
                        return PartialView("~/Views/Shared/RGI/RGI.cshtml", modelRgi);
                    }
                    else if (Rgi.PageIndicator == "2")
                    {
                        modelRgm.RGMDto = Rgi.RGMDto;
                        return PartialView("~/Views/Shared/RGI/RGM.cshtml", modelRgm);
                    }
                    else if (Rgi.PageIndicator == "4" || Rgi.PageIndicator == "9")
                    {
                        model.RGEDto = Rgi.RGEDto;
                        return PartialView("~/Views/Shared/RGI/RGE.cshtml", model);
                    }
                    return PartialView("~/Views/Shared/RGI/RGE.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        public PartialViewResult LoadPartialViewEXC(string controlLine, string Hotkey, string StrRgeDetaildto, string StrRgeDto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlLine, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlLine, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlLine);

                Common.RGEDto RgeDto = new Common.RGEDto();
                List<Common.RGEDetailDto> RgeDetailDto = new List<Common.RGEDetailDto>();

                if (!(string.IsNullOrEmpty(StrRgeDto) && string.IsNullOrEmpty(StrRgeDetaildto)))
                {
                    RgeDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.RGEDto>(HttpUtility.UrlDecode(StrRgeDto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    RgeDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.RGEDetailDto>>(HttpUtility.UrlDecode(StrRgeDetaildto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    RgeDto.RGEDetails = RgeDetailDto;
                }

                using (EXCController EXC = new EXCController())
                {
                    Rgi model = new Rgi();
                    RgmRgiRgeDto RgmRgiRge = new RgmRgiRgeDto();
                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();

                    RgmRgiRge = EXC.EXCInfo(controlLine, Hotkey, this.UserId.ToString(), RgeDto);
                    Default EdefaultModel = new Default();
                    object objresponse = RgmRgiRge.RGIDto != null ? RgmRgiRge.RGIDto.Response : (RgmRgiRge.RGMDto != null ? RgmRgiRge.RGMDto.Response : RgmRgiRge.RGEDto.Response);

                    if (ErrorHandlerForE(objresponse, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (RgmRgiRge.PageIndicator.Equals("1") || RgmRgiRge.PageIndicator.Equals("3"))
                    {
                        model.RGIDto = RgmRgiRge.RGIDto;
                        return PartialView("~/Views/Shared/RGI/RGI.cshtml", model);
                    }
                    else if (RgmRgiRge.PageIndicator.Equals("2"))
                    {
                        Rgm modelRgm = new Rgm();
                        modelRgm.RGMDto = RgmRgiRge.RGMDto;
                        return PartialView("~/Views/Shared/RGI/RGM.cshtml", modelRgm);
                    }
                    else if (RgmRgiRge.PageIndicator.Equals("4") || RgmRgiRge.PageIndicator.Equals("9"))
                    {
                        Rge modelRge = new Rge();
                        modelRge.RGEDto = RgmRgiRge.RGEDto;
                        return PartialView("~/Views/Shared/RGI/RGE.cshtml", modelRge);
                    }
                    return PartialView("~/Views/Shared/RGI/RGI.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        [HttpPost]
        public PartialViewResult LoadPartialViewUpdate_RGI(string controlLine, string Hotkey, string strrgiDTO)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlLine, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlLine, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;
            Common.RGIDto rgiDTO = new RGIDto();
            if (!string.IsNullOrEmpty(strrgiDTO))
            {
                rgiDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.RGIDto>(HttpUtility.UrlDecode(strrgiDTO), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }
            try
            {
                SessionManipulation(this, clientvalue, true);
                using (RGIController RGI = new RGIController())
                {
                    Rgi model = new Rgi();
                    Rgm modelRgm = new Rgm();
                    Rge modelRge = new Rge();
                    RgmRgiRgeDto RgmRgiRge = new RgmRgiRgeDto();
                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();

                    RgmRgiRge = RGI.RGIInfo(controlLine, Hotkey, this.UserId.ToString(), rgiDTO);
                    Default EdefaultModel = new Default();
                    object objresponse = RgmRgiRge.RGIDto != null ? RgmRgiRge.RGIDto.Response : (RgmRgiRge.RGMDto!=null ? RgmRgiRge.RGMDto.Response: RgmRgiRge.RGEDto.Response);
                    if (ErrorHandlerForE(objresponse, out EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (RgmRgiRge.PageIndicator == "1" || RgmRgiRge.PageIndicator == "3")
                    {
                        model.RGIDto = RgmRgiRge.RGIDto;
                        return PartialView("~/Views/Shared/RGI/RGI.cshtml", model);
                    }
                    else if (RgmRgiRge.PageIndicator == "2")
                    {
                        modelRgm.RGMDto = RgmRgiRge.RGMDto;
                        return PartialView("~/Views/Shared/RGI/RGM.cshtml", modelRgm);
                    }

                    else if (RgmRgiRge.PageIndicator == "4" || RgmRgiRge.PageIndicator == "9")
                    {
                        modelRge.RGEDto = RgmRgiRge.RGEDto;
                        return PartialView("~/Views/Shared/RGI/RGE.cshtml", modelRge);
                    }
                    return PartialView("~/Views/Shared/RGI/RGI.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }
        public PartialViewResult LoadPartialViewAPI(string controlValue, string hotkey, string strAPIDto, string strAPIDetaildto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            MPCCRICWIDto MPCAllDto = new MPCCRICWIDto();
            Common.APIMPCDto APIDto = new Common.APIMPCDto();
            if (!String.IsNullOrEmpty(strAPIDto))
            {
                APIDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.APIMPCDto>(HttpUtility.UrlDecode(strAPIDto), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
            }
            MPCAllDto.APIMPCDto = APIDto;

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                if (!String.IsNullOrEmpty(strAPIDetaildto))
                {
                    List<Common.APIMPCDetailDto> ApiDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.APIMPCDetailDto>>(HttpUtility.UrlDecode(strAPIDetaildto), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    MPCAllDto.APIMPCDto.ApiDetails = ApiDetailDto;
                }
                using (MPCController MPC = new MPCController())
                {
                    Mpc mpc = new Mpc();

                    this.UniqueId = MPCNewGuid(this.UniqueId.ToString());
                    MPCCRICWIDto MPCmodel = new MPCCRICWIDto();

                    MPCmodel = MPC.MPCInfo(controlValue, this.UserId.ToString(), hotkey, MPCAllDto);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(MPCmodel.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (MPCmodel != null && (MPCmodel.indicator == "1" || MPCmodel.indicator == "2" || MPCmodel.indicator == "5" || MPCmodel.indicator == "6"))
                    {
                        mpc.MPCDto = MPCmodel.MPCDto;
                        this.PreviousResponse = mpc.MPCDto.Response;
                        if (MPCmodel.indicator == "1" || MPCmodel.indicator == "5")
                            return PartialView("~/Views/Shared/MPC/MPCFirst.cshtml", mpc);
                        else if (MPCmodel.indicator == "2" || MPCmodel.indicator == "6")
                            return PartialView("~/Views/Shared/MPC/MPCSecond.cshtml", mpc);
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator == "A"))
                    {
                        ApiMPC api = new ApiMPC();
                        api.APIMPCDto = MPCmodel.APIMPCDto;
                        return PartialView("~/Views/Shared/API/API.cshtml", api);
                    }

                    LoggingHelper.Log(LogType.ERROR, "API-" + this.UserId.ToString() + " LoadPartialViewAPI", "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public PartialViewResult LoadPartialViewMCI(string controlLine, string strMCIDetailDTO)
        {
            MCIDto mciDTO = new MCIDto();
            mciDTO.ControlLine = controlLine;
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlLine);

                if (!String.IsNullOrEmpty(strMCIDetailDTO))
                {
                    List<Common.MCIDetailDto> MCIDetailDto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.MCIDetailDto>>(HttpUtility.UrlDecode(strMCIDetailDTO), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    });
                    mciDTO.mciDetails = MCIDetailDto;
                }
                using (MCIController MCI = new MCIController())
                {
                    Mci Mci = new Mci();
                    this.UniqueId = NewGuid().ToString();
                    ModelState.Clear();
                    Mci.MciDto = mciDTO;

                    Mci = MCI.MCIInfo(controlLine, this.UserId.ToString(), mciDTO);
                    return PartialView("~/Views/Shared/MCI/MCI.cshtml", Mci);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public PartialViewResult LoadPartialViewCMI(string controlValue, string rdoseVal, bool isdirect = true)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (CMIController CMI = new CMIController())
                {
                    Cmi Cmi = new Cmi();
                    if (this.UniqueId.ToString() == Constants.defaultObjectType)
                    {
                        this.UniqueId = NewGuid().ToString();
                    }

                    Cmi = CMI.CmiInfo(controlValue, this.UserId.ToString(), rdoseVal);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(Cmi.CMIDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    string indictr = string.Empty;
                    var cntlline = checkForMRI(Cmi.CMIDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        return LoadPartialViewMRIRedirection(cntlline, indictr);
                    }
                    if (!string.IsNullOrEmpty(Cmi.CMIDto.PageIndicator) && Cmi.CMIDto.PageIndicator == "1")
                    {
                        return PartialView("~/Views/Shared/CMI/CMISummary.cshtml", Cmi);
                    }
                    else if (!string.IsNullOrEmpty(Cmi.CMIDto.PageIndicator) && Cmi.CMIDto.PageIndicator == "2")
                    {
                        return PartialView("~/Views/Shared/CMI/CMIDetail.cshtml", Cmi);
                    }
                    else if (!string.IsNullOrEmpty(Cmi.CMIDto.PageIndicator) && Cmi.CMIDto.PageIndicator == "3")
                    {
                        return PartialView("~/Views/Shared/CMI/CMISummary.cshtml", Cmi);
                    }
                    return PartialView("~/Views/Shared/CMI/CMISummary.cshtml", Cmi);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        private static object GetPropValue(object obj, string name)
        {
            foreach (string part in name.Split('.'))
            {
                if (obj == null) { return null; }

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null) { return null; }

                obj = info.GetValue(obj, null);
            }
            return obj;
        }

        public PartialViewResult MaiMGIMTI(string controlValue)
        {
            string[] controlList = controlValue.Split(',');
            #region---Same page--
            Type tPrevResp = PreviousResponse.GetType();
            PropertyInfo[] propPrevResp = tPrevResp.GetProperties();

            var propVal = GetPropValue(PreviousResponse, "web_linkage");
            PropertyInfo[] PropWeblinkagePrev = propVal.GetType().GetProperties();
            string OldControlLine = Convert.ToString(GetPropValue(propVal, "req_ctln_line"));
            string[] OldControlLineList = OldControlLine.Split(',');

            if (controlList.Length >= 3 && OldControlLineList.Length >= 3)
            {
                if ((controlList[0].Substring(0, 2) != OldControlLineList[0].Substring(0, 2)) || (controlList[2] != OldControlLineList[2]))
                {
                    using (MSSController MSS = new MSSController())
                    {
                        Mss mss = new Mss();
                        this.UniqueId = NewGuid().ToString();
                        mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                        var MSSControlLine = mss.MSSDto.ControlLine.Split(',');
                        if (this.CacheControlLine != null && this.CacheControlLine.ToString() != Common.Constants.defaultObjectType)
                        {
                            string preContLn = Convert.ToString(CacheControlLine);
                            if (preContLn == controlValue)
                            {
                                mss.MSSDto.NextOperation = "Yes";
                            }
                        }
                        if (mss.MSSDto.NextOperation == "Yes")
                        {
                            var isdirect = false;
                            if (MSSControlLine[0] == "MAI")
                            {
                                return LoadPartialViewMAI(controlValue, "10", isdirect);
                            }
                            else if (MSSControlLine[0] == "MGI")
                            {
                                return LoadPartialViewMGI(controlValue, "10", isdirect);
                            }
                            else if (MSSControlLine[0] == "MTI")
                            {
                                return LoadPartialViewMTI(controlValue, "10", isdirect);
                            }
                        }
                        else
                        {
                            return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                        }
                        return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                    }
                }
                else
                {
                    if (controlList[0] == "MAI" || controlList[0] == "MAN" || controlList[0] == "MAP")
                    {
                        return LoadPartialViewMAI(controlValue, "10");
                    }
                    else if (controlList[0] == "MGI" || controlList[0] == "MGN" || controlList[0] == "MGP")
                    {
                        return LoadPartialViewMGI(controlValue, "10");
                    }
                    else if (controlList[0] == "MTI" || controlList[0] == "MTN" || controlList[0] == "MTP")
                    {
                        return LoadPartialViewMTI(controlValue, "10");
                    }
                }
            }

            LoggingHelper.Log(LogType.ERROR, "MAI-" + this.UserId.ToString() + " MAIMGIMTI", "IncorrectValue");
            return PartialView("");

            #endregion
        }

        [HttpGet]
        public JsonResult CheckFromMpcOrNot()
        {
            //VXP-investigate - why no get client number earlier
            string userId = string.Empty;
            string clientno = string.Empty;
            ValidateUserClient(out userId, out clientno); //improvement opurtunity- what to do if userid is not authenticated?


            string result = "NO";
            if (FromMPC.ToString() != Common.Constants.defaultObjectType && FromMPC.ToString() == "YES")
            {
                result = FromMPC.ToString();
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult CheckMJIOrNot(string controlValue)
        {
            //VXP-investigate - why no get client number earlier
            string userId = string.Empty;
            string clientno = string.Empty;
            ValidateUserClient(out userId, out clientno); //improvement opurtunity- what to do if userid is not authenticated?


            string result = "NO";
            if (MJIControlLine.ToString() != Common.Constants.defaultObjectType && MJIControlLine.ToString().Contains("MJI"))
            {
                result = "YES";
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private static bool IsSuffChangeMSSRequird(object PrevResp, string NewCntLine)
        {
            bool Output = false;

            Type tPrevResp = PrevResp.GetType();
            PropertyInfo[] propPrevResp = tPrevResp.GetProperties();

            var propVal = GetPropValue(PrevResp, "web_linkage");
            PropertyInfo[] PropWeblinkagePrev = propVal.GetType().GetProperties();
            var OldControlLine = GetPropValue(propVal, "req_ctln_line");

            Output = Convert.ToString(OldControlLine).Trim().Split(',')[2].Equals(NewCntLine.Trim().Split(',')[2]);

            if (Output && PrevResp.ToString().Contains("MSS"))
                Output = false;

            return Output;
        }

        [HttpPost]
        public string LoadPartialViewLoadFR(DigFrDto frDto)
        {
            // Sanitize values passed from client-side
            frDto.MemberId = Server.HtmlEncode(frDto.MemberId);
            frDto.Suffix = Server.HtmlEncode(frDto.Suffix);
            frDto.FirstName = Server.HtmlEncode(frDto.FirstName);
            frDto.Relationship = Server.HtmlEncode(frDto.Relationship);
            frDto.ClaimNumber = Server.HtmlEncode(frDto.ClaimNumber);

            string userId = string.Empty;
            string clientvalue = string.Empty;
            ValidateUserClient(out userId, out clientvalue); //improvement opurtunity- what to do if userid is not authenticated?

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (FrAndBaagController frBaagController = new FrAndBaagController())
                {
                    return frBaagController.GetApplicationUrl(frDto);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public string LoadPartialViewLoadBAAG(DigBaagDto baagDto)
        {
            // Sanitize values passed from client-side
            baagDto.MemberId = Server.HtmlEncode(baagDto.MemberId);
            baagDto.Suffix = Server.HtmlEncode(baagDto.Suffix);
            baagDto.FirstName = Server.HtmlEncode(baagDto.FirstName);
            baagDto.Relationship = Server.HtmlEncode(baagDto.Relationship);
            baagDto.CustomerNumber = Server.HtmlEncode(baagDto.CustomerNumber);
            baagDto.CustomerNumberSfx = Server.HtmlEncode(baagDto.CustomerNumberSfx);

            string userId = string.Empty;
            string clientvalue = string.Empty;
            ValidateUserClient(out userId, out clientvalue); //improvement opurtunity- what to do if userid is not authenticated?

            try
            {
                SessionManipulation(this, clientvalue, true);
                using (FrAndBaagController frBaagController = new FrAndBaagController())
                {
                    return frBaagController.GetApplicationUrl(baagDto);
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpPost]
        public JsonResult MssCheck(string controlValue)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                // This behavior does not seem to cancel the action, the data is still returned to the partialView, but
                // it does prevent possible malicious code from being stored in CacheControlLine
                return Json(new { ISMSSHit = false, message = "Invalid control line passed" }, JsonRequestBehavior.AllowGet);
            }

            string userId = string.Empty;
            string clientvalue = string.Empty;
            ValidateUserClient(out userId, out clientvalue); //improvement opurtunity- what to do if userid is not authenticated?

            try
            {
                this.ISMSSHit = true;
                this.CacheControlLine = controlValue;
                return Json(ISMSSHit, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        public string checkForMRI(dynamic responseObj, out string indicator)
        {
            string controlLine = string.Empty;
            string scnInd = string.Empty;
            indicator = "";
            if (responseObj != null)
            {
                controlLine = responseObj.web_linkage.req_ctln_line;
                scnInd = responseObj.web_linkage.req_screen_ind;
                indicator = scnInd;


            }
            if (string.IsNullOrWhiteSpace(controlLine) || controlLine.IndexOf("MRI") != 0 ||
                    string.IsNullOrWhiteSpace(scnInd) || (scnInd != Common.Constants.mriIndicator && scnInd != Common.Constants.mssIndicator))

            {
                controlLine = "";
                indicator = "";
            }
            return controlLine;
        }
        public PartialViewResult LoadPartialViewMRIRedirection(string controlValue, string screenIndicator, bool isdirect = true)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (isdirect)
                    SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                if (screenIndicator == Common.Constants.mriIndicator)
                {
                    using (MRIController MRI = new MRIController())
                    {
                        Mri mri = new Mri();
                        mri = MRI.MRIInfoScInd(controlValue, this.UserId.ToString(), screenIndicator);
                        return PartialView("~/Views/Shared/MRI/MRI.cshtml", mri);
                    }
                }
                else if (screenIndicator == Common.Constants.mssIndicator)
                {
                    return LoadPartialViewMSS(controlValue);
                }

                LoggingHelper.Log(LogType.ERROR, "MRI" + this.UserId.ToString() + " LoadPartialViewMRIRedirection", "IncorrectValue");
                return PartialView("");
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        [HttpGet]
        public PartialViewResult loadPartialViewDefault(string controlValue, string validationMsg)
        {

            //VXP-investigate - why no get client number earlier
            string userId = string.Empty;
            string clientvalue = string.Empty;
            ValidateUserClient(out userId, out clientvalue); //improvement opurtunity- what to do if userid is not authenticated?

            LoggingHelper.Log(ElasticType.ControlLine, controlValue);

            Default defaultModel = new Default();
            defaultModel.ControlLine = controlValue;
            defaultModel.UserId = this.UserId.ToString();
            defaultModel.ResMsgs = validationMsg;
            return PartialView("~/Views/Shared/Default/Default.cshtml", defaultModel);
        }

        private bool MssSkipCheck(string MssRespControlLine, string controlLine)
        {
            string[] arrMRI = new string[] { "MRI", "MRU" };
            var res = false;
            if (arrMRI.Any(s => s.Contains(MssRespControlLine)))
            {
                return res;
            }
            else
            {
                if (Utilities.IsMSSSkip(controlLine, this.ISMSSHit, this.CacheControlLine))
                    return res = true;
            }
            return res;
        }

        [HttpGet]
        public void closetabBrowser(string controlValue, bool iscallbybyimediate)
        {
            //var clientvalue = getValueFromURL("clientNumber");

            string userId = string.Empty;
            string clientvalue = string.Empty;
            ValidateUserClient(out userId, out clientvalue); //improvement opurtunity- what to do if userid is not authenticated?

            BybyFuction(clientvalue, controlValue, iscallbybyimediate);
            SessionManipulation(this, clientvalue, false, true, true);
        }

        #region---Session Related functions---
        private void SessionManipulation(CASUIBaseController objCasbase, string clientNo, bool isstart = true, bool isExcep = false, bool isfromMenu = false)
        {
            if (isExcep)
            {
                if (isfromMenu)
                {
                    //ClearSessionClientwiseMenu(this, typeof(CASUIBaseController), clientNo, isExcep); // MJS
                    this.ClearClientSessionValues(); // MJS
                }
                else
                    //ClearSessionClientwise(this, typeof(CASUIBaseController), clientNo, isExcep); //MJS
                    this.ClearClientSessionValues(); // MJS
            }
            /* else // no longer needed as of CR021 - MJS
                ExpandModifierSession(this, typeof(CASUIBaseController), this.UserId, clientNo, isstart); */
        }

        private static void ExpandModifierSession(ControlLineController controlLineController, Type tModifier, object userId, string clientno, bool defaultAssi = true)
        {

            var obj = Activator.CreateInstance(tModifier); // Create instance of CASUIBaseController

            if (controlLineController != null)
            {
                // Find the UserDetailsList item that matches the currentUserId
                UserClientDetail userClientDetail = controlLineController.UserClientDetailList.Find(x => x.UserId == userId);
                // gets properties for CASUIBaseController
                PropertyInfo[] propBase = tModifier.GetProperties();

                if (defaultAssi)
                {
                    #region Default--
                    var itemNamea = (from prbase in propBase
                                     where (prbase.Name.EndsWith(clientno))
                                     select prbase).ToArray();

                    foreach (var item in itemNamea)
                    {
                        var propName = item.Name;
                        var propNameclient = propName.Replace(clientno, "");

                        var prop = tModifier.GetProperty(item.Name);
                        var propClient = tModifier.GetProperty(propNameclient);

                        if (prop != null && item.PropertyType.FullName == "System.Object" && item.Name != "ViewBag"
                            && !item.Name.Contains("UserIdOver"))
                        {
                            var proval = GetPropValueSession(userClientDetail, propName);
                            propClient.SetValue(obj, proval, null);
                        }

                    }
                    #endregion
                }
                else
                {
                    #region --Cleint wise--
                    var cleintlist = Enum.GetValues(typeof(CASClient)).Cast<int>().ToList(); //getting all cleint from Enum

                    foreach (var item in propBase) // for each property in userDetails
                    {
                        var propName = item.Name;
                        var propNameclient = propName + clientno;
                        if (!cleintlist.Any(s => propName.EndsWith(s.ToString())))  // propName does not end with 56, 57, 58, 59, 60, 100
                        {
                            var prop = tModifier.GetProperty(item.Name); // get CASUIBaseController Property for base (e.g. ActualClientNumber)
                            var propClient = tModifier.GetProperty(propNameclient); // get CASUIBaseController Property for client (e.g. ActualClientNumber58)

                            if (prop != null && item.PropertyType.FullName == "System.Object" && item.Name != "ViewBag"
                                && !item.Name.Contains("UserIdOver"))  // if base property exists in CASUIBaseController...
                            {
                                var proval = GetPropValueSession(userClientDetail, propName);  // retrieve the value of from the session (e.g. 58)
                                propClient.SetValue(obj, proval, null); // set the value of client property (e.g. ActualClientNumber58  = 58), Note: Will stay populated unless overwritten or one of the "ClearSession" methods are called
                            }
                        }

                        #endregion
                    }
                }
            }
        }

        /* no longer needed as of CR021 - MJS
        private static void ClearSessionClientwise(object objbasecont, Type tModifier, string clientno, bool isclearCleintWise = true)
        {

            var obj = Activator.CreateInstance(tModifier);

            if (objbasecont != null)
            {
                Type tBase = objbasecont.GetType();
                PropertyInfo[] propBase = tBase.GetProperties();

                if (isclearCleintWise)
                {
                    #region cleint Wise--
                    var itemNamea = (from prbase in propBase
                                     where (prbase.Name.EndsWith(clientno))
                                     select prbase).ToArray();

                    foreach (var item in itemNamea)
                    {
                        var propName = item.Name;
                        var prop = tModifier.GetProperty(item.Name);
                        if (prop != null && item.PropertyType.FullName == "System.Object"
                            && item.Name != "ViewBag" && item.Name.IndexOf("ClientNumber") < 0
                            && item.Name.IndexOf("ActualClientNumber") < 0 && item.Name.IndexOf("UserId") < 0
                            && !item.Name.Contains("UserIdOver"))
                        {
                            prop.SetValue(obj, null, null);
                        }
                    }
                    #endregion
                }
                else
                {
                    #region cleint Wise --
                    var itemNamea = (from prbase in propBase
                                     where (prbase.Name.EndsWith(clientno))
                                     select prbase).ToArray();

                    foreach (var item in itemNamea)
                    {
                        var propName = item.Name;
                        var prop = tModifier.GetProperty(item.Name);
                        if (prop != null && item.PropertyType.FullName == "System.Object"
                            && item.Name != "ViewBag"
                            && item.Name.IndexOf("UserId") < 0
                            && !item.Name.Contains("UserIdOver"))
                        {
                            prop.SetValue(obj, null, null);
                        }
                    }
                    #endregion
                }

            }
        }*/

        /* no longer needed as of CR021 - MJS
        private static void ClearSessionClientwiseMenu(object objbasecont, Type tModifier, string clientno, bool isclearCleintWise = true)
        {

            var obj = Activator.CreateInstance(tModifier);

            if (objbasecont != null)
            {
                Type tBase = objbasecont.GetType();
                PropertyInfo[] propBase = tBase.GetProperties();

                if (isclearCleintWise)
                {
                    #region cleint Wise--
                    var itemNamea = (from prbase in propBase
                                     where (prbase.Name.EndsWith(clientno))
                                     select prbase).ToArray();

                    foreach (var item in itemNamea)
                    {                        
                        var prop = tModifier.GetProperty(item.Name);
                        if (prop != null && item.PropertyType.FullName == "System.Object"
                            && item.Name != "ViewBag" && !item.Name.Contains("UserId")
                            && !item.Name.Contains("UserIdOver"))
                        {
                            prop.SetValue(obj, null, null);
                        }
                    }
                    #endregion
                }

            }
        }*/

        /* no longer needed as of CR021 - MJS
        private static void ClearSessionDefaultClient(object objbasecont, Type tModifier, string clientno)
        {

            var obj = Activator.CreateInstance(tModifier);

            if (objbasecont != null)
            {
                Type tBase = objbasecont.GetType();
                PropertyInfo[] propBase = tBase.GetProperties();

                var cleintlist = Enum.GetValues(typeof(CASClient)).Cast<int>().ToList(); //getting all cleint from Enum

                foreach (var item in propBase)
                {
                    var propName = item.Name;                    
                    if (!cleintlist.Any(s => propName.EndsWith(s.ToString())))
                    {
                        var prop = tModifier.GetProperty(item.Name);

                        if (prop != null && item.PropertyType.FullName == "System.Object" && item.Name != "ViewBag"
                            && !item.Name.Contains("UserIdOver"))
                        {

                            prop.SetValue(obj, null, null);
                        }
                    }


                }
            }
        }*/

        private static object GetPropValueSession(object obj, string name)
        {
            foreach (string part in name.Split('.'))
            {
                if (obj == null) { return null; }

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null) { return null; }

                obj = info.GetValue(obj, null);
            }
            return obj;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="urlReferer">True will pull value from the referring URL.  False (the default) will pull the value from the current URL.</param>
        /// <returns></returns>
        private string GetValueFromURL(string variableName, bool urlReferer = true)
        {
            string variableValue = string.Empty;
            string url = urlReferer ? Convert.ToString(HttpContext.Request.UrlReferrer) : Convert.ToString(HttpContext.Request.Url);
            if (!string.IsNullOrWhiteSpace(url))
            {
                var queryCollection = urlReferer ?
                        System.Web.HttpUtility.ParseQueryString(HttpContext.Request.UrlReferrer.Query) :
                        System.Web.HttpUtility.ParseQueryString(HttpContext.Request.Url.Query);

                if (queryCollection.AllKeys.Contains(variableName) && !string.IsNullOrEmpty(queryCollection.Get(variableName)))
                {
                    variableValue = Server.HtmlEncode(queryCollection.Get(variableName)); // had to encode to satisy CheckMarx Scan
                }
            }
            else
            {
                variableValue = "";
            }            

            // Additional field specific validation to ensure against trust boundary violations
            if (variableName.ToLower() == "userid")
            {
                Match idMatch = Regex.Match(variableValue, Constants.userIdPattern);
                if (!idMatch.Success)
                {
                    variableValue = "";
                }
            }
            else if (variableName.ToLower() == "clientnumber")
            {
                Match clientMatch = Regex.Match(variableValue, Constants.clientNoPattern);
                if (!clientMatch.Success)
                {
                    variableValue = "";
                }
                else if (variableValue == ((int)CASClient.CASFileMaintenance).ToString())
                {
                    variableValue = Common.Constants.FileMaintenanceClientNumber;
                }
            }

            return variableValue;
        }

        #endregion

        private void BybyFuction(string clientno, string controlLine = "BYCL", bool isbybycall = false)
        {
            bybyFlag = false;
            using (ByebyeController bye = new ByebyeController())
            {
                this.UniqueId = NewGuid().ToString();
                var userID = "";
                if (controlLine == "BYBY" || controlLine == "EXIT")
                {
                    userID = CheckAndReturnUserID(clientno);
                    controlLine = "BYCL";
                }
                else
                {
                    userID = this.UserId.ToString();
                }
                bye.ByeByeInfo(controlLine, userID, clientno, isbybycall);

            }
        }
        //created this function to invoke byby for a particula user client combination after CR21
        private void BybyUser(string userId, string clientno, bool isbybycall = false)
        {
            bybyFlag = false;
            using (ByebyeController bye = new ByebyeController())
            {
                this.UniqueId = NewGuid().ToString();
                bye.ByeByeInfo("BYCL", userId, clientno, isbybycall);

            }
        }
        [HttpGet]
        public ActionResult OverUserId(string newUserId)
        {
            //var clientvalue = getValueFromURL("clientNumber");

            string userId = string.Empty;
            string clientvalue = string.Empty;
            ValidateUserClient(out userId, out clientvalue); //improvement opurtunity- what to do if userid is not authenticated?

            Match idMatch = Regex.Match(newUserId, Constants.userIdPattern);
            if (!idMatch.Success)
            {
                TempData["RedirectMessage"] = "Not a valid User Id.";
                return RedirectToAction("Menu", "Menu");
            }

            LoggingHelper.Log(ElasticType.Login, newUserId.ToUpper(), LoginType.Over, true, this.UserId.ToString());

            newUserId = newUserId.ToUpper();
            this.UserIdOver = newUserId;
            this.UserId = newUserId;
            if (CurrentUserClientIndex > -1 && UserClientDetailList.Count > CurrentUserClientIndex)
            {
                UserClientDetailList[CurrentUserClientIndex].UserId = UserId;
                AddToCache("UserClientDetailList", UserClientDetailList);
            }
            else
            {
                //improvement opurtunity- This is an error condition and should never happen
            }
            this.ClientNumber = clientvalue;
            this.IsPrimaryId = false;

            List<string> userList = this.AuthenticatedUserList == null ? new List<string>() : this.AuthenticatedUserList;
            userList.Add(newUserId);
            this.AuthenticatedUserList = userList;

            string uidClientNo = userId + ":" + clientvalue;
            this.ClientNumberList.Remove(uidClientNo);

            BybyUser(userId, clientvalue); //added this byby call after inplave of the two byby callsfrom javascript. closebrosertab and removesession
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        private string CheckAndReturnUserID(string clientNo)
        {
            string UserIDA = string.Empty;
            string returnUserID = string.Empty;
            //bool IsADOn = false;
            //IsADOn = (ConfigHelper.GetValue("AuthenticationMode") == "ActiveDirectory");
            //if(IsADOn.Equals(false))
            //{
            //    return this.UserId.ToString();
            //}

            returnUserID = this.UserIdOver.ToString();
            if (returnUserID != Common.Constants.defaultObjectType)
            {
                UserIDA = returnUserID;
            }
            else
            {
                UserIDA = this.UserId.ToString(); // User.Identity.Name.ToString().Replace(Common.Constants.HumandStr, "");
            }

            //UserIDA = UserIDA == "" ? this.UserId.ToString() : UserIDA;
            return UserIDA;


        }


        private bool ChekClientWiseSession(object objbasecont, Type tModifier, string sessionVarName)
        {
            bool checkRes = true;
            int SessionCount = 0;
            var obj = Activator.CreateInstance(tModifier);

            if (objbasecont != null)
            {
                Type tBase = objbasecont.GetType();
                PropertyInfo[] propBase = tBase.GetProperties();
                #region  check more than one session active or not--
                var itemNamea = (from prbase in propBase
                                 where (prbase.Name.StartsWith(sessionVarName))
                                 select prbase).ToArray();

                foreach (var item in itemNamea)
                {
                    var propName = item.Name;
                    var proval = GetPropValueSession(objbasecont, propName);
                    if (proval.ToString() != Common.Constants.defaultObjectType && sessionVarName != propName)
                    {
                        SessionCount++;
                    }
                }
                #endregion
            }
            if (SessionCount > 1)
            {
                checkRes = false;
            }

            return checkRes;
        }


        public void ClearAllsession()
        {
            BybyFuction("", "BYBY", true);
            UserSessionHandler.FlushUserSession(this.HttpContext);

            ////return Json(true, JsonRequestBehavior.AllowGet);
        }
        public void RemoveSession(string controlval = "BYCL")
        {
            //var clientno = getValueFromURL("clientNumber", true);
            //var userId = getValueFromURL("userId", true);

            string userId = string.Empty;
            string clientno = string.Empty;

            ValidateUserClient(out userId, out clientno);
            //    return; //loadPartialViewDefault("", "redirect");

            this.UserId = userId.ToUpper();
            string uidClientNo = userId + ":" + clientno;

            this.ClientNumberList.Remove(uidClientNo);
            if (controlval == "NO")
            {
                controlval = "BYCL";
                this.PrevResMPCToEDIISIDMI = null;
                BybyFuction(clientno, controlval, true);
                SessionManipulation(this, clientno, false, true, true);

            }
            else if (controlval == "YES")
            {
            }
            else if (controlval == "BYBY" || controlval == "EXIT")
            {
                this.PrevResMPCToEDIISIDMI = null;
                BybyFuction(clientno, controlval, true);
                SessionManipulation(this, clientno, false, true, true);
            }
            else
            {
                this.PrevResMPCToEDIISIDMI = null;
                BybyFuction(clientno, controlval, true);
                SessionManipulation(this, clientno, false, true, true);


            }

        }
        public ActionResult LoadPartialViewUMIDMSS(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    this.UserId = userId.ToUpper();
                }
                else
                {
                    this.UserId = ExtractUserId(User.Identity.Name).ToUpper();
                }

                SessionManipulation(this, clientvalue, true);
                string[] nonMsspayment = { "MQI", "MYI", "MZI", "MJI", "MKI", "MEI" };

                using (MSSController MSS = new MSSController())
                {
                    var controllist = controlValue.Split(',');
                    Mss mss = new Mss();

                    if (Array.IndexOf(nonMsspayment, controllist[0]) > -1)
                    {
                        MSSDto model = new MSSDto();
                        model.NextOperation = "Yes";
                        mss.MSSDto = model;
                    }
                    else
                        mss = MSS.MSSInfo(controlValue, this.UserId.ToString());

                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(mss.MSSDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (mss.MSSDto.NextOperation == "Yes")
                    {
                        switch (controllist[0])
                        {
                            case "MHI":
                                return LoadPartialViewUMIDMHI(controlValue);
                            case "CMI":
                                return LoadPartialViewCMI(controlValue, null, false);
                            case "CRI":
                                return LoadPartialViewCRI(controlValue, false);
                            case "PRI":
                                return LoadPartialViewPRI(controlValue, false);
                            case "MDI":
                                return LoadPartialViewMDI(controlValue, null, null, false, false);
                            case "MRI":
                                return LoadPartialViewMRI(controlValue, false);
                            case "MSI":
                                return LoadPartialViewMSI(controlValue);
                            case "CWI":
                                return LoadPartialViewCWI(controlValue, false);
                            case "RFI":
                                return LoadPartialViewRFI(controlValue, null, false);
                            case "RSI":
                                return LoadPartialViewRSI(controlValue, null, false);
                            case "PCI":
                                return LoadPartialViewPCI(controlValue, false);
                            case "MPI":
                            case "MXI":
                            case "MQI":
                            case "MYI":
                            case "MZI":
                            case "MJI":
                            case "MKI":
                            case "MEI":
                                return LoadPartialViewMPI(controlValue, "", null, null, false);
                            default:
                                {

                                    LoggingHelper.Log(LogType.ERROR, "UMID-" + this.UserId.ToString() + " LoadPartialViewUMIDMSS", "IncorrectValue");
                                    return PartialView("");
                                }

                        }
                    }
                    else
                    {
                        this.ISMSSHit = true;
                        return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                    }
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }

        }

        public PartialViewResult LoadPartialViewUMIDMHI(string controlValue)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                using (MHIController MHI = new MHIController())
                {
                    Mhi mhi = new Mhi();
                    ModelState.Clear();

                    mhi = MHI.MHIInfo(controlValue, this.UserId.ToString(), true);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(mhi.ModifierDto.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (!string.IsNullOrEmpty(mhi.ModifierDto.ind) && mhi.ModifierDto.ind.Equals("MRI"))
                    {
                        #region Added MRI Navigation logic 
                        using (MRIController MRI = new MRIController())
                        {
                            Mri mri = new Mri();

                            mri = MRI.MRIInfo(mhi.ModifierDto.ControlLine, this.UserId.ToString(), null);
                            return PartialView("~/Views/Shared/MRI/MRI.cshtml", mri);
                        }
                        #endregion
                    }
                    if (!string.IsNullOrEmpty(mhi.ModifierDto.ResMsgs))
                    {
                        Default defaultModel = new Default();
                        defaultModel.ControlLine = controlValue;
                        defaultModel.UserId = this.UserId.ToString();
                        defaultModel.ResMsgs = mhi.ModifierDto.ResMsgs;
                        return PartialView("~/Views/Shared/Default/Default.cshtml", defaultModel);

                    }
                    else if (mhi.ModifierDto.ResInnerMsgs.Equals("HISTORY MODIFIER INVALID"))
                    {
                        Default defaultModel = new Default();
                        defaultModel.ControlLine = controlValue;
                        defaultModel.UserId = this.UserId.ToString();
                        defaultModel.ResMsgs = "HISTORY MODIFIER INVALID";
                        return PartialView("~/Views/Shared/Default/Default.cshtml", defaultModel);
                    }
                    else
                    {
                        mhi.ListModifier = ModifierList();
                        return PartialView("~/Views/Shared/MHI/MHI.cshtml", mhi);
                    }
                }



            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }
        }

        public PartialViewResult LoadPartialViewPaymentToMRI(string controlValue, string iniControlln, string indicator)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;

            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            string[] iniControllnArr = string.IsNullOrWhiteSpace(iniControlln) ? new[] {""}: iniControlln.Split(',');

            MSSController MSS = new MSSController();
            Mri mri = new Mri();
            string[] cntlarrInitinal = { "MYI", "MZI", "MQI", "MJI", "MEI", "MKI" }; //"MXI", "MPI",
            if (Array.IndexOf(cntlarrInitinal, iniControllnArr[0]) > -1 )
            {
                Mss mss = new Mss();
                mss = MSS.MSSInfo(controlValue, this.UserId.ToString());
                Default EdefaultModel = new Default();
                if (ErrorHandlerForE(mss.MSSDto.Response, out  EdefaultModel))
                {
                    return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                }
                if (indicator.Equals(Common.Constants.mriIndicator))
                {
                    using (MRIController MRI = new MRIController())
                    {
                        mri = MRI.MRIInfo(controlValue, this.UserId.ToString(), null);
                    }
                    return PartialView("~/Views/Shared/MRI/MRI.cshtml", mri);
                }
                else
                {
                    return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                }
            }
            else
            {
                using (MRIController MRI = new MRIController())
                {
                    mri = MRI.MRIInfo(controlValue, this.UserId.ToString(), null);
                }
                return PartialView("~/Views/Shared/MRI/MRI.cshtml", mri);

            }

        }

        private PartialViewResult LoadPartialViewMZItoMRI(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MSSController MSS = new MSSController())
                {

                    Mss mss = new Mss();
                    this.UniqueId = NewGuid().ToString();

                    mss = MSS.MSSInfo(controlValue, this.UserId.ToString());

                    if (mss.MSSDto.NextOperation == "Yes" || ((!String.IsNullOrEmpty(Convert.ToString( this.ISMSSHit))) && this.ISMSSHit.Equals(true)))
                    {
                        return LoadPartialViewMRI(controlValue, false);
                    }
                    else
                    {
                        return PartialView("~/Views/Shared/MSS/MSS.cshtml", mss);
                    }
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }
        }


        public ActionResult LoadPartialViewMRICSP(string controlValue)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    this.UserId = userId.ToUpper();
                }
                else
                {
                    this.UserId = ExtractUserId(User.Identity.Name).ToUpper();
                }

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                using (MRICSPController MRICSP = new MRICSPController())
                {
                    MriCsp mricsp = MRICSP.MRICSPInfo(controlValue, this.UserId.ToString());
                    string indictr = string.Empty;
                    var cntlline = checkForMRI(mricsp.MRICSPDto.Response, out indictr);
                    if (!string.IsNullOrWhiteSpace(cntlline))
                    {
                        cntlline = cntlline.Replace("CSP", "").Trim();
                        return LoadPartialViewMRIRedirection(cntlline, indictr, false);
                    }
                    return PartialView("~/Views/Shared/MRICSP/MRICSP.cshtml", mricsp);
                }


            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }
        }


/// <summary>
      
        [HttpGet]
        public JsonResult CheckPrevControlLine(string controlValue, bool isOnMSS)
        {
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return Json("NO", JsonRequestBehavior.AllowGet); // loadPartialViewDefault("", "redirect");
            string result = "NO";
            string reqcontrolValue = "";
            string prevcontrolValue = "";
            dynamic PreviousResp = this.PreviousResponse;
            if (PreviousResp.GetType() != typeof(object))
            {
                string[] strArray = PreviousResponse.GetType().Namespace.ToUpper().Split(new char[] { '.' });
                if (!string.IsNullOrEmpty(strArray[strArray.Length - 1]))
                {
                    prevcontrolValue = strArray[strArray.Length - 1];
                }
                if(prevcontrolValue.ToUpper().Equals("CSP"))
                {
                    prevcontrolValue = "MRI";
                }

                if (controlValue.Substring(controlValue.Length - 1, 1) == "I")
                {
                    if (!isOnMSS && controlValue.Substring(0, 2) == prevcontrolValue.Substring(0, 2))
                        controlValue = controlValue.Replace(controlValue.Substring(controlValue.Length - 1, 1), "N");
                    reqcontrolValue = GetRequiredNamespace(controlValue, prevcontrolValue);

                    if (prevcontrolValue == reqcontrolValue)
                    {
                        result = "YES";
                    }
                    else
                    {
                        result = reqcontrolValue;
                    }
                }
                else
                {
                    reqcontrolValue = GetRequiredNamespace(controlValue, prevcontrolValue);

                    if (prevcontrolValue == reqcontrolValue)
                    {
                        result = "YES";
                    }
                }
            }
            else
            {
                result = "YES";
            }

            return Json(result, JsonRequestBehavior.AllowGet);

        }
        private string GetRequiredNamespace(string controlValue, string existcontrolValue)
        {

            switch (controlValue)
            {
                case "MRU":
                case "MRN":
                    return "MRI";

                case "MAN":
                case "MAP":
                    return "MAI";

                case "RFN":
                case "RFU":
                case "RFP":
                case "RFA":
                    return "RSI";

                case "MDN":
                case "MDP":
                    return "MDI";

                case "MSN":
                    return "MSI";

                case "MGN":
                case "MGP":
                    return "MGI";

                case "MTN":
                case "MTP":
                    return "MTI";

                case "CMP":
                case "CMN":
                    return "CMI";

                case "CWN":
                case "CWP":
                case "CWU":
                case "CWA":
                    if (existcontrolValue == "MPC") return "MPC";
                    else return "CWI";

                case "CRN":
                case "CRP":
                case "CRA":
                case "CRU":
                    return "CRI";

                case "PRN":
                case "PRP":
                case "PRU":
                    return "PRI";

                case "PCN":
                case "PCU":
                case "PCA":
                    return "PCI";

                case "OCU":
                    return "OCI";

                case "RSN":
                case "RSP":
                    return "RSI";

                case "RGU":
                case "RGA":
                case "RGN":
                case "RGP":
                    return "RGI";

                case "MHN":
                case "MHP":
                    return "MHI2";
                default:
                    return "MSS";

            }
        }

        private string ExtractUserId(string userId)
        {
            var userIdSplit = StripUserDomain(userId);

            return userIdSplit.Count() > 1 ? userIdSplit[1] : userIdSplit[0];
            //User.DomainName = userIdSplit.Count() > 1 ? userIdSplit[0] : string.Empty;


        }
        private string[] StripUserDomain(string userId)
        {
            return userId.Contains(Constants.CharBackSlash) ? userId.Split(Constants.CharBackSlash) : (userId.Contains(Constants.CharAt) ? userId.Split(Constants.CharAt) : new string[] { userId });
        }

        private bool ValidateUserClient(out string userId, out string clientNumber)
        {
            string userIdURL = string.Empty;
            userId = userIdURL = GetValueFromURL("userId", true);
            clientNumber = Server.HtmlEncode(GetValueFromURL("clientNumber", true));

            // if user has not been previously authenticated, 
            if (!string.IsNullOrWhiteSpace(userId)                                  //  if userId exists
                    && !string.IsNullOrWhiteSpace(clientNumber))
            {
                if (!AuthenticatedUserList.Exists(s => s == userIdURL.ToUpper()))   //  and not authenticated
                {
                    ClientNumberList.Remove(userId.ToUpper() + ":" + clientNumber);
                    return false;
                }
                else
                {
                    if (this.UserId.ToString() != userId.ToUpper())
                        this.UserId = userId.ToUpper();
                    //if(Convert.ToInt16(this.ClientNumber) != Convert.ToInt16(clientNumber))
                    //{
                    this.ClientNumber = clientNumber;
                    ////PPV#734
                    this.ActualClientNumber = (clientNumber == "FM") ? 100 : Convert.ToInt16(clientNumber);
                    //}
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private bool AnyOtherOverClientExists()
        {
            bool anyResult =
            UserClientDetailList.Any(
                x =>
                !(x.UserId.ToString().ToUpper() == this.UserId.ToString().ToUpper() && x.ClientNumber.ToString() == this.ClientNumber.ToString())
                &&
                (
                x.UserIdOver == null || x.UserIdOver.ToString() == "System.Object"
                )
                );
            return anyResult;

        }
        public PartialViewResult LoadPartialViewCBI(string controlValue, string strCBIDetaildto, string hotkey, string indc, string strcbiDto)
        {
            // Additonal validation to ensure against Trust Boundary Violations
            Match cntrlMatch = Regex.Match(controlValue, Constants.controlLinePattern);
            if (!cntrlMatch.Success)
            {
                return loadPartialViewDefault(controlValue, "Control line contains invalid characters");
            }

            CBIDto cbiDto = new CBIDto();
            MPCCRICWIDto MPCAllDto = new MPCCRICWIDto();
            string userId = string.Empty;
            string clientNumber = string.Empty;
            if (!ValidateUserClient(out userId, out clientNumber))
                return loadPartialViewDefault("", "redirect");
            var clientvalue = clientNumber;

            try
            {
                SessionManipulation(this, clientvalue, true);

                LoggingHelper.Log(ElasticType.ControlLine, controlValue);

                if (!String.IsNullOrEmpty(strCBIDetaildto))
                {
                    if (indc == "G")
                    {
                        List<Common.CBIDetailDto> CBIDetaildto = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.CBIDetailDto>>(HttpUtility.UrlDecode(strCBIDetaildto), new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        });
                        cbiDto.cbiDetails = CBIDetaildto;
                    }
                    else if (indc == "F")
                    {
                        List<Common.CBIDto> CBIdto1 = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Common.CBIDto>>(HttpUtility.UrlDecode(strCBIDetaildto), new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        });
                        cbiDto.cbfoio = CBIdto1[0].cbfoio;
                        cbiDto.OIPAID = CBIdto1[0].OIPAID;
                        cbiDto.OIALLOWED = CBIdto1[0].OIALLOWED;
                    }
                    MPCAllDto.CBIDto = cbiDto;
                }

                using (MPCController MPC = new MPCController())
                {
                    Mpc mpc = new Mpc();
                    this.UniqueId = MPCNewGuid(this.UniqueId.ToString());
                    MPCCRICWIDto MPCmodel = MPC.MPCInfo(controlValue, this.UserId.ToString(), hotkey, MPCAllDto);
                    Default EdefaultModel = new Default();
                    if (ErrorHandlerForE(MPCmodel.Response, out  EdefaultModel))
                    {
                        return loadPartialViewDefault(EdefaultModel.ControlLine, EdefaultModel.ResMsgs);
                    }

                    if (MPCmodel != null && (MPCmodel.indicator == "1" || MPCmodel.indicator == "2" || MPCmodel.indicator == "5" || MPCmodel.indicator == "6"))
                    {
                        mpc.MPCDto = MPCmodel.MPCDto;
                        this.PreviousResponse = mpc.MPCDto.Response;
                        if (MPCmodel.indicator == "1" || MPCmodel.indicator == "5")
                            return PartialView("~/Views/Shared/MPC/MPCFirst.cshtml", mpc);
                        else if (MPCmodel.indicator == "2" || MPCmodel.indicator == "6")
                            return PartialView("~/Views/Shared/MPC/MPCSecond.cshtml", mpc);
                        else
                        {
                            LoggingHelper.Log(LogType.ERROR, "CBI-" + this.UserId.ToString() + " LoadPartialViewCBI", "IncorrectValue");
                            return PartialView("");
                        }
                    }
                    else if (MPCmodel != null && (MPCmodel.indicator.ToUpper().Equals("G") || MPCmodel.indicator.ToUpper().Equals("F")))
                    {
                        Cbi cbi = new Cbi();
                        cbi.CBIDto = MPCmodel.CBIDto;
                        if(MPCmodel.indicator.ToUpper().Equals("G"))
                            return PartialView("~/Views/Shared/CBI/CBIPhysician.cshtml", cbi);
                        else
                            return PartialView("~/Views/Shared/CBI/CBIFacility.cshtml", cbi);
                    }
                    LoggingHelper.Log(LogType.ERROR, "CBI" + this.UserId.ToString() + " LoadPartialViewCBI no indicator", "IncorrectValue");
                    return PartialView("");
                }
            }
            catch (Exception ex)
            {
                SessionManipulation(this, clientvalue, false, true);
                throw;
            }
            finally
            {
                SessionManipulation(this, clientvalue, false);
            }
        }


        public bool ErrorHandlerForE(object objSource,out Default defaultobj)
        {

            bool chkRes = false;
            Default defobj = new Default();
            try
            {

                if (objSource != null)
                {
                    Type tPrevResp = objSource.GetType();
                    PropertyInfo[] propPrevResp = tPrevResp.GetProperties();
                    foreach (var Previtem in propPrevResp)
                    {
                        var propPrevName = Previtem.Name;
                        var propVal = GetPropValue(objSource, propPrevName);
                        PropertyInfo[] PropWeblinkagePrev = propVal.GetType().GetProperties();
                        var names = new string[] { "req_screen_ind", "res_msgs", "req_ctln_line" };
                        var itemNameo = (from prbase in PropWeblinkagePrev
                                         where (names.Contains(prbase.Name))
                                         select prbase).ToArray();
                        foreach (var itemWeblinkage in itemNameo)
                        {
                            var propName = itemWeblinkage.Name;
                            var ValWeblinkage = GetPropValue(propVal, propName);
                            if (propName.ToString().Equals("req_screen_ind"))
                            {
                                defobj.req_screen_ind = ValWeblinkage.ToString();
                                if (ValWeblinkage.ToString().ToUpper() == "E")
                                {
                                    chkRes = true;
                                }
                            }
                            else if (propName.ToString().Equals("res_msgs"))
                            {
                                defobj.ResMsgs = ValWeblinkage.ToString();
                            }
                            else if (propName.ToString().Equals("req_ctln_line"))
                            {
                                defobj.ControlLine = ValWeblinkage.ToString();
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw;
            }
            defaultobj = defobj;
            return chkRes;
        }




    }

}
