﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace AssociateTestsToTestCases
{
    public class VstsAccessor
    {
        private readonly WorkItemTrackingHttpClient _workItemTrackingHttpClient;
        private const string AutomationStatusName = "Microsoft.VSTS.TCM.AutomationStatus";
        private const string AutomationTestIdName = "Microsoft.VSTS.TCM.AutomatedTestId";
        private const string AutomatedTestStorageName = "Microsoft.VSTS.TCM.AutomatedTestStorage";
        private const string AutomatedTestName = "Microsoft.VSTS.TCM.AutomatedTestName";
        private const string AutomatedName = "Automated";

        public VstsAccessor(string collectionUri, string personalAccessToken)
        {
            var connection = new VssConnection(new Uri(collectionUri), new VssBasicCredential(string.Empty, personalAccessToken));
            _workItemTrackingHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();
        }

        public List<VstsTestCase> GetVstsTestCases()
        {
            var workItemQuery = _workItemTrackingHttpClient.QueryByWiqlAsync(new Wiql()
            {
                Query =
                    "SELECT * From WorkItems Where [System.WorkItemType] = 'Test Case'"
            }).Result;

            var testcasesId = workItemQuery.WorkItems?.Select(x => x.Id).ToArray();
            var testcases = _workItemTrackingHttpClient.GetWorkItemsAsync(testcasesId).Result;

            return CreateVstsTestCaseList(testcases);
        }

        public bool AssociateWorkItemWithTestMethod(int workItemId, string methodName, string assemblyName, string automatedTestId)
        {
            var patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.TCM.AutomatedTestName",
                    Value = methodName
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.TCM.AutomatedTestStorage",
                    Value = assemblyName
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.TCM.AutomatedTestId",
                    Value = automatedTestId
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.TCM.AutomatedTestType",
                    Value = "" // Todo: what's the purpose of this attribute?
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.TCM.AutomationStatus",
                    Value = AutomatedName
                }
            };

            var result = _workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, workItemId, true).Result;

            return result.Fields[AutomationStatusName].ToString() == AutomatedName &&
                   result.Fields[AutomationTestIdName].ToString() == automatedTestId &&
                   result.Fields[AutomatedTestStorageName].ToString() == assemblyName &&
                   result.Fields[AutomatedTestName].ToString() == methodName;
        }

        private List<VstsTestCase> CreateVstsTestCaseList(List<WorkItem> workItems)
        {
            var vstsTestCases = new List<VstsTestCase>();

            foreach (var workItem in workItems)
            {
                var workItemTitle = workItem.Fields?.FirstOrDefault(x => x.Key == "System.Title").Value.ToString();

                if (workItem.Fields[AutomationStatusName].ToString() == AutomatedName)
                {
                    continue;
                }

                vstsTestCases.Add(new VstsTestCase()
                {
                    Id = workItem.Id,
                    Title = workItemTitle
                });
            }

            return vstsTestCases;
        }
    }
}
