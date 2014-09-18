using System;
using System.IO;
using System.Linq;
using System.Text;

static class ExceptionExtensions
{
    public static string GetCleanStackTrace(this Exception exception)
    {
        if (exception.StackTrace == null)
        {
            return string.Empty;
        }
        using (var stringReader = new StringReader(exception.StackTrace))
        {
            var stringBuilder = new StringBuilder();
            while (true)
            {
                var line = stringReader.ReadLine();
                if (line == null)
                {
                    break;
                }
                if (line.Contains("at lambda_method(Closure"))
                {
                    continue;
                }
                stringBuilder.AppendLine(line.Split(new[]
                {
                    " in "
                }, StringSplitOptions.RemoveEmptyEntries).First().Trim());
            }
            return stringBuilder.ToString().Trim();
        }
    }
}