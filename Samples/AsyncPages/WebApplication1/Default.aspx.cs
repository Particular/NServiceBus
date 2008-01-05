using System;
using System.Web.UI;
using Messages;
using NServiceBus;

namespace WebApplication1
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        private void ending(IAsyncResult ar)
        {
            CompletionResult result = ar.AsyncState as CompletionResult;
            if (result == null)
                return;

            Label1.Text = Enum.GetName(typeof (ErrorCodes), result.errorCode);
        }

        private IAsyncResult beginning(object sender, EventArgs e, AsyncCallback cb, object extraData)
        {
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();
            IBus bus = builder.Build<IBus>();

            int number = int.Parse(TextBox1.Text);
            Command command = new Command();
            command.Id = number;

            return bus.Send(command).Register(cb, null);
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            RegisterAsyncTask(new PageAsyncTask(
                this.beginning,
                this.ending,
                null,
                null
                ));
        }

    }
}
