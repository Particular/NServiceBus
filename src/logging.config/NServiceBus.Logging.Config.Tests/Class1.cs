using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NServiceBus.Logging.Config.Tests
{
  [TestFixture]
  public class WhenConfiguringLog4Net
  {
    [Test]
    public void Test()
    {
      Configure.With().Log4Net();

      LogManager.GetLogger("Test").Debug("Testing");
    }
  }
}
