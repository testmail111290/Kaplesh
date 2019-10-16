using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
//using Microsoft.Practices.Unity;
//using System.DirectoryServices.AccountManagement;
using System.Configuration;
using CTMS.Common;
using CTMS.Models;
using CTMS.ViewModel;
using System.Web.Configuration;
using System.Web.Security;


namespace CTMS.Controllers
{
 
    public class HomeController : Controller
    {
        CommonUtils objCommonUtil = new CommonUtils();

        public ActionResult Index(int? id = null)
        {

            MasterViewModel objMenuViewModel = new MasterViewModel();
            if (id != null) return View();
            string DisplayName = string.Empty;

            if (id == null)
            {
                DisplayName = GetDomainUserName();
                 AuthenticationSection authSection = (AuthenticationSection)ConfigurationManager.GetSection("system.web/authentication");
                 if (authSection.Mode.ToString().ToLower() == "forms" && String.IsNullOrEmpty(DisplayName))
                 {
                     return RedirectToAction("Login", "User");
                 }
                 GetUserData(DisplayName);
            }
            if (Session["UserFirstLastName"] == null || Session["UserId"] == null)
                return View("Unauthorised");

            ViewBag.Message = Session["UserFirstLastName"];
            if (ViewBag.RoleList != null)
            {
                List<RoleModel> lstRole = (List<RoleModel>)ViewBag.RoleList;
                if (lstRole.Count == 1)
                {
                    var MenuIDs = objMenuViewModel.GetAllRoleBasedMenuAccess().Where(M => M.RoleID == lstRole[0].RoleID).Select(M => M.MenuID);
                    Session["Menu"] = objMenuViewModel.GetAllMenu().Where(M => MenuIDs.Contains(M.MenuID)).ToList();
                    Session["RoleId"] = lstRole[0].RoleID;
                    Session["RoleName"] = lstRole[0].RoleName;
                    //added by harsh- used to change the dashboard
                    Session["RoleType"] = lstRole[0].RoleType;
                }
            }
            return View();
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult MenuBasedRole(int roleid, string rolename, string roleType)
        {
            MasterViewModel objMenuViewModel = new MasterViewModel();
            var MenuIDs = objMenuViewModel.GetAllRoleBasedMenuAccess().Where(M => M.RoleID == roleid).Select(M => M.MenuID);
            Session["Menu"] = objMenuViewModel.GetAllMenu().Where(M => MenuIDs.Contains(M.MenuID)).ToList();
            Session["RoleId"] = roleid;
            Session["RoleName"] = rolename;
            //added by harsh- used to change the dashboard
            Session["RoleType"] = roleType;
            return Json("Success", JsonRequestBehavior.AllowGet);
        }


        public string GetDomainUserName()
        {
            string strUser = string.Empty;
            string usrName = string.Empty;
            try
            {
                 AuthenticationSection authSection = (AuthenticationSection)ConfigurationManager.GetSection("system.web/authentication");
                 if (authSection.Mode.ToString().ToLower() == "windows")
                 {
                     if (HttpContext.User.Identity.IsAuthenticated)
                     {
                         if (HttpContext.User.Identity.Name != null)
                         {
                             usrName = HttpContext.User.Identity.Name;
                         }
                     }

                     string[] uName = usrName.Split('\\');
                     strUser = uName[1].ToString();
                 }
                 else
                 {
                     if (Session["ExternalUserDomain"] != null && Convert.ToString(Session["ExternalUserDomain"]) != "")
                     {
                         strUser = Session["ExternalUserDomain"].ToString();
                     }
                 }
            }
            catch (Exception ex)
            {
                return strUser;
            }
            return strUser;
        }

        public void GetUserData(string domainName, int? intId = null)
        {
            try
            {
                HomeModel objHomeModel = new HomeModel();
                MasterViewModel objMasterViewModel = new MasterViewModel();
                UserModel objuser;
                //if (intId == null)
                //    //objuser = objMasterViewModel.GetAllUser().Where(u => u.UserDomainName.ToLower().Equals(domainName.ToLower())).SingleOrDefault();
                //    objuser = objMasterViewModel.GetAllUser().Where(u => u.UserDomainName != null && u.UserDomainName.ToLower().Equals(domainName.ToLower())).SingleOrDefault();

                //else
                    objuser = objMasterViewModel.GetAllUser().Where(u => u.UserId == 1).SingleOrDefault();

                if (objuser != null)
                {
                    Session["UserId"] = objuser.UserId;
                    Session["UserEmailID"] = objuser.EmailId;
                    Session["UserName"] = objuser.UserFirstName + " " + objuser.UserMiddleName + " " + objuser.UserLastName;
                    Session["UserFirstLastName"] = objuser.UserFirstName + " " + objuser.UserLastName;
                    Session["AlterEmailID"] = objuser.AlterEmailId;

                    ViewBag.RoleList = objMasterViewModel.GetUserRoleMappingbyUserId(objuser.UserId);
                }
            }
            catch (Exception ex)
            {
                //LoggingManager.Log(ex, EnumLogType.IsErrorEnabled);
            }
        }


        public ActionResult LogOff()
        {
             AuthenticationSection authSection = (AuthenticationSection)ConfigurationManager.GetSection("system.web/authentication");
             if (authSection.Mode.ToString().ToLower() == "windows")
             {
                 Session.Abandon();
                 Session.RemoveAll();
                 return RedirectToAction("PreLogin", "Home");
             }
             else
             {
                 //Disable back button In all browsers.
                 Response.Cache.SetCacheability(HttpCacheability.NoCache);
                 Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
                 Response.Cache.SetNoStore();
                 


                 FormsAuthentication.SignOut();
                 System.Web.HttpContext.Current.Session.Clear();
                 System.Web.HttpContext.Current.Session.Abandon();
                 return RedirectToAction("Login", "User");
             }
        }

        public ActionResult PreLogin()
        {
            return View();
        }
        /// <summary>
        /// Added by Niklank Jain
        /// </summary>
        /// <returns></returns>
        public ActionResult Error()
        {
            if (Session["ExceptionMsg"] != null)
            {
                ViewData["ExceptionMsg"] = Session["ExceptionMsg"];
                Session["ExceptionMsg"] = null;
            }

            ViewData["UserSession"] = Session["UserSession"];

            return View();
        }
        // Added By Niklank Jain 
        public ActionResult PageNotFound()
        {
            return View();
        }

        [Filters.Authorized()]
        public ActionResult Home()
        {
            if (Session["PasswordChangeSuccess"] != null && Convert.ToString(Session["PasswordChangeSuccess"]) != "")
            {
                string successmessage = Session["PasswordChangeSuccess"].ToString();
                TempData["PasswordChangeSuccess"] = successmessage;
                    Session["PasswordChangeSuccess"] = null;
            }
            if (Session["PasswordModifiedDate"] != null && Convert.ToString(Session["PasswordModifiedDate"]) != "")
            {
                DateTime lastpasswordModifiedDate = Convert.ToDateTime(Session["PasswordModifiedDate"].ToString());
                DateTime passwordExpiryDate = lastpasswordModifiedDate.AddDays(CommonUtils.ExpirePasswordDays);
                Double remainingDays = Math.Ceiling((passwordExpiryDate.Date - DateTime.Now.Date).TotalDays);
                if(remainingDays >=0 && remainingDays <= CommonUtils.PasswordExpireNotificationDays)
                {
                    String message = string.Empty;
                    if (remainingDays == 0)
                    {
                        message = Resources.UserResource.msgTodayPasswordExpireNotification;
                    }
                    else
                    {
                        message = String.Format(Resources.UserResource.msgPasswordExpireNotification, remainingDays);
                    }
                    ViewBag.PassWordNotificationMessage = message;
                }



            }
            DashboardModel mdlDashboard = new DashboardModel();
            HomeViewModel objHomeViewModel = new HomeViewModel();
            PublishedTrainingViewModel objPublishTrainingViewModel = new PublishedTrainingViewModel();
            mdlDashboard.FilterMonth = DateTime.Now.Month;
            mdlDashboard.FilterYear = DateTime.Now.Year;
            mdlDashboard.MonthStartDate = new DateTime(mdlDashboard.FilterYear, mdlDashboard.FilterMonth, 1);

            //Value will be changed corresponding to User and Role
            if (Session["RoleType"] != null && Session["RoleType"].ToString().ToLower() == "administrator")
            {
                objHomeViewModel.FillAdminDashBoardData(ref mdlDashboard);
                mdlDashboard.FilterMonth = DateTime.Now.Month;
                mdlDashboard.FilterYear = DateTime.Now.Year;
                mdlDashboard.MonthStartDate = new DateTime(mdlDashboard.FilterYear, mdlDashboard.FilterMonth, 1);
                mdlDashboard.DashboardName = "_AdminDashboard";
                mdlDashboard.ScheduledTrainings = objHomeViewModel.GetAllScheduledTrainings(mdlDashboard);
              
            }
            else if (Session["RoleType"] != null && Session["RoleType"].ToString().ToLower() == "trainer")
            {
                int iUserID = 0;
                int.TryParse(Session["UserId"].ToString(), out iUserID);
                objHomeViewModel.FillTrainerDashBoardData(ref mdlDashboard, iUserID);
                mdlDashboard.FilterMonth = DateTime.Now.Month;
                mdlDashboard.FilterYear = DateTime.Now.Year;
                mdlDashboard.MonthStartDate = new DateTime(mdlDashboard.FilterYear, mdlDashboard.FilterMonth, 1);
                mdlDashboard.DashboardName = "_TrainerDashboard";
                mdlDashboard.ScheduledTrainings = objHomeViewModel.GetTrainerScheduledTrainings(mdlDashboard, iUserID);
            }
            else if (Session["RoleType"] != null && (Session["RoleType"].ToString().ToLower() == "manager" || Session["RoleType"].ToString().ToLower() == "manager l1"))
            {
                int iroleID = 0;
                int iUserID = 0;
                int.TryParse(Session["UserId"].ToString(), out iUserID);
                int.TryParse(Session["RoleId"].ToString(), out iroleID);
                objHomeViewModel.FillManagerDashBoardData(ref mdlDashboard, iUserID,iroleID);
                mdlDashboard.FilterMonth = DateTime.Now.Month;
                mdlDashboard.FilterYear = DateTime.Now.Year;
                mdlDashboard.MonthStartDate = new DateTime(mdlDashboard.FilterYear, mdlDashboard.FilterMonth, 1);
                mdlDashboard.DashboardName = "_ManagerDashboard";
                mdlDashboard.ScheduledTrainings = objPublishTrainingViewModel.GetAllScheduledTrainings(mdlDashboard, iUserID, iroleID);
            }
            else if (Session["RoleType"] != null && (Session["RoleType"].ToString().ToLower() == "vendor" || Session["RoleType"].ToString().ToLower() == "candidate"))
            {
                int iUserID = 0;
                int.TryParse(Session["UserId"].ToString(), out iUserID);
                objHomeViewModel.FillVendorDashBoardData(ref mdlDashboard, iUserID);
                mdlDashboard.FilterMonth = DateTime.Now.Month;
                mdlDashboard.FilterYear = DateTime.Now.Year;
                mdlDashboard.MonthStartDate = new DateTime(mdlDashboard.FilterYear, mdlDashboard.FilterMonth, 1);
                mdlDashboard.DashboardName = "_VendorDashboard";
                VendorViewModel objVendorViewModel = new VendorViewModel();
                mdlDashboard.ScheduledTrainings = objVendorViewModel.GetVendorScheduledTrainings(mdlDashboard, iUserID);
            }
            if (mdlDashboard !=null) 
                mdlDashboard.News = objHomeViewModel.GetDashBoardNews(); 
            return View(mdlDashboard);
        }


        [HttpPost]
        [Filters.Authorized()]
        public ActionResult Home(DashboardModel mdlDashboard)
        {
            if (mdlDashboard.ActionType != null && (mdlDashboard.ActionType.ToLower().Equals("next") || mdlDashboard.ActionType.ToLower().Equals("previous")))
            {
                int month = 0;

                switch (mdlDashboard.ActionType.ToLower())
                {
                    case "next":
                        month = 1;
                        break;
                    case "previous":
                        month = -1;
                        break;
                   

                }

                mdlDashboard.MonthStartDate = mdlDashboard.MonthStartDate.AddMonths(month);
                mdlDashboard.FilterMonth = mdlDashboard.MonthStartDate.Month;
                mdlDashboard.FilterYear = mdlDashboard.MonthStartDate.Year;
            }
            HomeViewModel objHomeViewModel = new HomeViewModel();
            PublishedTrainingViewModel objPublishTrainingViewModel = new PublishedTrainingViewModel();

            if (mdlDashboard.ActionType == "showDetails")
            {
                //mdlDashboard.SelectedNews = mdlDashboard.News.Where(o => o.NewsID == mdlDashboard.).FirstOrDefault();
                //objHomeViewModel.getNewsDetailById(mdlDashboard.SelectedNews);
            }
            if (Session["RoleType"] != null && Session["RoleType"].ToString().ToLower() == "administrator")
            {
                mdlDashboard.ScheduledTrainings = objHomeViewModel.GetAllScheduledTrainings(mdlDashboard);
                return PartialView("_DashboardCalendar", mdlDashboard);
            }
            else if (Session["RoleType"] != null && Session["RoleType"].ToString().ToLower() == "trainer")
            {
                int iUserID = 0;
                int.TryParse(Session["UserId"].ToString(), out iUserID);
                mdlDashboard.ScheduledTrainings = objHomeViewModel.GetTrainerScheduledTrainings(mdlDashboard, iUserID);
                return PartialView("_DashboardCalendar", mdlDashboard);
            }
            else if (Session["RoleType"] != null && (Session["RoleType"].ToString().ToLower() == "manager" || Session["RoleType"].ToString().ToLower() == "manager l1"))
            {
                int iUserID = 0;
                int iroleID = 0;
                int.TryParse(Session["UserId"].ToString(), out iUserID);
                int.TryParse(Session["RoleId"].ToString(), out iroleID);
                mdlDashboard.ScheduledTrainings = objPublishTrainingViewModel.GetAllScheduledTrainings(mdlDashboard, iUserID, iroleID);
                return PartialView("_DashboardCalendar", mdlDashboard);
            }
            else if (Session["RoleType"] != null && (Session["RoleType"].ToString().ToLower() == "vendor" || Session["RoleType"].ToString().ToLower() == "candidate"))
            {
                int iUserID = 0;
                int.TryParse(Session["UserId"].ToString(), out iUserID);
                mdlDashboard.ScheduledTrainings = objHomeViewModel.GetVendorScheduledTrainings(mdlDashboard, iUserID);
                return PartialView("_DashboardCalendar", mdlDashboard);
            }
            return View(mdlDashboard);

        }

        [HttpGet]
        public ActionResult GetSelectedNews(int SelectedNewsID)
        {
            HomeViewModel objHomeViewModel = new HomeViewModel();
            NewsModel objNewsModel = new NewsModel();
            try
            {
                
                objNewsModel = objHomeViewModel.getNewsDetailById(SelectedNewsID);
                
            }
            catch (Exception ex)
            {
                //Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "Trainer", "GetSelectedTrainingDetail Get", ex);
            }
            return PartialView("_NewsDetails", objNewsModel);
        }


    }

    
}
