﻿using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using AssociateTestsToTestCases.Event;
using AssociateTestsToTestCases.Message;
using AssociateTestsToTestCases.Utility;
using AssociateTestsToTestCases.Access.DevOps;
using AssociateTestsToTestCases.Manager.Output;
using AssociateTestsToTestCases.Access.TestCase;

namespace AssociateTestsToTestCases.Manager.DevOps
{
    public class AzureDevOpsManager : IDevOpsManager
    {
        private readonly Messages _messages;
        private readonly IOutputManager _outputManager;
        private readonly ITestCaseAccess _testCaseAccess;
        private readonly IDevOpsAccess _azureDevOpsAccess;
        private readonly IWriteToConsoleEventLogger _writeToConsoleEventLogger;

        public AzureDevOpsManager(Messages messages, IOutputManager outputManager, ITestCaseAccess testCaseAccess, IDevOpsAccess azureDevOpsAccess, IWriteToConsoleEventLogger writeToConsoleEventLogger)
        {
            _messages = messages;
            _outputManager = outputManager;
            _testCaseAccess = testCaseAccess;
            _azureDevOpsAccess = azureDevOpsAccess;
            _writeToConsoleEventLogger = writeToConsoleEventLogger;
        }

        public void Associate(MethodInfo[] methods, List<Access.TestCase.TestCase> testCases, bool validationOnly, string testType)
        {
            _writeToConsoleEventLogger.Write(_messages.Stages.Association.Status, _messages.Types.Stage);

            var testMethods = methods.Select(x => new TestMethod(x.Name, x.Module.Name, x.DeclaringType.FullName)).ToList();

            var testMethodsNotAvailable = _azureDevOpsAccess.ListTestCasesWithNotAvailableTestMethods(testCases, testMethods);
            if (testMethodsNotAvailable.Count != 0)
            {
                testMethodsNotAvailable.ForEach(x => _writeToConsoleEventLogger.Write(string.Format(_messages.Associations.TestCaseWithNotAvailableTestMethod, x.Title, x.Id, x.AutomatedTestName), _messages.Types.Warning, _messages.Reasons.AssociatedTestMethodNotAvailable));
            }

            var testMethodsAssociationErrorCount = _azureDevOpsAccess.Associate(testMethods, testCases, _testCaseAccess, validationOnly, testType);
            if (testMethodsAssociationErrorCount != 0)
            {
                _writeToConsoleEventLogger.Write(string.Format(_messages.Stages.Association.Failure, testMethodsAssociationErrorCount), _messages.Types.Error);

                _outputManager.OutputSummary(methods, testCases);

                throw new InvalidOperationException(string.Format(_messages.Stages.Association.Failure, testMethodsAssociationErrorCount));
            }

            _writeToConsoleEventLogger.Write(string.Format(_messages.Stages.Association.Success, Counter.Success), _messages.Types.Success);
        }
    }
}
