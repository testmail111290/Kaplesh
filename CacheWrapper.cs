using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Practices.ServiceLocation;
using Polaris.HIP.Cache.Interface;
using Polaris.HIP.Common.Enums;
using Polaris.HIP.Core.Data.Repository.Enum;
using Polaris.HIP.Core.Data.Repository.Impl;
using Polaris.HIP.Core.Data.Repository.Interface;
using Polaris.HIP.Core.Extensions;
using Polaris.HIP.Core.State.Interface;
using Polaris.HIP.Data.EntityFramework.Impl;
using Polaris.HIP.Domain.Common;
using Polaris.HIP.Domain.Documents;
using Polaris.HIP.Domain.DomainExtensions;
using Polaris.HIP.Domain.DomainWrappers;
using Polaris.HIP.Domain.EmailTemplates;
using Polaris.HIP.Domain.MasterItems;
using Polaris.HIP.Domain.QuestionDomains;
using Polaris.HIP.Domain.QuestionDomains.Enum;
using Polaris.HIP.Domain.Users;
using Polaris.HIP.Domain.Requests;
using Polaris.HIP.Domain.Requests.ServiceProviders;
using Polaris.HIP.Domain.Requests.ServiceProviders.Enum;
using Polaris.HIP.Domain.AnnualPlan;
using System.Configuration;

namespace Polaris.HIP.Cache.Impl
{
    public sealed class CacheWrapper : ICacheWrapper
    {
        private readonly IState _state;
        private IRepository<MasterList> _masterListRepository;
        private IRepository<MasterListItem> _masterListItemRepository;
        private IRepository<Polaris.HIP.Domain.Requests.RequestSections.BrandingManagement> BrandingManagementRepository;
        //  private IState _state;
        //  private IRepository<MasterList> _masterListRepository;
        //  private IRepository<Currency> _currencyRepository;
        //   private IRepository<CountryMaster> _countryRepository;

        private const string GrantsType_3EducationalSupportforHCP = "3EducationalSupportforHCP&AdminStaff";
        private const string GrantsType_1ThirdPartySupport = "1ThirdPartySupport";
        private const string WhiteGloveEmailAddress = "%WhiteGloveEmail%";
        public CacheWrapper(IState state)
        {
            _state = state;
        }

        public string GetBrandName()
        {
            var brandName = _state.Cache.Get<string>("BrandName");
            if (brandName.IsNullOrEmpty())
            {
                BrandingManagementRepository = ServiceLocator.Current.GetInstance<IRepository<Polaris.HIP.Domain.Requests.RequestSections.BrandingManagement>>();
                brandName = BrandingManagementRepository.Select(brandObject => brandObject.BrandingName).ToList().LastOrDefault();
                _state.Cache.Put("BrandName", brandName);
            }
            return brandName;
        }

        public string GetContractTemplateContent(long templateId, ApplicationCode applicationCode)
        {
            var template = _state.Cache.Get<ContractTemplateMaster>("ContractTemplate" + templateId);
            if (template == null)
            {
                var contractTemplateMasterRepository =
                    ServiceLocator.Current.GetInstance<IRepository<ContractTemplateMaster>>();
                template = contractTemplateMasterRepository.FirstOrDefault(t => t.Id == templateId && t.Application.ApplicationCode == applicationCode);
                if (template != null)
                {
                    template.TemplateContent = template.TemplateContent.FormatPlaceholder("BrandName",
                        GetBrandName());
                    _state.Cache.Put("ContractTemplate" + templateId, template);
                }

            }
            return template != null ? template.TemplateContent : string.Empty;
        }

        public IReadOnlyList<FiscalYear> GetFiscalYears(ApplicationCode currentApplicationCode, int langCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<FiscalYear>>();
            var listFiscalYears = _state.Cache.Get<IReadOnlyList<FiscalYear>>();

            if (listFiscalYears.IsNullOrEmpty())
            {
                listFiscalYears = repository.Where(t => t.IsActive).ToList();
                _state.Cache.Put(listFiscalYears);
            }

            return listFiscalYears;
        }

        public List<ViewComboPair> GetAnnualPlanActivityTypes(ApplicationCode applicationCode, int langCode)
        {
            var activityCategories = GetMasterListItems(MasterListCode.ActivityCategory, applicationCode, langCode).Where(x => x.Code != ActivityCategoryCode.MasterServiceAgreement.ToString()).ToList();
            //var allCategory = GetMasterListItems(MasterListCode.AnnualPlanActivityType, applicationCode, langCode).First();
            //activityCategories.Add(allCategory);
            return activityCategories;
        }

        public IReadOnlyList<ViewComboPair> GetMasterListItems(MasterListCode masterListCode, ApplicationCode applicationCode, int langCode)
        {
            _masterListRepository = ServiceLocator.Current.GetInstance<IRepository<MasterList>>();
            var masterListItems = _state.Cache.Get<IReadOnlyList<MasterListItem>>(masterListCode + langCode.ToString());
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var tempMasterlistitem = new List<MasterListItem>();
            if (masterListItems.IsNullOrEmpty())
            {
                var masterListName = masterListCode.ToString();
                masterListItems = _masterListRepository.Fetch(m => m.AssociatedMasterList).FetchMany(m => m.ListValues)
                    .Single(m => m.Name.Equals(masterListName, StringComparison.InvariantCultureIgnoreCase) && m.IsActive).ListValues
                    .Where(l => l.IsActive && (l.IsDeleted == null || l.IsDeleted == false) && l.ApplicationMasterId == applicationMasterId).OrderBy(d => d.DisplayOrder).ToList();

                var allResources = GetAllResources(langCode, applicationCode);

                foreach (var item in masterListItems)
                {
                    if (allResources.ContainsKey(item.Name))
                    {
                        tempMasterlistitem.Add(new MasterListItem { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                    }
                }

                _state.Cache.Put(masterListCode + langCode.ToString(), tempMasterlistitem as IReadOnlyList<MasterListItem>);
            }
            else
            {
                foreach (var item in masterListItems)
                {
                    tempMasterlistitem.Add(new MasterListItem { Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                }
            }

            return tempMasterlistitem.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public IReadOnlyList<ViewComboPair> GetMasterListItems(MasterListCode masterListCode, ApplicationCode applicationCode, int langCode, long associateListItemId)
        {
            _masterListRepository = ServiceLocator.Current.GetInstance<IRepository<MasterList>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var tempMasterlistitem = new List<MasterListItem>();
            var masterListName = masterListCode.ToString();
            var masterListItems = _masterListRepository.Fetch(m => m.AssociatedMasterList).FetchMany(m => m.ListValues)
                .Single(m => m.Name.Equals(masterListName, StringComparison.InvariantCultureIgnoreCase) && m.IsActive).ListValues
                .Where(l => l.IsActive && (l.IsDeleted == null || l.IsDeleted == false) && l.ApplicationMasterId == applicationMasterId && l.AssociatedListItemId == associateListItemId).OrderBy(d => d.DisplayOrder).ToList();

            var allResources = GetAllResources(langCode, applicationCode);

            foreach (var item in masterListItems)
            {
                if (allResources.ContainsKey(item.Name))
                {
                    tempMasterlistitem.Add(new MasterListItem { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                }
            }
            return tempMasterlistitem.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public MasterListItem GetMasterListItem(MasterListCode masterListCode, long masterListItemId, ApplicationCode applicationCode, int langCode)
        {
            _masterListRepository = ServiceLocator.Current.GetInstance<IRepository<MasterList>>();
            var masterListItems = _state.Cache.Get<IReadOnlyList<MasterListItem>>(masterListCode + langCode.ToString());
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var tempMasterlistitem = new List<MasterListItem>();
            if (masterListItems.IsNullOrEmpty())
            {
                var masterListName = masterListCode.ToString();
                masterListItems = _masterListRepository.Fetch(m => m.AssociatedMasterList).FetchMany(m => m.ListValues).Single(m => m.Name.Equals(masterListName, StringComparison.InvariantCultureIgnoreCase) && m.IsActive).ListValues.Where(l => l.IsActive && (l.IsDeleted == null || l.IsDeleted == false) && l.ApplicationMasterId == applicationMasterId).OrderBy(l => l.DisplayOrder).ToList();
                var allResources = GetAllResources(langCode, applicationCode);
                foreach (var item in masterListItems)
                {
                    if (allResources.ContainsKey(item.Name))
                    {
                        tempMasterlistitem.Add(new MasterListItem { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                    }

                }
                _state.Cache.Put(masterListCode + langCode.ToString(), tempMasterlistitem as IReadOnlyList<MasterListItem>);
            }
            else
            {
                foreach (var item in masterListItems)
                {
                    tempMasterlistitem.Add(new MasterListItem { Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                }
            }

            var masterListItem = tempMasterlistitem.FirstOrDefault(l => l.Id == masterListItemId);

            return masterListItem;
        }

        public MasterListItem GetMasterListItem(MasterListCode masterListCode, long? masterlistitemId, ApplicationCode applicationCode)
        {
            _masterListRepository = ServiceLocator.Current.GetInstance<IRepository<MasterList>>();
            // var masterListItems = _state.Cache.Get<IReadOnlyList<MasterListItem>>(masterListCode);
            var applicationMasterId = Convert.ToInt64(applicationCode);
            // code = code.ToUpper();
            //if (masterListItems.IsNullOrEmpty())
            //{
            var masterListName = masterListCode.ToString();
            var masterListItems = _masterListRepository.Fetch(m => m.AssociatedMasterList).FetchMany(m => m.ListValues)
                .Single(m => m.Name.Equals(masterListName, StringComparison.InvariantCultureIgnoreCase) && m.IsActive)
                .ListValues.Where(l => l.IsActive && (l.IsDeleted == null || l.IsDeleted == false) && l.ApplicationMasterId == applicationMasterId).OrderBy(d => d.DisplayOrder).ToList();
            //  }

            var masterListItem = masterListItems.FirstOrDefault(l => l.Id == masterlistitemId);

            return masterListItem;
        }

        public MasterListItem GetMasterListItem(MasterListCode masterListCode, string code, ApplicationCode applicationCode)
        {
            _masterListRepository = ServiceLocator.Current.GetInstance<IRepository<MasterList>>();
            var masterListItems = _state.Cache.Get<IReadOnlyList<MasterListItem>>(masterListCode);
            var applicationMasterId = Convert.ToInt64(applicationCode);
            code = code.ToUpper();
            if (masterListItems.IsNullOrEmpty())
            {
                var masterListName = masterListCode.ToString();
                masterListItems = _masterListRepository.Fetch(m => m.AssociatedMasterList).FetchMany(m => m.ListValues)
                    .Single(m => m.Name.Equals(masterListName, StringComparison.InvariantCultureIgnoreCase) && m.IsActive)
                    .ListValues.Where(l => l.IsActive && (l.IsDeleted == null || l.IsDeleted == false) && l.ApplicationMasterId == applicationMasterId).OrderBy(d => d.DisplayOrder).ToList();
                _state.Cache.Put(masterListCode, masterListItems);
            }

            var masterListItem = masterListItems.FirstOrDefault(l => l.Code.ToUpper() == code);

            return masterListItem;
        }

        public IReadOnlyList<ViewComboPair> GetListItems<TListItem>(ApplicationCode applicationCode, int langCode)
            where TListItem : ListItemBase
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<IReadOnlyList<TListItem>>(typeof(TListItem).FullName + langCode);
            var tempListItems = new List<ListItemBase>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                var allResources = GetAllResources(langCode, applicationCode);

                foreach (var item in listItems)
                {
                    if (allResources.ContainsKey(item.Name))
                    {
                        if (allResources.ContainsKey(item.Name))
                        {
                            tempListItems.Add(new MasterListItem { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                        }
                    }
                }
                _state.Cache.Put(typeof(TListItem).FullName + langCode, tempListItems as IReadOnlyList<ListItemBase>);
            }
            else
            {
                foreach (var item in listItems)
                {
                    tempListItems.Add(new MasterListItem { Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                }
            }
            tempListItems = tempListItems.OrderBy(l => l.DisplayOrder).ToList();
            return tempListItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public TListItem GetListItem<TListItem>(long id, ApplicationCode applicationCode)
            where TListItem : ListItemBase
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<IReadOnlyList<TListItem>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(listItems);
            }

            var listItem = listItems.FirstOrDefault(l => l.Id == id);

            return listItem;
        }

        public TListItem GetListItem<TListItem>(string code, ApplicationCode applicationCode)
            where TListItem : ListItemBase
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<IReadOnlyList<TListItem>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(listItems);
            }

            var listItem = listItems.FirstOrDefault(l => l.Code.ToLower() == code.ToLower());

            return listItem;
        }

        public IReadOnlyList<ViewComboPair> GetListItems<TListItem, TListItemFetch>(Expression<Func<TListItem, TListItemFetch>> fetchPath, ApplicationCode applicationCode)
            where TListItem : ListItemBase
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<IReadOnlyList<TListItem>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Fetch(fetchPath).Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(listItems);
            }

            return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public TListItem GetListItem<TListItem, TListItemFetch>(long id, Expression<Func<TListItem, TListItemFetch>> fetchPath, ApplicationCode applicationCode)
            where TListItem : ListItemBase
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<IReadOnlyList<TListItem>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Fetch(fetchPath).Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(listItems);
            }

            var listItem = listItems.FirstOrDefault(l => l.Id == id);

            return listItem;
        }

        public TListItem GetListItem<TListItem, TListItemFetch>(string code, Expression<Func<TListItem, TListItemFetch>> fetchPath, ApplicationCode applicationCode)
            where TListItem : ListItemBase
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<IReadOnlyList<TListItem>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Fetch(fetchPath).Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(listItems);
            }

            var listItem = listItems.Single(l => l.Code == code);

            return listItem;
        }

        public IReadOnlyList<TUser> GetUsers<TUser>()
            where TUser : PersonAttributes
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TUser>>();
            var listItems = _state.Cache.Get<IReadOnlyList<TUser>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Where(u => u.IsActive).ToList();
                _state.Cache.Put(listItems);
            }

            return listItems;
        }

        public TUser GetUser<TUser>(long id)
            where TUser : PersonAttributes
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TUser>>();
            var users = _state.Cache.Get<IReadOnlyList<TUser>>();
            if (users.IsNullOrEmpty())
            {
                users = repository.Where(u => u.IsActive).ToList();
                _state.Cache.Put(users);
            }

            var user = users.Single(l => l.Id == id);

            return user;
        }

        public IReadOnlyList<TUser> GetUsers<TUser, TUserFetch>(Expression<Func<TUser, TUserFetch>> fetchPath)
            where TUser : PersonAttributes
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TUser>>();
            var listItems = _state.Cache.Get<IReadOnlyList<TUser>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Fetch(fetchPath).Where(u => u.IsActive).ToList();
                _state.Cache.Put(listItems);
            }

            return listItems;
        }

        public TUser GetUser<TUser, TUserFetch>(long id, Expression<Func<TUser, TUserFetch>> fetchPath)
            where TUser : PersonAttributes
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TUser>>();
            var users = _state.Cache.Get<IReadOnlyList<TUser>>();
            if (users.IsNullOrEmpty())
            {
                users = repository.Fetch(fetchPath).Where(u => u.IsActive).ToList();
                _state.Cache.Put(users);
            }

            var user = users.Single(l => l.Id == id);

            return user;
        }

        public IReadOnlyCollection<ViewComboPair> GetRoles(ApplicationCode applicationCode, int langCode)
        {
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<Role>>();
            var roles = _state.Cache.Get<IReadOnlyList<Role>>(typeof(Currency).Name + langCode);
            var tempRoles = new List<Role>();

            if (roles.IsNullOrEmpty())
            {
                roles = repository.Where(u => (u.IsActive && u.ApplicationMasterId == applicationMasterId)).OrderBy(r => r.Name).ToList();
                var allResources = GetAllResources(langCode, applicationCode);

                foreach (var item in roles)
                {
                    if (allResources.ContainsKey(item.Name))
                    {
                        tempRoles.Add(new Role { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, Id = item.Id, IsDisplay = item.IsDisplay });
                    }
                }
                _state.Cache.Put(typeof(Role).Name + langCode, tempRoles);
            }
            else
            {
                foreach (var item in roles)
                {
                    tempRoles.Add(new Role { Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder, Id = item.Id, IsDisplay = item.IsDisplay });
                }
            }



            return tempRoles.Where(u => u.IsDisplay).Create(t => t.Id, t => t.Name, t => t.Code).ToList();


        }

        public QuestionMaster GetQuestionMaster(QuestionCode questionCode, ApplicationCode applicationCode)
        {
            var code = questionCode.ToString();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<QuestionMaster>>();
            var questions = _state.Cache.Get<IReadOnlyList<QuestionMaster>>();

            if (questions.IsNullOrEmpty())
            {
                questions = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(questions);
            }

            return questions.SingleOrDefault(q => q.Code == code);
        }

        public List<QuestionMaster> GetQuestionMasterBySectionId(long sectionId, ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<QuestionMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var questions = _state.Cache.Get<IReadOnlyList<QuestionMaster>>();
            if (questions.IsNullOrEmpty())
            {
                questions = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(questions);
            }
            return questions.Where(u => u.IsActive && u.SectionId == sectionId).ToList();
        }

        public User GetUserByGroupType(GroupTypeCode groupTypeCode, ApplicationCode applicationCode)
        {
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<UserGroup>>();
            var userGroup = _state.Cache.Get<IReadOnlyList<UserGroup>>();
            string userGroupCode = groupTypeCode.ToString();

            if (userGroup.IsNullOrEmpty())
            {
                userGroup = repository.Fetch(u => u.GroupType).FetchMany(u => u.Users)
                    .ThenFetch(u => u.User).
                    Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(userGroup);
            }
            var userGroups = userGroup.FirstOrDefault(u => u.GroupType.Code == userGroupCode);
            if (userGroups != null)
            {
                var firstOrDefault = userGroups.Users.OrderBy(u => u.SequenceNumber).FirstOrDefault();
                if (firstOrDefault != null)
                    return firstOrDefault.User;
            }
            return null;
        }



        public Status GetRequestStatus(StatusCode statusCode, ApplicationCode applicationCode)
        {
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<Status>>();
            var statuses = _state.Cache.Get<IReadOnlyList<Status>>();

            if (statuses.IsNullOrEmpty())
            {
                statuses = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(statuses);
            }

            return statuses.FirstOrDefault(a => a.StatusCode == statusCode);
        }

        public Status GetRequestStatus(long statusId, ApplicationCode applicationCode)
        {
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<Status>>();
            var statuses = _state.Cache.Get<IReadOnlyList<Status>>();

            if (statuses.IsNullOrEmpty())
            {
                statuses = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(statuses);
            }

            return statuses.FirstOrDefault(a => a.Id == statusId);
        }


        public IReadOnlyList<ViewComboPair> GetEmailTemplates<TEmailTemplate>(ApplicationCode applicationCode)
            where TEmailTemplate : EmailTemplate
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TEmailTemplate>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var templateTypes = _state.Cache.Get<IReadOnlyList<TEmailTemplate>>();
            if (templateTypes.IsNullOrEmpty())
            {
                templateTypes = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId)
                                        .ToList();
            }

            return templateTypes.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public IReadOnlyCollection<ViewComboPair> GetEmailTemplateTags<TEmailTemplateTag>(ApplicationCode applicationCode)
            where TEmailTemplateTag : TemplateTag
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TEmailTemplateTag>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var templateTypes = _state.Cache.Get<IReadOnlyList<TEmailTemplateTag>>();
            if (templateTypes.IsNullOrEmpty())
            {
                templateTypes = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).OrderBy(e => e.Name).ToList();
            }

            return templateTypes.Create(l => l.Id, l => l.Tag, l => l.Code).ToList();
        }

        public string GetToolTip(ToolTipCode toolTipCode, int langauage, ApplicationCode applicationCode)
        {
            var brandingName = GetBrandName();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            string code = toolTipCode.ToString();
            string toolTiptext = string.Empty;
            var repository = ServiceLocator.Current.GetInstance<IRepository<InstructionTemplate>>();
            var toolTipTexts = _state.Cache.Get<IReadOnlyList<InstructionTemplate>>("ToolTipText_" + langauage);

            if (toolTipTexts.IsNullOrEmpty())
            {
                toolTipTexts = repository.Fetch(t => t.Language).Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId && u.LanguageId == langauage).ToList
                    ();
                _state.Cache.Put("ToolTipText_" + langauage, toolTipTexts);
            }

            var instructiontext = toolTipTexts.FirstOrDefault(a => a.Code == code);
            if (instructiontext != null)
                toolTiptext = instructiontext.DisplayText;
            toolTiptext = toolTiptext.FormatPlaceholder("BrandName", brandingName);

            return toolTiptext;
        }

        public void ClearToolTips(int langauage)
        {
            _state.Cache.Remove<IReadOnlyList<InstructionTemplate>>("ToolTipText_" + langauage);
        }
        public void ClearContractTemplateCache(long templateId)
        {
            _state.Cache.Remove<ContractTemplateMaster>("ContractTemplate" + templateId);
        }

        public void ClearMasterList(MasterListCode masterListCode)
        {
            _state.Cache.Remove<IReadOnlyList<MasterListItem>>(masterListCode);
        }

        public IReadOnlyList<ViewComboPair> GetContractTemplateMaster(ApplicationCode applicationCode, int langcode)
        {
            var tempList = new List<ContractTemplateMaster>();
            var repository = ServiceLocator.Current.GetInstance<IRepository<ContractTemplateMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var contractTemplates = _state.Cache.Get<IReadOnlyList<ContractTemplateMaster>>();
            if (contractTemplates.IsNullOrEmpty())
            {
                contractTemplates = repository.Where(u => u.IsActive && u.IsAvailableForGenerateContract && u.ApplicationMasterId == applicationMasterId)
                                        .ToList();
            }
            var allResources = GetAllResources(langcode, ApplicationCode.HIP);
            foreach (var item in contractTemplates)
            {
                if (allResources.ContainsKey(item.TemplateName))
                {
                    tempList.Add(new ContractTemplateMaster { Id = item.Id, ApplicationMasterId = item.ApplicationMasterId, TemplateName = allResources[item.TemplateName] });
                }
            }
            return tempList.Create(l => l.Id, l => l.TemplateName, l => l.Id.ToString()).ToList();
        }

        public IReadOnlyList<ViewComboPair> GetDocumentTypesByCategory(string category, ApplicationCode applicationCode, int langCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<DocumentType>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var documentTypes = _state.Cache.Get<IReadOnlyList<DocumentType>>();
            var tempDocumentTypes = new List<DocumentType>();
            if (documentTypes.IsNullOrEmpty())
            {
                documentTypes = repository.Where(u => u.IsActive && u.Category == category && u.ApplicationMasterId == applicationMasterId).ToList();
            }
            else
            {
                documentTypes = documentTypes.Where(u => u.IsActive && u.Category == category && u.ApplicationMasterId == applicationMasterId).ToList();
            }
            var allResources = GetAllResources(langCode, applicationCode);
            foreach (var item in documentTypes)
            {
                if (allResources.ContainsKey(item.Name))
                {
                    tempDocumentTypes.Add(new DocumentType { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                }
            }
            return tempDocumentTypes.Create(d => d.Id, d => d.Name, d => d.Code).ToList();

        }

        public string GetDocumentTypeById(long documentTypeId, ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<DocumentType>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var documentTypes = _state.Cache.Get<IReadOnlyList<DocumentType>>();
            if (documentTypes.IsNullOrEmpty())
            {
                documentTypes = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(documentTypes);
            }

            var documentType = documentTypes.SingleOrDefault(d => d.Id == documentTypeId);

            return documentType != null ? documentType.Name : string.Empty;
        }

        public IReadOnlyList<ViewComboPair> GetContractTemplateMasterBusinessUnitId(string businessUnitCode, ApplicationCode applicationCode)
        {
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<ContractTemplateMaster>>();
            var contractTemplates = _state.Cache.Get<IReadOnlyList<ContractTemplateMaster>>();
            if (contractTemplates.IsNullOrEmpty())
            {
                contractTemplates = repository.Fetch(c => c.BusinessUnit).Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId)
                                        .ToList();
            }

            return contractTemplates.Create(l => l.Id, l => l.TemplateName, l => l.TemplateName).ToList();
        }

        public ViewComboPair GetContractTemplateMasterDetailsByName(string templateName, ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<ContractTemplateMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var contractTemplates = _state.Cache.Get<IReadOnlyList<ContractTemplateMaster>>();
            if (contractTemplates.IsNullOrEmpty())
            {
                contractTemplates = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId)
                                        .ToList();
            }

            return contractTemplates.Create(l => l.Id, l => l.TemplateName, l => l.TemplateName).FirstOrDefault(a => a.Code == templateName);
        }

        public SpecialityMaster GetSpecialityMaster(string name, ApplicationCode applicationCode)
        {
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<SpecialityMaster>>();
            var listItems = repository.Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();

            return listItems.FirstOrDefault(s => s.Code == name);
        }

        public SpecialityMaster GetSpecialityMasterByCode(string code, ApplicationCode applicationCode)
        {
            List<SpecialityMaster> spcilitymaster = new List<SpecialityMaster>();
            spcilitymaster = _state.Cache.Get<List<SpecialityMaster>>("SpecialityMasterValue_" + code);

            if (spcilitymaster == null)
            {
                var applicationMasterId = Convert.ToInt64(applicationCode);
                var repository = ServiceLocator.Current.GetInstance<IRepository<SpecialityMaster>>();
                spcilitymaster = repository.Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put<List<SpecialityMaster>>("SpecialityMasterValue_" + code, spcilitymaster);
            }
            return spcilitymaster.FirstOrDefault(s => s.Code == code);
        }


        public IReadOnlyList<ViewComboPair> GetSpecialityMaster(string countryCode, long hcpType, ApplicationCode applicationCode, int langCode)
        {
            var listItems = _state.Cache.Get<IReadOnlyList<SpecialityMaster>>(typeof(SpecialityMaster).Name + langCode);
            var tempMasterlistitem = new List<SpecialityMaster>();
            if (listItems.IsNullOrEmpty())
            {
                var applicationMasterId = Convert.ToInt64(applicationCode);
                var repository = ServiceLocator.Current.GetInstance<IRepository<SpecialityMaster>>();

                listItems = repository.Fetch(s => s.ServiceProviderType).Fetch(s => s.Country).Where
                    (l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                var allResources = GetAllResources(langCode, applicationCode);
                foreach (var item in listItems)
                {
                    if (allResources.ContainsKey(item.Name))
                    {
                        tempMasterlistitem.Add(new SpecialityMaster { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id, ServiceProviderTypeId = item.ServiceProviderTypeId });
                    }
                }
                _state.Cache.Put(typeof(SpecialityMaster).Name + langCode, tempMasterlistitem as IReadOnlyList<SpecialityMaster>);
            }
            else
            {
                foreach (var item in listItems)
                {
                    tempMasterlistitem.Add(new SpecialityMaster { Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id, ServiceProviderTypeId = item.ServiceProviderTypeId });
                }
            }
            tempMasterlistitem = tempMasterlistitem.Where(l => l.ServiceProviderTypeId == hcpType).ToList();
            return tempMasterlistitem.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public DegreeMaster GetDegreeMaster(string code, ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<DegreeMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<IReadOnlyList<DegreeMaster>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(listItems);
            }

            return listItems.FirstOrDefault(s => s.Code == code);
        }

        public IReadOnlyList<ReadOnlyTabMaster> GetReadOnlyTabMaster(ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<ReadOnlyTabMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<IReadOnlyList<ReadOnlyTabMaster>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Fetch(s => s.ActivityCategory).Fetch(s => s.ConsultantType).Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(listItems);
            }

            return listItems;
        }

        public IReadOnlyList<TabCategoryMaster> GetTabCategoryMaster(ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<TabCategoryMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<IReadOnlyList<TabCategoryMaster>>();
            if (listItems.IsNullOrEmpty())
            {
                listItems = repository.Where(l => l.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(listItems);
            }

            return listItems;
        }

        public string GetInstructionText(InstructionTextCode instructionTextCode, int language, ApplicationCode applicationCode)
        {
            string code = instructionTextCode.ToString();
            return GetInstructionText(code, language, applicationCode);
        }

        public string GetLocalization(string key, int language, ApplicationCode applicationCode)
        {
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var formFieldrepository = ServiceLocator.Current.GetInstance<IRepository<FormField>>();
            string localizationText = string.Empty;
            var localizationTexts = _state.Cache.Get<IReadOnlyList<FormField>>("Localization_Text_" + language);

            if (localizationTexts.IsNullOrEmpty())
            {
                localizationTexts = formFieldrepository.Fetch(l => l.Language).Where(u => u.IsActive && u.Language.LanguageCode == language && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put("Localization_Text_" + language, localizationTexts);
            }

            var instructiontext = localizationTexts.SingleOrDefault(a => a.FieldName == key);
            if (instructiontext != null)
                localizationText = instructiontext.DisplayText;
            return localizationText;
        }


        public List<ViewComboPair> GetStates(string countryCode, ApplicationCode applicationCode, int langCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<CountryMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var country = repository.FetchMany(t => t.States).FirstOrDefault(u => u.Code == countryCode && u.IsActive && u.ApplicationMasterId == applicationMasterId);


            if (country != null && (country.States != null && country.States.Any()))
            {
                var allResources = GetAllResources(langCode, applicationCode);
                var stateList = new List<StateMaster>();
                foreach (var item in country.States)
                {
                    if (allResources.ContainsKey(item.Name))
                    {
                        stateList.Add(new StateMaster { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                    }
                }

                return stateList.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
            }
            return null;
        }

        public List<ViewComboPair> GetStates(long countryId, ApplicationCode applicationCode, int langCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<CountryMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var country = repository.FetchMany(t => t.States).FirstOrDefault(u => u.Id == countryId && u.IsActive && u.ApplicationMasterId == applicationMasterId);

            if (country != null && (country.States != null && country.States.Any()))
            {
                var allResources = GetAllResources(langCode, applicationCode);
                var stateList = new List<StateMaster>();
                foreach (var item in country.States)
                {
                    if (allResources.ContainsKey(item.Name))
                    {
                        stateList.Add(new StateMaster { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                    }
                }

                return stateList.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
            }
            return null;
        }


        public List<ViewComboPair> GetConsultantCountry(ApplicationCode applicationCode, int langCode)
        {

            var listItems = _state.Cache.Get<IReadOnlyList<CountryMaster>>(typeof(CountryMaster).Name + langCode);
            var tempMasterlistitem = new List<CountryMaster>();
            if (listItems.IsNullOrEmpty())
            {
                var applicationMasterId = Convert.ToInt64(applicationCode);
                var repository = ServiceLocator.Current.GetInstance<IRepository<CountryMaster>>();
                listItems = repository.Where(l => l.IsActive && l.IsConsultantCountry == true && l.ApplicationMasterId == applicationMasterId).ToList();
                var allResources = GetAllResources(langCode, applicationCode);
                foreach (var item in listItems)
                {
                    if (allResources.ContainsKey(item.Name))
                    {
                        tempMasterlistitem.Add(new CountryMaster { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                    }
                }
                _state.Cache.Put(typeof(CountryMaster).Name + langCode, tempMasterlistitem as IReadOnlyList<CountryMaster>);
            }
            else
            {
                foreach (var item in listItems)
                {
                    tempMasterlistitem.Add(new CountryMaster { Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                }
            }

            tempMasterlistitem = tempMasterlistitem.OrderBy(l => l.DisplayOrder).ToList();
            return tempMasterlistitem.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }


        public List<ViewComboPair> GetStates(ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<StateMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = repository.Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();

            listItems = listItems.OrderBy(l => l.DisplayOrder).ToList();
            return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }


        public string GetResourceText(string key, int langCode, ApplicationCode applicationCode)
        {
            string resourceText = string.Empty;
            var allResources = GetAllResources(langCode, applicationCode);
            if (allResources != null && allResources.Count > 0)
            {
                var resourceLang = allResources.FirstOrDefault(a => a.Key == key);
                if (!resourceLang.Equals(default(KeyValuePair<string, string>)))
                    resourceText = resourceLang.Value;
            }
            var texttoDisplay = resourceText;
            var textToReplace = WhiteGloveEmailAddress;
            var whiteGloveEmail = ConfigurationManager.AppSettings["whitegloveemail"].ToString();
            if (texttoDisplay.Contains(textToReplace))
            {
                texttoDisplay = texttoDisplay.Replace(textToReplace, whiteGloveEmail);
            }

            return texttoDisplay;
        }

        public Dictionary<string, string> GetAllResources(int langCode, ApplicationCode applicationCode)
        {
            using (new UnitOfWorkScope(TransactionMode.New))
            {
                var brandingName = GetBrandName();

                var resLangRepository = ServiceLocator.Current.GetInstance<IRepository<ResourceLanguage>>();
                List<ResourceLanguage> resourceLangugageUpdated = new List<ResourceLanguage>();
                var allResources = _state.Cache.Get<Dictionary<string, string>>("Resource_Text_" + langCode);
                if (allResources == null || allResources.Count == 0)
                {
                    var resourceTexts =
                        resLangRepository.Fetch(r => r.LanguageMaster)
                            .Fetch(r => r.Resource)
                            .Where(r => r.LanguageMaster.LanguageCode == langCode)
                            .ToList();
                    // Non English Language implement defaulting logic to fill missed entries from English Language
                    if (langCode != 1)
                    {
                        var defaultResourceTexts =
                            resLangRepository.Fetch(r => r.LanguageMaster)
                                .Fetch(r => r.Resource)
                                .Where(r => r.LanguageMaster.LanguageCode == 1)
                                .ToList();
                        var resource = resourceTexts;
                        if (resourceTexts.Count == 0)
                        {
                            resourceTexts = defaultResourceTexts;
                        }
                        else
                        {
                            var missedEntries =
                                defaultResourceTexts.Where(
                                    a => resourceTexts.FirstOrDefault(r => r.Resource.Name == a.Resource.Name) == null);
                            resource.AddRange(missedEntries);
                            resourceTexts = resource;
                        }
                    }
                    resourceLangugageUpdated.AddRange(resourceTexts.Select(item =>
                       new ResourceLanguage
                       {
                           Resource = item.Resource,
                           Resource_Id = item.Resource_Id,
                           Description = item.Description.FormatPlaceholder("BrandName", brandingName),
                           Language_Id = item.Language_Id,
                           LanguageMaster = item.LanguageMaster
                       }));
                    _state.Cache.Put("Resource_Text_" + langCode,
                        resourceLangugageUpdated.Where(a => a.Resource != null)
                            .ToDictionary(a => a.Resource.Name, a => a.Description));

                    allResources = _state.Cache.Get<Dictionary<string, string>>("Resource_Text_" + langCode);
                }
                return allResources;
            }

        }

        public void UpdateCache(bool completeUpdate, int langCode, ApplicationCode applicationCode)
        {
            if (completeUpdate)
            {
                _state.Cache.Remove<Dictionary<string, string>>("Resource_Text_" + langCode);
                GetAllResources(langCode, applicationCode);
            }
            else
            {
                //Update the code for conditional update
            }
        }

        public IReadOnlyList<ViewComboPair> GetApprovalDecision(SectionCode section, ApplicationCode applicationCode, bool? isHcp, int langCode)
        {

            var masterListItems = _state.Cache.Get<IReadOnlyList<ApproverDecisionMapping>>(section + langCode.ToString());
            var repository = ServiceLocator.Current.GetInstance<IRepository<ApproverDecisionMapping>>();
            var tempMasterlistitem = new List<ApproverDecisionMapping>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            if (masterListItems.IsNullOrEmpty())
            {
                masterListItems = repository.Fetch(m => m.HCPDecision).Fetch(m => m.Section)
                    .Where(l => l.ApplicationMasterId == applicationMasterId).OrderBy(d => d.DisplayOrder).ToList();

                var allResources = GetAllResources(langCode, applicationCode);

                foreach (var item in masterListItems)
                {
                    if (item.HCPDecision != null && allResources.ContainsKey(item.HCPDecision.Name))
                    {
                        tempMasterlistitem.Add(new ApproverDecisionMapping
                        {
                            HCPDecisionId = item.HCPDecisionId,
                            HCPDecision = (new MasterListItem { Name = allResources[item.HCPDecision.Name], Code = item.HCPDecision.Code }),
                            Section = (new SectionMaster { SectionCode = item.Section.SectionCode }),
                            IsHCPStatus = item.IsHCPStatus
                        });
                    }
                }

                _state.Cache.Put(section + langCode.ToString(), tempMasterlistitem as IReadOnlyList<ApproverDecisionMapping>);
            }
            else
            {
                foreach (var item in masterListItems)
                {
                    tempMasterlistitem.Add(new ApproverDecisionMapping
                    {
                        HCPDecisionId = item.HCPDecisionId,
                        HCPDecision = (new MasterListItem { Name = item.HCPDecision.Name, Code = item.HCPDecision.Code }),
                        Section = (new SectionMaster { SectionCode = item.Section.SectionCode }),
                        IsHCPStatus = item.IsHCPStatus
                    });
                }
            }


            tempMasterlistitem = tempMasterlistitem.Where(m => m.Section.SectionCode == section &&
                    (m.IsHCPStatus.Value == isHcp.Value)).ToList();
            return tempMasterlistitem.Create(l => l.HCPDecisionId ?? 0, l => l.HCPDecision.Name, l => l.HCPDecision.Code).ToList();

        }

        public string GetResourceText(LabelTextCode key, int langCode, ApplicationCode applicationCode)
        {
            var resourceText = string.Empty;
            var code = key.ToString();
            var allResources = GetAllResources(langCode, applicationCode);
            if (allResources != null && allResources.Count > 0)
            {
                var resourceLang = allResources.FirstOrDefault(a => a.Key == code);
                if (!resourceLang.Equals(default(KeyValuePair<string, string>)))
                    resourceText = resourceLang.Value;
            }
            var texttoDisplay = resourceText;
            var textToReplace = WhiteGloveEmailAddress;
            var whiteGloveEmail = ConfigurationManager.AppSettings["whitegloveemail"].ToString();
            if (texttoDisplay.Contains(textToReplace))
            {
                texttoDisplay = texttoDisplay.Replace(textToReplace, whiteGloveEmail);
            }

            return texttoDisplay;
        }


        public string GetInstructionText(string instructionTextCode, int language, ApplicationCode applicationCode)
        {
            var brandName = GetBrandName();
            var code = instructionTextCode;
            var instructionText = string.Empty;
            var repository = ServiceLocator.Current.GetInstance<IRepository<InstructionTemplate>>();
            var instructionTexts = _state.Cache.Get<IReadOnlyList<InstructionTemplate>>("InstructionText_" + language);

            var applicationMasterId = Convert.ToInt64(applicationCode);
            if (instructionTexts.IsNullOrEmpty())
            {
                instructionTexts = repository.Fetch(u => u.Language).Where(u => u.IsActive && !u.IsToolTip
                    && u.ApplicationMasterId == applicationMasterId && u.Language.LanguageCode == language).ToList();
                if (language != 1)
                {
                    var instruction = instructionTexts.ToList();
                    //Default - English Language
                    var defaultinstructionTexts = instructionTexts = repository.Fetch(u => u.Language).Where(u => u.IsActive && !u.IsToolTip
                        && u.ApplicationMasterId == applicationMasterId && u.Language.LanguageCode == 1).ToList();

                    if (instructionTexts.Count == 0)
                    {
                        instructionTexts = defaultinstructionTexts;
                    }
                    else
                    {
                        var missedEntries = defaultinstructionTexts.Where(a => instructionTexts.FirstOrDefault(r => r.Code == a.Code) == null);
                        instruction.AddRange(missedEntries);
                        instructionTexts = instruction;
                    }
                }
                _state.Cache.Put("InstructionText_" + language, instructionTexts);
            }



            var instructiontext = instructionTexts.SingleOrDefault(a => a.Code == code);
            if (instructiontext != null)
                instructionText = instructiontext.DisplayText;
            instructionText = instructionText.FormatPlaceholder("BrandName", brandName);
            return instructionText;
        }




        public void ClearBrandingReference(int langauage, ApplicationCode applicationCode)
        {
            var LangugaeMasterRepository = ServiceLocator.Current.GetInstance<IRepository<LanguageMaster>>();
            var TotalLangRecords = LangugaeMasterRepository.ToList();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<InstructionTemplate>>();
            var toolTipTexts = repository.Fetch(t => t.Language).Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
            _state.Cache.Put("ToolTipText_" + langauage, toolTipTexts);
            _state.Cache.Put("InstructionText_" + langauage, toolTipTexts);

            var questionRepository = ServiceLocator.Current.GetInstance<IRepository<QuestionMaster>>();
            var questions = _state.Cache.Get<IReadOnlyList<QuestionMaster>>();

            if (questions.IsNullOrEmpty())
            {
                questions = questionRepository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(questions);
            }

            var resLangRepository = ServiceLocator.Current.GetInstance<IRepository<ResourceLanguage>>();
            var languageName = langauage.ToString();

            var resourceTexts = resLangRepository.Fetch(r => r.LanguageMaster).Fetch(r => r.Resource).Where(r => r.LanguageMaster.Name == languageName).ToList();

            _state.Cache.Put("Resource_Text_" + langauage, resourceTexts.Where(a => a.Resource != null).ToDictionary(a => a.Resource.Name, a => a.Description));
            var state = ServiceLocator.Current.GetInstance<IState>();
            foreach (var item in TotalLangRecords)
            {
                state.Cache.Remove<Dictionary<string, string>>("Resource_Text_" + item.LanguageCode);
            }

            state.Cache.Remove<string>("BrandName");
        }

        public IReadOnlyList<ViewComboPair> GetCurrency(ApplicationCode applicationCode, int langCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<Currency>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var filteredListItems = _state.Cache.Get<IReadOnlyList<Currency>>(typeof(Currency).Name + langCode);
            var tempMasterlistitem = new List<Currency>();
            if (filteredListItems == null)
            {
                filteredListItems = repository.Where(a => a.IsActive && a.ApplicationMasterId == applicationMasterId).ToList();
                var allResources = GetAllResources(langCode, applicationCode);
                foreach (var item in filteredListItems)
                {
                    if (allResources.ContainsKey(item.Name))
                        tempMasterlistitem.Add(new Currency { Code = item.Code, Name = allResources[item.Name], DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });

                }
                _state.Cache.Put(typeof(Currency).Name + langCode, tempMasterlistitem);
            }
            else
            {
                foreach (var item in filteredListItems)
                {
                    tempMasterlistitem.Add(new Currency { Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                }
            }

            tempMasterlistitem = tempMasterlistitem.OrderBy(c => c.DisplayOrder).ToList();
            return tempMasterlistitem.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public List<ViewComboPair> GetNatureOfPayment(string countryCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<NatureOfPayment>>();
            List<NatureOfPayment> natureOfPayments;

            if (repository.Any(t => t.IsActive && t.Country.Code == countryCode))
                natureOfPayments = repository.Where(t => t.IsActive && t.Country.Code == countryCode).ToList();
            else
            {
                natureOfPayments = repository.Where(t => t.IsActive && t.Country.Code == null).ToList();

            }

            natureOfPayments = natureOfPayments.OrderBy(n => n.DisplayOrder).ToList();
            return natureOfPayments.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public List<ViewComboPair> GetNatureOfPayment()
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<NatureOfPayment>>();
            List<NatureOfPayment> natureOfPayments;
            natureOfPayments = repository.Where(t => t.IsActive).ToList();
            natureOfPayments = natureOfPayments.OrderBy(n => n.DisplayOrder).ToList();
            return natureOfPayments.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public List<ViewComboPair> GetCountries(ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<CountryMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = repository.Where(l => l.IsActive && l.ApplicationMasterId == applicationMasterId).ToList();
            listItems = listItems.OrderBy(l => l.DisplayOrder).ToList();
            return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public string GetCountryById(long countryId, ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<CountryMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var country = repository.FirstOrDefault(l => l.Id == countryId && l.IsActive && l.ApplicationMasterId == applicationMasterId);

            return country == null ? "" : country.Name;
        }
        public long GetSubCategoryId(string SubCategoryCode, long categroyid, ApplicationCode applicationCode)
        {
            var code = SubCategoryCode.ToString();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var questions = _state.Cache.Get<IReadOnlyCollection<MasterListItem>>();

            if (questions.IsNullOrEmpty())
            {
                questions = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(questions);
            }

            if (code != "")
                return questions.FirstOrDefault(q => q.Code == code && q.AssociatedListItemId == categroyid).Id;
            else
                return 0;
        }
        public long GetCategoryId(string CategoryCode, ApplicationCode applicationCode)
        {
            var code = CategoryCode.ToString();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var questions = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId && u.Code == code).FirstOrDefault();
            if (code != "" && questions != null)
            {
                return questions.Id;
            }
            else
                return 0;
        }
        public List<ViewComboPair> GetSubCategories(long categoryID, ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<List<MasterListItem>>();
            if (listItems.IsNullOrEmpty())
            {
                if (categoryID != 0)
                {
                    listItems = repository.Where(u => u.AssociatedListItemId == categoryID && u.IsActive == true && u.ApplicationMasterId == applicationMasterId).ToList();
                    return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
                }
                else
                {
                    listItems = repository.Where(u => u.IsActive == true && u.ApplicationMasterId == applicationMasterId && u.AssociatedListItemId != null).ToList();
                    return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
                }
            }
            else
                return null;
        }

        public long? GetActivityCategoryId(string ActivityCode, ApplicationCode applicationCode)
        {
            var code = ActivityCode.ToString();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<Request>>();
            var activities = _state.Cache.Get<IReadOnlyCollection<Request>>();
            long? ActivityCategoryId = 0;

            if (repository.IsNullOrEmpty())
            {
                activities = repository.Where(u => u.RequestDisplayId == ActivityCode && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(activities);
            }

            if (code != "")
            {
                var activity = repository.SingleOrDefault(q => q.RequestDisplayId == code);
                if (activity != null)
                    ActivityCategoryId = activity.ActivityCategoryId;
            }

            return ActivityCategoryId;
        }

        public string GetCategoryName(long Categoryid, ApplicationCode applicationCode)
        {
            var code = Categoryid;
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var questions = _state.Cache.Get<IReadOnlyCollection<MasterListItem>>();

            if (questions.IsNullOrEmpty())
            {
                questions = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(questions);
            }

            if (code != 0)
            {
                var question = questions.FirstOrDefault(q => q.Id == Categoryid);

                if (question != null)
                {
                    return question.Name;
                }
                else
                {
                    //mismatch
                    return "";
                }
            }
            else
                return "";
        }


        //BNAGRANT  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public string GetSubCategoryName(string SubCategoryCode, ApplicationCode applicationCode)
        {
            var code = SubCategoryCode.ToString();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var questions = _state.Cache.Get<IReadOnlyCollection<MasterListItem>>();

            if (questions.IsNullOrEmpty())
            {
                questions = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                //_state.Cache.Put(questions);
            }

            if (code != "")
                return null;
            //  return questions.FirstOrDefault(q => q.Code== code).Name;
            else
                return null;
        }

        public string GetGrantsType(string SubCategoryCode, ApplicationCode applicationCode)
        {
            var subcategoryname = GetSubCategoryName(SubCategoryCode, applicationCode);

            var repository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<List<MasterListItem>>();
            string granttype = string.Empty;
            if (listItems.IsNullOrEmpty())
            {


                listItems = repository.Where(u => u.IsActive == true && u.ApplicationMasterId == applicationMasterId).ToList();
                //_state.Cache.Put(listItems);


            }
            if (SubCategoryCode != "")
            {
                var activity = listItems.SingleOrDefault(q => q.Name == subcategoryname && q.AssociatedListItemId == null);
                if (activity != null)
                    granttype = activity.Name;
            }

            return granttype;
        }

        public long GetGrantsTypeId(string SubCategoryCode, ApplicationCode applicationCode)
        {
            var subcategoryname = GetSubCategoryName(SubCategoryCode, applicationCode);

            var repository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<List<MasterListItem>>();
            long granttype = 0;
            if (listItems.IsNullOrEmpty())
            {


                listItems = repository.Where(u => u.IsActive == true && u.ApplicationMasterId == applicationMasterId).ToList();
                //_state.Cache.Put(listItems);


            }
            if (SubCategoryCode != "")
            {
                var activity = listItems.SingleOrDefault(q => q.Name == subcategoryname && q.AssociatedListItemId == null);
                if (activity != null)
                    granttype = activity.Id;
            }

            return granttype;
        }

        public List<ViewComboPair> GetHCOList(string strName, ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<ServiceProvider>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<List<ServiceProvider>>();
            if (listItems.IsNullOrEmpty())
            {
                if (strName != "")
                {
                    listItems = repository.Where(u => u.UserName.StartsWith(strName) && u.IsActive && u.ConsultantType.Name.ToString() == ServiceProviderType.HCO.ToString()).ToList();
                    return listItems.Create(l => l.Id, l => l.UserName, l => l.ServiceProviderUniqueId).ToList();
                }
                else
                    return null;
            }
            else
                return null;
        }

        public string GetCategoryCode(long Categoryid, ApplicationCode applicationCode)
        {
            var code = Categoryid;
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var repository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var questions = _state.Cache.Get<IReadOnlyCollection<MasterListItem>>();

            if (questions.IsNullOrEmpty())
            {
                questions = repository.Where(u => u.IsActive && u.ApplicationMasterId == applicationMasterId).ToList();
                _state.Cache.Put(questions);
            }

            if (code != 0)
            {
                var question = questions.FirstOrDefault(q => q.Id == Categoryid);

                if (question != null)
                {
                    return question.Code;
                }
                else
                {
                    //mismatch
                    return "";
                }
            }
            else
                return "";
        }

        public List<ViewComboPair> GetContributionTypes(long categoryID, int LangID, ApplicationCode applicationCode)
        {
            var categoryCode = GetCategoryCode(categoryID, applicationCode);
            var ContributionTypeOtherId = GetCategoryId(GrantsType_1ThirdPartySupport.ToString(), applicationCode);

            var repository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = _state.Cache.Get<List<MasterListItem>>();
            if (listItems.IsNullOrEmpty())
            {
                if (categoryID != 0)
                {
                    if (categoryCode.ToString() == GrantsType_3EducationalSupportforHCP.ToString())
                    {
                        listItems = repository.Where(u => u.AssociatedListItemId == categoryID && u.IsActive == true && u.ApplicationMasterId == applicationMasterId).ToList();
                        return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
                    }
                    else if (string.IsNullOrEmpty(categoryCode.ToString()))
                    {
                        IReadOnlyList<ViewComboPair> ff = (List<ViewComboPair>)(GetMasterListItems(MasterListCode.ContributionType, applicationCode)).ToList();

                        return ff.ToList();
                        //listItems = repository.Where(u => u.AssociatedListItemId == categoryID && u.IsActive == true && u.ApplicationMasterId == applicationMasterId && u.CountryId == countryId).ToList();
                        //return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
                    }
                    else
                    {
                        listItems = repository.Where(u => u.AssociatedListItemId == ContributionTypeOtherId && u.IsActive == true && u.ApplicationMasterId == applicationMasterId).ToList();
                        return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
                    }
                }
                else
                {
                    listItems = repository.Where(u => u.IsActive == true && u.ApplicationMasterId == applicationMasterId && u.AssociatedListItemId != null).ToList();
                    return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
                }
            }
            if (categoryID != 0)
            {
                listItems = repository.Where(u => u.AssociatedListItemId == categoryID && u.IsActive == true && u.ApplicationMasterId == applicationMasterId).ToList();
                return listItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
            }
            else
                return null;
        }

        public IReadOnlyList<ViewComboPair> GetMasterListItems(MasterListCode masterListCode, ApplicationCode applicationCode)
        {
            _masterListRepository = ServiceLocator.Current.GetInstance<IRepository<MasterList>>();

            var masterListItems = _state.Cache.Get<IReadOnlyList<MasterListItem>>(masterListCode);
            var applicationMasterId = Convert.ToInt64(applicationCode);

            if (masterListItems.IsNullOrEmpty())
            {
                var masterListName = masterListCode.ToString();
                masterListItems = _masterListRepository.Fetch(m => m.AssociatedMasterList).FetchMany(m => m.ListValues)
                    .Single(m => m.Name.Equals(masterListName, StringComparison.InvariantCultureIgnoreCase) && m.IsActive).ListValues
                    .Where(l => l.IsActive && (l.IsDeleted == null || l.IsDeleted == false) && l.ApplicationMasterId == applicationMasterId).OrderBy(d => d.DisplayOrder).ToList();
                _state.Cache.Put(masterListCode, masterListItems);
            }
            return masterListItems.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public List<ViewComboPair> GetCurrencyList(ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<Currency>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = new List<Currency>();
            //   listItems = repository.Where(u => u.IsAvailableForFeeSchedule == true && u.IsActive).ToList();
            return listItems.Create(l => l.Id, l => l.Code, l => l.Name).ToList();
        }

        public Currency GetCurrency(ApplicationCode applicationCode)
        {
            var langId = "";
            IRepository<Currency> _currencyRepository = ServiceLocator.Current.GetInstance<IRepository<Currency>>();
            IRepository<CountryMaster> _countryRepository = ServiceLocator.Current.GetInstance<IRepository<CountryMaster>>();
            string defaultCurrency = "USD";
            var applicationMasterId = Convert.ToInt64(applicationCode);
            if (langId != "")
            {
                var lCurrencyList = _countryRepository.Fetch(c => c.Currency).Where(a => a.ApplicationMasterId == applicationMasterId);
                return _currencyRepository.Where(a => a.ApplicationMasterId == applicationMasterId).FirstOrDefault(h => h.Code == defaultCurrency);
            }
            else
                return _currencyRepository.Where(a => a.ApplicationMasterId == applicationMasterId).FirstOrDefault(h => h.Code == defaultCurrency);
        }

        public ServiceProvider GetServiceProvider(Int64 HCOId, ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<ServiceProvider>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var listItems = repository.Where(l => l.IsActive && l.Id == HCOId).ToList();

            return listItems.FirstOrDefault();
        }

        public IReadOnlyList<ViewComboPair> GetCurrencyWithSymbol(ApplicationCode applicationCode, int langCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<Currency>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            var filteredListItems = _state.Cache.Get<IReadOnlyList<Currency>>(typeof(Currency).Name + langCode);
            var tempMasterlistitem = new List<Currency>();
            if (filteredListItems == null)
            {
                filteredListItems = repository.Where(a => a.ApplicationMasterId == applicationMasterId).ToList();
                //var allResources = GetAllResources(langCode, applicationCode);
                foreach (var item in filteredListItems)
                {
                    // if (allResources.ContainsKey(item.Name))
                    tempMasterlistitem.Add(new Currency { Code = item.Code, Name = item.ShortName, DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });

                }
                // _state.Cache.Put(typeof(Currency).Name + langCode, tempMasterlistitem);
            }
            else
            {
                foreach (var item in filteredListItems)
                {
                    tempMasterlistitem.Add(new Currency { Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder, ApplicationMasterId = item.ApplicationMasterId, Id = item.Id });
                }
            }

            return tempMasterlistitem.Create(l => l.Id, l => l.Name, l => l.Code).ToList();
        }

        public List<CountryMaster> GetSelectedCountry(List<string> selectedCountryIds)
        {
            var repositoryCountry = ServiceLocator.Current.GetInstance<IRepository<CountryMaster>>();
            var country = repositoryCountry.Where(u => selectedCountryIds.Contains(u.Id.ToString())).ToList();
            return country;
        }

        public string GetCategoryName(string categoryCode, ApplicationCode applicationCode)
        {
            if (categoryCode != null)
            {
                var category = GetMasterListItem(MasterListCode.ActivityCategory, categoryCode, applicationCode);

                if (category != null)
                {
                    return category.Name;
                }
                else
                {
                    //mismatch
                    return "";
                }
            }
            else
                return "";
        }
        public ApplicationMaster GetApplicationMaster()
        {
            ApplicationMaster appMaster = new ApplicationMaster();
            appMaster = _state.Cache.Get<ApplicationMaster>("ApplicationMasterDetails");
            if (appMaster == null)
            {
                var appMasterRepository = ServiceLocator.Current.GetInstance<IRepository<ApplicationMaster>>();
                appMaster = appMasterRepository.FirstOrDefault();
                _state.Cache.Put<ApplicationMaster>("ApplicationMasterDetails", appMaster);
            }
            return appMaster;
        }

        /// <summary>
        /// This method is used to retrieve resource name based on actual value
        /// </summary>
        /// <param name="langCode"></param>
        /// <param name="description"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string GetNameFromDescription(int langCode, string description, string tableName)
        {
            var resLangRepository = ServiceLocator.Current.GetInstance<IRepository<ResourceLanguage>>();
            string resourceName = string.Empty;
            List<string> resourceTexts =
                          resLangRepository.Fetch(r => r.LanguageMaster)
                              .Fetch(r => r.Resource)
                              .Where(r => r.LanguageMaster.LanguageCode == langCode && r.Description == description)
                              .Select(r => r.Resource.Name).ToList();
            if (tableName == "DegreeMaster")
            {
                var degreeMaster = ServiceLocator.Current.GetInstance<IRepository<DegreeMaster>>();
                var query = from degree in degreeMaster
                            join resourceText in resourceTexts on degree.Name equals resourceText
                            select degree.Name.ToString();
                resourceName = query.FirstOrDefault();
            }
            else if (tableName == "SpecialityMaster")
            {
                var specialityMaster = ServiceLocator.Current.GetInstance<IRepository<SpecialityMaster>>();
                var query = from speciality in specialityMaster
                            join resourceText in resourceTexts on speciality.Name equals resourceText
                            select speciality.Name.ToString();
                resourceName = query.FirstOrDefault();
            }

            return resourceName;
        }
        public string GetMasterListCode(long id, ApplicationCode applicationCode, int langCode)
        {
            _masterListItemRepository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);


         return  _masterListItemRepository
                .FirstOrDefault(am => am.Id == id && am.IsActive).Code.ToString();
        }

        public Currency GetCurrencyById(long CurrencyId, int langCode)
        {
            var _currency = _state.Cache.Get<IReadOnlyList<Currency>>(typeof(Currency).Name + langCode);
            var _currencyRepository = ServiceLocator.Current.GetInstance<IRepository<Currency>>();
            string defaultCurrency = "USD";
            if (_currency == null)
            {
                if (CurrencyId > 0)
                {
                    return _currencyRepository.FirstOrDefault(h => h.Id == CurrencyId);
                }
                else
                {
                    return _currencyRepository.FirstOrDefault(h => h.Code == defaultCurrency);
                }
            }
            else
            {
                return _currency.FirstOrDefault(h => h.Id == CurrencyId);
            }
        }

        public string GetMasterListItemNameById(long id)
        {
            var masterListItemtRepository = ServiceLocator.Current.GetInstance<IRepository<MasterListItem>>();
            var masterListItem = masterListItemtRepository.Where(m => m.Id == id).FirstOrDefault();

            return masterListItem == null ? string.Empty : masterListItem.Name;
        }

        // Anshul Takalkar : 15 May 2019 start
        public QuestionMaster GetQuestionMasterByQuestionId(long questionId, ApplicationCode applicationCode)
        {
            var repository = ServiceLocator.Current.GetInstance<IRepository<QuestionMaster>>();
            var applicationMasterId = Convert.ToInt64(applicationCode);
            return repository.SingleOrDefault(u => u.Id == questionId && u.ApplicationMasterId == applicationMasterId);  
        }
        // Anshul Takalkar : 15 May 2019 end
    }
}
