using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit.Sdk;

namespace PollQT.Questrade
{
    public class FileDataAttribute : DataAttribute
    {
        private readonly string filePath;

        public FileDataAttribute(string filePath) : base() => this.filePath = filePath;


        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (testMethod == null) { throw new ArgumentNullException(nameof(testMethod)); }

            // Get the absolute path to the file
            var path = Path.IsPathRooted(this.filePath)
                ? this.filePath
                : Path.GetRelativePath(Directory.GetCurrentDirectory(), this.filePath);

            if (!File.Exists(path))
            {
                throw new ArgumentException($"Could not find file at path: {path}");
            }

            // Load the file
            var fileData = File.ReadAllText(this.filePath);
            var testCase = new object[] { fileData };

            return new List<object[]> { testCase };
        }
    }
}