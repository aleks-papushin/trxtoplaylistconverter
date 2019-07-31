namespace TestResultsToPlaylist
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            var resultsDocumentsList = IterateThroughDocumentsAndGetResults();
            var testResultsList = GetDescendantsElementsFromDocumentsByName(resultsDocumentsList, "UnitTestResult");
            var failedTestsIds = testResultsList
                .Where(res => res.Attribute("outcome").Value.Equals("Failed"))
                .Select(res => res.Attribute("testId").Value);

            var allTestMethods = GetDescendantsElementsFromDocumentsByName(resultsDocumentsList, "TestMethod");

            var failedTestCasesFullNames = allTestMethods
                .Where(t => failedTestsIds.Contains(
                    t.Ancestors()
                        .Single(a => a.Name.LocalName.Equals("UnitTest"))
                            .Attribute("id").Value))
                .Select(t => ($"{t.Attribute("className").Value}.{t.Attribute("name").Value}"));

            var playlist = new XDocument(
                new XElement("Playlist",
                    new XAttribute("Version", "1.0"),
                        from testCase in failedTestCasesFullNames
                        select new XElement("Add",
                            new XAttribute("Test", testCase))));

            XmlWriterSettings xws = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            OutputToConsoleForDebugging(playlist, xws);

            playlist.Save("test.playlist");
        }

        private static void OutputToConsoleForDebugging(XDocument playlist, XmlWriterSettings xws)
        {
            StringBuilder sb = new StringBuilder();

            using (XmlWriter xw = XmlWriter.Create(sb, xws))
            {
                playlist.Save(xw);
            }

            Console.WriteLine(sb.ToString());
        }

        private static IEnumerable<XElement> GetDescendantsElementsFromDocumentsByName(IEnumerable<XDocument> from, string name)
        {
            var descendants = Enumerable.Empty<XElement>();

            foreach (var item in from)
            {
                descendants = descendants.Concat(item.Descendants().Elements()
                    .Where(el => el.Name.LocalName.Equals(name)));
            }

            return descendants;
        }

        private static IEnumerable<XDocument> IterateThroughDocumentsAndGetResults()
        {
            var files = GetTrxFilesInCurrentDirectory();
            var testRunResultsList = new List<XDocument>();

            foreach(var f in files)
            {
                testRunResultsList.Add(XDocument.Load(f));
            }

            return testRunResultsList;
        }

        private static IEnumerable<string> GetTrxFilesInCurrentDirectory()
        {
            var ext = ".trx";
            var dir = Directory.GetCurrentDirectory();
            var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                 .Where(s => ext.Contains(Path.GetExtension(s)));
            return files;
        }
    }
}
