using System;
using System.Web.UI;
using Messages;
using NServiceBus;

namespace WebApplication1
{
    public partial class _Default : Page
    {
        protected void Button1_Click(object sender, EventArgs e)
        {
            int number = int.Parse(TextBox1.Text);
            var command = new Command { Id = number };

            Global.Bus.Send(command).RegisterWebCallback<ErrorCodes>(
                code => Label1.Text = Enum.GetName(typeof (ErrorCodes), code)
                , null
                );
        }

    }
}
