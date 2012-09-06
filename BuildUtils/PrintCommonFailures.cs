using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using NUnit.Framework;

namespace BuildUtils
{
    public class PrintCommonFailures
    {
        public static void Main(string[] args)
        {
            PrintAllTestFailures();
        }

        [Test]
        public void TestAllFailures()
        {
            PrintAllTestFailures();
        }

        [Test]
        public void TestAllSingleFailure()
        {
            PrintSingleTest();
        }

        public static void PrintSingleTest()
        {
            var directories = GetBuildDirectories();

            var tests = new List<TestResult>();

            foreach (var dir in directories)
            {
                var files = Directory.GetFiles(dir, "junitResult.xml");
                if (files.Any())
                {
                    var fileName = files[0];
                    var iterator = GetTestCasesWithErrors("UserAcceptanceTests.Features.ProductFeature","RefreshOnProductAfterAddingCommentShouldNOTResultInErrorBugFix",fileName);

                    while (iterator.MoveNext())
                    {
                        var failingTest = GetFailingTestName(iterator);

                        var testResult = new TestResult {BuildName = GetBuildName(dir),TestName = failingTest};

                        tests.Add(testResult);
                    }
                }
 
            }
            foreach (var failingTest in tests.ToList().OrderBy(x=>x.BuildName).Reverse())
            {
                Console.WriteLine(failingTest);
            }
        }

        public static void PrintAllTestFailures()
        {
            var directories = GetBuildDirectories();

            var tests = new HashSet<AggregateTestResult>();

            var totalBuilds = 0;
            var days = 1;

            foreach (var dir in directories.Where(x=>Directory.GetCreationTime(x) > DateTime.Now.AddDays(-1 * days)))
            {
                totalBuilds++;
                var files = Directory.GetFiles(dir, "junitResult.xml");
                if (files.Any())
                {
                    var fileName = files[0];
                    var iterator = GetTestCasesWithErrors(fileName);

                    while (iterator.MoveNext())
                    {
                        var failingTest = GetFailingTestName(iterator);

                        var testResult = GetTestResult(failingTest, tests);

                        UpdateResults(failingTest, dir, tests, testResult);
                    }
                }
            }
            foreach (var failingTest in tests.ToList().OrderBy(x=>x.FailureCount).Reverse())
            {
                Console.WriteLine(failingTest);
            }
            Console.WriteLine("Tests performed during last: " + days + " days");
            Console.WriteLine("Total Builds performed during test run: " + totalBuilds);
        }

        private static void UpdateResults(string failingTest, string dir, HashSet<AggregateTestResult> tests, AggregateTestResult aggregateTestResult)
        {
            AggregateTestResult newAggregateTestResult;
            if (aggregateTestResult != null)
            {
                tests.Remove(aggregateTestResult);
                newAggregateTestResult = new AggregateTestResult {BuildName = aggregateTestResult.BuildName, TestName = aggregateTestResult.TestName, FailureCount = ++aggregateTestResult.FailureCount};
            }
            else
            {
                newAggregateTestResult = new AggregateTestResult {BuildName = GetBuildName(dir), TestName = failingTest, FailureCount = 1};
            }
            tests.Add(newAggregateTestResult);
        }

        private static string GetBuildName(string dir)
        {
            return dir.Substring(dir.LastIndexOf("\\") + 1);
        }

        private static string GetFailingTestName(XPathNodeIterator iterator)
        {
            XPathNavigator testCaseNavigator = iterator.Current;

            XPathNavigator classNameNav = testCaseNavigator.SelectSingleNode("className/text()");
            XPathNavigator testNameNav = testCaseNavigator.SelectSingleNode("testName/text()");

            var failingTest = classNameNav + "." + testNameNav;
            return failingTest;
        }

        private static XPathNodeIterator GetTestCasesWithErrors(string fileName)
        {
            XPathDocument doc = new XPathDocument(fileName);
            XPathNavigator nav = doc.CreateNavigator();
            XPathExpression expr = nav.Compile("//case[errorStackTrace]");
            XPathNodeIterator iterator = nav.Select(expr);
            return iterator;
        }

        private static XPathNodeIterator GetTestCasesWithErrors(string className, string testName, string fileName)
        {
            XPathDocument doc = new XPathDocument(fileName);
            XPathNavigator nav = doc.CreateNavigator();
            XPathExpression expr = nav.Compile("//case[errorStackTrace and className='" +className+"' and testName='" +testName+"']");
            XPathNodeIterator iterator = nav.Select(expr);
            return iterator;
        }

        private static string[] GetBuildDirectories()
        {
            var buildDirName = @"Z:\hudson_home\jobs\Branch_Runner_2\builds";
            var directories = Directory.GetDirectories(buildDirName);
            return directories;
        }

        private static AggregateTestResult GetTestResult(string testName, IEnumerable<AggregateTestResult> allTests )
        {
            return allTests.ToList().SingleOrDefault(x => x.TestName == testName);
        }
    }

    internal class AggregateTestResult
    {
        public string BuildName { get; set; }
        public string TestName { get; set; }
        public int FailureCount { get; set; }



        public bool Equals(AggregateTestResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.TestName, TestName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (AggregateTestResult)) return false;
            return Equals((AggregateTestResult) obj);
        }

        public override int GetHashCode()
        {
            return (TestName != null ? TestName.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return String.Join(";", new[] {FailureCount + "", BuildName, TestName});
        }
    }

    internal class TestResult
    {
        public string TestName { get; set; }
        public string BuildName { get; set; }
        public override string ToString()
        {
            return String.Join(";", new[] {BuildName, TestName});
        }        
    }
}
