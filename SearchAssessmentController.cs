/*
* @author  : Soumen Bhowmick
* @version : 0.0.0.1
* @since   : 5th June 2014 
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

namespace CTMS.Controllers
{
    public class SearchAssessmentController : Controller
    {
		CommonUtils objCommonUtilError = new CommonUtils();

        //
        // GET: /SearchAssessment/

		public ActionResult SearchAssessment(int PointType, int CircleId, int CityId, int MaintainancePointId, int TrainingId, int ScheduledTrainingId)
        {
			int UserId, RoleId;

			ViewSearchAssesmentModel objViewSearchAssesmentModel = new ViewSearchAssesmentModel();
			SearchAssessmentViewModel objSearchAssessmentViewModel = new SearchAssessmentViewModel();

			MasterViewModel objMasterViewModel = new MasterViewModel();


			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);

			objViewSearchAssesmentModel.CurrentPage = 1;
			objViewSearchAssesmentModel.PageSize = CommonUtils.PageSize;
			objViewSearchAssesmentModel.TotalPages = 0;
			//objViewPublishedTrainingModel.PublishedTrainings = new List<PublishedTrainingModel>();
			objViewSearchAssesmentModel.FilterPointType = PointType;
			objViewSearchAssesmentModel.FilterCircleId = CircleId;
			objViewSearchAssesmentModel.FilterCityId = CityId;
			objViewSearchAssesmentModel.FilterMaintanencePointId = MaintainancePointId;
			objViewSearchAssesmentModel.FilterTrainingId = TrainingId;
			objViewSearchAssesmentModel.FilterScheduleTrainingId = ScheduledTrainingId;
			objSearchAssessmentViewModel.SearchAssessment(UserId, RoleId, objViewSearchAssesmentModel);

			BindTrainingTypeDDL();
			BindCircleDDL();
			BindCityDDL();
			BindMPDDL();
			BindTrainingDDL(PointType, CircleId, CityId, MaintainancePointId, TrainingId, ScheduledTrainingId);
			ViewBag.ProgramID = ScheduledTrainingId;
			return View(objViewSearchAssesmentModel);
        }

		[HttpPost]
        [Filters.Authorized()]
		public ActionResult SearchAssessment(ViewSearchAssesmentModel objViewSearchAssesmentModel)
		{
			int UserId, RoleId;
			SearchAssessmentViewModel objSearchAssessmentViewModel = new SearchAssessmentViewModel();
			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);
			objSearchAssessmentViewModel.SearchAssessment(UserId, RoleId, objViewSearchAssesmentModel);
			return PartialView("_AssessmentListPartial", objViewSearchAssesmentModel);
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

		[HttpGet]
		public JsonResult GetTrainingList(int PointType, int CircleId, int CityId, int MaintainancePointId, int ProgramId)
		{
			int UserId, RoleId;

			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);

			var Trainings = this.GetTrainings(UserId, RoleId, PointType, CircleId, CityId, MaintainancePointId, ProgramId);

			return Json(Trainings, JsonRequestBehavior.AllowGet);
		}
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
			TrainingViewModel objTrainingViewModel = new TrainingViewModel();
			List<TrainingTypeModel> TrainingTypes = objTrainingViewModel.GetAllActiveTrainigType(false).ToList();
			TrainingTypes.Insert(0, new TrainingTypeModel { TrainingTypeID = 0, TrainingTypeName = "--ALL--" });
			ViewBag.TrainingTypeList = new SelectList(TrainingTypes, "TrainingTypeID", "TrainingTypeName", null);
		}

		private void BindTrainingDDL( int PointType, int CircleId, int CityId, int MaintainancePointId,int TrainingId,int ScheduledTrainingId, int? SelectedValue = 0)
		{
			int UserId, RoleId;

			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);

			List<TrainingModel> Trainings = this.GetTrainings(UserId, RoleId, PointType, CircleId, CityId, MaintainancePointId, ScheduledTrainingId);
			ViewBag.TrainingList = new SelectList(Trainings, "TrainingID", "TrainingName", SelectedValue);
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


		private List<TrainingModel> GetTrainings(int UserId, int RoleId, int PointType, int CircleId, int CityId, int MPId, int ScheduledTrainingId)
		{
			SearchAssessmentViewModel objSearchAssessmentViewModel=new SearchAssessmentViewModel();
			List<TrainingModel> lstTrainings = objSearchAssessmentViewModel.GetTrainingList(UserId, RoleId, PointType, CircleId, CityId, MPId, ScheduledTrainingId).ToList<TrainingModel>();

			if (lstTrainings == null)
				lstTrainings = new List<TrainingModel>();


			lstTrainings.Insert(0, new TrainingModel { TrainingID = 0, TrainingName = "--ALL--" });
			return lstTrainings;
		}

		#endregion
    }
}
