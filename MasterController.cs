using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Models;
using MvcJqGrid;
using CTMS.ViewModel;
using CTMS.Common;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.Data.Entity;
using System.Configuration;
using System.IO;
using System.Web.UI;
using CTMS.DataModels;
using System.Web.Mvc.Html;

namespace CTMS.Controllers
{
    public partial class MasterController : Controller
    {

        CommonUtils objCommonUtilError = new CommonUtils();
        #region MasterMenuLevel

        [Filters.MenuAccess()]
        public ActionResult ViewLevel()
        {
            ViewLevelModel objViewLeveleModel = new ViewLevelModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
            {
                objViewLeveleModel.Message = Convert.ToString(Session["Message"]);
                objViewLeveleModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }

            ViewBag.StatusList = new SelectList(new[]
                                          {
                                              new {ID="-1",Name="All"},
                                              new {ID="1",Name="Active"},
                                              new{ID="0",Name="InActive"},
                                          },
                         "ID", "Name", -1);

            objViewLeveleModel.CurrentPage = 1;
            objViewLeveleModel.PageSize = CommonUtils.PageSize;

            objViewLeveleModel.TotalPages = 0;

            objMasterViewModel.SearchLevel(objViewLeveleModel);

            return View(objViewLeveleModel);
        }

        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewLevel(ViewLevelModel objViewLevelModel)
        {
            objViewLevelModel.Message = objViewLevelModel.MessageType = String.Empty;
            MasterViewModel objMasterViewModel = new MasterViewModel();
            objMasterViewModel.SearchLevel(objViewLevelModel);
            return PartialView("LevelList", objViewLevelModel);
        }

        [Filters.MenuAccess()]
        public ActionResult SaveLevel(int? LevelID)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveLevelModel objLevel = new SaveLevelModel();
            int LevelId;

            try
            {

                LevelId = LevelID != null ? (int)LevelID : 0;
                ViewBag.LevelId = LevelID != null ? (int)LevelID : 0;
                objLevel = LevelId != 0 ? objMasterViewModel.GetLevelByLevelID(LevelId) : new SaveLevelModel();

                if (LevelId == 0)
                {
                    objLevel.IsActive = true;
                }


            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "Save Level Get", ex);
            }
            return View("SaveLevel", objLevel);
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveLevel(SaveLevelModel objLevel)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string strMessage = string.Empty;
            string strRetMessage = string.Empty;
            string redirectUrl = string.Empty;
            SaveLevelModel objLevelSave = new SaveLevelModel();
            objLevelSave.OperationType = "A";

            MasterViewModel objMasterViewModel;
            try
            {
                objMasterViewModel = new MasterViewModel();
                strMessage = objLevel.LevelID != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;


                if (ModelState.IsValid)
                {

                    if (objLevel.LevelID != 0)
                    {
                        objLevelSave.OperationType = "E";
                    }

                    objLevelSave.LevelName = objLevel.LevelName.Trim();
                    objLevelSave.LevelCode = objLevel.LevelCode.Trim();

                    objLevelSave.IsActive = objLevel.IsActive;
                    objLevelSave.LevelID = objLevel.LevelID;

                    objLevelSave.CreatedOn = DateTime.Now;
                    objLevelSave.CreatedBy = createdBy;

                    objLevelSave = objMasterViewModel.InsertUpdateDelLevel(objLevelSave);


                    if (objLevelSave.ErrorCode.Equals(0))
                    {
                        Session["Message"] = string.Format(strMessage, "Level");
                        Session["MessageType"] = MessageType.Success.ToString().ToLower();
                    }
                    else
                    {
                        Session["Message"] = objLevelSave.ErrorMessage;
                        Session["MessageType"] = MessageType.Error.ToString().ToLower();
                    }

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveLevel Post", ex);
            }

            redirectUrl = Url.Action("ViewLevel");

            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });

        }

        //public JsonResult IsLevelNameExist(string LevelName, int LevelID)
        //{
        //    MasterViewModel objMasterViewModel = new MasterViewModel();
        //    List<LevelModel> lstLevel = (from a in objMasterViewModel.GetAllLevel()
        //                                 where a.LevelName.ToLower().Trim() == LevelName.ToLower().Trim() && a.LevelID != LevelID
        //                                 select a).ToList();

        //    return Json(lstLevel.Count == 0, JsonRequestBehavior.AllowGet);
        //}
        //public JsonResult IsLevelCodeExist(string LevelCode, int LevelID)
        //{
        //    MasterViewModel objMasterViewModel = new MasterViewModel();
        //    List<LevelModel> lstLevel = (from a in objMasterViewModel.GetAllLevel()
        //                                 where a.LevelCode.ToLower().Trim() == LevelCode.ToLower().Trim() && a.LevelID != LevelID
        //                                 select a).ToList();

        //    return Json(lstLevel.Count == 0, JsonRequestBehavior.AllowGet);
        //}


        public ActionResult DeleteLevel(int id)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            SaveLevelModel objLevelDelete = new SaveLevelModel();
            objLevelDelete.OperationType = "D";
            objLevelDelete.LevelID = id;

            MasterViewModel objMasterViewModel;
            string strRetMessage = string.Empty;
            try
            {
                objMasterViewModel = new MasterViewModel();

                objLevelDelete = objMasterViewModel.InsertUpdateDelLevel(objLevelDelete);

                if (objLevelDelete.ErrorCode.Equals(0))
                {

                    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Level") + "</div>";
                }
                else
                {
                    strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objLevelDelete.ErrorMessage) + "</div>";

                }
                return Content(strRetMessage);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "DeleteLevel", ex);
                strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + UserMessage.EXCEPTION_MESSAGE + "</div>";
            }
            return Content(strRetMessage);
        }

        #endregion

        #region MasterMenuTeam

        [Filters.MenuAccess()]
        public ActionResult ViewTeam()
        {
            ViewTeamModel objViewTeameModel = new ViewTeamModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
            {
                objViewTeameModel.Message = Convert.ToString(Session["Message"]);
                objViewTeameModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }

            ViewBag.StatusList = new SelectList(new[]
                                          {
                                              new {ID="-1",Name="All"},
                                              new {ID="1",Name="Active"},
                                              new{ID="0",Name="InActive"},
                                          },
                         "ID", "Name", -1);

            objViewTeameModel.CurrentPage = 1;
            objViewTeameModel.PageSize = CommonUtils.PageSize;

            objViewTeameModel.TotalPages = 0;

            objMasterViewModel.SearchTeam(objViewTeameModel);

            return View(objViewTeameModel);
        }


        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewTeam(ViewTeamModel objViewTeamModel)
        {
            objViewTeamModel.Message = objViewTeamModel.MessageType = String.Empty;
            MasterViewModel objMasterViewModel = new MasterViewModel();
            objMasterViewModel.SearchTeam(objViewTeamModel);
            return PartialView("TeamList", objViewTeamModel);
        }


        [Filters.MenuAccess()]
        public ActionResult SaveTeam(int? TeamID)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveTeamModel objTeam = new SaveTeamModel();
            int TeamId;

            try
            {

                TeamId = TeamID != null ? (int)TeamID : 0;
                ViewBag.TeamId = TeamID != null ? (int)TeamID : 0;
                objTeam = TeamId != 0 ? objMasterViewModel.GetTeamByTeamID(TeamId) : new SaveTeamModel();

                if (TeamId == 0)
                {
                    objTeam.IsActive = true;
                }


            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "Save Team Get", ex);
            }
            return View("SaveTeam", objTeam);
        }


        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTeam(SaveTeamModel objTeam)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string strMessage = string.Empty;
            string strRetMessage = string.Empty;
            string redirectUrl = string.Empty;
            SaveTeamModel objTeamSave = new SaveTeamModel();
            objTeamSave.OperationType = "A";

            MasterViewModel objMasterViewModel;
            try
            {
                objMasterViewModel = new MasterViewModel();
                strMessage = objTeam.TeamID != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;


                if (ModelState.IsValid)
                {

                    if (objTeam.TeamID != 0)
                    {
                        objTeamSave.OperationType = "E";
                    }

                    objTeamSave.TeamName = objTeam.TeamName.Trim();

                    objTeamSave.IsActive = objTeam.IsActive;
                    objTeamSave.TeamID = objTeam.TeamID;

                    objTeamSave.CreatedOn = DateTime.Now;
                    objTeamSave.CreatedBy = createdBy;

                    objTeamSave = objMasterViewModel.InsertUpdateDelTeam(objTeamSave);


                    if (objTeamSave.ErrorCode.Equals(0))
                    {
                        Session["Message"] = string.Format(strMessage, "Team");
                        Session["MessageType"] = MessageType.Success.ToString().ToLower();
                    }
                    else
                    {
                        Session["Message"] = objTeamSave.ErrorMessage;
                        Session["MessageType"] = MessageType.Error.ToString().ToLower();
                    }

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveTeam Post", ex);
            }

            redirectUrl = Url.Action("ViewTeam");

            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });

        }


        public JsonResult IsTeamNameExist(string TeamName, int TeamID)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<TeamModel> lstTeam = (from a in objMasterViewModel.GetAllTeam(null, null, string.Empty)
                                       where a.TeamName.ToLower().Trim() == TeamName.ToLower().Trim() && a.TeamID != TeamID
                                       select a).ToList();

            return Json(lstTeam.Count == 0, JsonRequestBehavior.AllowGet);
        }



        public ActionResult DeleteTeam(int id)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            SaveTeamModel objTeamDelete = new SaveTeamModel();
            objTeamDelete.OperationType = "D";
            objTeamDelete.TeamID = id;

            MasterViewModel objMasterViewModel;
            string strRetMessage = string.Empty;
            try
            {
                objMasterViewModel = new MasterViewModel();

                objTeamDelete = objMasterViewModel.InsertUpdateDelTeam(objTeamDelete);

                if (objTeamDelete.ErrorCode.Equals(0))
                {

                    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Team") + "</div>";
                }
                else
                {
                    strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTeamDelete.ErrorMessage) + "</div>";

                }
                return Content(strRetMessage);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "DeleteLevel", ex);
                strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + UserMessage.EXCEPTION_MESSAGE + "</div>";
            }
            return Content(strRetMessage);
        }

        #endregion

        #region MasterMenuRole


        [Filters.MenuAccess()]
        public ActionResult ViewRole()
        {
            ViewRoleModel objViewRoleModel = new ViewRoleModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
            {
                objViewRoleModel.Message = Convert.ToString(Session["Message"]);
                objViewRoleModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }

            ViewBag.StatusList = new SelectList(new[]
                                          {
                                              new {ID="-1",Name="All"},
                                              new {ID="1",Name="Active"},
                                              new{ID="0",Name="InActive"},
                                          },
                         "ID", "Name", -1);

            objViewRoleModel.CurrentPage = 1;
            objViewRoleModel.PageSize = CommonUtils.PageSize;
            objViewRoleModel.TotalPages = 0;

            objMasterViewModel.SearchRole(objViewRoleModel);

            return View(objViewRoleModel);
        }


        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewRole(ViewRoleModel objViewRoleModel)
        {
            objViewRoleModel.Message = objViewRoleModel.MessageType = String.Empty;
            MasterViewModel objMasterViewModel = new MasterViewModel();
            objMasterViewModel.SearchRole(objViewRoleModel);
            return PartialView("RoleList", objViewRoleModel);
        }


        [Filters.MenuAccess()]
        public ActionResult SaveRole(int? RoleID)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveRoleModel objRole = new SaveRoleModel();
            int RoleId;

            objRole.LevelList = objMasterViewModel.GetAllLevel().Where(s => s.IsActive == true).ToList();
            objRole.TeamList = objMasterViewModel.GetAllTeam(null, null, string.Empty).Where(s => s.IsActive == true).ToList();

            ViewBag.LevelList = new SelectList(objRole.LevelList, "LevelID", "LevelName", objRole.LevelID);
            ViewBag.TeamList = new SelectList(objRole.TeamList, "TeamID", "TeamName", objRole.TeamID);


            try
            {
                RoleId = RoleID != null ? (int)RoleID : 0;
                objRole = RoleId != 0 ? objMasterViewModel.GetRoleByRoleID(RoleId) : new SaveRoleModel();

                if (RoleId == 0)
                {
                    objRole.IsActive = true;
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveRole", ex);
            }
            return View("SaveRole", objRole);
        }


        [HttpPost]
        [Filters.Authorized()]
        public ActionResult SaveRole(RoleModel objRole)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string strMessage = string.Empty;
            string strRetMessage = string.Empty;
            string redirectUrl = string.Empty;
            SaveRoleModel objRoleSave = new SaveRoleModel();
            objRoleSave.OperationType = "A";
            MasterViewModel objMasterViewModel;
            try
            {
                objMasterViewModel = new MasterViewModel();
                strMessage = objRole.RoleID != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;
                if (ModelState.IsValid)
                {
                    if (objRole.RoleID != 0)
                    {
                        objRoleSave.OperationType = "E";
                    }
                    objRoleSave.RoleName = objRole.RoleName.Trim();
                    objRoleSave.RoleType = objRole.RoleType.Trim();

                    objRoleSave.IsActive = objRole.IsActive;

                    objRoleSave.TeamID = objRole.TeamID;
                    objRoleSave.LevelID = objRole.LevelID;

                    objRoleSave.RoleID = objRole.RoleID;
                    objRoleSave.CreatedOn = DateTime.Now;
                    objRoleSave.CreatedBy = createdBy;

                    objRoleSave = objMasterViewModel.InsertUpdateDelRole(objRoleSave);

                    if (objRoleSave.ErrorCode.Equals(0))
                    {
                        Session["Message"] = string.Format(strMessage, "Role");
                        Session["MessageType"] = MessageType.Success.ToString().ToLower();
                    }
                    else
                    {
                        Session["Message"] = objRoleSave.ErrorMessage;
                        Session["MessageType"] = MessageType.Error.ToString().ToLower();
                    }

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveRole Post", ex);
            }

            redirectUrl = Url.Action("ViewRole");

            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });

        }



        //public JsonResult IsRoleNameExist(string RoleName, int RoleID)
        //{
        //    MasterViewModel objMasterViewModel = new MasterViewModel();
        //    List<RoleModel> lstRole = (from a in objMasterViewModel.GetAllRole(null, null, string.Empty)
        //                               where a.RoleName.ToLower().Trim() == RoleName.ToLower().Trim() && a.RoleID != RoleID
        //                               select a).ToList();

        //    return Json(lstRole.Count == 0, JsonRequestBehavior.AllowGet);
        //}



        public ActionResult DeleteRole(int id)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            SaveRoleModel objRoleDelete = new SaveRoleModel();
            objRoleDelete.OperationType = "D";
            objRoleDelete.RoleID = id;

            MasterViewModel objMasterViewModel;
            string strRetMessage = string.Empty;
            try
            {
                objMasterViewModel = new MasterViewModel();

                objRoleDelete = objMasterViewModel.InsertUpdateDelRole(objRoleDelete);

                if (objRoleDelete.ErrorCode.Equals(0))
                {

                    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Role") + "</div>";
                }
                else
                {
                    strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objRoleDelete.ErrorMessage) + "</div>";

                }
                return Content(strRetMessage);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "DeleteLevel", ex);
                strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + UserMessage.EXCEPTION_MESSAGE + "</div>";
            }
            return Content(strRetMessage);
        }

        #endregion

        #region Master Menu Create


        [Filters.MenuAccess()]
        public ActionResult ViewMenu()
        {

            ViewMenuModel objViewMenuModel = new ViewMenuModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
            {
                objViewMenuModel.Message = Convert.ToString(Session["Message"]);
                objViewMenuModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }

            ViewBag.StatusList = new SelectList(new[]
                                          {
                                              new {ID="-1",Name="All"},
                                              new {ID="1",Name="Active"},
                                              new{ID="0",Name="InActive"},
                                          },
                         "ID", "Name", -1);

            objViewMenuModel.CurrentPage = 1;
            objViewMenuModel.PageSize = CommonUtils.PageSize;
            objViewMenuModel.TotalPages = 0;

            objMasterViewModel.SearchMenu(objViewMenuModel);
            return View(objViewMenuModel);
        }

        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewMenu(ViewMenuModel objViewMenuModel)
        {
            objViewMenuModel.Message = objViewMenuModel.MessageType = String.Empty;
            MasterViewModel objMasterViewModel = new MasterViewModel();
            objMasterViewModel.SearchMenu(objViewMenuModel);
            return PartialView("MenuList", objViewMenuModel);
        }

        [Filters.MenuAccess()]
        public ActionResult SaveMenu(int? MenuID)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);

            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveMenuModel objSaveMenuModel = new SaveMenuModel();


            int menuid;

            try
            {

                MenuModel objMenu = new MenuModel();

                List<MenuModel> lstPageName = (from a in objMasterViewModel.GetAllMenu()
                                               select a).OrderBy(a => a.MenuID).ToList();

                MenuModel lstMenuItem = new MenuModel();
                lstMenuItem.MenuName = "N/A";
                lstMenuItem.MenuID = 0;
                lstPageName.Insert(0, lstMenuItem);

                menuid = MenuID != null ? (int)MenuID : 0;

                objSaveMenuModel = MenuID != 0 ? objMasterViewModel.GetMenuByMenuID(menuid) : new SaveMenuModel();

                if (menuid == 0)
                {
                    objSaveMenuModel.IsActive = true;
                }

                objSaveMenuModel.SelectedPageName = objSaveMenuModel.PageName;

                objSaveMenuModel.Url = objSaveMenuModel.MenuUrl;
                if (String.IsNullOrEmpty(objSaveMenuModel.SelectedPageName))
                {
                    objSaveMenuModel.SelectedPageName = string.Empty;
                }
                ViewBag.ParentMenuList = new SelectList(lstPageName, "MenuID", "MenuName", objSaveMenuModel.ParentID);

                ViewBag.PageNameList = new MultiSelectList(lstPageName, "MenuID", "MenuName", objSaveMenuModel.SelectedPageName.Split(",".ToCharArray()));

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Menu", "SaveMenu Get", ex);
            }
            return View(objSaveMenuModel);
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveMenu(SaveMenuModel objMenu)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string strMessage = string.Empty;
            string strRetMessage = string.Empty;
            SaveMenuModel objMenuSave = new SaveMenuModel();
            objMenuSave.OperationType = "A";
            MasterViewModel objMasterViewModel;
            string redirectUrl;

            try
            {
                objMasterViewModel = new MasterViewModel();
                strMessage = objMenu.MenuID != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;
                if (ModelState.IsValid)
                {

                    if (objMenu.MenuID != 0)
                    {
                        objMenuSave.OperationType = "E";
                    }

                    objMenuSave.MenuID = objMenu.MenuID;
                    objMenuSave.MenuName = objMenu.MenuName.Trim();
                    objMenuSave.ParentID = objMenu.ParentID;
                    objMenuSave.Url = !string.IsNullOrEmpty(objMenu.Url) ? objMenu.Url.Trim() : string.Empty;
                    objMenuSave.RoleID = 0;
                    objMenuSave.SortOrder = 1;
                    objMenuSave.IsActive = objMenu.IsActive;
                    objMenuSave.CreatedOn = DateTime.Now;
                    objMenuSave.Menulevel = objMenu.Menulevel;
                    objMenuSave.MenuOrder = objMenu.MenuOrder;
                    objMenuSave.PageName = objMenu.SelectedPageName;
                    objMenuSave.IsDisplay = objMenu.IsDisplay;

                    objMenuSave.CreatedBy = createdBy;

                    objMenuSave = objMasterViewModel.InsertUpdateDelMenu(objMenuSave);

                    if (objMenuSave.ErrorCode.Equals(0))
                    {
                        Session["Message"] = string.Format(strMessage, "Menu");
                        Session["MessageType"] = MessageType.Success.ToString().ToLower();
                    }
                    else
                    {
                        Session["Message"] = objMenuSave.ErrorMessage;
                        Session["MessageType"] = MessageType.Error.ToString().ToLower();
                    }

                    //Added by Harsh to update Menu Cache
                    if (HttpRuntime.Cache["AllMenus"] != null)
                        HttpRuntime.Cache.Remove("AllMenus");
                    List<MenuModel> lstMenus = new List<MenuModel>();
                    using (var context = new CTMSEntities())
                    {
                        IList<spGetAllMenu_Result> lstMenuResult = context.spGetAllMenu().ToList<spGetAllMenu_Result>();

                        foreach (spGetAllMenu_Result itmMenuResult in lstMenuResult)
                        {

                            lstMenus.Add(Common.CommonUtils.GetComplexTypeToEntity<MenuModel>(itmMenuResult));

                        }
                    }
                    HttpRuntime.Cache.Insert("AllMenus", lstMenus, null, DateTime.Now.AddYears(1), System.Web.Caching.Cache.NoSlidingExpiration);

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveMenu Post", ex);
            }

            redirectUrl = Url.Action("ViewMenu");
            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });
        }


        public ActionResult DeleteMenu(int id)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            SaveMenuModel objMenuDelete = new SaveMenuModel();
            objMenuDelete.OperationType = "D";
            objMenuDelete.MenuID = id;

            MasterViewModel objMasterViewModel;
            string strRetMessage = string.Empty;
            try
            {
                objMasterViewModel = new MasterViewModel();

                objMenuDelete = objMasterViewModel.InsertUpdateDelMenu(objMenuDelete);

                if (objMenuDelete.ErrorCode.Equals(0))
                {
                    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Menu") + "</div>";
                }
                else
                {
                    strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objMenuDelete.ErrorMessage) + "</div>";

                }
                return Json(new { url = Url.Action("ViewMenu") }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "DeleteMenu", ex);
                strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + UserMessage.EXCEPTION_MESSAGE + "</div>";
            }
            return Json(new { url = Url.Action("ViewMenu") }, JsonRequestBehavior.AllowGet);
        }

        #region Old Menu Code
        //public ActionResult GetMenu(GridSettings gridSettings)
        //{
        //    IList<RoleModel> objlstRoleModel = new List<RoleModel>();
        //    MasterViewModel objMasterViewModel = new MasterViewModel();
        //    List<MenuModel> lstMenu = null;
        //    int totalRecords;

        //    MenuModel filter = new MenuModel();
        //    if (gridSettings.IsSearch)
        //    {
        //        filter.MenuID = gridSettings.Where.rules.Any(r => r.field == "MenuID") ?
        //                 Int32.Parse(gridSettings.Where.rules.FirstOrDefault(r => r.field == "MenuID").data) : int.MinValue;
        //        filter.MenuName = gridSettings.Where.rules.Any(r => r.field == "MenuName") ?
        //                gridSettings.Where.rules.FirstOrDefault(r => r.field == "MenuName").data : string.Empty;
        //        filter.ParentMenu = gridSettings.Where.rules.Any(r => r.field == "ParentMenu") ?
        //                gridSettings.Where.rules.FirstOrDefault(r => r.field == "ParentMenu").data : string.Empty;
        //        filter.Url = gridSettings.Where.rules.Any(r => r.field == "Url") ?
        //                gridSettings.Where.rules.FirstOrDefault(r => r.field == "Url").data : string.Empty;
        //        filter.Menulevel = gridSettings.Where.rules.Any(r => r.field == "MenuLevel") ?
        //                 Int32.Parse(gridSettings.Where.rules.FirstOrDefault(r => r.field == "MenuLevel").data) : int.MinValue;
        //        filter.MenuOrder = gridSettings.Where.rules.Any(r => r.field == "MenuOrder") ?
        //                 Int32.Parse(gridSettings.Where.rules.FirstOrDefault(r => r.field == "MenuOrder").data) : int.MinValue;

        //        filter.IsMenuActive = gridSettings.Where.rules.Any(r => r.field == "IsMenuActive") ?
        //                gridSettings.Where.rules.FirstOrDefault(r => r.field == "IsMenuActive").data : string.Empty;
        //    }

        //    lstMenu = MenuSearch(filter, gridSettings.SortColumn, gridSettings.SortOrder, gridSettings.PageSize, gridSettings.PageIndex, out totalRecords);

        //    var jsonData = new
        //    {
        //        total = (totalRecords + gridSettings.PageSize - 1) / gridSettings.PageSize,
        //        page = gridSettings.PageIndex,
        //        records = totalRecords,
        //        rows = (
        //            from c in lstMenu
        //            select new
        //            {
        //                MenuID = c.MenuID,
        //                MenuName = c.MenuName,
        //                ParentMenu = c.ParentMenu,
        //                Url = c.Url,
        //                MenuLevel = c.Menulevel,
        //                MenuOrder = c.MenuOrder,

        //                IsMenuActive = c.IsMenuActive
        //            })
        //    };
        //    return Json(jsonData, JsonRequestBehavior.AllowGet);
        //}

        //public List<MenuModel> MenuSearch(MenuModel filter, string sortColumn, string sortOrder, int pageSize, int pageIndex, out int totalRecords)
        //{
        //    MasterViewModel objMasterViewModel = new MasterViewModel();

        //    var q = from l in objMasterViewModel.GetAllMenu()
        //            select new MenuModel
        //            {
        //                MenuID = l.MenuID,
        //                MenuName = l.MenuName,
        //                ParentMenu = l.ParentMenu,
        //                Url = l.Url,
        //                Menulevel = l.Menulevel,
        //                MenuOrder = l.MenuOrder,
        //                IsMenuActive = Convert.ToBoolean(l.IsActive) ? "Yes" : "No"
        //            };

        //    if (filter.MenuID != 0 && filter.MenuID != Int32.MinValue)
        //    {
        //        q = q.Where(c => c.MenuID.ToString().StartsWith(filter.MenuID.ToString()));
        //    }
        //    if (!string.IsNullOrEmpty(filter.MenuName))
        //    {
        //        q = q.Where(c => c.MenuName.ToLower().StartsWith(filter.MenuName.ToLower()));
        //    }
        //    if (!string.IsNullOrEmpty(filter.ParentMenu))
        //    {
        //        q = q.Where(c => (!string.IsNullOrEmpty(c.ParentMenu) ? c.ParentMenu.ToLower() : string.Empty).StartsWith(filter.ParentMenu.ToLower()));
        //    }
        //    if (!string.IsNullOrEmpty(filter.Url))
        //    {
        //        q = q.Where(c => c.Url.ToLower().StartsWith(filter.Url.ToLower()));
        //    }
        //    if (filter.Menulevel != 0 && filter.Menulevel != Int32.MinValue)
        //    {
        //        q = q.Where(c => c.Menulevel.ToString().StartsWith(filter.Menulevel.ToString()));
        //    }
        //    if (filter.MenuOrder != 0 && filter.MenuOrder != Int32.MinValue)
        //    {
        //        q = q.Where(c => c.MenuOrder.ToString().StartsWith(filter.MenuOrder.ToString()));
        //    }

        //    if (!string.IsNullOrEmpty(filter.IsMenuActive))
        //    {
        //        q = q.Where(c => c.IsMenuActive.ToLower().StartsWith(filter.IsMenuActive.ToLower()));
        //    }

        //    switch (sortColumn)
        //    {
        //        case "MenuID":
        //            q = (sortOrder == "desc") ? q.OrderByDescending(c => c.MenuID) : q.OrderBy(c => c.MenuID);
        //            break;
        //        case "MenuName":
        //            q = (sortOrder == "desc") ? q.OrderByDescending(c => c.MenuName) : q.OrderBy(c => c.MenuName);
        //            break;
        //        case "ParentMenu":
        //            q = (sortOrder == "desc") ? q.OrderByDescending(c => c.ParentMenu) : q.OrderBy(c => c.ParentMenu);
        //            break;
        //        case "Url":
        //            q = (sortOrder == "desc") ? q.OrderByDescending(c => c.Url) : q.OrderBy(c => c.Url);
        //            break;
        //        case "Menulevel":
        //            q = (sortOrder == "desc") ? q.OrderByDescending(c => c.Menulevel) : q.OrderBy(c => c.Menulevel);
        //            break;
        //        case "MenuOrder":
        //            q = (sortOrder == "desc") ? q.OrderByDescending(c => c.MenuOrder) : q.OrderBy(c => c.MenuOrder);
        //            break;
        //        case "IsMenuActive":
        //            q = (sortOrder == "desc") ? q.OrderByDescending(c => c.IsMenuActive) : q.OrderBy(c => c.IsMenuActive);
        //            break;
        //        default:
        //            break;
        //    }
        //    totalRecords = q.Count();
        //    return q.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        //}

        //public JsonResult IsMenuNameExist(string MenuName, int MenuID)
        //{
        //    MasterViewModel objMasterViewModel = new MasterViewModel();
        //    List<MenuModel> lstMenu = (from a in objMasterViewModel.GetAllMenu()
        //                               where a.MenuName.ToLower().Trim() == MenuName.ToLower().Trim() && a.MenuID != MenuID
        //                               select a).ToList();
        //    return Json(lstMenu.Count == 0, JsonRequestBehavior.AllowGet);

        //}
        #endregion

        #endregion

        #region Master Menu Assign

        [Filters.MenuAccess()]
        public ActionResult ViewAssignMenu()
        {

            MasterViewModel objMasterViewModel = new MasterViewModel();

            ViewBag.rolelist = new SelectList((from c in objMasterViewModel.GetAllRole(null, null, string.Empty).Where(s => s.IsActive == true)
                                               select new { c.RoleID, c.RoleName }).ToList(), "RoleID", "RoleName");

            ViewBag.displayTreeView = MakeRoleBasedTree();

            return View();
        }

        private string MakeRoleBasedTree()
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            IList<MenuModel> mnulist = objMasterViewModel.GetAllMenu().Where(s => s.IsActive == true).ToList();



            string strHtml = "<ul id='tree1'>";
            foreach (var mp in mnulist.Where(p => p.ParentID == 0))
            {



                strHtml += "<li id='" + @mp.MenuID + "'><input type='checkbox' value='" + @mp.MenuID + "'><label>" + @mp.MenuName + "</label>";

                if (mnulist.Count(p => p.ParentID == mp.MenuID) > 0)
                {
                    strHtml += "<ul>";
                }

                string strchild;

                RenderMenuItem(mnulist, mp, out strchild);

                strHtml += strchild;

                if (mnulist.Count(p => p.ParentID == mp.MenuID) > 0)
                {
                    strHtml += "</ul>";
                }

                strHtml += "</li>";
            }
            return strHtml;
        }

        private void RenderMenuItem(IList<CTMS.Models.MenuModel> menuList, CTMS.Models.MenuModel mi, out string strTree)
        {
            strTree = string.Empty;
            foreach (var cp in menuList.Where(p => p.ParentID == mi.MenuID))
            {
                strTree += "<li id='" + @cp.MenuID + "'><input type='checkbox' value='" + @cp.MenuID + "'><label>" + cp.MenuName + "</label>";

                if (menuList.Count(p => p.ParentID == cp.MenuID) > 0)
                {
                    strTree += "<ul>";
                }

                string strchild;
                RenderMenuItem(menuList, cp, out strchild);
                strTree += strchild;

                if (menuList.Count(p => p.ParentID == cp.MenuID) > 0)
                {
                    strTree += "</ul>";
                }
                else
                {
                    strTree += "</li>";
                }
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetRoleBasedAssignedMenu(int roleid)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            var classesMenu = objMasterViewModel.GetAllRoleBasedMenuAccess().Where(R => R.RoleID == roleid).Select(R => R.MenuID);
            return Json(classesMenu, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SavesAssignedRoles(string menuid, int roleid)
        {
            string classesMenu = "";
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<RoleBasedMenuAccessModel> lstmap = addrolebasedmenus(menuid, roleid);
            //IList<RoleBasedMenuAccessModel> objLstMenu;
            //objLstMenu = new List<RoleBasedMenuAccessModel>();
            RoleBasedMenuAccessModel objMenu;
            int createdBy = Convert.ToInt32(Session["UserId"]);
            objMenu = objMasterViewModel.DeleteMenuAssignRoleId(roleid);
            if (objMenu.ErrorCode.Equals(0))
            {
                foreach (RoleBasedMenuAccessModel itemspGetMenu_Result in lstmap)
                {
                    objMenu = new RoleBasedMenuAccessModel();
                    objMenu.MenuID = itemspGetMenu_Result.MenuID;
                    objMenu.AccessID = itemspGetMenu_Result.AccessID;
                    objMenu.RoleID = itemspGetMenu_Result.RoleID;
                    objMenu.CreatedBy = createdBy;
                    objMenu = objMasterViewModel.InsertMenuAssign(objMenu);
                    if (objMenu.ErrorCode.Equals(0))
                    {
                        classesMenu = "Success";
                    }
                    else
                    {
                        classesMenu = "Failed";
                        Session["ExceptionMsg"] = objMenu.ErrorMessage;
                    }

                }
            }
            else
            {
                classesMenu = "Failed";
                Session["ExceptionMsg"] = objMenu.ErrorMessage;
            }
            return Json(new { classesMenu = classesMenu, JsonRequestBehavior.AllowGet });
        }

        private List<RoleBasedMenuAccessModel> addrolebasedmenus(string mnuid, int roleid)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<RoleBasedMenuAccessModel> lstmenus = new List<RoleBasedMenuAccessModel>();
            List<string> menus = mnuid.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = 0; i < menus.Count; i++)
            {
                int Menuid = int.Parse(menus[i]);
                // find the Parent ID
                int Parentid = objMasterViewModel.GetAllMenu().Where(P => P.MenuID == Menuid).Select(p => p.ParentID).Single();
                if (!menus.Contains(Parentid.ToString()))
                {
                    //Parend menu added
                    while (Parentid != 0 && !menus.Contains(Parentid.ToString()))
                    {
                        menus.Add(Parentid.ToString());
                        Parentid = objMasterViewModel.GetAllMenu().Where(P => P.MenuID == Parentid).Select(p => p.ParentID).Single();
                    }
                }

                lstmenus.Add(
                    new RoleBasedMenuAccessModel()
                    {
                        CreatedBy = createdBy,
                        CreatedOn = DateTime.Now,
                        IsActive = true,
                        MenuID = Menuid,
                        RoleID = roleid
                    });
            }
            return lstmenus;
        }

        #endregion

        #region Master ViewUser

        [Filters.MenuAccess()]
        public ActionResult ViewUser()
        {
            ViewUserMapModel objViewUserMapModel = new ViewUserMapModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();


            if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
            {
                objViewUserMapModel.Message = Convert.ToString(Session["Message"]);
                objViewUserMapModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }


            if (Session["RoleName"] != null)
            {
                ViewBag.LoginUserRole = Session["RoleName"];
            }

            ViewBag.MainAdminRoleName = ConfigurationManager.AppSettings["MainAdminRoleName"].ToString();
            ViewBag.MainAdminRoleID = ConfigurationManager.AppSettings["MainAdminRoleID"].ToString();

            objViewUserMapModel.CurrentPage = 1;
            objViewUserMapModel.PageSize = CommonUtils.PageSize;

            objViewUserMapModel.TotalPages = 0;
            objMasterViewModel.GetAllUserMap(objViewUserMapModel);

            return View(objViewUserMapModel);
        }


        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewUser(ViewUserMapModel objViewUserMapModel)
        {

            if (Session["RoleName"] != null)
            {
                ViewBag.LoginUserRole = Session["RoleName"];
            }

            ViewBag.MainAdminRoleName = ConfigurationManager.AppSettings["MainAdminRoleName"].ToString();
            ViewBag.MainAdminRoleID = ConfigurationManager.AppSettings["MainAdminRoleID"].ToString();

            MasterViewModel objMasterViewModel = new MasterViewModel();
            objMasterViewModel.GetAllUserMap(objViewUserMapModel);
            return PartialView("UserMapList", objViewUserMapModel);
        }


        [HttpPost]
        public ActionResult DeleteUserRoles(int? userid)
        {
            int UserID = Convert.ToInt32(userid);
            int createdBy = Convert.ToInt32(Session["UserId"]);
            ViewUser objMenuDelete = new ViewUser();
            objMenuDelete.RoleUserId = UserID;
            objMenuDelete.CreatedBy = createdBy;

            MasterViewModel objMasterViewModel;
            string strRetMessage = string.Empty;

            try
            {
                objMasterViewModel = new MasterViewModel();

                objMenuDelete = objMasterViewModel.DeleteUserRoleMapping(objMenuDelete);

                if (objMenuDelete.ErrorCode.Equals(0))
                {
                    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Feedback") + "</div>";
                }
                else
                {
                    strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objMenuDelete.ErrorMessage) + "</div>";
                }

                return Json(new { url = Url.Action("ViewUser") }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "DeleteFeedback", ex);
                strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + UserMessage.EXCEPTION_MESSAGE + "</div>";
            }
            return Json(new { url = Url.Action("ViewUser") }, JsonRequestBehavior.AllowGet);

        }


        public ActionResult ExporttoExcel()
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            var gvUserExcel = new GridView();

            gvUserExcel.AutoGenerateColumns = true;

            gvUserExcel.DataSource = (from u in objMasterViewModel.GetAllUserRoleMap()
                                      select new
                                      {
                                          UserID = u.UserID,
                                          UserFName = u.UserFName,
                                          UserLName = u.UserLName,
                                          UserDomainName = u.UserDomainName,
                                          MobileNumber = u.MobileNumber,
                                          EmailID = u.EmailID,
                                          CompanyName = u.CompanyName,
                                          Address = u.Address,
                                          TeamName = u.TeamName,
                                          RoleName = u.RoleName,
                                          LevelName = u.LevelName,
                                          CircleCityArea = u.CircleCityArea
                                      }).ToArray();
            gvUserExcel.DataBind();

            //gvUserExcel = MergeRows(gvUserExcel);
            return new DownloadResult(gvUserExcel, "User Mapping.xls");

            //Response.ClearContent();
            //Response.Buffer = true;
            //Response.AddHeader("content-disposition", "attachment; filename=Marklist.xls");
            //Response.ContentType = "application/ms-excel";
            //Response.Charset = "";
            //StringWriter sw = new StringWriter();
            //HtmlTextWriter htw = new HtmlTextWriter(sw);
            //gvUserExcel.RenderControl(htw);
            //Response.Output.Write(sw.ToString());
            //Response.Flush();
            //Response.End();

            //return new DownloadResult(gvUserExcel, "Excel.xls");

            // return RedirectToAction("ViewUser");


        }

        #endregion

        #region Master Create User Mapping


        [Filters.MenuAccess()]
        public ActionResult MapUserView(int? id, string flag)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            int roleID = Convert.ToInt32(Session["RoleId"]);


            if (Session["RoleName"] != null)
            {
                ViewBag.LoginUserRole = Session["RoleName"];
            }

            ViewBag.MainAdminRoleName = ConfigurationManager.AppSettings["MainAdminRoleName"].ToString();
            ViewBag.MainAdminRoleID = ConfigurationManager.AppSettings["MainAdminRoleID"].ToString();


            MasterViewModel objMasterViewModel = new MasterViewModel();
            #region [Defalut Loading views]
            ViewBag.SearchName = new SelectList(new[]
                                          {
                                              new {ID="0",Name="--Select--"},
                                              new {ID="1",Name="First Name"},
                                              new{ID="2",Name="Last Name"},
                                              new{ID="3",Name="Domain Name"},
                                              new{ID="4",Name="Email ID"},

                                          },
                            "ID", "Name", 0);


            ViewBag.TeamList = new SelectList((from c in objMasterViewModel.GetAllTeam(createdBy, roleID, "U").Where(s => s.IsActive == true).ToList()
                                               select new { c.TeamID, c.TeamName }).ToList(), "TeamId", "TeamName");

            //ViewBag.LevelList = new SelectList((from c in objMasterViewModel.GetAllLevel().Where(s => s.IsActive == true).ToList()
            //                                    select new { c.LevelID, c.LevelName }).ToList(), "LevelID", "LevelName");
            ViewBag.LevelList = new SelectList(new[] 
                                        { 
                                            new { LevelID = "0", LevelName = "--Select--" }
                                        },
                                       "LevelID", "LevelName", 0);

            ViewBag.RoleList = new SelectList(new[] 
                                        { 
                                            new { RoleID = "0", RoleName = "--Select--" }
                                        },
                                        "RoleID", "RoleName", 0);

            ViewBag.CircleList = new SelectList(new[] 
                                        { 
                                            new { CircleID = "0", CircleName = "--Select--" }
                                        },
                                        "CircleID", "CircleName", 0);

            ViewBag.ActiveCircleList = new SelectList(new[] 
                                        { 
                                            new { ActiveCircleID = "0", ActiveCircleName = "--Select--" }
                                        },
                                        "ActiveCircleID", "ActiveCircleName", 0);

            ViewBag.CityList = new SelectList(new[] 
                                        { 
                                            new { CityID = "0", CityName = "--Select--" }
                                        },
                                        "CityID", "CityName", 0);

            ViewBag.ActiveCityList = new SelectList(new[] 
                                        { 
                                            new { ActiveCityID = "0", ActiveCityName = "--Select--" }
                                        },
                                        "ActiveCityID", "ActiveCityName", 0);
            ViewBag.MaintenancePointList = new SelectList(new[] 
                                        { 
                                            new { MaintenancePointId = "0", MaintenancePoint = "--Select--" }
                                        },
                                       "MaintenancePointId", "MaintenancePoint", 0);

            ViewBag.ActiveMaintenancePointList = new SelectList(new[] 
                                        { 
                                            new { ActiveMaintenancePointId = "0", ActiveMaintenancePoint = "--Select--" }
                                        },
                                        "ActiveMaintenancePointId", "ActiveMaintenancePoint", 0);

            ViewBag.RoleMapHtml = "<div class='row'><div class='col-sm-12'><div class='table-responsive'>" +
                                  "<table width='100%' id='tblrolemapmgmt' class='table table-striped remove-bottom table-selector'" +
                                  "style='display: none'> " +
                                  "<tr style = 'background-color:#cdd1e0'>" +
                                  "<td><div>Team Name</div></td><td><div >Role Name</div></td>" +
                                  "<td><div>Level Name</div></td><td><div>Active Circle/City/Maintenance Point</div></td>" +
                                  "<td><div>Action</div></td></tr>   ";

            #endregion [End Defalut Loading views]

            if (id != null && flag == "A")
            {
                UserMap obj = new UserMap();
                obj.userlist = objMasterViewModel.GetAllUser().Where(m => m.UserId == id.Value).Take(1).Single();
                obj.flag = "A";
                ViewBag.RoleMapHtml += "</table>";
                ViewBag.UserID = obj.userlist.UserId;
                return View("UserMapping", obj);
            }

            if (id != null && flag == null)
            {
                UserMap obj = new UserMap();
                obj.userlist = objMasterViewModel.GetAllUser().Where(m => m.UserId == id.Value).Take(1).Single();

                List<UserRoleMappingModel> lstUserRole = objMasterViewModel.GetUserRoleMapping().Where(m => m.UserID == id.Value).ToList();
                int? prevRole = 0;
                foreach (UserRoleMappingModel usrrole in lstUserRole)
                {
                    const string vcentral = "central", vcity = "city", vcircle = "circle", vmaintenance = "maintenancepoint";

                    if (prevRole != usrrole.RoleID)
                    {
                        string TeamName = objMasterViewModel.GetAllTeam(null, null, string.Empty).Where(t => t.TeamID == usrrole.TeamID).SingleOrDefault().TeamName;
                        string RoleName = objMasterViewModel.GetAllRole(null, null, string.Empty).Where(t => t.RoleID == usrrole.RoleID).SingleOrDefault().RoleName;
                        string LevelName = objMasterViewModel.GetAllLevel().Where(t => t.LevelID == usrrole.LevelID).SingleOrDefault().LevelName;

                        ViewBag.RoleMapHtml += "  <tr><td name='" + usrrole.TeamID + "'>" + TeamName + "</td><td name='" + usrrole.RoleID + "'>" + RoleName + "</td>" +
                                           "<td name='" + usrrole.LevelID + "'>" + LevelName + "</td>";

                        int vcircleid = objMasterViewModel.GetAllLevel().Where(l => l.LevelName.ToLower() == vcircle).SingleOrDefault().LevelID;
                        int vcityid = objMasterViewModel.GetAllLevel().Where(l => l.LevelName.ToLower() == vcity).SingleOrDefault().LevelID;
                        int vmaintenanceid = objMasterViewModel.GetAllLevel().Where(l => l.LevelName.ToLower() == vmaintenance).SingleOrDefault().LevelID;

                        int[] activecircles = lstUserRole.Where(C => C.LevelID == vcircleid && C.RoleID == usrrole.RoleID).Select(c => c.CircleCityAreaID).ToArray();
                        string[] activecirclesName = objMasterViewModel.GetCircleMaster().Where(C => activecircles.Contains(C.CircleID)).Select(c => c.CircleName).ToArray();


                        int[] activecity = lstUserRole.Where(C => C.LevelID == vcityid && C.RoleID == usrrole.RoleID).Select(c => c.CircleCityAreaID).ToArray();
                        string[] activecityName = objMasterViewModel.GetCityMaster().Where(C => activecity.Contains(C.CityID)).Select(c => c.CityName).ToArray();

                        int? circleid = 0, circlecityid = 0;
                        if (activecity.Length > 0)
                            circleid = objMasterViewModel.GetCityMaster().Where(C => activecity.Contains(C.CityID)).Select(c => c.CircleID).Take(1).Single();


                        int[] activemaintenance = lstUserRole.Where(C => C.LevelID == vmaintenanceid && C.RoleID == usrrole.RoleID).Select(c => c.CircleCityAreaID).ToArray();
                        string[] activemaintenanceName = objMasterViewModel.GetMaintenancePointMaster().Where(C => activemaintenance.Contains(C.MaintenancePointId)).Select(c => c.MaintenancePoint).ToArray();

                        if (activemaintenance.Length > 0)
                        {
                            circlecityid = objMasterViewModel.GetMaintenancePointMaster().Where(C => activemaintenance.Contains(C.MaintenancePointId)).Select(c => c.CityID).Take(1).Single();
                            circleid = objMasterViewModel.GetCityMaster().Where(C => C.CityID == circlecityid).Select(c => c.CircleID).Single();
                        }

                        switch (LevelName.ToLower())
                        {
                            case vcentral:
                                ViewBag.RoleMapHtml += "<td name='0'></td><td ><a id='btnEditRole' class='btnsmall' href='#' onclick='EditRole($(this));return false;'>Edit</a>" +
                                                       "&nbsp;&nbsp;&nbsp;<a id='btnDelRole' class='btnsmall' href='#' onclick='DeleteRole($(this));'>Delete</a>" +
                                                       "</td></tr>";
                                break;
                            case vcircle:
                                ViewBag.RoleMapHtml += "</td><td name='" + String.Join(",", activecircles) + "'> " + String.Join(",", activecirclesName) + " </td><td ><a id='btnEditRole' class='btnsmall' href='#' onclick='EditRole($(this)); return false;'>Edit</a>" +
                                                       "&nbsp;&nbsp;&nbsp;<a id='btnDelRole' class='btnsmall' href='#' onclick='DeleteRole($(this));'>Delete</a>" +
                                                       "</td></tr>";
                                break;
                            case vcity:
                                ViewBag.RoleMapHtml += "<td id='" + circleid + "' name='" + String.Join(",", activecity) + "'>" + String.Join(",", activecityName) + "</td><td ><a id='btnEditRole' class='btnsmall' href='#' onclick='EditRole($(this));return false;'>Edit</a>" +
                                                       "&nbsp;&nbsp;&nbsp;<a id='btnDelRole' class='btnsmall' href='#' onclick='DeleteRole($(this));'>Delete</a>" +
                                                       "</td></tr>";
                                break;
                            case vmaintenance:
                                ViewBag.RoleMapHtml += "<td id='" + circleid + "' class='" + circlecityid + "' name='" + String.Join(",", activemaintenance) + "'>" + String.Join(",", activemaintenanceName) + "</td><td ><a id='btnEditRole' class='btnsmall' href='#' onclick='EditRole($(this));return false;'>Edit</a>" +
                                                       "&nbsp;&nbsp;&nbsp;<a id='btnDelRole' class='btnsmall' href='#' onclick='DeleteRole($(this));'>Delete</a>" +
                                                       "</td></tr>";
                                break;
                        }
                    }
                    prevRole = usrrole.RoleID;
                }
                ViewBag.RoleMapHtml += "</table></div></div></div>";

                return View("UserMapping", obj);
            }

            ViewBag.RoleMapHtml += "</table>";
            return View("UserMapping"); //
        }

        [Filters.MenuAccess()]
        public ActionResult getSearchUser(GridSettings gridSettings, string type, string values)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<UserModel> users = null;
            int totalRecords;

            UserModel filter = new UserModel();
            if (gridSettings.IsSearch)
            {
                filter.UserId = gridSettings.Where.rules.Any(r => r.field == "UserId") ?
                        Int32.Parse(gridSettings.Where.rules.FirstOrDefault(r => r.field == "UserId").data) : int.MinValue;
                filter.UserDomainName = gridSettings.Where.rules.Any(r => r.field == "UserDomainName") ?
                        gridSettings.Where.rules.FirstOrDefault(r => r.field == "UserDomainName").data : string.Empty;
                filter.UserFirstName = gridSettings.Where.rules.Any(r => r.field == "UserFirstName") ?
                        gridSettings.Where.rules.FirstOrDefault(r => r.field == "UserFirstName").data : string.Empty;
                filter.UserLastName = gridSettings.Where.rules.Any(r => r.field == "UserLastName") ?
                        gridSettings.Where.rules.FirstOrDefault(r => r.field == "UserLastName").data : string.Empty;
                filter.UserDomainName = gridSettings.Where.rules.Any(r => r.field == "UserDomainName") ?
                        gridSettings.Where.rules.FirstOrDefault(r => r.field == "UserDomainName").data : string.Empty;
                filter.EmailId = gridSettings.Where.rules.Any(r => r.field == "EmailId") ?
                        gridSettings.Where.rules.FirstOrDefault(r => r.field == "EmailId").data : string.Empty;
                filter.CompanyName = gridSettings.Where.rules.Any(r => r.field == "CompanyName") ?
                        gridSettings.Where.rules.FirstOrDefault(r => r.field == "CompanyName").data : string.Empty;
            }

            if (!string.IsNullOrEmpty(type))
            {
                switch (type)
                {
                    case "First Name":
                        filter.UserFirstName = values;
                        break;
                    case "Last Name":
                        filter.UserLastName = values;
                        break;
                    case "Domain Name":
                        filter.UserDomainName = values;
                        break;
                    case "Email ID":
                        filter.EmailId = values;
                        break;
                }
            }

            var q = from listusr in objMasterViewModel.GetAllUser()
                    select new UserModel
                    {
                        UserId = listusr.UserId,
                        UserFirstName = listusr.UserFirstName,
                        UserLastName = listusr.UserLastName,
                        UserDomainName = listusr.UserDomainName,
                        EmailId = listusr.EmailId,
                        CompanyName = listusr.CompanyName
                    };

            totalRecords = q.Count();

            if (!string.IsNullOrEmpty(filter.EmailId))
            {
                q = q.Where(c => c.EmailId.ToLower().StartsWith(filter.EmailId.ToLower()));
            }
            if (!string.IsNullOrEmpty(filter.CompanyName))
            {
                q = q.Where(c => c.CompanyName.ToLower().StartsWith(filter.CompanyName.ToLower()));
            }
            if (!string.IsNullOrEmpty(filter.UserDomainName))
            {
                q = q.Where(c => c.UserDomainName.ToLower().StartsWith(filter.UserDomainName.ToLower()));
            }

            if (filter.UserFirstName != null)
            {
                q = q.Where(c => c.UserFirstName.ToLower().StartsWith(filter.UserFirstName.ToLower()));
            }

            if (!string.IsNullOrEmpty(filter.UserLastName))
            {
                q = q.Where(c => c.UserLastName.ToLower().StartsWith(filter.UserLastName.ToLower()));
            }


            users = q.Skip((gridSettings.PageIndex - 1) * gridSettings.PageSize).Take(gridSettings.PageSize).ToList();

            var jsonData = new
            {
                total = totalRecords / gridSettings.PageSize + 1,
                page = gridSettings.PageIndex,
                records = totalRecords,
                rows = (
                    from c in users
                    select new
                    {
                        UserId = c.UserId,
                        UserFirstName = c.UserFirstName,
                        UserLastName = c.UserLastName,
                        UserDomainName = c.UserDomainName,
                        EmailId = c.EmailId,
                        CompanyName = c.CompanyName
                    })
            };
            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

        [Filters.Authorized()]
        public ActionResult GetUserforMAP(string type, string values)
        {

            MasterViewModel objMasterViewModel = new MasterViewModel();
            ViewUserMapModel objViewUserMapModel = new ViewUserMapModel();

            objViewUserMapModel.CurrentPage = 1;
            objViewUserMapModel.PageSize = CommonUtils.PageSize;
            objViewUserMapModel.TotalPages = 0;

            Session["Type"] = type;
            Session["Values"] = values;

            objMasterViewModel.SearchUser(type, values, objViewUserMapModel);
            return PartialView("UserSearchMain", objViewUserMapModel);
        }


        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult SearchUserList(ViewUserMapModel objViewUserMapModel)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            string type = Convert.ToString(Session["Type"]);
            string values = Convert.ToString(Session["Values"]);

            objMasterViewModel.SearchUser(type, values, objViewUserMapModel);
            return PartialView("UserSearchMain", objViewUserMapModel);
        }


        [Filters.Authorized()]
        //[AcceptVerbs(HttpVerbs.Get)]
        public ActionResult SelectUser(int userid)
        {
            MasterViewModel objmasterViewModel = new MasterViewModel();
            UserModel objUser;
            objUser = objmasterViewModel.CheckUserRoleExist(userid);
            if (objUser.ErrorCode.Equals(2))
            {
                var newlist = new UserModel
                {
                    UserId = 0
                };
                return Json(newlist, JsonRequestBehavior.AllowGet);

            }
            else
            {
                var classesList = objmasterViewModel.GetAllUser().Where(m => m.UserId == userid).Take(1).Single();
                var newlist = new UserModel
                {
                    UserId = classesList.UserId,
                    UserDomainName = classesList.UserDomainName,
                    UserFirstName = classesList.UserFirstName,
                    UserMiddleName = classesList.UserMiddleName,
                    LandLineNumber = classesList.LandLineNumber,
                    UserLastName = classesList.UserLastName,
                    Address = classesList.Address,
                    AlterEmailId = classesList.AlterEmailId,
                    MobileNumber = classesList.MobileNumber,
                    CompanyId = classesList.CompanyId,
                    CompanyName = classesList.CompanyName,
                    IsActive = classesList.IsActive,
                    EmailId = classesList.EmailId
                };
                return Json(newlist, JsonRequestBehavior.AllowGet);

            }
        }

        //[AcceptVerbs(HttpVerbs.Get)]
        public JsonResult SelectUserforTrainer(int Userid)
        {
            MasterViewModel objmasterViewModel = new MasterViewModel();
            UserModel objUser;
            objUser = objmasterViewModel.CheckDupTrainerDetails(Userid);
            if (objUser.ErrorCode.Equals(2))
            {
                var newlist = new UserModel
                {
                    UserId = 0
                };
                return Json(newlist, JsonRequestBehavior.AllowGet);

            }
            else
            {
                var classesList = objmasterViewModel.GetAllUser().Where(m => m.UserId == Userid).Take(1).Single();
                var newlist = new UserModel
                {
                    UserId = classesList.UserId,
                    UserDomainName = classesList.UserDomainName,
                    UserFirstName = classesList.UserFirstName,
                    UserLastName = classesList.UserLastName,
                    Address = classesList.Address,
                    MobileNumber = classesList.MobileNumber,
                    CompanyName = classesList.CompanyName,
                    EmailId = classesList.EmailId

                };
                return Json(newlist, JsonRequestBehavior.AllowGet);

            }
        }


        public JsonResult FillRollByTeamId(int teamid)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            int roleID = Convert.ToInt32(Session["RoleId"]);

            MasterViewModel objmasterViewModel = new MasterViewModel();

            var classesList = objmasterViewModel.GetAllRole(createdBy, roleID, "U").Where(m => m.TeamID == teamid);
            var classesData = classesList.Select(m => new SelectListItem()
            {
                Text = m.RoleName,
                Value = m.RoleID.ToString(),
            });
            return Json(classesData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult FillLevelByRoleID(int roleId)
        {
            MasterViewModel objmasterViewModel = new MasterViewModel();

            var classesList = objmasterViewModel.GetLevelbyRoleid(roleId);
            var classesData = classesList.Select(m => new SelectListItem()
            {
                Text = m.LevelName,
                Value = m.LevelID.ToString(),
            });
            return Json(classesData, JsonRequestBehavior.AllowGet);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult FillCircle()
        {
            MasterViewModel objmasterViewModel = new MasterViewModel();
            var classesList = objmasterViewModel.GetCircleMaster();
            var classesData = classesList.Select(m => new SelectListItem()
            {
                Text = m.CircleName,
                Value = m.CircleID.ToString(),
            });
            return Json(classesData, JsonRequestBehavior.AllowGet);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult FillCityByCircleId(int circleid)
        {
            MasterViewModel objmasterViewModel = new MasterViewModel();
            var classesList = objmasterViewModel.GetCityMaster().Where(m => m.CircleID == circleid);
            var classesData = classesList.Select(m => new SelectListItem()
            {
                Text = m.CityName,
                Value = m.CityID.ToString(),
            });
            return Json(classesData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult FillMaintenanceByCityId(int cityid)
        {
            MasterViewModel objmasterViewModel = new MasterViewModel();
            var classesList = objmasterViewModel.GetMaintenancePointMaster().Where(m => m.CityID == cityid);
            var classesData = classesList.Select(m => new SelectListItem()
            {
                Text = m.MaintenancePoint,
                Value = m.MaintenancePointId.ToString(),
            });
            return Json(classesData, JsonRequestBehavior.AllowGet);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveRoles(List<string> usermapping)
        {
            string result = String.Empty;
            if (usermapping != null)
            {
                List<UserRoleMappingModel> lstmap = AddMultipleRoleMap(usermapping);
                int userid = lstmap[0].UserID;

                int createdBy = Convert.ToInt32(Session["UserId"]);
                MasterViewModel objMasterViewModel = new MasterViewModel();
                IList<UserRoleMappingModel> objlstUserRoleMappingModel = new List<UserRoleMappingModel>();

                try
                {
                    int errorCode;
                    string errorMessage;
                    objlstUserRoleMappingModel = objMasterViewModel.InsertUserRoleMapping(lstmap, userid, createdBy, out errorCode, out errorMessage);
                    if (errorCode == 0)
                    {

                        result = "User Mapped Successfully";
                    }
                    else
                    {
                        result = errorMessage;
                    }

                }
                catch (Exception ex)
                {
                    Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", " SaveRoles", ex);
                    return Json(new { result = true, error = Session["ExceptionMsg"] }, JsonRequestBehavior.AllowGet);

                }
            }
            else
            {
                result = "Kindly add at least one role";
                return Json(new { success = false, message = result }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = result }, JsonRequestBehavior.AllowGet);

        }
        private List<UserRoleMappingModel> AddMultipleRoleMap(List<string> usersmap)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            List<UserRoleMappingModel> lstMappingRole = new List<UserRoleMappingModel>();
            try
            {

                for (int i = 0; i < usersmap.Count; i++)
                {
                    string[] maproles = usersmap[i].Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                    string[] maplocs = maproles[4].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                    for (int j = 0; j < maplocs.Length; j++)
                    {
                        UserRoleMappingModel objUserRoleMapping = new UserRoleMappingModel();
                        objUserRoleMapping.UserID = Convert.ToInt32(maproles[0]);
                        objUserRoleMapping.TeamID = Convert.ToInt32(maproles[1]);
                        objUserRoleMapping.RoleID = Convert.ToInt32(maproles[2]);
                        objUserRoleMapping.ReportingTo = Convert.ToInt32(0);
                        objUserRoleMapping.LevelID = Convert.ToInt32(maproles[3]);
                        objUserRoleMapping.CreatedBy = Convert.ToInt16(Session["UserID"]);
                        objUserRoleMapping.CreatedOn = DateTime.Now;
                        objUserRoleMapping.IsActive = true;

                        objUserRoleMapping.CircleCityAreaID = Convert.ToInt32(maplocs[j]);

                        lstMappingRole.Add(objUserRoleMapping);
                    }
                }
            }
            catch (Exception ex)
            {
                //Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "AddMultipleRoleMap", ex);
            }

            return lstMappingRole;
        }

        //[AcceptVerbs(HttpVerbs.Post)]
        //public JsonResult CheckInfraLead(int? roleId, int? circlecityId)
        //{
        //    UserRoleMappingModel objUserRole = new UserRoleMappingModel();
        //    MasterViewModel objMasterViewModel;
        //    int createdBy = Convert.ToInt32(Session["UserId"]);
        //    string result = String.Empty;
        //    try
        //    {

        //        objMasterViewModel = new MasterViewModel();
        //        objUserRole = objMasterViewModel.CheckInfraLeadExist(roleId, circlecityId);
        //        if (objUserRole.ErrorCode.Equals(2))
        //        {
        //            result = "Infra Lead " + objUserRole.InfraLeadName + " already exist for above Maintenance point";
        //        }
        //        else if (objUserRole.ErrorCode.Equals(0)) 
        //        { 

        //        } 
        //        else 
        //        {
        //            result = "Exception: " + HttpUtility.JavaScriptStringEncode(objUserRole.ErrorMessage);
        //        }
        //    }
        //    catch (Exception objException)
        //    {
        //        Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "CheckInfraLead", objException);
        //        return Json(new { success = false, error = Session["ExceptionMsg"] }, JsonRequestBehavior.AllowGet);
        //    }
        //    return Json(new { success = true, result = result }, JsonRequestBehavior.AllowGet);
        //}

        #endregion

        #region Question Type Master


        [Filters.MenuAccess()]
        public ActionResult ViewAssessmentType(string FilterQuestionTypeName, string FilterWeightage)
        {
            ViewQuestionTypeModel objViewQuestionTypeModel = new ViewQuestionTypeModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
            {
                objViewQuestionTypeModel.Message = Convert.ToString(Session["Message"]);
                objViewQuestionTypeModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }

            ViewBag.StatusList = new SelectList(new[]
                                          {
                                              new {ID="-1",Name="All"},
                                              new {ID="1",Name="Active"},
                                              new{ID="0",Name="InActive"},
                                          },
                         "ID", "Name", -1);

            objViewQuestionTypeModel.CurrentPage = 1;
            objViewQuestionTypeModel.PageSize = CommonUtils.PageSize;

            objViewQuestionTypeModel.TotalPages = 0;
            objViewQuestionTypeModel.FilterQuestionTypeName = FilterQuestionTypeName;
            objViewQuestionTypeModel.FilterWeightage = FilterWeightage;

            objMasterViewModel.SearchQuestionType(objViewQuestionTypeModel);

            return View(objViewQuestionTypeModel);
        }

        /// <summary>
        /// Show Training List based on paging and sorting filter
        /// </summary>
        /// <param name="objViewTrainingsModel"></param>
        /// <returns></returns>

        [Filters.Authorized()]
        [HttpPost]
        public ActionResult ViewAssessmentType(ViewQuestionTypeModel objViewQuestionTypeModel)
        {
            objViewQuestionTypeModel.Message = objViewQuestionTypeModel.MessageType = String.Empty;
            MasterViewModel objMasterViewModel = new MasterViewModel();
            objMasterViewModel.SearchQuestionType(objViewQuestionTypeModel);
            return PartialView("AssesementTypeList", objViewQuestionTypeModel);
        }

        [Filters.MenuAccess()]
        public ActionResult SaveAssessmentType(int? QuestionTypeID)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveQuestionTypeModel objSaveQuestionTypeModel = new SaveQuestionTypeModel();
            int QuestionTypeId;

            try
            {

                QuestionTypeId = QuestionTypeID != null ? (int)QuestionTypeID : 0;
                objSaveQuestionTypeModel = QuestionTypeID != 0 ? objMasterViewModel.GetQuestionTypeByQuestionTypeID(QuestionTypeId) : new SaveQuestionTypeModel();

                if (QuestionTypeId == 0)
                {
                    objSaveQuestionTypeModel.IsActive = true;
                }

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "QuestionType", "SaveQuestionTYpe Get", ex);
            }
            return View(objSaveQuestionTypeModel);
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveAssessmentType(SaveQuestionTypeModel objQuestionType)
        {

            int createdBy = Convert.ToInt32(Session["UserId"]);
            string strMessage = string.Empty;
            string strRetMessage = string.Empty;
            SaveQuestionTypeModel objQuestionTypeSave = new SaveQuestionTypeModel();
            string redirectUrl = string.Empty;
            objQuestionTypeSave.OperationType = "A";

            MasterViewModel objMasterViewModel;
            try
            {
                objMasterViewModel = new MasterViewModel();


                strMessage = objQuestionType.QuestionTypeID != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;
                if (ModelState.IsValid)
                {

                    if (objQuestionType.QuestionTypeID != 0)
                    {
                        objQuestionTypeSave.OperationType = "E";
                    }

                    objQuestionTypeSave.QuestionTypeName = objQuestionType.QuestionTypeName.Trim();
                    objQuestionTypeSave.Weightage = objQuestionType.Weightage;
                    objQuestionTypeSave.IsActive = objQuestionType.IsActive;
                    objQuestionTypeSave.QuestionTypeID = objQuestionType.QuestionTypeID;
                    objQuestionTypeSave.CreatedOn = DateTime.Now;
                    objQuestionTypeSave.CreatedBy = createdBy;

                    objQuestionTypeSave = objMasterViewModel.InsertUpdateDelQuestionType(objQuestionTypeSave);

                    if (objQuestionTypeSave.ErrorCode.Equals(0))
                    {
                        Session["Message"] = string.Format(strMessage, "Question Type");
                        Session["MessageType"] = MessageType.Success.ToString().ToLower();
                    }
                    else
                    {
                        Session["Message"] = objQuestionTypeSave.ErrorMessage;
                        Session["MessageType"] = MessageType.Error.ToString().ToLower();
                    }

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveQuestionType Post", ex);
            }

            redirectUrl = Url.Action("ViewAssessmentType");
            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });

        }

        public JsonResult IsQuestionTypeNameExist(string QuestionTypeName, int QuestionTypeID)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<QuestionTypeModel> lstQuestionType = (from a in objMasterViewModel.GetAllQuestionType()
                                                       where a.QuestionTypeName.ToLower().Trim() == QuestionTypeName.ToLower().Trim() && a.QuestionTypeID != QuestionTypeID
                                                       select a).ToList();

            return Json(lstQuestionType.Count == 0, JsonRequestBehavior.AllowGet);
        }


        public ActionResult DeleteQuestionType(int id, string FilterQuestionTypeName, string FilterWeightage)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            SaveQuestionTypeModel objQuestionTypeDelete = new SaveQuestionTypeModel();
            objQuestionTypeDelete.OperationType = "D";
            objQuestionTypeDelete.QuestionTypeID = id;

            MasterViewModel objMasterViewModel;
            string strRetMessage = string.Empty;
            try
            {
                objMasterViewModel = new MasterViewModel();

                objQuestionTypeDelete = objMasterViewModel.InsertUpdateDelQuestionType(objQuestionTypeDelete);

                if (objQuestionTypeDelete.ErrorCode.Equals(0))
                {
                    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Question Type") + "</div>";
                }
                else
                {
                    strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objQuestionTypeDelete.ErrorMessage) + "</div>";

                }
                return Json(new { url = Url.Action("ViewAssessmentType", new { pFilterQuestionTypeName = FilterQuestionTypeName, pFilterWeightage = FilterWeightage }) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "DeleteLevel", ex);
                strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + UserMessage.EXCEPTION_MESSAGE + "</div>";
            }
            return Json(new { url = Url.Action("ViewAssessmentType", new { pFilterQuestionTypeName = FilterQuestionTypeName, pFilterWeightage = FilterWeightage }) }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Feedback

        [Filters.MenuAccess()]
        public ActionResult ViewFeedback(string pFilterFeedbackDescription, string pFilterFeedbackType)
        {

            ViewFeedbackModel objViewFeedbackModel = new ViewFeedbackModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            IList<FeedbackTypeModel> lstFeedbackType = objMasterViewModel.GetAllFeedbackType();

            objViewFeedbackModel.CurrentPage = 1;
            objViewFeedbackModel.PageSize = CommonUtils.PageSize;

            objViewFeedbackModel.FilterFeedbackDescription = pFilterFeedbackDescription;
            objViewFeedbackModel.FilterFeedbackType = pFilterFeedbackType;

            ViewBag.FeedbackTypeList = new SelectList(lstFeedbackType, "FeedbackType", "FeedbackType", objViewFeedbackModel.FilterFeedbackType);

            objViewFeedbackModel.TotalPages = 0;
            objMasterViewModel.GetAllFeedbacks(objViewFeedbackModel);

            return View(objViewFeedbackModel);
        }

        [Filters.Authorized()]
        [HttpPost]
        public ActionResult ViewFeedback(ViewFeedbackModel objViewFeedbacksModel)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            objMasterViewModel.GetAllFeedbacks(objViewFeedbacksModel);
            return PartialView("FeedbackList", objViewFeedbacksModel);
        }


        [Filters.MenuAccess()]
        public ActionResult SaveFeedback(int? FeedbackID)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveFeedbackModel objSaveFeedbackModel = new SaveFeedbackModel();
            List<FeedBackFunctionCategoryModel> lstFunctionCategoryModel = new List<FeedBackFunctionCategoryModel>();
            int FeedbackId;
            try
            {
                FeedbackId = FeedbackID != null ? (int)FeedbackID : 0;
                objSaveFeedbackModel = FeedbackId != 0 ? objMasterViewModel.GetFeedbackByFeedbackID(FeedbackId) : new SaveFeedbackModel();

                string CategoryIDs = String.Empty;
                if (FeedbackId > 0)
                {

                    objSaveFeedbackModel.SelectedFunctionList = objMasterViewModel.GetFunctionsByFeedbackID(FeedbackId).ToList().OrderBy(o => o.CategoryID).ToList();
                    if (objSaveFeedbackModel.SelectedFunctionList != null)
                    {
                        foreach (FeedBackFunctionCategoryModel objFunctionCategory in objSaveFeedbackModel.SelectedFunctionList)
                        {
                            objFunctionCategory.IsSelected = true;
                        }

                        CategoryIDs = String.Join(",", objSaveFeedbackModel.SelectedFunctionList.Select(o => o.CategoryID).Distinct());

                        lstFunctionCategoryModel = objMasterViewModel.GetFunctionsByCategoryIds(CategoryIDs);
                        FillCategoryFunctionDetail(objSaveFeedbackModel, lstFunctionCategoryModel);
                        objSaveFeedbackModel.SelectedFunctionList = lstFunctionCategoryModel;
                    }
                }
                objSaveFeedbackModel.SelectedCategories = CategoryIDs;
                FillFeedbackTypeAndCategory(objSaveFeedbackModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Feedback", "SaveFeedback Get", ex);
            }
            return View(objSaveFeedbackModel);
        }


        [Filters.Authorized()]
        [HttpPost]
        public ActionResult SaveFeedback(SaveFeedbackModel objSaveFeedbackModel)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            try
            {
                MasterViewModel objMasterViewModel = new MasterViewModel();
                List<FeedBackFunctionCategoryModel> lstFunctionCategoryModel = new List<FeedBackFunctionCategoryModel>();
                FillFeedbackTypeAndCategory(objSaveFeedbackModel);
                switch (objSaveFeedbackModel.ActionType)
                {
                    case "changecategory":

                        lstFunctionCategoryModel = objMasterViewModel.GetFunctionsByCategoryIds(objSaveFeedbackModel.SelectedCategories);

                        if (objSaveFeedbackModel.SelectedFunctionList != null)
                        {
                            FillCategoryFunctionDetail(objSaveFeedbackModel, lstFunctionCategoryModel);
                        }
                        objSaveFeedbackModel.SelectedFunctionList = lstFunctionCategoryModel;
                        return PartialView("FeedbackCategoryFunctionList", objSaveFeedbackModel);

                    default:

                        foreach (FeedBackFunctionCategoryModel objSelectedFunctionCategoryModel in objSaveFeedbackModel.SelectedFunctionList)
                        {
                            if (objSelectedFunctionCategoryModel.IsSelected)
                            {
                                lstFunctionCategoryModel.Add(objSelectedFunctionCategoryModel);
                            }
                        }

                        string strCategoryFunctionListXml = CommonUtils.GetBulkXML(lstFunctionCategoryModel);
                        //objSaveFeedbackModel.FeedbackType = "Trainer";
                        objSaveFeedbackModel.IsActive = true;
                        objSaveFeedbackModel.CreatedOn = DateTime.Now;
                        objSaveFeedbackModel.CreatedBy = createdBy;
                        objSaveFeedbackModel = objMasterViewModel.InsertUpdateFeedbackDetail(objSaveFeedbackModel, strCategoryFunctionListXml);
                        break;
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Feedback", "SaveFeedback Get", ex);
            }
            return Json(new { url = Url.Action("ViewFeedback") });
        }


        public ActionResult DeleteFeedback(int id, string FilterFeedbackDescription, string FilterFeedbackType)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            ViewFeedbackModel objViewFeedbackModel = new ViewFeedbackModel();
            FeedbackModel objFeedbackModel = new FeedbackModel();

            MasterViewModel objMasterViewModel;
            string strRetMessage = string.Empty;
            try
            {
                objMasterViewModel = new MasterViewModel();

                objFeedbackModel.FeedbackID = id;

                objFeedbackModel = objMasterViewModel.DeleteFeedback(objFeedbackModel);

                if (objFeedbackModel.ErrorCode.Equals(0))
                {
                    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Feedback") + "</div>";
                }
                else
                {
                    strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objFeedbackModel.ErrorMessage) + "</div>";
                }

                return Json(new { url = Url.Action("ViewFeedback", new { pFilterFeedbackDescription = FilterFeedbackDescription, pFilterFeedbackType = FilterFeedbackType }) }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "DeleteFeedback", ex);
                strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + UserMessage.EXCEPTION_MESSAGE + "</div>";
            }
            return Json(new { url = Url.Action("ViewFeedback", new { pFilterFeedbackDescription = FilterFeedbackDescription, pFilterFeedbackType = FilterFeedbackType }) }, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Fill Category Function Detail in lstFunctionCategoryModel object from objSaveFeedbackModel object.
        /// </summary>
        /// <param name="objSaveFeedbackModel"></param>
        /// <param name="lstFunctionCategoryModel"></param>
        public void FillCategoryFunctionDetail(SaveFeedbackModel objSaveFeedbackModel, List<FeedBackFunctionCategoryModel> lstFunctionCategoryModel)
        {
            foreach (FeedBackFunctionCategoryModel objFunctionCategory in lstFunctionCategoryModel)
            {
                FeedBackFunctionCategoryModel objSelectedFunctionCategoryModel = objSaveFeedbackModel.SelectedFunctionList.Where(o => o.FunctionID == objFunctionCategory.FunctionID).SingleOrDefault();
                if (objSelectedFunctionCategoryModel != null)
                {
                    objFunctionCategory.FeedbackFunctionMappingID = objSelectedFunctionCategoryModel.FeedbackFunctionMappingID;
                    objFunctionCategory.FeedbackID = objSelectedFunctionCategoryModel.FeedbackID;
                    objFunctionCategory.FunctionID = objSelectedFunctionCategoryModel.FunctionID;
                    objFunctionCategory.FunctionName = objSelectedFunctionCategoryModel.FunctionName;
                    objFunctionCategory.CategoryID = objSelectedFunctionCategoryModel.CategoryID;
                    objFunctionCategory.CategoryName = objSelectedFunctionCategoryModel.CategoryName;
                    objFunctionCategory.IsSelected = objSelectedFunctionCategoryModel.IsSelected;
                    objFunctionCategory.IsActive = objSelectedFunctionCategoryModel.IsActive;
                    objFunctionCategory.CreatedOn = objSelectedFunctionCategoryModel.CreatedOn;
                    objFunctionCategory.CreatedBy = objSelectedFunctionCategoryModel.CreatedBy;
                }
            }
        }


        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetFunctions(string CategoryIds)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
            try
            {

                if (!String.IsNullOrEmpty(CategoryIds) && CategoryIds != "null")
                {
                    TrainingViewModel objTrainingViewModel = new TrainingViewModel();
                    lstFunctionCategoryModel = objTrainingViewModel.GetFunctionsByCategoryIds(CategoryIds);
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Feedback", "GetFunctions", ex);
            }
            return Json(lstFunctionCategoryModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Fill and select FeedbackType and Category dropdown values
        /// </summary>
        /// <param name="objSaveFeedbackModel"></param>
        public void FillFeedbackTypeAndCategory(SaveFeedbackModel objSaveFeedbackModel)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            //IList<FeedbackTypeModel> lstFeedbackType = objMasterViewModel.GetAllFeedbackType();
            IList<CategoryModel> lstCategory = objMasterViewModel.GetAllActiveCategory();

            //ViewBag.FeedbackTypeList = new SelectList(lstFeedbackType, "FeedbackTypeID", "FeedbackTypeName", objSaveFeedbackModel.FeedbackTypeID);
            ViewBag.CategoryList = new MultiSelectList(lstCategory, "CategoryID", "CategoryName", objSaveFeedbackModel.SelectedCategories);
        }

        #endregion

        #region Trainer

        [Filters.MenuAccess()]
        public ActionResult ViewTrainer(string pFilterTrainerType, string pFilterFirstName, string pFilterLastName, string pFilterDomainName)
        {

            ViewTrainerModel objViewTrainerModel = new ViewTrainerModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();



            ViewBag.FilterTrainerTypeList = new SelectList(new[]
                                          {
                                              new {ID="All",Name="All"},
                                              new {ID="Internal",Name="Internal"},
                                              new {ID="External",Name="External"},
                                          }, "ID", "Name");


            ViewBag.StatusList = new SelectList(new[]
                                          {
                                              new {ID="-1",Name="All"},
                                              new {ID="1",Name="Active"},
                                              new{ID="0",Name="InActive"},
                                          },
                         "ID", "Name", -1);

            objViewTrainerModel.CurrentPage = 1;
            objViewTrainerModel.PageSize = CommonUtils.PageSize;

            objViewTrainerModel.FilterTrainerType = pFilterTrainerType;
            objViewTrainerModel.FilterFirstName = pFilterFirstName;
            objViewTrainerModel.FilterLastName = pFilterLastName;
            objViewTrainerModel.FilterDomainName = pFilterDomainName;

            objViewTrainerModel.Message = "";
            objViewTrainerModel.MessageType = "";

            if (Session["Message"] != null && Session["MessageType"] != null)
            {
                objViewTrainerModel.Message = Convert.ToString(Session["Message"]);
                objViewTrainerModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }


            objViewTrainerModel.TotalPages = 0;
            objMasterViewModel.GetAllTrainer(objViewTrainerModel);

            return View(objViewTrainerModel);
        }

        [Filters.Authorized()]
        [HttpPost]
        public ActionResult ViewTrainer(ViewTrainerModel objViewTrainersModel)
        {

            MasterViewModel objMasterViewModel = new MasterViewModel();
            // objViewTrainersModel.Message = objMasterViewModel.MessageType = String.Empty;
            objMasterViewModel.GetAllTrainer(objViewTrainersModel);
            return PartialView("TrainerList", objViewTrainersModel);
        }


        [HttpPost]
        public ActionResult GetTraining(string id)
        {

            int createdBy = Convert.ToInt32(Session["UserId"]);

            MasterViewModel objMasterViewModel = new MasterViewModel();
            try
            {
                List<TrainingModel> lstTraining = objMasterViewModel.GetAllTrainingforTrainer(Convert.ToString(id)).ToList();
                SelectList objTraining = new SelectList(lstTraining, "TrainingID", "TrainingName", 0);
                return Json(objTraining);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Trainer Master", "GetTraining", ex);
                return Json(ex);
            }
        }


        [Filters.MenuAccess()]
        public ActionResult SaveTrainer(int? TrainerID, string Flag)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveTrainerModel objSaveTrainerModel = new SaveTrainerModel();

            int TrainerId;
            try
            {


                ViewBag.QualificationList = new SelectList((from c in objMasterViewModel.GetAllQualification()
                                                            select new { c.QualificationId, c.Qualification }).ToList(), "QualificationId", "Qualification");

                IList<TrainingCategoryModel> lstTrainingCategory = objMasterViewModel.GetAllTrainingCategory();
                IList<TrainingModel> lstTrainingModel = objMasterViewModel.GetAllTrainingforTrainer(string.Empty);


                if (Flag == "A")
                {
                    SaveTrainerModel obj = new SaveTrainerModel();
                    obj = objMasterViewModel.GetInternalTrainerDetail(Convert.ToInt32(TrainerID));
                    ViewBag.flag = 'A';

                    ViewBag.TrainingCategoryList = new MultiSelectList(lstTrainingCategory, "TrainingCategoryId", "TrainingCategoryName", objSaveTrainerModel.TrainingCategories);
                    return View("SaveTrainer", obj);
                }

                TrainerId = TrainerID != null ? (int)TrainerID : 0;



                objSaveTrainerModel = TrainerId != 0 ? objMasterViewModel.GetTrainerByTrainerID(TrainerId) : new SaveTrainerModel();

                if (TrainerId == 0)
                {
                    objSaveTrainerModel.IsActive = true;
                }

                ViewBag.TrainingCategoryList = new MultiSelectList(lstTrainingCategory, "TrainingCategoryId", "TrainingCategoryName", objSaveTrainerModel.TrainingCategories);
                ViewBag.TrainingList = new SelectList(lstTrainingModel, "TrainingId", "TrainingName", objSaveTrainerModel.TrainingCategories);


            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Trainer", "SaveTrainer Get", ex);
            }
            return View(objSaveTrainerModel);
        }

        [Filters.Authorized()]
        [HttpPost]
        public ActionResult SaveTrainer(SaveTrainerModel objSaveTrainerModel)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string strRetMessage = string.Empty;
            ViewTrainerModel objViewTrainerModel = new ViewTrainerModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();


            string redirectUrl = string.Empty;

            try
            {


                if (!String.IsNullOrEmpty(objSaveTrainerModel.ActionType))
                {
                    switch (objSaveTrainerModel.ActionType)
                    {

                        default:

                            string strMessage = string.Empty;
                            objSaveTrainerModel.OperationalType = "A";
                            strMessage = objSaveTrainerModel.TrainerID != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;

                            HttpFileCollectionBase files = Request.Files;
                            HttpPostedFileBase Resumefile = null;
                            HttpPostedFileBase Photofile = null;


                            if (files.Count > 0)
                            {
                                Resumefile = files[0];
                                Photofile = files[1];


                                if (Resumefile.ContentLength > 0)
                                {
                                    string fileName = Path.GetFileName(Resumefile.FileName);
                                    string extension = Path.GetExtension(Resumefile.FileName);
                                    fileName = fileName.Substring(0, fileName.LastIndexOf('.'));
                                    string filePath = string.Empty;

                                    string fName = fileName + "." + extension;

                                    //fName += DateTime.Now + DateTime.Now.Millisecond.ToString();
                                    //fName = fName.Replace("-", "").Replace(":", "").Replace(" ", "").Replace("/", "");
                                    //fName += extension;

                                    //filePath = Server.MapPath("~/" + ConfigurationManager.AppSettings["AppResumeFolderPath"].ToString() + "/") + fName;
                                    //file.SaveAs(filePath);

                                    MemoryStream memStream = new MemoryStream();
                                    Request.Files[0].InputStream.CopyTo(memStream);

                                    //byte[] fileContent = System.IO.File.ReadAllBytes(filePath);
                                    //System.IO.File.Delete(filePath);

                                    objSaveTrainerModel.ResumeContent = memStream.ToArray();
                                    objSaveTrainerModel.ResumeFile = fName;

                                }


                                if (Photofile.ContentLength > 0 && Photofile.ContentLength <= 1048576)
                                {
                                    string fileName = Path.GetFileName(Photofile.FileName);
                                    string extension = Path.GetExtension(Photofile.FileName);
                                    fileName = fileName.Substring(0, fileName.LastIndexOf('.'));
                                    string filePath = string.Empty;

                                    string fName = fileName + "." + extension;

                                    //fName += DateTime.Now + DateTime.Now.Millisecond.ToString();
                                    //fName = fName.Replace("-", "").Replace(":", "").Replace(" ", "").Replace("/", "");
                                    //fName += extension;

                                    //filePath = Server.MapPath("~/" + ConfigurationManager.AppSettings["AppResumeFolderPath"].ToString() + "/") + fName;
                                    //file.SaveAs(filePath);

                                    System.Drawing.Bitmap bmpPostedImage = new System.Drawing.Bitmap(Request.Files[1].InputStream);
                                    System.Drawing.Image objImage;

                                    objImage = ScaleImage(bmpPostedImage, 300, 300, 325);

                                    MemoryStream memStream = new MemoryStream();

                                    if (Photofile.ContentType == "image/pjpeg" || Photofile.ContentType == "image/jpeg")
                                    {
                                        objImage.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    }
                                    else if (Photofile.ContentType == "image/gif")
                                    {
                                        objImage.Save(memStream, System.Drawing.Imaging.ImageFormat.Gif);
                                    }
                                    else
                                    {
                                        objImage.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    }

                                    // Request.Files[1].InputStream.CopyTo(memStream);

                                    //byte[] fileContent = System.IO.File.ReadAllBytes(filePath);
                                    //System.IO.File.Delete(filePath);

                                    objSaveTrainerModel.PhotoContent = memStream.ToArray();
                                    objSaveTrainerModel.PhotoFile = fName;

                                }
                                else if (Photofile.ContentLength > 1048576)  // Greater than 1 MB
                                {
                                    Session["Message"] = "Trainer Photo Size cannot exceed 1 MB";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("ViewTrainer");
                                }
                            }




                            if (objSaveTrainerModel.strTrainerInternal == "Internal" && objSaveTrainerModel.TrainerID == 0)
                            {

                                objSaveTrainerModel.FirstName = objSaveTrainerModel.hdnFirstName;
                                objSaveTrainerModel.LastName = objSaveTrainerModel.hdnLastName;
                                objSaveTrainerModel.DomainName = objSaveTrainerModel.hdnDomainName;
                                objSaveTrainerModel.EmailAddress = objSaveTrainerModel.hdnEmailAddress;
                                objSaveTrainerModel.ContactNumber = objSaveTrainerModel.hdnContactNumber;
                                objSaveTrainerModel.AddressDtl = objSaveTrainerModel.hdnAddressDtl;
                                objSaveTrainerModel.CompanyName = objSaveTrainerModel.hdnCompanyName;

                            }

                            ViewBag.TrainingCategoryList = new SelectList((from c in objMasterViewModel.GetAllTrainingCategory()
                                                                           select new { c.TrainingCategoryId, c.TrainingCategoryName }).ToList(), "TrainingCategoryId", "TrainingCategoryName");

                            ViewBag.QualificationList = new SelectList((from c in objMasterViewModel.GetAllQualification()
                                                                        select new { c.QualificationId, c.Qualification }).ToList(), "QualificationId", "Qualification");

                            if (objSaveTrainerModel.TrainerID != 0)
                            {
                                objSaveTrainerModel.OperationalType = "E";

                                if (Resumefile.ContentLength == 0)
                                {
                                    objSaveTrainerModel.ResumeFile = objSaveTrainerModel.hdnResumeFile;
                                    objSaveTrainerModel.ResumeContent = objSaveTrainerModel.hdnResumeContent;
                                }

                                if (Photofile.ContentLength == 0)
                                {
                                    objSaveTrainerModel.PhotoFile = objSaveTrainerModel.hdnPhotoFile;
                                    objSaveTrainerModel.PhotoContent = objSaveTrainerModel.hdnPhotoContent;
                                }

                                if (objSaveTrainerModel.IsInternalTrainer == true)
                                {

                                    objSaveTrainerModel.FirstName = objSaveTrainerModel.hdnFirstName;
                                    objSaveTrainerModel.LastName = objSaveTrainerModel.hdnLastName;
                                    objSaveTrainerModel.DomainName = objSaveTrainerModel.hdnDomainName;
                                    objSaveTrainerModel.EmailAddress = objSaveTrainerModel.hdnEmailAddress;
                                    objSaveTrainerModel.ContactNumber = objSaveTrainerModel.hdnContactNumber;
                                    objSaveTrainerModel.AddressDtl = objSaveTrainerModel.hdnAddressDtl;
                                    objSaveTrainerModel.CompanyName = objSaveTrainerModel.hdnCompanyName;

                                }

                            }

                            //objSaveTrainerModel.IsActive = true;
                            //objSaveTrainerModel.CreatedOn = DateTime.Now;
                            objSaveTrainerModel.CreatedBy = createdBy;
                            objSaveTrainerModel = objMasterViewModel.InsertUpdateDeleteTrainerDetail(objSaveTrainerModel);


                            if (objSaveTrainerModel.ErrorCode.Equals(0))
                            {
                                Session["Message"] = string.Format(strMessage, "Trainer");
                                Session["MessageType"] = MessageType.Success.ToString().ToLower();
                            }
                            else
                            {
                                Session["Message"] = objSaveTrainerModel.ErrorMessage;
                                Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                return RedirectToAction("ViewTrainer");
                            }


                            ViewBag.FilterTrainerTypeList = new SelectList(new[]
                                          {
                                              new {ID="All",Name="All"},
                                              new {ID="Internal",Name="Internal"},
                                              new {ID="External",Name="External"},
                                          }, "ID", "Name");
                            objViewTrainerModel.Trainer = new List<TrainerModelFull>();
                            //redirectUrl = Url.Action("ViewTraining");

                            if (objSaveTrainerModel.ActionType != "addtraining" && objSaveTrainerModel.ActionType != "addLocation")
                            {
                                return RedirectToAction("ViewTrainer");
                            }

                            break;

                    }

                    if (objSaveTrainerModel.ActionType == "addtraining")
                    {
                        return RedirectToAction("AsssignTraining", "Master", new { tID = objSaveTrainerModel.TrainerID });
                    }

                    if (objSaveTrainerModel.ActionType == "addLocation")
                    {
                        return RedirectToAction("TrainerLocation", "Master", new { tID = objSaveTrainerModel.TrainerID });
                    }

                }

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Trainer", "SaveTrainer POst", ex);
            }

            return null;
            //return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });

            //return Json(new { url = Url.Action("ViewTrainer") });
            // return Json(new { url = Url.Action("ViewTrainer", new { pFilterTrainerType = "", pFilterFirstName = "", pFilterLastName = "", pFilterDomainName = "" }) }, JsonRequestBehavior.AllowGet);


            //return View("ViewTrainer", objViewTrainerModel); 

            // return RedirectToAction("ViewTrainer");

            //string FilterTrainerType = String.Empty;
            //string FilterFirstName = String.Empty;
            //string FilterLastName = String.Empty;
            //string FilterDomainName = String.Empty;

            //redirectUrl = "ViewTrainer?pFilterTrainerType='" + FilterTrainerType + "'&pFilterFirstName='" + FilterFirstName + "'&pFilterLastName = '" + FilterLastName + "' &pFilterDomainName='" + FilterDomainName + "'";

            ////redirectUrl = Url.Action("ViewTrainer", new { pFilterTrainerType = FilterTrainerType, pFilterFirstName = FilterFirstName, pFilterLastName = FilterLastName, pFilterDomainName = FilterDomainName });

            //return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"]});

        }


        static System.Drawing.Image ScaleImage(System.Drawing.Image image, int maxHeight, int intWidth, int intHeight)
        {

            var newWidth = intWidth;
            var newHeight = intHeight;
            var newImage = new Bitmap(image.Width, image.Height);

            newImage = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(newImage))
            {
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            if (newWidth == 300 && newHeight == 325)
            {
                newImage.SetResolution(300.0f, 300.0f);
            }




            return newImage;
        }



        public ActionResult AsssignTraining(int tID)
        {
            AssignTrainingListModel objAssignTrainingListModel = new AssignTrainingListModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();
            int createdBy = Convert.ToInt32(Session["UserId"]);

            if (Session["Message"] != null && Session["MessageType"] != null)
            {
                Session["Message"] = null;
                Session["MessageType"] = null;
            }
            if (Session["msgContent"] != null && Session["msgContentType"] != null)
            {

                objAssignTrainingListModel.Message = Convert.ToString(Session["msgContent"]);
                objAssignTrainingListModel.MessageType = Convert.ToString(Session["msgContentType"]);
                Session["msgContent"] = null;
                Session["msgContentType"] = null;

            }

            try
            {
                objAssignTrainingListModel.TrainerData = objMasterViewModel.GetTrainerByTrainerID(tID);
                if (objAssignTrainingListModel.TrainerData == null)
                {
                    objAssignTrainingListModel.TrainerData = new SaveTrainerModel();
                }

                objAssignTrainingListModel.TrainingList = objMasterViewModel.GetAssignedTrainings(objAssignTrainingListModel).ToList();

                FillTraining(objAssignTrainingListModel.TrainerData.TrainerID, objAssignTrainingListModel.TrainerData.TrainingCategories);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "AssignTrainer Get", ex);
            }
            return View(objAssignTrainingListModel);
        }

        [HttpPost]
        //[Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult AssignTraining(AssignTrainingListModel objAssignTrainingListModel, HttpPostedFileBase file)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            try
            {

                MasterViewModel objMasterViewModel = new MasterViewModel();
                TrainingViewModel objTrainingViewModel = new TrainingViewModel();
                AttachmentModel objAttachmentModel = new AttachmentModel();

                if (objAssignTrainingListModel.ActionType != null)
                {
                    switch (objAssignTrainingListModel.ActionType)
                    {
                        case "AddTraining":
                            if (file != null && file.ContentLength > 0)
                            {

                                MemoryStream memStream = new MemoryStream();
                                file.InputStream.CopyTo(memStream);
                                objAttachmentModel.AttachmentName = objAssignTrainingListModel.SelectedFileName;
                                objAttachmentModel.AttachmentSize = Math.Round(Convert.ToDecimal(file.ContentLength), 2);
                                objAttachmentModel.AttachmentType = file.ContentType;
                                objAttachmentModel.AttachmentContent = memStream.ToArray();
                                objAttachmentModel.IsActive = true;
                                objAttachmentModel.CreatedBy = createdBy;
                                objAttachmentModel.CreatedOn = DateTime.Now;
                            }

                            objAssignTrainingListModel.IsActive = true;
                            objAssignTrainingListModel.CreatedBy = createdBy;
                            objAssignTrainingListModel.CreatedOn = DateTime.Now;

                            objMasterViewModel.InsertUpdateTrainingAndCertificate(objAssignTrainingListModel, objAttachmentModel);
                            break;
                        case "deleteTraining":
                            objMasterViewModel.DeleteAssignedTraining(objAssignTrainingListModel);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "AssignTrainer Post", ex);
            }
            Session["msgContent"] = objAssignTrainingListModel.Message;
            Session["msgContentType"] = objAssignTrainingListModel.MessageType;
            return RedirectToAction("AsssignTraining", "Master", new { tID = objAssignTrainingListModel.TrainerData.TrainerID });
        }

        public ActionResult GetCertificateFile(long id)
        {

            int createdBy = Convert.ToInt32(Session["UserId"]);
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
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


        public void FillTraining(int TrainerID, string TrainingCategory)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            IList<TrainingModel> lstTraining = objMasterViewModel.GetTrainingByTrainerID(TrainerID, TrainingCategory);
            ViewBag.TrainingList = new SelectList(lstTraining, "TrainingID", "TrainingName", 0);
        }


        public ActionResult TrainerLocation(int tID)
        {
            AssignExternalTrainerLocListModel objAssignExternalTrainerLocListModel = new AssignExternalTrainerLocListModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();
            int createdBy = Convert.ToInt32(Session["UserId"]);

            if (Session["Message"] != null && Session["MessageType"] != null)
            {
                Session["Message"] = null;
                Session["MessageType"] = null;
            }

            int UserId = Convert.ToInt32(Session["UserId"]);
            int RoleId = Convert.ToInt32(Session["RoleId"]);

            List<CircleModel> Circles = this.GetCirles(UserId, RoleId);
            List<CityModel> Cities = new List<CityModel>();
            List<MaintenancePointModel> MaintanencePoints = new List<MaintenancePointModel>();


            objAssignExternalTrainerLocListModel.LevelList = objMasterViewModel.GetAllLevel().Where(s => s.IsActive == true).ToList();
            ViewBag.LevelList = new SelectList(objAssignExternalTrainerLocListModel.LevelList, "LevelID", "LevelName", objAssignExternalTrainerLocListModel.SelectLevelID);


            ViewBag.CircleList = new SelectList(Circles, "CircleID", "CircleName", null);
            ViewBag.CircleMultiList = new MultiSelectList(Circles, "CircleID", "CircleName", null);

            ViewBag.CityList = new SelectList(Cities, "CityID", "CityName", null);
            ViewBag.CityMultiList = new MultiSelectList(Cities, "CityID", "CityName", null);

            ViewBag.MaintanencePointList = new MultiSelectList(MaintanencePoints, "MaintenancePointId", "MaintenancePoint", null);

            try
            {

                objAssignExternalTrainerLocListModel = objMasterViewModel.GetAssignTrainerLocByTrainerID(tID);

                if (objAssignExternalTrainerLocListModel.hdnLevelID == 2)  // Circle
                {
                    ViewBag.CircleList = new SelectList(Circles, "CircleID", "CircleName", null);
                    ViewBag.CircleMultiList = new MultiSelectList(Circles, "CircleID", "CircleName", objAssignExternalTrainerLocListModel.SelectCircleCityAreaID.Split(",".ToCharArray()));
                }

                if (objAssignExternalTrainerLocListModel.hdnLevelID == 3)  // City
                {

                    ViewBag.CircleList = new SelectList(Circles, "CircleID", "CircleName", objAssignExternalTrainerLocListModel.CircleID);
                    ViewBag.CircleMultiList = new MultiSelectList(Circles, "CircleID", "CircleName", null);

                    List<CityModel> lstCities = objMasterViewModel.GetUserwiseCity(UserId, RoleId, objAssignExternalTrainerLocListModel.CircleID).ToList<CityModel>();

                    ViewBag.CityList = new SelectList(lstCities, "CityID", "CityName", null);
                    ViewBag.CityMultiList = new MultiSelectList(lstCities, "CityID", "CityName", objAssignExternalTrainerLocListModel.SelectCircleCityAreaID.Split(",".ToCharArray()));

                }

                if (objAssignExternalTrainerLocListModel.hdnLevelID == 4)  // MP
                {

                    ViewBag.CircleList = new SelectList(Circles, "CircleID", "CircleName", objAssignExternalTrainerLocListModel.CircleID);
                    ViewBag.CircleMultiList = new MultiSelectList(Circles, "CircleID", "CircleName", null);

                    List<CityModel> lstCities = objMasterViewModel.GetUserwiseCity(UserId, RoleId, objAssignExternalTrainerLocListModel.CircleID).ToList<CityModel>();

                    ViewBag.CityList = new SelectList(lstCities, "CityID", "CityName", objAssignExternalTrainerLocListModel.CityID);
                    ViewBag.CityMultiList = new MultiSelectList(lstCities, "CityID", "CityName", null);

                    List<MaintenancePointModel> lstMaintanences = objMasterViewModel.GetUserwiseMaintenancePoint(UserId, RoleId, objAssignExternalTrainerLocListModel.CircleID, objAssignExternalTrainerLocListModel.CityID).ToList<MaintenancePointModel>();
                    ViewBag.MaintanencePointList = new MultiSelectList(lstMaintanences, "MaintenancePointId", "MaintenancePoint", objAssignExternalTrainerLocListModel.SelectCircleCityAreaID.Split(",".ToCharArray()));
                }

                // objAssignExternalTrainerLocListModel.TrainerData = objMasterViewModel.GetTrainerByTrainerID(tID);
                //FillTraining(objAssignExternalTrainerLocListModel.TrainerData.TrainerID, objAssignTrainingListModel.TrainerData.TrainingCategories);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "AssignTrainer Get", ex);
            }

            if (Session["msgContent"] != null && Session["msgContentType"] != null)
            {
                objAssignExternalTrainerLocListModel.Message = Convert.ToString(Session["msgContent"]);
                objAssignExternalTrainerLocListModel.MessageType = Convert.ToString(Session["msgContentType"]);
                Session["msgContent"] = null;
                Session["msgContentType"] = null;
            }

            return View(objAssignExternalTrainerLocListModel);
        }

        [HttpPost]
        //[Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult TrainerLocation(AssignExternalTrainerLocListModel objAssignExternalTrainerLocListModel)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            try
            {

                MasterViewModel objMasterViewModel = new MasterViewModel();
                TrainingViewModel objTrainingViewModel = new TrainingViewModel();
                AttachmentModel objAttachmentModel = new AttachmentModel();

                if (objAssignExternalTrainerLocListModel.SelectLevelID == 1)
                {
                    objAssignExternalTrainerLocListModel.SelectCircleCityAreaID = "0";
                }


                objAssignExternalTrainerLocListModel.IsActive = true;
                objAssignExternalTrainerLocListModel.CreatedBy = createdBy;
                objAssignExternalTrainerLocListModel.CreatedOn = DateTime.Now;
                objMasterViewModel.InsertUpdateTrainerLocation(objAssignExternalTrainerLocListModel);


            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "TrainerLocation Post", ex);
            }

            Session["msgContent"] = objAssignExternalTrainerLocListModel.Message;
            Session["msgContentType"] = objAssignExternalTrainerLocListModel.MessageType;

            return RedirectToAction("TrainerLocation", "Master", new { tID = objAssignExternalTrainerLocListModel.SelectTrainerID });

        }



        private List<CircleModel> GetCirles(int UserId, int RoleId)
        {
            List<CircleModel> lstCircles = new List<CircleModel>();
            try
            {
                MasterViewModel objMasterViewModel = new MasterViewModel();
                lstCircles = objMasterViewModel.GetUserwiseCircles(UserId, RoleId).ToList<CircleModel>();

                if (lstCircles == null)
                    lstCircles = new List<CircleModel>();

                lstCircles.Insert(0, new CircleModel { CircleID = 0, CircleName = "--Select--" });
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "GetCirles Get", ex);
            }
            return lstCircles;
        }


        public ActionResult GetFile(int id)
        {

            MasterViewModel objMasterViewModel = new MasterViewModel();
            TrainerModel objTrainerModel = new TrainerModel();

            objTrainerModel.TrainerID = id;

            objTrainerModel = objMasterViewModel.GetResume(objTrainerModel);

            if (objTrainerModel != null)
            {
                Byte[] data = (Byte[])objTrainerModel.ResumeContent;
                return new DownloadResult
                {
                    DocumentData = data,
                    DownloadFileName = objTrainerModel.ResumeFile
                };
            }
            return new DownloadResult
            {
                DocumentData = null,
                DownloadFileName = objTrainerModel.ResumeFile
            };
        }


        public ActionResult GetPhoto(int id)
        {

            MasterViewModel objMasterViewModel = new MasterViewModel();
            TrainerModel objTrainerModel = new TrainerModel();

            objTrainerModel.TrainerID = id;

            objTrainerModel = objMasterViewModel.GetTrainerPhoto(objTrainerModel);

            if (objTrainerModel != null)
            {
                Byte[] data = (Byte[])objTrainerModel.PhotoContent;
                return new DownloadResult
                {
                    DocumentData = data,
                    DownloadFileName = objTrainerModel.PhotoFile
                };
            }
            return new DownloadResult
            {
                DocumentData = null,
                DownloadFileName = objTrainerModel.PhotoFile
            };
        }


        public ActionResult DeleteTrainer(int id, string FilterTrainerType, string FilterFirstName, string FilterLastName, string FilterDomainName)
        {

            int createdBy = Convert.ToInt32(Session["UserId"]);
            SaveTrainerModel objSaveTrainerModel = new SaveTrainerModel();
            TrainerModel objTrainerModel = new TrainerModel();

            MasterViewModel objMasterViewModel;
            string strRetMessage = string.Empty;

            try
            {
                objMasterViewModel = new MasterViewModel();

                objSaveTrainerModel.TrainerID = id;
                objSaveTrainerModel.CreatedBy = createdBy;
                objSaveTrainerModel.OperationalType = "D";

                objSaveTrainerModel = objMasterViewModel.InsertUpdateDeleteTrainerDetail(objSaveTrainerModel);

                if (objSaveTrainerModel.ErrorCode.Equals(0))
                {
                    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Trainer") + "</div>";
                }
                else
                {
                    strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objSaveTrainerModel.ErrorMessage) + "</div>";
                }

                return Json(new { url = Url.Action("ViewTrainer", new { pFilterTrainerType = FilterTrainerType, pFilterFirstName = FilterFirstName, pFilterLastName = FilterLastName, pFilterDomainName = FilterDomainName }) }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "DeleteTrainer", ex);
                strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + UserMessage.EXCEPTION_MESSAGE + "</div>";
            }
            return Json(new { url = Url.Action("ViewTrainer", new { pFilterTrainerType = FilterTrainerType, pFilterFirstName = FilterFirstName, pFilterLastName = FilterLastName, pFilterDomainName = FilterDomainName }) }, JsonRequestBehavior.AllowGet);

        }


        [Filters.Authorized()]
        public ActionResult GetUserforInternalTrainer()
        {

            MasterViewModel objMasterViewModel = new MasterViewModel();
            ViewUserMapModel objViewUserMapModel = new ViewUserMapModel();

            objViewUserMapModel.CurrentPage = 1;
            objViewUserMapModel.PageSize = CommonUtils.PageSize;
            objViewUserMapModel.TotalPages = 0;

            objMasterViewModel.SearchTrainerUser(null, null, objViewUserMapModel);
            return PartialView("InternalTrainerMain", objViewUserMapModel);
        }


        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult SearchInternalTrainerList(ViewUserMapModel objViewUserMapModel)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            objMasterViewModel.SearchTrainerUser(null, null, objViewUserMapModel);
            return PartialView("InternalTrainerMain", objViewUserMapModel);
        }


        #endregion

        #region Assesement

        //
        // GET: /Training/
        /// <summary>
        /// Show Assesement List
        /// </summary>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult ViewAssesement()
        {
            ViewAssessmentModel objViewAssesementModel = new ViewAssessmentModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();

            if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
            {
                objViewAssesementModel.Message = Convert.ToString(Session["Message"]);
                objViewAssesementModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }

            objViewAssesementModel.CurrentPage = 1;
            objViewAssesementModel.PageSize = CommonUtils.PageSize;

            objViewAssesementModel.TotalPages = 0;
            objMasterViewModel.GetAllAssessment(objViewAssesementModel);

            IList<CategoryModel> lstCategory = objTrainingViewModel.GetAllActiveCategory();
            ViewBag.CategoryList = new MultiSelectList(lstCategory, "CategoryID", "CategoryName", objViewAssesementModel.FilterCategoryIDs);


            ViewBag.StatusList = new SelectList(new[]
                                          {
                                              new {ID="-1",Name="All"},
                                              new {ID="1",Name="Active"},
                                              new{ID="0",Name="InActive"},
                                          },
                          "ID", "Name", -1);

            return View(objViewAssesementModel);

        }

        /// <summary>
        /// Show Assesement List based on paging and sorting filter
        /// </summary>
        /// <param name="objViewTrainingsModel"></param>
        /// <returns></returns>
        [Filters.Authorized()]
        [HttpPost]
        public ActionResult ViewAssesement(ViewAssessmentModel objViewAssesementModel)
        {
            objViewAssesementModel.Message = objViewAssesementModel.MessageType = String.Empty;
            MasterViewModel objMasterViewModel = new MasterViewModel();

            // objViewAssesementModel.FilterCategoryIDs = "1";

            objMasterViewModel.GetAllAssessment(objViewAssesementModel);
            return PartialView("AssesementList", objViewAssesementModel);
        }

        [Filters.MenuAccess()]
        public ActionResult SaveAssesement(int? AssessmentID)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveAssessmentModel objSaveAssessmentModel = new SaveAssessmentModel();

            List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
            int AssesementId;
            try
            {
                AssesementId = AssessmentID != null ? (int)AssessmentID : 0;

                objSaveAssessmentModel = AssesementId != 0 ? objMasterViewModel.GetAssesementByAssesementID(AssesementId) : new SaveAssessmentModel();
                string CategoryIDs = String.Empty;



                if (AssesementId > 0)
                {

                    //objSaveAssessmentModel.SelectedFunctionList = objMasterViewModel.GetFunctionsByTrainingID(AssesementId).ToList().OrderBy(o => o.CategoryID).ToList();

                    //if (objSaveAssessmentModel.SelectedFunctionList != null)
                    //{
                    //    foreach (FunctionCategoryModel objFunctionCategory in objSaveAssessmentModel.SelectedFunctionList)
                    //    {
                    //        objFunctionCategory.IsSelected = true;
                    //        if (!CategoryIDs.Contains(objFunctionCategory.CategoryID.ToString()))
                    //        {
                    //            CategoryIDs += objFunctionCategory.CategoryID;
                    //        }
                    //    }

                    //lstFunctionCategoryModel = objTrainingViewModel.GetFunctionsByCategoryIds(CategoryIDs);
                    //FillCategoryFunctionDetail(objSaveTrainingModel, lstFunctionCategoryModel);
                    //objSaveTrainingModel.SelectedFunctionList = lstFunctionCategoryModel;
                    //}
                }
                else
                {
                    objSaveAssessmentModel.IsActive = true;
                }
                //objSaveAssessmentModel.SelectedCategories = CategoryIDs;
                FillCategory(objSaveAssessmentModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveAssesement Get", ex);
            }
            return View(objSaveAssessmentModel);
        }


        /// <summary>
        /// Save detail of Assesement
        /// </summary>
        /// <param name="objSaveTrainingModel"></param>
        /// <returns></returns>

        [Filters.Authorized()]
        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveAssesement(SaveAssessmentModel objSaveAssessmentModel)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string redirectUrl = string.Empty;
            try
            {

                String strMessage = objSaveAssessmentModel.AssessmentID != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;

                if (objSaveAssessmentModel.AssessmentID != 0)
                {
                    objSaveAssessmentModel.OperationType = "E";
                }
                else if (objSaveAssessmentModel.AssessmentID == 0)
                {
                    objSaveAssessmentModel.OperationType = "A";
                }


                MasterViewModel objMasterViewModel = new MasterViewModel();
                //objSaveAssessmentModel.IsActive = true;
                objSaveAssessmentModel.CreatedOn = DateTime.Now;
                objSaveAssessmentModel.CreatedBy = createdBy;

                objSaveAssessmentModel = objMasterViewModel.InsertUpdateDeleteAssesement(objSaveAssessmentModel);

                if (objSaveAssessmentModel.ErrorCode.Equals(0))
                {
                    Session["Message"] = string.Format(strMessage, "Assesement");
                    Session["MessageType"] = MessageType.Success.ToString().ToLower();
                }
                else
                {
                    Session["Message"] = objSaveAssessmentModel.ErrorMessage;
                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                }

                redirectUrl = Url.Action("ViewAssesement");


            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assesement", "SaveAssesement POst", ex);
            }
            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });

        }

        /// <summary>
        /// Fill Category Function Detail in lstFunctionCategoryModel object from objSaveTrainingModel object.
        /// </summary>
        /// <param name="objSaveTrainingModel"></param>
        /// <param name="lstFunctionCategoryModel"></param>
        public void FillCategoryDetail(SaveAssessmentModel objSaveAssessmentModel, List<AssessmentCategory> lstAssessmentCategory)
        {

            //foreach (AssessmentCategory objAssessmentCategory in lstAssessmentCategory)
            //{

            //    FunctionCategoryModel objSelectedFunctionCategoryModel = objSaveAssessmentModel.SelectedFunctionList.Where(o => o.FunctionID == objFunctionCategory.FunctionID).SingleOrDefault();

            //    if (objSelectedFunctionCategoryModel != null)
            //    {
            //        objFunctionCategory.TrainingFunctionMappingID = objSelectedFunctionCategoryModel.TrainingFunctionMappingID;
            //        objFunctionCategory.CategoryID = objSelectedFunctionCategoryModel.CategoryID;
            //        objFunctionCategory.CategoryName = objSelectedFunctionCategoryModel.CategoryName;
            //        objFunctionCategory.IsSelected = objSelectedFunctionCategoryModel.IsSelected;
            //        objFunctionCategory.IsActive = objSelectedFunctionCategoryModel.IsActive;
            //        objFunctionCategory.CreatedOn = objSelectedFunctionCategoryModel.CreatedOn;
            //        objFunctionCategory.CreatedBy = objSelectedFunctionCategoryModel.CreatedBy;
            //    }

            //}
        }

        /// <summary>
        /// Fill and select TrainingType and Category dropdown values
        /// </summary>
        /// <param name="objSaveTrainingModel"></param>

        public void FillCategory(SaveAssessmentModel objSaveAssessmentModel)
        {

            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            IList<CategoryModel> lstCategory = objTrainingViewModel.GetAllActiveCategory();

            ViewBag.CategoryList = new MultiSelectList(lstCategory, "CategoryID", "CategoryName", objSaveAssessmentModel.SelectedCategories);

            ViewBag.QuestionType = new SelectList((from c in objMasterViewModel.GetAllQuestionType()
                                                   select new { c.QuestionTypeID, c.QuestionTypeName }).ToList(), "QuestionTypeID", "QuestionTypeName", objSaveAssessmentModel.QuestionTypeID);


        }

        public JsonResult GetWeightagebyQuestionTypeID(int QuestionTypeID)
        {
            MasterViewModel objmasterViewModel = new MasterViewModel();
            String strweightage = objmasterViewModel.GetWeightagebyQuestionTypeID(QuestionTypeID);
            return Json(strweightage, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region Employee
        [Filters.MenuAccess()]
        public ActionResult ViewEmployee(string pFilterDomainName)
        {
            ViewEmployeeModel objViewEmployeeModel = new ViewEmployeeModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            objViewEmployeeModel.CurrentPage = 1;
            objViewEmployeeModel.PageSize = CommonUtils.PageSize;

            objViewEmployeeModel.FilterDomainName = pFilterDomainName;

            objViewEmployeeModel.Message = "";
            objViewEmployeeModel.MessageType = "";

            if (Session["Message"] != null && Session["MessageType"] != null)
            {
                objViewEmployeeModel.Message = Convert.ToString(Session["Message"]);
                objViewEmployeeModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }
            objViewEmployeeModel.TotalPages = 0;
            objMasterViewModel.GetAllEmployee(objViewEmployeeModel);
            return View(objViewEmployeeModel);
        }
        [Filters.Authorized()]
        [HttpPost]
        public ActionResult ViewEmployee(ViewEmployeeModel objViewEmployeeModel)
        {

            MasterViewModel objMasterViewModel = new MasterViewModel();
            // objViewTrainersModel.Message = objMasterViewModel.MessageType = String.Empty;
            objMasterViewModel.GetAllEmployee(objViewEmployeeModel);
            return PartialView("EmployeeList", objViewEmployeeModel);
        }
        [Filters.MenuAccess()]
        public ActionResult SaveEmployee(int? EmployeeID, string UserID, string Flag)/**/
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveEmployeeModel objSaveEmployeeModel = new SaveEmployeeModel();
            int employeeId;
            try
            {
                ViewBag.SearchName = new SelectList(new[] { new 
                    { ID = "0", Name = "--Select--" }
                    , new { ID = "1", Name = "First Name" }
                    , new { ID = "2", Name = "Last Name" }
                    , new { ID = "3", Name = "Domain Name" }
                    , new { ID = "4", Name = "Email ID" }
                    , }, "ID", "Name", 0);
                ViewBag.EmployeeStatusList = new SelectList(new[] { new 
                    { ID = "0", Name = "---Select---" }
                    , new { ID = "E", Name = "Employee" }
                    , new { ID = "C", Name = "Contractor" }
                    , }, "ID", "Name");

                if (Flag == "A")
                {
                    //SaveEmployeeModel obj = new SaveEmployeeModel();
                    objSaveEmployeeModel = objMasterViewModel.GetInternalUserDetail(Convert.ToInt32(UserID));
                    //ViewBag.flag = 'A';
                    objSaveEmployeeModel.IsActive = true;
                    return View("SaveEmployee", objSaveEmployeeModel);
                }

                employeeId = EmployeeID != null ? (int)EmployeeID : 0;
                if (employeeId != 0)
                {
                    objSaveEmployeeModel = objMasterViewModel.GetEmployeeByEmployeeID(employeeId);
                    objSaveEmployeeModel.PhotoContentUploaded = objMasterViewModel.GetGetEmployeePhotoByEmployeeIDEmployeePhotoId(employeeId, null);
                }
                else
                    objSaveEmployeeModel = new SaveEmployeeModel();
                if (employeeId == 0)
                {
                    objSaveEmployeeModel.IsActive = true;
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Employee", "SaveEmployee Get", ex);
            }
            return View(objSaveEmployeeModel);
        }

        [Filters.Authorized()]
        [HttpPost]
        public ActionResult SaveEmployee(SaveEmployeeModel objSaveEmployeeModel)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string redirectUrl = string.Empty;
            try
            {
                String strMessage = objSaveEmployeeModel.EmployeeId != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;
                if (objSaveEmployeeModel.EmployeeId != 0)
                {
                    objSaveEmployeeModel.OperationType = "E";
                }
                else if (objSaveEmployeeModel.EmployeeId == 0)
                {
                    objSaveEmployeeModel.OperationType = "A";
                }
                MasterViewModel objMasterViewModel = new MasterViewModel();
                HttpFileCollectionBase files = Request.Files;
                HttpPostedFileBase file = null;
                if (files.Count > 0)
                {
                    file = files[0];
                    if (file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);
                        string extension = Path.GetExtension(file.FileName);
                        fileName = fileName.Substring(0, fileName.LastIndexOf('.'));
                        string filePath = string.Empty;

                        string fName = fileName + "." + extension;

                        MemoryStream memStream = new MemoryStream();
                        Request.Files[0].InputStream.CopyTo(memStream);

                        objSaveEmployeeModel.PhotoContent = memStream.ToArray();
                        objSaveEmployeeModel.PhotoFile = fName;
                    }
                }
                objSaveEmployeeModel.CreatedBy = createdBy;
                objSaveEmployeeModel = objMasterViewModel.InsertUpdateDeleteEmployee(objSaveEmployeeModel);

                if (objSaveEmployeeModel.ErrorCode.Equals(0))
                {
                    Session["Message"] = string.Format(strMessage, "Employee");
                    Session["MessageType"] = MessageType.Success.ToString().ToLower();
                }
                else
                {
                    Session["Message"] = objSaveEmployeeModel.ErrorMessage;
                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                }
                redirectUrl = Url.Action("ViewEmployee");
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Employee", "SaveEmployee POst", ex);
            }
            return RedirectToAction("ViewEmployee");
        }
        [Filters.Authorized()]
        public ActionResult GetInternalUserData(string type, string values)
        {

            MasterViewModel objMasterViewModel = new MasterViewModel();
            ViewUserMapModel objViewUserMapModel = new ViewUserMapModel();

            objViewUserMapModel.CurrentPage = 1;
            objViewUserMapModel.PageSize = CommonUtils.PageSize;
            objViewUserMapModel.TotalPages = 0;

            Session["Type"] = type;
            Session["Values"] = values;

            objMasterViewModel.SearchUser(type, values, objViewUserMapModel);
            return PartialView("EmployeeSearchMain", objViewUserMapModel);
        }
        [Filters.Authorized()]
        //[AcceptVerbs(HttpVerbs.Get)]
        public ActionResult SelectEmployee(string EmailID)
        {
            MasterViewModel objmasterViewModel = new MasterViewModel();
            UserModel objUser;
            objUser = objmasterViewModel.CheckEmployeeExist(EmailID);
            if (objUser.ErrorCode.Equals(2))
            {
                var newlist = new UserModel
                {
                    UserId = 0
                };
                return Json(newlist, JsonRequestBehavior.AllowGet);

            }
            else
            {
                var classesList = objmasterViewModel.GetAllUser().Where(m => m.EmailId == EmailID).Take(1).Single();
                var newlist = new UserModel
                {
                    UserId = classesList.UserId,
                    UserDomainName = classesList.UserDomainName,
                    UserFirstName = classesList.UserFirstName,
                    UserMiddleName = classesList.UserMiddleName,
                    LandLineNumber = classesList.LandLineNumber,
                    UserLastName = classesList.UserLastName,
                    Address = classesList.Address,
                    AlterEmailId = classesList.AlterEmailId,
                    MobileNumber = classesList.MobileNumber,
                    CompanyId = classesList.CompanyId,
                    CompanyName = classesList.CompanyName,
                    IsActive = classesList.IsActive,
                    EmailId = classesList.EmailId
                };
                return Json(newlist, JsonRequestBehavior.AllowGet);

            }
        }
        public ActionResult GetPhotoFile(int EmployeePhotoId, int I)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            EmployeePhoto objEmployeePhoto = new EmployeePhoto();

            objEmployeePhoto.EmployeePhotoId = EmployeePhotoId;

            objEmployeePhoto = objMasterViewModel.GetGetEmployeePhotoByEmployeeIDEmployeePhotoId(null, objEmployeePhoto.EmployeePhotoId).FirstOrDefault();

            if (objEmployeePhoto != null)
            {
                Byte[] data = (Byte[])objEmployeePhoto.EmployeePhotoContent;
                return new DownloadResult
                {
                    DocumentData = data,
                    DownloadFileName = objEmployeePhoto.EmployeePhotoId + "_" + I + ".jpg"
                };
            }
            return new DownloadResult
            {
                DocumentData = null,
                DownloadFileName = objEmployeePhoto.EmployeePhotoId + "_" + I + ".jpg"
            };
        }
        [Filters.Authorized()]
        public JsonResult DeleteEmployeePhoto(int EmployeePhotoId)
        {
            try
            {
                MasterViewModel objMasterViewModel = new MasterViewModel();
                EmployeePhoto objEmployeePhoto = new EmployeePhoto();
                objEmployeePhoto = objMasterViewModel.DeleteEmployeePhoto(EmployeePhotoId);
                string result;
                if (objEmployeePhoto.ErrorCode.Equals(0))
                {
                    result = "Employee Photo Deleted Successfully.";
                }
                else
                {
                    result = "Exception: " + HttpUtility.JavaScriptStringEncode(objEmployeePhoto.ErrorMessage);
                }
                Session["Message"] = result;
                Session["MessageType"] = MessageType.Success.ToString().ToLower();
                return Json(new { success = true, result = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                int createdBy = Convert.ToInt32(Session["UserId"]);
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Employee", "DeleteEmployeePhoto", ex);
                return Json(new { success = true, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        /*, string Flag*/
        [Filters.MenuAccess()]
        public ActionResult SaveEmployeeJioCenter(int? EmployeeJIOCenterId, string EmployeeCode, string EmployeeName)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveEmployeeJioCenterModel objSaveEmployeeJioCenterModel = new SaveEmployeeJioCenterModel();
            int employeeJIOCenterId;
            try
            {
                employeeJIOCenterId = EmployeeJIOCenterId != null ? (int)EmployeeJIOCenterId : 0;
                if (employeeJIOCenterId != 0)
                {
                    objSaveEmployeeJioCenterModel = objMasterViewModel.GetEmployeeJioCenterByEmpJIOCenterId(employeeJIOCenterId).FirstOrDefault();
                }
                else
                {
                    objSaveEmployeeJioCenterModel = new SaveEmployeeJioCenterModel();
                    objSaveEmployeeJioCenterModel.IsActive = true;
                }

                IList<EmployeeModel> lstEmployee = objMasterViewModel.GetEmployeeForDDL();
                ViewBag.L1List = new SelectList(from c in lstEmployee select new { L1Code = c.EmployeeCode, L1Name = c.EmployeeName }, "L1Code", "L1Name", objSaveEmployeeJioCenterModel.L1Code);
                ViewBag.L2List = new SelectList(lstEmployee, "EmployeeCode", "EmployeeName", objSaveEmployeeJioCenterModel.L1Code);

                IList<JIOCenterModelList> lstJIOCenterList = objMasterViewModel.GetJioCenterForDDL();
                ViewBag.JioCenterList = new SelectList(lstJIOCenterList, "FacilityCode", "FacilityName", objSaveEmployeeJioCenterModel.FacilityCode);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Employee", "SaveEmployee Get", ex);
            }
            return View(objSaveEmployeeJioCenterModel);
        }

        [Filters.Authorized()]
        [HttpPost]
        public ActionResult SaveEmployeeJioCenter(SaveEmployeeJioCenterModel objSaveEmployeeJioCenterModel)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string redirectUrl = string.Empty;
            try
            {
                String strMessage = objSaveEmployeeJioCenterModel.EmployeeJIOCenterId != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;
                if (objSaveEmployeeJioCenterModel.EmployeeJIOCenterId != 0 & objSaveEmployeeJioCenterModel.EmployeeJIOCenterId != null)
                {
                    objSaveEmployeeJioCenterModel.OperationType = "E";
                }
                else if (objSaveEmployeeJioCenterModel.EmployeeJIOCenterId == 0 | objSaveEmployeeJioCenterModel.EmployeeJIOCenterId == null)
                {
                    objSaveEmployeeJioCenterModel.OperationType = "A";
                }
                MasterViewModel objMasterViewModel = new MasterViewModel();
                objSaveEmployeeJioCenterModel.CreatedBy = createdBy;
                objSaveEmployeeJioCenterModel = objMasterViewModel.InsertUpdateEmployeeJioCenter(objSaveEmployeeJioCenterModel);

                if (objSaveEmployeeJioCenterModel.ErrorCode.Equals(0))
                {
                    Session["Message"] = string.Format(strMessage, "Employee Facility Mapping");
                    Session["MessageType"] = MessageType.Success.ToString().ToLower();
                }
                else
                {
                    Session["Message"] = objSaveEmployeeJioCenterModel.ErrorMessage;
                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                }
                redirectUrl = Url.Action("ViewEmployee");
                //RedirectToAction("ViewEmployee");
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Employee", "SaveEmployeeJioCenter POst", ex);
            }
            return RedirectToAction("ViewEmployee");
        }
        #endregion

        #region TrainingCategory

        [Filters.MenuAccess()]
        public ActionResult ViewTrainingCategory()
        {
            ViewTrainingCategoryModel objViewTrainingCategoryeModel = new ViewTrainingCategoryModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
            {
                objViewTrainingCategoryeModel.Message = Convert.ToString(Session["Message"]);
                objViewTrainingCategoryeModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }

            ViewBag.StatusList = new SelectList(new[]
                                          {
                                              new {ID="-1",Name="All"},
                                              new {ID="1",Name="Active"},
                                              new{ID="0",Name="InActive"},
                                          },
                         "ID", "Name", -1);

            objViewTrainingCategoryeModel.CurrentPage = 1;
            objViewTrainingCategoryeModel.PageSize = CommonUtils.PageSize;

            objViewTrainingCategoryeModel.TotalPages = 0;

            objMasterViewModel.SearchTrainingCategory(objViewTrainingCategoryeModel);

            return View(objViewTrainingCategoryeModel);
        }


        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewTrainingCategory(ViewTrainingCategoryModel objViewTrainingCategoryModel)
        {
            objViewTrainingCategoryModel.Message = objViewTrainingCategoryModel.MessageType = String.Empty;
            MasterViewModel objMasterViewModel = new MasterViewModel();
            objMasterViewModel.SearchTrainingCategory(objViewTrainingCategoryModel);
            return PartialView("TrainingCategoryList", objViewTrainingCategoryModel);
        }


        [Filters.MenuAccess()]
        public ActionResult SaveTrainingCategory(int? TrainingCategoryID)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            SaveTrainingCategoryModel objTrainingCategory = new SaveTrainingCategoryModel();
            int TrainingCategoryId;

            try
            {

                TrainingCategoryId = TrainingCategoryID != null ? (int)TrainingCategoryID : 0;
                ViewBag.TrainingCategoryId = TrainingCategoryID != null ? (int)TrainingCategoryID : 0;
                objTrainingCategory = TrainingCategoryId != 0 ? objMasterViewModel.GetTrainingCatByTrainingCatID(TrainingCategoryId) : new SaveTrainingCategoryModel();

                if (TrainingCategoryId == 0)
                {
                    objTrainingCategory.IsActive = true;
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "Save TrainingCategory Get", ex);
            }
            return View("SaveTrainingCategory", objTrainingCategory);
        }


        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTrainingCategory(SaveTrainingCategoryModel objTrainingCategory)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string strMessage = string.Empty;
            string strRetMessage = string.Empty;
            string redirectUrl = string.Empty;
            SaveTrainingCategoryModel objTrainingCategorySave = new SaveTrainingCategoryModel();
            objTrainingCategorySave.OperationType = "A";

            MasterViewModel objMasterViewModel;
            try
            {
                objMasterViewModel = new MasterViewModel();
                strMessage = objTrainingCategory.TrainingCategoryId != 0 ? UserMessage.SUCCESS_MESSAGE_EDIT : UserMessage.SUCCESS_MESSAGE_CREATE;


                if (ModelState.IsValid)
                {

                    if (objTrainingCategory.TrainingCategoryId != 0)
                    {
                        objTrainingCategorySave.OperationType = "E";
                    }

                    objTrainingCategorySave.TrainingCategoryName = objTrainingCategory.TrainingCategoryName.Trim();

                    objTrainingCategorySave.IsActive = objTrainingCategory.IsActive;
                    objTrainingCategorySave.TrainingCategoryId = objTrainingCategory.TrainingCategoryId;

                    objTrainingCategorySave.CreatedOn = DateTime.Now;
                    objTrainingCategorySave.CreatedBy = createdBy;

                    objTrainingCategorySave = objMasterViewModel.InsertUpdateDelTrainingCategory(objTrainingCategorySave);


                    if (objTrainingCategorySave.ErrorCode.Equals(0))
                    {
                        Session["Message"] = string.Format(strMessage, "Training Category");
                        Session["MessageType"] = MessageType.Success.ToString().ToLower();
                    }
                    else
                    {
                        Session["Message"] = objTrainingCategorySave.ErrorMessage;
                        Session["MessageType"] = MessageType.Error.ToString().ToLower();
                    }

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveTrainingCategory Post", ex);
            }

            redirectUrl = Url.Action("ViewTrainingCategory");

            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });

        }


        //public JsonResult IsTrainingCategoryNameExist(string TrainingCategoryName, int TrainingCategoryID)
        //{
        //    MasterViewModel objMasterViewModel = new MasterViewModel();
        //    List<TrainingCategoryModel> lstTrainingCategory = (from a in objMasterViewModel.GetAllTrainingCategory(null, null, string.Empty)
        //                               where a.TrainingCategoryName.ToLower().Trim() == TrainingCategoryName.ToLower().Trim() && a.TrainingCategoryID != TrainingCategoryID
        //                               select a).ToList();

        //    return Json(lstTrainingCategory.Count == 0, JsonRequestBehavior.AllowGet);
        //}



        public ActionResult DeleteTrainingCategory(int id)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            SaveTrainingCategoryModel objTrainingCategoryDelete = new SaveTrainingCategoryModel();
            objTrainingCategoryDelete.OperationType = "D";
            objTrainingCategoryDelete.TrainingCategoryId = id;

            MasterViewModel objMasterViewModel;
            string strRetMessage = string.Empty;
            try
            {
                objMasterViewModel = new MasterViewModel();

                objTrainingCategoryDelete = objMasterViewModel.InsertUpdateDelTrainingCategory(objTrainingCategoryDelete);

                if (objTrainingCategoryDelete.ErrorCode.Equals(0))
                {

                    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "TrainingCategory") + "</div>";
                }
                else
                {
                    strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTrainingCategoryDelete.ErrorMessage) + "</div>";

                }
                return Content(strRetMessage);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "DeleteLevel", ex);
                strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + UserMessage.EXCEPTION_MESSAGE + "</div>";
            }
            return Content(strRetMessage);
        }

        #endregion

        #region Certificate

        CertificateTemplateViewModel objCertificateViewModel = new CertificateTemplateViewModel();

        [Filters.MenuAccess()]
        public ActionResult ViewCertificateTemplate()
        {

            CertificateTemplateFull objCertificateTemplateFull = new CertificateTemplateFull();
            try
            {
                GetCertificateDetail(objCertificateTemplateFull);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "ViewCertificateTemplate Get", ex);

            }
            return View("viewCertificateTemplate", objCertificateTemplateFull);
        }
        public void GetCertificateDetail(CertificateTemplateFull objCertificateTemplateFull)
        {
            objCertificateTemplateFull.CurrentPage = 1;
            objCertificateTemplateFull.PageSize = CommonUtils.PageSize;
            objCertificateTemplateFull.TotalPages = 0;
            objCertificateTemplateFull = objCertificateViewModel.GetCertificateTemplate(objCertificateTemplateFull);
        }


       
        [HttpPost]
        [ValidateInput(false)]
        [Filters.Authorized()]
        public ActionResult ViewCertificateTemplate(CertificateTemplateFull objCertificateTemplateFull)
        {


            CertificateTemplateViewModel objCertificateViewModel = new CertificateTemplateViewModel();
            try
            {
                if (objCertificateTemplateFull != null && objCertificateTemplateFull.ActionType == "delete")
                {

                    objCertificateTemplateFull = objCertificateViewModel.DelCertificateTemplate(objCertificateTemplateFull);
                }
                objCertificateTemplateFull = objCertificateViewModel.GetCertificateTemplate(objCertificateTemplateFull);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "ViewCertificateTemplate Post", ex);

            }
            return PartialView("_CertificateTemplateList", objCertificateTemplateFull);
        }



        [Filters.MenuAccess()]
        public ActionResult SaveCertificateTemplate(int CTID = 0)
        {

            CertificateTemplateFull objCertificateTemplateFull = new CertificateTemplateFull();

            try
            {
                objCertificateTemplateFull.CurrentPage = 1;
                objCertificateTemplateFull.PageSize = CommonUtils.PageSize;
                objCertificateTemplateFull.TotalPages = 0;
                objCertificateTemplateFull.CertificateTemplateID = CTID;
                objCertificateTemplateFull = objCertificateViewModel.GetCertificateTemplate(objCertificateTemplateFull);
                GetPlaceHolder();
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveCertificateTemplate Get", ex);

            }
            return View("SaveCertificateTemplate", objCertificateTemplateFull);
        }

        [HttpPost]
        [ValidateInput(false)]
        [Filters.Authorized()]
        public ActionResult SaveCertificateTemplate(CertificateTemplateFull objCertificateTemplateFull)
        {

            try
            {
                if (objCertificateTemplateFull != null && objCertificateTemplateFull.ActionType == "savecertificatetemplate")
                {
                    objCertificateTemplateFull.CreatedBy = createdBy;
                    objCertificateTemplateFull.IsActive = true;

                    objCertificateTemplateFull = objCertificateViewModel.InsertUpdateCertificateTemplate(objCertificateTemplateFull);
                    GetCertificateDetail(objCertificateTemplateFull);
                    
                    return RedirectToAction("viewCertificateTemplate", "Master");

                }
               // GetPlaceHolder();
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveCertificateTemplate Get", ex);

            }
            return View("SaveCertificateTemplate", objCertificateTemplateFull);
        }


        ///// <summary>
        ///// Get Place Holder list 
        ///// </summary>

        ///// <returns></returns>
        //private void GetPlaceHolder()
        //{
        //    List<PlaceHolderModel> lstPlaceHolderModel = new List<PlaceHolderModel>();
        //    try
        //    {

        //        lstPlaceHolderModel = objCertificateViewModel.GetPlaceHolder().ToList<PlaceHolderModel>();

        //        if (lstPlaceHolderModel == null)
        //            lstPlaceHolderModel = new List<PlaceHolderModel>();

        //        lstPlaceHolderModel.Insert(0, new PlaceHolderModel { PlaceHolderName = "", PropertyName = "--Select--" });
        //    }
        //    catch (Exception ex)
        //    {
        //        Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "GetPlaceHolder Get", ex);
        //    }
        //    ViewBag.PlaceHolderList = new SelectList(lstPlaceHolderModel, "PlaceHolderName", "PropertyName", null);
        //}


        /// <summary>
        /// Get Place Holder list
        /// </summary> 
        /// 
       
        public ActionResult GetPlaceHolder( )
        {
            try
            {
                List<PlaceHolderModel> lstPlaceHolderModel = new List<PlaceHolderModel>();
                lstPlaceHolderModel = objCertificateViewModel.GetPlaceHolder().ToList<PlaceHolderModel>();

                if (lstPlaceHolderModel == null)
                    lstPlaceHolderModel = new List<PlaceHolderModel>();
                return Json(lstPlaceHolderModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "GetPlaceHolder Get", ex);

                return Json(ex);
            }
        }
        #endregion

    }

    
}