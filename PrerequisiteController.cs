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

namespace CTMS.Controllers
{
    public class PrerequisiteController : Controller
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

        //Object of Prerequisite View Model where all CRUD operation perform. 
        PrerequisiteViewModel ObjPrerequisiteViewModel = new PrerequisiteViewModel();


        //
        // GET: /Prerequesite/
        /// <summary>
        /// Load Prerequesite list
        /// </summary>
         [Filters.MenuAccess()]
        public ActionResult ViewPrerequisite()
        {
            ViewPrerequisite objViewPrerequisite = new ViewPrerequisite();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            try
            {
                if (Session["PreRequisiteSuccess"] != null && Convert.ToString(Session["PreRequisiteSuccess"]) != "")
                {
                    objViewPrerequisite.Message = Convert.ToString(Session["PreRequisiteSuccess"]);
                    objViewPrerequisite.MessageType = MessageType.Success.ToString().ToLower();
                    Session["PreRequisiteSuccess"] = null;
                }
                FillTrainingCategory();
                objViewPrerequisite.CurrentPage = 1;
                objViewPrerequisite.PageSize = CommonUtils.PageSize;
                objViewPrerequisite.TotalPages = 0;
                List<CategoryModel> lstCategory = objTrainingViewModel.GetAllActiveCategory(false).ToList();
                lstCategory.Insert(0, new CategoryModel { CategoryID = 0, CategoryName = "--Select--" });
                ViewBag.CategoryList = new SelectList(lstCategory, "CategoryID", "CategoryName", objViewPrerequisite.FilterCategory.ToString());
                List<FunctionModel> lstFunction = new List<FunctionModel>();
                FunctionModel objFunction = new FunctionModel();
                objFunction.FunctionID = 0;
                objFunction.FunctionName = "--Select--";
                lstFunction.Insert(0, objFunction);
                ViewBag.FunctionList = new SelectList(lstFunction, "FunctionID", "FunctionName", objViewPrerequisite.FilterFunction.ToString());
                ObjPrerequisiteViewModel.GetPrerequisite(objViewPrerequisite);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Prerequisite", "ViewPrerequisite Get", ex);
            }
            return View(objViewPrerequisite);
        }

        /// <summary>
        /// Show Prerequesite list 
        /// </summary>
        /// <param name="objViewPrerequisite"></param>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ViewPrerequisite(ViewPrerequisite objViewPrerequisite)
        {
            String ResultUrl;
            objViewPrerequisite.Message = objViewPrerequisite.MessageType = String.Empty;
  

            try
            {
                if (objViewPrerequisite.ActionType != null)
                {
                    switch (objViewPrerequisite.ActionType)
                    {
                        case "search": objViewPrerequisite.CurrentPage = 1;
                            break;

                        case "Add":
                            objViewPrerequisite.CurrentPage = 1;
                            ResultUrl = Url.Action("SavePreRequisite", "Prerequisite");
                            return Json(new { Url = ResultUrl }, JsonRequestBehavior.AllowGet);
                            

                        case "Edit": 
                            objViewPrerequisite.CurrentPage = 1;
                            ResultUrl = Url.Action("SavePreRequisite", "Prerequisite", new { prID = objViewPrerequisite.SelectedId });
                            return Json(new { Url = ResultUrl }, JsonRequestBehavior.AllowGet);

                        case "Delete":
                            objViewPrerequisite.CurrentPage = 1;
                             bool IsDeleted = ObjPrerequisiteViewModel.DeletePrerequisiteByPrerequisiteId(objViewPrerequisite.SelectedId);
                             if (IsDeleted)
                             {
                                 objViewPrerequisite.Message = Resources.PreRequisiteResource.msgDeletePreRequisteSuccess;
                                 objViewPrerequisite.MessageType = MessageType.Success.ToString().ToLower();
                             }
                             else
                             {
                                 objViewPrerequisite.Message = Resources.PreRequisiteResource.msgDeletePreRequisiteError;
                                objViewPrerequisite.MessageType = MessageType.Error.ToString().ToLower();
                                
                             }
                            break;
                     }
                }
                ObjPrerequisiteViewModel.GetPrerequisite(objViewPrerequisite);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Prerequisite", "ViewPrerequisite Post", ex);
            }

            return PartialView("_PrerequisiteList", objViewPrerequisite);
        }


        /// <summary>
        /// Show edit and add prerequisite page based on selected id
        /// </summary>
        /// <param name="SelectedId"></param>
         [Filters.MenuAccess()]
        public ActionResult SavePreRequisite(int? prID)
        {
            
            PreRequisiteDetail ObjPreRequisiteDetail = new PreRequisiteDetail();
            if (prID.HasValue)
                ObjPreRequisiteDetail.PrereqSelectedId = prID.Value;
            try
            {
                if (prID.HasValue)
                {
                    ObjPreRequisiteDetail = ObjPrerequisiteViewModel.GetPrerequisiteByPrerequisiteID(ObjPreRequisiteDetail);
              
                }
                else
                {
                   
                    ObjPreRequisiteDetail = ObjPrerequisiteViewModel.GetPrerequisiteTrainings(ObjPreRequisiteDetail);

              }
                ObjPreRequisiteDetail.UnSelectedTrainings = ObjPreRequisiteDetail.preReqTrainings.Where(M => M.PrerequisiteID == 0).ToList();
                ObjPreRequisiteDetail.ActiveTrainings = ObjPreRequisiteDetail.preReqTrainings.Where(M => M.PrerequisiteID != 0).ToList();
               
                GetAllList(ObjPreRequisiteDetail);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Prerequisite", "Save Prerequisite get", ex);
            }

            return View(ObjPreRequisiteDetail);
        }


        /// <summary>
        /// Saves Prerequesite list 
        /// </summary>
        /// <param name="ObjPreRequisiteDetail"></param>
        [HttpPost]
         public ActionResult SavePreRequisite(PreRequisiteDetail ObjPreRequisiteDetail)
         {
             try
             {
                 if (ObjPreRequisiteDetail.ActiveTrainings == null)
                     ObjPreRequisiteDetail.ActiveTrainings = new List<PrerequisiteTrainingDetail>();
                 if (ObjPreRequisiteDetail.UnSelectedTrainings == null)
                     ObjPreRequisiteDetail.UnSelectedTrainings = new List<PrerequisiteTrainingDetail>();
                 if (!String.IsNullOrEmpty(ObjPreRequisiteDetail.ActionType))
                 {
                     switch (ObjPreRequisiteDetail.ActionType)
                     {
                         case "Move": MoveUnselectedTraining(ObjPreRequisiteDetail);
                             break;
                         case "Delete":
                             DeleteActiveTraining(ObjPreRequisiteDetail);
                             break;

                         case "Up": UpActiveTraining(ObjPreRequisiteDetail);
                             break;
                         case "Down":
                             DownActiveTraining(ObjPreRequisiteDetail);
                             break;
                         case "change":
                             GetTrainingsByIndex(ObjPreRequisiteDetail);
                             break;
                         case "save": 
                                List<PrerequisiteTrainingDetail> lstXmlModel = new List<PrerequisiteTrainingDetail>();
                                  ObjPreRequisiteDetail.CreatedBy = createdBy;
                             for (int i = 0; i < ObjPreRequisiteDetail.ActiveTrainings.Count; i++)
                                 ObjPreRequisiteDetail.ActiveTrainings[i].IsActive = true;

                             string strListXmlModel = CommonUtils.GetBulkXML(ObjPreRequisiteDetail.ActiveTrainings);
                             ObjPrerequisiteViewModel.SavePreRequisiteDetails(ObjPreRequisiteDetail, strListXmlModel);
                             string redirectUrl = string.Empty;
                             if (ObjPreRequisiteDetail.ErrorCode == 0)
                             {
                                 if (ObjPreRequisiteDetail.PrerequisiteID > 0)
                                 {
                                     Session["PreRequisiteSuccess"] = Resources.PreRequisiteResource.msgPreRequisiteUpdateSuccess;
                                 }
                                 else
                                 {
                                     Session["PreRequisiteSuccess"] = Resources.PreRequisiteResource.msgPreRequisiteSaveSuccess;
                                 }
                                 redirectUrl = Url.Action("ViewPrerequisite", "Prerequisite");
                                 return Json(new { Url = redirectUrl }, JsonRequestBehavior.AllowGet);
                             }
                             else
                             {
                                 if (ObjPreRequisiteDetail.PrerequisiteID > 0)
                                 {
                                     ObjPreRequisiteDetail.Message = Resources.PreRequisiteResource.msgPreRequisiteUpdateError;
                                 }
                                 else
                                 {
                                     ObjPreRequisiteDetail.Message = Resources.PreRequisiteResource.msgPreRequisiteSaveError;
                                 }
                                 ObjPreRequisiteDetail.MessageType = MessageType.Error.ToString().ToLower();
                                 redirectUrl = Url.Action("SavePreRequisite", "Prerequisite", new { SelectedId = ObjPreRequisiteDetail.PrerequisiteID });
                                 return Json(new { Url = redirectUrl }, JsonRequestBehavior.AllowGet);

                             }
                     }
                 }
              }
             catch (Exception ex)
             {
                 Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Prerequisite", "Save Prerequisite Post", ex);
             }
             return PartialView("_viewPrerequisiteTraining", ObjPreRequisiteDetail);
        }


        /// <summary>
        /// Return Training category
        /// </summary>    
        public void FillTrainingCategory()
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            IList<TrainingCategoryModel> lstTrainingCategoryModel = objMasterViewModel.GetAllTrainingCategory();
            List<TrainingCategoryModel> lstTrainingCategory = new List<TrainingCategoryModel>();

            lstTrainingCategory.Insert(0, new TrainingCategoryModel { TrainingCategoryId = 0, TrainingCategoryName = "--Select--" });
            for (int i = 0; i < lstTrainingCategoryModel.Count(); i++)
            {
                lstTrainingCategory.Add(new TrainingCategoryModel { TrainingCategoryId = lstTrainingCategoryModel[i].TrainingCategoryId, TrainingCategoryName = lstTrainingCategoryModel[i].TrainingCategoryName });
            }
            ViewBag.TrainingCategoryList = new SelectList(lstTrainingCategory, "TrainingCategoryId", "TrainingCategoryName");
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
                   lstFunctionCategoryModel = ObjPrerequisiteViewModel.GetFunctionsByCategoryIds(Convert.ToString(CategoryID));
                   SelectList objFunctions = new SelectList(lstFunctionCategoryModel, "FunctionID", "FunctionName", 0);
                   return Json(objFunctions);
               }
               catch (Exception ex)
               {
                  
                   return Json(ex);
               }
           }


        public void GetTrainingsByIndex(PreRequisiteDetail ObjPreRequisiteDetail)
           {
               
             ObjPrerequisiteViewModel.GetPrerequisiteTrainings(ObjPreRequisiteDetail);
              ObjPreRequisiteDetail.ActiveTrainings.Clear();
              
           }

        /// <summary>
        /// Sets TrainingCateogry List, Function List and JobRole List
        /// </summary> 
        public void GetAllList(PreRequisiteDetail ObjPreRequisiteDetail )
        {

            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            FillTrainingCategory();
            List<CategoryModel> lstCategory = new List<CategoryModel>();
            lstCategory = objTrainingViewModel.GetAllActiveCategory(false).ToList();
            lstCategory.Insert(0, new CategoryModel { CategoryID = 0, CategoryName = "--Select--" });
            ViewBag.CategoryList = new SelectList(lstCategory, "CategoryID", "CategoryName", ObjPreRequisiteDetail.FunctionID);
            List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
            //FunctionCategoryModel objFunction = new FunctionCategoryModel();
            //objFunction.FunctionID = 0;
            //objFunction.FunctionName = "--Select--";
           
            if (ObjPreRequisiteDetail.FunctionID > 0)
            {
                lstFunctionCategoryModel = ObjPrerequisiteViewModel.GetFunctionsByCategoryIds(Convert.ToString(ObjPreRequisiteDetail.FunctionID));
           }
            if (String.IsNullOrEmpty(ObjPreRequisiteDetail.JobRoleIDs))
            {
                ObjPreRequisiteDetail.JobRoleIDs = string.Empty;
            }
            //lstFunctionCategoryModel.Insert(0, objFunction);
            ViewBag.FunctionList = new MultiSelectList(lstFunctionCategoryModel, "FunctionID", "FunctionName", ObjPreRequisiteDetail.JobRoleIDs.Split(",".ToCharArray()));
            
        
        }

        /// <summary>
        /// Move items of Unselected training list to Active training list
        /// </summary> 
        public void MoveUnselectedTraining(PreRequisiteDetail ObjPreRequisiteDetail)
        {
            PrerequisiteTrainingDetail UnSelected = new PrerequisiteTrainingDetail();
            if (!String.IsNullOrEmpty(ObjPreRequisiteDetail.selectedTrainings))
            {
                List<string> lstTrainingIds = ObjPreRequisiteDetail.selectedTrainings.Split(',').ToList();
                foreach(var strTrainingID in lstTrainingIds)
                {
                    int TrainingID = Convert.ToInt32(strTrainingID);
                  UnSelected = ObjPreRequisiteDetail.UnSelectedTrainings.Where(M => M.TrainingID == TrainingID).FirstOrDefault();
                UnSelected.Sequence = ((ObjPreRequisiteDetail.ActiveTrainings.Count) + 1);
                UnSelected.PrerequisiteID = ObjPreRequisiteDetail.PrerequisiteID;
                ObjPreRequisiteDetail.ActiveTrainings.Add(UnSelected);
                ObjPreRequisiteDetail.UnSelectedTrainings.Remove(UnSelected);
                }
            }
        }

        /// <summary>
        ///Delete items from Active training list
        /// </summary> 
        public void DeleteActiveTraining(PreRequisiteDetail ObjPreRequisiteDetail)
        {
            PrerequisiteTrainingDetail Active = new PrerequisiteTrainingDetail();
            Active = ObjPreRequisiteDetail.ActiveTrainings.Where(M => M.TrainingID == ObjPreRequisiteDetail.TrainingID).FirstOrDefault();
            int Sequence = Active.Sequence;
            ObjPreRequisiteDetail.ActiveTrainings.Remove(Active);
            for (int i = (Sequence-1); i < ObjPreRequisiteDetail.ActiveTrainings.Count; i++)
            {
                ObjPreRequisiteDetail.ActiveTrainings[i].Sequence = Sequence; 
            }
            ObjPreRequisiteDetail.UnSelectedTrainings.Add(Active);

        }

        /// <summary>
        ///Changes the position of items of Active training list
        /// </summary> 
        public void UpActiveTraining(PreRequisiteDetail ObjPreRequisiteDetail)
        {
            PrerequisiteTrainingDetail Active = new PrerequisiteTrainingDetail();
            PrerequisiteTrainingDetail prev = new PrerequisiteTrainingDetail();
            Active = ObjPreRequisiteDetail.ActiveTrainings.Where(M => M.TrainingID == ObjPreRequisiteDetail.TrainingID).FirstOrDefault();
            int Sequence = Active.Sequence;
            Active.Sequence=(Sequence-1);
            prev = ObjPreRequisiteDetail.ActiveTrainings.Where(M => M.Sequence == (Sequence-1)).FirstOrDefault();
            prev.Sequence = Sequence;
            ObjPreRequisiteDetail.ActiveTrainings.RemoveAt(Sequence - 2);
            ObjPreRequisiteDetail.ActiveTrainings.Insert((Sequence - 2), Active);
            ObjPreRequisiteDetail.ActiveTrainings.RemoveAt(Sequence-1);
            ObjPreRequisiteDetail.ActiveTrainings.Insert((Sequence-1), prev);
        }

        /// <summary>
        ///Changes the position of items of Active training list
        /// </summary> 
        public void DownActiveTraining(PreRequisiteDetail ObjPreRequisiteDetail)
        {
            PrerequisiteTrainingDetail Active = new PrerequisiteTrainingDetail();
            PrerequisiteTrainingDetail next = new PrerequisiteTrainingDetail();
            Active = ObjPreRequisiteDetail.ActiveTrainings.Where(M => M.TrainingID == ObjPreRequisiteDetail.TrainingID).FirstOrDefault();
            int Sequence = Active.Sequence;
            next = ObjPreRequisiteDetail.ActiveTrainings.Where(M => M.Sequence == (Sequence + 1)).FirstOrDefault();
            Active.Sequence = (Sequence+1);
            
           next.Sequence = Sequence;
            ObjPreRequisiteDetail.ActiveTrainings.RemoveAt(Sequence);
            ObjPreRequisiteDetail.ActiveTrainings.Insert((Sequence), Active);
            ObjPreRequisiteDetail.ActiveTrainings.RemoveAt(Sequence-1);
            ObjPreRequisiteDetail.ActiveTrainings.Insert((Sequence-1), next);

        }

        
    }
}
