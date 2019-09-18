using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Sitecore.ContentTesting;
using Sitecore.ContentTesting.ContentSearch.Models;
using Sitecore.ContentTesting.Data;
using Sitecore.ContentTesting.Helpers;
using Sitecore.ContentTesting.Model;
using Sitecore.ContentTesting.Model.Data.Items;
using Sitecore.ContentTesting.Requests.Controllers;
using Sitecore.ContentTesting.ViewModel;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace Sitecore.Support.ContentTesting.Requests.Controllers.Optimization
{
    public class SupportTestsController : ContentTestingControllerBase
    {
        public SupportTestsController() : this(ContentTestingFactory.Instance.ContentTestStore)
        {
        }
        public SupportTestsController(IContentTestStore contentTestStore)
        {
            this._contentTestStore = contentTestStore;
        }
        
        private readonly IContentTestStore _contentTestStore;

        [HttpGet]
        public IHttpActionResult GetHistoricalTests(int? page = new int?(), int? pageSize = new int?(), string hostItemId = null, string searchText = null, string language = null)
        {
            int? nullable = page;
            page = new int?((nullable != null) ? nullable.GetValueOrDefault() : 1);
            nullable = pageSize;
            pageSize = new int?((nullable != null) ? nullable.GetValueOrDefault() : 20);
            DataUri hostItemDataUri = null;
            ID result = null;
            if (!string.IsNullOrEmpty(hostItemId))
            {
                ID.TryParse(hostItemId, out result);
            }
            Language language2 = Context.Language;
            if (!string.IsNullOrEmpty(language))
            {
                Language.TryParse(language, out language2);
            }
            if ((result != (ID) null) && !string.IsNullOrEmpty(language))
            {
                hostItemDataUri = new DataUri(result, language2);
            }
            IEnumerable<TestingSearchResultItem> historicalTests = base.ContentTestStore.GetHistoricalTests(hostItemDataUri, searchText);
            List<ExecutedTestViewModel> list = new List<ExecutedTestViewModel>();
            foreach (TestingSearchResultItem item in (from x in historicalTests
                                                      orderby x.UpdatedDate
                                                      select x).Skip<TestingSearchResultItem>(((page.Value - 1) * pageSize.Value)).Take<TestingSearchResultItem>(pageSize.Value))
            {
                Item item2 = Database.GetItem(item.Uri);
                if (item2 != null)
                {
                    TestDefinitionItem test = TestDefinitionItem.Create(item2);
                    if ((test != null) && (item.HostItemUri != null))
                    {
                        Item contentItem = item2.Database.GetItem(item.HostItemUri);
                        if (contentItem != null)
                        {
                            TestConfiguration configuration = new TestConfiguration(contentItem, test.Device.TargetID, test);
                            ExecutedTestViewModel model1 = new ExecutedTestViewModel();
                            model1.HostPageId = contentItem.ID.ToString();
                            model1.HostPageUri = contentItem.Uri.ToDataUri();
                            model1.HostPageName = contentItem.DisplayName;
                            model1.DeviceId = configuration.DeviceId.ToString();
                            model1.DeviceName = configuration.DeviceName;
                            model1.Language = configuration.LanguageName;
                            model1.CreatedBy = FormattingHelper.GetFriendlyUserName(item2.Security.GetOwner());
                            model1.ItemId = test.ID.ToString();
                            model1.ContentOnly = test.Variables.Count == test.PageLevelTestVariables.Count;
                            ExecutedTestViewModel model = model1;
                            HistoricalDataModel historicalTestData = test.GetHistoricalTestData();
                            if (historicalTestData == null)
                            {
                                model.Date = "--";
                                model.ExperienceCount = configuration.TestSet.GetExperiences().Count<TestExperience>();
                            }
                            else
                            {
                                model.Date = historicalTestData.IsTestCanceled ? (historicalTestData.EndDate + $" ({Translate.Text("Test was cancelled")})") : historicalTestData.EndDate;
                                model.ExperienceCount = historicalTestData.ExperiencesCount;
                                model.Days = historicalTestData.TestDuration;
                                model.Effect = historicalTestData.Effect;
                                model.TestScore = historicalTestData.TestScore;
                            }
                            model.EffectCss = (model.Effect >= 0.0) ? ((model.Effect <= 0.0) ? "value-nochange" : "value-increase") : "value-decrease";
                            list.Add(model);
                        }
                    }
                }
            }
            TestListViewModel content = new TestListViewModel();
            content.Items = (IEnumerable<BaseTestViewModel>)list;
            content.TotalResults = historicalTests.Count<TestingSearchResultItem>();
            return base.Json<TestListViewModel>(content);
        }

    }
}