/*
* @author  : Soumen Bhowmick
* @version : 0.0.0.1
* @since   : 12th June 2014 
* 
* Modification History :
* Date of Modification		Modified By			Changes made 
* -------------------------------------------------------------------------------------------------------
* 
*                                              
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using CTMS.Models;
using CTMS.ViewModel;
using CTMS.Common;
namespace CTMS.Controllers
{
    public class ParticipantFeedbackController : Controller
    {
        CommonUtils objCommonUtilError = new CommonUtils();

        public ActionResult ViewFeedback(int ScheduledTrainingId, string CallingModule, int TrainingId)
        {
            int RoleId;
            BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
            int.TryParse(Session["RoleId"].ToString(), out RoleId);
            ViewScheduledTrainingDetails objViewScheduledTrainingDetails = new ViewScheduledTrainingDetails();
            objViewScheduledTrainingDetails.CurrentPage = 1;
            objViewScheduledTrainingDetails.PageSize = CommonUtils.PageSize;
            objViewScheduledTrainingDetails.TotalPages = 0;

            objBookedCandidateViewModel.GetParticipantFeedback(ScheduledTrainingId, TrainingId, RoleId, objViewScheduledTrainingDetails);

            if (TempData["Message"] != null && TempData["MessageType"] != null)
            {
                objViewScheduledTrainingDetails.Message = TempData["Message"].ToString();
                objViewScheduledTrainingDetails.MessageType = TempData["MessageType"].ToString();
            }
            else
            {
                objViewScheduledTrainingDetails.Message = string.Empty;
                objViewScheduledTrainingDetails.MessageType = string.Empty;
            }
            ViewBag.FeedbackCallingModule = CallingModule;
            Session["FeedbackCallingModule"] = CallingModule;
            return View(objViewScheduledTrainingDetails);
        }

        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewFeedback(ViewScheduledTrainingDetails objViewScheduledTrainingDetails)
        {
            int RoleId;
            BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
            int.TryParse(Session["RoleId"].ToString(), out RoleId);
            objBookedCandidateViewModel.GetParticipantFeedback(objViewScheduledTrainingDetails.ScheduleTraining.ScheduledTrainingID, objViewScheduledTrainingDetails.ScheduleTraining.TrainingID, RoleId, objViewScheduledTrainingDetails);
            return PartialView("_FeedbackListPartial", objViewScheduledTrainingDetails);
        }

        [HttpPost]
        [Filters.Authorized()]
        public ActionResult SubmitFeedback(string jsonData, int IsSubmitted, int ScheduledTrainingID, int TrainingID)
        {
            BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
            JavaScriptSerializer jss = new JavaScriptSerializer();
            List<SubmitFeedbackModel> lstFeedback = new List<SubmitFeedbackModel>();
            int UserId, errorCode;
            string errorMessage, redirectUrl;
            string CallingModule = string.Empty;

            if (Session["FeedbackCallingModule"] != null)
                CallingModule = Session["FeedbackCallingModule"].ToString();

            int.TryParse(Session["UserId"].ToString(), out UserId);
            try
            {
                lstFeedback = jss.Deserialize<List<SubmitFeedbackModel>>(jsonData);

                objBookedCandidateViewModel.SaveCandidateFeedback(lstFeedback, TrainingID, Convert.ToBoolean(IsSubmitted), UserId, out errorCode, out errorMessage);
                if (errorCode.Equals(0))
                {
                    TempData["Message"] = errorMessage;
                    TempData["MessageType"] = MessageType.Success.ToString().ToLower();
                }
                else
                {
                    TempData["Message"] = errorMessage;
                    TempData["MessageType"] = MessageType.Error.ToString().ToLower();
                }

                TempData.Keep("Message");
                TempData.Keep("MessageType");
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "ParticipantFeedback", "SubmitFeedback", ex);
            }

            redirectUrl = "ViewFeedback?ScheduledTrainingId=" + ScheduledTrainingID.ToString() + "&CallingModule=" + CallingModule + "&TrainingId=" + TrainingID.ToString();//+ "&Message=" + TempData["Message"] + "&MessageType=" + TempData["MessageType"];

            Session["FeedbackCallingModule"] = null;

            return Json(redirectUrl, JsonRequestBehavior.AllowGet);//new { url = redirectUrl, message = TempData["Message"], messageType = TempData["MessageType"] }
        }

        /// <summary>
        /// save Online Feedback 
        /// </summary>
        /// <param name="arID"></param>
        /// <returns></returns>
         [Filters.MenuAccess()] 
        public ActionResult Feedback(int BCID)
        {
             int UserId;
             int bookedCandidateId;
             int.TryParse(Convert.ToString(BCID), out bookedCandidateId);
             int.TryParse(Convert.ToString(Session["UserId"]), out UserId);
            BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
            OnlineFeedbackModel objOnlineFeedbackModel = new OnlineFeedbackModel();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            VendorViewModel objVendorViewModel = new VendorViewModel();
            try
            {
                objOnlineFeedbackModel.IsTrainingComplete = objVendorViewModel.CheckOnlineTrainingComplete(bookedCandidateId);
                var IsTrainingComplete = objOnlineFeedbackModel.IsTrainingComplete;
                if (IsTrainingComplete)
                {
                    TempData["UnauthorizedAccess"] = Resources.TrainingResource.msgNotViewAsTrainingCompleted;
                    return RedirectToAction("Home", "Home");
                }
                objOnlineFeedbackModel.BookedCandidateID = bookedCandidateId;
                    objOnlineFeedbackModel.CreatedBy = UserId;
                 objOnlineFeedbackModel= objBookedCandidateViewModel.GetParticipantOnlineFeedback(objOnlineFeedbackModel);
                
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "ParticipantFeedback", "Feedback GET", ex);
            }

            return View(objOnlineFeedbackModel);
        }

       /// <summary>
        /// Submit Online Feedback
       /// </summary>
       /// <param name="jsonData"></param>
       /// <param name="IsSubmitted"></param>
       /// <param name="TrainingID"></param>
       /// <returns></returns>
        [HttpPost]
        public ActionResult Feedback(string jsonData, int IsSubmitted, int TrainingID,int BookedCandidateID)
        {
            BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
            JavaScriptSerializer jss = new JavaScriptSerializer();
            List<SubmitFeedbackModel> lstFeedback = new List<SubmitFeedbackModel>();
            int UserId, errorCode;
            string errorMessage, redirectUrl;
           int.TryParse(Session["UserId"].ToString(), out UserId);

            try
            {
                lstFeedback = jss.Deserialize<List<SubmitFeedbackModel>>(jsonData);

                objBookedCandidateViewModel.SaveCandidateOnlineFeedback(lstFeedback, TrainingID, Convert.ToBoolean(IsSubmitted), UserId, out errorCode, out errorMessage);
                if (errorCode.Equals(0))
                {
                    TempData["Message"] = errorMessage;
                    TempData["MessageType"] = MessageType.Success.ToString().ToLower();
                }
                else
                {
                    TempData["Message"] = errorMessage;
                    TempData["MessageType"] = MessageType.Error.ToString().ToLower();
                }

                TempData.Keep("Message");
                TempData.Keep("MessageType");
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "ParticipantFeedback", "SubmitFeedback", ex);
            }
            redirectUrl = Url.Action("StartTraining", "Vendor", new { BCID = BookedCandidateID, ISF = false});
            return Json(redirectUrl, JsonRequestBehavior.AllowGet);
        }

    }
}
