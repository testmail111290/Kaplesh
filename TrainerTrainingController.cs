using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Models;
using CTMS.ViewModel;
using CTMS.Common;
using System.Globalization;
using System.Collections;
using System.IO;
using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.html;
using iTextSharp.text.pdf;
using CTMS.Resources;

namespace CTMS.Controllers
{
    public class TrainerController : Controller
    {

        TrainerViewModel objTrainerViewModel = new TrainerViewModel();
        TrainingViewModel objTrainingViewModel = new TrainingViewModel();
        public int UserId
        {
            get
            {
                int _userID = 0;
                if (Session["UserId"] != null)
                    int.TryParse(Convert.ToString(Session["UserId"]), out _userID);
                return _userID;
            }
        }
        public int RoleId
        {
            get
            {
                int _RoleId = 0;
                if (Session["RoleId"] != null)
                    int.TryParse(Convert.ToString(Session["RoleId"]), out _RoleId);
                return _RoleId;
            }
        }

        CommonUtils objCommonUtilError = new CommonUtils();
        /// <summary>
        /// Get Training of Trainer 
        /// </summary>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult ViewTrainingList()
        {
            ViewTrainerTrainingsModel objViewTrainerTrainingModel = new ViewTrainerTrainingsModel();
            try
            {
                //get Today training list 
                objViewTrainerTrainingModel.FilterFromDate = System.DateTime.Now.Date;
                objViewTrainerTrainingModel.FilterToDate = null;
                objViewTrainerTrainingModel.CurrentPage = 1;
                objViewTrainerTrainingModel.PageSize = CommonUtils.PageSize;
                objViewTrainerTrainingModel.TotalPages = 0;
                FillFilter(objViewTrainerTrainingModel);
                objViewTrainerTrainingModel.IsGetOldTraining = false;
                objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId, true);
                //objViewTrainerTrainingModel.ScheduledTrainings = (from o in objViewTrainerTrainingModel.ScheduledTrainings
                //                                                  where o.TrainingStatusName == CTMS.Common.CommonUtils.TrainingStatus.Published.ToString()
                //                                                  select o).ToList();
                #region Calendar
                List<KeyValueModel> lstMonths = new List<KeyValueModel>();
                List<KeyValueModel> lstYears = new List<KeyValueModel>();
                for (int i = 0; i < 12; i++)
                {
                    lstMonths.Add(new KeyValueModel() { Key = i + 1, Value = CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames[i] });
                    lstYears.Add(new KeyValueModel() { Key = ((DateTime.Now.Year - 6) + i), Value = ((DateTime.Now.Year - 6) + i).ToString() });

                }
                ViewBag.MonthList = new SelectList(lstMonths, "Key", "Value", objViewTrainerTrainingModel.FilterMonth);
                ViewBag.YearList = new SelectList(lstYears, "Key", "Value", objViewTrainerTrainingModel.FilterYear);

                ViewBag.StatusLegend = objTrainerViewModel.GetResourceByResourceName("TrainingStatus");

                #endregion
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "ViewTrainingList Get", ex);
            }




            return View("TrainingList", objViewTrainerTrainingModel);
        }
        /// <summary>
        /// Post Trainer Training Model
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewTrainingList(ViewTrainerTrainingsModel objViewTrainerTrainingModel)
        {
            try
            {
                FillFilter(objViewTrainerTrainingModel);
                TrainerViewModel objTrainerViewModel = new TrainerViewModel();
                if (objViewTrainerTrainingModel.DisplayType == CommonUtils.DisplayType.List)
                {
                    //                    if (objViewTrainerTrainingModel.ActionType != null && objViewTrainerTrainingModel.ActionType == "history")

                    if (objViewTrainerTrainingModel.ActionType != null && objViewTrainerTrainingModel.ActionType == "search")
                    {

                        objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId, true);
                        objViewTrainerTrainingModel.ActionType = null;
                        return PartialView("_TrainerTrainingDetails", objViewTrainerTrainingModel);
                    }
                    else if (objViewTrainerTrainingModel.ActionType != null && objViewTrainerTrainingModel.ActionType == "reset")
                    {
                        if (objViewTrainerTrainingModel.FilterFromDate == null || objViewTrainerTrainingModel.FilterFromDate == System.DateTime.MinValue || objViewTrainerTrainingModel.FilterToDate == null || objViewTrainerTrainingModel.FilterToDate == System.DateTime.MinValue)
                        {
                            if (objViewTrainerTrainingModel.IsGetOldTraining)
                            {
                                objViewTrainerTrainingModel.FilterFromDate = null;
                                objViewTrainerTrainingModel.FilterToDate = System.DateTime.Now.Date;
                            }
                            else
                            {
                                objViewTrainerTrainingModel.FilterFromDate = System.DateTime.Now.Date;
                                objViewTrainerTrainingModel.FilterToDate = null;
                            }

                        }



                        objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId, true);
                        objViewTrainerTrainingModel.ActionType = null;
                        return PartialView("_TrainerTrainingDetails", objViewTrainerTrainingModel);
                    }
                    ////Forcefully complete training 
                    //    else if (objViewTrainerTrainingModel.ActionType != null && objViewTrainerTrainingModel.ActionType == "completetraining")
                    //    {
                    //        objViewTrainerTrainingModel.CreatedBy = UserId;
                    //        objViewTrainerTrainingModel.CreatedOn = System.DateTime.Now;
                    //        objViewTrainerTrainingModel = objTrainerViewModel.ForceFullyCompleteTraining(objViewTrainerTrainingModel);
                    //        objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId,true);
                    //        return PartialView("_TrainerTrainingDetails", objViewTrainerTrainingModel);
                    //    }

                    //Complete training 
                    else if (objViewTrainerTrainingModel.ActionType != null && objViewTrainerTrainingModel.ActionType == "completetraining")
                    {
                        objViewTrainerTrainingModel.CreatedBy = UserId;
                        objViewTrainerTrainingModel.CreatedOn = System.DateTime.Now;
                        objViewTrainerTrainingModel = objTrainerViewModel.CompleteTraining(objViewTrainerTrainingModel);
                        //Send notification to program manager and manager for complete training.
                        //Send notification when Program completed.
                        if (objViewTrainerTrainingModel.IsProgramComplete == true)
                        {
                            NotificationUtil.SendTrainerCompleteTrainingNotification(objViewTrainerTrainingModel);
                            NotificationUtil.SendTrainerCompleteProgramNotification(objViewTrainerTrainingModel);
                        }
                        //Send notification when training completed.
                        else
                        {
                            NotificationUtil.SendTrainerCompleteTrainingNotification(objViewTrainerTrainingModel);
                        }

                        objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId, true);
                        objViewTrainerTrainingModel.ActionType = string.Empty;
                        return PartialView("_TrainerTrainingDetails", objViewTrainerTrainingModel);
                    }
                    else if (objViewTrainerTrainingModel.ActionType != null && objViewTrainerTrainingModel.ActionType == "changeview")
                    {
                        objViewTrainerTrainingModel.FilterFromDate = System.DateTime.Now.Date;
                        objViewTrainerTrainingModel.FilterToDate = null;
                        objViewTrainerTrainingModel.IsGetOldTraining = false;
                        objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId, true);
                        return PartialView("_TrainerTrainingDetails", objViewTrainerTrainingModel);
                    }
                    else if (objViewTrainerTrainingModel.IsGetOldTraining == true)
                    {
                        objViewTrainerTrainingModel.FilterFromDate = null;
                        objViewTrainerTrainingModel.FilterToDate = System.DateTime.Now.Date;
                        objViewTrainerTrainingModel.IsGetOldTraining = true;
                        objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId, true);
                        return PartialView("_TrainerTrainingDetails", objViewTrainerTrainingModel);
                    }
                    else if (objViewTrainerTrainingModel.IsGetOldTraining == false)
                    {
                        objViewTrainerTrainingModel.FilterFromDate = System.DateTime.Now.Date;
                        objViewTrainerTrainingModel.FilterToDate = null;
                        objViewTrainerTrainingModel.IsGetOldTraining = false;
                        objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId, true);
                        return PartialView("_TrainerTrainingDetails", objViewTrainerTrainingModel);
                    }
                }
                else if (objViewTrainerTrainingModel.DisplayType == CommonUtils.DisplayType.Calendar)
                {
                    switch (objViewTrainerTrainingModel.ActionType.ToLower())
                    {
                        case "changeview":
                        case "search":
                        case "reset":
                            if (objViewTrainerTrainingModel.FilterMonth == 0)
                                objViewTrainerTrainingModel.FilterMonth = DateTime.Now.Month;
                            if (objViewTrainerTrainingModel.FilterYear == 0)
                                objViewTrainerTrainingModel.FilterYear = DateTime.Now.Year;
                            objViewTrainerTrainingModel.MonthStartDate = new DateTime(objViewTrainerTrainingModel.FilterYear, objViewTrainerTrainingModel.FilterMonth, 1);
                            break;
                        case "next":
                            objViewTrainerTrainingModel.MonthStartDate = objViewTrainerTrainingModel.MonthStartDate.AddMonths(1);
                            objViewTrainerTrainingModel.FilterMonth = objViewTrainerTrainingModel.MonthStartDate.Month;
                            objViewTrainerTrainingModel.FilterYear = objViewTrainerTrainingModel.MonthStartDate.Year;
                            break;
                        case "previous":
                            objViewTrainerTrainingModel.MonthStartDate = objViewTrainerTrainingModel.MonthStartDate.AddMonths(-1);
                            objViewTrainerTrainingModel.FilterMonth = objViewTrainerTrainingModel.MonthStartDate.Month;
                            objViewTrainerTrainingModel.FilterYear = objViewTrainerTrainingModel.MonthStartDate.Year;
                            break;

                    }
                    objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId, false);
                    return PartialView("_TrainerTrainingDetails", objViewTrainerTrainingModel);
                }
                objTrainerViewModel.GetAllTrainerTrainings(objViewTrainerTrainingModel, UserId, RoleId, true);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "ViewTrainingList Post", ex);
            }
            return PartialView("_TrainerTrainingDetails", objViewTrainerTrainingModel);

        }
        /// <summary>
        /// Get Selected Training Details
        /// </summary>
        /// <param name="SelectedScheduledTrainingID"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetSelectedTrainingDetail(int SelectedScheduledTrainingID, int TrainingID)
        {
            ViewTrainerTrainingsModel objViewTrainerTrainingModel = new ViewTrainerTrainingsModel();
            try
            {
                TrainerViewModel objTrainerViewModel = new TrainerViewModel();
                objViewTrainerTrainingModel = objTrainerViewModel.GetSelectedTrainingDetail(SelectedScheduledTrainingID, TrainingID);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "GetSelectedTrainingDetail Get", ex);
            }
            return PartialView("_SelectedTrainingDetail", objViewTrainerTrainingModel.SelectedTraining);
        }


        //[ChildActionOnly]
        /// <summary>
        /// Get Candidate Attendance details
        /// </summary>
        /// <param name="scheduledtrainingid"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult UpdateAttendance(int scheduledtrainingid, int trainingid)
        {
            UpdateAttendanceModel objUpdateAttendanceModel = new UpdateAttendanceModel();
            try
            {
                objUpdateAttendanceModel.ScheduledTrainingId = scheduledtrainingid;
                objUpdateAttendanceModel.TrainingID = trainingid;
                objUpdateAttendanceModel.CreatedBy = UserId;
                objUpdateAttendanceModel.TrainerID = UserId;
                TrainerViewModel objTrainerViewModel = new TrainerViewModel();
                objUpdateAttendanceModel.CurrentPage = 1;
                objUpdateAttendanceModel.PageSize = CommonUtils.PageSize;
                objUpdateAttendanceModel.TotalPages = 0;
                objTrainerViewModel.GetCandidateAttendance(objUpdateAttendanceModel);

                objUpdateAttendanceModel.IsTrainingComplete = objTrainerViewModel.CheckTrainingComplete(scheduledtrainingid, trainingid);
                var IsTrainingComplete = objUpdateAttendanceModel.IsTrainingComplete;
                if (IsTrainingComplete)
                {
                    objUpdateAttendanceModel.Message = Resources.TrainingResource.msgTrainingAlreadyComplete.ToString();
                    objUpdateAttendanceModel.MessageType = MessageType.Notice.ToString().ToLower();
                    objUpdateAttendanceModel.IsTrainingComplete = IsTrainingComplete;

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "UpdateAttendance Get", ex);
            }
            return View(objUpdateAttendanceModel);
        }

        /// <summary>
        /// Save attendance and paging 
        /// </summary>
        /// <param name="objPostUpdateAttendanceModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        public ActionResult UpdateAttendance(UpdateAttendanceModel objPostUpdateAttendanceModel)
        {
            TrainerViewModel objTrainerViewModel = new TrainerViewModel();
            try
            {
                //
                if (objPostUpdateAttendanceModel.ActionType != null)
                {

                    switch (objPostUpdateAttendanceModel.ActionType)
                    {
                        case "pagechange":
                            objTrainerViewModel.GetCandidateAttendance(objPostUpdateAttendanceModel);

                            for (int iCA = 0; iCA < objPostUpdateAttendanceModel.CandidateAttendance.Count; iCA++)
                            {
                                for (int iCD = 0; iCD < objPostUpdateAttendanceModel.CandidateAttendance[iCA].CandidateAttendanceDetails.Count; iCD++)
                                {
                                    if (!String.IsNullOrEmpty(objPostUpdateAttendanceModel.SelectedCandidateList))
                                    {
                                        if (objPostUpdateAttendanceModel.SelectedCandidateList.Split(',').ToList().Contains(objPostUpdateAttendanceModel.CandidateAttendance[iCA].CandidateId.ToString() + "_" + objPostUpdateAttendanceModel.CandidateAttendance[iCA].CandidateAttendanceDetails[iCD].AttendDate.ToShortDateString()))
                                        {
                                            objPostUpdateAttendanceModel.CandidateAttendance[iCA].CandidateAttendanceDetails[iCD].IsAttended = true;
                                        }
                                    }
                                    if (!String.IsNullOrEmpty(objPostUpdateAttendanceModel.UnSelectedCandidateList))
                                    {
                                        if (objPostUpdateAttendanceModel.UnSelectedCandidateList.Split(',').ToList().Contains(objPostUpdateAttendanceModel.CandidateAttendance[iCA].CandidateId.ToString() + "_" + objPostUpdateAttendanceModel.CandidateAttendance[iCA].CandidateAttendanceDetails[iCD].AttendDate.ToString()))
                                        {
                                            objPostUpdateAttendanceModel.CandidateAttendance[iCA].CandidateAttendanceDetails[iCD].IsAttended = false;
                                        }
                                    }
                                }
                            }
                            return PartialView("_UpdateAttendanceList", objPostUpdateAttendanceModel);

                        case "save":


                            objPostUpdateAttendanceModel.CreatedBy = UserId;
                            objPostUpdateAttendanceModel.TrainerID = UserId;
                            // update candidate attendance
                            objTrainerViewModel.UpdateAttendance(objPostUpdateAttendanceModel);

                            objPostUpdateAttendanceModel.SelectedCandidateList = string.Empty;
                            objPostUpdateAttendanceModel.UnSelectedCandidateList = string.Empty;

                            // get candidate attendance
                            objTrainerViewModel.GetCandidateAttendance(objPostUpdateAttendanceModel);
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "UpdateAttendance Post", ex);
            }
            return PartialView("_UpdateAttendanceList", objPostUpdateAttendanceModel);
        }
        /// <summary>
        /// Get Circle list user wise
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        private List<CircleModel> GetCirles(int UserId, int RoleId)
        {
            List<CircleModel> lstCircles = new List<CircleModel>();
            try
            {
                MasterViewModel objMasterViewModel = new MasterViewModel();
                lstCircles = objMasterViewModel.GetUserwiseCircles(UserId, RoleId).ToList<CircleModel>();

                if (lstCircles == null)
                    lstCircles = new List<CircleModel>();

                lstCircles.Insert(0, new CircleModel { CircleID = 0, CircleName = "--ALL--" });
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "GetCirles Get", ex);
            }
            return lstCircles;
        }
        /// <summary>
        /// Get City list by user, role and circle wise
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="RoleId"></param>
        /// <param name="CircleId"></param>
        /// <returns></returns>
        private List<CityModel> GetCity(int UserId, int RoleId, int CircleId)
        {
            List<CityModel> lstCitys = new List<CityModel>();
            try
            {
                MasterViewModel objMasterViewModel = new MasterViewModel();
                lstCitys = objMasterViewModel.GetUserwiseCity(UserId, RoleId, CircleId).ToList<CityModel>();

                if (lstCitys == null)
                    lstCitys = new List<CityModel>();

                lstCitys.Insert(0, new CityModel { CityID = 0, CityName = "--ALL--" });
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "GetCity Get", ex);
            }
            return lstCitys;
        }

        /// <summary>
        /// Get Maintanence detail by user, role, circle and city wise
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="RoleId"></param>
        /// <param name="CircleId"></param>
        /// <param name="CityId"></param>
        /// <returns></returns>
        private List<MaintenancePointModel> GetMaintanence(int UserId, int RoleId, int CircleId, int CityId)
        {
            List<MaintenancePointModel> lstMaintanences = new List<MaintenancePointModel>();
            try
            {
                MasterViewModel objMasterViewModel = new MasterViewModel();
                lstMaintanences = objMasterViewModel.GetUserwiseMaintenancePoint(UserId, RoleId, CircleId, CityId).ToList<MaintenancePointModel>();

                if (lstMaintanences == null)
                    lstMaintanences = new List<MaintenancePointModel>();

                lstMaintanences.Insert(0, new MaintenancePointModel { MaintenancePointId = 0, MaintenancePoint = "--ALL--" });
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "GetMaintanence Get", ex);
            }
            return lstMaintanences;
        }

        /// <summary>
        /// Fill Filter details
        /// </summary>
        /// <param name="objViewTrainerTrainingModel"></param>
        public void FillFilter(ViewTrainerTrainingsModel objViewTrainerTrainingModel)
        {
            try
            {
                List<TrainerCategoryModel> TrainerCategory = objTrainingViewModel.GetAllTrainerCategory(UserId).ToList();
                List<TrainingTypeModel> TrainingTypes = objTrainingViewModel.GetAllActiveTrainigType(true).ToList();
                List<CircleModel> Circles = this.GetCirles(UserId, RoleId);
                List<CityModel> Cities = new List<CityModel>();
                List<MaintenancePointModel> MaintanencePoints = new List<MaintenancePointModel>();
                //Circles.Insert(0, new CircleModel { CircleID=0, CircleName="--Select--" });
                Cities.Insert(0, new CityModel { CityID = 0, CityName = "--Select--" });
                MaintanencePoints.Insert(0, new MaintenancePointModel { MaintenancePointId = 0, MaintenancePoint = "--Select--" });

                //objViewTrainerTrainingModel.CurrentPage = 1;
                //objViewTrainerTrainingModel.PageSize = CommonUtils.PageSize;
                //objViewTrainerTrainingModel.TotalPages = 0;
                objViewTrainerTrainingModel.IsReadOnly = true;
                ViewBag.TrainerCategoryList = new SelectList(TrainerCategory, "TrainingCategoryId", "TrainingCategoryName", null);
                ViewBag.TrainingTypeList = new SelectList(TrainingTypes, "TrainingTypeID", "TrainingTypeName", null);
                ViewBag.CircleList = new SelectList(Circles, "CircleID", "CircleName", null);
                ViewBag.CityList = new SelectList(Cities, "CityID", "CityName", null);
                ViewBag.MaintanencePointList = new SelectList(MaintanencePoints, "MaintenancePointId", "MaintenancePoint", null);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "FillFilter Get", ex);
            }
        }

        /// <summary>
        /// Fill maintenance detail 
        /// </summary>
        /// <param name="ParentID"></param>
        /// <param name="DDLType"></param>
        /// <param name="CircleId"></param>
        /// <returns></returns>
        public ActionResult FillMaintenance(int ParentID = 0, string DDLType = "", int CircleId = 0)
        {
            try
            {
                switch (DDLType)
                {
                    case "City":
                        List<CityModel> Cities = this.GetCity(UserId, RoleId, ParentID);
                        var classesData = Cities.Select(m => new SelectListItem()
                        {
                            Text = m.CityName,
                            Value = m.CityID.ToString(),
                        });
                        return Json(classesData, JsonRequestBehavior.AllowGet);

                    case "Maintanence":
                        List<MaintenancePointModel> MaintanencePoints = this.GetMaintanence(UserId, RoleId, CircleId, ParentID);
                        var lstMaintanencePoints = MaintanencePoints.Select(m => new SelectListItem()
                        {
                            Text = m.MaintenancePoint,
                            Value = m.MaintenancePointId.ToString(),
                        });
                        return Json(lstMaintanencePoints, JsonRequestBehavior.AllowGet);

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "FillMaintenance Get", ex);
            }
            return Json("", JsonRequestBehavior.AllowGet);


        }




        public ActionResult StartTrainingTime()
        {
            //show time when your training started
            var TrainingStartTime = string.Format(TrainerResource.msgTrainingStart, System.DateTime.Now.ToString());

            return Json(TrainingStartTime, JsonRequestBehavior.AllowGet);

        }


        /// <summary>
        /// Start training topic wise 
        /// </summary>
        /// <param name="ScheduledTrainingId"></param>
        /// <returns></returns>
        /// 
        [Filters.MenuAccess()]

        public ActionResult StartTraining(int SID, int TID, int TNRID, Boolean ISF)
        {
            StartTrainingModel objStartTrainingModel = new StartTrainingModel();
            try
            {

                //Added Start time in training study database table 
                //Training start time save into database
                //  ScheduledTrainingDetail objScheduledTrainingDetail = new ScheduledTrainingDetail();
                objStartTrainingModel.IsTrainingComplete = objTrainerViewModel.CheckTrainingComplete(SID, TID);
                var IsTrainingComplete = objStartTrainingModel.IsTrainingComplete;
                if (!IsTrainingComplete)
                {

                    objStartTrainingModel = SaveTrainingTime(SID, TID, TNRID, null, null, ISF);

                }
                objStartTrainingModel.ScheduledTrainingID = SID;
                objStartTrainingModel.TrainerID = TNRID;
                objStartTrainingModel.TrainingID = TID;
                objStartTrainingModel = objTrainerViewModel.GetScheduledTrainingWithSpendTimeByID(objStartTrainingModel);
                //Get Topic detail of training 
                //      TopicViewModel objTopicViewModel = new TopicViewModel();

                List<TopicModel> lstTopicModel = objTrainerViewModel.GetTopicWithSpendTimeByTrainingID(objStartTrainingModel).ToList();
                objStartTrainingModel.lstTopicModel = lstTopicModel.OrderBy(x => x.TopicOrderNo).ToList();
                ViewBag.TimerTickValue = CommonUtils.TimerTickValue;
                if (IsTrainingComplete)
                {
                    objStartTrainingModel.Message = Resources.TrainingResource.msgTrainingAlreadyComplete.ToString();
                    objStartTrainingModel.MessageType = MessageType.Notice.ToString().ToLower();
                    objStartTrainingModel.IsTrainingComplete = IsTrainingComplete;

                }



            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "StartTraining Post", ex);
            }
            return View("StartTraining", objStartTrainingModel);
        }

        /// <summary>
        /// Save training Time 
        /// </summary>
        /// <param name="ScheduledTrainingID"></param>
        /// <param name="TrainingID"></param>
        /// <param name="TrainerID"></param>
        /// <param name="StartDateTime"></param>
        /// <param name="EndDateTime"></param>
        /// <param name="IsFinished"></param>
        /// <returns></returns>
        public StartTrainingModel SaveTrainingTime(int ScheduledTrainingID, int TrainingID, int TrainerID, DateTime? StartDateTime, DateTime? EndDateTime, Boolean IsFinished)
        {

            StartTrainingModel objStartTrainingModel = new StartTrainingModel();
            try
            {
                objStartTrainingModel.ScheduledTrainingID = ScheduledTrainingID;
                objStartTrainingModel.TrainerID = TrainerID;
                objStartTrainingModel.TrainingID = TrainingID;
                objStartTrainingModel.CreatedBy = UserId;
                objStartTrainingModel.StartDateTime = StartDateTime;
                objStartTrainingModel.EndDateTime = EndDateTime;
                objStartTrainingModel.IsFinished = IsFinished;
                objStartTrainingModel.IsTrainingClosed = false;
                objStartTrainingModel = objTrainerViewModel.InsertUpdateTrainingStartTime(objStartTrainingModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "SaveTrainingTime Post", ex);
            }
            return objStartTrainingModel;

        }
        [Filters.Authorized()]

        public ActionResult TrainingTimeTick(int ScheduledTrainingID, int TrainingID, int TrainerID, int IsWindowClosed, Boolean IsFinished)
        {
            StartTrainingModel objStartTrainingModel = new StartTrainingModel();
            try
            {
                if (UserId > 0)
                {
                    objStartTrainingModel.ScheduledTrainingID = ScheduledTrainingID;
                    objStartTrainingModel.TrainerID = TrainerID;
                    objStartTrainingModel.TrainingID = TrainingID;
                    objStartTrainingModel.CreatedBy = UserId;
                    objStartTrainingModel.IsFinished = IsFinished;
                    if (IsFinished == false)
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
                    objStartTrainingModel = objTrainerViewModel.InsertUpdateTrainingStartTime(objStartTrainingModel);
                    ViewBag.TimerTickValue = CommonUtils.TimerTickValue;
                    Session.Timeout = 15;

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "TrainingTimeTick Post", ex);
            }
            return Json(objStartTrainingModel.SpendTime, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TopicStart(int TopicID, int TrainingID, int TopicStatus, int IsWindowClosed, Boolean IsTopicFinish, int ScheduledTrainingID, int TrainerID, Boolean IsTrainingCompleted, string TopicStatusName)
        {

            var objStartTrainingModel = new StartTrainingModel();
            try
            {
                objStartTrainingModel.TopicID = TopicID;
                objStartTrainingModel.TopicStatus = TopicStatus;
                objStartTrainingModel.TrainingID = TrainingID;

                objStartTrainingModel.ScheduledTrainingID = ScheduledTrainingID;
                objStartTrainingModel.TrainingID = TrainingID;
                objStartTrainingModel.TrainerID = TrainerID;
                objStartTrainingModel.CreatedBy = UserId;
                objStartTrainingModel = objTrainerViewModel.GetTopicWithSpendTimeByIDs(objStartTrainingModel);
                objStartTrainingModel.TopicID = TopicID;
                objStartTrainingModel.TopicStatus = TopicStatus;
                objStartTrainingModel.TrainingID = TrainingID;
                objStartTrainingModel.ScheduledTrainingID = ScheduledTrainingID;
                objStartTrainingModel.TrainerID = TrainerID;
                objStartTrainingModel.CreatedBy = UserId;
                objStartTrainingModel.IsFinished = IsTopicFinish;
                objStartTrainingModel.StartDateTime = null; //null;

                objStartTrainingModel.EndDateTime = null;
                if (IsTrainingCompleted == false)
                {
                    if (TopicStatusName != "completed")
                        objStartTrainingModel = objTrainerViewModel.InsertUpdateTopicTime(objStartTrainingModel);
                }
                else
                    objStartTrainingModel.IsFinished = IsTrainingCompleted;
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "TopicStart Post", ex);
            }
            return PartialView("_ViewTopic", objStartTrainingModel);
        }

        public ActionResult TopicTimeTick(int TopicID, int ScheduledTrainingID, int TrainingID, int TrainerID, int TopicStatus, int IsWindowClosed, Boolean IsTopicFinish)
        {
            StartTrainingModel objStartTrainingModel = new StartTrainingModel();
            try
            {
                objStartTrainingModel.TopicID = TopicID;
                objStartTrainingModel.ScheduledTrainingID = ScheduledTrainingID;
                objStartTrainingModel.TrainingID = TrainingID;
                objStartTrainingModel.TrainerID = TrainerID;
                objStartTrainingModel.TopicStatus = TopicStatus;
                objStartTrainingModel.CreatedBy = UserId;
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
                objStartTrainingModel = objTrainerViewModel.InsertUpdateTopicTime(objStartTrainingModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "TopicTimeTick Post", ex);
            }
            // return Json(objStartTrainingModel, JsonRequestBehavior.AllowGet);
            return Json(new { TopicStatus = objStartTrainingModel.TopicStatus }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult UpdateTopicList(int TrainingID, int TopicID, int ScheduledTrainingID, int TrainerID)
        {
            StartTrainingModel objStartTrainingModel = new StartTrainingModel();
            try
            {
                //Get Topic detail of training 
                objStartTrainingModel.TrainingID = TrainingID;
                objStartTrainingModel.ScheduledTrainingID = ScheduledTrainingID;
                objStartTrainingModel.TrainerID = TrainerID;
                List<TopicModel> lstTopicModel = objTrainerViewModel.GetTopicWithSpendTimeByTrainingID(objStartTrainingModel).ToList();
                objStartTrainingModel.lstTopicModel = lstTopicModel.OrderBy(x => x.TopicOrderNo).ToList();
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "UpdateTopicList Post", ex);
            }
            return PartialView("_TrainingTopicList", objStartTrainingModel);
        }


        public ActionResult CheckTraining(int SID, int TID)
        {
            TrainingCompleteModel objTrainingCompleteModel = new TrainingCompleteModel();
            try
            {
                TrainerViewModel objTrainerViewModel = new TrainerViewModel();
                objTrainingCompleteModel = objTrainerViewModel.CheckTrainingBeforeComplete(SID, TID);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "CheckTraining Post", ex);
            }
            return PartialView("_CheckTrainingComplete", objTrainingCompleteModel);
        }

        //public ActionResult GetTrainingSpendTime(int ScheduledTrainingID, int TrainerID)
        //{
        //    StartTrainingModel objStartTrainingModel = new StartTrainingModel();
        //    objStartTrainingModel.ScheduledTrainingID = ScheduledTrainingID;
        //    objStartTrainingModel.TrainerID = TrainerID;

        //    objStartTrainingModel = objTrainerViewModel.InsertUpdateTrainingStartTime(objStartTrainingModel);
        //    return Json("", JsonRequestBehavior.AllowGet);
        //}

        public void DownloadPDF(string url)
        {
            Response.ContentType = "Application/pdf";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + System.IO.Path.GetFileName(url));
            Response.TransmitFile(Server.MapPath(url));
            Response.End();
        }


        #region Print and PDF functionality

        public ActionResult GetTopicByTraining(int TrainingID, int ScheduledTrainingID, bool IsMultipleCopies, bool IsPrint)
        {
            ViewSelectTopicModel objViewSelectTopicModel = new ViewSelectTopicModel();
            TopicViewModel objTopicViewModel = new TopicViewModel();
            objViewSelectTopicModel.TopicList = objTopicViewModel.GetTopicsbyTrainingID(TrainingID).ToList();
            objViewSelectTopicModel.TrainingID = TrainingID;
            objViewSelectTopicModel.ScheduledTrainingID = ScheduledTrainingID;
            objViewSelectTopicModel.IsTopicWiseAssessment = true;
            objViewSelectTopicModel.IsPrint = IsPrint;
            objViewSelectTopicModel.IsMultipleCopies = IsMultipleCopies;
            return PartialView("_SelectTopic", objViewSelectTopicModel);
        }




        public void GetPDF(int TrainingID, int ScheduledTrainingID, int TopicID, bool IsMultipleCopies, bool IsTopicWiseAssessment, bool IsPrint)
        {
            ViewSelectTopicModel objViewSelectTopicModel = new ViewSelectTopicModel();
            objViewSelectTopicModel.IsPrint = IsPrint;
            objViewSelectTopicModel.IsMultipleCopies = IsMultipleCopies;
            objViewSelectTopicModel.IsTopicWiseAssessment = IsTopicWiseAssessment;
            objViewSelectTopicModel.TrainingID = TrainingID;
            objViewSelectTopicModel.TopicID = TopicID;
            objViewSelectTopicModel.ScheduledTrainingID = ScheduledTrainingID;
            TrainerViewModel objTrainerViewModel = new TrainerViewModel();
            ViewQuestionPaperModel objViewQuestionPaperModel = new ViewQuestionPaperModel();
            objViewQuestionPaperModel = objTrainerViewModel.GetAssessmentByTrainingIDAndTopicID(objViewSelectTopicModel.TrainingID, objViewSelectTopicModel.TopicID, objViewSelectTopicModel.IsTopicWiseAssessment);
            if (objViewQuestionPaperModel.Questions != null && objViewQuestionPaperModel.Questions.Count > 0)
            {
                decimal totalMarks = 0;
                List<ViewQuestionModel> objViewQuestionPaperModelRandom = new List<ViewQuestionModel>();
                foreach (var obj in objViewQuestionPaperModel.Questions.GroupBy(x => x.QuestionTypeID).Select(x => x.First()))
                {
                    objViewQuestionPaperModelRandom.Add(obj);
                    totalMarks = totalMarks + (obj.QuestionCount * obj.Weightage);
                }

                decimal passingMarks = (totalMarks * Convert.ToDecimal(objViewQuestionPaperModel.PassingMarks)) / 100;
                objViewQuestionPaperModel.TotalMarks = Convert.ToInt32(totalMarks).ToString();
                objViewQuestionPaperModel.PassingMarks = Convert.ToInt32(passingMarks).ToString();

                List<string> objCandidateName = new List<string>();
                if (objViewSelectTopicModel.IsMultipleCopies)
                {
                    objCandidateName = objTrainerViewModel.GetAllCandidateNameByScheduledTrainingID(objViewSelectTopicModel.ScheduledTrainingID);

                }
                if (objCandidateName.Count() == 0)
                {
                    objCandidateName.Add(" ");
                }
                List<string> HtmlString = new List<string>();
                List<ViewQuestionPaperModel> lstViewQuestionPaperModelFinal = new List<ViewQuestionPaperModel>();
                foreach (var obj in objCandidateName)
                {
                    ViewQuestionPaperModel objViewQuestionPaperModelFinal = new ViewQuestionPaperModel();
                    objViewQuestionPaperModelFinal.TopicName = objViewQuestionPaperModel.TopicName;
                    objViewQuestionPaperModelFinal.TotalMarks = objViewQuestionPaperModel.TotalMarks;
                    objViewQuestionPaperModelFinal.PassingMarks = objViewQuestionPaperModel.PassingMarks;
                    objViewQuestionPaperModelFinal.StandardDuration = objViewQuestionPaperModel.StandardDuration;
                    objViewQuestionPaperModelFinal.TrainingName = objViewQuestionPaperModel.TrainingName;
                    objViewQuestionPaperModelFinal.CandidateName = obj.ToString();
                    List<ViewQuestionModel> objViewQuestionModel = new List<ViewQuestionModel>();
                    // List<ViewQuestionModel> lstViewQuestionModel = new List<ViewQuestionModel>();
                    objViewQuestionPaperModelFinal.Questions = new List<ViewQuestionModel>(); //lstViewQuestionModel;
                    int count = 1;
                    Random rmd = new Random();
                    for (int n = 0; n < objViewQuestionPaperModelRandom.Count; n++)
                    {
                        int m = rmd.Next(objViewQuestionPaperModelRandom.Count);
                        if (objViewQuestionPaperModelFinal.Questions.Select(x => x.QuestionTypeID).ToList().Contains(objViewQuestionPaperModelRandom[m].QuestionTypeID))
                        {
                            n--;
                        }
                        else
                        {
                            objViewQuestionModel = objViewQuestionPaperModel.Questions.Where(x => x.QuestionTypeID.ToString() == objViewQuestionPaperModelRandom[m].QuestionTypeID.ToString()).OrderBy(x => Guid.NewGuid()).Take(objViewQuestionPaperModelRandom[m].QuestionCount).ToList();
                            foreach (var objq in objViewQuestionModel)
                            {
                                objq.QuestionNo = count;
                                objViewQuestionPaperModelFinal.Questions.Add(objq);
                                count++;
                            }
                        }
                    }
                    // return View("_QuestionPaper", objViewQuestionPaperModelFinal);



                    lstViewQuestionPaperModelFinal.Add(objViewQuestionPaperModelFinal);
                }
                string path = Server.MapPath("~/Images/");
                foreach (var obj1 in lstViewQuestionPaperModelFinal)
                {
                    // generate the pdfs
                    string html = RenderRazorViewToString("_QuestionPaper", obj1).Replace("\r\n", "").Replace("#Image#", path);
                    HtmlString.Add(html);
                }
                HTMLToPdf(HtmlString, objViewSelectTopicModel.IsPrint);

            }
            // return null;

        }

        //this will return pdf from html
        public void HTMLToPdf(List<string> HTML, bool IsPrint)
        {
            string fileName = "Assessment Paper.pdf";
            Document document1 = new Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document1, System.Web.HttpContext.Current.Response.OutputStream);
            if (IsPrint)
            {
                PdfAction action = new PdfAction(PdfAction.PRINTDIALOG);
                writer.SetOpenAction(action);
            }
            document1.Open();

            iTextSharp.text.html.simpleparser.HTMLWorker hw1 = new iTextSharp.text.html.simpleparser.HTMLWorker(document1);
            for (int j = 0; j < HTML.Count; j++)
            {
                hw1.Parse(new StringReader(HTML[j]));
                if (j != (HTML.Count - 1))
                    document1.NewPage();
            }

            document1.Close();
            writer.Close();

            System.Web.HttpContext.Current.Response.ContentType = "application/pdf";
            System.Web.HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=\"" + fileName + "\"");
            System.Web.HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            System.Web.HttpContext.Current.Response.Write(document1);
            System.Web.HttpContext.Current.Response.End();




        }

        //this will return html from view
        public string RenderRazorViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                ViewData.Model = null;
                return sw.GetStringBuilder().ToString();
            }
        }

        #endregion


        #region SubmitAssessment
        /// <summary>
        /// Get Assessment Result Detail by scheduled Training ID
        /// </summary>
        /// <param name="stID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult SubmitAssessment(int stID, int TID)
        {
            SubmitAssessmentModel objSubmitAssessmentModel = new SubmitAssessmentModel();
            ScheduledTrainingViewModel objScheduledTrainingViewModel = new ScheduledTrainingViewModel();
            TrainerViewModel objTrainerViewModel = new TrainerViewModel();
            TopicViewModel objTopicViewModel = new TopicViewModel();
            try
            {
                objSubmitAssessmentModel.ScheduleTraining = objScheduledTrainingViewModel.GetScheduledTrainingByScheduledTrainingID(stID);
                objSubmitAssessmentModel.CurrentPage = 1;
                objSubmitAssessmentModel.PageSize = CommonUtils.PageSize;
                objSubmitAssessmentModel.IsTrainingComplete = objTrainerViewModel.CheckTrainingComplete(stID, TID);
                objSubmitAssessmentModel.TotalPages = 0;
                objSubmitAssessmentModel.TrainingID = TID;
                if (objSubmitAssessmentModel.ScheduleTraining != null)
                {
                    if (objSubmitAssessmentModel.ScheduleTraining.IsTopicWiseAssessment)
                    {
                        List<TopicModel> lstTopicModel = objTopicViewModel.GetTopicsbyTrainingID(objSubmitAssessmentModel.ScheduleTraining.TrainingID).ToList();
                        if (lstTopicModel != null && lstTopicModel.Count > 0)
                        {
                            ViewBag.TopicList = new SelectList(lstTopicModel, "TopicID", "TopicName", objSubmitAssessmentModel.TopicID);
                            objSubmitAssessmentModel.TopicID = lstTopicModel[0].TopicID;
                        }
                    }
                }
                else
                {
                    objSubmitAssessmentModel.ScheduleTraining = new ScheduledTrainingDetail();
                }
                if (objSubmitAssessmentModel.IsTrainingComplete)
                {
                    objSubmitAssessmentModel.Message = Resources.TrainingResource.msgTrainingAlreadyComplete.ToString();
                    objSubmitAssessmentModel.MessageType = MessageType.Notice.ToString().ToLower();
                }
                objTrainerViewModel.GetSubmitAssessmentDetail(objSubmitAssessmentModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "SubmitAssessment Get", ex);
            }
            return View(objSubmitAssessmentModel);
        }

        [HttpPost]
        [ValidateInput(false)]
        [Filters.Authorized()]
        public ActionResult SubmitAssessment(SubmitAssessmentModel objSubmitAssessmentModel)
        {
            TrainerViewModel objTrainerViewModel = new TrainerViewModel();
            try
            {
                switch (objSubmitAssessmentModel.ActionType)
                {
                    case "submitassessment":
                        string strAssessmentDetailXml = CommonUtils.GetBulkXML(objSubmitAssessmentModel.AssessmentDetails);
                        objSubmitAssessmentModel.IsActive = true;
                        objSubmitAssessmentModel.CreatedBy = UserId;
                        if (objSubmitAssessmentModel.TopicID == 0)
                        {
                            objSubmitAssessmentModel.TopicID = null;
                        }
                        objTrainerViewModel.InsertUpdateSubmitAssessment(strAssessmentDetailXml, objSubmitAssessmentModel);
                        break;
                    case "changetopic":
                        int topicId = 0;
                        int.TryParse(Convert.ToString(objSubmitAssessmentModel.TopicID), out topicId);
                        objSubmitAssessmentModel.TopicID = topicId;
                        break;
                }

                objTrainerViewModel.GetSubmitAssessmentDetail(objSubmitAssessmentModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "SubmitAssessment Post", ex);
            }
            return PartialView("_SubmitAssessmentDetail", objSubmitAssessmentModel);
        }
        #endregion



    }
}
