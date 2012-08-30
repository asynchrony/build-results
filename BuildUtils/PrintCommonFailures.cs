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
//        [Ignore]
        [Test]
        public void TestName()
        {
            print();
        }

        public void print()
        {
            var buildDirName = @"Z:\hudson_home\jobs\Branch_Runner_2\builds";
            var directories = Directory.GetDirectories(buildDirName);
            
//            var testResults = new Dictionary<string, int>();

            var tests = new HashSet<TestResult>();

            foreach (var dir in directories)
            {
                var files = Directory.GetFiles(dir, "junitResult.xml");
                if (files.Any())
                {
                    var fileName = files[0];
                    XPathDocument doc = new XPathDocument(fileName);
                    XPathNavigator nav = doc.CreateNavigator();
                    XPathExpression expr = nav.Compile("//case[errorStackTrace]");
                    XPathNodeIterator iterator = nav.Select(expr);
                    
                    while (iterator.MoveNext())
                    {
                        XPathNavigator testCaseNavigator = iterator.Current;

                        XPathNavigator classNameNav = testCaseNavigator.SelectSingleNode("className/text()");
                        XPathNavigator testNameNav = testCaseNavigator.SelectSingleNode("testName/text()");

                        var failingTest = classNameNav + "." + testNameNav;

                        var testResult = GetTestResult(failingTest,tests);

                        TestResult newTestResult;
                        if (testResult != null)
                        {
                            tests.Remove(testResult);
                           newTestResult =  new TestResult{BuildName = testResult.BuildName,TestName = testResult.TestName,TestFailures = testResult.TestFailures++};
                           newTestResult.TestFailures = testResult.TestFailures++;
                           
                        } else
                        {
                           newTestResult = new TestResult{BuildName = dir.Substring(dir.LastIndexOf("\\")+1),TestName = failingTest,TestFailures = 1};
                        }
                        tests.Add(newTestResult);
                            
                    }
                }
 
            }
            foreach (var failingTest in tests.ToList().OrderBy(x=>x.TestFailures).Reverse())
            {
                Console.WriteLine(failingTest);
            }
        }

        private TestResult GetTestResult(string testName, IEnumerable<TestResult> allTests )
        {
            return allTests.ToList().SingleOrDefault(x => x.TestName == testName);
        }
    }

    class TestResult
    {
        public string BuildName { get; set; }
        public string TestName { get; set; }
        public int TestFailures { get; set; }



        public bool Equals(TestResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.TestName, TestName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (TestResult)) return false;
            return Equals((TestResult) obj);
        }

        public override int GetHashCode()
        {
            return (TestName != null ? TestName.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return TestFailures + " : " + BuildName + " - " + TestName;
        }
    }
}
