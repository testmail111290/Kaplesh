using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.ViewModel;
using CTMS.Models;
using CTMS.Common;
using System.Globalization;

namespace CTMS.Controllers
{
    public class ScheduledTrainingController : Controller
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
        public int roleId
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
        //
        // GET: /ScheduledTraining/
        CommonUtils objCommonUtilError = new CommonUtils();
        #region ViewScheduledTraining
        /// <summary>
        /// View Scheduled Training by TrainingID
        /// </summary>
        /// <param name="TrainingID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult ViewScheduledTraining()
        {
            ViewScheduledTrainingsModel objViewScheduledTrainingsModel = new ViewScheduledTrainingsModel();
            ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();

            try
            {
                List<PointTypeModel> lstPointTypes = objScheduledTrainingViewModel.GetAllActivePointTypes(true).ToList();
                List<CircleModel> lstTrainingCircles = objScheduledTrainingViewModel.GetCirclesList(true).ToList();
                objViewScheduledTrainingsModel.CurrentPage = 1;
                objViewScheduledTrainingsModel.PageSize = CommonUtils.PageSize;

                objViewScheduledTrainingsModel.TotalPages = 0;
                
                if (objViewScheduledTrainingsModel.FilterTrainingStatus == null || objViewScheduledTrainingsModel.FilterTrainingStatus == "")
                {
                    objViewScheduledTrainingsModel.FilterTrainingStatus = CommonUtils.TrainingStatus.Non_Published.ToString().Replace("_","-").ToString();
                }
               
                //if (lstPointTypes != null && lstPointTypes.Count > 0)
                //{
                //    objViewScheduledTrainingsModel.FilterPointType = lstPointTypes[0].PointTypeID;
                //}
                
                objScheduledTrainingViewModel.GetScheduledTrainings(objViewScheduledTrainingsModel);

                ViewBag.PointTypeList = new SelectList(lstPointTypes, "PointTypeID", "PointName", objViewScheduledTrainingsModel.FilterPointType);
                ViewBag.TrainingCircleList = new SelectList(lstTrainingCircles, "CircleID", "CircleName", objViewScheduledTrainingsModel.FilterCircle);

                if (Session["ScheduledTrainingSuccess"] != null && Convert.ToString(Session["ScheduledTrainingSuccess"]) != "")
                {
                    objViewScheduledTrainingsModel.Message = Convert.ToString(Session["ScheduledTrainingSuccess"]);
                    objViewScheduledTrainingsModel.MessageType = MessageType.Success.ToString().ToLower();
                    Session["ScheduledTrainingSuccess"] = null;
                }

                //TempData["TrainingID"] = TrainingID;
                //TempData.Keep("TrainingID");

                #region Calendar
                List<KeyValueModel> lstMonths = new List<KeyValueModel>();
                List<KeyValueModel> lstYears = new List<KeyValueModel>();
                for (int i = 0; i < 12; i++)
                {
                    lstMonths.Add(new KeyValueModel() { Key = i + 1, Value = CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames[i] });
                    lstYears.Add(new KeyValueModel() { Key = ((DateTime.Now.Year - 6) + i), Value = ((DateTime.Now.Year - 6) + i).ToString() });

                }
                ViewBag.MonthList = new SelectList(lstMonths, "Key", "Value", objViewScheduledTrainingsModel.FilterMonth);
                ViewBag.YearList = new SelectList(lstYears, "Key", "Value", objViewScheduledTrainingsModel.FilterYear);


                ViewBag.StatusLegend = objScheduledTrainingViewModel.GetResourceByResourceName("TrainingStatus");
                #endregion
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "ScheduledTraining", "ViewScheduledTraining Get", ex);
            }
            return View(objViewScheduledTrainingsModel);
        }

        /// <summary>
        /// Searching, paging,sorting on scheduled traing list and publish scheduled training functionality.
        /// </summary>
        /// <param name="objViewScheduledTrainingsModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult ViewScheduledTraining(ViewScheduledTrainingsModel objViewScheduledTrainingsModel)
        {
            
            try
            {
                objViewScheduledTrainingsModel.Message = string.Empty;
                objViewScheduledTrainingsModel.MessageType = string.Empty;
                ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
                if (objViewScheduledTrainingsModel.DisplayType == CommonUtils.DisplayType.List)
                {
                    if (objViewScheduledTrainingsModel.ActionType != null)
                    {
                        switch (objViewScheduledTrainingsModel.ActionType.ToLower())
                        {
                            case "search": objViewScheduledTrainingsModel.CurrentPage = 1;
                                break;
                            case "delete": if (objViewScheduledTrainingsModel.DeletedScheduledTrainingID > 0)
                                {
                                    objScheduledTrainingViewModel.DeleteScheduledTraining(objViewScheduledTrainingsModel);
                                    if (objViewScheduledTrainingsModel.ErrorCode.Equals(0))
                                    {
                                        objViewScheduledTrainingsModel.Message = Resources.TrainingResource.msgSchTrainingDeleteSuccess;
                                        objViewScheduledTrainingsModel.MessageType = MessageType.Success.ToString().ToLower();
                                    }
                                    else
                                    {
                                        objViewScheduledTrainingsModel.Message = Resources.TrainingResource.msgDeleteErrorMessage;
                                        objViewScheduledTrainingsModel.MessageType = MessageType.Error.ToString().ToLower();

                                    }
                                }
                                break;
                            case "changetrainingstaus":
                                if (objViewScheduledTrainingsModel.FilterTrainingStatus == CommonUtils.TrainingStatus.Published.ToString())
                                {
                                    objViewScheduledTrainingsModel.FilterTrainingStatus = CommonUtils.TrainingStatus.Non_Published.ToString().Replace("_", "-").ToString();
                                }
                                else
                                {
                                    objViewScheduledTrainingsModel.FilterTrainingStatus = CommonUtils.TrainingStatus.Published.ToString();
                                }
                                break;
                            case "publishtraining":
                                var selectedTrainings = string.Join(",", objViewScheduledTrainingsModel.ScheduledTrainings.Where(st => st.IsSelectedTraining == true).Select(st => st.ScheduledTrainingID.ToString()));
                                objScheduledTrainingViewModel.PublishScheduledTrainings(objViewScheduledTrainingsModel, selectedTrainings);
                                if (objViewScheduledTrainingsModel.ErrorCode.Equals(0))
                                {
                                    List<ScheduledTrainingDetail> lstScheduledTrainings = objViewScheduledTrainingsModel.ScheduledTrainings.Where(st => st.IsSelectedTraining == true).ToList();
                                    objViewScheduledTrainingsModel.Message = Resources.TrainingResource.msgPublishSuccess;
                                    objViewScheduledTrainingsModel.MessageType = MessageType.Success.ToString().ToLower();
                                    NotificationUtil.SendPublishCalendarNotification(lstScheduledTrainings);
                                    
                                }
                                else
                                {
                                    objViewScheduledTrainingsModel.Message = Resources.TrainingResource.msgPublishError;
                                    objViewScheduledTrainingsModel.MessageType = MessageType.Error.ToString().ToLower();

                                }
                                break;
                            case "showdetails":
                                objViewScheduledTrainingsModel.SelectedScheduledTraining = objViewScheduledTrainingsModel.ScheduledTrainings.Where(o => o.ScheduledTrainingID == objViewScheduledTrainingsModel.ScheduledTrainingDetailID).FirstOrDefault();
                                if (objViewScheduledTrainingsModel.SelectedScheduledTraining != null && objViewScheduledTrainingsModel.SelectedScheduledTraining.ScheduledTrainingID > 0)
                                {
                                    List<ProgramTrainingMappingDetailModel> lstProgramTrainings = objScheduledTrainingViewModel.GetProgramTrainingDetailByStID(objViewScheduledTrainingsModel.SelectedScheduledTraining.ScheduledTrainingID).ToList();
                                    objViewScheduledTrainingsModel.SelectedScheduledTraining.ProgramTrainings = lstProgramTrainings;
                                }
                                break;

                        }
                    }

                    objScheduledTrainingViewModel.GetScheduledTrainings(objViewScheduledTrainingsModel);
                }
                else if (objViewScheduledTrainingsModel.DisplayType == CommonUtils.DisplayType.Calendar)
                {
                    switch (objViewScheduledTrainingsModel.ActionType.ToLower())
                    {
                        case "changeview":
                        case "search":
                            if(objViewScheduledTrainingsModel.FilterMonth==0)
                            objViewScheduledTrainingsModel.FilterMonth = DateTime.Now.Month;
                            if(objViewScheduledTrainingsModel.FilterYear==0)
                            objViewScheduledTrainingsModel.FilterYear = DateTime.Now.Year;
                            objViewScheduledTrainingsModel.MonthStartDate = new DateTime(objViewScheduledTrainingsModel.FilterYear, objViewScheduledTrainingsModel.FilterMonth, 1);
                            break;
                        case "next":
                            objViewScheduledTrainingsModel.MonthStartDate = objViewScheduledTrainingsModel.MonthStartDate.AddMonths(1);
                            objViewScheduledTrainingsModel.FilterMonth = objViewScheduledTrainingsModel.MonthStartDate.Month;
                            objViewScheduledTrainingsModel.FilterYear = objViewScheduledTrainingsModel.MonthStartDate.Year;
                            break;
                        case "previous":
                              objViewScheduledTrainingsModel.MonthStartDate = objViewScheduledTrainingsModel.MonthStartDate.AddMonths(-1);
                            objViewScheduledTrainingsModel.FilterMonth = objViewScheduledTrainingsModel.MonthStartDate.Month;
                            objViewScheduledTrainingsModel.FilterYear = objViewScheduledTrainingsModel.MonthStartDate.Year;
                            break;
                        case "changetrainingstaus":
                            if (objViewScheduledTrainingsModel.FilterTrainingStatus == CommonUtils.TrainingStatus.Published.ToString())
                            {
                                objViewScheduledTrainingsModel.FilterTrainingStatus = CommonUtils.TrainingStatus.Non_Published.ToString().Replace("_", "-").ToString();
                            }
                            else
                            {
                                objViewScheduledTrainingsModel.FilterTrainingStatus = CommonUtils.TrainingStatus.Published.ToString();
                            }
                            break;

                    }
                   // objViewScheduledTrainingsModel.ScheduledTrainings = objScheduledTrainingViewModel.GetAllScheduledTrainings(objViewScheduledTrainingsModel);
                    objScheduledTrainingViewModel.GetScheduledTrainings(objViewScheduledTrainingsModel);
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "ScheduledTraining", "ViewScheduledTraining Post", ex);
            }
            return PartialView("ScheduledTrainingList", objViewScheduledTrainingsModel);
        }

        #endregion

        #region NewEditScheduledTraining
        /// <summary>
        /// Get New or Edit scheduled detail
        /// </summary>
        /// <param name="ScheduledTrainingID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult SaveScheduledTraining(int? stID)
        {
            ScheduledTrainingDetail objScheduledTrainingDetail = new ScheduledTrainingDetail();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            int schTrainingId;
            bool showAll = false;
            try
            {
                ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
                schTrainingId = stID != null ? (int)stID : 0;

                objScheduledTrainingDetail = schTrainingId != 0 ? objScheduledTrainingViewModel.GetScheduledTrainingByScheduledTrainingID(schTrainingId) : new ScheduledTrainingDetail();

                List<CircleModel> lstCircles = new List<CircleModel>();
                if (objScheduledTrainingDetail.PointID > 0)
                {
                    showAll = true;
                    lstCircles = objScheduledTrainingViewModel.GetCirclesByActiveAccess(createdBy, roleId,showAll,false).ToList();
                }
                else
                {
                    showAll = false;
                    lstCircles = objScheduledTrainingViewModel.GetCirclesByActiveAccess(createdBy, roleId, showAll, false).ToList();
                }
                
                
                List<CityModel> lstCities = new List<CityModel>();
                List<MaintenancePointModel> lstMaintenancePoitns = new List<MaintenancePointModel>();
                if (schTrainingId > 0)
                {
                    lstCities = objScheduledTrainingViewModel.GetCitiesByCircleID(createdBy, roleId,false,objScheduledTrainingDetail.SelectedCircles, false).ToList();
                    lstMaintenancePoitns = objScheduledTrainingViewModel.GetMaintenancePointByCityID(createdBy, roleId, objScheduledTrainingDetail.SelectedCities,false).ToList();
                    
                }
               
                
                objScheduledTrainingDetail.ProgramTrainings = objScheduledTrainingViewModel.GetProgramTrainingDetailByStID(schTrainingId).ToList();
               
                if(objScheduledTrainingDetail.PointID == 2)
                {
                    objScheduledTrainingDetail.CircleID = Convert.ToInt32(objScheduledTrainingDetail.MaintenancePointID);
                }

                if (String.IsNullOrEmpty(objScheduledTrainingDetail.SelectedMaintenancePoints))
                {
                    objScheduledTrainingDetail.SelectedMaintenancePoints = string.Empty;
                }
                if (String.IsNullOrEmpty(objScheduledTrainingDetail.SelectedCircles))
                {
                    objScheduledTrainingDetail.SelectedCircles = string.Empty;
                }
                if (String.IsNullOrEmpty(objScheduledTrainingDetail.SelectedCities))
                {
                    objScheduledTrainingDetail.SelectedCities = string.Empty;
                }
                ViewBag.CircleList = new MultiSelectList(lstCircles, "CircleID", "CircleName", objScheduledTrainingDetail.SelectedCircles.Split(",".ToCharArray()));
                ViewBag.CityList = new MultiSelectList(lstCities, "CityID", "CityFullName", objScheduledTrainingDetail.SelectedCities.Split(",".ToCharArray()));
                ViewBag.MaintenancePointList = new MultiSelectList(lstMaintenancePoitns, "MaintenancePointId", "MaintenancePointFullName", objScheduledTrainingDetail.SelectedMaintenancePoints.Split(",".ToCharArray()));
               // ViewBag.TrainerList = new SelectList(lstTrainer, "TrainerID", "FullName");
                FillTrainingsAndTrainers(objScheduledTrainingDetail);
                if (!String.IsNullOrEmpty(objScheduledTrainingDetail.ProgramStatusName) && (objScheduledTrainingDetail.ProgramStatusName.ToLower() == "published" || objScheduledTrainingDetail.ProgramStatusName.ToLower() == "in progress" || objScheduledTrainingDetail.ProgramStatusName.ToLower() == "completed"))
                {
                    objScheduledTrainingDetail.Message = String.Format(Resources.TrainingResource.msgScheduledNotEdit, objScheduledTrainingDetail.ProgramStatusName); ;
                    objScheduledTrainingDetail.MessageType = MessageType.Notice.ToString().ToLower();
                }
             }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "ScheduledTraining", "SaveScheduledTraining Get", ex);
            }
            return View("TrainingSchedule",objScheduledTrainingDetail);
        }

        /// <summary>
        /// Save the detail of Scheduled Training.
        /// </summary>
        /// <param name="objScheduledTrainingDetail"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult SaveScheduledTraining(ScheduledTrainingDetail objScheduledTrainingDetail)
        {
            ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
            objScheduledTrainingDetail.Message = string.Empty;
            objScheduledTrainingDetail.MessageType = string.Empty;
            if (objScheduledTrainingDetail.ProgramTrainings == null)
            {
                objScheduledTrainingDetail.ProgramTrainings = new List<ProgramTrainingMappingDetailModel>();
            }
            try
            {

                if (!String.IsNullOrEmpty(objScheduledTrainingDetail.ActionType))
                {
                    switch (objScheduledTrainingDetail.ActionType)
                    {
                        case "addnewitem": ProgramTrainingMappingDetailModel objProgramTrainingMappingDetailModel = new ProgramTrainingMappingDetailModel();
                            objScheduledTrainingDetail.lastInsertedId = objScheduledTrainingDetail.lastInsertedId - 1;
                            objProgramTrainingMappingDetailModel.TrainingID = objScheduledTrainingDetail.SelectedTrainingID;
                            objProgramTrainingMappingDetailModel.StartDateTime = objScheduledTrainingDetail.SelectedStartDate;
                            objProgramTrainingMappingDetailModel.EndDateTime = objScheduledTrainingDetail.SelectedEndDate;
                            objProgramTrainingMappingDetailModel.AssignedTrainerID = objScheduledTrainingDetail.SelectedTrainerID;
                            objProgramTrainingMappingDetailModel.TrainingName = objScheduledTrainingDetail.SelectedTrainingName;
                            objProgramTrainingMappingDetailModel.AssignedTrainerName = objScheduledTrainingDetail.SelectedTrainerName;
                            objProgramTrainingMappingDetailModel.ProgramTrainingMappingID = objScheduledTrainingDetail.lastInsertedId;
                            objProgramTrainingMappingDetailModel.TrainingCategoryName = objScheduledTrainingViewModel.GetTrainingCategoriesByID(objScheduledTrainingDetail.SelectedTrainingID);
                            objProgramTrainingMappingDetailModel.IsActive = true;
                            if (objScheduledTrainingDetail.SelectedTrainerType > 0)
                            {
                                objProgramTrainingMappingDetailModel.IsInternalTrainer = true;
                            }
                            else
                            {
                                objProgramTrainingMappingDetailModel.IsInternalTrainer = false;
                            }
                           
                            bool isConflict = objScheduledTrainingViewModel.isTrainingConflict(objScheduledTrainingDetail);
                            if (!isConflict)
                            {
                                objScheduledTrainingDetail.ProgramTrainings.Add(objProgramTrainingMappingDetailModel);
                            }
                            break;
                        case "deleteitem":
                            ProgramTrainingMappingDetailModel objProgTrainingMapping = new ProgramTrainingMappingDetailModel();
                            objProgTrainingMapping = objScheduledTrainingDetail.ProgramTrainings.Where(o => o.ProgramTrainingMappingID == objScheduledTrainingDetail.DeletedId).SingleOrDefault();
                            if (objProgTrainingMapping.ProgramTrainingMappingID > 0)
                            {
                                objProgTrainingMapping.IsActive = false;
                            }
                            else
                            {
                                objScheduledTrainingDetail.ProgramTrainings.Remove(objProgTrainingMapping);
                            }
                            break;
                        case "GetTrainings":
                            break;

                        case "saveschedule":
                            string strSuccessMessage;

                            //Get Point Type ID based on PointID
                            List<PointTypeModel> lstPointType = objScheduledTrainingViewModel.GetAllActivePointTypes().ToList();
                            ResourceModel objResourceModel = objScheduledTrainingViewModel.GetResourceByResourceName("TrainingStatus", "Non-Published").SingleOrDefault();
                            switch (objScheduledTrainingDetail.PointID)
                            {
                                case 0: objScheduledTrainingDetail.PointTypeID = lstPointType.Where(o => o.PointID == 0).Select(o => o.PointTypeID).SingleOrDefault();
                                    break;
                                case 1: objScheduledTrainingDetail.MaintenancePointID = null;
                                    objScheduledTrainingDetail.PointTypeID = lstPointType.Where(o => o.PointID == 1).Select(o => o.PointTypeID).SingleOrDefault();
                                    break;
                                case 2: objScheduledTrainingDetail.MaintenancePointID = objScheduledTrainingDetail.CircleID;
                                    objScheduledTrainingDetail.PointTypeID = lstPointType.Where(o => o.PointID == 2).Select(o => o.PointTypeID).SingleOrDefault();
                                    break;
                            }
                            objScheduledTrainingDetail.IsActive = true;
                            objScheduledTrainingDetail.CreatedOn = DateTime.Now;
                            objScheduledTrainingDetail.CreatedBy = createdBy;
                            if (objScheduledTrainingDetail.ScheduledTrainingID <= 0)
                            {
                                objScheduledTrainingDetail.TrainingStatus = objResourceModel.ResourceID;
                                strSuccessMessage = Resources.TrainingResource.msgSchTrainingSaveSuccess;
                            }
                            else
                            {
                                strSuccessMessage = Resources.TrainingResource.msgSchTrainingUdateSuccess;
                            }

                            int duration = objScheduledTrainingViewModel.GetDuration(objScheduledTrainingDetail.StartDateTime, objScheduledTrainingDetail.EndDateTime);
                            objScheduledTrainingDetail.TrainingDuration = duration;

                            string traineIDs = string.Join(",", objScheduledTrainingDetail.ProgramTrainings.Select(pt => pt.AssignedTrainerID.ToString()));

                            if (String.IsNullOrEmpty(traineIDs))
                            {
                                traineIDs = string.Empty;
                            }

                            IList<TrainerInfoModel> lstTrainersInfo = objScheduledTrainingViewModel.GetTrainersInformationByIds(traineIDs).ToList();

                           

                            foreach (ProgramTrainingMappingDetailModel objProTrainingMapping in objScheduledTrainingDetail.ProgramTrainings)
                            {
                                objProTrainingMapping.TrainingDuration = objScheduledTrainingViewModel.GetDuration(objProTrainingMapping.StartDateTime, objProTrainingMapping.EndDateTime);
                                //InActive Trainigns if Trainer assigned but Removed Maintenance point or Circle or City.
                                bool isTrainerValid = false;
                                if (lstTrainersInfo != null && lstTrainersInfo.Count > 0)
                                    {
                                        List<TrainerInfoModel> lstTrainerInformation = lstTrainersInfo.Where(o => o.TrainerID == objProTrainingMapping.AssignedTrainerID).ToList();
                                        foreach (TrainerInfoModel objTrainerInfoModel in lstTrainerInformation)
                                            { 
                                                if (objScheduledTrainingDetail.PointID == 0)
                                                {
                                                     if(objScheduledTrainingDetail.SelectedCircles.Split(',').Contains(objTrainerInfoModel.CircleID.ToString()) || objTrainerInfoModel.CircleID == 0)
                                                     {
                                                         isTrainerValid = true;
                                                     }
                                                }
                                                else if (objScheduledTrainingDetail.PointID == 1)
                                                {
                                                      isTrainerValid = true;
                                                }
                                                else if(objScheduledTrainingDetail.PointID == 2)
                                                {
                                                    if (objScheduledTrainingDetail.SelectedMaintenancePoints.Split(',').Contains(objTrainerInfoModel.CircleID.ToString()) || objTrainerInfoModel.CircleID == 0)
                                                    {
                                                        isTrainerValid = true;
                                                    }
                                                }
                                                
                                            }
                                }
                                if (!isTrainerValid)
                                {
                                    objProTrainingMapping.IsActive = false;
                                }

                            }
                          

                            string programTrainingMappingXML = CommonUtils.GetBulkXML(objScheduledTrainingDetail.ProgramTrainings);
                            objScheduledTrainingDetail = objScheduledTrainingViewModel.InsertUpdateScheduledTraining(objScheduledTrainingDetail, programTrainingMappingXML);

                            if (objScheduledTrainingDetail.ErrorCode.Equals(0))
                            {

                               int activeTrainingsCount =  objScheduledTrainingDetail.ProgramTrainings.Where(pt => pt.IsActive == true).Count();
                               if (activeTrainingsCount > 0)
                               {
                                   objScheduledTrainingDetail.Message = strSuccessMessage;
                                   objScheduledTrainingDetail.MessageType = MessageType.Success.ToString().ToLower();
                                   Session["ScheduledTrainingSuccess"] = strSuccessMessage;
                                   FillTrainingsAndTrainers(objScheduledTrainingDetail);
                                   string redirectUrl = Url.Action("ViewScheduledTraining", "ScheduledTraining");
                                   return Json(new { url = redirectUrl, message = objScheduledTrainingDetail.Message, messageType = objScheduledTrainingDetail.MessageType });
                               }
                               else
                               {
                                   objScheduledTrainingDetail.Message = Resources.TrainingResource.msgAddTraining;
                                   objScheduledTrainingDetail.MessageType = MessageType.Notice.ToString().ToLower();
                               }
                            }
                            else
                            {
                                objScheduledTrainingDetail.Message = HttpUtility.JavaScriptStringEncode(Resources.TrainingResource.msgSaveErrorMessage);
                                objScheduledTrainingDetail.MessageType = MessageType.Error.ToString().ToLower();

                            }
                            break;
                    }

                }
                FillTrainingsAndTrainers(objScheduledTrainingDetail);
                return PartialView("_ProgramTrainings", objScheduledTrainingDetail);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "ScheduledTraining", "SaveScheduledTraining POST", ex);
                var result = new { message = Resources.TrainingResource.msgSaveErrorMessage, messageType = MessageType.Error.ToString().ToLower() };
                return PartialView("_ProgramTrainings", objScheduledTrainingDetail);
            }
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult GetCircles(bool showAll)
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            int roleId = Convert.ToInt32(Session["RoleId"]);
            try
            {
                ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
                List<CircleModel> lstCircles = objScheduledTrainingViewModel.GetCirclesByActiveAccess(userId, roleId, showAll, true).ToList();
                SelectList objcircle = new SelectList(lstCircles, "CircleID", "CircleName", 0);
                return Json(objcircle);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(userId.ToString(), "ScheduledTraining", "GetCircles", ex);
                return Json(ex);
            }
           
        }

        /// <summary>
        /// Fill City by Circle id
        /// </summary>
        /// <param name="CircleID"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult GetCityByCircleID(string Circles)
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            int roleId = Convert.ToInt32(Session["RoleId"]);
            try
            {
                ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
                List<CityModel> lstCities = objScheduledTrainingViewModel.GetCitiesByCircleID(userId, roleId, false, Circles, false).ToList();
                MultiSelectList objcity = new MultiSelectList(lstCities, "CityID", "CityFullName", "");
                return Json(objcity);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(userId.ToString(), "ScheduledTraining", "GetCityByCircleID", ex);
                return Json(ex);
            }
        }

        /// <summary>
        /// Fill Maintenance point list by City Id
        /// </summary>
        /// <param name="CityID"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult GetMaintenancePointByCityID(string Cities)
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            int roleId = Convert.ToInt32(Session["RoleId"]);
            try
            {
                ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
                List<MaintenancePointModel> lstMaintenancePoint = objScheduledTrainingViewModel.GetMaintenancePointByCityID(userId, roleId, Cities, false).ToList();
                MultiSelectList objMaintenacePoint = new MultiSelectList(lstMaintenancePoint, "MaintenancePointId", "MaintenancePointFullName", "");
                return Json(objMaintenacePoint);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(userId.ToString(), "ScheduledTraining", "GetMaintenancePointByCityID", ex);
                return Json(ex);
            }
        }

        public void FillTrainingsAndTrainers(ScheduledTrainingDetail objScheduledTrainingDetail)
        {
            ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            List<TrainingModel> lstTraining = objScheduledTrainingViewModel.GetAllActiveTrainings(true,objScheduledTrainingDetail.SelectedTrainingCategory).ToList();
            List<TrainingCategoryModel> lstTrainingCategories = objTrainingViewModel.GetAllTrainingCategory(true).ToList();
            List<TrainerModel> lstTrainer = new List<TrainerModel>();
            string TrainingIds = string.Empty;
            
            if(objScheduledTrainingDetail.ProgramTrainings != null)
            {
                foreach (ProgramTrainingMappingDetailModel objProgramTrainingMappingDetail in objScheduledTrainingDetail.ProgramTrainings)
                {
                    TrainingModel objTrainingModel = lstTraining.Where(o => o.TrainingID == objProgramTrainingMappingDetail.TrainingID && objProgramTrainingMappingDetail.IsActive == true).SingleOrDefault();
                  if (objTrainingModel != null && objTrainingModel.TrainingID > 0)
                  {
                      lstTraining.Remove(objTrainingModel);
                  }
                }
            }
            if (lstTraining != null && lstTraining.Count > 0)
            {
                TrainingIds = string.Join(",", lstTraining.Select(t => t.TrainingID.ToString()));
            }
            //lstTrainer = objScheduledTrainingViewModel.GetTrainersByTrainingIDs(TrainingIds, objScheduledTrainingDetail.SelectedMaintenancePoints, true, null).ToList();
            objScheduledTrainingDetail.Trainers = lstTrainer;
            ViewBag.TrainingList = new SelectList(lstTraining, "TrainingID", "TrainingName", 0);
            ViewBag.TrainingCategoyList = new SelectList(lstTrainingCategories, "TrainingCategoryId", "TrainingCategoryName", objScheduledTrainingDetail.SelectedTrainingCategory);
            //ViewBag.SelectTrainerList = new SelectList(lstTrainer, "TrainerID", "FullName");
        }

        /// <summary>
        /// Get Trainings by Training Category ID
        /// </summary>
        /// <param name="TrainingCategoryID"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult GetTrainings(int TrainingCategoryID, ScheduledTrainingDetail scheduledTrainingDetail)
        {
            ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            int userId = Convert.ToInt32(Session["UserId"]);
            try
            {
                List<TrainingModel> lstTraining = objScheduledTrainingViewModel.GetAllActiveTrainings(false, TrainingCategoryID).ToList();
                SelectList objTraining = new SelectList(lstTraining, "TrainingID", "TrainingName", 0);
                return Json(objTraining);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(userId.ToString(), "ScheduledTraining", "GetTrainings", ex);
                return Json(ex);
            }
        }

        /// <summary>
        /// Fill Trainers drop down based on IsInternalTrainer flag. if true all internal Trainer will be filled else external trainer.
        /// </summary>
        /// <param name="IsInternalTrainer"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult GetTrainers(int selectedTraining, int trainerType, string maintenancePointIDs, int pointID)
        {
            ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
            int userId = Convert.ToInt32(Session["UserId"]);
            
            bool isInternal;
            if(trainerType >0)
            {
                isInternal = true;
            }
            else
            {
                isInternal = false;
            }
            try
            {
                int pointTypeID;
                List<PointTypeModel> lstPointType = objScheduledTrainingViewModel.GetAllActivePointTypes().ToList();
                pointTypeID = lstPointType.Where(o => o.PointID == pointID).Select(o => o.PointTypeID).SingleOrDefault();

                List<TrainerModel> lstTrainers = objScheduledTrainingViewModel.GetTrainersByTrainingIDs(Convert.ToString(selectedTraining), maintenancePointIDs, false,pointTypeID,isInternal).ToList();
                SelectList objTrainer = new SelectList(lstTrainers, "TrainerID", "FullName", 0);
                return Json(objTrainer);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(userId.ToString(), "ScheduledTraining", "GetTrainers", ex);
                return Json(ex);
            }
        }

        #endregion


        #region Training Calendar
        [Filters.Authorized()]
        public ActionResult TrainingCalendar()
        {
            ViewTrainingCalendar objTrainingCalendar = new ViewTrainingCalendar();
           
            objTrainingCalendar.FilterMonth =DateTime.Now.Month;
            objTrainingCalendar.FilterYear =DateTime.Now.Year;
            List<KeyValueModel> lstMonths = new List<KeyValueModel>();
            List<KeyValueModel> lstYears = new List<KeyValueModel>();
            for (int i = 0; i < 12; i++)
            {
                lstMonths.Add(new KeyValueModel() { Key = i + 1, Value = CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames[i] });
                lstYears.Add(new KeyValueModel() { Key = ((DateTime.Now.Year - 6) + i), Value = ((DateTime.Now.Year - 6) + i).ToString() });
                
            }
            ViewBag.MonthList = new SelectList(lstMonths, "Key", "Value", objTrainingCalendar.FilterMonth);
            ViewBag.YearList = new SelectList(lstYears, "Key", "Value", objTrainingCalendar.FilterYear);


            objTrainingCalendar.MonthStartDate = new DateTime(objTrainingCalendar.FilterYear, objTrainingCalendar.FilterMonth, 1);

            //objTrainingCalendar.FilterFromDate = objTrainingCalendar.MonthStartDate;
            //objTrainingCalendar.FilterToDate = objTrainingCalendar.MonthStartDate.AddMonths(1).AddDays(-1);
            
            ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
            objTrainingCalendar.ScheduledTrainings = objScheduledTrainingViewModel.GetAllScheduledTrainings(objTrainingCalendar);
            ViewBag.StatusLegend = objScheduledTrainingViewModel.GetResourceByResourceName("TrainingStatus");
            return View(objTrainingCalendar);
        }

        [HttpPost]
        [Filters.Authorized()]
        public ActionResult TrainingCalendar(ViewTrainingCalendar objTrainingCalendar)
        {
            if (objTrainingCalendar.ActionType != null && (objTrainingCalendar.ActionType.ToLower().Equals("next") || objTrainingCalendar.ActionType.ToLower().Equals("previous")))
            {
                int month = 0;
               
                    switch (objTrainingCalendar.ActionType.ToLower())
                    {
                        case "next":
                            month = 1;
                            break;
                        case "previous":
                            month = -1;
                            break;
                    }
              
                objTrainingCalendar.MonthStartDate = objTrainingCalendar.MonthStartDate.AddMonths(month);
                objTrainingCalendar.FilterMonth = objTrainingCalendar.MonthStartDate.Month;
                objTrainingCalendar.FilterYear = objTrainingCalendar.MonthStartDate.Year;
            }
            else 
            {
            objTrainingCalendar.MonthStartDate = new DateTime(objTrainingCalendar.FilterYear, objTrainingCalendar.FilterMonth, 1);
            }
            //objTrainingCalendar.FilterFromDate = objTrainingCalendar.MonthStartDate;
            //objTrainingCalendar.FilterToDate = objTrainingCalendar.MonthStartDate.AddMonths(1).AddDays(-1);

            ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
            objTrainingCalendar.ScheduledTrainings = objScheduledTrainingViewModel.GetAllScheduledTrainings(objTrainingCalendar);
            return PartialView("MonthCalendar", objTrainingCalendar);
        }
         #endregion
    }
}
