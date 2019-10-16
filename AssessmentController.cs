using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Models;
using CTMS.Common;
using CTMS.ViewModel;

namespace CTMS.Controllers
{
    public class AssessmentController : Controller
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

        #region AssignAssessment
        /// <summary>
        /// Get Assign Assessment detail based on TrainingID or TopicID
        /// </summary>
        /// <param name="TrainingID"></param>
        /// <param name="IsTopicAssessment"></param>
        /// <param name="TopicID"></param>
        /// <returns></returns>
         [Filters.MenuAccess()]
        public ActionResult AssignAssessment(int tID, bool isTa, int? tpID = null, bool? IsT = false)
        {
            SaveTrainingAssignAssessment objSaveTrainingAssignment = new SaveTrainingAssignAssessment();
            
            
            if (Session["Message"] != null && Session["MessageType"] != null)
            {
                Session["Message"] = null;
                Session["MessageType"] = null;
            }
            if (Session["SelectAssessmentSuccess"] != null)
            {
                objSaveTrainingAssignment.Message = Session["SelectAssessmentSuccess"].ToString();
                objSaveTrainingAssignment.MessageType = MessageType.Success.ToString().ToLower();
                Session["SelectAssessmentSuccess"] = null;
            }
            AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();
            TopicViewModel objTopicViewModel = new TopicViewModel();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            try
            {
                objSaveTrainingAssignment.CurrentPage = 1;
                objSaveTrainingAssignment.PageSize = CommonUtils.PageSize;

                objSaveTrainingAssignment.TotalPages = 0;
                objSaveTrainingAssignment.TrainingData = objTrainingViewModel.GetTrainingByTrainingID(tID);
                if (objSaveTrainingAssignment.TrainingData == null)
                {
                    objSaveTrainingAssignment.TrainingData = new SaveTrainingModel();
                }
                objSaveTrainingAssignment.PublishCount = objSaveTrainingAssignment.TrainingData.PublishCount;
                objSaveTrainingAssignment.IsTopicWiseAssessment = isTa;
                objSaveTrainingAssignment.IsFromTopicPage = IsT != null ? (bool)IsT : false;
                objSaveTrainingAssignment.CreatedBy = createdBy;
                Session["PublishCount"] = objSaveTrainingAssignment.PublishCount;
                if (objSaveTrainingAssignment.PublishCount > 0)
                {
                    objSaveTrainingAssignment.Message = Resources.TrainingResource.msgTrainingPublishNotification;
                    objSaveTrainingAssignment.MessageType = MessageType.Notice.ToString().ToLower();
                }
                if (objSaveTrainingAssignment.IsTopicWiseAssessment)
                {
                    List<TopicModel> lstTopicModel = objTopicViewModel.GetTopicsbyTrainingID(tID).ToList();
                    if (lstTopicModel != null && lstTopicModel.Count > 0)
                    {

                        objSaveTrainingAssignment.TopicID = tpID != null ? tpID : lstTopicModel[0].TopicID;
                        if (objSaveTrainingAssignment.IsFromTopicPage)
                        {
                            objSaveTrainingAssignment.TopicName = lstTopicModel.Where(t => t.TopicID == objSaveTrainingAssignment.TopicID).Select(t => t.TopicName).SingleOrDefault();
                        }
                        objSaveTrainingAssignment.MaterialTypeName = lstTopicModel.Where(t => t.TopicID == objSaveTrainingAssignment.TopicID).Select(t => t.MaterialTypeName).SingleOrDefault();
                        ViewBag.TopicList = new SelectList(lstTopicModel, "TopicID", "TopicName", objSaveTrainingAssignment.TopicID);
                       
                    }
                    
                }
                objSaveTrainingAssignment = objAssessmentViewModel.GetAssignAssessment(objSaveTrainingAssignment);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assessment", "AssignAssessment Get", ex);
            }
            return View(objSaveTrainingAssignment);
        }

        /// <summary>
        /// save detail of assign assessment if action type is saveassignassessment else if change topic than save detail if not exists.
        /// </summary>
        /// <param name="objSaveTrainingAssignment"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult AssignAssessment(SaveTrainingAssignAssessment objSaveTrainingAssignment)
        {
            AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();
            try
            {
                objSaveTrainingAssignment.Message = string.Empty;
                objSaveTrainingAssignment.MessageType = string.Empty;
                TopicViewModel objTopicViewModel = new TopicViewModel();
                switch (objSaveTrainingAssignment.ActionType)
                {
                    case "saveassignassessment":
                        string strAssessmentXml = CommonUtils.GetBulkXML(objSaveTrainingAssignment.AssignAssessments);
                        objSaveTrainingAssignment.IsActive = true;
                        objSaveTrainingAssignment.CreatedBy = createdBy;
                        objSaveTrainingAssignment.CreatedOn = DateTime.Now;
                        objAssessmentViewModel.InsertUpdateAssignAssessment(objSaveTrainingAssignment, strAssessmentXml);
                        break;
                    case "changetopic":
                        int topicId = 0;
                        int.TryParse(Convert.ToString(objSaveTrainingAssignment.TopicID),out topicId);
                        objSaveTrainingAssignment.CreatedBy = createdBy;
                        TopicModel objTopicModel = objTopicViewModel.GetTopicDetailbyTopicID(topicId);
                        if (objTopicModel != null)
                        {
                            objSaveTrainingAssignment.MaterialTypeName = objTopicModel.MaterialTypeName;
                            objSaveTrainingAssignment.TopicName = objTopicModel.TopicName;
                        }
                        objAssessmentViewModel.InsertAssignAssessmentDetail(objSaveTrainingAssignment);
                         break;

                }
                objSaveTrainingAssignment = objAssessmentViewModel.GetAssignAssessment(objSaveTrainingAssignment);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assessment", "AssignAssessment Post", ex);
            }
            return PartialView("_AssignAssessmentList", objSaveTrainingAssignment);
        }

        #endregion

        #region SelectAssessment
        /// <summary>
        /// Get Question list with selected records to assign.
        /// </summary>
        /// <param name="aaID"></param>
        /// <returns></returns>
         [Filters.MenuAccess()]
        public ActionResult SelectAssessment(int aadID, bool? IsT = false, int? aaID = null, int? qtID = null)
        {
            SelectAssessment objSelectAssessment = new SelectAssessment();
            objSelectAssessment.Message = string.Empty;
            objSelectAssessment.MessageType = string.Empty;
            try
            {
                if (Session["PublishCount"] != null && Convert.ToString(Session["PublishCount"]) != "")
                {
                    objSelectAssessment.PublishCount = Convert.ToInt32(Session["PublishCount"]);
                }
                if (objSelectAssessment.PublishCount > 0)
                {
                    objSelectAssessment.Message = Resources.TrainingResource.msgTrainingPublishNotification;
                    objSelectAssessment.MessageType = MessageType.Notice.ToString().ToLower();
                }
                AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();
                if (aadID <= 0)
                {
                    aadID = objAssessmentViewModel.InsertAssignAssessmentDetailByAAD((int)aaID, (int)qtID, createdBy);
                    if (aadID.Equals(0))
                    {
                        objSelectAssessment.Message = Resources.AssessmentResource.msgErrorMessage;
                        objSelectAssessment.MessageType = MessageType.Error.ToString().ToLower();
                    }

                }
                AssignAssessmentDetail objAssessmentDetail = aadID != 0 ? objAssessmentViewModel.GetAssignAssessmentDetailByID(aadID) : new AssignAssessmentDetail();

                objSelectAssessment.CurrentPage = 1;
                objSelectAssessment.PageSize = CommonUtils.PageSize;

                objSelectAssessment.TotalPages = 0;
                objSelectAssessment.FilterQuestionType = objAssessmentDetail.QuestionTypeID;
                objSelectAssessment.FilterQuestionTypeName = objAssessmentDetail.QuestionTypeName;
                objSelectAssessment.IsTopicWiseAssessment = objAssessmentDetail.IsTopicWiseAssessment;
                objSelectAssessment.SelectedTrainingID = objAssessmentDetail.TrainingID;
                objSelectAssessment.SelectedTopicID = objAssessmentDetail.TopicID;
                objSelectAssessment.AssignAssessmentDetailID = aadID;
                objSelectAssessment.QuestionCount = objAssessmentDetail.QuestionCount;
                objSelectAssessment.IsFromTopicPage = IsT != null ? (bool)IsT : false;
                objAssessmentViewModel.GetAllAssessment(objSelectAssessment);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assessment", "SelectAssessment GET", ex);
            }
            return View(objSelectAssessment);
        }

        /// <summary>
        /// search and save assessment question detail based on action type.
        /// </summary>
        /// <param name="objSelectAssessment"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult SelectAssessment(SelectAssessment objSelectAssessment)
        {

            try
            {
                objSelectAssessment.Message = string.Empty;
                objSelectAssessment.MessageType = string.Empty;
                AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();

                if (objSelectAssessment.ActionType != null)
                {
                    switch (objSelectAssessment.ActionType)
                    {
                        case "search": objSelectAssessment.CurrentPage = 1;
                            break;
                        case "saveassessment":
                            objSelectAssessment.IsActive = true;
                            objSelectAssessment.CreatedBy = createdBy;
                              objAssessmentViewModel.InsertUpdateAssignAssessmentQuestionDetail(objSelectAssessment);
                             string redirectUrl = String.Empty;
                             if (objSelectAssessment.IsTopicWiseAssessment)
                             {
                                 //check that is Page open from Topic page or Not.
                                 if (objSelectAssessment.IsFromTopicPage)
                                 {
                                     redirectUrl = Url.Action("AssignAssessment", "Assessment", new { tID = objSelectAssessment.SelectedTrainingID, isTa = objSelectAssessment.IsTopicWiseAssessment, tpID = objSelectAssessment.SelectedTopicID, IsT = objSelectAssessment.IsFromTopicPage });
                                 }
                                 else
                                 {
                                     redirectUrl = Url.Action("AssignAssessment", "Assessment", new { tID = objSelectAssessment.SelectedTrainingID, isTa = objSelectAssessment.IsTopicWiseAssessment, tpID = objSelectAssessment.SelectedTopicID });
                                 }
                             }
                             else
                             {
                                 redirectUrl = Url.Action("AssignAssessment", "Assessment", new { tID = objSelectAssessment.SelectedTrainingID, isTa = objSelectAssessment.IsTopicWiseAssessment });
                             }
                             if (objSelectAssessment.MessageType.ToLower() == MessageType.Success.ToString().ToLower())
                             {
                                 Session["SelectAssessmentSuccess"] = objSelectAssessment.Message;
                             }
                             return Json(new { url = redirectUrl, message = objSelectAssessment.Message, messageType = objSelectAssessment.MessageType });
                    }

                }
                objAssessmentViewModel.GetAllAssessment(objSelectAssessment);
                //select unselect checkbox based select checkbox list and unselect checkbox list on page change
                if (objSelectAssessment.ActionType == "pagechange")
                {
                    foreach (var assessment in objSelectAssessment.Assessments)
                    {
                        if (!String.IsNullOrEmpty(objSelectAssessment.SelectedQuestionList))
                        {
                            if(objSelectAssessment.SelectedQuestionList.Split(',').ToList().Contains(assessment.AssessmentID.ToString()))
                            {
                                assessment.IsSelected = true;
                            }
                        }
                        if (!String.IsNullOrEmpty(objSelectAssessment.UnSelectedQuestionList))
                         {
                            if (objSelectAssessment.UnSelectedQuestionList.Split(',').ToList().Contains(assessment.AssessmentID.ToString()))
                            {
                                assessment.IsSelected = false;
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assessment", "SelectAssessment Post", ex);
            }
            return PartialView("_SelectAssessmentList", objSelectAssessment);
        }
        #endregion

        #region OnlineAssessment
        /// <summary>
        /// Start Online Exam by Assessment Result ID
        /// </summary>
        /// <param name="bcId"></param>
        /// <param name="tId"></param>
        /// <param name="tpId"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult OnlineExam(int arID)
        {
            OnlineQuestionPaperModel objOnlineQuestionPaperModel = new OnlineQuestionPaperModel();
            OnlineAssessmentResultModel objOnlineAssessmentResultModel = new OnlineAssessmentResultModel();
            VendorViewModel objVendorViewModel = new VendorViewModel();
            AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();
            try
            {
                objOnlineQuestionPaperModel.AssessmentResultID = arID;
                objOnlineQuestionPaperModel.QuestionNo = 1;
                objOnlineQuestionPaperModel = objAssessmentViewModel.GetAssessmentDetailByAssessmentResultID(objOnlineQuestionPaperModel);
                objOnlineQuestionPaperModel.QuestionsStatus = objAssessmentViewModel.GetQuestionsStatusByAssessmentResultID(objOnlineQuestionPaperModel);
                Session.Timeout = CommonUtils.AssessmentTimeOut;

                objOnlineQuestionPaperModel.IsTrainingComplete = objVendorViewModel.CheckOnlineTrainingComplete(objOnlineQuestionPaperModel.BookedCandidateID);
                var IsTrainingComplete = objOnlineQuestionPaperModel.IsTrainingComplete;
                if (IsTrainingComplete)
                {
                    TempData["UnauthorizedAccess"] = Resources.TrainingResource.msgNotViewAsTrainingCompleted;
                    return RedirectToAction("Home", "Home");
                }

                if (objOnlineQuestionPaperModel.IsPass != null || DateTime.Compare(DateTime.Now, objOnlineQuestionPaperModel.EndDate) > 0)
                {
                    TempData["UnauthorizedAccess"] = Resources.TrainingResource.msgNotViewAsAttemptGivenOrTimeOver;
                    return RedirectToAction("Home", "Home");
                }

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assessment", "OnlineExam GET", ex);
            }
            return View(objOnlineQuestionPaperModel);
        }


        ///// <summary>
        ///// Start Online Exam by Assessment Result ID
        ///// </summary>
        ///// <param name="bcId"></param>
        ///// <param name="tId"></param>
        ///// <param name="tpId"></param>
        ///// <returns></returns>
        //public ActionResult OnlineExam(int arID)
        //{
        //    OnlineQuestionPaperModel objOnlineQuestionPaperModel = new OnlineQuestionPaperModel();
        //    OnlineAssessmentResultModel objOnlineAssessmentResultModel = new OnlineAssessmentResultModel();
        //    VendorViewModel objVendorViewModel = new VendorViewModel();
        //    AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();
        //    try
        //    {
        //        objOnlineQuestionPaperModel.AssessmentResultID = arID;
        //        objOnlineQuestionPaperModel.QuestionNo = 1;
        //        objOnlineQuestionPaperModel = objAssessmentViewModel.GetAssessmentDetailByAssessmentResultID(objOnlineQuestionPaperModel);
        //        objOnlineQuestionPaperModel.QuestionsStatus = objAssessmentViewModel.GetQuestionsStatusByAssessmentResultID(objOnlineQuestionPaperModel);
        //        Session.Timeout = CommonUtils.AssessmentTimeOut;

        //        objOnlineQuestionPaperModel.IsTrainingComplete = objVendorViewModel.CheckOnlineTrainingComplete(objOnlineQuestionPaperModel.BookedCandidateID);
        //        var IsTrainingComplete = objOnlineQuestionPaperModel.IsTrainingComplete;
        //        if (IsTrainingComplete)
        //        {
        //            TempData["UnauthorizedAccess"] = Resources.TrainingResource.msgNotViewAsTrainingCompleted;
        //            return RedirectToAction("Home", "Home");
        //        }

        //        if (objOnlineQuestionPaperModel.IsPass != null || DateTime.Compare(DateTime.Now, objOnlineQuestionPaperModel.EndDate) > 0)
        //        {
        //            TempData["UnauthorizedAccess"] = Resources.TrainingResource.msgNotViewAsAttemptGivenOrTimeOver;
        //            return RedirectToAction("Home", "Home");
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assessment", "OnlineExam GET", ex);
        //    }
        //    return PartialView(@"~/Views/Assessment/OnlineExam.cshtml",objOnlineQuestionPaperModel);
        //}

        /// <summary>
        /// Save Answer informatin in Previous , Next , save and save and finish click
        /// </summary>
        /// <param name="objOnlineAssessmentResultModel"></param>
        /// <returns></returns>
         [HttpPost]
        public ActionResult OnlineExam(OnlineQuestionPaperModel objOnlineQuestionPaperModel)
        {
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            
            AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();
            try
            {
                if (objOnlineQuestionPaperModel != null && !String.IsNullOrEmpty(objOnlineQuestionPaperModel.ActionType))
                {
                    objOnlineQuestionPaperModel.IsActive = true;
                    objOnlineQuestionPaperModel.CreatedBy = createdBy;
                    int qNo = objOnlineQuestionPaperModel.QuestionNo;
                    string actionType = objOnlineQuestionPaperModel.ActionType.ToLower();
                    switch (objOnlineQuestionPaperModel.ActionType.ToLower())
                    {
                        case "previous":
                            
                                qNo = objOnlineQuestionPaperModel.QuestionNo - 1;
                           
                            break;
                        case "next":
                                qNo = objOnlineQuestionPaperModel.QuestionNo + 1;
                            
                            break;
                        case "changequestion":
                                  qNo = objOnlineQuestionPaperModel.ChangeQuestionNo;
                            break;
                    }
                    objAssessmentViewModel.UpdateOnlineAssessmentAnswer(objOnlineQuestionPaperModel);
                    objOnlineQuestionPaperModel.QuestionNo = qNo;
                    objOnlineQuestionPaperModel = objAssessmentViewModel.GetAssessmentDetailByAssessmentResultID(objOnlineQuestionPaperModel);
                    objOnlineQuestionPaperModel.QuestionsStatus = objAssessmentViewModel.GetQuestionsStatusByAssessmentResultID(objOnlineQuestionPaperModel);
                    if (String.Compare(actionType, "submit", true) == 0)
                    {
                        objAssessmentViewModel.SubmitOnlineAssessmentResult(objOnlineQuestionPaperModel);
                        string redirectUrl = Url.Action("AssessmentResult", "Assessment", new { arID=objOnlineQuestionPaperModel.AssessmentResultID});
                        return Json(new { url = redirectUrl});
                    }
                }
                
             }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assessment", "OnlineExam Post", ex);
            }
            return PartialView("_OnlineExamDetail",objOnlineQuestionPaperModel);
        }


        /// <summary>
        /// Show Assessment Result by Assessment Result ID
        /// </summary>
        /// <param name="arID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
         public ActionResult AssessmentResult(int arID)
         {
             OnlineAssessmentResult objOnlineAssessmentResult = new OnlineAssessmentResult();
             AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();
             VendorViewModel objVendorViewModel = new VendorViewModel();
             try
             {
                 objOnlineAssessmentResult.AssessmentResultID = arID;
                 objOnlineAssessmentResult = objAssessmentViewModel.GetAssessmentResultByAssessmentResultID(objOnlineAssessmentResult);
                 if (objOnlineAssessmentResult != null && objOnlineAssessmentResult.AssessmentResultID > 0)
                 {
                     objOnlineAssessmentResult.IsTrainingComplete = objVendorViewModel.CheckOnlineTrainingComplete(objOnlineAssessmentResult.BookedCandidateID);
                     var IsTrainingComplete = objOnlineAssessmentResult.IsTrainingComplete;
                     if (IsTrainingComplete)
                     {
                         TempData["UnauthorizedAccess"] = Resources.TrainingResource.msgNotViewAsTrainingCompleted;
                         return RedirectToAction("Home", "Home");
                     }
                     Decimal requiredMarks = (objOnlineAssessmentResult.TotalMarks * objOnlineAssessmentResult.RequiredPassingPercentage) / 100;
                     objOnlineAssessmentResult.RequiredMarks = requiredMarks;
                     objOnlineAssessmentResult.ObtainedPercentage = (objOnlineAssessmentResult.MarkOptained / objOnlineAssessmentResult.TotalMarks) * 100;
                     objOnlineAssessmentResult.CandidateName = Convert.ToString(Session["UserName"]);
                     objOnlineAssessmentResult.CurrentDate = DateTime.Now.ToString(CommonUtils.ShortDateFormat);
                     objOnlineAssessmentResult.AssessmentDuration = Common.CommonUtils.GetActualTrainingDuration((Int32)(objOnlineAssessmentResult.EndDate - objOnlineAssessmentResult.StartDate).TotalMinutes);
                 }
             }
             catch (Exception ex)
             {
                  Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assessment", "AssessmentResult GET", ex);
             }
             return View(objOnlineAssessmentResult);
         }
        #endregion
    }
}
