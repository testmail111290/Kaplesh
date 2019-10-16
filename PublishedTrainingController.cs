/*
* @author  : Soumen Bhowmick
* @version : 0.0.0.1
* @since   : 13 may 2014 
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
using CTMS.Models;
using CTMS.ViewModel;
using CTMS.Common;
using System.Globalization;

namespace CTMS.Controllers
{
    public class PublishedTrainingController : Controller
    {
		CommonUtils objCommonUtilError = new CommonUtils();

		#region ACTION METHODS
		//
		// GET: /PublishedTraining/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[Filters.MenuAccess()]
		public ActionResult ViewPublishedTraining()
		{
			int UserId, RoleId;

			ViewPublishedTrainingModel objViewPublishedTrainingModel = new ViewPublishedTrainingModel();
			PublishedTrainingViewModel objPublishedTrainingViewModel = new PublishedTrainingViewModel();

			MasterViewModel objMasterViewModel = new MasterViewModel();


			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);

			objViewPublishedTrainingModel.CurrentPage = 1;
			objViewPublishedTrainingModel.PageSize = CommonUtils.PageSize;
			objViewPublishedTrainingModel.TotalPages = 0;
			//objViewPublishedTrainingModel.PublishedTrainings = new List<PublishedTrainingModel>();
			objViewPublishedTrainingModel.FilterPointType = null;

			objPublishedTrainingViewModel.GetAllPublishedProgram(UserId, RoleId, objViewPublishedTrainingModel);

			BindTrainingTypeDDL();
			BindCircleDDL();
			BindCityDDL();
			BindMPDDL();
			BindTrainingCategoryDDL();


			return View(objViewPublishedTrainingModel);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[HttpPost]
        [Filters.Authorized()]
		public ActionResult ViewPublishedTraining(ViewPublishedTrainingModel objViewPublishedTrainingModel)
		{
			PublishedTrainingViewModel objPublishedTrainingViewModel = new PublishedTrainingViewModel();

			int UserId, RoleId;

			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);


			objPublishedTrainingViewModel.GetAllPublishedProgram(UserId, RoleId, objViewPublishedTrainingModel);

			

			BindTrainingTypeDDL();
			BindCircleDDL(objViewPublishedTrainingModel.FilterCircleId);
			BindCityDDL(objViewPublishedTrainingModel.FilterCircleId == null ? 0 : (int)objViewPublishedTrainingModel.FilterCircleId, objViewPublishedTrainingModel.FilterCityId);
			BindMPDDL(objViewPublishedTrainingModel.FilterCircleId == null ? 0 : (int)objViewPublishedTrainingModel.FilterCircleId, objViewPublishedTrainingModel.FilterCityId == null ? 0 : (int)objViewPublishedTrainingModel.FilterCityId, objViewPublishedTrainingModel.FilterMaintanencePointId);
			BindTrainingCategoryDDL();
			return PartialView("_PublishedTrainingGrid", objViewPublishedTrainingModel);
			//return View(objViewPublishedTrainingModel);
		}

		[HttpGet]
		public ActionResult ViewProgramDetails(int ProgramId)
		{
			PublishedTrainingViewModel objPublishedTrainingViewModel = new PublishedTrainingViewModel();
			ViewScheduledTrainingDetails objViewScheduledTrainingDetails = new ViewScheduledTrainingDetails();
			objPublishedTrainingViewModel.GetProgramDetailsByProgramId(ProgramId, objViewScheduledTrainingDetails);
			//show topic list 
			TrainingTopic objTrainingTopic = new TrainingTopic();
			TopicViewModel objTopicViewModel = new TopicViewModel();
			objTrainingTopic.CurrentPage = 1;
			objTrainingTopic.PageSize = CommonUtils.PageSize;

			objTrainingTopic.TotalPages = 0;
			objTopicViewModel.GetAllTopicbyTrainingID(objViewScheduledTrainingDetails.ScheduleTraining.TrainingID, objTrainingTopic, false);
			objViewScheduledTrainingDetails.TrainingTopics = new ViewTopicModel();
			objViewScheduledTrainingDetails.TrainingTopics.TopicList = objTrainingTopic.lstTopicModel;


			return PartialView("_ScheduledTrainingDetailsPartial", objViewScheduledTrainingDetails);
		}


		[HttpGet]
		public ActionResult ViewScheduleDetails(int ScheduledTrainingId, int TrainingID)
		{
			PublishedTrainingViewModel objPublishedTrainingViewModel = new PublishedTrainingViewModel();
			ViewScheduledTrainingDetails objViewScheduledTrainingDetails=new ViewScheduledTrainingDetails();
			objPublishedTrainingViewModel.GetTrainingDetailsByScheduledTrainingID(ScheduledTrainingId,TrainingID, objViewScheduledTrainingDetails);
            //show topic list 
            TrainingTopic objTrainingTopic = new TrainingTopic();
            TopicViewModel objTopicViewModel = new TopicViewModel();
            objTrainingTopic.CurrentPage = 1;
            objTrainingTopic.PageSize = CommonUtils.PageSize;

            objTrainingTopic.TotalPages = 0;
            objTopicViewModel.GetAllTopicbyTrainingID(objViewScheduledTrainingDetails.ScheduleTraining.TrainingID, objTrainingTopic, false);
            objViewScheduledTrainingDetails.TrainingTopics = new ViewTopicModel();
            objViewScheduledTrainingDetails.TrainingTopics.TopicList = objTrainingTopic.lstTopicModel;


			return PartialView("_ScheduledTrainingDetailsPartial", objViewScheduledTrainingDetails);
		}


		[HttpGet]
		public ActionResult ViewTraining(int ProgramId)
		{
			int UserId, RoleId;

			ViewPublishedTrainingModel objViewPublishedTrainingModel = new ViewPublishedTrainingModel();
			PublishedTrainingViewModel objPublishedTrainingViewModel = new PublishedTrainingViewModel();
			

			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);

			objViewPublishedTrainingModel.CurrentPage = 1;
			objViewPublishedTrainingModel.PageSize = CommonUtils.PageSize;
			objViewPublishedTrainingModel.TotalPages = 0;
			//objViewPublishedTrainingModel.PublishedTrainings = new List<PublishedTrainingModel>();
			objViewPublishedTrainingModel.FilterPointType = 1;
			objViewPublishedTrainingModel.FilterScheduleTrainingId = ProgramId;
			objPublishedTrainingViewModel.GetAllPublishedTraining(UserId, RoleId, objViewPublishedTrainingModel);

			return View(objViewPublishedTrainingModel);
		}

		[HttpPost]
		public ActionResult ViewTraining(ViewPublishedTrainingModel objViewPublishedTrainingModel)
		{
			int UserId, RoleId;

			//ViewPublishedTrainingModel objViewPublishedTrainingModel = new ViewPublishedTrainingModel();
			PublishedTrainingViewModel objPublishedTrainingViewModel = new PublishedTrainingViewModel();

			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);

			objPublishedTrainingViewModel.GetAllPublishedTraining(UserId, RoleId, objViewPublishedTrainingModel);

			return View(objViewPublishedTrainingModel);
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
		
		#region Control Binding
		private void BindCircleDDL(int? SelectedValue=0)
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
			TrainingViewModel objTrainingViewModel = new TrainingViewModel();
			List<TrainingTypeModel> TrainingTypes = objTrainingViewModel.GetAllActiveTrainigType(false).ToList();
			TrainingTypes.Insert(0, new TrainingTypeModel { TrainingTypeID=0, TrainingTypeName="--ALL--" });
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


        #region Training Calendar
        [Filters.MenuAccess()]
        public ActionResult TrainingCalendar()
        {

            int UserID = 0;
            int RoleID = 0;

            int.TryParse(Session["RoleId"].ToString(), out RoleID);
            int.TryParse(Session["UserId"].ToString(), out UserID);
            ViewTrainingCalendar objTrainingCalendar = new ViewTrainingCalendar();

            objTrainingCalendar.FilterMonth = DateTime.Now.Month;
            objTrainingCalendar.FilterYear = DateTime.Now.Year;
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
            objTrainingCalendar.IsReadOnly = true;
            //objTrainingCalendar.FilterFromDate = objTrainingCalendar.MonthStartDate;
            //objTrainingCalendar.FilterToDate = objTrainingCalendar.MonthStartDate.AddMonths(1).AddDays(-1);

            PublishedTrainingViewModel objPublishedTrainingViewModel = new PublishedTrainingViewModel();
            objTrainingCalendar.ScheduledTrainings = objPublishedTrainingViewModel.GetAllScheduledTrainings(objTrainingCalendar,UserID,RoleID);
            ViewBag.StatusLegend = objPublishedTrainingViewModel.GetResourceByResourceName("TrainingStatus");
            return View(objTrainingCalendar);
        }

        [HttpPost]
        [Filters.MenuAccess()]
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
            int UserID = 0;
            int RoleID = 0;

            int.TryParse(Session["RoleId"].ToString(), out RoleID);
            int.TryParse(Session["UserId"].ToString(), out UserID);

            PublishedTrainingViewModel objPublishedTrainingViewModel = new PublishedTrainingViewModel();
            objTrainingCalendar.ScheduledTrainings = objPublishedTrainingViewModel.GetAllScheduledTrainings(objTrainingCalendar,UserID,RoleID);
            return PartialView("MonthCalendar", objTrainingCalendar);
        }

        #endregion

    }
}
