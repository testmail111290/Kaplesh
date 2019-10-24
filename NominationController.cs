using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.Practices.ServiceLocation;
using Polaris.HIP.Common.Enums;
using Polaris.HIP.Core.Data.Repository.Enum;
using Polaris.HIP.Core.Data.Repository.Impl;
using Polaris.HIP.Core.Logger.Enum;
using Polaris.HIP.Domain.MasterItems;
using Polaris.HIP.Infrastructure.Controller.Interface;
using Polaris.HIP.Infrastructure.DomainFactory.Interface;
using Polaris.HIP.Infrastructure.RepositoryService.Interface;
using Polaris.HIP.Infrastructure.RepositoryService.Interface.RequestSections;
using Polaris.HIP.UserInterface.Infrastructure;
using Polaris.HIP.ViewModel.Common;
using Polaris.HIP.ViewModel.RequestSection.Nomination;
using Polaris.HIP.Workflow.Interface;
using Polaris.HIP.Core.State.Interface;
using System.Collections.Generic;
using Polaris.HIP.Core.Extensions;
using Polaris.HIP.Cache.Interface;
using Polaris.HIP.Infrastructure.RepositoryService.Interface.RequestService;
using Polaris.HIP.ViewModel.Screening;
using Polaris.HIP.Domain.Requests.RequestSections;

namespace Polaris.HIP.UserInterface.Controllers.RequestSections.Nomination
{
    public class NominationController : RequestSectionBaseController<NominationViewModel>
    {
        private IRequestApproverService _requestApproverService;
        [UnitOfWork]
        public async Task<ActionResult> CreateAsync(long? id)
        {
            try
            {
                SessionWrapper.CurrentTabCategory = TabCategory.SubmitterTab;
                SessionWrapper.SectionCode = SectionCode.Nomination;
                NominationViewModel nominationViewModel;
                ApplicationMaster applicationMaster;
                ICacheWrapper cacheWrapper = ServiceLocator.Current.GetInstance<ICacheWrapper>();
                using (new UnitOfWorkScope(TransactionMode.New))
                {
                    applicationMaster = cacheWrapper.GetApplicationMaster();
                }
                var IsGlobalDBExists = applicationMaster.IsGlobalDBExists;
                var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();
                if (!id.HasValue)
                {
                    nominationViewModel = await CreateRequestSectionViewModelAsync();
                }
                else
                {
                    nominationViewModel = await businessNeedsNominationService.GetNominationSectionBySectionIdAsync(SessionWrapper.RequestUniqueId, id.Value);
                }
                if (nominationViewModel != null)
                    if (nominationViewModel.HCPVendorDetails == null)
                        nominationViewModel.HCPVendorDetails = new HCPVendorSearchAndAddViewModel();                 
                
                if (nominationViewModel.CoveredRecipients != null && !nominationViewModel.CoveredRecipients.Any(x => x.IsAttendee == false))
                    nominationViewModel.SelectedCoveredRecipient = null;
                var coveredRecipient = new CoveredRecipientViewModel
                {
                    HCPCountry = CacheWrapper.GetConsultantCountry(ApplicationCode.HIP, SessionWrapper.CurrentUserLanguage).ToList(),
                    HCPType = CacheWrapper.GetMasterListItems(MasterListCode.ServiceProviderType, ApplicationCode.HIP, SessionWrapper.CurrentUserLanguage).ToList(),
                    HcpTypeTextToolTip = CacheWrapper.GetToolTip(ToolTipCode.NOMINATION_HCPTYPE_TEXT, SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP),
                    AddHCPErrorLabelText = CacheWrapper.GetResourceText("Resource-Error-Nomination-HcpNotFound", SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP),
                    HCPCityToolTip = CacheWrapper.GetToolTip(ToolTipCode.NOMINATION_HCPCITY_TEXT, SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP),
                    HCPFirstNameToolTip = CacheWrapper.GetToolTip(ToolTipCode.NOMINATION_HCPFIRSTNAME_TEXT, SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP),
                    HcpLastNameTextToolTip = CacheWrapper.GetToolTip(ToolTipCode.NOMINATION_HCPLASTNAME_TEXT, SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP),
                    HCPCountryCodeTextToolTip = CacheWrapper.GetToolTip(ToolTipCode.NOMINATION_NEW_HCPCOUNTRY_TEXT, SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP),
                    DegreeTextToopTip = CacheWrapper.GetToolTip(ToolTipCode.NOMINATION_HCPDEGREES_TEXT, SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP),
                    HcpStateProvinceTextToolTip = CacheWrapper.GetToolTip(ToolTipCode.NOMINATION_STATEPROVINCE_TEXT, SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP),
                    DateOfBirthToopTip = CacheWrapper.GetToolTip(ToolTipCode.NOMINATION_HCPDOB_TEXT, SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP),
                    ServiceProviderDegree = CacheWrapper.GetListItems<DegreeMaster>(ApplicationCode.HIP, SessionWrapper.CurrentUserLanguage).OrderBy(m => m.Code).ToList(),
                    States = CacheWrapper.GetListItems<StateMaster>(ApplicationCode.HIP, SessionWrapper.CurrentUserLanguage).OrderBy(m => m.Code).ToList(),
                    SPSearchFirstName = CacheWrapper.GetToolTip(ToolTipCode.SPSearch_First_Name, SessionWrapper.CurrentUserLanguage, SessionWrapper.CurrentApplicationCode),
                    SPSearchFirstNameValid = CacheWrapper.GetToolTip(ToolTipCode.SPSearch_First_Name_Valid, SessionWrapper.CurrentUserLanguage, SessionWrapper.CurrentApplicationCode),
                    SPSearchValidCity = CacheWrapper.GetToolTip(ToolTipCode.SPSearch_Valid_City, SessionWrapper.CurrentUserLanguage, SessionWrapper.CurrentApplicationCode),
                    SPSearchLastName = CacheWrapper.GetToolTip(ToolTipCode.SPSearch_Last_Name, SessionWrapper.CurrentUserLanguage, SessionWrapper.CurrentApplicationCode),
                    SPSearchValidLastName = CacheWrapper.GetToolTip(ToolTipCode.SPSearch_Last_Name_Valid, SessionWrapper.CurrentUserLanguage, SessionWrapper.CurrentApplicationCode),
                    SPSearchOrganization = CacheWrapper.GetToolTip(ToolTipCode.SPSearch_Organization, SessionWrapper.CurrentUserLanguage, SessionWrapper.CurrentApplicationCode),
                    SPSearchValidOrganization = CacheWrapper.GetToolTip(ToolTipCode.SPSearch_Organization_Valid, SessionWrapper.CurrentUserLanguage, SessionWrapper.CurrentApplicationCode),
                    IsSearchFromInbox = false,
                    HCPFirstName = string.Empty,
                    HCPLastName = string.Empty
                };
                nominationViewModel.CoveredSearch = new CoveredRecipientSearchViewModel
                {
                    CoveredRecipientViewModel = coveredRecipient,
                    HcpSearchInstructionText = CacheWrapper.GetInstructionText(InstructionTextCode.HcpSearchInstruction, SessionWrapper.CurrentUserLanguage, ApplicationCode.HIP)
                };
                nominationViewModel.IsCoownerOfActivity = SessionWrapper.IsCoownerOfActivity;

                nominationViewModel.ActivityCategory = SessionWrapper.ActivityCategory.ToString();
                nominationViewModel.IsGlobalDBExists = IsGlobalDBExists;

                _requestApproverService = ServiceLocator.Current.GetInstance<IRequestApproverService>();
                var requestUsers = _requestApproverService.GetUserForSequenceBypass(SessionWrapper.RequestID);
                nominationViewModel.IsSubmitterOrOriginatorOrDelegate = (requestUsers.OriginatorId == SessionWrapper.CurrentUserId && SessionWrapper.IsOriginator) || (requestUsers.SubmitterId == SessionWrapper.CurrentUserId && SessionWrapper.IsSubmitter) || (requestUsers.SubmitterDelegateId == SessionWrapper.CurrentUserId && SessionWrapper.IsDelegatorOfActivity) || (requestUsers.OrginatorDelegateId == SessionWrapper.CurrentUserId && SessionWrapper.IsDelegatorOfActivity) ? true : false;

                if (Session["AttendeeInformationViewModels"] != null)
                    Session["AttendeeInformationViewModels"] = null;

                if (nominationViewModel.SelectedCoveredRecipient != null)
                {
                    nominationViewModel.IsReadOnly = 
                        (nominationViewModel.SelectedCoveredRecipient.StatusCode == StatusCode.Dummy ||
                       nominationViewModel.SelectedCoveredRecipient.StatusCode == StatusCode.HCPCoordinatorRAI)
                       ? false : true;
                }

                return PartialView(nominationViewModel);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ExceptionType.Application, "NominationController", "Create", e);
            }
            return null;
        }

        [UnitOfWork]
        public async Task<ActionResult> RefreshNominationGridAsync()
        {
            try
            {

                SessionWrapper.CurrentTabCategory = TabCategory.SubmitterTab;
                NominationViewModel nominationViewModel;
                nominationViewModel = await CreateRequestSectionViewModelAsync();

                return PartialView("~/Views/Shared/_NominationGrid.cshtml", nominationViewModel);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ExceptionType.Application, "NominationController", "RefreshNominationGrid", e);
            }
            return null;
        }

        [UnitOfWork]
        public async Task<ActionResult> NominateDataSelectionAsync(long? id)
        {
            try
            {

                SessionWrapper.CurrentTabCategory = TabCategory.SubmitterTab;
                var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();
                var nominationViewModel = await businessNeedsNominationService.GetNominationSectionBySectionIdAsync(SessionWrapper.RequestUniqueId, id.Value);
                if (nominationViewModel != null && nominationViewModel.HCPVendorDetails != null)
                    nominationViewModel.HCPVendorDetails.VendorSearchViewModel = businessNeedsNominationService.PrepareVendorSearchViewModel();
                _requestApproverService = ServiceLocator.Current.GetInstance<IRequestApproverService>();
                var requestUsers = _requestApproverService.GetUserForSequenceBypass(SessionWrapper.RequestID);
                nominationViewModel.IsSubmitterOrOriginatorOrDelegate = (requestUsers.OriginatorId == SessionWrapper.CurrentUserId && SessionWrapper.IsOriginator) || (requestUsers.SubmitterId == SessionWrapper.CurrentUserId && SessionWrapper.IsSubmitter) || (requestUsers.SubmitterDelegateId == SessionWrapper.CurrentUserId && SessionWrapper.IsDelegatorOfActivity) || (requestUsers.OrginatorDelegateId == SessionWrapper.CurrentUserId && SessionWrapper.IsDelegatorOfActivity) ? true : false;
                return PartialView("~/Views/Nomination/_NominateDataSelection.cshtml", nominationViewModel);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ExceptionType.Application, "NominationController", "NominateDataSelection", e);
            }
            return null;
        }

        [UnitOfWork]
        public ActionResult LoadNomination(SectionCode currentApproverSection)
        {
            try
            {

                TempData["isFromAddServiceProvider"] = true;
                TempData["TabName"] = "sp_Nomination";
                SessionWrapper.CurrentTabCategory = TabCategory.SubmitterTab;
                SessionWrapper.IsSummary = true;
                return RedirectToAction("Create", "ActivityCommom");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ExceptionType.Application, "NominationController", "Create", e);
            }
            return null;
        }


        [HttpPost]
        [UnitOfWork]
        public async Task<ActionResult> CreateAsync(NominationViewModel nominationViewModel)
        {
            try
            {
                var coveredRecipientSearchService = ServiceLocator.Current.GetInstance<ICoveredRecipientSearchService>();
                NominationSection previousNominationSection = null;

                if (nominationViewModel.SelectedCoveredRecipient  != null &&
                    nominationViewModel.SelectedCoveredRecipient.SectionStatusCode == StatusCode.HCPAmendmentInProgress.ToString() ||
                    nominationViewModel.SelectedCoveredRecipient.SectionStatusCode == StatusCode.HCPCoordinatorRAI.ToString() ||
                    nominationViewModel.SelectedCoveredRecipient.SectionStatusCode == StatusCode.HCPSeniorManagementRAI.ToString())
                {   
                    previousNominationSection = coveredRecipientSearchService.GetNominationSectionById(nominationViewModel.Id);
                }
                if (SessionWrapper.WorkflowActivityCategory == WorkflowActivityCategoryCode.WorkflowCode_MasterServiceAgreement)
                {
                    var checkMSAa = ValidateMSACheck(nominationViewModel.SelectedCoveredRecipient.CoveredRecipientId);
                    if (!checkMSAa)
                    {
                        return Json("MSAErrorCheckFailed", JsonRequestBehavior.AllowGet);

                    }
                }
                using (new UnitOfWorkScope(TransactionMode.New))
                {
                    CreateRequestSection(nominationViewModel, SessionWrapper.CurrentRequestStatus,
                                         SessionWrapper.ActivityCategory,
                                         WorkflowAction.BusinessNeedAssementNominationSave,
                                         WorkflowAction.BusinessNeedAssementNominationSave, nominationViewModel.Id, SectionCode.Nomination);


                }
                using (new UnitOfWorkScope(TransactionMode.New))
                {
                    var serviceProviderRepository = ServiceLocator.Current.GetInstance<IServiceProviderRepositoryService>();
                    var serviceProviderResumeController = ServiceLocator.Current.GetInstance<IServiceProviderResumeController>();
                    var serviceProviderResumes = serviceProviderRepository.GetResumes(SessionWrapper.RequestUniqueId,
                                                                                    nominationViewModel.
                                                                                        SelectedCoveredRecipient.
                                                                                        BiUniqueIdentifier).ToList();
                    serviceProviderResumeController.Save(nominationViewModel.SelectedCoveredRecipient.DeleteDocumentId,
                        nominationViewModel.SelectedCoveredRecipient.BiUniqueIdentifier, serviceProviderResumes);
                }
                using (new UnitOfWorkScope(TransactionMode.New))
                {
                    var serviceProviderRepository = ServiceLocator.Current.GetInstance<IServiceProviderRepositoryService>();
                    var serviceProviderCertificationFac = ServiceLocator.Current.GetInstance<IServiceProviderCertification>();
                    var serviceProviderCertifications = serviceProviderRepository.GetCertification(SessionWrapper.RequestUniqueId, nominationViewModel.SelectedCoveredRecipient.BiUniqueIdentifier).ToList();
                    serviceProviderCertificationFac.Save(nominationViewModel.SelectedCoveredRecipient.DeleteCertificationDocumentId, nominationViewModel.SelectedCoveredRecipient.BiUniqueIdentifier, serviceProviderCertifications);
                }
                using (new UnitOfWorkScope(TransactionMode.New))
                {
                    var serviceProviderRepository = ServiceLocator.Current.GetInstance<IServiceProviderRepositoryService>();
                    var serviceProviderAudioVideoFac = ServiceLocator.Current.GetInstance<IServiceProviderAudioVideo>();
                    var serviceProviderGetAudioVideo = serviceProviderRepository.GetAudioVideo(SessionWrapper.RequestUniqueId, nominationViewModel.SelectedCoveredRecipient.BiUniqueIdentifier).ToList();
                    serviceProviderAudioVideoFac.Save(serviceProviderGetAudioVideo);
                }
                if (previousNominationSection != null)
                {
                    using (new UnitOfWorkScope(TransactionMode.New))
                    {
                        NominationSection currentNominationSection = coveredRecipientSearchService.GetNominationSectionById(nominationViewModel.Id);

                        if (currentNominationSection != null)
                        {
                            LogType logType = LogType.Audit;

                            var prev = previousNominationSection.ToJson().ToObject<NominationSection>();
                            var current = currentNominationSection.ToJson().ToObject<NominationSection>();
                            prev.HCPEmail = previousNominationSection.HCPEmail;
                            current.HCPEmail = currentNominationSection.HCPEmail;

                            if (previousNominationSection.ServiceProviderCV != null)
                            {
                                prev.ServiceProviderCV = new List<Domain.Documents.Document>();
                                foreach (var item in previousNominationSection.ServiceProviderCV)
                                {
                                    prev.ServiceProviderCV.Add((new Domain.Documents.Document()
                                    {
                                        Id = item.Id,
                                        Name = item.Name,
                                        DocumentEffectiveDate = item.DocumentEffectiveDate
                                    }).ToJson().ToObject<Domain.Documents.Document>());
                                }
                            }
                            if (currentNominationSection.ServiceProviderCV != null)
                            {
                                current.ServiceProviderCV = new List<Domain.Documents.Document>();
                                foreach (var item in currentNominationSection.ServiceProviderCV)
                                {
                                    current.ServiceProviderCV.Add((new Domain.Documents.Document()
                                    {
                                        Id = item.Id,
                                        Name = item.Name,
                                        DocumentEffectiveDate = item.DocumentEffectiveDate
                                    }).ToJson().ToObject<Domain.Documents.Document>());
                                }
                            }
                            if (previousNominationSection.HCPCertificationDocument != null)
                            {
                                prev.HCPCertificationDocument = new List<Domain.Documents.Document>();
                                foreach (var item in previousNominationSection.HCPCertificationDocument)
                                {
                                    prev.HCPCertificationDocument.Add((new Domain.Documents.Document()
                                    {
                                        Id = item.Id,
                                        Name = item.Name,
                                        DocumentEffectiveDate = item.DocumentEffectiveDate
                                    }).ToJson().ToObject<Domain.Documents.Document>());
                                }
                            }
                            if (currentNominationSection.HCPCertificationDocument != null)
                            {
                                current.HCPCertificationDocument = new List<Domain.Documents.Document>();
                                foreach (var item in currentNominationSection.HCPCertificationDocument)
                                {
                                    current.HCPCertificationDocument.Add((new Domain.Documents.Document()
                                    {
                                        Id = item.Id,
                                        Name = item.Name,
                                        DocumentEffectiveDate = item.DocumentEffectiveDate

                                    }).ToJson().ToObject<Domain.Documents.Document>());
                                }
                            }
                            if (previousNominationSection.UploadConsultantAVRecordDocument != null)
                            {
                                prev.UploadConsultantAVRecordDocument = new List<Domain.Documents.Document>();
                                foreach (var item in previousNominationSection.UploadConsultantAVRecordDocument)
                                {
                                    prev.UploadConsultantAVRecordDocument.Add((new Domain.Documents.Document()
                                    {
                                        Id = item.Id,
                                        Name = item.Name,
                                        DocumentEffectiveDate = item.DocumentEffectiveDate
                                    }).ToJson().ToObject<Domain.Documents.Document>());
                                }
                            }
                            if (currentNominationSection.UploadConsultantAVRecordDocument != null)
                            {
                                current.UploadConsultantAVRecordDocument = new List<Domain.Documents.Document>();
                                foreach (var item in currentNominationSection.UploadConsultantAVRecordDocument)
                                {
                                    current.UploadConsultantAVRecordDocument.Add((new Domain.Documents.Document()
                                    {
                                        Id = item.Id,
                                        Name = item.Name,
                                        DocumentEffectiveDate = item.DocumentEffectiveDate

                                    }).ToJson().ToObject<Domain.Documents.Document>());
                                }
                            }

                            if (currentNominationSection.SectionStatusId == (long)StatusCode.HCPAmendmentInProgress || currentNominationSection.SectionStatusId == (long)StatusCode.HCPCoordinatorRAI || currentNominationSection.SectionStatusId == (long)StatusCode.HCPSeniorManagementRAI)
                            {
                                if (currentNominationSection.SectionStatusId == (long)StatusCode.HCPAmendmentInProgress)
                                {
                                    logType = LogType.Amendment;
                                }
                                if (currentNominationSection.SectionStatusId == (long)StatusCode.HCPCoordinatorRAI || currentNominationSection.SectionStatusId == (long)StatusCode.HCPSeniorManagementRAI)
                                {
                                    logType = LogType.RAI;
                                }

                                coveredRecipientSearchService.AuditServiceProviderFieldSection(prev, current, SessionWrapper.RequestUniqueId, SessionWrapper.CurrentUserId, logType, nominationViewModel.Id, SessionWrapper.CurrentUserLanguage, SessionWrapper.CurrentApplicationCode);
                            }

                            coveredRecipientSearchService.AuditHCPSection(prev, current, SessionWrapper.RequestUniqueId, SessionWrapper.CurrentUserId, logType, SessionWrapper.CurrentUserLanguage, SessionWrapper.CurrentApplicationCode);
                        }
                    }
                }
                if (nominationViewModel.IsSaveAndNextClicked)
                {
                    //Removing paging results from cache
                    var state = ServiceLocator.Current.GetInstance<IState>();
                    state.Cache.Remove<List<CoveredRecipientViewModel>>("CoveredRecipientForPaging" + SessionWrapper.RequestUniqueId.ToString());
                    ClearCache();
                    return Json("FairMarketValue");
                }
                if (nominationViewModel.IsSaveAndReturnToInbox)
                {
                    ClearCache();
                    return Json("Inbox");
                }

                return Json("Nomination");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ExceptionType.Application, "NominationController", "Create", e);
            }

            return null;
        }

        [UnitOfWork]
        public async Task<ActionResult> ValidateNominationAsync()
        {
            var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();
            var nominationSettings = GetNominationSettings();
            var nominationCompleteCount = SessionWrapper.RequestUniqueId == default(Guid) ? default(long) : (await businessNeedsNominationService.GetCompleteNominationCountAsync(SessionWrapper.RequestUniqueId)).Count;
            if (nominationSettings != null && nominationSettings.NumberOfNominationRequired == 1)
            {
                return Json(new
                                {
                                    hasNominationValidationPassed = true,
                                    NumberOfNominationRequired = 1,
                                    hasRequiredNominationAdded = nominationSettings.NumberOfNominationRequired <= nominationCompleteCount
                                }, JsonRequestBehavior.AllowGet);
            }
            return nominationSettings != null ? Json(new
                                                         {
                                                             hasNominationValidationPassed = nominationSettings.NumberOfNominationRequired <= nominationCompleteCount,
                                                             hasScreened = nominationSettings.IsScreening,
                                                             hasRequiredNominationAdded = true,
                                                             numberOfNominations = nominationSettings.NumberOfNominationRequired,
                                                             validationMessage = string.Format("In order to proceed with nomination at least {0} Service provider should be added in a Activity.", nominationSettings.NumberOfNominationRequired)
                                                         }, JsonRequestBehavior.AllowGet) : Json(new
                                                         {
                                                             hasNominationValidationPassed = true,
                                                             NumberOfNominationRequired = 0,
                                                             hasRequiredNominationAdded = true
                                                         }, JsonRequestBehavior.AllowGet);
        }

        private NominationSettingsViewModel GetNominationSettings()
        {

            var nominationSettingViewModel = new NominationSettingsViewModel
            {
                RequestStatusId = (long)SessionWrapper.CurrentRequestStatus
            };
            var activityCategory = CacheWrapper.GetMasterListItem(MasterListCode.ActivityCategory, SessionWrapper.ActivityCategory.ToString(), ApplicationCode.HIP);
            nominationSettingViewModel.ActivityCategoryId = activityCategory != null
                                                                ? activityCategory.Id
                                                                : default(long);
            var nominationSettingsController = ServiceLocator.Current.GetInstance<INominationSettingsController>();
            var nominationSettings = nominationSettingsController.ValidateNominationSettings(nominationSettingViewModel);

            return nominationSettings;
        }


        [UnitOfWork]
        public ActionResult Navigate(string SectionCode)
        {
            var requestSectionNavigationController = ServiceLocator.Current.GetInstance<IRequestSectionNavigationController>();
            var navigation = requestSectionNavigationController.Navigate((SectionCode)Enum.Parse(typeof(SectionCode), SectionCode), SessionWrapper.ActivityCategory);
            return Json(navigation.ControllerName);
        }

        [UnitOfWork]
        public ActionResult Remove(long id)
        {
            try
            {
                var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();
                businessNeedsNominationService.RemoveNomination(id);
                return RedirectToAction("RefreshNominationGrid", "Nomination");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ExceptionType.Application, "NominationController", "Remove", e);
            }

            return null;
        }

        [UnitOfWork]
        public ActionResult SetServiceProviderStatus(int nominationId)
        {
            WorkFlowProcess = ServiceLocator.Current.GetInstance<IWorkflowProcess>();

            var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();
            businessNeedsNominationService.Update(nominationId, StatusCode.RequestCancelled);
            WorkFlowProcess.CancelHCP(SessionWrapper.RequestUniqueId, SessionWrapper.ActivityCategory, WorkflowAction.CancelHCP,
                SessionWrapper.CurrentRequestStatus, SessionWrapper.CurrentUserId, nominationId);
            return Json("Service Provider Canceled Successfully", JsonRequestBehavior.AllowGet);
        }

        [UnitOfWork]
        public ActionResult DeleteDocument(long documentId, string biUniqueIdentifier)
        {
            using (new UnitOfWorkScope(TransactionMode.New))
            {
                var serviceProviderResumeController =
                    ServiceLocator.Current.GetInstance<IServiceProviderResumeController>();
                serviceProviderResumeController.Save(biUniqueIdentifier, documentId);
            }
            return null;
        }
        [UnitOfWork]
        [HttpGet]
        public ActionResult SearchVendors(string vendorName, string countryCode, string stateCode, string vendorId, string city, int Header, int currentPageIndex = 0,bool isFromContract=false)
        {
            if (currentPageIndex == 0)
                currentPageIndex = 1;
            string sortColumnName = "CrName";
            int pageSize = 10;
            string sortOrder = "Asc";
            int isHeaderSorting = Header;
            var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();
            var resultSet = businessNeedsNominationService.SearchVendors(vendorName, countryCode, stateCode, vendorId, city);
            if (resultSet != null && resultSet.Any())
            {
                if (!isFromContract)
                {
                    var selectedVendorsForTHisActivity = businessNeedsNominationService.GetAssociatedVendorsForThisActivity(SessionWrapper.RequestUniqueId);
                    if (selectedVendorsForTHisActivity != null && selectedVendorsForTHisActivity.Any())
                        resultSet.ForEach(x => x.AllAssociatedVendors = string.Join(",", selectedVendorsForTHisActivity.Select(n => n.ToString()).ToArray()));
                }
                var totalRowCount = resultSet.Count();
                PaginationHelper(totalRowCount, currentPageIndex, pageSize, sortColumnName, sortOrder);
                resultSet = resultSet.Page(currentPageIndex, pageSize).ToList();
            }
            return PartialView("~/Views/Shared/EditorTemplates/VendorSearchDetails.cshtml", resultSet);
        }
        [UnitOfWork]
        [HttpPost]
        public ActionResult AssociateVendor(long vendorId, long nominationSectionId)
        {
            try
            {
                var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();
                businessNeedsNominationService.AssociateVendor(vendorId, nominationSectionId);
                return Json(new { IsSuccess = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            { return Json(new { IsSuccess = false }, JsonRequestBehavior.AllowGet); }
        }
        [UnitOfWork]
        [HttpPost]
        public ActionResult RemoveVendor(long vendorId, long nominationSectionId)
        {
            try
            {
                var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();
                businessNeedsNominationService.RemoveVendor(vendorId, nominationSectionId);
                return Json(new { IsSuccess = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            { return Json(new { IsSuccess = false }, JsonRequestBehavior.AllowGet); }
        }
        [UnitOfWork]
        [HttpPost]
        public ActionResult GetVendorDetailById(long vendorId)
        {
            SelectedVendorDetailViewModel vendorDetail = new SelectedVendorDetailViewModel();
            var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();
            using (new UnitOfWorkScope(TransactionMode.New))
            {
                vendorDetail = businessNeedsNominationService.GetSelectedVendorDetail(vendorId);
            }
            return PartialView("~/Views/Nomination/SelectedVendorDetails.cshtml", vendorDetail);
        }

        [HttpGet]
        [UnitOfWork]
        public bool ValidateMSACheck(long serviceProviderId)
        {
            var masterServiceAgreementService = ServiceLocator.Current.GetInstance<IMasterServiceAgreementManagementService>();
            var businessUnitCode = SessionWrapper.BusinessUnit;
            var requestBusinessUnitId = CacheWrapper.GetMasterListItem(MasterListCode.BusinessUnit, businessUnitCode, SessionWrapper.CurrentApplicationCode).Id;
            string effectiveStartDate = SessionWrapper.ActivityStartDate != null ? SessionWrapper.ActivityStartDate.ToString() : null;
            string effectiveEndDate = SessionWrapper.ActivityEndDate != null ? SessionWrapper.ActivityEndDate.ToString() : null;           
            var result = masterServiceAgreementService.CheckDuplicateMsaHCp(serviceProviderId, effectiveStartDate, effectiveEndDate, requestBusinessUnitId);           
            return result;
        }

        /// <summary>
        /// To clear the service Provider search results from Cache
        /// </summary>
        private void ClearCache()
        {
            try
            {
                Session["coveredRecipientViewModel"] = null;
                Session.Remove("coveredRecipientViewModel");
                var state = ServiceLocator.Current.GetInstance<IState>();
                state.Cache.Remove<IEnumerable<CoveredRecipientViewModel>>(SessionWrapper.RequestUniqueId + "CoveredRecepients");
                state.Cache.Remove<IEnumerable<CoveredRecipientViewModel>>(SessionWrapper.RequestUniqueId + "CoveredRecpientsDependentSections");
                state.Cache.Remove<CoveredRecipientViewModel>(SessionWrapper.RequestUniqueId + "SearchViewModel");
                var coveredRecipientSearchService = ServiceLocator.Current.GetInstance<ICoveredRecipientSearchService>();
                coveredRecipientSearchService.CleanStageData(SessionWrapper.RequestUniqueId.ToString());
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ExceptionType.Application, "NominationController", "ClearCache", e);

            }
        }

        [UnitOfWork]
        [HttpPost]
        public ActionResult AmendServiceProvider(long nominationId)
        {
            try
            {
                var businessNeedsNominationService = ServiceLocator.Current.GetInstance<IBusinessNeedsNominationRepositoryService>();

                var amendServiceProviderResult = businessNeedsNominationService.AmendServiceProvider(nominationId, SessionWrapper.RequestID, SessionWrapper.CurrentUserId);

                var requestService = ServiceLocator.Current.GetInstance<IRequestService>();
                List<ActivityServiceProviderViewModel> objActivityServiceProviderViewModel = new List<ActivityServiceProviderViewModel>();
                objActivityServiceProviderViewModel.Add(new ActivityServiceProviderViewModel
                {
                    RequestID = SessionWrapper.RequestID,
                    ApprovalSequence = 0,
                    ApproverId = SessionWrapper.CurrentUserId,
                    DecisionId = (long)StatusCode.HCPAmendmentInProgress,
                    isAny = false,
                    isSectionComplete = false,
                    NominationSectionId = nominationId,
                    SectionMasterId = (long)SectionCode.Nomination
                });

                using (new UnitOfWorkScope(TransactionMode.New))
                {

                    string resultServiceProvider = string.Empty;
                    if (objActivityServiceProviderViewModel != null && objActivityServiceProviderViewModel.Count > 0)
                    {
                        resultServiceProvider = requestService.ActivityServiceProviderAuditDetail(SessionWrapper.RequestID, SessionWrapper.CurrentUserId, SectionCode.CompletenessReview, objActivityServiceProviderViewModel);
                    }
                }

                return Json(new { amendStatus = amendServiceProviderResult.ServiceProviderAmendStatus, FailureReasonText = string.Empty }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ExceptionType.Application, "NominationController", "AmendServiceProvider", e, CurrentUserId.ToString());
                return null;
            }
        }
    }
}
