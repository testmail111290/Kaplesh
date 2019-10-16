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
using CaptchaMvc.HtmlHelpers;
using CaptchaMvc.Infrastructure;


namespace CTMS.Controllers
{
    public class UserController : Controller
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

        //Common Utils class for common operation.
        CommonUtils objCommonUtilError = new CommonUtils();


        //Object of User View Model where all CRUD operation perform. 
        UserViewModel objUserViewModel = new UserViewModel();

        #endregion

        #region [Registration]

        // GET: /User/
        /// <summary>
        /// Load Registration Page
        /// </summary>
        public ActionResult Registration()
        {
            ExternalUserModel objExternalUserModel = new ExternalUserModel();
            try
            {
                GetDropDownList();
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "Registration GET", ex);
                return View(objExternalUserModel);
            }
            return View(objExternalUserModel);
        }

        /// <summary>
        /// Get Registration Details
        /// </summary>
        /// <param name="objExternalUserModel"></param>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Registration(ExternalUserModel objExternalUserModel)
        {
            try
            {
                GetDropDownList();
                if (ModelState.IsValid)
                {
                    if (this.IsCaptchaVerify(Resources.UserResource.msgInvalidCaptcha))
                    {

                        objExternalUserModel.CreatedBy = createdBy;
                        objExternalUserModel.IsActive = false;
                        objExternalUserModel.CreatedOn = DateTime.Now;
                        int UserId = objUserViewModel.SaveUserDetails(objExternalUserModel);
                        if (UserId > 0)
                        {
                            objExternalUserModel = new ExternalUserModel();
                            Session["SuccessMessage"] = Resources.UserResource.msgRegistrationSuccess;
                            return RedirectToAction("Login", "User");
                        }
                        else
                        {

                            objExternalUserModel.Message = Resources.UserResource.msgRegistrationError;
                            objExternalUserModel.MessageType = MessageType.Error.ToString().ToLower();
                        }
                    }

                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "Registration POST", ex);

            }
            return View(objExternalUserModel);
        }

        /// <summary>
        /// Get Function list as SubCategory list by Category ID
        /// </summary>
        /// <param name="CategoryID"></param>
        public ActionResult GetFunctionsByCategoryID(int CategoryID)
        {
            try
            {
                List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
                lstFunctionCategoryModel = objUserViewModel.GetFunctionsByCategoryIds(Convert.ToString(CategoryID));
                SelectList objFunctions = new SelectList(lstFunctionCategoryModel, "FunctionID", "FunctionName", 0);
                return Json(objFunctions);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "GetFunctionsByCategoryID", ex);
                return Json(ex);
            }

        }

        /// <summary>
        /// Get Circle list by Category ID
        /// </summary>
        /// <param name="CategoryID"></param>
        public ActionResult GetCircleByCategoryID(int CategoryID)
        {

            try
            {
                List<CircleModel> lstCircleModel = new List<CircleModel>();
                lstCircleModel = objUserViewModel.GetCircleByCategoryId(CategoryID);
                SelectList objCircles = new SelectList(lstCircleModel, "CircleID", "CircleName", 0);
                return Json(objCircles);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "GetCircleByCategoryID", ex);
                return Json(ex);
            }

        }

        /// <summary>
        /// Get City list by Circle ID  and Category ID
        /// </summary>
        /// <param name="CategoryID" and "CircleID"></param>
        public ActionResult GetCityByCircleID(int CategoryID, int CircleID)
        {

            try
            {
                List<CityModel> lstCityModel = new List<CityModel>();
                lstCityModel = objUserViewModel.GetCityByCategoryId(CategoryID, CircleID);
                SelectList objCity = new SelectList(lstCityModel, "CityID", "CityName", 0);
                return Json(objCity);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "GetCityByCircleID", ex);
                return Json(ex);
            }

        }

        /// <summary>
        /// Get Location list by Category ID,Circle ID and City ID
        /// </summary>
        /// <param name="CategoryID","CircleID","CityID"></param>
        public ActionResult GetLocationByCityID(int CategoryID, int CircleID, int CityID)
        {

            try
            {
                List<LocationModel> lstLocationModel = new List<LocationModel>();
                lstLocationModel = objUserViewModel.GetLocationByCategoryId(CategoryID, CircleID, CityID);
                SelectList objLocation = new SelectList(lstLocationModel, "LocationID", "LocationName", 0);
                return Json(objLocation);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "GetLocationByCityID", ex);
                return Json(ex);
            }

        }

        /// <summary>
        /// Get ManagerId by ManagerName and Circle ID
        /// </summary>
        /// <param name="circleId" and "ManagerName"></param>
        public ActionResult GetManagerIdByManagerName(int circleId, string ManagerName)
        {

            try
            {
                int ManagerId;
                ManagerId = objUserViewModel.GetManagerId(circleId, ManagerName);
                return Json(ManagerId);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "GetManagerIdByManagerName", ex);
                return Json(ex);
            }

        }

        /// <summary>
        /// Check Duplication of Employee Code, Email, UniqueIDNumber and UserDomainName
        /// </summary>
        /// <param name="FieldName" and "FieldValue"></param>
        public ActionResult CheckDuplicateFields(string FieldName, string FieldValue)
        {
            try
            {
                bool IsValid;
                IsValid = objUserViewModel.checkDuplicateValues(FieldName, FieldValue);
                return Json(IsValid);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "GetManagerIdByManagerName", ex);
                return Json(ex);
            }


        }

        /// <summary>
        /// Initialises the Dropdownlists of Registration form 
        /// </summary>
        public void GetDropDownList()
        {   //Fill Business Categories List
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            List<CategoryModel> lstCategory = new List<CategoryModel>();
            lstCategory = objTrainingViewModel.GetAllActiveCategory(false).ToList();
            ViewBag.CategoryList = new SelectList(lstCategory, "CategoryID", "CategoryName", 0);

            //Fill Business Sub Category List
            List<FunctionCategoryModel> lstFunctionCategoryModel = new List<FunctionCategoryModel>();
            FunctionCategoryModel objFunction = new FunctionCategoryModel();
            ViewBag.FunctionList = new SelectList(lstFunctionCategoryModel, "FunctionID", "FunctionName", 0);

            //Fill Circle List
            List<CircleModel> lstCircleModel = new List<CircleModel>();
            CircleModel objCircleModel = new CircleModel();
            ViewBag.CircleList = new SelectList(lstCircleModel, "CircleID", "CircleName", 0);

            //Fill City List
            List<CityModel> lstCities = new List<CityModel>();
            CityModel objCityModel = new CityModel();
            ViewBag.CityList = new SelectList(lstCities, "CityID", "CityName", 0);

            //Fill Location List
            List<LocationModel> lstLocation = new List<LocationModel>();
            LocationModel objLocationModel = new LocationModel();
            ViewBag.LocationList = new SelectList(lstLocation, "LocationId", "LocationName", 0);

            //Fill Role List
            MasterViewModel objMasterViewModel = new MasterViewModel();
            List<RoleModel> lstRoles = objMasterViewModel.GetAllRole(null, null, string.Empty).ToList();
            int candidateRoleID = lstRoles.Where(r => r.RoleType.ToLower() == "candidate").Select(r => r.RoleID).SingleOrDefault();
            ViewBag.RoleList = new SelectList(lstRoles, "RoleID", "RoleName", candidateRoleID);

            //Fill UniqueIDType
            CommonViewModel objCommonViewModel = new CommonViewModel();
            List<ResourceModel> lstResources;
            lstResources = objCommonViewModel.GetResourceByResourceName("UniqueIDType").ToList<ResourceModel>();
            ViewBag.UniqueIDType = new SelectList(lstResources, "ResourceValue", "ResourceValue", null);
        }

        #endregion

        #region [Login]
        /// <summary>
        /// Load Login Page
        /// </summary>
        public ActionResult Login()
        {
            ExternalUserModel objExternalUserModel = new ExternalUserModel();
            try
            {
                if (Session["SuccessMessage"] != null && Convert.ToString(Session["SuccessMessage"]) != "")
                {
                    objExternalUserModel.Message = Session["SuccessMessage"].ToString();
                    objExternalUserModel.MessageType = MessageType.Success.ToString().ToLower();
                    Session["SuccessMessage"] = null;
                }
                if (Session["ErrorMessage"] != null && Convert.ToString(Session["ErrorMessage"]) != "")
                {
                    objExternalUserModel.Message = Session["ErrorMessage"].ToString();
                    objExternalUserModel.MessageType = MessageType.Error.ToString().ToLower();
                    Session["ErrorMessage"] = null;
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "Login GET", ex);

            }
            return View(objExternalUserModel);
        }

        /// <summary>
        /// Check User Credentials 
        /// </summary>
        /// <param name="objExternalUserModel"></param>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Login(ExternalUserModel objExternalUserModel)
        {
            try
            {
                UserViewModel ObjUserViewModel = new UserViewModel();
                objExternalUserModel.LoginPassword = CommonUtils.Encrypt(objExternalUserModel.LoginPassword);
                objExternalUserModel = ObjUserViewModel.CheckUsercredentials(objExternalUserModel);
                if (objExternalUserModel.ErrorCode == 52)
                {

                    objExternalUserModel.Message = Resources.UserResource.msgDoNotMatchcredential;
                    objExternalUserModel.MessageType = MessageType.Error.ToString().ToLower();
                    return View(objExternalUserModel);
                }
                objExternalUserModel.LoginPassword = CommonUtils.Decrypt(objExternalUserModel.LoginPassword);

                if (objExternalUserModel.ErrorCode == 53)
                {
                    objExternalUserModel.Message = Resources.UserResource.msgAccountLocked;
                    objExternalUserModel.MessageType = MessageType.Error.ToString().ToLower();
                    return View(objExternalUserModel);
                }


                if (objExternalUserModel.ErrorCode == 55)
                {
                    objExternalUserModel.Message = Resources.UserResource.msgExpiryPassword;
                    objExternalUserModel.MessageType = MessageType.Error.ToString().ToLower();
                    Session["ExternalUserDomain"] = objExternalUserModel.UserDomainName;
                    Session["PasswordModifiedDate"] = objExternalUserModel.PasswordModifiedDate;
                    Session["IsPasswordExpired"] = true;

                    return RedirectToAction("ChangePassword", "User");
                }

                Session["PasswordModifiedDate"] = objExternalUserModel.PasswordModifiedDate;
                Session["ExternalUserDomain"] = objExternalUserModel.UserDomainName;
                FormsAuthenticationUtil.SetAuthCookie(objExternalUserModel.UserFirstName.ToString(), objExternalUserModel.RoleID + "#" + objExternalUserModel.UserFirstName + "#" + objExternalUserModel.EmailId + "#" + objExternalUserModel.UserId.ToString(), false);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog("0", "Login", "Login Post", ex);
            }

            return RedirectToAction("index", "Home");
        }

        #endregion

        #region Manager

        /// <summary>
        /// View Pending Requests of External user
        /// </summary>
        /// <param name="UD" ></param>
        [Filters.MenuAccess()]
        public ActionResult ViewPendingExtUser(int UD = 0)
        {
            ViewExternalUserModel objViewExternalUserModel = new ViewExternalUserModel();
            try
            {
                UserViewModel objUserViewModel = new UserViewModel();
                BindStatusDDL();

                objViewExternalUserModel.CurrentPage = 1;
                objViewExternalUserModel.PageSize = CommonUtils.PageSize;
                objViewExternalUserModel.TotalPages = 0;
                objViewExternalUserModel.ManagerID = createdBy;
                objViewExternalUserModel.UserId = UD;
                objUserViewModel.GetExternalUserDetailForManager(objViewExternalUserModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "ViewPendingExtUser Get", ex);

            }
            return View(objViewExternalUserModel);
        }


        /// <summary>
        /// Apporval or Rejection of External user
        /// </summary>
        ///  <param name="objViewExternalUserModel" ></param>
        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewPendingExtUser(ViewExternalUserModel objViewExternalUserModel)
        {
            UserViewModel objUserViewModel = new UserViewModel();
            try
            {
                objViewExternalUserModel.ManagerID = createdBy;
                BindStatusDDL();

                if (objViewExternalUserModel.ActionType == "saveapprove")
                {
                    objUserViewModel.SaveUserApprovalDetail(objViewExternalUserModel);

                }
                objUserViewModel.GetExternalUserDetailForManager(objViewExternalUserModel);
                if (objViewExternalUserModel.ActionType == "pagechange")
                {

                    foreach (var user in objViewExternalUserModel.ListExternalUser)
                    {
                        if (!String.IsNullOrEmpty(objViewExternalUserModel.ApprovedList))
                        {
                            if (objViewExternalUserModel.ApprovedList.Split(',').ToList().Contains(user.UserId.ToString()))
                            {
                                user.IsApproved = true;
                            }
                        }
                        if (!String.IsNullOrEmpty(objViewExternalUserModel.RejectedList))
                        {
                            if (objViewExternalUserModel.RejectedList.Split(',').ToList().Contains(user.UserId.ToString()))
                            {
                                user.IsApproved = false;
                            }
                        }
                    }
                }

                objViewExternalUserModel.SelectedxternalUser = null;
                if (objViewExternalUserModel.ActionType == "showDetails")
                {
                    objViewExternalUserModel.SelectedxternalUser = objViewExternalUserModel.ListExternalUser.Where(o => o.UserId == objViewExternalUserModel.SelectedExtUserId).FirstOrDefault();
                }

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "ViewPendingExtUser Post", ex);

            }
            return PartialView("_PendingExtUserList", objViewExternalUserModel);
        }


        /// <summary>
        /// Get booking status
        /// </summary>
        private void BindStatusDDL()
        {
            CommonViewModel objCommonViewModel = new CommonViewModel();
            List<ResourceModel> lstResources1 = new List<ResourceModel>();
            lstResources1.Add(new ResourceModel { ResourceID = 0, ResourceValue = "Select" });


            var lstResources = objCommonViewModel.GetResourceByResourceName("ApprovalStatus").ToList<ResourceModel>();
            lstResources1.AddRange(lstResources);
            ViewBag.BookingStatus = lstResources1;//new SelectList(lstResources, "ResourceID", "ResourceName", null);
        }

        #endregion


        #region ChangePassword

        /// <summary>
        /// Change password of user.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult ChangePassword()
        {
            ChangePasswordModel objChangePasswordModel = new ChangePasswordModel();
            try
            {
                objChangePasswordModel.IsReset = false;
                if (Session["ExternalUserDomain"] != null && Convert.ToString(Session["ExternalUserDomain"]) != "")
                {
                    objChangePasswordModel.UserDomainName = Session["ExternalUserDomain"].ToString();
                }
                //else
                //{
                //    return RedirectToAction("Login", "User");
                //}
                if (Session["IsPasswordExpired"] != null && Convert.ToString(Session["IsPasswordExpired"]) != "")
                {
                    objChangePasswordModel.IsPasswordExpired = Convert.ToBoolean(Session["IsPasswordExpired"].ToString());
                    objChangePasswordModel.Message = Resources.UserResource.msgPasswordExpireToChange;
                    objChangePasswordModel.MessageType = MessageType.Notice.ToString().ToLower();
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "ChangePassword Get", ex);
            }
            return View(objChangePasswordModel);
        }
        /// <summary>
        /// Change password of user.
        /// </summary>
        /// <param name="objChangePasswordModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ChangePassword(ChangePasswordModel objChangePasswordModel)
        {
            UserViewModel objUserViewModel = new UserViewModel();
            ExternalUserModel objExternalUserModel = new ExternalUserModel();
            string redirectUrl = string.Empty;
            try
            {

                string encOldPassword = CommonUtils.Encrypt(objChangePasswordModel.OldPassword);

                objExternalUserModel.UserDomainName = objChangePasswordModel.UserDomainName;
                objExternalUserModel = objUserViewModel.GetUserDetailByUserDomainName(objExternalUserModel);
                if (objExternalUserModel != null && objExternalUserModel.UserId > 0 && objExternalUserModel.IsActive)
                {
                    //In case of change password check old password and match with past 3(configurable) passwords.
                    if (!objChangePasswordModel.IsReset)
                    {
                        if (objChangePasswordModel != null && !String.IsNullOrEmpty(objChangePasswordModel.OldPassword) && objExternalUserModel.Password != encOldPassword)
                        {
                            objChangePasswordModel.Message = Resources.UserResource.msgOldPasswordNotValid;
                            objChangePasswordModel.MessageType = MessageType.Error.ToString().ToLower();
                            return Json(new { message = objChangePasswordModel.Message, messageType = objChangePasswordModel.MessageType });

                        }
                    }
                    else
                    {
                        if (objExternalUserModel.IsLock != null && (bool)objExternalUserModel.IsLock)
                        {
                            objExternalUserModel.Message = Resources.UserResource.msgAccountLocked;
                            objExternalUserModel.MessageType = MessageType.Error.ToString().ToLower();
                            redirectUrl = Url.Action("Login", "User");
                            return Json(new { url = redirectUrl, IsReset = objChangePasswordModel.IsReset, message = objExternalUserModel.Message, messageType = objExternalUserModel.MessageType });
                        }
                    }

                    string encNewPassword = CommonUtils.Encrypt(objChangePasswordModel.NewPassword);
                    if (encOldPassword == encNewPassword)
                    {
                        objChangePasswordModel.Message = Resources.UserResource.msgNewPasswordInvalid;
                        objChangePasswordModel.MessageType = MessageType.Error.ToString().ToLower();
                    }
                    else
                    {
                        bool isPasswordMatchWithOld = objUserViewModel.IsPasswordMatchWithOldPassword(objExternalUserModel.UserDomainName, encNewPassword, CommonUtils.CheckLastPasswordCount);
                        if (isPasswordMatchWithOld)
                        {
                            objChangePasswordModel.Message = String.Format(Resources.UserResource.msgMatchWithOldPasswords, CommonUtils.CheckLastPasswordCount);
                            objChangePasswordModel.MessageType = MessageType.Error.ToString().ToLower();
                        }
                        else
                        {
                            objUserViewModel.UpdatePassword(objChangePasswordModel);
                            Session["PasswordModifiedDate"] = DateTime.Now;
                            if (objChangePasswordModel.MessageType == MessageType.Success.ToString().ToLower() && !objChangePasswordModel.IsReset)
                            {
                                Session["IsPasswordExpired"] = null;
                                Session["PasswordChangeSuccess"] = objChangePasswordModel.Message;
                                redirectUrl = Url.Action("Home", "Home");
                                return Json(new { url = redirectUrl, IsReset = objChangePasswordModel.IsReset, message = objChangePasswordModel.Message, messageType = objChangePasswordModel.MessageType });
                            }
                            else if (objChangePasswordModel.MessageType == MessageType.Success.ToString().ToLower() && objChangePasswordModel.IsReset)
                            {
                                Session["IsPasswordExpired"] = null;
                                Session["SuccessMessage"] = Resources.UserResource.msgPasswordResetSuccess;
                                objExternalUserModel.MessageType = MessageType.Success.ToString().ToLower();
                                redirectUrl = Url.Action("Login", "User");
                                return Json(new { url = redirectUrl, IsReset = objChangePasswordModel.IsReset, message = objChangePasswordModel.Message, messageType = objChangePasswordModel.MessageType });
                            }
                        }
                    }
                }


                else
                {
                    Session["ErrorMessage"] = Resources.UserResource.msgAccountInActive;
                    objExternalUserModel.MessageType = MessageType.Error.ToString().ToLower();
                    redirectUrl = Url.Action("Login", "User");
                    return Json(new { url = redirectUrl, IsReset = objChangePasswordModel.IsReset, message = Resources.UserResource.msgAccountInActive, messageType = objExternalUserModel.MessageType });
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "ChangePassword Post", ex);
            }
            return Json(new { message = objChangePasswordModel.Message, messageType = objChangePasswordModel.MessageType });
        }


        #endregion

        #region Forgot AND Reset Password

        /// <summary>
        /// View Reset password of user.
        /// </summary>
        /// <param name="pd" and "ud"></param> 
        /// <returns></returns>
        public ActionResult Reset(string ud, string pd)
        {
            ChangePasswordModel objChangePasswordModel = new ChangePasswordModel();
            UserViewModel objUserViewModel = new UserViewModel();
            ExternalUserModel objExternalUserModel = new ExternalUserModel();

            try
            {
                objExternalUserModel.UserDomainName = ud;
                objChangePasswordModel.UserDomainName = ud;
                objChangePasswordModel.IsReset = true;

                objExternalUserModel = objUserViewModel.GetUserDetailByUserDomainName(objExternalUserModel);
                if (objExternalUserModel != null && objExternalUserModel.UserId > 0 && objExternalUserModel.IsActive)
                {
                    if (String.Compare(objExternalUserModel.Password, pd) == 0)
                    {
                        return View("ChangePassword", objChangePasswordModel);
                    }
                    else
                    {
                        Session["ErrorMessage"] = Resources.UserResource.msgForgotLinkExpired;
                    }
                }
                else
                {
                    Session["ErrorMessage"] = Resources.UserResource.msgAccountInActive;
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "Reset Get", ex);
            }
            return RedirectToAction("Login", "User");
        }

        /// <summary>
        /// View Forget password.
        /// </summary>
        /// <param name="pd" and "ud"></param> 
        public ActionResult ForgotPassword()
        {

            ChangePasswordModel objChangePasswordModel = new ChangePasswordModel();
            return View(objChangePasswordModel);
        }

        /// <summary>
        /// Performs Forget password operation
        /// </summary>
        /// <param name="objChangePasswordModel"></param>
        [HttpPost]
        public ActionResult ForgotPassword(ChangePasswordModel objChangePasswordModel)
        {
            UserViewModel ObjUserViewModel = new UserViewModel();
            try
            {
                objChangePasswordModel = ObjUserViewModel.GetUserDetailByEmailID(objChangePasswordModel);
                if (objChangePasswordModel == null)
                {
                    ChangePasswordModel objChangePasswordModel1 = new ChangePasswordModel();
                    objChangePasswordModel1.Message = Resources.UserResource.msgDoNotMatchEmail;
                    objChangePasswordModel1.MessageType = MessageType.Notice.ToString().ToLower();
                    objChangePasswordModel = objChangePasswordModel1;

                }
                else
                {
                    ObjUserViewModel.GenerateForgotPasswordTemplate(objChangePasswordModel);
                    Session["SuccessMessage"] = Resources.UserResource.msgSucessEmail;
                    return RedirectToAction("Login", "User");

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "User", "ForgotPassword Post", ex);
            }

            return View("ForgotPassword", objChangePasswordModel);
        }
        #endregion

    }
}
