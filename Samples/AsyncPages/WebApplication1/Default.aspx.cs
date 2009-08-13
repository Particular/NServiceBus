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
            var result = ar.AsyncState as CompletionResult;
            if (result == null)
                return;

            Label1.Text = Enum.GetName(typeof (ErrorCodes), result.ErrorCode);
        }

        private IAsyncResult beginning(object sender, EventArgs e, AsyncCallback cb, object extraData)
        {
            int number = int.Parse(TextBox1.Text);
            var command = new Command {Id = number};

            return Global.Bus.Send(command).Register(cb, null);
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            //RegisterAsyncTask(new PageAsyncTask(
            //    beginning,
            //    ending,
            //    null,
            //    null
            //    ));

            int number = int.Parse(TextBox1.Text);
            var command = new Command { Id = number };

            Global.Bus.Send(command).RegisterWebCallback(
                i => Label1.Text = Enum.GetName(typeof (ErrorCodes), i)
                , null
                );
        }

    }
}
