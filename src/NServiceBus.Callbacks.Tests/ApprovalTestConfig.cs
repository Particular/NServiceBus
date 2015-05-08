using ApprovalTests.Reporters;
#if(DEBUG)
[assembly: UseReporter(typeof(AllFailingTestsClipboardReporter), typeof(DiffReporter))]
#else
[assembly: UseReporter(typeof(DiffReporter))]
#endif