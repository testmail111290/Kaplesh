using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Common;
using CTMS.Models;
using CTMS.ViewModel;
using System.Configuration;
using CTMS.Resources;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using iTextSharp.text.html.simpleparser;

namespace CTMS.Controllers
{
    public class CandidateController : Controller
    {


        #region [Properties]

        /// <summary>
        /// Set createdBy 
        /// </summary>

        public int createdBy
        {
            get
            {
                int _userID = 0;
                if (Session["UserId"] != null)
                    int.TryParse(Convert.ToString(Session["UserId"]), out _userID);
                return _userID;
            }
        }
        //common utils class for common operation.
        CommonUtils objCommonUtilError = new CommonUtils();

        #endregion

        //Object of Candidate View Model where all CRUD operation perform. 
        CandidateViewModel ObjCandidateViewModel = new CandidateViewModel();


        // GET: /Candidate/
        /// <summary>
        /// Load Candidate list
        /// </summary>
         [Filters.MenuAccess()]
        public ActionResult ViewCandidate()
        {
            ViewCandidate ObjViewCandidate = new ViewCandidate();
            try
            {
                int UserId = Convert.ToInt32(Session["UserId"]);
                ObjViewCandidate.CurrentPage = 1;
                ObjViewCandidate.PageSize = CommonUtils.PageSize;
                ObjViewCandidate.TotalPages = 0;
                GetDropDownList(ObjViewCandidate);
                ObjCandidateViewModel.GetCandidate(ObjViewCandidate, UserId);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "ViewCandidate", "ViewCandidate Get", ex);
            }
            return View(ObjViewCandidate);

        }

        /// <summary>
        /// Show Candidate list 
        /// </summary>
        /// <param name="ObjViewCandidate"></param>
        [HttpPost]
        [ValidateInput(false)]
        [Filters.Authorized()]
        public ActionResult ViewCandidate(ViewCandidate ObjViewCandidate)
        {
            int UserId = Convert.ToInt32(Session["UserId"]);
            ObjViewCandidate.Message = ObjViewCandidate.MessageType = String.Empty;
            try
            {
                if (ObjViewCandidate.ActionType != null)
                {
                    switch (ObjViewCandidate.ActionType)
                    {
                        case "search": ObjViewCandidate.CurrentPage = 1;
                            break;
                    }
                }
                ObjCandidateViewModel.GetCandidate(ObjViewCandidate, UserId);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "ViewCandidate", "ViewCandidate Post", ex);
            }

            return PartialView("_ViewCandidateList", ObjViewCandidate);
        }


        /// <summary>
        /// Return Function Names
        /// Here Function names are used as Job Roles
        /// </summary> 
        public ActionResult GetFunctionsByCategoryID(int CategoryID)
        {
            try
            {
                List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
                lstFunctionCategoryModel = ObjCandidateViewModel.GetFunctionsByCategoryIds(Convert.ToString(CategoryID));
                SelectList objFunctions = new SelectList(lstFunctionCategoryModel, "FunctionID", "FunctionName", 0);
                return Json(objFunctions);
            }
            catch (Exception ex)
            {

                return Json(ex);
            }
        }

        /// <summary>
        /// Sets Function List and JobRole List
        /// </summary> 
        public void GetDropDownList(ViewCandidate ObjViewCandidate)
        {
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            List<CategoryModel> lstCategory = objTrainingViewModel.GetAllActiveCategory(false).ToList();
            lstCategory.Insert(0, new CategoryModel { CategoryID = 0, CategoryName = "--Select--" });
            ViewBag.CategoryList = new SelectList(lstCategory, "CategoryID", "CategoryName", ObjViewCandidate.FilterCategory.ToString());
            List<FunctionModel> lstFunction = new List<FunctionModel>();
            FunctionModel objFunction = new FunctionModel();
            objFunction.FunctionID = 0;
            objFunction.FunctionName = "--Select--";
            lstFunction.Insert(0, objFunction);
            ViewBag.FunctionList = new SelectList(lstFunction, "FunctionID", "FunctionName", ObjViewCandidate.FilterFunction.ToString());
        }
         [Filters.MenuAccess()]
        public ActionResult CandidateTrainingList(int CID)
        {

            ViewCandidate ObjViewCandidate = new ViewCandidate();
            try
            {
                TrainingViewModel objTrainingViewModel = new TrainingViewModel();
                List<TrainingTypeModel> TrainingTypes = objTrainingViewModel.GetAllActiveTrainigType(true).ToList();
                ViewBag.TrainingTypeList = new SelectList(TrainingTypes, "TrainingTypeID", "TrainingTypeName", ObjViewCandidate.FilterTrainingType);
                ObjViewCandidate.CurrentPage = 1;
                ObjViewCandidate.PageSize = CommonUtils.PageSize;
                ObjViewCandidate.TotalPages = 0;
                ObjViewCandidate.CandidateID = CID;
                GetDropDownList(ObjViewCandidate);
                ObjCandidateViewModel.GetCandidateBookedTraining(ObjViewCandidate);
                ObjViewCandidate.CandidateID = CID;

                ObjCandidateViewModel.GetCandidateDetail(ObjViewCandidate, createdBy);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "ViewCandidate", "CandidateTrainingList Get", ex);
            }
            return View(ObjViewCandidate);
        }
        [HttpPost]
        [Filters.Authorized()]
        public ActionResult CandidateTrainingList(ViewCandidate ObjViewCandidate)
        {

            try
            {

                if (ObjViewCandidate.ActionType != null)
                {
                    switch (ObjViewCandidate.ActionType)
                    {
                        case "search": ObjViewCandidate.CurrentPage = 1;
                            break;
                        

                    }
                }
                ObjCandidateViewModel.GetCandidateBookedTraining(ObjViewCandidate);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "ViewCandidate", "CandidateTrainingList Post", ex);
            }
            return PartialView("_ViewTrainingList", ObjViewCandidate);
        }
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        /// <summary>
        /// Print certificate details
        /// </summary>
        /// <param name="TID"></param>
        /// <param name="CID"></param>
        /// 

        public void PrintCertificate(int TID, int CID)
        {
            string certificate = string.Empty;
            try
            {
                certificate = ObjCandidateViewModel.GetPrintCertificate(TID, CID, createdBy);
                Uri uri = new Uri(HttpContext.Request.Url.AbsoluteUri);

                //certificate = certificate.Replace("/CertificateImages/", uri.Scheme + "://" + uri.Host + ":" + uri.Port + "/CertificateImages/");
                certificate = certificate.Replace("/CertificateImages/", CommonUtils.WebSiteUrl + "/CertificateImages/");

                if (!string.IsNullOrEmpty(certificate))
                {

                    HTMLToPdf(certificate, true);
                }

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Candidate", "PrintCertificate", ex);
            }
        }
        //this will return pdf from html
        public void HTMLToPdf(string HTML, bool IsPrint)
        {
            try
            {

                string fileName = "Certificate.pdf";
                Document document1 = new Document(PageSize.A4, 25, 25, 30, 30);
                // Set the page size
                document1.SetPageSize(iTextSharp.text.PageSize.A4.Rotate());
                PdfWriter writer = PdfWriter.GetInstance(document1, System.Web.HttpContext.Current.Response.OutputStream);
                if (IsPrint)
                {
                    PdfAction action = new PdfAction(PdfAction.PRINTDIALOG);
                    writer.SetOpenAction(action);
                }
                document1.Open();

                iTextSharp.text.html.simpleparser.HTMLWorker hw1 = new iTextSharp.text.html.simpleparser.HTMLWorker(document1);
                StringReader rdr = new StringReader(HTML);
                hw1.Parse(rdr);
                document1.Close();
                writer.Close();

                System.Web.HttpContext.Current.Response.ContentType = "application/pdf";
                System.Web.HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=\"" + fileName + "\"");
                System.Web.HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);

                System.Web.HttpContext.Current.Response.Write(document1);
                System.Web.HttpContext.Current.Response.End();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
       



    }
}
