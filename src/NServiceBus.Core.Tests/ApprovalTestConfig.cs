using ApprovalTests.Reporters;
#if NET452
[assembly: UseReporter(typeof(DiffReporter), typeof(AllFailingTestsClipboardReporter))]
#else
[assembly: UseReporter(typeof(NUnitReporter))]
#endif
