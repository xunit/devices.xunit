using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Xunit.Runners.ResultChannels
{
    /// <summary>
    /// Generates a TRX-like report for the testrun
    /// </summary>
    public class TrxResultChannel : IResultChannel
    {
        private const string xmlNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        private const string testListId = "8c84fa94-04c1-424b-9868-57a2d4851a1d";
        private System.IO.Stream outputStream;
        private readonly bool disposeStream;
        private XmlDocument doc;
        private int testCount;
        private int testFailed;
        private int testSucceeded;
        private XmlElement rootNode;
        private XmlElement resultsNode;
        private XmlElement testDefinitions;
        private XmlElement header;
        private XmlElement testEntries;
#if __IOS__ || MAC
        private bool showNetworkIndicator;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="TrxResultChannel"/> class.
        /// </summary>
        /// <param name="filename">The name of the file to write the report to.</param>
        public TrxResultChannel(string filename) : this(System.IO.File.Open(filename, System.IO.FileMode.Create), true) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrxResultChannel"/> class.
        /// </summary>
        /// <param name="outputStream">The stream to write the report to.</param>
        /// <param name="disposeStream">Whether this instance should dispose the provided stream on completion</param>
        public TrxResultChannel(System.IO.Stream outputStream, bool disposeStream = false)
        {
            this.outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
            this.disposeStream = disposeStream;
        }

        /// <summary>
        /// Creates a TCP connection and writes the report to the socket at the provided hostname and port
        /// </summary>
        /// <param name="hostName">hostname</param>
        /// <param name="port">port</param>
        /// <returns>TrxResultChannel</returns>
        public static Task<TrxResultChannel> CreateTcpTrxResultChannel(string hostName, int port)
        {
            if ((port < 0) || (port > ushort.MaxValue))
            {
                throw new ArgumentException("port");
            }

            var HostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
            var Port = port;

#if __IOS__ || MAC
            UIKit.UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
#endif
            try
            {
#if __IOS__ || MAC || __ANDROID__ || __MACOS__
                var client = new System.Net.Sockets.TcpClient(hostName, port);
                var channel = new TrxResultChannel(client.GetStream(), true);
#if __IOS__ || MAC
                channel.showNetworkIndicator = true;
#endif
                return Task.FromResult(channel);
#elif WINDOWS_PHONE || NETFX_CORE
                var socket = new Windows.Networking.Sockets.StreamSocket();
                return socket.ConnectAsync(new Windows.Networking.HostName(hostName), port.ToString(CultureInfo.InvariantCulture))
                    .AsTask()
                    .ContinueWith(_ => new TrxResultChannel(socket.OutputStream.AsStreamForWrite(), true));
#endif
            }
            catch
            {
#if __IOS__ || MAC
                UIKit.UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
#endif
                throw;
            }
        }

        /// <summary>
        /// The key name of the trait that is used for writing the Category field to the report
        /// </summary>
        public string CategoryTraitName { get; set; } = "Category";

        /// <summary>
        /// The name of the test run to include in the report header
        /// </summary>
        public string TestRunName { get; set; } = "";

        /// <summary>
        /// The name of the user running the report header
        /// </summary>
        public string TestRunUser { get; set; } = "";

        Task<bool> IResultChannel.OpenChannel(string message)
        {
            testCount = testFailed = testSucceeded = 0;
            doc = new XmlDocument();
            rootNode = doc.CreateElement("TestRun", xmlNamespace);
            rootNode.SetAttribute("id", Guid.NewGuid().ToString());
            rootNode.SetAttribute("name", TestRunName);
            rootNode.SetAttribute("runUser", TestRunUser);
            doc.AppendChild(rootNode);
            header = doc.CreateElement("Times", xmlNamespace);
            header.SetAttribute("finish", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            header.SetAttribute("start", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            header.SetAttribute("creation", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            rootNode.AppendChild(header);
            resultsNode = doc.CreateElement("Results", xmlNamespace);
            rootNode.AppendChild(resultsNode);
            testDefinitions = doc.CreateElement("TestDefinitions", xmlNamespace);
            rootNode.AppendChild(testDefinitions);
            testEntries = doc.CreateElement("TestEntries", xmlNamespace);
            rootNode.AppendChild(testEntries);
            var testLists = doc.CreateElement("TestLists", xmlNamespace);
            var testList = doc.CreateElement("TestList", xmlNamespace);
            testList.SetAttribute("name", "Results Not in a List");
            testList.SetAttribute("id", testListId);
            testLists.AppendChild(testList);
            rootNode.AppendChild(testLists);
            return Task.FromResult(true);
        }

        void ITestListener.RecordResult(TestResultViewModel result)
        {
            var id = Guid.NewGuid().ToString();
            var executionId = Guid.NewGuid().ToString();
            var resultNode = doc.CreateElement("UnitTestResult", xmlNamespace);
            resultNode.SetAttribute("outcome", ToTrxStatus(result.TestCase.Result));
            resultNode.SetAttribute("testType", "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b");
            resultNode.SetAttribute("testListId", testListId);
            resultNode.SetAttribute("executionId", executionId);
            var idx = result.TestCase.DisplayName.LastIndexOf('.');
            var testName = result.TestCase.TestCase.TestMethod.Method.Name;
            var className = result.TestCase.TestCase.TestMethod.TestClass.Class.Name;
            resultNode.SetAttribute("testName", testName);
            resultNode.SetAttribute("testId", id);
            resultNode.SetAttribute("duration", result.Duration.ToString("G", CultureInfo.InvariantCulture));
            resultNode.SetAttribute("computerName", "");

            if (result.TestCase.Result == TestState.Failed)
            {
                testFailed++;
                var output = doc.CreateElement("Output", xmlNamespace);
                var errorInfo = doc.CreateElement("ErrorInfo", xmlNamespace);
                var message = doc.CreateElement("Message", xmlNamespace);
                message.InnerText = result.ErrorMessage;
                var stackTrace = doc.CreateElement("StackTrace", xmlNamespace);
                stackTrace.InnerText = result.ErrorStackTrace;
                output.AppendChild(errorInfo);
                errorInfo.AppendChild(message);
                errorInfo.AppendChild(stackTrace);
                resultNode.AppendChild(output);
            }
            else
            {
                testSucceeded++;
            }
            testCount++;

            resultsNode.AppendChild(resultNode);

            var testNode = doc.CreateElement("UnitTest", xmlNamespace);
            testNode.SetAttribute("name", testName);
            testNode.SetAttribute("id", id);
            testNode.SetAttribute("storage", result.TestCase.AssemblyFileName);
            XmlElement properties = null;
            List<string> categories = null;
            foreach (var prop in result.TestCase.TestCase.Traits)
            {
                if (prop.Key == CategoryTraitName)
                {
                    categories = prop.Value;
                    continue;
                }
                foreach (var v in prop.Value)
                {
                    if (properties == null)
                    {
                        properties = doc.CreateElement("Properties", xmlNamespace);
                        testNode.AppendChild(properties);
                    }

                    var property = doc.CreateElement("Property", xmlNamespace);
                    var key = doc.CreateElement("Key", xmlNamespace);
                    key.InnerText = prop.Key;
                    property.AppendChild(key);
                    var value = doc.CreateElement("Value", xmlNamespace);
                    value.InnerText = v;
                    property.AppendChild(value);
                    properties.AppendChild(property);
                }
            }

            if (categories != null && categories.Any())
            {
                var testCategory = doc.CreateElement("TestCategory", xmlNamespace);
                foreach (var category in categories)
                {
                    var item = doc.CreateElement("TestCategoryItem", xmlNamespace);
                    item.SetAttribute("TestCategory", category);
                    testCategory.AppendChild(item);
                }
                testNode.AppendChild(testCategory);
            }
            var execution = doc.CreateElement("Execution", xmlNamespace);
            execution.SetAttribute("id", executionId);
            testNode.AppendChild(execution);
            var testMethodName = doc.CreateElement("TestMethod", xmlNamespace);
            testMethodName.SetAttribute("name", testName);
            testMethodName.SetAttribute("className", className);
            testMethodName.SetAttribute("codeBase", result.TestCase.AssemblyFileName);
            testNode.AppendChild(testMethodName);

            testDefinitions.AppendChild(testNode);

            var testEntry = doc.CreateElement("TestEntry", xmlNamespace);
            testEntry.SetAttribute("testListId", testListId);
            testEntry.SetAttribute("testId", id);
            testEntry.SetAttribute("executionId", executionId);
            testEntries.AppendChild(testEntry);
        }

        Task IResultChannel.CloseChannel()
        {
            header.SetAttribute("finish", DateTime.Now.ToString("O"));
            var resultSummary = doc.CreateElement("ResultSummary", xmlNamespace);
            resultSummary.SetAttribute("outcome", "Completed");
            var counters = doc.CreateElement("Counters", xmlNamespace);
            counters.SetAttribute("passed", testSucceeded.ToString(CultureInfo.InvariantCulture));
            counters.SetAttribute("failed", testFailed.ToString(CultureInfo.InvariantCulture));
            counters.SetAttribute("total", testCount.ToString(CultureInfo.InvariantCulture));
            resultSummary.AppendChild(counters);
            rootNode.AppendChild(resultSummary);
            doc.Save(outputStream);
            if (disposeStream)
            {
                outputStream.Dispose();
            }

#if __IOS__ || MAC
            if (showNetworkIndicator)
                UIKit.UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
#endif
            return Task.CompletedTask;
        }

        private static string ToTrxStatus(TestState result)
        {
            switch (result)
            {
                case TestState.NotRun: return "NotRunnable";
                case TestState.Skipped: return "NotExecuted";
                case TestState.Failed:
                case TestState.Passed:
                default: return result.ToString();
            }
        }
    }
}
