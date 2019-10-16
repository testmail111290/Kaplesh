using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Models;
using CTMS.Common;
using CTMS.ViewModel;
using System.Configuration;
using CTMS.Resources;
/*----------------------------------
 Created By :- Vishal Gupta
 Date:- 20 May  2014
 Desc. :- Vendor Controller
 ---------------------------------*/
namespace CTMS.Controllers
{


    public class VendorController : Controller
    {


        #region [Properties]

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
        /// <summary>
        /// Return message 
        /// </summary>
        public object strRetMessage { get; set; }

        /// <summary>
        /// Message 
        /// </summary>
        public IFormatProvider strMessage { get; set; }

        //common utils class for common operation.
        CommonUtils objCommonUtilError = new CommonUtils();
        //Object of Topic View Model where all CRUD operation perform. 
        VendorViewModel objVendorViewModel = new VendorViewModel();

        // Name of default file.
        string DefaultFileinZip = ConfigurationManager.AppSettings["DefaultFileinZip"].ToString();

        // Path of  folder to store zip extract file. 
        string TrainingMaterialDirectory = ConfigurationManager.AppSettings["TrainingMaterialDirectory"].ToString();

        #endregion

        //
        // GET: /Vendor/
        /// <summary>
        /// Load Vendor pending training list
        /// </summary>
        /// <param name="createdBy"></param>
        /// <returns></returns>
        /// 
        [Filters.MenuAccess()]
        public ActionResult VendorPendingTraining()
        {
            VendorPendingTraining objVendorPendingTraining = new VendorPendingTraining();
            try
            {
                FillTrainingCategory();
                //if (objVendorPendingTraining.ActionType != null && objVendorPendingTraining.ActionType == "showDetails")
                //{
                //    if (objVendorPendingTraining.ActionType == "showDetails")
                //    {
                //        objVendorPendingTraining.SelectedTraining = objVendorPendingTraining.lstPendingTraining.Where(o => o.TrainingID == objVendorPendingTraining.TrainingDetailID).FirstOrDefault();
                //    }
                //    return PartialView("PendingTrainingList", objVendorPendingTraining);
                //}
                //else
                //{

                objVendorPendingTraining.CurrentPage = 1;
                objVendorPendingTraining.PageSize = CommonUtils.PageSize;

                objVendorPendingTraining.TotalPages = 0;
                objVendorPendingTraining = objVendorViewModel.GetPendingTrainingbyCandID(createdBy, objVendorPendingTraining, true);
                objVendorPendingTraining.CandidateID = createdBy;
                //  }

            }
            catch (Exception ex)
            {
                TempData["Message"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "VendorPendingTraining Get", ex);

            }
            return View("VendorPendingTraining", objVendorPendingTraining);

        }

        /// <summary>
        /// Show pending Training List 
        /// </summary>
        /// <param name="objVendorPendingTraining"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        public ActionResult VendorPendingTraining(VendorPendingTraining objVendorPendingTraining)
        {
            try
            {
                objVendorPendingTraining = objVendorViewModel.GetPendingTrainingbyCandID(objVendorPendingTraining.CandidateID, objVendorPendingTraining, true);
                objVendorPendingTraining.SelectedTraining = null;
                if (objVendorPendingTraining.ActionType == "showDetails")
                {
                    ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
                    //objVendorPendingTraining.SelectedTraining = objVendorPendingTraining.lstPendingTraining.Where(o => o.TrainingID == objVendorPendingTraining.TrainingDetailID).FirstOrDefault();
                    // objVendorViewModel.GetTrainingsForCandidate(objVendorPendingTraining);
                    objVendorPendingTraining.SelectedTraining = objVendorPendingTraining.lstPendingTraining.Where(o => o.ScheduledTrainingID == objVendorPendingTraining.SelSechduleID).FirstOrDefault();
                    if (objVendorPendingTraining.SelectedTraining != null && objVendorPendingTraining.SelectedTraining.ScheduledTrainingID > 0)
                    {
                        List<ProgramTrainingMappingDetailModel> lstProgramTrainings = objScheduledTrainingViewModel.GetProgramTrainingDetailByStID(objVendorPendingTraining.SelectedTraining.ScheduledTrainingID).ToList();
                        objVendorPendingTraining.SelectedTraining.ProgramTrainings = lstProgramTrainings;
                    }
                    return PartialView("PendingTrainingList", objVendorPendingTraining);
                }

            }
            catch (Exception ex)
            {
                TempData["Message"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "VendorPendingTraining Post", ex);

            }
            return PartialView("PendingTrainingList", objVendorPendingTraining);
        }


        #region MY Acknowledgement
        //Get Acknowledgement details
        [Filters.MenuAccess()]
        public ActionResult MyAcknowledgement()
        {
            MyAcknowledgementModel objMyAcknowledgementModel = new MyAcknowledgementModel();
            try
            {
                FillTrainingCategory();
                objMyAcknowledgementModel.CurrentPage = 1;
                objMyAcknowledgementModel.PageSize = CommonUtils.PageSize;

                objMyAcknowledgementModel.TotalPages = 0;
                objMyAcknowledgementModel.CreatedBy = createdBy;
                objVendorViewModel.GetAckTrainingbyCandID(objMyAcknowledgementModel);
                FillCategory(objMyAcknowledgementModel);
                GetFunctions(objMyAcknowledgementModel.FilterCategory.ToString());

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "MyAcknowledgement GET", ex);
            }
            return View(objMyAcknowledgementModel);
        }

        //Get Acknowledgement details
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult MyAcknowledgement(MyAcknowledgementModel objMyAcknowledgementModel)
        {
            try
            {
                ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
                objMyAcknowledgementModel.CreatedBy = createdBy;
                if (objMyAcknowledgementModel.ActionType != null)
                {
                    switch (objMyAcknowledgementModel.ActionType)
                    {
                        case "search": objMyAcknowledgementModel.CurrentPage = 1;
                            break;
                        case "showDetails":
                            objVendorViewModel.GetAckTrainingbyCandID(objMyAcknowledgementModel);
                            objMyAcknowledgementModel.SelectedTraining = objMyAcknowledgementModel.lstTraining.Where(o => o.ScheduledTrainingID == objMyAcknowledgementModel.SelectedID).FirstOrDefault();
                            if (objMyAcknowledgementModel.SelectedTraining != null && objMyAcknowledgementModel.SelectedTraining.ScheduledTrainingID > 0)
                            {
                                List<ProgramTrainingMappingDetailModel> lstProgramTrainings = objScheduledTrainingViewModel.GetProgramTrainingDetailByStID(objMyAcknowledgementModel.SelectedTraining.ScheduledTrainingID).ToList();
                                objMyAcknowledgementModel.SelectedTraining.ProgramTrainings = lstProgramTrainings;
                            }
                            return PartialView("AcknowledgementTrainingList", objMyAcknowledgementModel);
                    }
                }

                objVendorViewModel.GetAckTrainingbyCandID(objMyAcknowledgementModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "MyAcknowledgement Post", ex);
            }
            return PartialView("AcknowledgementTrainingList", objMyAcknowledgementModel);
        }


        /// <summary>
        /// Fill  Category dropdown values currently its label as bussiness function.
        /// </summary>
        /// <param name="objSaveTrainingModel"></param>
        public void FillCategory(MyAcknowledgementModel objMyAcknowledgementModel)
        {
            try
            {
                TrainingViewModel objTrainingViewModel = new TrainingViewModel();
                List<CategoryModel> lstCategory = new List<CategoryModel>();
                IList<CategoryModel> Categories = objTrainingViewModel.GetAllActiveCategory();

                lstCategory.Add(new CategoryModel { CategoryID = 0, CategoryName = "All" });
                for (int i = 0; i < Categories.Count(); i++)
                {
                    lstCategory.Add(new CategoryModel { CategoryID = Categories[i].CategoryID, CategoryName = Categories[i].CategoryName });
                }
                ViewBag.CategoryList = new SelectList(lstCategory, "CategoryID", "CategoryName", objMyAcknowledgementModel.FilterCategory);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "FillCategory", ex);
            }
        }
        /// <summary>
        /// Return Training category.
        /// </summary>
        public void FillTrainingCategory()
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            IList<TrainingCategoryModel> lstTrainingCategoryModel = objMasterViewModel.GetAllTrainingCategory();
            List<TrainingCategoryModel> lstTrainingCategory = new List<TrainingCategoryModel>();

            lstTrainingCategory.Insert(0, new TrainingCategoryModel { TrainingCategoryId = 0, TrainingCategoryName = "All" });
            for (int i = 0; i < lstTrainingCategoryModel.Count(); i++)
            {
                lstTrainingCategory.Add(new TrainingCategoryModel { TrainingCategoryId = lstTrainingCategoryModel[i].TrainingCategoryId, TrainingCategoryName = lstTrainingCategoryModel[i].TrainingCategoryName });
            }
            ViewBag.TrainingCategoryList = new SelectList(lstTrainingCategory, "TrainingCategoryId", "TrainingCategoryName");
        }

        /// <summary>
        /// Currently its label as Job Role.
        /// </summary>
        /// <param name="CategoryIds"></param>
        public void GetFunctions(string CategoryIds)
        {
            //  int createdBy = Convert.ToInt32(Session["createdBy"]);
            List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
            try
            {
                lstFunctionCategoryModel.Add(new FunctionCategoryModel { FunctionID = 0, FunctionName = "All" });

                if (!String.IsNullOrEmpty(CategoryIds) && CategoryIds != "null")
                {
                    TrainingViewModel objTrainingViewModel = new TrainingViewModel();
                    var lstFunctions = objTrainingViewModel.GetFunctionsByCategoryIds(CategoryIds);
                    for (int i = 0; i < lstFunctions.Count(); i++)
                    {
                        lstFunctionCategoryModel.Add(new FunctionCategoryModel() { FunctionID = lstFunctions[i].FunctionID, FunctionName = lstFunctions[i].FunctionName });
                    }

                    ViewBag.FunctionList = new SelectList(lstFunctionCategoryModel, "FunctionID", "FunctionName", null);
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "GetFunctions", ex);
            }
        }


        /// <summary>
        /// Fill function by category id
        /// </summary>
        /// <param name="CategoryIds"></param>
        /// <returns></returns>
        public ActionResult FillFunctions(string CategoryIds)
        {
            try
            {

                List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
                TrainingViewModel objTrainingViewModel = new TrainingViewModel(); if (!String.IsNullOrEmpty(CategoryIds) && CategoryIds != "null")
                {
                    lstFunctionCategoryModel = objTrainingViewModel.GetFunctionsByCategoryIds(CategoryIds);
                    var classesData = lstFunctionCategoryModel.Select(m => new SelectListItem()
                    {
                        Text = m.FunctionName,
                        Value = m.FunctionID.ToString(),
                    });

                    return Json(classesData, JsonRequestBehavior.AllowGet);
                }



            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "FillFunctions", ex);
            }
            return Json("", JsonRequestBehavior.AllowGet);


        }

        #endregion MY ACKNOWLEDGEMENT


        #region ViewTraining
        [Filters.MenuAccess()]
        public ActionResult ViewTraining()
        {
            CandidateTraining objCandidateTraining = new CandidateTraining();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            try
            {
                FillTrainingCategory();
                objCandidateTraining.CurrentPage = 1;
                objCandidateTraining.PageSize = CommonUtils.PageSize;
                objCandidateTraining.TotalPages = 0;
                List<CategoryModel> lstCategory = objTrainingViewModel.GetAllActiveCategory(true).ToList();
                ViewBag.CategoryList = new SelectList(lstCategory, "CategoryID", "CategoryName", objCandidateTraining.FilterCategory.ToString());
                List<FunctionModel> lstFunction = new List<FunctionModel>();
                FunctionModel objFunction = new FunctionModel();
                objFunction.FunctionID = 0;
                objFunction.FunctionName = "All";
                lstFunction.Insert(0, objFunction);
                ViewBag.FunctionList = new SelectList(lstFunction, "FunctionID", "FunctionName", objCandidateTraining.FilterFunction.ToString());
                objCandidateTraining.CreatedBy = createdBy;
                objVendorViewModel.GetTrainingsForCandidate(objCandidateTraining);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "ViewTraining Get", ex);
            }
            return View(objCandidateTraining);
        }
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult ViewTraining(CandidateTraining objCandidateTraining)
        {
            ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
            objCandidateTraining.Message = objCandidateTraining.MessageType = String.Empty;
            objCandidateTraining.CreatedBy = createdBy;

            try
            {
                if (objCandidateTraining.ActionType != null)
                {
                    switch (objCandidateTraining.ActionType)
                    {
                        case "search": objCandidateTraining.CurrentPage = 1;
                            break;
                        case "showDetails":
                            objVendorViewModel.GetTrainingsForCandidate(objCandidateTraining);
                            objCandidateTraining.SelectedTraining = objCandidateTraining.Trainings.Where(o => o.ScheduledTrainingID == objCandidateTraining.SelectedID).FirstOrDefault();
                            if (objCandidateTraining.SelectedTraining != null && objCandidateTraining.SelectedTraining.ScheduledTrainingID > 0)
                            {
                                List<ProgramTrainingMappingDetailModel> lstProgramTrainings = objScheduledTrainingViewModel.GetProgramTrainingDetailByStID(objCandidateTraining.SelectedTraining.ScheduledTrainingID).ToList();
                                objCandidateTraining.SelectedTraining.ProgramTrainings = lstProgramTrainings;
                            }
                            return PartialView("_TrainingList", objCandidateTraining);
                        case "sendrequest":
                            objCandidateTraining.CreatedBy = createdBy;
                            objCandidateTraining.IsActive = true;
                            objCandidateTraining.CandidateName = Session["UserName"].ToString();
                            objVendorViewModel.InsertCandidateRequest(objCandidateTraining);
                            break;
                        case "withdraw":
                            objCandidateTraining.CreatedBy = createdBy;
                            objCandidateTraining.IsActive = true;
                            objCandidateTraining.CandidateName = Session["UserName"].ToString();
                            objVendorViewModel.WithdrawCandidateRequest(objCandidateTraining);
                            break;
                    }
                }
                objVendorViewModel.GetTrainingsForCandidate(objCandidateTraining);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "ViewTraining Post", ex);
            }
            return PartialView("_TrainingList", objCandidateTraining);
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]

        public ActionResult GetFunctionsByCategoryID(int CategoryID)
        {
            try
            {
                List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
                lstFunctionCategoryModel = objVendorViewModel.GetFunctionsByCategoryIds(Convert.ToString(CategoryID));
                SelectList objFunctions = new SelectList(lstFunctionCategoryModel, "FunctionID", "FunctionName", 0);
                return Json(objFunctions);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(Convert.ToString(createdBy), "Vendor", "GetFunctionsByCategoryID", ex);
                return Json(ex);
            }
        }

        #endregion

        #region Search online training
        [Filters.MenuAccess()]
        public ActionResult SearchOnlineTraining()
        {
            CandidateTraining objCandidateTraining = new CandidateTraining();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            try
            {
                FillTrainingCategory();
                objCandidateTraining.CurrentPage = 1;
                objCandidateTraining.PageSize = CommonUtils.PageSize;
                objCandidateTraining.TotalPages = 0;
                List<CategoryModel> lstCategory = objTrainingViewModel.GetAllActiveCategory(true).ToList();
                ViewBag.CategoryList = new SelectList(lstCategory, "CategoryID", "CategoryName", objCandidateTraining.FilterCategory.ToString());
                List<FunctionModel> lstFunction = new List<FunctionModel>();
                FunctionModel objFunction = new FunctionModel();
                objFunction.FunctionID = 0;
                objFunction.FunctionName = "All";
                lstFunction.Insert(0, objFunction);
                ViewBag.FunctionList = new SelectList(lstFunction, "FunctionID", "FunctionName", objCandidateTraining.FilterFunction.ToString());
                objCandidateTraining.CreatedBy = createdBy;
                objVendorViewModel.GetOnlineTrainingListForCandidate(objCandidateTraining);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "SearchOnlineTraining Get", ex);
            }
            return View(objCandidateTraining);
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult SearchOnlineTraining(CandidateTraining objCandidateTraining)
        {
            TrainingViewModel objScheduledTrainingViewModel = new TrainingViewModel();
            objCandidateTraining.Message = objCandidateTraining.MessageType = String.Empty;
            objCandidateTraining.CreatedBy = createdBy;

            try
            {
                if (objCandidateTraining.ActionType != null)
                {
                    switch (objCandidateTraining.ActionType)
                    {
                        case "search": objCandidateTraining.CurrentPage = 1;
                            break;

                        case "sendrequest":
                            objCandidateTraining.CreatedBy = createdBy;
                            objCandidateTraining.IsActive = true;
                            
                            objCandidateTraining.CandidateName = Session["UserName"].ToString();
                            objVendorViewModel.InsertOnlineCandidateRequest(objCandidateTraining);
                            break;
                        //case "selfrequest":
                        //    objCandidateTraining.CreatedBy = createdBy;
                        //    objCandidateTraining.IsActive = true;

                        //    objCandidateTraining.CandidateName = Session["UserName"].ToString();
                        //    objVendorViewModel.InsertSelfCandidateRequest(objCandidateTraining);
                        //    break;
                        case "completetraining":
                            objCandidateTraining.CreatedBy = createdBy;
                            objCandidateTraining = objVendorViewModel.CompleteOnlineTraining(objCandidateTraining);
                            ////Send notification to program manager and manager for complete training.
                            ////Send notification when Program completed.
                            //if (objCandidateTraining.IsProgramComplete == true)
                            //{
                            //    NotificationUtil.SendTrainerCompleteTrainingNotification(objCandidateTraining);
                            //    NotificationUtil.SendTrainerCompleteProgramNotification(objCandidateTraining);
                            //}
                            ////Send notification when training completed.
                            //else
                            //{
                            //    NotificationUtil.SendTrainerCompleteTrainingNotification(objCandidateTraining);
                            //}

                            break;
                        case "showDetails":
                            objVendorViewModel.GetOnlineTrainingListForCandidate(objCandidateTraining);
                            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
                            objCandidateTraining.SelectOnlineTraining = objCandidateTraining.OnlineTraining.Where(o => o.TrainingID == objCandidateTraining.SelectedID).FirstOrDefault();
                            if (objCandidateTraining.SelectOnlineTraining != null)
                            {
                                objCandidateTraining.SelectOnlineTraining.OnlineAssessmentDetails = objTrainingViewModel.GetOnlineAssessmentDetails(objCandidateTraining.SelectOnlineTraining).ToList();
                            }
                            return PartialView("_OnlineTrainingList", objCandidateTraining);

                    }
                }
                objVendorViewModel.GetOnlineTrainingListForCandidate(objCandidateTraining);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "SearchOnlineTraining Post", ex);
            }
            return PartialView("_OnlineTrainingList", objCandidateTraining);
        }




        public ActionResult StartTrainingTime()
        {
            //show time when your training started
            var TrainingStartTime = string.Format(TrainerResource.msgTrainingStart, System.DateTime.Now.ToString());
            return Json(TrainingStartTime, JsonRequestBehavior.AllowGet);

        }
        /// <summary>
        /// self registration training 
        /// </summary>
        /// <param name="TRID"></param>
        /// <returns></returns>


        public JsonResult SelfRequest(int TRID)
        {
            CandidateTraining objCandidateTraining = new CandidateTraining();
            try
            {
                objCandidateTraining.CreatedBy = createdBy;
                objCandidateTraining.IsActive = true;
                objCandidateTraining.SelectedID = TRID;
                objCandidateTraining.CandidateName = Session["UserName"].ToString();
                objVendorViewModel.InsertSelfCandidateRequest(objCandidateTraining);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "SelfRequest", ex);
            }
            return Json(new { BCID = objCandidateTraining.BookedCandidateID }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// Start training topic wise 
        /// </summary>
        /// <param name="ScheduledTrainingId"></param>
        /// <returns></returns>
        /// 
        [Filters.MenuAccess()]

        public ActionResult StartTraining(int BCID, Boolean ISF)
        {
            StartTrainingModel objStartTrainingModel = new StartTrainingModel();

            try
            {

                //Added Start time in training study database table 
                //Training start time save into database
                objStartTrainingModel.IsTrainingComplete = objVendorViewModel.CheckOnlineTrainingComplete(BCID);
                var IsTrainingComplete = objStartTrainingModel.IsTrainingComplete;
                if (!IsTrainingComplete)
                {
                    objStartTrainingModel = SaveTrainingTime(BCID, null, null, ISF);

                }
                objStartTrainingModel.BookedCandidateID = BCID;
                objStartTrainingModel = objVendorViewModel.GetOnlineTrainingWithSpendTimeByID(objStartTrainingModel);
                List<TopicModel> lstTopicModel = objVendorViewModel.GetTopicWithSpendTimeByBookedCandidateID(objStartTrainingModel).ToList();
                objStartTrainingModel.lstTopicModel = lstTopicModel.OrderBy(x => x.TopicOrderNo).ToList();
                ViewBag.TimerTickValue = CommonUtils.TimerTickValue;
                if (IsTrainingComplete)
                {
                    objStartTrainingModel.Message = Resources.TrainingResource.msgTrainingAlreadyComplete.ToString();
                    objStartTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                    objStartTrainingModel.IsTrainingComplete = IsTrainingComplete;

                }




                SetContent(objStartTrainingModel);


                //if (item != null)
                //{
                //       //topic is also start here
                //        objStartTrainingModel.TopicID = item.TopicID;
                //        objStartTrainingModel.TopicStatus = item.TopicStatus;
                //        objStartTrainingModel.BookedCandidateID = BCID;
                //        objStartTrainingModel.CreatedBy = createdBy;
                //        objStartTrainingModel = objVendorViewModel.GetTopicWithSpendTimeByBookedCandidateIDs(objStartTrainingModel);
                //        objStartTrainingModel.TopicID = item.TopicID;
                //        objStartTrainingModel.BookedCandidateID = BCID;
                //        objStartTrainingModel.TopicStatus = item.TopicStatus;
                //        objStartTrainingModel.CreatedBy = createdBy;
                //        objStartTrainingModel.IsFinished = false;
                //        objStartTrainingModel.StartDateTime = null; //null;

                //        objStartTrainingModel.EndDateTime = null;
                //        if (objStartTrainingModel.IsTrainingComplete == false)
                //        {
                //            if (item.TopicStatusName != "completed")
                //                objStartTrainingModel = objVendorViewModel.InsertUpdateOnlineTopicTime(objStartTrainingModel);
                //        }
                //        else
                //            objStartTrainingModel.IsFinished = objStartTrainingModel.IsTrainingComplete;
                //        //  TopicStart(Convert.ToInt32(item.TopicID), Convert.ToInt32(item.BookedCandidateID), Convert.ToInt32(item.TopicStatus), 0, false, objStartTrainingModel.IsTrainingComplete, item.TopicStatusName);

                //        objStartTrainingModel.TopicID = 0;
                //        lstTopicModel = objVendorViewModel.GetTopicWithSpendTimeByBookedCandidateID(objStartTrainingModel).ToList();
                //        objStartTrainingModel.lstTopicModel = lstTopicModel.OrderBy(x => x.TopicOrderNo).ToList();


                //}
                //if (objStartTrainingModel.IsAssessmentRequired)
                //{
                //    if (objStartTrainingModel.IsTopicWiseAssessment)
                //    {
                //        foreach (var obj in objStartTrainingModel.lstTopicModel)
                //        {
                //            if (!(obj.TopicNoOfAttempts < obj.TopicMaxAttempts) && obj.TopicStatusName.ToLower() == "completed" && !obj.IsTopicPass)
                //            {
                //                objStartTrainingModel.Message = Resources.VendorResource.msgExceedAttempt;
                //                objStartTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                //                break;
                //            }
                //            //else if (obj.TopicStatusName.ToLower() == "completed" && !obj.IsTopicPass)
                //            //{
                //            //    //if first topic is finish then start assessment of this topic 
                //            //    objStartTrainingModel.ActionType = "topicassessment";
                //            //}
                //        }
                //    }
                //    else
                //    {
                //        if (!(objStartTrainingModel.TrainingNoOfAttempts < objStartTrainingModel.TrainingMaxAttempts) && (!objStartTrainingModel.IsPass))
                //        {
                //            objStartTrainingModel.Message = Resources.VendorResource.msgExceedAttempt;
                //            objStartTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                //        }
                //    }
                //}







                //objStartTrainingModel.TopicID = 0;
                //lstTopicModel = objVendorViewModel.GetTopicWithSpendTimeByBookedCandidateID(objStartTrainingModel).ToList();
                //objStartTrainingModel.lstTopicModel = lstTopicModel.OrderBy(x => x.TopicOrderNo).ToList();
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "StartTraining Post", ex);
            }
            return View("StartTraining", objStartTrainingModel);
        }



        //[HttpPost]
        //[Filters.Authorized()]
        //[ValidateInput(false)]
        //public ActionResult StartTraining(StartTrainingModel objstartTrainingModel)
        //{
        //    if (objstartTrainingModel != null && objstartTrainingModel.ActionType == "starttopic")
        //    {
        //        objstartTrainingModel = TopicStartModel(objstartTrainingModel);
        //        return PartialView("_ViewTopic", objstartTrainingModel);
        //    }

        //    return View
        //        ();
        //}

        public void SetContent(StartTrainingModel objStartTrainingModel)
        {
            foreach (var item in objStartTrainingModel.lstTopicModel)
            {
                if (item != null)
                {
                    //
                    if (item.TopicStatusName.ToLower() == "not started" || item.TopicStatusName.ToLower() == "in progress")
                    {
                        objStartTrainingModel.SelectedTopicID = item.TopicID;
                        objStartTrainingModel.SelectedTopicStatus = item.TopicStatus;
                        objStartTrainingModel.ActionType = "topicstart";
                        break;
                    }
                    else if (objStartTrainingModel.IsAssessmentRequired)
                    {
                        if (item.TopicStatusName.ToLower() == "completed" && objStartTrainingModel.IsTopicWiseAssessment && (item.IsTopicPass == null || item.IsTopicPass == false))
                        {
                            if (item.RemainTopAsesmntTime != null && ((item.RemainTopAsesmntTime <= 0 && item.TopicNoOfAttempts > item.TopicMaxAttempts && item.IsTopicPass != true) || (item.RemainTopAsesmntTime > 0 && item.IsTopicPass != null && item.TopicNoOfAttempts > item.TopicMaxAttempts)))
                            //if (!(item.TopicNoOfAttempts < item.TopicMaxAttempts) && (objStartTrainingModel.RemainTrnAsesmntTime == null || objStartTrainingModel.RemainTrnAsesmntTime <= 0))
                            {
                                objStartTrainingModel.Message = Resources.VendorResource.msgExceedAttempt;
                                objStartTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                               item.enableTopicAssmnt = false;
                                break;
                            }
                            else if (item.RemainTopAsesmntTime == null && item.IsTopicPass != true && item.TopicNoOfAttempts > item.TopicMaxAttempts)
                            {
                                objStartTrainingModel.Message = Resources.VendorResource.msgExceedAttempt;
                                objStartTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                                item.enableTopicAssmnt = false;
                                break;

                            }
                            else if (item.TopicNoOfAttempts > 0)
                            {
                                objStartTrainingModel.SelectedTopicID = item.TopicID;
                                objStartTrainingModel.SelectedTopicStatus = item.TopicStatus;
                                objStartTrainingModel.ActionType = "topicfailed";
                                if (item.RemainTopAsesmntTime != null && item.RemainTopAsesmntTime > 0 && (item.IsTopicPass == null || item.IsTopicPass != false))
                                {
                                    objStartTrainingModel.ActionType = "topicassessment";
                                }
                                item.enableTopicAssmnt = true;
                                break;
                            }
                            else
                            {
                                objStartTrainingModel.SelectedTopicID = item.TopicID;
                                objStartTrainingModel.SelectedTopicStatus = item.TopicStatus;
                                objStartTrainingModel.ActionType = "topicassessment";
                                item.enableTopicAssmnt = true;
                                break;
                            }
                        }
                        else if (item.TopicStatusName.ToLower() == "completed" && objStartTrainingModel.IsTopicWiseAssessment && item.IsTopicPass != null && item.IsTopicPass == true)
                        {
                            if (objStartTrainingModel.lstTopicModel.Where(x => x.IsTopicPass == null || x.IsTopicPass == false).Count() == 0 && objStartTrainingModel.IsFeedbackRequired == true && !objStartTrainingModel.IsFeedBackCompleted)
                            {

                                objStartTrainingModel.SelectedTopicID = item.TopicID;
                                objStartTrainingModel.SelectedTopicStatus = item.TopicStatus;
                                objStartTrainingModel.ActionType = "feedback";
                                item.enableTopicAssmnt = false;

                                break;

                            }
                        }

                        else if (item.TopicStatusName.ToLower() == "completed" && !objStartTrainingModel.IsTopicWiseAssessment)
                        {
                            if (objStartTrainingModel.lstTopicModel.Where(x => x.TopicStatusName.ToLower() == "not started" || x.TopicStatusName.ToLower() == "in progress").Count() == 0)
                            {
                                if (objStartTrainingModel.RemainTrnAsesmntTime != null && ((objStartTrainingModel.RemainTrnAsesmntTime <= 0 && objStartTrainingModel.TrainingNoOfAttempts > objStartTrainingModel.TrainingMaxAttempts  && objStartTrainingModel.IsPass!= true) || (objStartTrainingModel.RemainTrnAsesmntTime > 0 && objStartTrainingModel.IsPass == false && objStartTrainingModel.TrainingNoOfAttempts > objStartTrainingModel.TrainingMaxAttempts)))
                                {
                                    objStartTrainingModel.Message = Resources.VendorResource.msgExceedAttempt;
                                    objStartTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                                    objStartTrainingModel.enableTrnAssmnt = false;
                                    break;
                                }
                                else if (objStartTrainingModel.RemainTrnAsesmntTime == null && objStartTrainingModel.IsPass != true && objStartTrainingModel.TrainingNoOfAttempts > objStartTrainingModel.TrainingMaxAttempts)
                                {
                                    objStartTrainingModel.Message = Resources.VendorResource.msgExceedAttempt;
                                    objStartTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                                    objStartTrainingModel.enableTrnAssmnt = false;
                                    break;
                                }
                                else if (objStartTrainingModel.TrainingNoOfAttempts > 0 && (objStartTrainingModel.IsPass == null || objStartTrainingModel.IsPass == false))
                                {

                                    objStartTrainingModel.SelectedTopicID = item.TopicID;
                                    objStartTrainingModel.SelectedTopicStatus = item.TopicStatus;
                                    objStartTrainingModel.ActionType = "trainingfailed";
                                    if (objStartTrainingModel.RemainTrnAsesmntTime != null && objStartTrainingModel.RemainTrnAsesmntTime > 0 && (objStartTrainingModel.IsPass == null || objStartTrainingModel.IsPass != false))
                                    {
                                        objStartTrainingModel.ActionType = "trainingassessment";
                                    }
                                    objStartTrainingModel.enableTrnAssmnt = true;
                                    break;
                                }

                                else if (objStartTrainingModel.IsPass == null || objStartTrainingModel.IsPass == false)
                                {
                                    objStartTrainingModel.SelectedTopicID = item.TopicID;
                                    objStartTrainingModel.SelectedTopicStatus = item.TopicStatus;
                                    objStartTrainingModel.ActionType = "trainingassessment";
                                    objStartTrainingModel.enableTrnAssmnt = true;
                                    break;
                                }
                                else if (objStartTrainingModel.IsFeedbackRequired && !objStartTrainingModel.IsFeedBackCompleted)
                                {
                                    objStartTrainingModel.SelectedTopicID = item.TopicID;
                                    objStartTrainingModel.SelectedTopicStatus = item.TopicStatus;
                                    objStartTrainingModel.enableTrnAssmnt = false;
                                    objStartTrainingModel.ActionType = "feedback";
                                    break;
                                }
                            }
                        }
                    }
                    else if (objStartTrainingModel.IsFeedbackRequired && !objStartTrainingModel.IsFeedBackCompleted)
                    {
                        objStartTrainingModel.SelectedTopicID = item.TopicID;
                        objStartTrainingModel.SelectedTopicStatus = item.TopicStatus;
                        objStartTrainingModel.enableTrnAssmnt = false;
                        objStartTrainingModel.ActionType = "feedback";
                        break;
                    }

                }
            }
        }
        //load dynamic content by this method 
        public ActionResult LoadContent(int TopicID, int BCID, int TID, int TopicStatus, string ActionType, Boolean IsTrainingCompleted, string TopicStatusName = "")
        {
            return TopicStart(TopicID, BCID, TopicStatus, IsTrainingCompleted, TopicStatusName);
            //StartTrainingModel obj = new StartTrainingModel();
            //switch (ActionType)
            //{
            //    case "topicstart":

            //    case "trainingassessment":
            //        return TopicStart(TopicID, BCID, TopicStatus, IsTrainingCompleted, TopicStatusName);
            //    //  return Assessment(BCID, TID);
            //    case "topicassessment":
            //        return TopicStart(TopicID, BCID, TopicStatus, IsTrainingCompleted, TopicStatusName);
            //    // return Assessment(BCID, TID, TopicID);
            //    case "feedback":
            //        return TopicStart(TopicID, BCID, TopicStatus, IsTrainingCompleted, TopicStatusName);
            //        // return RedirectToAction("Feedback", "ParticipantFeedback", new { BCID = BCID });


            //}
            //return PartialView("_ViewTopic", obj);
        }

        /// <summary>
        /// Save training Time 
        /// </summary>
        /// <param name="ScheduledTrainingID"></param>
        /// <param name="TrainingID"></param>
        /// <param name="TrainerID"></param>
        /// <param name="StartDateTime"></param>
        /// <param name="EndDateTime"></param>
        /// <param name="IsTrainingFinished"></param>
        /// <returns></returns>
        public StartTrainingModel SaveTrainingTime(int BCID, DateTime? StartDateTime, DateTime? EndDateTime, Boolean IsTrainingFinished)
        {

            StartTrainingModel objStartTrainingModel = new StartTrainingModel();
            try
            {
                objStartTrainingModel.BookedCandidateID = BCID;
                objStartTrainingModel.CreatedBy = createdBy;
                objStartTrainingModel.StartDateTime = StartDateTime;
                objStartTrainingModel.EndDateTime = EndDateTime;
                objStartTrainingModel.IsTrainingFinished = IsTrainingFinished;
                objStartTrainingModel.IsTrainingClosed = false;
                objStartTrainingModel = objVendorViewModel.InsertUpdateOnlineTrainingStartTime(objStartTrainingModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "SaveTrainingTime Post", ex);
            }
            return objStartTrainingModel;

        }
        [Filters.Authorized()]

        public ActionResult TrainingTimeTick(int BCID, int IsWindowClosed, Boolean IsTrainingFinished)
        {
            StartTrainingModel objStartTrainingModel = new StartTrainingModel();
            try
            {
                if (createdBy > 0)
                {
                    objStartTrainingModel.BookedCandidateID = BCID;

                    objStartTrainingModel.CreatedBy = createdBy;
                    objStartTrainingModel.IsTrainingFinished = IsTrainingFinished;
                    if (IsTrainingFinished == false)
                    {
                        objStartTrainingModel.StartDateTime = System.DateTime.Now; //null;
                        objStartTrainingModel.EndDateTime = null;
                    }
                    else
                    {
                        objStartTrainingModel.StartDateTime = null; //null;
                        objStartTrainingModel.EndDateTime = System.DateTime.Now;

                    }
                    objStartTrainingModel.IsTrainingClosed = Convert.ToBoolean(IsWindowClosed);
                    objStartTrainingModel = objVendorViewModel.InsertUpdateOnlineTrainingStartTime(objStartTrainingModel);
                    ViewBag.TimerTickValue = CommonUtils.TimerTickValue;
                    Session.Timeout = 15;

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "TrainingTimeTick Post", ex);
            }
            return Json(objStartTrainingModel.SpendTime, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TopicStart(int TopicID, int BCID, int TopicStatus, Boolean IsTrainingCompleted, string TopicStatusName)
        {

            var objStartTrainingModel = new StartTrainingModel();
            try
            {
                objStartTrainingModel.TopicID = TopicID;
                objStartTrainingModel.TopicStatus = TopicStatus;
                objStartTrainingModel.BookedCandidateID = BCID;
                objStartTrainingModel.CreatedBy = createdBy;
                objStartTrainingModel = objVendorViewModel.GetTopicWithSpendTimeByBookedCandidateIDs(objStartTrainingModel);
                objStartTrainingModel.TopicID = TopicID;
                objStartTrainingModel.BookedCandidateID = BCID;
                objStartTrainingModel.TopicStatus = TopicStatus;
                objStartTrainingModel.CreatedBy = createdBy;
                objStartTrainingModel.IsFinished = TopicStatusName == "completed" ? true : false;// IsTopicFinish;
                objStartTrainingModel.StartDateTime = null; //null;

                objStartTrainingModel.EndDateTime = null;
                if (IsTrainingCompleted == false)
                {
                    if (TopicStatusName != "completed")
                        objStartTrainingModel = objVendorViewModel.InsertUpdateOnlineTopicTime(objStartTrainingModel);
                }
                else
                    objStartTrainingModel.IsFinished = IsTrainingCompleted;
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "TopicStart Post", ex);
            }
            return PartialView("_ViewTopic", objStartTrainingModel);
        }
        //public StartTrainingModel TopicStartModel(StartTrainingModel objStartTrainingModel)
        //{


        //    try
        //    {
        //        objStartTrainingModel.TopicID = objStartTrainingModel.SelectedTopicID;
        //        objStartTrainingModel.TopicStatus = objStartTrainingModel.SelectedTopicStatus;
        //        objStartTrainingModel.BookedCandidateID = objStartTrainingModel.BookedCandidateID;
        //        objStartTrainingModel.CreatedBy = createdBy;
        //        objStartTrainingModel = objVendorViewModel.GetTopicWithSpendTimeByBookedCandidateIDs(objStartTrainingModel);
        //        objStartTrainingModel.TopicID = objStartTrainingModel.SelectedTopicID;
        //        objStartTrainingModel.BookedCandidateID = objStartTrainingModel.BookedCandidateID;
        //        objStartTrainingModel.TopicStatus = objStartTrainingModel.SelectedTopicStatus; ;
        //        objStartTrainingModel.CreatedBy = createdBy;
        //        objStartTrainingModel.IsFinished = false;
        //        objStartTrainingModel.StartDateTime = null; //null;

        //        objStartTrainingModel.EndDateTime = null;
        //        if (objStartTrainingModel.IsTrainingComplete == false)
        //        {
        //            if (objStartTrainingModel.TopicStatusName != "completed")
        //                objStartTrainingModel = objVendorViewModel.InsertUpdateOnlineTopicTime(objStartTrainingModel);
        //        }
        //        else
        //            objStartTrainingModel.IsFinished = objStartTrainingModel.IsTrainingComplete;
        //    }
        //    catch (Exception ex)
        //    {
        //        Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "TopicStart Post", ex);
        //    }
        //    return objStartTrainingModel;
        //}

        public ActionResult TopicTimeTick(int TopicID, int BCID, int TopicStatus, int IsWindowClosed, Boolean IsTopicFinish)
        {
            StartTrainingModel objStartTrainingModel = new StartTrainingModel();
            try
            {
                objStartTrainingModel.TopicID = TopicID;
                objStartTrainingModel.BookedCandidateID = BCID;
                objStartTrainingModel.TopicStatus = TopicStatus;
                objStartTrainingModel.CreatedBy = createdBy;
                objStartTrainingModel.IsFinished = IsTopicFinish;

                if (IsTopicFinish == false)
                {
                    if (IsWindowClosed == 0)
                    {
                        objStartTrainingModel.StartDateTime = System.DateTime.Now; //null;
                        objStartTrainingModel.EndDateTime = null;
                    }
                    else
                    {
                        objStartTrainingModel.StartDateTime = null; //null;
                        objStartTrainingModel.EndDateTime = System.DateTime.Now;
                    }

                }
                else
                {
                    objStartTrainingModel.StartDateTime = null; //null;
                    objStartTrainingModel.EndDateTime = System.DateTime.Now;

                }
                objStartTrainingModel = objVendorViewModel.InsertUpdateOnlineTopicTime(objStartTrainingModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "TopicTimeTick Post", ex);
            }
            // return Json(objStartTrainingModel, JsonRequestBehavior.AllowGet);
            return Json(new { TopicStatus = objStartTrainingModel.TopicStatus }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult UpdateTopicList(int BCID, int TopicID)
        {
            StartTrainingModel objStartTrainingModel = new StartTrainingModel();
            try
            {
                //Get Topic detail of training 
                objStartTrainingModel.BookedCandidateID = BCID;
                objStartTrainingModel = objVendorViewModel.GetOnlineTrainingWithSpendTimeByID(objStartTrainingModel);
                List<TopicModel> lstTopicModel = objVendorViewModel.GetTopicWithSpendTimeByBookedCandidateID(objStartTrainingModel).ToList();
                objStartTrainingModel.lstTopicModel = lstTopicModel.OrderBy(x => x.TopicOrderNo).ToList();
                SetContent(objStartTrainingModel);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "UpdateTopicList Post", ex);
            }
            return PartialView("_TrainingTopicList", objStartTrainingModel);
        }


        public ActionResult CheckTraining(int BCID)
        {
            CandidateCompleteModel objCandidateCompleteModel = new CandidateCompleteModel();
            try
            {
                VendorViewModel objVendorViewModel = new VendorViewModel();
                objCandidateCompleteModel = objVendorViewModel.CheckOnlineTrainingBeforeComplete(BCID);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "CheckTraining Post", ex);
            }
            return PartialView("_CheckTrainingComplete", objCandidateCompleteModel);
        }


        /// <summary>
        ///  Check PreRequisiteTraining Status
        /// </summary>
        /// <param name="TrainingID"></param>
        /// <returns></returns>
        public ActionResult CheckPreRequisiteTraining(int TID, string RegType)
        {
            List<VendorPreRequisiteModel> lstPreRequisiteTrainingModel = new List<VendorPreRequisiteModel>();
            try
            {
                VendorViewModel objVendorViewModel = new VendorViewModel();
                lstPreRequisiteTrainingModel = objVendorViewModel.CheckPreRequisiteTraining(TID, createdBy);
                ViewBag.TrnID = TID;
                ViewBag.RegType = RegType;
                if (lstPreRequisiteTrainingModel.Count() > 0)
                {
                    return PartialView("_PreRequisiteTraining", lstPreRequisiteTrainingModel);
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "CheckPreRequisiteTraining", ex);
            }
            return Json(new { TID = 0 }, JsonRequestBehavior.AllowGet);

        }



        /// <summary>
        /// Start Training Assessment 
        /// </summary>
        /// <param name="BCID"></param>
        /// <param name="TID"></param>
        /// <param name="TPID"></param>
        /// <returns></returns>
        public JsonResult AssessmentTraining(int BCID, int TID, int TPID = 0)
        {
            OnlineAssessmentResultModel objAssessment = new OnlineAssessmentResultModel();
            try
            {
                AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();
                objAssessment.BookedCandidateID = BCID;
                objAssessment.TrainingID = TID;
                objAssessment.CreatedBy = createdBy;
                objAssessment.TopicID = TPID == 0 ? null : (Nullable<int>)TPID;
                objAssessmentViewModel.StartOnlineAssessment(objAssessment);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "AssessmentTraining ", ex);
            }
            return Json(new { arID = objAssessment.AssessmentResultID });

        }


        //public ActionResult Assessment(int BCID, int TID, int TPID = 0)
        //{
        //    OnlineAssessmentResultModel objAssessment = new OnlineAssessmentResultModel();
        //    try
        //    {
        //        AssessmentViewModel objAssessmentViewModel = new AssessmentViewModel();
        //        objAssessment.BookedCandidateID = BCID;
        //        objAssessment.TrainingID = TID;
        //        objAssessment.CreatedBy = createdBy;
        //        objAssessment.TopicID = TPID == 0 ? null : (Nullable<int>)TPID;
        //        objAssessmentViewModel.StartOnlineAssessment(objAssessment);

        //    }
        //    catch (Exception ex)
        //    {
        //        Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Vendor", "AssessmentTraining ", ex);
        //    }
        //    return RedirectToAction("OnlineExam", "Assessment", new { arID = objAssessment.AssessmentResultID });//, @"~/Views/Assessment/OnlineExam.cshtml?arID=" + objAssessment.AssessmentResultID);

        //}


        #endregion

    }
}
