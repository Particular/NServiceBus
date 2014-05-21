using System;
using System.Collections.Generic;

namespace NServiceBus.Core.Tests.Logging
{
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using NServiceBus.Logging;
    using NUnit.Framework;

    [TestFixture]
    public class RollingLoggerTests
    {


        [Test]
        public void When_file_already_exists_that_file_is_written_to()
        {
            using (var tempPath = new TempPath())
            {
                var dateTime = new DateTime(2010, 10, 1);
                var logger1 = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => dateTime
                };
                logger1.Write("Foo");
                var files1 = tempPath.GetFiles();
                Assert.AreEqual(1, files1.Count);
                Assert.AreEqual("Foo\r\n", NonBlocking.ReadAllText(files1.First()));
                var logger2 = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => dateTime
                };
                logger2.Write("Bar");
                var files2 = tempPath.GetFiles();
                Assert.AreEqual(1, files2.Count);
                Assert.AreEqual("Foo\r\nBar\r\n", NonBlocking.ReadAllText(files2.First()));
            }
        }

        [Test]
        public void When_file_is_deleted_underneath_continues_to_write_afterwards()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                logger.Write("Foo");
                var single = tempPath.GetSingle();
                File.Delete(single);
                logger.Write("Bar");
                Assert.AreEqual("Bar\r\n", NonBlocking.ReadAllText(single));
            }
        }

        [Test]
        public void When_file_is_locked_exception_is_swallowed()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                logger.Write("Foo");
                var single = tempPath.GetSingle();
                using (LockFile(single))
                {
                    logger.Write("Bar");
                }
                Assert.AreEqual("Foo\r\n", NonBlocking.ReadAllText(single));
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
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                logger.Write("Foo");
                var singleFile = tempPath.GetSingle();
                File.Delete(singleFile);
                logger.Write("Bar");
                Assert.AreEqual("Bar\r\n", NonBlocking.ReadAllText(singleFile));
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
                var dateTime = new DateTime(2010, 10, 1);
                var logger1 = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => dateTime
                };
                logger1.Write("Some long text");
                var logger2 = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => dateTime
                };
                logger2.Write("Bar");
                var files = tempPath.GetFiles();

                Assert.AreEqual(2, files.Count);

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_0.txt", Path.GetFileName(first));
                Assert.AreEqual("Some long text\r\n", NonBlocking.ReadAllText(files.First()));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-01_1.txt", Path.GetFileName(second));
                Assert.AreEqual("Bar\r\n", NonBlocking.ReadAllText(second));
            }
        }

        [Test]
        public void When_file_already_exists_with_wrong_date_a_file_is_written()
        {
            using (var tempPath = new TempPath())
            {
                var logger1 = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                logger1.Write("Foo");
                var logger2 = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => new DateTime(2010, 10, 2)
                };
                logger2.Write("Bar");
                var files = tempPath.GetFiles();

                Assert.AreEqual(2, files.Count);

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_0.txt", Path.GetFileName(first));
                Assert.AreEqual("Foo\r\n", NonBlocking.ReadAllText(files.First()));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-02_0.txt", Path.GetFileName(second));
                Assert.AreEqual("Bar\r\n", NonBlocking.ReadAllText(second));
            }
        }

        [Test]
        public void When_line_is_write_line_appears_in_file()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory);
                logger.Write("Foo");
                var singleFile = tempPath.GetSingle();
                Assert.AreEqual("Foo\r\n", NonBlocking.ReadAllText(singleFile));
            }
        }

        [Test]
        public void When_multiple_lines_are_written_lines_appears_in_file()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory);
                logger.Write("Foo");
                logger.Write("Bar");
                var singleFile = tempPath.GetSingle();
                Assert.AreEqual("Foo\r\nBar\r\n", NonBlocking.ReadAllText(singleFile));
            }
        }

        [Test]
        public void When_max_file_size_is_exceeded_sequence_number_is_added()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                logger.Write("Some long text");
                logger.Write("Bar");
                var files = tempPath.GetFiles();
                Assert.AreEqual(2, files.Count);

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_0.txt", Path.GetFileName(first));
                Assert.AreEqual("Some long text\r\n", NonBlocking.ReadAllText(first));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-01_1.txt", Path.GetFileName(second));
                Assert.AreEqual("Bar\r\n", NonBlocking.ReadAllText(second));
            }
        }

        [Test]
        [Explicit]
        public void MaxFilesTest()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                for (var i = 0; i < 1000000000; i++)
                {
                    logger.Write("Some long text");
                }
                Debug.WriteLine("finished");
            }
        }

        [Test]
        [Explicit]
        public void ManyWritesTest()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                for (var i = 0; i < 1000000; i++)
                {
                    logger.Write("Some long text");
                }
                Debug.WriteLine("finished");
            }
        }

        [Test]
        public void When_many_sequence_files_are_written_the_max_is_not_exceeded()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory, maxFileSize: 10)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                for (var i = 0; i < 100; i++)
                {
                    logger.Write("Some long text");
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
                logger.Write("Foo");
                logger.Write("Some long text");
                var singleFile = tempPath.GetSingle();
                Assert.AreEqual("Foo\r\nSome long text\r\n", NonBlocking.ReadAllText(singleFile));
            }
        }

        [Test]
        public void When_date_changes_new_file_is_written()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                logger.Write("Foo");
                logger.GetDate = () => new DateTime(2010, 10, 2);
                logger.Write("Bar");
                var files = tempPath.GetFiles();
                Assert.AreEqual(2, files.Count);

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_0.txt", Path.GetFileName(first));
                Assert.AreEqual("Foo\r\n", NonBlocking.ReadAllText(first));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-02_0.txt", Path.GetFileName(second));
                Assert.AreEqual("Bar\r\n", NonBlocking.ReadAllText(second));
            }
        }

        [Test]
        public void When_getting_todays_log_file_sequence_number_is_used_in_sorting()
        {
            var today = new DateTime(2010, 10, 2);
            var logFiles = new List<RollingLogger.LogFile>
        {
            new RollingLogger.LogFile
            {
                SequenceNumber = 0, DatePart = today
            },
            new RollingLogger.LogFile
            {
                SequenceNumber = 2, DatePart = today
            },
        };
            var logFile = RollingLogger.GetTodaysNewest(logFiles, today);
            Assert.AreEqual(2, logFile.SequenceNumber);
        }

        [Test]
        public void When_getting_todays_log_file_only_today_is_respected()
        {
            var today = new DateTime(2010, 10, 2);
            var yesterday = new DateTime(2010, 10, 1);
            var tomorrow = new DateTime(2010, 10, 3);
            var logFiles = new List<RollingLogger.LogFile>
        {
            new RollingLogger.LogFile
            {
                SequenceNumber = 2, DatePart = tomorrow
            },
            new RollingLogger.LogFile
            {
                SequenceNumber = 2, DatePart = yesterday
            },
            new RollingLogger.LogFile
            {
                SequenceNumber = 0, DatePart = today
            },
            new RollingLogger.LogFile
            {
                SequenceNumber = 2, DatePart = tomorrow
            },
            new RollingLogger.LogFile
            {
                SequenceNumber = 2, DatePart = yesterday
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
                var logger = new RollingLogger(tempPath.TempDirectory, numberOfArchiveFilesToKeep: 2, maxFileSize: 5)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                logger.Write("Long text0");
                logger.Write("Long text1");
                logger.Write("Long text2");
                logger.Write("Long text3");
                logger.Write("Long text4");
                var files = tempPath.GetFiles();
                Assert.AreEqual(3, files.Count, "Should be numberOfArchiveFilesToKeep + 1 (the current file) ");

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-01_2.txt", Path.GetFileName(first));
                Assert.AreEqual("Long text2\r\n", NonBlocking.ReadAllText(first));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-01_3.txt", Path.GetFileName(second));
                Assert.AreEqual("Long text3\r\n", NonBlocking.ReadAllText(second));

                var third = files[2];
                Assert.AreEqual("nsb_log_2010-10-01_4.txt", Path.GetFileName(third));
                Assert.AreEqual("Long text4\r\n", NonBlocking.ReadAllText(third));
            }
        }

        [Test]
        public void When_many_files_written_over_dates_old_files_are_deleted()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory, numberOfArchiveFilesToKeep: 2)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                logger.Write("Foo1");
                logger.GetDate = () => new DateTime(2010, 10, 2);
                logger.Write("Foo2");
                logger.GetDate = () => new DateTime(2010, 10, 3);
                logger.Write("Foo3");
                logger.GetDate = () => new DateTime(2010, 10, 4);
                logger.Write("Foo4");
                logger.GetDate = () => new DateTime(2010, 10, 5);
                logger.Write("Foo5");
                var files = tempPath.GetFiles();
                Assert.AreEqual(3, files.Count, "Should be numberOfArchiveFilesToKeep + 1 (the current file) ");

                var first = files[0];
                Assert.AreEqual("nsb_log_2010-10-03_0.txt", Path.GetFileName(first));
                Assert.AreEqual("Foo3\r\n", NonBlocking.ReadAllText(first));

                var second = files[1];
                Assert.AreEqual("nsb_log_2010-10-04_0.txt", Path.GetFileName(second));
                Assert.AreEqual("Foo4\r\n", NonBlocking.ReadAllText(second));

                var third = files[2];
                Assert.AreEqual("nsb_log_2010-10-05_0.txt", Path.GetFileName(third));
                Assert.AreEqual("Foo5\r\n", NonBlocking.ReadAllText(third));
            }
        }

        [Test]
        public void When_line_is_write_file_has_correct_name()
        {
            using (var tempPath = new TempPath())
            {
                var logger = new RollingLogger(tempPath.TempDirectory)
                {
                    GetDate = () => new DateTime(2010, 10, 1)
                };
                logger.Write("Foo");
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

        static class NonBlocking
        {
            public static string ReadAllText(string path)
            {
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var textReader = new StreamReader(fileStream))
                {
                    return textReader.ReadToEnd();
                }
            }
        }
    }
}
