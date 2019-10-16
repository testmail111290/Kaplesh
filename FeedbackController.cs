using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Common;
using CTMS.ViewModel;
using CTMS.Models;

namespace CTMS.Controllers
{
    public class FeedbackController : Controller
    {


        #region Properties
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
        #endregion

        CommonUtils objCommonUtilError = new CommonUtils();
        //
        // GET: /Feedback/
        #region AssignFeedback
        /// <summary>
        /// Assign Feedback to Training based on Training ID
        /// </summary>
        /// <param name="TrainingID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult AssignFeedback(int tID)
        {
            AssignFeedbackDetail  objAssignFeedbackDetail = new AssignFeedbackDetail();
           
         //   AssignFeedbackd objSaveTrainingAssignment = new SaveTrainingAssignAssessment();
            if (Session["Message"] != null && Session["MessageType"] != null)
            {
                Session["Message"] = null;
                Session["MessageType"] = null;
            }
           
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            FeedbackViewModel objFeedbackViewModel = new FeedbackViewModel();
            try
            {
                objAssignFeedbackDetail.CurrentPage = 1;
                objAssignFeedbackDetail.PageSize = CommonUtils.PageSize;

                objAssignFeedbackDetail.TotalPages = 0;
                objAssignFeedbackDetail.TrainingData = objTrainingViewModel.GetTrainingByTrainingID(tID);
                


                if (objAssignFeedbackDetail.TrainingData == null)
                {
                    objAssignFeedbackDetail.TrainingData = new SaveTrainingModel();
                }
                objAssignFeedbackDetail.PublishCount = objAssignFeedbackDetail.TrainingData.PublishCount;
                if (objAssignFeedbackDetail.PublishCount > 0)
                {
                    objAssignFeedbackDetail.Message = Resources.TrainingResource.msgTrainingPublishNotification;
                    objAssignFeedbackDetail.MessageType = MessageType.Notice.ToString().ToLower();
                }
                objFeedbackViewModel.GetAssignFeedback(objAssignFeedbackDetail);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Feedback", "AssignFeedback Get", ex);
            }
            return View(objAssignFeedbackDetail);
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult AssignFeedback(AssignFeedbackDetail objAssignFeedbackDetail)
        {
            try
            {
                objAssignFeedbackDetail.Message = string.Empty;
                objAssignFeedbackDetail.MessageType = string.Empty;
                FeedbackViewModel objFeedbackViewModel = new FeedbackViewModel();
                if (objAssignFeedbackDetail.ActionType != null)
                {
                    switch (objAssignFeedbackDetail.ActionType)
                    {
                        case "savefeedback":
                            string redirectUrl = String.Empty;
                            objAssignFeedbackDetail.IsActive = true;
                            objAssignFeedbackDetail.CreatedBy = createdBy;
                            string strAssignFeedbackXML = CommonUtils.GetBulkXML(objAssignFeedbackDetail.Feedbacks);
                            objFeedbackViewModel.InsertUpdateAssignFeedback(objAssignFeedbackDetail, strAssignFeedbackXML);
                            break;
                            //if (objAssignFeedbackDetail.MessageType.ToLower() == MessageType.Success.ToString().ToLower())
                            // {
                            //     Session["AssignFeedbackSuccess"] = objAssignFeedbackDetail.Message;
                            // }
                            //redirectUrl = Url.Action("SaveTraining", "Training", new { TrainingID = objAssignFeedbackDetail.TrainingData.TrainingID });
                            //return Json(new { url = redirectUrl, message = objAssignFeedbackDetail.Message, messageType = objAssignFeedbackDetail.MessageType });
                    }
                }
                objFeedbackViewModel.GetAssignFeedback(objAssignFeedbackDetail);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Feedback", "AssignFeedback Post", ex);
            }
            return PartialView("_AssignFeedbackList", objAssignFeedbackDetail);
        }
        #endregion

    }
}
