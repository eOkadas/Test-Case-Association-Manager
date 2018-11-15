﻿using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using AssociateTestsToTestCases.Event;
using AssociateTestsToTestCases.Message;
using AssociateTestsToTestCases.Utility;
using AssociateTestsToTestCases.Access.TestCase;

namespace AssociateTestsToTestCases.Access.DevOps
{
    public class AzureDevOpsAccess
    {
        private const string AutomationName = "Automated";

        private List<TestMethod> _testMethodsNotMapped;

        private readonly Messages _messages;
        private readonly bool _verboseLogging;
        private readonly WriteToConsoleEventLogger _writeToConsoleEventLogger;

        public AzureDevOpsAccess(WriteToConsoleEventLogger writeToConsoleEventLogger, Messages messages, bool verboseLogging)
        {
            _messages = messages;
            _verboseLogging = verboseLogging;
            _writeToConsoleEventLogger = writeToConsoleEventLogger;
        }

        public List<TestMethod> Associate(MethodInfo[] testMethods, List<TestCase.TestCase> testCases,
            TestCaseAccess azureDevOpsAccessor, bool validationOnly, string testType)
        {
            _testMethodsNotMapped = testMethods.Select(x => new TestMethod(x.Name, x.DeclaringType.FullName)).ToList();

        foreach (var testCase in testCases)
            {
                var testMethod = testMethods.SingleOrDefault(x => x.Name == testCase.Title);

                if (testCase.Id == null)
                {
                    _writeToConsoleEventLogger.Write(string.Format(_messages.Associations.TestCaseSkipped, testCase.Title), _messages.Types.Warning, _messages.Reasons.MissingTestCaseId);
                    Counter.WarningMissingId += 1;
                    continue;
                }

                if (testCase.AutomationStatus == AutomationName && testMethod == null)
                {
                    _writeToConsoleEventLogger.Write(string.Format(_messages.Associations.TestCaseInfo, testCase.Title, testCase.Id), _messages.Types.Warning, _messages.Reasons.AssociatedTestMethodNotAvailable);
                    Counter.WarningTestMethodNotAvailable += 1;
                    continue;
                }

                if (testMethod == null)
                {
                    _writeToConsoleEventLogger.Write(string.Format(_messages.Associations.TestCaseInfo, testCase.Title, testCase.Id), _messages.Types.Warning, _messages.Reasons.MissingTestMethod);
                    Counter.WarningNoCorrespondingTestMethod += 1;
                    continue;
                }

                if (testCase.AutomationStatus == AutomationName)
                {
                    _testMethodsNotMapped.Remove(_testMethodsNotMapped.SingleOrDefault(x => x.Name.Equals(testCase.Title)));
                    Counter.Total += 1;
                    continue;
                }

                var operationSuccess = azureDevOpsAccessor.AssociateTestCaseWithTestMethod((int)testCase.Id, $"{testMethod.DeclaringType.FullName}.{testMethod.Name}", testMethod.Module.Name, Guid.NewGuid().ToString(), validationOnly, testType);

                if (!operationSuccess)
                {
                    _writeToConsoleEventLogger.Write(string.Format(_messages.Associations.TestCaseInfo, testCase.Title, testCase.Id), _messages.Types.Failure, _messages.Reasons.Association);
                    Counter.Error += 1;
                }

                if (_verboseLogging)
                {
                    _writeToConsoleEventLogger.Write(string.Format(_messages.Associations.TestCaseInfo, testCase.Title, testCase.Id), _messages.Types.Success, _messages.Reasons.Association);
                }

                _testMethodsNotMapped.Remove(_testMethodsNotMapped.Single(x => x.Name == testCase.Title));

                Counter.Total += 1;
                Counter.Success += 1;
            }

            Counter.Error += _testMethodsNotMapped.Count;

            return _testMethodsNotMapped;
        }
    }
}
