using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using CTMS.Common;
using CTMS.Models;
using CTMS.ViewModel;
using Newtonsoft.Json;
using System.IO;

namespace CTMS.Controllers
{
    public class TrainingController : BaseController
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
        public int RoleID
        {
            get
            {
                int _roleID = 0;
                if (Session["RoleId"] != null)
                    int.TryParse(Convert.ToString(Session["RoleId"]), out _roleID);
                return _roleID;
            }
        }
        #endregion
        CommonUtils objCommonUtilError = new CommonUtils();
        TrainingViewModel objTrainingViewModel = new TrainingViewModel();

        #region View Training List
        //
        // GET: /Training/
        /// <summary>
        /// Show Training List
        /// </summary>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult ViewTraining()
        {
            ViewTrainingsModel objViewTrainingsModel = new ViewTrainingsModel();
           
            
            List<TrainingTypeModel> TrainingTypes = objTrainingViewModel.GetAllActiveTrainigType(true).ToList();
           
            List<TrainingCategoryModel> lstTrainingCategories = objTrainingViewModel.GetAllTrainingCategory(true).ToList();
            if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
            {
                objViewTrainingsModel.Message = Convert.ToString(Session["Message"]);
                objViewTrainingsModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;

            }
            
            objViewTrainingsModel.CurrentPage = 1;
            objViewTrainingsModel.PageSize = CommonUtils.PageSize;
           
            objViewTrainingsModel.TotalPages = 0;
            objTrainingViewModel.GetAllTrainings(objViewTrainingsModel);

                 
            
            
          
            ViewBag.TrainingTypeList = new SelectList(TrainingTypes, "TrainingTypeID", "TrainingTypeName", objViewTrainingsModel.FilterTrainingType);
            ViewBag.TrainingCategoyList = new SelectList(lstTrainingCategories, "TrainingCategoryId", "TrainingCategoryName", objViewTrainingsModel.FilterTrainingCategory);
            return View(objViewTrainingsModel);
        }

        /// <summary>
        /// Show Training List based on paging and sorting filter
        /// </summary>
        /// <param name="objViewTrainingsModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewTraining(ViewTrainingsModel objViewTrainingsModel)
        {
            objViewTrainingsModel.Message = objViewTrainingsModel.MessageType = String.Empty;

            if (objViewTrainingsModel.ActionType == "delete")
            {
                objTrainingViewModel.DeleteTraining(objViewTrainingsModel);
            }
            objTrainingViewModel.GetAllTrainings(objViewTrainingsModel);
            objViewTrainingsModel.SelectedTraining = null;
            if (objViewTrainingsModel.ActionType == "showDetails")
            {
                objViewTrainingsModel.SelectedTraining = objViewTrainingsModel.Trainings.Where(o=>o.TrainingID == objViewTrainingsModel.TrainingDetailID).FirstOrDefault();
                TrainingTopic objTrainingTopic = new TrainingTopic();
                TopicViewModel objTopicViewModel = new TopicViewModel();
                objTrainingTopic.CurrentPage = 1;
                objTrainingTopic.PageSize = CommonUtils.PageSize;

                objTrainingTopic.TotalPages = 0;
                objTopicViewModel.GetAllTopicbyTrainingID(objViewTrainingsModel.SelectedTraining.TrainingID, objTrainingTopic, false);
                objViewTrainingsModel.SelectedTraining.TrainingTopics = new ViewTopicModel();
                objViewTrainingsModel.SelectedTraining.TrainingTopics.TopicList = objTrainingTopic.lstTopicModel;
            }
            return PartialView("TrainingList", objViewTrainingsModel);
          }

        #endregion

        #region Add/Edit Training

        /// <summary>
        /// Show Training Detail, If TrainingID is null or 0 means New Training else Edit Training.
        /// </summary>
        /// <param name="TrainingID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult SaveTraining(int? TrainingID)
        {
            SaveTrainingModel objSaveTrainingModel = new SaveTrainingModel();
            List<TrainingCategoryModel> lstTrainingCategoryModel = new List<TrainingCategoryModel>();
            List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
            int TrainingId;
            try
            {
                TrainingId = TrainingID != null ? (int)TrainingID : 0;
                objSaveTrainingModel = TrainingId != 0 ? objTrainingViewModel.GetTrainingByTrainingID(TrainingId) : new SaveTrainingModel();

                string CategoryIDs = String.Empty;
                if (TrainingId > 0)
                {

                    objSaveTrainingModel.SelectedFunctionList = objTrainingViewModel.GetFunctionsByTrainingID(TrainingId).ToList().OrderBy(o => o.CategoryID).ToList();
                    if (objSaveTrainingModel.SelectedFunctionList != null)
                    {
                        foreach (FunctionCategoryModel objFunctionCategory in objSaveTrainingModel.SelectedFunctionList)
                        {
                            objFunctionCategory.IsSelected = true;
                        }
                        CategoryIDs = String.Join(",",objSaveTrainingModel.SelectedFunctionList.Select(o => o.CategoryID).Distinct());
                        
                        lstFunctionCategoryModel = objTrainingViewModel.GetFunctionsByCategoryIds(CategoryIDs);
                         FillCategoryFunctionDetail(objSaveTrainingModel, lstFunctionCategoryModel);
                        objSaveTrainingModel.SelectedFunctionList = lstFunctionCategoryModel;
                    }
                }
                objSaveTrainingModel.SelectedCategories = CategoryIDs;
               
                FillTrainingTypeAndCategory(objSaveTrainingModel);
                if (objSaveTrainingModel.PublishCount > 0)
                {
                    objSaveTrainingModel.Message = Resources.TrainingResource.msgTrainingPublishNotification;
                    objSaveTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                }
                else if (objSaveTrainingModel.IsPublished) //cehck for online Training
                {
                    objSaveTrainingModel.PublishCount = 1;
                    objSaveTrainingModel.Message = Resources.TrainingResource.msgTrainingPublishNotification;
                    objSaveTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                }
            }
            catch(Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "SaveTraining Get", ex);
            }
            return View(objSaveTrainingModel);
        }

        /// <summary>
        /// Get Function List based on CategoryIds
        /// </summary>
        /// <param name="CategoryIds"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public JsonResult GetFunctions(string CategoryIds)
        {
            List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
            try
            {
               
                if (!String.IsNullOrEmpty(CategoryIds) && CategoryIds != "null")
                {
                   lstFunctionCategoryModel = objTrainingViewModel.GetFunctionsByCategoryIds(CategoryIds);
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "GetFunctions", ex);
            }
            return Json(lstFunctionCategoryModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Save detail of Training
        /// </summary>
        /// <param name="objSaveTrainingModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTraining(SaveTrainingModel objSaveTrainingModel)
        {
            string redirectUrl = string.Empty;
            try
            {
                List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
                FillTrainingTypeAndCategory(objSaveTrainingModel);
                switch (objSaveTrainingModel.ActionType)
                {
                    case "changecategory":
                        
                        lstFunctionCategoryModel = objTrainingViewModel.GetFunctionsByCategoryIds(objSaveTrainingModel.SelectedCategories);
                       
                        if (objSaveTrainingModel.SelectedFunctionList != null)
                        {
                            FillCategoryFunctionDetail(objSaveTrainingModel, lstFunctionCategoryModel);
                        }
                        objSaveTrainingModel.SelectedFunctionList = lstFunctionCategoryModel;
                        return PartialView("CategoryFunctionList", objSaveTrainingModel);

                    default:
                        if (objSaveTrainingModel.ActionType.ToLower() == "publishtraining")
                        {
                            objSaveTrainingModel.IsPublished = true;
                            objSaveTrainingModel.IsSubmit   = true;
                        }
                        if (objSaveTrainingModel.ActionType.ToLower() == "submittraining")
                        {
                            objSaveTrainingModel.IsSubmit = true;
                        }
                        foreach (FunctionCategoryModel objSelectedFunctionCategoryModel in objSaveTrainingModel.SelectedFunctionList)
                        {
                            if (objSelectedFunctionCategoryModel.IsSelected)
                            {
                                lstFunctionCategoryModel.Add(objSelectedFunctionCategoryModel);
                            }
                        }

                        string strCategoryFunctionListXml = CommonUtils.GetBulkXML(lstFunctionCategoryModel);
                        objSaveTrainingModel.IsActive = true;
                        objSaveTrainingModel.CreatedOn = DateTime.Now;
                        objSaveTrainingModel.CreatedBy = createdBy;
                        if (!objSaveTrainingModel.IsAssessmentRequired)
                        {
                            objSaveTrainingModel.IsTopicWiseAssessment = null;
                        }
                        
                        //save standard duration in minutes
                        objSaveTrainingModel.StandardDuration = objSaveTrainingModel.StandardDuration * 60;
                        objSaveTrainingModel = objTrainingViewModel.InsertUpdateTrainingDetail(objSaveTrainingModel, strCategoryFunctionListXml);
                        if (objSaveTrainingModel.ErrorCode.Equals(0))
                        {
                            if (objSaveTrainingModel.IsPublished)
                            {
                                Session["Message"] = Resources.TrainingResource.msgTrainingPublishSuccess;
                            }
                            else if (objSaveTrainingModel.IsSubmit)
                            {
                                Session["Message"] = Resources.TrainingResource.msgTrainingSaveSuccess;
                            }
                            else
                            {
                                Session["Message"] = Resources.TrainingResource.msgTrainingSaveDraftSuccess;
                            }
                            Session["MessageType"] = MessageType.Success.ToString().ToLower();
                        }
                        else
                        {
                            if (objSaveTrainingModel.IsPublished)
                            {
                                Session["Message"] = Resources.TrainingResource.msgPublishErrorMessage;
                            }
                            else
                            {
                                Session["Message"] = Resources.TrainingResource.msgSaveErrorMessage;
                            }
                            
                            Session["MessageType"] = MessageType.Error.ToString().ToLower();
                        }
                        redirectUrl = Url.Action("ViewTraining");
                        break;
                }

                switch (objSaveTrainingModel.ActionType)
                {
                    case "addtopic": redirectUrl = Url.Action("AddTopic", "Topic", new { TrainingId = objSaveTrainingModel.TrainingID, Assessment = objSaveTrainingModel.IsTopicWiseAssessment });
                                      break;
                    case "addassessment": redirectUrl = Url.Action("AssignAssessment", "Assessment", new { tID = objSaveTrainingModel.TrainingID, isTa = objSaveTrainingModel.IsTopicWiseAssessment });
                                      break;
                    case "addfeedback": redirectUrl = Url.Action("AssignFeedback", "Feedback", new { tID = objSaveTrainingModel.TrainingID });
                                      break;
                    case "addreoccurrence": redirectUrl = Url.Action("AssignReoccurrence", "Reoccurrence", new { tID = objSaveTrainingModel.TrainingID });
                                      break;
                    case "addtrainer": redirectUrl = Url.Action("AssignTrainer", "Training", new { tID = objSaveTrainingModel.TrainingID });
                                      break;
                    case "addeffectiveness": redirectUrl = Url.Action("AssignEffectiveness", "Training", new { tID = objSaveTrainingModel.TrainingID });
                                      break;
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "SaveTraining Get", ex);
            }
            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });
            
        }

        #endregion

        /// <summary>
        /// Fill Category Function Detail in lstFunctionCategoryModel object from objSaveTrainingModel object.
        /// </summary>
        /// <param name="objSaveTrainingModel"></param>
        /// <param name="lstFunctionCategoryModel"></param>
        public void FillCategoryFunctionDetail(SaveTrainingModel objSaveTrainingModel, List<FunctionCategoryModel> lstFunctionCategoryModel)
        {
            foreach (FunctionCategoryModel objFunctionCategory in lstFunctionCategoryModel)
            {
                FunctionCategoryModel objSelectedFunctionCategoryModel = objSaveTrainingModel.SelectedFunctionList.Where(o => o.FunctionID == objFunctionCategory.FunctionID).SingleOrDefault();
                if (objSelectedFunctionCategoryModel != null)
                {
                    objFunctionCategory.TrainingFunctionMappingID = objSelectedFunctionCategoryModel.TrainingFunctionMappingID;
                    objFunctionCategory.TrainingID = objSelectedFunctionCategoryModel.TrainingID;
                    objFunctionCategory.FunctionID = objSelectedFunctionCategoryModel.FunctionID;
                    objFunctionCategory.FunctionName = objSelectedFunctionCategoryModel.FunctionName;
                    objFunctionCategory.CategoryID = objSelectedFunctionCategoryModel.CategoryID;
                    objFunctionCategory.CategoryName = objSelectedFunctionCategoryModel.CategoryName;
                    objFunctionCategory.IsSelected = objSelectedFunctionCategoryModel.IsSelected;
                    objFunctionCategory.IsMandatory = objSelectedFunctionCategoryModel.IsMandatory;
                    objFunctionCategory.IsActive = objSelectedFunctionCategoryModel.IsActive;
                    objFunctionCategory.CreatedOn = objSelectedFunctionCategoryModel.CreatedOn;
                    objFunctionCategory.CreatedBy = objSelectedFunctionCategoryModel.CreatedBy;
                }

            }
        }

        /// <summary>
        /// Fill and select TrainingType and Category dropdown values
        /// </summary>
        /// <param name="objSaveTrainingModel"></param>
        public void FillTrainingTypeAndCategory(SaveTrainingModel objSaveTrainingModel)
        {
            int certTempID = 0;
            if (String.IsNullOrEmpty(objSaveTrainingModel.TrainingCategories))
            {
                objSaveTrainingModel.TrainingCategories = String.Empty;
            }
            if(objSaveTrainingModel.CertificateTemplateID != null)
            {
                certTempID = Convert.ToInt32(objSaveTrainingModel.CertificateTemplateID);
            }
            MasterViewModel objMasterViewModel = new MasterViewModel();
            IList<TrainingTypeModel> lstTrainingType = objTrainingViewModel.GetAllActiveTrainigType();
            IList<CategoryModel> lstCategory = objTrainingViewModel.GetAllActiveCategory();
            IList<TrainingCategoryModel> lstTrainingCategoryModel = objMasterViewModel.GetAllTrainingCategory();
            IList<CertificateTemplateModel> lstCertificateTemplateModel = objTrainingViewModel.GetAllActiveCertificates();
            ViewBag.TrainingTypeList = new SelectList(lstTrainingType, "TrainingTypeID", "TrainingTypeName", objSaveTrainingModel.TrainingTypeID);
            ViewBag.CategoryList = new MultiSelectList(lstCategory, "CategoryID", "CategoryName", objSaveTrainingModel.SelectedCategories.Split(",".ToCharArray()));
            ViewBag.TrainingCategoryList = new MultiSelectList(lstTrainingCategoryModel, "TrainingCategoryId", "TrainingCategoryName", objSaveTrainingModel.TrainingCategories.Split(",".ToCharArray()));
            ViewBag.CertificateTemplateList = new SelectList(lstCertificateTemplateModel, "CertificateTemplateID", "CertificateTemplateName", certTempID);
        }


        #region Assign Trainer
        public ActionResult AssignTrainer(int tID)
        {
            AssignTrainerListModel objAssignTrainerListModel = new AssignTrainerListModel();
            if (Session["Message"] != null && Session["MessageType"] != null)
            {
                Session["Message"] = null;
                Session["MessageType"] = null;
            }
            if (Session["msgContent"] != null && Session["msgContentType"] != null)
            {
                objAssignTrainerListModel.Message = Convert.ToString(Session["msgContent"]);
                objAssignTrainerListModel.MessageType = Convert.ToString(Session["msgContentType"]);
                Session["msgContent"] = null;
                Session["msgContentType"] = null;

            }
            
            try
            {
                objAssignTrainerListModel.TrainingData = objTrainingViewModel.GetTrainingByTrainingID(tID);
                if (objAssignTrainerListModel.TrainingData == null)
                {
                    objAssignTrainerListModel.TrainingData = new SaveTrainingModel();
                }
                objAssignTrainerListModel.TrainerList = objTrainingViewModel.GetAssignedTrainers(objAssignTrainerListModel).ToList();

                FillTrainers(objAssignTrainerListModel.TrainingData.TrainingID);

                if (objAssignTrainerListModel.TrainingData.PublishCount > 0)
                {
                    objAssignTrainerListModel.Message = Resources.TrainingResource.msgTrainingPublishNotification;
                    objAssignTrainerListModel.MessageType = MessageType.Notice.ToString().ToLower();
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "AssignTrainer Get", ex);
            }
            return View(objAssignTrainerListModel);
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult AssignTrainer(AssignTrainerListModel objAssignTrainerListModel, HttpPostedFileBase file)
        {
            try
            {
                AttachmentModel objAttachmentModel = new AttachmentModel();
                
                if (objAssignTrainerListModel.ActionType != null)
                {
                    switch (objAssignTrainerListModel.ActionType)
                    {
                        case "AddTrainer":
                            if (file != null && file.ContentLength > 0)
                            {

                                MemoryStream memStream = new MemoryStream();
                                 file.InputStream.CopyTo(memStream);
                                 objAttachmentModel.AttachmentName = objAssignTrainerListModel.SelectedFileName;
                                objAttachmentModel.AttachmentSize = Math.Round(Convert.ToDecimal(file.ContentLength), 2);
                                objAttachmentModel.AttachmentType = file.ContentType;
                                objAttachmentModel.AttachmentContent = memStream.ToArray();
                                objAttachmentModel.IsActive = true;
                                objAttachmentModel.CreatedBy = createdBy;
                                objAttachmentModel.CreatedOn = DateTime.Now;
                            }
                            objAssignTrainerListModel.IsActive = true;
                            objAssignTrainerListModel.CreatedBy = createdBy;
                            objAssignTrainerListModel.CreatedOn = DateTime.Now;
                            objTrainingViewModel.InsertUpdateTrainerAndCertificate(objAssignTrainerListModel, objAttachmentModel);
                            break;
                        case "deleteTrainer":
                            objTrainingViewModel.DeleteAssignedTrainer(objAssignTrainerListModel);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "AssignTrainer Post", ex);
            }
            Session["msgContent"] = objAssignTrainerListModel.Message;
            Session["msgContentType"] = objAssignTrainerListModel.MessageType;
            return RedirectToAction("AssignTrainer", "Training", new { tID = objAssignTrainerListModel.TrainingData.TrainingID });
        }

        #endregion

        /// <summary>
        /// Get Assessment Count based on Training ID and IsTopicWiseAssessment
        /// </summary>
        /// <param name="TrainingID"></param>
        /// <param name="IsTopicWiseAssessment"></param>
        /// <returns></returns>
        public ActionResult GetCountsOfAssessment(int TrainingID, bool IsTopicWiseAssessment)
        {
            int assessmentCount = 0;
            try
            {
                
                assessmentCount = objTrainingViewModel.GetAssessmentCount(TrainingID, IsTopicWiseAssessment);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "GetCountsOfAssessment", ex);
                return Json(ex);
            }
            return Json(assessmentCount,JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetFile(long id)
        {

            AttachmentModel objAttachmentModel = new AttachmentModel();

            objAttachmentModel.AttachmentID = id;

            objAttachmentModel = objTrainingViewModel.GetAttachmentDetail(objAttachmentModel);

            if (objAttachmentModel != null)
            {
                Byte[] data = (Byte[])objAttachmentModel.AttachmentContent;
                return new DownloadResult
                {
                    DocumentData = data,
                    DownloadFileName = objAttachmentModel.AttachmentName
                };
            }
            return new DownloadResult
            {
                DocumentData = null,
                DownloadFileName = String.Empty
            };
        }


        public void FillTrainers(int trainingId)
        {
            IList<TrainerModel> lstTrainer = objTrainingViewModel.GetTrainerByTrainingID(trainingId, true);
            ViewBag.TrainerList = new SelectList(lstTrainer, "TrainerID", "FullName", 0);
        }


        #region Assign Effectiveness
        /// <summary>
        /// Get Effectiveness list with already assigned Effectiveness of Training by Training ID
        /// </summary>
        /// <param name="tID"></param>
        /// <returns></returns>
         [Filters.MenuAccess()]
        public ActionResult AssignEffectiveness(int tID)
        {
            ViewAssignEffectivenessModel objViewAssignEffectivenessModel = new ViewAssignEffectivenessModel();
            try
            {
                int effectivenessDuration;
                int effectivenessExpireDuration;
                
                objViewAssignEffectivenessModel.TrainingData = objTrainingViewModel.GetTrainingByTrainingID(tID);
                if (objViewAssignEffectivenessModel.TrainingData == null)
                {
                    objViewAssignEffectivenessModel.TrainingData = new SaveTrainingModel();
                }
                int.TryParse(Convert.ToString(objViewAssignEffectivenessModel.TrainingData.EffectivenessDuration), out effectivenessDuration);
                int.TryParse(Convert.ToString(objViewAssignEffectivenessModel.TrainingData.EffectivenessExpireDuration), out effectivenessExpireDuration);
                objViewAssignEffectivenessModel.CurrentPage = 1;
                objViewAssignEffectivenessModel.PageSize = CommonUtils.PageSize;

                objViewAssignEffectivenessModel.TotalPages = 0;
                objViewAssignEffectivenessModel.EffectivenessDuration = effectivenessDuration;
                objViewAssignEffectivenessModel.EffectivenessExpireDuration = effectivenessExpireDuration;
                objTrainingViewModel.GetAllTrainingEffectiveness(objViewAssignEffectivenessModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "AssignEffectiveness", ex);
            }
            return View(objViewAssignEffectivenessModel);
        }

        /// <summary>
        /// Assign Effectiveness with Training
        /// </summary>
        /// <param name="objViewAssignEffectivenessModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult AssignEffectiveness(ViewAssignEffectivenessModel objViewAssignEffectivenessModel)
        {
            try
            {
                objViewAssignEffectivenessModel.Message = string.Empty;
                objViewAssignEffectivenessModel.MessageType = string.Empty;

                if (objViewAssignEffectivenessModel.ActionType != null)
                {
                    switch (objViewAssignEffectivenessModel.ActionType)
                    {
                        case "search": objViewAssignEffectivenessModel.CurrentPage = 1;
                            break;
                        case "saveeffectiveness":
                            objViewAssignEffectivenessModel.IsActive = true;
                            objViewAssignEffectivenessModel.CreatedBy = createdBy;
                            objTrainingViewModel.InsertUpdateAssignEffectivenessDetail(objViewAssignEffectivenessModel);
                            break;
                    }

                }
                objTrainingViewModel.GetAllTrainingEffectiveness(objViewAssignEffectivenessModel);

                //select unselect checkbox based select checkbox list and unselect checkbox list on page change
                if (objViewAssignEffectivenessModel.ActionType == "pagechange")
                {
                    foreach (var trainingEffectiveness in objViewAssignEffectivenessModel.AssignEffectivenessList)
                    {
                        if (!String.IsNullOrEmpty(objViewAssignEffectivenessModel.SelectedEffectivenessList))
                        {
                            if (objViewAssignEffectivenessModel.SelectedEffectivenessList.Split(',').ToList().Contains(trainingEffectiveness.EffectivenessQuestionID.ToString()))
                            {
                                trainingEffectiveness.IsSelected = true;
                            }
                        }
                        if (!String.IsNullOrEmpty(objViewAssignEffectivenessModel.UnSelectedEffectivenessList))
                        {
                            if (objViewAssignEffectivenessModel.UnSelectedEffectivenessList.Split(',').ToList().Contains(trainingEffectiveness.EffectivenessQuestionID.ToString()))
                            {
                                trainingEffectiveness.IsSelected = false;
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "AssingEffectiveness Post", ex);
            }
            return PartialView("_AssignEffectivenessList", objViewAssignEffectivenessModel);
        }
        #endregion


        #region Training Effectiveness
        /// <summary>
        /// Get Class room Trainings list with Effectiveness field.
        /// </summary>
        /// <param name="tID"></param>
        /// <param name="stID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult TrainingEffectiveness(int? tID = null,int? stID = null)
        {
            ViewTrainingEffectiveness objViewTrainingEffectiveness = new ViewTrainingEffectiveness();
            try
            {
                if (Session["EffectivenessSubmitSuccess"] != null)
                {
                    objViewTrainingEffectiveness.Message = Convert.ToString(Session["EffectivenessSubmitSuccess"]);
                    objViewTrainingEffectiveness.MessageType = MessageType.Success.ToString().ToLower();
                    Session["EffectivenessSubmitSuccess"] = null;
                    
                }
                if (String.IsNullOrEmpty(Convert.ToString(tID)) && String.IsNullOrEmpty(Convert.ToString(stID)))
                {
                    objViewTrainingEffectiveness.TrainingID = tID;
                    objViewTrainingEffectiveness.ProgramID = stID;
                    if (objTrainingViewModel.CheckEffectivenessExpire(Convert.ToInt32(tID),Convert.ToInt32(stID)))
                    {
                        TempData["UnauthorizedAccess"] = Resources.TrainingResource.msgEffectivenessLinkExpire;
                        return RedirectToAction("Home", "Home");
                    }
                }
                objViewTrainingEffectiveness.UserID = createdBy;
                objViewTrainingEffectiveness.RoleID = RoleID;
                objViewTrainingEffectiveness.CurrentPage = 1;
                objViewTrainingEffectiveness.PageSize = CommonUtils.PageSize;
                objViewTrainingEffectiveness.TotalPages = 0;
                //objViewPublishedTrainingModel.PublishedTrainings = new List<PublishedTrainingModel>();
                objViewTrainingEffectiveness.FilterPointType = null;

                BindCircleDDL(objViewTrainingEffectiveness.FilterCircle);
                BindCityDDL(objViewTrainingEffectiveness.FilterCircle == null ? 0 : (int)objViewTrainingEffectiveness.FilterCircle, objViewTrainingEffectiveness.FilterCityId);
                BindMPDDL(objViewTrainingEffectiveness.FilterCircle == null ? 0 : (int)objViewTrainingEffectiveness.FilterCircle, objViewTrainingEffectiveness.FilterCityId == null ? 0 : (int)objViewTrainingEffectiveness.FilterCityId, objViewTrainingEffectiveness.FilterMaintanencePointId);
                BindTrainingCategoryDDL();
                objTrainingViewModel.GetTrainingsByActiveAccess(objViewTrainingEffectiveness);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "TrainingEffectiveness Get", ex);
            }
            return View(objViewTrainingEffectiveness);
        }

        /// <summary>
        /// Paging, searching , sorting of Effectiveness List
        /// </summary>
        /// <param name="objViewTrainingEffectiveness"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult TrainingEffectiveness(ViewTrainingEffectiveness objViewTrainingEffectiveness)
        {
            try
            {
                objViewTrainingEffectiveness.UserID = createdBy;
                objViewTrainingEffectiveness.RoleID = RoleID;
                objTrainingViewModel.GetTrainingsByActiveAccess(objViewTrainingEffectiveness);
            }

            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "TrainingEffectiveness Post", ex);
            }
            return PartialView("_TrainingEffectivenessList", objViewTrainingEffectiveness);
        }
        private void BindDDL()
        {
            List<TrainingCategoryModel> lstTrainingCategories = this.GetCategory();
            ViewBag.TrainingCategoryList = new SelectList(lstTrainingCategories, "TrainingCategoryId", "TrainingCategoryName", null);
        }

        [HttpGet]
        public JsonResult GetCitiesByCircle(int CircleId)
        {
            int UserId, RoleId;

            int.TryParse(Session["RoleId"].ToString(), out RoleId);
            int.TryParse(Session["UserId"].ToString(), out UserId);
            var cities = this.GetCities(UserId, RoleId, CircleId);
            return Json(cities, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMaintenancePointByCircleCity(int CircleId, int CityId)
        {
            int UserId, RoleId;

            int.TryParse(Session["RoleId"].ToString(), out RoleId);
            int.TryParse(Session["UserId"].ToString(), out UserId);

            var Maintainencepoint = this.GetMaintenancePoints(UserId, RoleId, CircleId, CityId);

            return Json(Maintainencepoint, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region Online Training Effectiveness

        /// <summary>
        /// Get Online Trainings list with Effectiveness field.
        /// </summary>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult OnlineTrainingEffectiveness(int? tID = null, int? stID = null, int? bcID = null)
        {
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            ViewTrainingEffectiveness objViewTrainingEffectiveness = new ViewTrainingEffectiveness();
            try
            {
                if (Session["EffectivenessSubmitSuccess"] != null)
                {
                    objViewTrainingEffectiveness.Message = Convert.ToString(Session["EffectivenessSubmitSuccess"]);
                    objViewTrainingEffectiveness.MessageType = MessageType.Success.ToString().ToLower();
                    Session["EffectivenessSubmitSuccess"] = null;
                }

              if(!String.IsNullOrEmpty(Convert.ToString(tID)) && !String.IsNullOrEmpty(Convert.ToString(stID)) && !String.IsNullOrEmpty(Convert.ToString(bcID)))
             {
                 objViewTrainingEffectiveness.TrainingID = tID;
                 objViewTrainingEffectiveness.ProgramID = stID;
                 objViewTrainingEffectiveness.BookedCandidateID = bcID;

                 if (objTrainingViewModel.CheckEffectivenessExpire(Convert.ToInt32(tID), Convert.ToInt32(stID),Convert.ToInt32(bcID)))
                 {
                     TempData["UnauthorizedAccess"] = Resources.TrainingResource.msgEffectivenessLinkExpire;
                     return RedirectToAction("Home", "Home");
                 }
             }
             objViewTrainingEffectiveness.UserID = createdBy;
             objViewTrainingEffectiveness.CurrentPage = 1;
             objViewTrainingEffectiveness.PageSize = CommonUtils.PageSize;
             objViewTrainingEffectiveness.TotalPages = 0;
             objTrainingViewModel.GetOnlineTrainingEffectivenessList(objViewTrainingEffectiveness);
            
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "OnlineTrainingEffectiveness Get", ex);
            }
            return View(objViewTrainingEffectiveness);
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult OnlineTrainingEffectiveness(ViewTrainingEffectiveness objViewTrainingEffectiveness)
        {
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            try
            {
                objViewTrainingEffectiveness.UserID = createdBy;
                objTrainingViewModel.GetOnlineTrainingEffectivenessList(objViewTrainingEffectiveness);
            }

            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "OnlineTrainingEffectiveness Post", ex);
            }
            return PartialView("_OnlineTrainingEffectivenessList", objViewTrainingEffectiveness);
        }

        #endregion

        #region Control Binding
        private void BindCircleDDL(int? SelectedValue = 0)
        {
            int UserId, RoleId;

            int.TryParse(Session["RoleId"].ToString(), out RoleId);
            int.TryParse(Session["UserId"].ToString(), out UserId);

            List<CircleModel> Circles = this.GetCirles(UserId, RoleId);

            ViewBag.CircleList = new SelectList(Circles, "CircleID", "CircleName", SelectedValue);
        }

        private void BindCityDDL(int CircleId = 0, int? SelectedValue = 0)
        {

            int UserId, RoleId;

            int.TryParse(Session["RoleId"].ToString(), out RoleId);
            int.TryParse(Session["UserId"].ToString(), out UserId);

            List<CityModel> Cities = this.GetCities(UserId, RoleId, CircleId);

            ViewBag.CityList = new SelectList(Cities, "CityID", "CityName", SelectedValue);
        }

        private void BindMPDDL(int CircleID = 0, int CityID = 0, int? SelectedValue = 0)
        {
            int UserId, RoleId;

            int.TryParse(Session["RoleId"].ToString(), out RoleId);
            int.TryParse(Session["UserId"].ToString(), out UserId);

            List<MaintenancePointModel> MaintanencePoints = this.GetMaintenancePoints(UserId, RoleId, CircleID, CityID);

            //MaintanencePoints.Insert(0, new MaintenancePointModel { MaintenancePointId = 0, MaintenancePoint = "--Select--" });
            ViewBag.MaintanencePointList = new SelectList(MaintanencePoints, "MaintenancePointId", "MaintenancePoint", SelectedValue);
        }

        private void BindTrainingTypeDDL()
        {
            List<TrainingTypeModel> TrainingTypes = objTrainingViewModel.GetAllActiveTrainigType(false).ToList();
            TrainingTypes.Insert(0, new TrainingTypeModel { TrainingTypeID = 0, TrainingTypeName = "--ALL--" });
            ViewBag.TrainingTypeList = new SelectList(TrainingTypes, "TrainingTypeID", "TrainingTypeName", null);
        }

        private void BindTrainingCategoryDDL()
        {
            List<TrainingCategoryModel> lstTrainingCategoryModel = this.GetCategory();

            ViewBag.TrainingCategoryList = new SelectList(lstTrainingCategoryModel, "TrainingCategoryId", "TrainingCategoryName", null);
        }
        #endregion

        #region Private Methods
        private List<CircleModel> GetCirles(int UserId, int RoleId)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<CircleModel> lstCircles = objMasterViewModel.GetUserwiseCircles(UserId, RoleId).ToList<CircleModel>();

            if (lstCircles == null)
                lstCircles = new List<CircleModel>();

            lstCircles.Insert(0, new CircleModel { CircleID = 0, CircleName = "--ALL--" });

            return lstCircles;
        }

        private List<CityModel> GetCities(int UserId, int RoleId, int CircleId)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<CityModel> lstCities = objMasterViewModel.GetUserwiseCity(UserId, RoleId, CircleId).ToList<CityModel>();

            if (lstCities == null)
                lstCities = new List<CityModel>();

            lstCities.Insert(0, new CityModel { CityID = 0, CityName = "--ALL--" });

            return lstCities;
        }

        private List<MaintenancePointModel> GetMaintenancePoints(int UserId, int RoleId, int CircleId, int CityId)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<MaintenancePointModel> lstMaintenancePoints = objMasterViewModel.GetUserwiseMaintenancePoint(UserId, RoleId, CircleId, CityId).ToList<MaintenancePointModel>();

            if (lstMaintenancePoints == null)
                lstMaintenancePoints = new List<MaintenancePointModel>();

            lstMaintenancePoints.Insert(0, new MaintenancePointModel { MaintenancePointId = 0, MaintenancePoint = "--ALL--" });

            return lstMaintenancePoints;
        }

        private List<TrainingCategoryModel> GetCategory()
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<TrainingCategoryModel> lstTrainingCategoryModel = objMasterViewModel.GetAllTrainingCategory().ToList<TrainingCategoryModel>();

            if (lstTrainingCategoryModel == null)
                lstTrainingCategoryModel = new List<TrainingCategoryModel>();

            lstTrainingCategoryModel.Insert(0, new TrainingCategoryModel { TrainingCategoryId = 0, TrainingCategoryName = "--ALL--" });

            return lstTrainingCategoryModel;
        }
        #endregion


        #region Submit Efectiveness
        /// <summary>
        /// Submit Effectiveness of Training (classroom or online)
        /// </summary>
        /// <param name="tID"></param>
        /// <param name="stID"></param>
        /// <param name="bcID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult SubmitEffectiveness(int tID, int? stID = null, int? bcID = null)
        {
            
            EffectivenessQuestionPaperModel objEffectivenessQuestionPaperModel = new EffectivenessQuestionPaperModel();
            try
            {
                objEffectivenessQuestionPaperModel.TrainingID = tID;
                objEffectivenessQuestionPaperModel.ProgramID = stID == null? 0:Convert.ToInt32(stID);
                objEffectivenessQuestionPaperModel.BookedCandidateID = bcID;
                objEffectivenessQuestionPaperModel.QuestionNo = 1;
                objEffectivenessQuestionPaperModel = objTrainingViewModel.GetEffectivenessAssessment(objEffectivenessQuestionPaperModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "SubmitEffectiveness Get", ex);
            }
            return View(objEffectivenessQuestionPaperModel);
        }

        /// <summary>
        /// Submit Effectiveness of Training (classroom or online) with Next, previous , save and save and finish.
        /// </summary>
        /// <param name="objEffectivenessQuestionPaperModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        public ActionResult SubmitEffectiveness(EffectivenessQuestionPaperModel objEffectivenessQuestionPaperModel)
        {
            try
            {
                string redirectUrl = string.Empty;
                if (objEffectivenessQuestionPaperModel != null && !String.IsNullOrEmpty(objEffectivenessQuestionPaperModel.ActionType))
                {
                    objEffectivenessQuestionPaperModel.IsActive = true;
                    objEffectivenessQuestionPaperModel.CreatedBy = createdBy;
                    int qNo = objEffectivenessQuestionPaperModel.QuestionNo;

                    switch (objEffectivenessQuestionPaperModel.ActionType.ToLower())
                    {
                        case "previous":

                            qNo = objEffectivenessQuestionPaperModel.QuestionNo - 1;

                            break;
                        case "next":
                            qNo = objEffectivenessQuestionPaperModel.QuestionNo + 1;

                            break;
                    }

                    


                    if (!String.IsNullOrEmpty(objEffectivenessQuestionPaperModel.ActionType) && String.Compare(objEffectivenessQuestionPaperModel.ActionType.ToLower(), "submit", true) == 0)
                    {
                        objEffectivenessQuestionPaperModel.IsSubmit = true;
                        objTrainingViewModel.InsertUpdateEffectivenessAssessment(objEffectivenessQuestionPaperModel);
                        objEffectivenessQuestionPaperModel.QuestionNo = qNo;
                        objEffectivenessQuestionPaperModel = objTrainingViewModel.GetEffectivenessAssessment(objEffectivenessQuestionPaperModel);
                        if (objEffectivenessQuestionPaperModel.ProgramID > 0)
                        {
                            redirectUrl = Url.Action("TrainingEffectiveness", "Training");
                        }
                        else
                        {
                            redirectUrl = Url.Action("OnlineTrainingEffectiveness", "Training");

                        }
                        Session["EffectivenessSubmitSuccess"] = Resources.TrainingResource.EffectivenessSubmitSuccess;
                        return Json(new { url = redirectUrl });
                    }
                    else
                    {
                        objTrainingViewModel.InsertUpdateEffectivenessAssessment(objEffectivenessQuestionPaperModel);
                        objEffectivenessQuestionPaperModel.QuestionNo = qNo;
                        objEffectivenessQuestionPaperModel = objTrainingViewModel.GetEffectivenessAssessment(objEffectivenessQuestionPaperModel);
                    }
                   
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "SubmitEffectiveness Post", ex);
            }
            return PartialView("_SubmitEffectivenessDetail", objEffectivenessQuestionPaperModel);
        }
        #endregion

        #region View Effectiveness After submit Effectiveness
        /// <summary>
        /// View Effectiveness Result after submit effectiveness
        /// </summary>
        /// <param name="tID"></param>
        /// <param name="stID"></param>
        /// <param name="bcID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult ViewEffectiveness(int? tID = null, int? stID = null, int? bcID = null)
        {
            ViewEffectivenessResult objViewEffectivenessResult = new ViewEffectivenessResult();
            try
            {
                int trainingID;
                int scheduledTrainingID;
                int.TryParse(Convert.ToString(tID), out trainingID);
                int.TryParse(Convert.ToString(stID), out scheduledTrainingID);
                objViewEffectivenessResult.TrainingID = trainingID;
                objViewEffectivenessResult.ScheduledTrainingID = scheduledTrainingID;
                objViewEffectivenessResult.BookedCandidateID = bcID;
               objViewEffectivenessResult.CurrentPage = 1;
                objViewEffectivenessResult.PageSize = CommonUtils.PageSize;
                objViewEffectivenessResult.TotalPages = 0;
                objTrainingViewModel.GetEffectivenessResultDetail(objViewEffectivenessResult);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "ViewEffectiveness GET", ex);
            }
            return View(objViewEffectivenessResult);
        }

        /// <summary>
        /// paging on Effectiveness result list in view effectiveness
        /// </summary>
        /// <param name="objViewEffectivenessResult"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult ViewEffectiveness(ViewEffectivenessResult objViewEffectivenessResult)
        {
            try
            {
                objTrainingViewModel.GetEffectivenessResultDetail(objViewEffectivenessResult);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "ViewEffectiveness Post", ex);
            }
            return PartialView("_ViewEffectivenessList", objViewEffectivenessResult);
        }
        #endregion
    }
}
