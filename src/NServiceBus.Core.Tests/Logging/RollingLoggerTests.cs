namespace NServiceBus.Core.Tests.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class RollingLoggerTests
    {

        [Test]
        public void When_file_already_exists_that_file_is_written_to()
        {
            using (var tempPath = new TempPath())
            {
                var dateTime = new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero);
                var logger1 = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => dateTime
                };
                logger1.WriteLine("Foo");
                var files1 = tempPath.GetFiles();
                Assert.AreEqual(1, files1.Count);
                Assert.AreEqual($"Foo{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(files1.First()));
                var logger2 = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => dateTime
                };
                logger2.WriteLine("Bar");
                var files2 = tempPath.GetFiles();
                Assert.AreEqual(1, files2.Count);
                Assert.AreEqual($"Foo{Environment.NewLine}Bar{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(files2.First()));
            }
        }

        [Test]
        public void When_file_is_deleted_underneath_continues_to_write_afterwards()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero)
                };
                logger.WriteLine("Foo");
                var single = tempPath.GetSingle();
                File.Delete(single);
                logger.WriteLine("Bar");
                Assert.AreEqual($"Bar{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(single));
            }
        }

        [Test]
        public void When_file_is_locked_exception_is_swallowed()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero)
                };
                logger.WriteLine("Foo");
                var single = tempPath.GetSingle();
                using (LockFile(single))
                {
                    logger.WriteLine("Bar");
                }
                Assert.AreEqual($"Foo{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(single));
            }
        }

        static IDisposable LockFile(string single)
        {
            return new FileStream(single, FileMode.Open, FileAccess.Read, FileShare.None);
        }

        [Test]
        public void When_file_is_deleted_underneath_immediately_before_write()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLoggerThatDeletesBeforeWrite(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero)
                };
                logger.WriteLine("Foo");
                var singleFile = tempPath.GetSingle();
                File.Delete(singleFile);
                logger.WriteLine("Bar");
                Assert.AreEqual($"Bar{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(singleFile));
            }
        }

        class RollingLoggerThatDeletesBeforeWrite : RollingLogger
        {
            public RollingLoggerThatDeletesBeforeWrite(string targetDirectory)
                : base(targetDirectory)
            {
            }

            protected override void AppendLine(string message)
            {
                File.Delete(currentfilePath);
                base.AppendLine(message);
            }
        }

        [Test]
        public void When_file_already_exists_and_is_too_large_a_new_sequence_file_is_written()
        {
            using (var tempPath = new TempPath())
            {
                var utcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);

                var dateTime = new DateTimeOffset(2010, 10, 1, 0, 0, 0, utcOffset);
                var logger1 = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => dateTime
                };
                logger1.WriteLine("Some long text");
                var logger2 = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => dateTime
                };
                logger2.WriteLine("Bar");
                var files = tempPath.GetFiles();

                Assert.AreEqual(2, files.Count);

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_0.txt", Path.GetFileName(first));
                Assert.AreEqual($"Some long text{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(files.First()));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-01_1.txt", Path.GetFileName(second));
                Assert.AreEqual($"Bar{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(second));
            }
        }

        [Test]
        public void When_file_already_exists_with_wrong_date_a_file_is_written()
        {
            using (var tempPath = new TempPath())
            {
                var logger1 = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero)
                };
                logger1.WriteLine("Foo");
                var logger2 = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 2, 0, 0, 0, TimeSpan.Zero)
                };
                logger2.WriteLine("Bar");
                var files = tempPath.GetFiles();

                Assert.AreEqual(2, files.Count);

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_0.txt", Path.GetFileName(first));
                Assert.AreEqual($"Foo{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(files.First()));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-02_0.txt", Path.GetFileName(second));
                Assert.AreEqual($"Bar{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(second));
            }
        }

        [Test]
        public void When_line_is_write_line_appears_in_file()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory);
                logger.WriteLine("Foo");
                var singleFile = tempPath.GetSingle();
                Assert.AreEqual($"Foo{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(singleFile));
            }
        }

        [Test]
        public void When_multiple_lines_are_written_lines_appears_in_file()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory);
                logger.WriteLine("Foo");
                logger.WriteLine("Bar");
                var singleFile = tempPath.GetSingle();
                Assert.AreEqual($"Foo{Environment.NewLine}Bar{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(singleFile));
            }
        }

        [Test]
        public void When_max_file_size_is_exceeded_sequence_number_is_added()
        {
            using (var tempPath = new TempPath())
            {
                var utcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
                var logger = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, utcOffset)
                };
                logger.WriteLine("Some long text");
                logger.WriteLine("Bar");
                var files = tempPath.GetFiles();
                Assert.AreEqual(2, files.Count);

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_0.txt", Path.GetFileName(first));
                Assert.AreEqual($"Some long text{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(first));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-01_1.txt", Path.GetFileName(second));
                Assert.AreEqual($"Bar{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(second));
            }
        }

        [Test]
        public void When_many_sequence_files_are_written_the_max_is_not_exceeded()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero)
                };
                for (var i = 0; i < 100; i++)
                {
                    logger.WriteLine("Some long text");
                    Assert.LessOrEqual(tempPath.GetFiles().Count, 11);
                }
            }
        }

        [Test]
        public void When_new_write_causes_overlap_of_file_size_line_is_written_to_current_file()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10);
                logger.WriteLine("Foo");
                logger.WriteLine("Some long text");
                var singleFile = tempPath.GetSingle();
                Assert.AreEqual($"Foo{Environment.NewLine}Some long text{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(singleFile));
            }
        }

        [Test]
        public void When_date_changes_new_file_is_written()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero)
                };
                logger.WriteLine("Foo");
                logger.GetDate = () => new DateTimeOffset(2010, 10, 2, 0, 0, 0, TimeSpan.Zero);
                logger.WriteLine("Bar");
                var files = tempPath.GetFiles();
                Assert.AreEqual(2, files.Count);

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_0.txt", Path.GetFileName(first));
                Assert.AreEqual($"Foo{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(first));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-02_0.txt", Path.GetFileName(second));
                Assert.AreEqual($"Bar{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(second));
            }
        }

        [Test]
        public void When_getting_todays_log_file_sequence_number_is_used_in_sorting()
        {
            var today = new DateTimeOffset(2010, 10, 2, 0, 0, 0, TimeSpan.Zero);
            var logFiles = new List<RollingLogger.LogFile>
            {
                new RollingLogger.LogFile
                {
                    SequenceNumber = 0,
                    DatePart = today
                },
                new RollingLogger.LogFile
                {
                    SequenceNumber = 2,
                    DatePart = today
                },
            };
            var logFile = RollingLogger.GetTodaysNewest(logFiles, today);
            Assert.AreEqual(2, logFile.SequenceNumber);
        }

        [Test]
        public void When_getting_todays_log_file_only_today_is_respected()
        {
            var today = new DateTimeOffset(2010, 10, 2, 0, 0, 0, TimeSpan.Zero);
            var yesterday = new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero);
            var tomorrow = new DateTimeOffset(2010, 10, 3, 0, 0, 0, TimeSpan.Zero);
            var logFiles = new List<RollingLogger.LogFile>
            {
                new RollingLogger.LogFile
                {
                    SequenceNumber = 2,
                    DatePart = tomorrow
                },
                new RollingLogger.LogFile
                {
                    SequenceNumber = 2,
                    DatePart = yesterday
                },
                new RollingLogger.LogFile
                {
                    SequenceNumber = 0,
                    DatePart = today
                },
                new RollingLogger.LogFile
                {
                    SequenceNumber = 2,
                    DatePart = tomorrow
                },
                new RollingLogger.LogFile
                {
                    SequenceNumber = 2,
                    DatePart = yesterday
                },
            };
            var logFile = RollingLogger.GetTodaysNewest(logFiles, today);
            Assert.AreEqual(0, logFile.SequenceNumber);
        }

        [Test]
        public void When_many_files_written_over_size_old_files_are_deleted()
        {
            using (var tempPath = new TempPath())
            {
                var utcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
                var logger = new RollingLogger(tempPath.TempDirectory, numberOfArchiveFilesToKeep: 2, maxFileSize: 5)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, utcOffset)
                };
                logger.WriteLine("Long text0");
                logger.WriteLine("Long text1");
                logger.WriteLine("Long text2");
                logger.WriteLine("Long text3");
                logger.WriteLine("Long text4");
                var files = tempPath.GetFiles();
                Assert.AreEqual(3, files.Count, "Should be numberOfArchiveFilesToKeep + 1 (the current file) ");

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_2.txt", Path.GetFileName(first));
                Assert.AreEqual($"Long text2{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(first));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-01_3.txt", Path.GetFileName(second));
                Assert.AreEqual($"Long text3{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(second));

                var third = files[2];
                Assert.AreEqual("nsb_log_2010-10-01_4.txt", Path.GetFileName(third));
                Assert.AreEqual($"Long text4{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(third));
            }
        }

        [Test]
        public void When_many_files_written_over_dates_old_files_are_deleted()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory, numberOfArchiveFilesToKeep: 2)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero)
                };
                logger.WriteLine("Foo1");
                logger.GetDate = () => new DateTimeOffset(2010, 10, 2, 0, 0, 0, TimeSpan.Zero);
                logger.WriteLine("Foo2");
                logger.GetDate = () => new DateTimeOffset(2010, 10, 3, 0, 0, 0, TimeSpan.Zero);
                logger.WriteLine("Foo3");
                logger.GetDate = () => new DateTimeOffset(2010, 10, 4, 0, 0, 0, TimeSpan.Zero);
                logger.WriteLine("Foo4");
                logger.GetDate = () => new DateTimeOffset(2010, 10, 5, 0, 0, 0, TimeSpan.Zero);
                logger.WriteLine("Foo5");
                var files = tempPath.GetFiles();
                Assert.AreEqual(3, files.Count, "Should be numberOfArchiveFilesToKeep + 1 (the current file) ");

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-03_0.txt", Path.GetFileName(first));
                Assert.AreEqual($"Foo3{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(first));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-04_0.txt", Path.GetFileName(second));
                Assert.AreEqual($"Foo4{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(second));

                var third = files[2];
                Assert.AreEqual("nsb_log_2010-10-05_0.txt", Path.GetFileName(third));
                Assert.AreEqual($"Foo5{Environment.NewLine}", NonLockingFileReader.ReadAllTextWithoutLocking(third));
            }
        }

        [Test]
        public void When_line_is_write_file_has_correct_name()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTimeOffset(2010, 10, 1, 0, 0, 0, TimeSpan.Zero)
                };
                logger.WriteLine("Foo");
                var singleFile = tempPath.GetSingle();
                Assert.AreEqual("nsb_log_2010-10-01_0.txt", Path.GetFileName(singleFile));
            }
        }
        class TempPath : IDisposable
        {
            public TempPath()
            {
                TempDirectory = Path.Combine(Path.GetTempPath(), "nsbLogging", Guid.NewGuid().ToString());
                Directory.CreateDirectory(TempDirectory);
            }

            public string TempDirectory;

            public List<string> GetFiles()
            {
                return Directory.GetFiles(TempDirectory).OrderBy(x => x).ToList();
            }
            public string GetSingle()
            {
                return GetFiles().Single();
            }
            public void Dispose()
            {
                Directory.Delete(TempDirectory, true);
            }
        }

        static class NonLockingFileReader
        {
            internal static string ReadAllTextWithoutLocking(string path)
            {
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var textReader = new StreamReader(fileStream))
                    {
                        return textReader.ReadToEnd();
                    }
                }
            }
        }

    }
}
