using ApprovalTests.Reporters;
#if NETFRAMEWORK
[assembly: UseReporter(typeof(DiffReporter), typeof(AllFailingTestsClipboardReporter))]
#else
[assembly: UseReporter(typeof(NUnitReporter))]
#endif
