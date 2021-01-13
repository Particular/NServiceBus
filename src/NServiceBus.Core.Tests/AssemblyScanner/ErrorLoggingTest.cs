namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.IO;
    using System.Reflection;
    using Hosting.Helpers;
    using NUnit.Framework;
    using Particular.Approvals;

    [TestFixture]
    public class ErrorLoggingTest
    {
        [Test]
        public void ApproveErrorLog_FileLoadException_NServiceBus()
        {
            var exception = new ReflectionTypeLoadException(new Type[0], new[]
            {
                new Exception("Generic exception 1"),
                new FileLoadException("File load exception", typeof(AssemblyScanner).Assembly.FullName),
                new Exception("Generic exception 2"),
            });

            var formattedException = AssemblyScanner.FormatReflectionTypeLoadException("MyFile", exception);
            Approver.Verify(formattedException);
        }

        [Test]
        public void ApproveErrorLog_FileLoadException_NServiceBus_Only()
        {
            var exception = new ReflectionTypeLoadException(new Type[0], new Exception[]
            {
                new FileLoadException("File load exception", typeof(AssemblyScanner).Assembly.FullName),
            });

            var formattedException = AssemblyScanner.FormatReflectionTypeLoadException("MyFile", exception);
            Approver.Verify(formattedException);
        }

        [Test]
        public void ApproveErrorLog_FileLoadException()
        {
            var exception = new ReflectionTypeLoadException(new Type[0], new[]
            {
                new Exception("Generic exception 1"),
                new FileLoadException("File load exception", typeof(ErrorLoggingTest).Assembly.FullName),
                new Exception("Generic exception 2"),
            });

            var formattedException = AssemblyScanner.FormatReflectionTypeLoadException("MyFile", exception);
            Approver.Verify(formattedException);
        }

        [Test]
        public void ApproveErrorLog_FileLoadException_Only()
        {
            var exception = new ReflectionTypeLoadException(new Type[0], new Exception[]
            {
                new FileLoadException("File load exception", typeof(ErrorLoggingTest).Assembly.FullName),
            });

            var formattedException = AssemblyScanner.FormatReflectionTypeLoadException("MyFile", exception);
            Approver.Verify(formattedException);
        }

        [Test]
        public void ApproveErrorLog_FileLoadException_NServiceBus_Other()
        {
            var exception = new ReflectionTypeLoadException(new Type[0], new Exception[]
            {
                new FileLoadException("File load exception", typeof(ErrorLoggingTest).Assembly.FullName),
                new FileLoadException("File load exception", typeof(AssemblyScanner).Assembly.FullName),
            });

            var formattedException = AssemblyScanner.FormatReflectionTypeLoadException("MyFile", exception);
            Approver.Verify(formattedException);
        }

        [Test]
        public void ApproveErrorLog_GenericExceptions()
        {
            var exception = new ReflectionTypeLoadException(new Type[0], new[]
            {
                new Exception("Generic exception 1"),
                new Exception("Generic exception 2"),
            });

            var formattedException = AssemblyScanner.FormatReflectionTypeLoadException("MyFile", exception);
            Approver.Verify(formattedException);
        }

        [Test]
        public void ApproveErrorLog_NoExceptions()
        {
            var exception = new ReflectionTypeLoadException(new Type[0], new Exception[0]);

            var formattedException = AssemblyScanner.FormatReflectionTypeLoadException("MyFile", exception);
            Approver.Verify(formattedException);
        }
    }
}